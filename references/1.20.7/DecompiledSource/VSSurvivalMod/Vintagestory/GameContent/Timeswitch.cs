using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace Vintagestory.GameContent;

public class Timeswitch : ModSystem
{
	public const float CooldownTime = 3f;

	private const GlKeys TimeswitchHotkey = GlKeys.Y;

	private const int OtherDimension = 2;

	private const double SquareRootOf2 = 1.41421356;

	private ICoreServerAPI sapi;

	private IServerNetworkChannel serverChannel;

	private Dictionary<string, TimeSwitchState> timeswitchStatesByPlayerUid = new Dictionary<string, TimeSwitchState>();

	private bool dim2ChunksLoaded;

	private bool allowTimeswitch;

	private bool posEnabled;

	private int baseChunkX;

	private int baseChunkZ;

	private int size = 3;

	private int deactivateRadius = 2;

	private Vec3d centerpos = new Vec3d();

	private CollisionTester collTester;

	private ICoreClientAPI capi;

	private IClientNetworkChannel clientChannel;

	private StoryStructureLocation genStoryStructLoc;

	private GenStoryStructures genGenStoryStructures;

	private int storyTowerBaseY;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		allowTimeswitch = api.World.Config.GetBool("loreContent", defaultValue: true) || api.World.Config.GetBool("allowTimeswitch");
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		capi.Input.RegisterHotKey("timeswitch", Lang.Get("Time switch"), GlKeys.Y);
		capi.Input.SetHotKeyHandler("timeswitch", OnHotkeyTimeswitch);
		clientChannel = api.Network.RegisterChannel("timeswitch").RegisterMessageType(typeof(TimeSwitchState)).SetMessageHandler<TimeSwitchState>(OnStateReceived);
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		new CmdTimeswitch(api);
		sapi = api;
		if (allowTimeswitch)
		{
			api.Event.PlayerJoin += OnPlayerJoin;
			api.Event.SaveGameLoaded += OnSaveGameLoaded;
			api.Event.GameWorldSave += OnGameGettingSaved;
			serverChannel = api.Network.RegisterChannel("timeswitch").RegisterMessageType(typeof(TimeSwitchState));
			api.Event.RegisterGameTickListener(PlayerEntryCheck, 500);
			collTester = new CollisionTester();
		}
	}

	private void PlayerEntryCheck(float dt)
	{
		if (!posEnabled)
		{
			return;
		}
		ItemStack skillStack = new ItemStack(sapi.World.GetItem("timeswitch"));
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer player = (IServerPlayer)allOnlinePlayers[i];
			if (player.ConnectionState != EnumClientState.Playing)
			{
				continue;
			}
			ItemSlot skillSlot = player.InventoryManager.GetHotbarInventory()[10];
			if (WithinRange(player.Entity.ServerPos, deactivateRadius - 1))
			{
				if (!timeswitchStatesByPlayerUid.TryGetValue(player.PlayerUID, out var state))
				{
					state = new TimeSwitchState(player.PlayerUID);
					timeswitchStatesByPlayerUid[player.PlayerUID] = state;
				}
				if (skillSlot.Empty && player.Entity.Alive)
				{
					skillSlot.Itemstack = skillStack;
					skillSlot.MarkDirty();
				}
				if (!state.Enabled)
				{
					state.Enabled = true;
					OnStartCommand(player);
					player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.GetL(player.LanguageCode, "message-timeswitch-detected"), EnumChatType.Notification);
					player.SendMessage(GlobalConstants.GeneralChatGroup, Lang.GetL(player.LanguageCode, "message-timeswitch-controls"), EnumChatType.Notification);
				}
			}
			else if (!WithinRange(player.Entity.ServerPos, deactivateRadius))
			{
				if (!skillSlot.Empty)
				{
					skillSlot.Itemstack = null;
					skillSlot.MarkDirty();
				}
				if (player.Entity.ServerPos.Dimension == 2)
				{
					ActivateTimeswitchServer(player, raiseToWorldSurface: true, out var _);
				}
				if (!timeswitchStatesByPlayerUid.TryGetValue(player.PlayerUID, out var state2))
				{
					state2 = new TimeSwitchState(player.PlayerUID);
					timeswitchStatesByPlayerUid[player.PlayerUID] = state2;
				}
				state2.Enabled = false;
			}
		}
	}

	private bool OnHotkeyTimeswitch(KeyCombination comb)
	{
		if (ItemSkillTimeswitch.timeSwitchCooldown > 0f)
		{
			return true;
		}
		capi.SendChatMessage($"/timeswitch toggle");
		capi.World.AddCameraShake(0.25f);
		ItemSkillTimeswitch.timeSwitchCooldown = 3f;
		return true;
	}

	private void OnGameGettingSaved()
	{
		int[] positions = new int[3] { baseChunkX, baseChunkZ, size };
		sapi.WorldManager.SaveGame.StoreData("timeswitchPos", SerializerUtil.Serialize(positions));
	}

	private void OnSaveGameLoaded()
	{
		if (timeswitchStatesByPlayerUid == null)
		{
			timeswitchStatesByPlayerUid = new Dictionary<string, TimeSwitchState>();
		}
		try
		{
			byte[] data = sapi.WorldManager.SaveGame.GetData("timeswitchPos");
			if (data != null)
			{
				int[] positions = SerializerUtil.Deserialize<int[]>(data);
				if (positions.Length >= 3)
				{
					baseChunkX = positions[0];
					baseChunkZ = positions[1];
					size = positions[2];
					SetupCenterPos();
				}
			}
		}
		catch (Exception e)
		{
			sapi.World.Logger.Error("Failed loading timeswitchPos. Maybe not yet worldgenned, or else use /timeswitch setpos to set it manually.");
			sapi.World.Logger.Error(e);
		}
	}

	private void OnPlayerJoin(IServerPlayer byPlayer)
	{
		if (!timeswitchStatesByPlayerUid.TryGetValue(byPlayer.PlayerUID, out var _))
		{
			timeswitchStatesByPlayerUid[byPlayer.PlayerUID] = new TimeSwitchState(byPlayer.PlayerUID);
		}
		if (byPlayer.Entity.Pos.Dimension == 2)
		{
			OnStartCommand(byPlayer);
			int cx = (int)byPlayer.Entity.Pos.X / 32;
			int cy = (int)byPlayer.Entity.Pos.Y / 32;
			int cz = (int)byPlayer.Entity.Pos.Z / 32;
			sapi.WorldManager.SendChunk(cx, cy, cz, byPlayer, onlyIfInRange: false);
		}
	}

	public void OnStartCommand(IServerPlayer player)
	{
		if (allowTimeswitch && posEnabled)
		{
			LoadChunkColumns();
			if (player != null)
			{
				ForceSendChunkColumns(player);
			}
		}
	}

	private void ActivateTimeswitchClient(TimeSwitchState tsState)
	{
		EntityPlayer player = capi.World.Player.Entity;
		if (tsState.forcedY != 0)
		{
			player.SidedPos.Y = tsState.forcedY;
		}
		player.ChangeDimension(tsState.Activated ? 2 : 0);
	}

	private bool WithinRange(EntityPos pos, int radius)
	{
		return pos.HorDistanceTo(centerpos) < (double)radius;
	}

	public bool ActivateTimeswitchServer(IServerPlayer player, bool raiseToWorldSurface, out string failurereason)
	{
		bool result = ActivateTimeswitchInternal(player, raiseToWorldSurface, out failurereason);
		if (!result && failurereason != null)
		{
			TimeSwitchState tempState = new TimeSwitchState();
			tempState.failureReason = failurereason;
			serverChannel.SendPacket(tempState, player);
		}
		return result;
	}

	private bool ActivateTimeswitchInternal(IServerPlayer byPlayer, bool forced, out string failurereason)
	{
		failurereason = null;
		if (!allowTimeswitch || !posEnabled)
		{
			return false;
		}
		if (byPlayer.Entity.MountedOn != null)
		{
			failurereason = "mounted";
			return false;
		}
		if (byPlayer.Entity.ServerPos.Dimension == 0)
		{
			if (!timeswitchStatesByPlayerUid.TryGetValue(byPlayer.PlayerUID, out var state))
			{
				state = new TimeSwitchState(byPlayer.PlayerUID);
				timeswitchStatesByPlayerUid[byPlayer.PlayerUID] = state;
			}
			if (!state.Enabled)
			{
				return false;
			}
			if (!WithinRange(byPlayer.Entity.ServerPos, deactivateRadius))
			{
				failurereason = "outofrange";
				return false;
			}
			if (!dim2ChunksLoaded)
			{
				failurereason = "wait";
				return false;
			}
			if (genStoryStructLoc != null && !genStoryStructLoc.DidGenerateAdditional)
			{
				failurereason = "wait";
				return false;
			}
		}
		bool forceYToWorldSurface = forced;
		if (genStoryStructLoc != null)
		{
			double distanceFromTowerX = Math.Max(0.0, Math.Abs(byPlayer.Entity.ServerPos.X - (double)genStoryStructLoc.CenterPos.X - 0.5) - 9.5);
			double distanceFromTowerZ = Math.Max(0.0, Math.Abs(byPlayer.Entity.ServerPos.Z - (double)genStoryStructLoc.CenterPos.Z - 0.5) - 9.5);
			int towerBlocksConeHeightY = storyTowerBaseY + (int)Math.Max(distanceFromTowerX, distanceFromTowerZ) * 3;
			forced |= byPlayer.Entity.ServerPos.Y <= (double)towerBlocksConeHeightY && !byPlayer.Entity.Controls.IsFlying && !byPlayer.Entity.Controls.Gliding && byPlayer.Entity.ServerPos.Motion.Y > -0.19;
		}
		bool farFromTimeswitch = !WithinRange(byPlayer.Entity.ServerPos, deactivateRadius + 64);
		int targetDimension = ((byPlayer.Entity.Pos.Dimension == 0) ? 2 : 0);
		if (timeswitchStatesByPlayerUid.TryGetValue(byPlayer.PlayerUID, out var tsState))
		{
			tsState.forcedY = 0;
			if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival && !farFromTimeswitch)
			{
				if (forceYToWorldSurface && (byPlayer.Entity.OnGround || OtherDimensionPositionWouldCollide(byPlayer.Entity, targetDimension, allowTolerance: false)))
				{
					RaisePlayerToTerrainSurface(byPlayer.Entity, targetDimension, tsState);
				}
				else if (OtherDimensionPositionWouldCollide(byPlayer.Entity, targetDimension, allowTolerance: true))
				{
					failurereason = "blocked";
					return false;
				}
			}
			tsState.Activated = targetDimension == 2;
			byPlayer.Entity.ChangeDimension(targetDimension);
			tsState.baseChunkX = baseChunkX;
			tsState.baseChunkZ = baseChunkZ;
			tsState.size = size;
			serverChannel.BroadcastPacket(tsState);
			spawnTeleportParticles(byPlayer.Entity.ServerPos);
			return true;
		}
		return false;
	}

	private void spawnTeleportParticles(EntityPos pos)
	{
		int r = 53;
		int g = 221;
		int b = 172;
		SimpleParticleProperties teleportParticles = new SimpleParticleProperties(150f, 200f, (r << 16) | (g << 8) | b | 0x64000000, new Vec3d(pos.X - 0.5, pos.Y, pos.Z - 0.5), new Vec3d(pos.X + 0.5, pos.Y + 1.8, pos.Z + 0.5), new Vec3f(-0.7f, -0.7f, -0.7f), new Vec3f(1.4f, 1.4f, 1.4f), 2f, 0f, 0.1f, 0.2f, EnumParticleModel.Quad);
		teleportParticles.addLifeLength = 1f;
		teleportParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -10f);
		int dim = pos.Dimension;
		sapi.World.SpawnParticles(teleportParticles);
		sapi.World.PlaySoundAt(new AssetLocation("sounds/effect/timeswitch"), pos.X, pos.Y, pos.Z, null, randomizePitch: false, 16f);
		teleportParticles.MinPos.Y += dim * 32768;
		sapi.World.SpawnParticles(teleportParticles);
		sapi.World.PlaySoundAt(new AssetLocation("sounds/effect/timeswitch"), pos.X, pos.Y + (double)(dim * 32768), pos.Z, null, randomizePitch: false, 16f);
	}

	private void OnStateReceived(TimeSwitchState state)
	{
		if (capi.World?.Player != null)
		{
			if (state.failureReason.Length > 0)
			{
				capi.TriggerIngameError(capi.World, state.failureReason, Lang.Get("ingameerror-timeswitch-" + state.failureReason));
			}
			else if (state.playerUID == capi.World.Player.PlayerUID)
			{
				ActivateTimeswitchClient(state);
				MakeChunkColumnsVisibleClient(state.baseChunkX, state.baseChunkZ, state.size, state.Activated ? 2 : 0);
			}
			else
			{
				capi.World.PlayerByUid(state.playerUID)?.Entity?.ChangeDimension(state.Activated ? 2 : 0);
			}
		}
	}

	public void SetPos(BlockPos pos)
	{
		baseChunkX = pos.X / 32;
		baseChunkZ = pos.Z / 32;
		SetupCenterPos();
	}

	private void SetupCenterPos()
	{
		centerpos.Set(baseChunkX * 32 + 16, 0.0, baseChunkZ * 32 + 16);
		deactivateRadius = (size - 1) * 32 + 1;
		posEnabled = true;
	}

	public void CopyBlocksToAltDimension(IBlockAccessor sourceblockAccess, IServerPlayer player)
	{
		if (allowTimeswitch && posEnabled)
		{
			BlockPos start = new BlockPos((baseChunkX - size + 1) * 32, 0, (baseChunkZ - size + 1) * 32);
			BlockPos end = start.AddCopy(32 * (size * 2 - 1), 0, 32 * (size * 2 - 1));
			start.Y = Math.Max(0, (sapi.World.SeaLevel - 8) / 32 * 32);
			end.Y = sapi.WorldManager.MapSizeY;
			BlockSchematic blockSchematic = new BlockSchematic(sapi.World, sourceblockAccess, start, end, notLiquids: false);
			CreateChunkColumns();
			BlockPos originPos = start.AddCopy(0, 65536, 0);
			IBlockAccessor blockAccess = sapi.World.BlockAccessor;
			blockSchematic.Init(blockAccess);
			blockSchematic.Place(blockAccess, sapi.World, originPos, EnumReplaceMode.ReplaceAll);
			blockSchematic.PlaceDecors(blockAccess, originPos);
			if (player != null)
			{
				start.dimension = 2;
				end.dimension = 2;
				sapi.WorldManager.FullRelight(start, end, sendToClients: false);
				ForceSendChunkColumns(player);
			}
		}
	}

	public void RelightCommand(IBlockAccessor sourceblockAccess, IServerPlayer player)
	{
		RelightAltDimension();
		ForceSendChunkColumns(player);
	}

	private void CreateChunkColumns()
	{
		for (int x = 0; x <= size * 2; x++)
		{
			for (int z = 0; z <= size * 2; z++)
			{
				int cx = baseChunkX - size + x;
				int cz = baseChunkZ - size + z;
				sapi.WorldManager.CreateChunkColumnForDimension(cx, cz, 2);
			}
		}
	}

	public void LoadChunkColumns()
	{
		if (!allowTimeswitch || !posEnabled || dim2ChunksLoaded)
		{
			return;
		}
		for (int x = 0; x < size * 2 - 1; x++)
		{
			for (int z = 0; z < size * 2 - 1; z++)
			{
				int cx = baseChunkX - size + 1 + x;
				int cz = baseChunkZ - size + 1 + z;
				sapi.WorldManager.LoadChunkColumnForDimension(cx, cz, 2);
			}
		}
		dim2ChunksLoaded = true;
	}

	private void ForceSendChunkColumns(IServerPlayer player)
	{
		if (!allowTimeswitch || !posEnabled)
		{
			return;
		}
		int maxSize = size * 2;
		int czBase = baseChunkZ - size;
		for (int x = 0; x <= maxSize; x++)
		{
			int cx = baseChunkX - size + x;
			for (int z = 0; z <= maxSize; z++)
			{
				sapi.WorldManager.ForceSendChunkColumn(player, cx, czBase + z, 2);
			}
		}
	}

	private void MakeChunkColumnsVisibleClient(int baseChunkX, int baseChunkZ, int size, int dimension)
	{
		for (int x = 0; x <= size * 2; x++)
		{
			for (int z = 0; z <= size * 2; z++)
			{
				int cx = baseChunkX - size + x;
				int cz = baseChunkZ - size + z;
				capi.World.SetChunkColumnVisible(cx, cz, dimension);
			}
		}
	}

	public int SetupDim2TowerGeneration(StoryStructureLocation structureLocation, GenStoryStructures genStoryStructures)
	{
		genStoryStructLoc = structureLocation;
		genGenStoryStructures = genStoryStructures;
		storyTowerBaseY = structureLocation.CenterPos.Y + 10;
		sapi.Logger.VerboseDebug("Setup dim2 " + baseChunkX * 32 + ", " + baseChunkZ * 32);
		return size;
	}

	public void AttemptGeneration(IWorldGenBlockAccessor worldgenBlockAccessor)
	{
		if (genStoryStructLoc == null || genStoryStructLoc.DidGenerateAdditional || !AreAllDim0ChunksGenerated(worldgenBlockAccessor))
		{
			return;
		}
		sapi.Logger.VerboseDebug("Timeswitch dim 2 generation: finished stage 1");
		BlockPos startPos = genStoryStructLoc.Location.Start.AsBlockPos;
		startPos.dimension = 2;
		PlaceSchematic(sapi.World.BlockAccessor, "story/" + genStoryStructLoc.Code + "-past", startPos);
		sapi.Logger.VerboseDebug("Timeswitch dim 2 generation: finished stage 2");
		RelightAltDimension();
		AddClaimForDim(2);
		genStoryStructLoc.DidGenerateAdditional = true;
		genGenStoryStructures.StoryStructureInstancesDirty = true;
		sapi.Logger.VerboseDebug("Timeswitch dim 2 generation: finished stage 3");
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer player = (IServerPlayer)allOnlinePlayers[i];
			if (player.ConnectionState == EnumClientState.Playing && WithinRange(player.Entity.ServerPos, deactivateRadius + 2))
			{
				ForceSendChunkColumns(player);
			}
		}
		sapi.Logger.VerboseDebug("Timeswitch dim 2 generation: finished stage 4");
	}

	private void RelightAltDimension()
	{
		if (size > 0)
		{
			BlockPos start = new BlockPos((baseChunkX - size) * 32, 0, (baseChunkZ - size) * 32, 2);
			BlockPos end = start.AddCopy(32 * (size * 2 + 1) - 1, sapi.WorldManager.MapSizeY - 1, 32 * (size * 2 + 1) - 1);
			start.Y = (sapi.World.SeaLevel - 8) / 32 * 32;
			sapi.WorldManager.FullRelight(start, end, sendToClients: false);
		}
	}

	private void AddClaimForDim(int dim)
	{
		int centerX = baseChunkX * 32 + 16;
		int centerZ = baseChunkZ * 32 + 16;
		int radius = size * 32 + 16;
		int dimY = dim * 32768;
		Cuboidi struclocDeva = new Cuboidi(centerX - radius, dimY, centerZ - radius, centerX + radius, dimY + sapi.WorldManager.MapSizeY, centerZ + radius);
		LandClaim[] claims = sapi.World.Claims.Get(struclocDeva.Center.AsBlockPos);
		if (claims == null || claims.Length == 0)
		{
			sapi.World.Claims.Add(new LandClaim
			{
				Areas = new List<Cuboidi> { struclocDeva },
				Description = "Past Dimension",
				ProtectionLevel = 10,
				LastKnownOwnerName = "custommessage-thepast",
				AllowUseEveryone = true
			});
		}
	}

	private void PlaceSchematic(IBlockAccessor blockAccessor, string genSchematicName, BlockPos start)
	{
		BlockSchematicPartial blocks = LoadSchematic(sapi, genSchematicName);
		if (blocks != null)
		{
			blocks.Init(blockAccessor);
			blocks.blockLayerConfig = genGenStoryStructures.blockLayerConfig;
			blocks.Place(blockAccessor, sapi.World, start, EnumReplaceMode.ReplaceAllNoAir);
			blocks.PlaceDecors(blockAccessor, start);
		}
	}

	private bool AreAllDim0ChunksGenerated(IBlockAccessor wgenBlockAccessor)
	{
		IBlockAccessor blockAccess = sapi.World.BlockAccessor;
		for (int cx = baseChunkX - size + 1; cx < baseChunkX + size; cx++)
		{
			for (int cz = baseChunkZ - size + 1; cz < baseChunkZ + size; cz++)
			{
				IMapChunk mc = blockAccess.GetMapChunk(cx, cz);
				if (mc == null)
				{
					return false;
				}
				if (mc.CurrentPass <= EnumWorldGenPass.Vegetation)
				{
					return false;
				}
			}
		}
		return true;
	}

	private BlockSchematicPartial LoadSchematic(ICoreServerAPI sapi, string schematicName)
	{
		IAsset asset = sapi.Assets.Get(new AssetLocation("worldgen/schematics/" + schematicName + ".json"));
		if (asset == null)
		{
			return null;
		}
		BlockSchematicPartial schematic = asset.ToObject<BlockSchematicPartial>();
		if (schematic == null)
		{
			sapi.World.Logger.Warning("Could not load timeswitching schematic {0}", schematicName);
			return null;
		}
		schematic.FromFileName = asset.Name;
		return schematic;
	}

	private bool OtherDimensionPositionWouldCollide(EntityPlayer entity, int otherDim, bool allowTolerance)
	{
		Vec3d tmpVec = entity.ServerPos.XYZ;
		tmpVec.Y = entity.ServerPos.Y + (double)(otherDim * 32768);
		Cuboidf reducedBox = entity.CollisionBox.Clone();
		if (allowTolerance)
		{
			reducedBox.OmniNotDownGrowBy(-0.0625f);
		}
		return collTester.IsColliding(sapi.World.BlockAccessor, reducedBox, tmpVec, alsoCheckTouch: false);
	}

	private void RaisePlayerToTerrainSurface(EntityPlayer entity, int targetDimension, TimeSwitchState tss)
	{
		double px = entity.ServerPos.X;
		double py = entity.ServerPos.Y;
		double pz = entity.ServerPos.Z;
		Cuboidd cuboidd = entity.CollisionBox.ToDouble().Translate(px, py, pz);
		int minX = (int)cuboidd.X1;
		int minZ = (int)cuboidd.Z1;
		int maxX = (int)cuboidd.X2;
		int maxZ = (int)cuboidd.Z2;
		int terrainY = 0;
		BlockPos bp = new BlockPos(targetDimension);
		for (int x = minX; x <= maxX; x++)
		{
			for (int z = minZ; z <= maxZ; z++)
			{
				bp.Set(x, terrainY, z);
				int y;
				if (targetDimension == 0)
				{
					y = entity.World.BlockAccessor.GetRainMapHeightAt(bp);
					if (y > storyTowerBaseY)
					{
						y = getWorldSurfaceHeight(entity.World.BlockAccessor, bp);
					}
				}
				else
				{
					y = getWorldSurfaceHeight(entity.World.BlockAccessor, bp);
				}
				if (y > terrainY)
				{
					terrainY = y;
				}
			}
		}
		if (terrainY > 0)
		{
			tss.forcedY = terrainY + 1;
		}
	}

	private int getWorldSurfaceHeight(IBlockAccessor blockAccessor, BlockPos bp)
	{
		while (bp.Y < blockAccessor.MapSizeY)
		{
			if (!blockAccessor.GetBlock(bp, 1).SideIsSolid(bp, BlockFacing.UP.Index))
			{
				return bp.Y - 1;
			}
			bp.Up();
		}
		return 0;
	}
}
