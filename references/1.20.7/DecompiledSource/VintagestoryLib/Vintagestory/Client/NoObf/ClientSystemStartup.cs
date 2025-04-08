using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ClientSystemStartup : ClientSystem
{
	public static ClientSystemStartup instance;

	private int assetsLoadedPrerequisites;

	private volatile int blockShapesPrerequisites;

	private volatile int itemShapesPrerequisites;

	private volatile int entityShapesPrerequisites;

	public volatile int loadSoundsSlowPrerequisites;

	private Thread collectionThreadBlockAtlas;

	private Thread collectionThreadEntityAtlas;

	private Thread collectionThreadItemAtlas;

	private bool lightLevelsReceived;

	private Packet_ServerAssets pkt_srvrassets;

	private int multiplayerAuthAttempts;

	private string multiplayerAuthRetryToken;

	private ILogger logger => game.Logger;

	public override string Name => "startup";

	public ClientSystemStartup(ClientMain game)
		: base(game)
	{
		instance = this;
		game.PacketHandlers[77] = HandleLoginTokenAnswer;
		game.PacketHandlers[1] = HandleServerIdent;
		game.PacketHandlers[82] = HandleQueue;
		game.PacketHandlers[73] = HandleServerReady;
		game.PacketHandlers[4] = HandleLevelInitialize;
		game.PacketHandlers[5] = HandleLevelDataChunk;
		game.PacketHandlers[21] = HandleWorldMetaData;
		game.PacketHandlers[19] = HandleServerAssets;
		game.PacketHandlers[6] = HandleLevelFinalize;
		game.ServerInfo = new ServerInformation();
	}

	private void HandleQueue(Packet_Server packet)
	{
		game.Logger.Notification("Client is in connect queue at position: " + packet.QueuePacket.Position);
		game.Connectdata.PositionInQueue = packet.QueuePacket.Position;
	}

	private void HandleLoginTokenAnswer(Packet_Server packet)
	{
		game.networkProc.StartUdpConnectRequest(packet.Token.Token);
		if (!game.IsSingleplayer)
		{
			if (game.ScreenRunningGame.ScreenManager.ClientIsOffline)
			{
				onMpTokenReceived(EnumAuthServerResponse.Good, "offline");
				return;
			}
			multiplayerAuthAttempts = 0;
			multiplayerAuthRetryToken = packet.Token.Token;
			game.ScreenRunningGame.ScreenManager.sessionManager.RequestMpToken(onMpTokenReceived, packet.Token.Token);
		}
		else
		{
			sendIdentificationPacket();
		}
	}

	private void onMpTokenReceived(EnumAuthServerResponse resp, string errorreason)
	{
		switch (resp)
		{
		case EnumAuthServerResponse.Good:
			multiplayerAuthAttempts = 0;
			multiplayerAuthRetryToken = null;
			logger.Debug("Okay, received single use mp token from auth server. Sending ident packet");
			game.EnqueueGameLaunchTask(sendIdentificationPacket, "sendIdentPacket");
			return;
		case EnumAuthServerResponse.Offline:
			if (multiplayerAuthRetryToken != null && multiplayerAuthAttempts++ < 1)
			{
				Thread.Sleep(900);
				game.ScreenRunningGame.ScreenManager.sessionManager.RequestMpToken(onMpTokenReceived, multiplayerAuthRetryToken);
				return;
			}
			break;
		}
		game.Connectdata.ErrorMessage = Lang.Get("Failed requesting mp token from auth server. Server says: {0}", errorreason);
		multiplayerAuthRetryToken = null;
	}

	private void sendIdentificationPacket()
	{
		game.SendPacketClient(ClientPackets.CreateIdentificationPacket(game.Platform, game.Connectdata));
		logger.Debug("Ident packet sent.");
	}

	private void HandleServerAssets(Packet_Server packet)
	{
		pkt_srvrassets = packet.Assets;
		if (lightLevelsReceived)
		{
			game.EnqueueGameLaunchTask(HandleServerAssets_Initial, "serverassetsreceived");
		}
		else
		{
			logger.VerboseDebug("Received server assets packet; waiting on light levels packet before decoding.");
		}
	}

	private void HandleServerIdent(Packet_Server packet)
	{
		game.ServerNetworkVersion = packet.Identification.NetworkVersion;
		game.ServerGameVersion = packet.Identification.GameVersion;
		game.Connectdata.PositionInQueue = 0;
		if ("1.20.8" != packet.Identification.NetworkVersion)
		{
			game.disconnectReason = Lang.Get("disconnect-wrongversion", "1.20.7", "1.20.8", game.ServerGameVersion, game.ServerNetworkVersion);
			game.Platform.Logger.Warning(game.disconnectReason);
			game.exitReason = "client<=>server game version mismatch";
			game.DestroyGameSession(gotDisconnected: true);
			return;
		}
		game.TrySetWorldConfig(packet.Identification.WorldConfiguration);
		game.ServerMods = parseMods(packet.Identification.Mods, packet.Identification.ModsCount);
		int cnt = packet.Identification.ServerModIdBlackListCount;
		if (cnt > 0)
		{
			string[] blockedModIds = new string[cnt];
			Array.Copy(packet.Identification.ServerModIdBlackList, blockedModIds, cnt);
			game.ServerModIdBlacklist = new List<string>(blockedModIds);
		}
		cnt = packet.Identification.ServerModIdWhiteListCount;
		if (cnt > 0)
		{
			string[] whitelistedModIds = new string[cnt];
			Array.Copy(packet.Identification.ServerModIdWhiteList, whitelistedModIds, cnt);
			game.ServerModIdWhitelist = new List<string>(whitelistedModIds);
		}
		logger.VerboseDebug("Handling ServerIdentification packet; requires remapping is " + (packet.Identification.RequireRemapping > 0));
		game.ServerInfo.connectdata = game.Connectdata;
		game.ServerInfo.Seed = packet.Identification.Seed;
		game.ServerInfo.SavegameIdentifier = packet.Identification.SavegameIdentifier;
		game.ServerInfo.ServerName = packet.Identification.ServerName;
		game.ServerInfo.Playstyle = packet.Identification.PlayStyle;
		game.ServerInfo.PlayListCode = packet.Identification.PlayListCode;
		game.ServerInfo.RequiresRemappings = packet.Identification.RequireRemapping > 0;
		for (int i = 0; i < game.clientSystems.Length; i++)
		{
			game.clientSystems[i].OnServerIdentificationReceived();
			if (!game.IsSingleplayer && game.clientSystems[i] is SystemModHandler)
			{
				AfterAssetsLoaded();
			}
		}
		game.Platform.Logger.Notification("Processed server identification");
		if (packet.Identification.MapSizeX != game.WorldMap.MapSizeX || packet.Identification.MapSizeY != game.WorldMap.MapSizeY || packet.Identification.MapSizeZ != game.WorldMap.MapSizeZ)
		{
			game.WorldMap.OnMapSizeReceived(new Vec3i(packet.Identification.MapSizeX, packet.Identification.MapSizeY, packet.Identification.MapSizeZ), new Vec3i(packet.Identification.RegionMapSizeX, packet.Identification.RegionMapSizeY, packet.Identification.RegionMapSizeZ));
		}
		game.Platform.Logger.Notification("Map initialized");
	}

	private static List<ModId> parseMods(Packet_ModId[] mods, int modCount)
	{
		List<ModId> servermods = new List<ModId>();
		for (int i = 0; i < modCount; i++)
		{
			Packet_ModId p = mods[i];
			ModId modid = new ModId
			{
				Id = p.Modid,
				Version = p.Version,
				Name = p.Name,
				NetworkVersion = p.Networkversion,
				RequiredOnClient = p.RequiredOnClient
			};
			servermods.Add(modid);
		}
		return servermods;
	}

	private void HandleWorldMetaData(Packet_Server packet)
	{
		game.TrySetWorldConfig(packet.WorldMetaData.WorldConfiguration);
		game.WorldMap.BlockLightLevels = new float[packet.WorldMetaData.BlockLightlevelsCount];
		game.WorldMap.BlockLightLevelsByte = new byte[game.WorldMap.BlockLightLevels.Length];
		game.WorldMap.SunLightLevels = new float[packet.WorldMetaData.SunLightlevelsCount];
		game.WorldMap.SunLightLevelsByte = new byte[game.WorldMap.SunLightLevels.Length];
		game.WorldMap.SunBrightness = packet.WorldMetaData.SunBrightness;
		for (int k = 0; k < packet.WorldMetaData.BlockLightlevelsCount; k++)
		{
			game.WorldMap.BlockLightLevels[k] = CollectibleNet.DeserializeFloat(packet.WorldMetaData.BlockLightlevels[k]);
			game.WorldMap.BlockLightLevelsByte[k] = (byte)(255f * game.WorldMap.BlockLightLevels[k]);
			game.WorldMap.SunLightLevels[k] = CollectibleNet.DeserializeFloat(packet.WorldMetaData.SunLightlevels[k]);
			game.WorldMap.SunLightLevelsByte[k] = (byte)(255f * game.WorldMap.SunLightLevels[k]);
		}
		game.WorldMap.hueLevels = new byte[ColorUtil.HueQuantities];
		for (int j = 0; j < ColorUtil.HueQuantities; j++)
		{
			game.WorldMap.hueLevels[j] = (byte)(4 * j);
		}
		game.WorldMap.satLevels = new byte[ColorUtil.SatQuantities];
		for (int i = 0; i < ColorUtil.SatQuantities; i++)
		{
			game.WorldMap.satLevels[i] = (byte)(32 * i);
		}
		ClientWorldMap.seaLevel = packet.WorldMetaData.SeaLevel;
		ScreenManager.Platform.Logger.VerboseDebug("Received world meta data");
		game.TerrainChunkTesselator.LightlevelsReceived();
		game.WorldMap.OnLightLevelsReceived();
		if (lightLevelsReceived)
		{
			return;
		}
		lightLevelsReceived = true;
		if (pkt_srvrassets != null)
		{
			game.EnqueueGameLaunchTask(delegate
			{
				HandleServerAssets_Initial();
			}, "lightlevelsreceived");
		}
	}

	private void HandleServerAssets_Initial()
	{
		logger.Notification("Received server assets");
		logger.VerboseDebug("Received server assets");
		game.AssetsReceived = true;
		if (game.IsSingleplayer)
		{
			game.modHandler.SinglePlayerStart();
			if (game.exitToDisconnectScreen)
			{
				return;
			}
			game.modHandler.PreStartMods();
			logger.VerboseDebug("Single player game - starting mods on the client-side");
			game.modHandler.StartMods();
			TyronThreadPool.QueueTask(ReloadExternalAssets_Async);
		}
		logger.VerboseDebug("Server assets - done step 1, next steps off-thread");
		game.SuspendMainThreadTasks = true;
		game.AssetLoadingOffThread = true;
		TyronThreadPool.QueueTask(HandleServerAssets_Step1);
	}

	private void ReloadExternalAssets_Async()
	{
		try
		{
			game.modHandler.ReloadExternalAssets();
		}
		finally
		{
			game.EnqueueGameLaunchTask(AfterAssetsLoaded, "assetsLoaded");
		}
	}

	private void HandleServerAssets_Step1()
	{
		LoadEntityTypes();
		if (game.disposed)
		{
			return;
		}
		StartLoadingEntityShapesWhenReady();
		LoadItemTypes();
		if (!game.disposed)
		{
			StartLoadingItemShapesWhenReady();
			LoadBlockTypes();
			if (!game.disposed)
			{
				StartLoadingBlockShapesWhenReady();
			}
		}
	}

	internal void AfterAssetsLoaded()
	{
		game.modHandler.OnAssetsLoaded();
		logger.VerboseDebug("All client-side assets loaded and patched; configs, shapes, textures and sounds are now available for loading");
		if (!game.disposed)
		{
			game.BlockAtlasManager.CreateNewAtlas("blocks");
			game.TesselatorManager.PrepareToLoadShapes();
			StartLoadingEntityShapesWhenReady();
			StartLoadingItemShapesWhenReady();
			StartLoadingBlockShapesWhenReady();
		}
	}

	private void LoadBlockTypes()
	{
		int maxBlockId = 0;
		Packet_BlockType[] packetisedBlocks = pkt_srvrassets.Blocks;
		int maxCount = pkt_srvrassets.BlocksCount;
		for (int i = 0; i < packetisedBlocks.Length && i < maxCount; i++)
		{
			maxBlockId = Math.Max(maxBlockId, packetisedBlocks[i].BlockId);
		}
		Block[] blocks = new Block[maxBlockId + 1];
		int quantityBlocks = PopulateBlocks(blocks, 0, maxCount);
		game.FastBlockTextureSubidsByBlockAndFace = new int[blocks.Length][];
		game.Blocks = new BlockList(game, blocks);
		logger.VerboseDebug("Populated " + quantityBlocks + " BlockTypes from server, highest BlockID is " + maxBlockId);
	}

	private void LoadItemTypes()
	{
		int maxItemId = 0;
		Packet_ItemType[] packetisedItems = pkt_srvrassets.Items;
		int maxCount = pkt_srvrassets.ItemsCount;
		for (int i = 0; i < packetisedItems.Length && i < maxCount; i++)
		{
			maxItemId = Math.Max(maxItemId, packetisedItems[i].ItemId);
		}
		int listSize = Math.Max(4000, maxItemId + 1);
		List<Item> items = new List<Item>(listSize);
		int quantityItems = PopulateItems(items, listSize);
		game.Items = items;
		logger.VerboseDebug("Populated " + quantityItems + " ItemTypes from server, highest ItemID is " + maxItemId);
	}

	public void StartLoadingBlockShapesWhenReady()
	{
		if (Interlocked.Increment(ref blockShapesPrerequisites) == 2)
		{
			blockShapesPrerequisites = 0;
			logger.VerboseDebug("Starting to load block shapes");
			collectionThreadBlockAtlas = new Thread((ThreadStart)delegate
			{
				loadBlockAtlasManagerAsync(game.Blocks);
			});
			collectionThreadBlockAtlas.IsBackground = true;
			collectionThreadBlockAtlas.Name = "collecttexturesasync";
			collectionThreadBlockAtlas.Start();
		}
	}

	public void StartLoadingItemShapesWhenReady()
	{
		if (Interlocked.Increment(ref itemShapesPrerequisites) == 2)
		{
			itemShapesPrerequisites = 0;
			logger.VerboseDebug("Starting to load item shapes");
			collectionThreadItemAtlas = new Thread((ThreadStart)delegate
			{
				prepareAsync(game.Items);
			});
			collectionThreadItemAtlas.IsBackground = true;
			collectionThreadItemAtlas.Start();
		}
	}

	public void StartLoadingEntityShapesWhenReady()
	{
		if (Interlocked.Increment(ref entityShapesPrerequisites) == 2)
		{
			entityShapesPrerequisites = 0;
			logger.VerboseDebug("Starting to load entity shapes");
			collectionThreadEntityAtlas = new Thread((ThreadStart)delegate
			{
				loadAsyncEntityAtlas(game.EntityTypes);
			});
			collectionThreadEntityAtlas.IsBackground = true;
			collectionThreadEntityAtlas.Priority = ThreadPriority.AboveNormal;
			collectionThreadEntityAtlas.Start();
		}
	}

	public void StartSlowLoadingSoundsWhenReady()
	{
		if (Interlocked.Increment(ref loadSoundsSlowPrerequisites) != 2)
		{
			return;
		}
		loadSoundsSlowPrerequisites = 0;
		logger.VerboseDebug("Starting to load sounds fully");
		ScreenManager.soundAudioDataAsyncLoadTemp.Clear();
		foreach (KeyValuePair<AssetLocation, AudioData> val in ScreenManager.soundAudioData)
		{
			ScreenManager.soundAudioDataAsyncLoadTemp[val.Key] = val.Value;
		}
		ScreenManager.LoadSoundsSlow_Async(game);
	}

	private void loadBlockAtlasManagerAsync(IList<Block> blocks)
	{
		try
		{
			ResolveBlockItemStacks();
			OrderedDictionary<AssetLocation, UnloadableShape> shapes = game.TesselatorManager.LoadBlockShapes(blocks);
			game.Logger.VerboseDebug("BlockTextureAtlas start collecting textures (already holds " + (game.BlockAtlasManager.Count - 1) + " colormap textures)");
			game.BlockAtlasManager.CollectTextures(blocks, shapes);
			if (game.disposed)
			{
				return;
			}
			WaitFor(ref game.DoneColorMaps, 2000, "colormaps", "loading all the colormaps from file: config / colormaps.json");
			if (game.disposed)
			{
				return;
			}
			game.BlockAtlasManager.PopulateTextureAtlassesFromTextures();
			if (!game.disposed)
			{
				StartSlowLoadingMusicAndSounds();
				game.EnqueueGameLaunchTask(delegate
				{
					FinaliseTextureAtlas(game.BlockAtlasManager, "block", HandleServerAssets_Step9);
				}, "ServerAssetsReceived");
			}
		}
		catch (ThreadAbortException)
		{
		}
		catch (Exception e)
		{
			game.Logger.Fatal("Caught unhandled exception in BlockTextureCollection thread. Exiting game.");
			game.Logger.Fatal(e);
			game.Platform.XPlatInterface.ShowMessageBox("Client Thread Crash", "Whoops, a client game thread crashed, please check the client-main.log for more Information. I will now exit the game (and stop the server if in singleplayer). Sorry about that :(");
			game.KillNextFrame = true;
		}
	}

	private void loadAsyncEntityAtlas(List<EntityProperties> entityClasses)
	{
		try
		{
			game.TesselatorManager.LoadEntityShapesAsync(entityClasses, game.api);
			TyronThreadPool.QueueTask(LoadColorMapsAndCatalogSoundsIfSingleplayer);
			game.EntityAtlasManager.CollectTextures(entityClasses);
			if (game.disposed)
			{
				return;
			}
			game.EntityAtlasManager.CreateNewAtlas("entities");
			if (game.disposed)
			{
				return;
			}
			game.EntityAtlasManager.PopulateTextureAtlassesFromTextures();
			if (!game.disposed)
			{
				game.EnqueueGameLaunchTask(delegate
				{
					FinaliseTextureAtlas(game.EntityAtlasManager, "entity", null);
				}, "ServerAssetsReceived");
			}
		}
		catch (ThreadAbortException)
		{
		}
		catch (Exception e)
		{
			game.Logger.Fatal("Caught unhandled exception in EntityTextureCollection thread. Exiting game.");
			game.Logger.Fatal(e);
			game.Platform.XPlatInterface.ShowMessageBox("Client Thread Crash", "Whoops, a client game thread crashed, please check the client-main.log for more Information. I will now exit the game (and stop the server if in singleplayer). Sorry about that :(");
			game.KillNextFrame = true;
		}
	}

	private void prepareAsync(IList<Item> items)
	{
		try
		{
			Dictionary<AssetLocation, UnloadableShape> shapes = game.TesselatorManager.LoadItemShapes(items);
			game.ItemAtlasManager.CollectTextures(items, shapes);
			if (game.disposed)
			{
				return;
			}
			game.ItemAtlasManager.CreateNewAtlas("items");
			if (game.disposed)
			{
				return;
			}
			game.ItemAtlasManager.PopulateTextureAtlassesFromTextures();
			if (!game.disposed)
			{
				game.EnqueueGameLaunchTask(delegate
				{
					FinaliseTextureAtlas(game.ItemAtlasManager, "item", BeginItemTesselation);
				}, "ServerAssetsReceived");
			}
		}
		catch (ThreadAbortException)
		{
		}
		catch (Exception e)
		{
			game.Logger.Fatal("Caught unhandled exception in ItemTextureCollection thread. Exiting game.");
			game.Logger.Fatal(e);
			game.Platform.XPlatInterface.ShowMessageBox("Client Thread Crash", "Whoops, a client game thread crashed, please check the client-main.log for more Information. I will now exit the game (and stop the server if in singleplayer). Sorry about that :(");
			game.KillNextFrame = true;
		}
	}

	private int PopulateBlocks(Block[] blocks, int start, int maxCount)
	{
		int quantityBlocks = 0;
		Packet_BlockType[] packetisedBlocks = pkt_srvrassets.Blocks;
		for (int i = start; i < packetisedBlocks.Length && i < maxCount; i++)
		{
			Packet_BlockType pt = packetisedBlocks[i];
			if (pt.Code != null)
			{
				blocks[pt.BlockId] = BlockTypeNet.ReadBlockTypePacket(pt, game, ClientMain.ClassRegistry);
				quantityBlocks++;
			}
		}
		return quantityBlocks;
	}

	private int PopulateItems(List<Item> items, int listSize)
	{
		int quantityItems = 0;
		Item noitem = new Item(0);
		for (int j = 0; j < listSize; j++)
		{
			items.Add(noitem);
		}
		Packet_ItemType[] packetisedItems = pkt_srvrassets.Items;
		int maxCount = pkt_srvrassets.ItemsCount;
		for (int i = 0; i < packetisedItems.Length && i < maxCount; i++)
		{
			Packet_ItemType pt = packetisedItems[i];
			if (pt.Code != null)
			{
				items[pt.ItemId] = ItemTypeNet.ReadItemTypePacket(pt, game, ClientMain.ClassRegistry);
				quantityItems++;
			}
		}
		return quantityItems;
	}

	private void LoadEntityTypes()
	{
		for (int i = 0; i < pkt_srvrassets.EntitiesCount; i++)
		{
			Packet_EntityType packet = pkt_srvrassets.Entities[i];
			try
			{
				EntityProperties config = EntityTypeNet.FromPacket(packet, null);
				game.EntityClassesByCode[config.Code] = config;
			}
			catch (Exception e)
			{
				logger.Error("Loading error for entity " + packet.Code);
				logger.Error(e);
			}
		}
		logger.VerboseDebug("Populated " + pkt_srvrassets.EntitiesCount + " EntityTypes from server");
	}

	internal void LoadColorMapsAndCatalogSoundsIfSingleplayer()
	{
		int count = game.WorldMap.LoadColorMaps();
		logger.VerboseDebug("Loaded " + count + " ColorMap textures");
		game.DoneColorMaps = true;
		if (game.IsSingleplayer)
		{
			ScreenManager.CatalogSounds(StartSlowLoadingSoundsWhenReady);
		}
	}

	internal void ResolveBlockItemStacks()
	{
		game.LoadCollectibles(game.Items, game.Blocks);
		for (int j = 0; j < game.Items.Count; j++)
		{
			Item item = game.Items[j];
			if (item.Code == null)
			{
				continue;
			}
			game.ItemsByCode[item.Code] = item;
			if (item.CombustibleProps?.SmeltedStack?.ResolvedItemstack != null)
			{
				item.CombustibleProps.SmeltedStack.ResolvedItemstack.ResolveBlockOrItem(game);
			}
			if (item.NutritionProps?.EatenStack?.ResolvedItemstack != null)
			{
				item.NutritionProps.EatenStack.ResolvedItemstack.ResolveBlockOrItem(game);
			}
			if (item.TransitionableProps != null)
			{
				TransitionableProperties[] transitionableProps = item.TransitionableProps;
				for (int l = 0; l < transitionableProps.Length; l++)
				{
					transitionableProps[l].TransitionedStack?.ResolvedItemstack?.ResolveBlockOrItem(game);
				}
			}
			if (item.GrindingProps != null && item.GrindingProps.GroundStack?.ResolvedItemstack != null)
			{
				item.GrindingProps.GroundStack.ResolvedItemstack.ResolveBlockOrItem(game);
			}
			if (item.CrushingProps != null && item.CrushingProps.CrushedStack?.ResolvedItemstack != null)
			{
				item.CrushingProps.CrushedStack.ResolvedItemstack.ResolveBlockOrItem(game);
			}
		}
		logger.Notification("Received {0} item types from server", game.ItemsByCode.Count);
		int[] unknownTextureSubIds = new int[7];
		Cuboidf[] boxes = new Cuboidf[1] { Block.DefaultCollisionBox };
		int blockCount = 0;
		for (int i = 0; i < game.Blocks.Count; i++)
		{
			Block block = game.Blocks[i];
			if (block.Code == null)
			{
				game.FastBlockTextureSubidsByBlockAndFace[i] = unknownTextureSubIds;
				block.DrawType = EnumDrawType.Cube;
				block.SelectionBoxes = boxes;
				block.CollisionBoxes = boxes;
				continue;
			}
			blockCount++;
			game.BlocksByCode[block.Code] = block;
			game.FastBlockTextureSubidsByBlockAndFace[i] = new int[7];
			if (block.CombustibleProps?.SmeltedStack?.ResolvedItemstack != null)
			{
				block.CombustibleProps.SmeltedStack.ResolvedItemstack.ResolveBlockOrItem(game);
			}
			if (block.NutritionProps?.EatenStack?.ResolvedItemstack != null)
			{
				block.NutritionProps.EatenStack.ResolvedItemstack.ResolveBlockOrItem(game);
			}
			if (block.TransitionableProps != null)
			{
				TransitionableProperties[] transitionableProps = block.TransitionableProps;
				for (int l = 0; l < transitionableProps.Length; l++)
				{
					transitionableProps[l].TransitionedStack?.ResolvedItemstack?.ResolveBlockOrItem(game);
				}
			}
			if (block.GrindingProps?.GroundStack?.ResolvedItemstack != null)
			{
				block.GrindingProps.GroundStack.ResolvedItemstack.ResolveBlockOrItem(game);
			}
			if (block.CrushingProps?.CrushedStack?.ResolvedItemstack != null)
			{
				block.CrushingProps.CrushedStack.ResolvedItemstack.ResolveBlockOrItem(game);
			}
			if (block.Drops != null)
			{
				for (int k = 0; k < block.Drops.Length; k++)
				{
					block.Drops[k].ResolvedItemstack.ResolveBlockOrItem(game);
				}
			}
			if (block.SeasonColorMap != null)
			{
				game.ColorMaps.TryGetValue(block.SeasonColorMap, out block.SeasonColorMapResolved);
			}
			if (block.ClimateColorMap != null)
			{
				game.ColorMaps.TryGetValue(block.ClimateColorMap, out block.ClimateColorMapResolved);
			}
		}
		foreach (EntityProperties entityType in game.EntityTypes)
		{
			entityType.PopulateDrops(game.api.World);
		}
		logger.Notification("Loaded {0} block types from server", blockCount);
		logger.VerboseDebug("Resolved blocks and items stacks and drops");
	}

	internal void FinaliseTextureAtlas(TextureAtlasManager manager, string type, Action onCompleted)
	{
		logger.VerboseDebug("Server assets - composing " + type + " texture atlas on main thread");
		manager.ComposeTextureAtlasses_StageA();
		logger.VerboseDebug("Server assets - done " + type + " textures composition");
		if (ClientSettings.OffThreadMipMapCreation)
		{
			TyronThreadPool.QueueTask(delegate
			{
				FinaliseTextureAtlas_StageB(manager, "off-thread " + type);
			});
		}
		else
		{
			game.EnqueueGameLaunchTask(delegate
			{
				FinaliseTextureAtlas_StageB(manager, type);
			}, "ServerAssetsReceived");
		}
		TyronThreadPool.QueueTask(delegate
		{
			FinaliseTextureAtlas_StageC(manager, type, onCompleted);
		});
	}

	private void FinaliseTextureAtlas_StageB(TextureAtlasManager manager, string type)
	{
		manager.ComposeTextureAtlasses_StageB();
		logger.VerboseDebug("Server assets - done " + type + " textures mipmap creation");
	}

	private void FinaliseTextureAtlas_StageC(TextureAtlasManager manager, string type, Action onCompleted)
	{
		manager.ComposeTextureAtlasses_StageC();
		logger.VerboseDebug("Server assets - done " + type + " textures random and average color collection");
		onCompleted?.Invoke();
	}

	internal void BeginItemTesselation()
	{
		if (game.disposed)
		{
			return;
		}
		WaitFor(ref game.DoneBlockAndItemShapeLoading, 12000, "block shape loading", "loading block and item shapes, it did not complete");
		if (!game.disposed)
		{
			game.EnqueueGameLaunchTask(delegate
			{
				game.TesselatorManager.TesselateItems_Pre(game.Items);
			}, "ServerAssetsReceived");
			game.EnqueueGameLaunchTask(delegate
			{
				HandleServerAssets_Step7(0, 1);
			}, "ServerAssetsReceived");
		}
	}

	private void HandleServerAssets_Step7(int index, int frameCount)
	{
		index = game.TesselatorManager.TesselateItems(game.Items, index, game.Items.Count);
		if (index < game.Items.Count)
		{
			game.EnqueueGameLaunchTask(delegate
			{
				HandleServerAssets_Step7(index, ++frameCount);
			}, "ServerAssetsReceived");
		}
		else
		{
			logger.VerboseDebug("Server assets - done item tesselation spread over " + frameCount + " frames");
		}
	}

	internal void StartSlowLoadingMusicAndSounds()
	{
		game.MusicEngine.Initialise_SeparateThread();
		StartSlowLoadingSoundsWhenReady();
	}

	private void HandleServerAssets_Step9()
	{
		game.EnqueueGameLaunchTask(delegate
		{
			game.SuspendMainThreadTasks = false;
			game.AssetLoadingOffThread = false;
			game.WorldMap.BlockTexturesLoaded();
			game.TesselatorManager.TesselateBlocks_Pre();
			TyronThreadPool.QueueTask(delegate
			{
				game.TesselatorManager.TesselateBlocks_Async(game.Blocks);
			});
			TyronThreadPool.QueueTask(delegate
			{
				game.TesselatorManager.TesselateBlocksForInventory_ASync(game.Blocks);
			});
			logger.VerboseDebug("Server assets - done step 9");
			game.EnqueueGameLaunchTask(HandleServerAssets_Step10, "ServerAssetsReceived");
		}, "ServerAssetsReceived");
	}

	private void HandleServerAssets_Step10()
	{
		game.BlockAtlasManager.GenFramebuffer();
		logger.VerboseDebug("Server assets - done texture atlas frame buffer");
		game.EnqueueGameLaunchTask(HandleServerAssets_Step11, "ServerAssetsLoaded");
	}

	internal void HandleServerAssets_Step11()
	{
		for (int i = 0; i < pkt_srvrassets.RecipesCount; i++)
		{
			Packet_Recipes rp = pkt_srvrassets.Recipes[i];
			game.GetRecipeRegistry(rp.Code).FromBytes(game, rp.Quantity, rp.Data);
		}
		logger.Notification("Server assets loaded");
		pkt_srvrassets = null;
		FinishAssetLoadingIfUnfinished();
	}

	internal bool FinishAssetLoadingIfUnfinished()
	{
		if (Interlocked.Increment(ref assetsLoadedPrerequisites) == 2)
		{
			game.SuspendMainThreadTasks = true;
			logger.VerboseDebug("World configs received and block tesselation complete");
			game.EnqueueGameLaunchTask(AllAssetsLoadedAndSpawnChunkReceived, "onAllAssetsLoaded");
			assetsLoadedPrerequisites = 0;
			return false;
		}
		return true;
	}

	private void HandleServerReady(Packet_Server packet)
	{
		logger.VerboseDebug("Handling ServerReady packet");
		if (game.exitReason == null)
		{
			game.modHandler.StartModsFully();
			game.api.Shader.ReloadShaders();
			logger.Notification("Reloaded shaders now with mod assets");
			if (!game.IsSingleplayer)
			{
				ScreenManager.CatalogSounds(StartSlowLoadingSoundsWhenReady);
			}
			game.SendRequestJoin();
		}
	}

	private void HandleLevelInitialize(Packet_Server packet)
	{
		game.WorldMap.ServerChunkSize = packet.LevelInitialize.ServerChunkSize;
		game.WorldMap.MapChunkSize = packet.LevelInitialize.ServerMapChunkSize;
		game.WorldMap.regionSize = packet.LevelInitialize.ServerMapRegionSize;
		game.WorldMap.MaxViewDistance = packet.LevelInitialize.MaxViewDistance;
		if (game.WorldMap.ServerChunkSize == 0 || game.WorldMap.MapChunkSize == 0 || game.WorldMap.RegionSize == 0)
		{
			throw new Exception("Invalid server response, it sent wrong chunk/map/region sizes during LevelInitialize");
		}
		game.sendRuntimeSettings();
		logger.Notification("Received level init");
	}

	private void HandleLevelDataChunk(Packet_Server packet)
	{
		if (packet.LevelDataChunk.PercentComplete == 100 && FinishAssetLoadingIfUnfinished())
		{
			logger.VerboseDebug(game.IsSingleplayer ? "Received server configs and level data, but not yet completed block tesselation" : "Received 100% map level data");
			game.SuspendMainThreadTasks = true;
		}
	}

	internal void AllAssetsLoadedAndSpawnChunkReceived()
	{
		game.terrainIlluminator.OnBlockTexturesLoaded();
		game.EnqueueGameLaunchTask(delegate
		{
			OnAllAssetsLoaded_ClientSystems(0);
		}, "onAllAssetsLoaded");
	}

	internal void OnAllAssetsLoaded_ClientSystems(int i)
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();
		while (i < game.clientSystems.Length)
		{
			game.clientSystems[i].OnBlockTexturesLoaded();
			if (sw.ElapsedMilliseconds >= 60)
			{
				if (sw.ElapsedMilliseconds > 65)
				{
					logger.VerboseDebug("Slow to load clientSystem " + game.clientSystems[i].Name);
				}
				game.EnqueueGameLaunchTask(delegate
				{
					OnAllAssetsLoaded_ClientSystems(i + 1);
				}, "onAllAssetsLoaded");
				return;
			}
			i++;
		}
		logger.VerboseDebug("Done client systems OnLoaded");
		game.EnqueueGameLaunchTask(delegate
		{
			OnAllAssetsLoaded_Blocks(0);
		}, "onAllAssetsLoaded");
	}

	internal void OnAllAssetsLoaded_Blocks(int i)
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();
		while (i < game.Blocks.Count)
		{
			game.Blocks[i]?.OnLoadedNative(game.api);
			if (i > 0 && i % 4 == 0 && sw.ElapsedMilliseconds >= 60)
			{
				if (sw.ElapsedMilliseconds > 61)
				{
					logger.VerboseDebug(string.Concat("Slow to load blocks (>1ms) ", game.Blocks[i - 4].Code, ",", game.Blocks[i - 3].Code, ",", game.Blocks[i - 2].Code, ",", game.Blocks[i - 1].Code));
				}
				game.EnqueueGameLaunchTask(delegate
				{
					OnAllAssetsLoaded_Blocks(i + 1);
				}, "onAllAssetsLoaded");
				return;
			}
			i++;
		}
		logger.VerboseDebug("Done blocks OnLoaded");
		game.EnqueueGameLaunchTask(delegate
		{
			OnAllAssetsLoaded_Items(0);
		}, "onAllAssetsLoaded");
	}

	internal void OnAllAssetsLoaded_Items(int i)
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();
		while (i < game.Items.Count)
		{
			game.Items[i]?.OnLoadedNative(game.api);
			if (i > 0 && i % 4 == 0 && sw.ElapsedMilliseconds >= 60)
			{
				if (sw.ElapsedMilliseconds > 61)
				{
					logger.VerboseDebug(string.Concat("Slow to load items (>1ms) ", game.Items[i - 4].Code, ",", game.Items[i - 3].Code, ",", game.Items[i - 2].Code, ",", game.Items[i - 1].Code));
				}
				game.EnqueueGameLaunchTask(delegate
				{
					OnAllAssetsLoaded_Items(i + 1);
				}, "onAllAssetsLoaded");
				return;
			}
			i++;
		}
		logger.VerboseDebug("Done items OnLoaded");
		game.EnqueueGameLaunchTask(WaitForBlockTesselation, "blockTesselation");
	}

	private void WaitForBlockTesselation()
	{
		if (game.TesselatorManager.finishedAsyncBlockTesselation < 2)
		{
			game.EnqueueGameLaunchTask(WaitForBlockTesselation, "BlockTesselation");
		}
		else
		{
			game.EnqueueGameLaunchTask(OnAllAssetsLoadedAndClientJoined, "OnAllAssetsLoaded");
		}
	}

	internal void OnAllAssetsLoadedAndClientJoined()
	{
		game.SuspendMainThreadTasks = false;
		game.BlocksReceivedAndLoaded = true;
		CompositeTexture.basicTexturesCache = null;
		CompositeTexture.wildcardsCache = null;
		game.Platform.AssetManager.UnloadAssets(AssetCategory.textures);
		game.TesselatorManager.LoadDone();
		TyronThreadPool.QueueTask(Lang.InitialiseSearch);
		logger.VerboseDebug("All clientside asset loading complete, game launch can proceed");
	}

	private void HandleLevelFinalize(Packet_Server packet)
	{
		logger.Notification("Received level finalize");
		logger.VerboseDebug("Received level finalize");
		game.InWorldStopwatch.Start();
		ClientSystem[] clientSystems = game.clientSystems;
		for (int i = 0; i < clientSystems.Length; i++)
		{
			clientSystems[i].OnLevelFinalize();
		}
		logger.VerboseDebug("Done level finalize clientsystems");
		game.api.eventapi.TriggerLevelFinalize();
		if (ClientSettings.PauseGameOnLostFocus && !game.Platform.IsFocused)
		{
			game.eventManager?.AddDelayedCallback(delegate
			{
				if (!game.Platform.IsFocused)
				{
					if (game.IsSingleplayer && !game.OpenedToLan)
					{
						logger.Notification("Window not focused. Pausing game.");
						game.PauseGame(paused: true);
					}
					ScreenManager.hotkeyManager.HotKeys["escapemenudialog"].Handler(new KeyCombination());
				}
			}, 1000L);
		}
		logger.VerboseDebug("Done level finalize");
		game.AmbientManager.LateInit();
	}

	internal static bool ReceiveAssetsPacketDirectly(Packet_Server packet)
	{
		if (instance == null || instance.game == null)
		{
			return false;
		}
		instance.HandleServerAssets(packet);
		return true;
	}

	internal static bool ReceiveServerPacketDirectly(Packet_Server packet)
	{
		if (instance == null || instance.game == null)
		{
			return false;
		}
		ServerPacketHandler<Packet_Server> handler = instance.game.PacketHandlers[packet.Id];
		if (handler == null)
		{
			return false;
		}
		if (packet.Id == 73)
		{
			instance.game.ServerReady = true;
		}
		instance.game.EnqueueMainThreadTask(delegate
		{
			handler(packet);
		}, "readpacket" + packet.Id);
		return true;
	}

	internal void WaitFor(ref bool flag, int timeOut, string logMessage, string logError)
	{
		bool logged = false;
		while (!flag && timeOut-- > 0 && !game.disposed)
		{
			if (!logged)
			{
				logger.VerboseDebug("Waiting for " + logMessage);
				logged = true;
			}
			Thread.Sleep(10);
		}
		if (!game.disposed && (timeOut <= 0 || !flag))
		{
			logger.Fatal("The game probably cannot continue to launch.  There was a problem " + logError);
		}
		else if (logged && flag)
		{
			logger.VerboseDebug("Done " + logMessage);
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}

	public override void Dispose(ClientMain game)
	{
		int timeOut = 250;
		while (game.AssetLoadingOffThread && timeOut-- > 0)
		{
			Thread.Sleep(10);
		}
		instance = null;
	}
}
