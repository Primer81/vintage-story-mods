using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Vintagestory.ServerMods.WorldEdit;

public class WorldEdit : ModSystem
{
	public ICoreServerAPI sapi;

	public WorldEditClientHandler clientHandler;

	private IServerNetworkChannel serverChannel;

	private Dictionary<string, WorldEditWorkspace> workspaces = new Dictionary<string, WorldEditWorkspace>();

	public static string ExportFolderPath;

	private WorldEditWorkspace workspace;

	public static bool ReplaceMetaBlocks { get; set; }

	public override void StartPre(ICoreAPI api)
	{
		ToolRegistry.RegisterDefaultTools();
	}

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("worldedit").RegisterMessageType(typeof(RequestWorkSpacePacket)).RegisterMessageType(typeof(WorldEditWorkspace))
			.RegisterMessageType(typeof(ChangePlayerModePacket))
			.RegisterMessageType(typeof(CopyToClipboardPacket))
			.RegisterMessageType(typeof(SchematicJsonPacket))
			.RegisterMessageType(typeof(WorldInteractPacket))
			.RegisterMessageType(typeof(PreviewBlocksPacket));
	}

	public override void StartClientSide(ICoreClientAPI capi)
	{
		clientHandler = new WorldEditClientHandler(capi);
	}

	public override void StartServerSide(ICoreServerAPI sapi)
	{
		this.sapi = sapi;
		ExportFolderPath = sapi.GetOrCreateDataPath("WorldEdit");
		sapi.Permissions.RegisterPrivilege("worldedit", "Ability to use world edit tools");
		RegisterCommands();
		sapi.Event.PlayerNowPlaying += Event_PlayerNowPlaying;
		sapi.Event.PlayerSwitchGameMode += OnSwitchedGameMode;
		sapi.Event.BreakBlock += OnBreakBlock;
		sapi.Event.DidPlaceBlock += OnDidBuildBlock;
		sapi.Event.SaveGameLoaded += OnLoad;
		sapi.Event.GameWorldSave += OnSave;
		serverChannel = sapi.Network.GetChannel("worldedit").SetMessageHandler<RequestWorkSpacePacket>(OnRequestWorkSpaceMessage).SetMessageHandler<ChangePlayerModePacket>(OnChangePlayerModeMessage)
			.SetMessageHandler<SchematicJsonPacket>(OnReceivedSchematic)
			.SetMessageHandler<WorldInteractPacket>(OnWorldInteract);
		IChatCommandApi chatCommands = sapi.ChatCommands;
		CommandArgumentParsers parsers = sapi.ChatCommands.Parsers;
		chatCommands.GetOrCreate("land").RequiresPrivilege(Privilege.chat).RequiresPlayer()
			.BeginSub("claim")
			.BeginSub("download")
			.WithDesc("Download a claim of yours to your computer")
			.WithArgs(parsers.Int("claim id"))
			.HandleWith(downloadClaim)
			.EndSub()
			.BeginSub("export")
			.RequiresPrivilege("exportclaims")
			.WithDesc("Export a claim of yours to a file on the game server")
			.WithArgs(parsers.Int("claim id"))
			.HandleWith(exportClaim)
			.EndSub()
			.EndSub();
	}

	private TextCommandResult downloadClaim(TextCommandCallingArgs args)
	{
		IServerPlayer plr = args.Caller.Player as IServerPlayer;
		LandClaim[] ownclaims = sapi.WorldManager.SaveGame.LandClaims.Where((LandClaim claim) => claim.OwnedByPlayerUid == plr.PlayerUID).ToArray();
		int claimid = (int)args[0];
		if (claimid < 0 || claimid >= ownclaims.Length)
		{
			return TextCommandResult.Error(Lang.Get("Incorrect claimid, you only have {0} claims", ownclaims.Length));
		}
		LandClaim claim2 = ownclaims[claimid];
		IServerWorldAccessor world = sapi.World;
		BlockSchematic blockdata = new BlockSchematic();
		BlockPos minPos = null;
		foreach (Cuboidi area in claim2.Areas)
		{
			blockdata.AddArea(world, area.Start.ToBlockPos(), area.End.ToBlockPos());
			if (minPos == null)
			{
				minPos = area.Start.ToBlockPos();
			}
			minPos.X = Math.Min(area.Start.X, minPos.X);
			minPos.Y = Math.Min(area.Start.Y, minPos.Y);
			minPos.Z = Math.Min(area.Start.Z, minPos.Z);
		}
		blockdata.Pack(world, minPos);
		serverChannel.SendPacket(new SchematicJsonPacket
		{
			Filename = "claim-" + GamePaths.ReplaceInvalidChars(claim2.Description),
			JsonCode = blockdata.ToJson()
		}, plr);
		return TextCommandResult.Success(Lang.Get("Ok, claim sent"));
	}

	private TextCommandResult exportClaim(TextCommandCallingArgs args)
	{
		IServerPlayer plr = args.Caller.Player as IServerPlayer;
		LandClaim[] ownclaims = sapi.WorldManager.SaveGame.LandClaims.Where((LandClaim claim) => claim.OwnedByPlayerUid == plr.PlayerUID).ToArray();
		int claimid = (int)args[0];
		if (claimid < 0 || claimid >= ownclaims.Length)
		{
			return TextCommandResult.Error(Lang.Get("Incorrect claimid, you only have {0} claims", ownclaims.Length));
		}
		LandClaim obj = ownclaims[claimid];
		IServerWorldAccessor world = sapi.World;
		BlockSchematic blockdata = new BlockSchematic();
		BlockPos minPos = null;
		foreach (Cuboidi area in obj.Areas)
		{
			blockdata.AddArea(world, area.Start.ToBlockPos(), area.End.ToBlockPos());
			if (minPos == null)
			{
				minPos = area.Start.ToBlockPos();
			}
			minPos.X = Math.Min(area.Start.X, minPos.X);
			minPos.Y = Math.Min(area.Start.Y, minPos.Y);
			minPos.Z = Math.Min(area.Start.Z, minPos.Z);
		}
		blockdata.Pack(world, minPos);
		string filename = "claim-" + claimid + "-" + GamePaths.ReplaceInvalidChars(plr.PlayerName) + ".json";
		blockdata.Save(Path.Combine(ExportFolderPath, filename));
		return TextCommandResult.Success(Lang.Get("Ok, claim saved to file " + filename));
	}

	private void OnWorldInteract(IServerPlayer fromPlayer, WorldInteractPacket packet)
	{
		BlockSelection blockSel = new BlockSelection
		{
			SelectionBoxIndex = packet.SelectionBoxIndex,
			DidOffset = packet.DidOffset,
			Face = BlockFacing.ALLFACES[packet.Face],
			Position = packet.Position,
			HitPosition = packet.HitPosition
		};
		if (packet.Mode == 1)
		{
			OnDidBuildBlock(fromPlayer, -1, blockSel, fromPlayer.InventoryManager.ActiveHotbarSlot.Itemstack);
			return;
		}
		EnumHandling handle = EnumHandling.PassThrough;
		float sdf = 1f;
		OnBreakBlock(fromPlayer, blockSel, ref sdf, ref handle);
	}

	private void OnReceivedSchematic(IServerPlayer fromPlayer, SchematicJsonPacket networkMessage)
	{
		WorldEditWorkspace workspace = GetOrCreateWorkSpace(fromPlayer);
		if (workspace.ToolsEnabled && workspace.ToolInstance is ImportTool)
		{
			ImportTool impTool = workspace.ToolInstance as ImportTool;
			string error = null;
			BlockSchematic schematic = BlockSchematic.LoadFromString(networkMessage.JsonCode, ref error);
			if (error == null)
			{
				impTool.SetBlockDatas(this, schematic);
				BlockPos pos = fromPlayer.CurrentBlockSelection?.Position?.UpCopy() ?? fromPlayer.Entity.Pos.AsBlockPos;
				BlockPos origin = schematic.GetStartPos(pos, impTool.Origin);
				workspace.CreatePreview(schematic, origin);
				workspace.PreviewPos = origin;
				fromPlayer.SendMessage(GlobalConstants.CurrentChatGroup, Lang.Get("Ok, schematic loaded into clipboard."), EnumChatType.CommandSuccess);
			}
			else
			{
				fromPlayer.SendMessage(GlobalConstants.CurrentChatGroup, Lang.Get("Error loading schematic: {0}", error), EnumChatType.CommandError);
			}
		}
	}

	private void OnChangePlayerModeMessage(IPlayer fromPlayer, ChangePlayerModePacket plrmode)
	{
		IServerPlayer obj = fromPlayer as IServerPlayer;
		bool freeMoveAllowed = obj.HasPrivilege(Privilege.freemove);
		bool pickRangeAllowed = obj.HasPrivilege(Privilege.pickingrange);
		if (plrmode.axisLock.HasValue)
		{
			fromPlayer.WorldData.FreeMovePlaneLock = plrmode.axisLock.Value;
		}
		if (plrmode.pickingRange.HasValue && pickRangeAllowed)
		{
			fromPlayer.WorldData.PickingRange = plrmode.pickingRange.Value;
		}
		if (plrmode.fly.HasValue)
		{
			fromPlayer.WorldData.FreeMove = plrmode.fly.Value && freeMoveAllowed;
		}
		if (plrmode.noclip.HasValue)
		{
			fromPlayer.WorldData.NoClip = plrmode.noclip.Value && freeMoveAllowed;
		}
	}

	private void OnRequestWorkSpaceMessage(IPlayer fromPlayer, RequestWorkSpacePacket networkMessage)
	{
		SendPlayerWorkSpace(fromPlayer.PlayerUID);
	}

	public WorldEditWorkspace GetWorkSpace(string playerUid)
	{
		workspaces.TryGetValue(playerUid, out var space);
		return space;
	}

	public void SendPlayerWorkSpace(string playerUID)
	{
		serverChannel.SendPacket(workspaces[playerUID], (IServerPlayer)sapi.World.PlayerByUid(playerUID));
	}

	public void RegisterTool(string toolname, Type tool)
	{
		ToolRegistry.RegisterToolType(toolname, tool);
	}

	private void OnSwitchedGameMode(IServerPlayer player)
	{
		if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			WorldEditWorkspace orCreateWorkSpace = GetOrCreateWorkSpace(player);
			orCreateWorkSpace.ToolsEnabled = false;
			orCreateWorkSpace.StartMarker = null;
			orCreateWorkSpace.EndMarker = null;
			orCreateWorkSpace.ResendBlockHighlights();
		}
	}

	private void Event_PlayerNowPlaying(IServerPlayer player)
	{
		WorldEditWorkspace workspace = GetOrCreateWorkSpace(player);
		foreach (KeyValuePair<string, Type> toolType in ToolRegistry.ToolTypes)
		{
			ToolRegistry.InstanceFromType(toolType.Key, workspace, workspace.revertableBlockAccess);
		}
		if (workspace.ToolsEnabled && workspace.ToolInstance != null)
		{
			workspace.ToolInstance?.Load(sapi);
			workspace.ResendBlockHighlights();
			SendPlayerWorkSpace(player.PlayerUID);
		}
		else if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			SendPlayerWorkSpace(player.PlayerUID);
		}
	}

	private void RevertableBlockAccess_BeforeCommit(IBulkBlockAccessor ba, WorldEditWorkspace workspace)
	{
		if (workspace.WorldEditConstraint == EnumWorldEditConstraint.Selection && workspace.StartMarker != null && workspace.EndMarker != null)
		{
			constrainEditsToSelection(ba, workspace);
		}
	}

	private void constrainEditsToSelection(IBulkBlockAccessor ba, WorldEditWorkspace workspace)
	{
		Cuboidi selection = new Cuboidi(workspace.StartMarker, workspace.EndMarker);
		foreach (BlockPos pos2 in ba.StagedBlocks.Keys.ToList())
		{
			if (!selection.Contains(pos2))
			{
				ba.StagedBlocks.Remove(pos2);
			}
		}
		KeyValuePair<BlockPos, BlockUpdate>[] array = ba.StagedBlocks.Where((KeyValuePair<BlockPos, BlockUpdate> b) => b.Value.Decors != null).ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<BlockPos, BlockUpdate> pos = array[i];
			if (!selection.Contains(new Vec3i(pos.Key.X, pos.Key.Y, pos.Key.Z)))
			{
				ba.StagedBlocks[pos.Key].Decors = null;
			}
		}
	}

	private void OnSave()
	{
		using MemoryStream ms = new MemoryStream();
		BinaryWriter writer = new BinaryWriter(ms);
		writer.Write(workspaces.Count);
		foreach (WorldEditWorkspace value in workspaces.Values)
		{
			value.ToBytes(writer);
		}
		sapi.WorldManager.SaveGame.StoreData("worldeditworkspaces", ms.ToArray());
	}

	private void OnLoad()
	{
		byte[] data = sapi.WorldManager.SaveGame.GetData("worldeditworkspaces");
		if (data == null || data.Length == 0)
		{
			return;
		}
		try
		{
			using MemoryStream ms = new MemoryStream(data);
			BinaryReader reader = new BinaryReader(ms);
			int count = reader.ReadInt32();
			while (count-- > 0)
			{
				IBlockAccessorRevertable revertableBlockAccess = sapi.World.GetBlockAccessorRevertable(synchronize: true, relight: true);
				WorldEditWorkspace workspace = new WorldEditWorkspace(sapi.World, revertableBlockAccess);
				revertableBlockAccess.BeforeCommit += delegate(IBulkBlockAccessor ba)
				{
					RevertableBlockAccess_BeforeCommit(ba, workspace);
				};
				workspace.FromBytes(reader);
				workspace.Init(sapi);
				if (workspace.PlayerUID != null)
				{
					workspaces[workspace.PlayerUID] = workspace;
				}
			}
		}
		catch (Exception)
		{
			sapi.Server.LogEvent("Exception thrown when trying to load worldedit workspaces. Will ignore.");
		}
	}

	public static bool CanUseWorldEdit(IServerPlayer player, bool showError = false)
	{
		if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			if (showError)
			{
				player.SendMessage(GlobalConstants.GeneralChatGroup, "Only available in creative mode.", EnumChatType.CommandError);
			}
			return false;
		}
		if (!player.HasPrivilege("worldedit"))
		{
			player.SendMessage(GlobalConstants.GeneralChatGroup, "No privilege to use", EnumChatType.CommandError);
			return false;
		}
		return true;
	}

	private WorldEditWorkspace GetOrCreateWorkSpace(IPlayer player)
	{
		string playeruid = player.PlayerUID;
		if (workspaces.ContainsKey(playeruid))
		{
			return workspaces[playeruid];
		}
		IBlockAccessorRevertable revertableBlockAccess = sapi.World.GetBlockAccessorRevertable(synchronize: true, relight: true);
		WorldEditWorkspace worldEditWorkspace2 = (workspaces[playeruid] = new WorldEditWorkspace(sapi.World, revertableBlockAccess));
		WorldEditWorkspace workspace = worldEditWorkspace2;
		revertableBlockAccess.BeforeCommit += delegate(IBulkBlockAccessor ba)
		{
			RevertableBlockAccess_BeforeCommit(ba, workspace);
		};
		workspace.Init(sapi);
		workspace.PlayerUID = playeruid;
		return workspace;
	}

	private TextCommandResult GenMarkedMultiblockCode(IServerPlayer player)
	{
		BlockPos centerPos = player.CurrentBlockSelection.Position;
		new OrderedDictionary<int, int>();
		new List<Vec4i>();
		MultiblockStructure ms = new MultiblockStructure();
		WorldEditWorkspace workspace = workspaces[player.PlayerUID];
		sapi.World.BlockAccessor.WalkBlocks(workspace.StartMarker, workspace.EndMarker, delegate(Block block, int x, int y, int z)
		{
			if (block.Id != 0)
			{
				int orCreateBlockNumber = ms.GetOrCreateBlockNumber(block);
				BlockOffsetAndNumber item = new BlockOffsetAndNumber(x - centerPos.X, y - centerPos.Y, z - centerPos.Z, orCreateBlockNumber);
				ms.Offsets.Add(item);
			}
		}, centerOrder: true);
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("multiblockStructure: {");
		sb.AppendLine("\tblockNumbers: {");
		foreach (KeyValuePair<AssetLocation, int> val2 in ms.BlockNumbers)
		{
			sb.AppendLine($"\t\t\"{val2.Key.ToShortString()}\": {val2.Value},");
		}
		sb.AppendLine("\t},");
		sb.AppendLine("\toffsets: [");
		foreach (BlockOffsetAndNumber val in ms.Offsets)
		{
			sb.AppendLine($"\t\t{{ x: {val.X}, y: {val.Y}, z: {val.Z}, w: {val.W} }},");
		}
		sb.AppendLine("\t]");
		sb.AppendLine("}");
		sapi.World.Logger.Notification("Multiblockstructure centered around {0}:\n{1}", centerPos, sb.ToString());
		return TextCommandResult.Success("Json code written to server-main.log");
	}

	private int RebuildRainMap()
	{
		int mapChunksRebuilt = 0;
		Dictionary<long, IMapChunk> allLoadedMapchunks = sapi.WorldManager.AllLoadedMapchunks;
		int ymax = sapi.WorldManager.MapSizeY / sapi.WorldManager.ChunkSize;
		IServerChunk[] column = new IServerChunk[ymax];
		int chunksize = sapi.WorldManager.ChunkSize;
		foreach (KeyValuePair<long, IMapChunk> val in allLoadedMapchunks)
		{
			int cx = (int)(val.Key % (sapi.WorldManager.MapSizeX / chunksize));
			int cz = (int)(val.Key / (sapi.WorldManager.MapSizeX / chunksize));
			mapChunksRebuilt++;
			for (int cy = 0; cy < ymax; cy++)
			{
				column[cy] = sapi.WorldManager.GetChunk(cx, cy, cz);
				column[cy]?.Unpack();
			}
			for (int dx = 0; dx < chunksize; dx++)
			{
				for (int dz = 0; dz < chunksize; dz++)
				{
					for (int dy = sapi.WorldManager.MapSizeY - 1; dy >= 0; dy--)
					{
						IServerChunk chunk = column[dy / chunksize];
						if (chunk != null)
						{
							int index = (dy % chunksize * chunksize + dz) * chunksize + dx;
							if (!sapi.World.Blocks[chunk.Data.GetBlockId(index, 3)].RainPermeable || dy == 0)
							{
								val.Value.RainHeightMap[dz * chunksize + dx] = (ushort)dy;
								break;
							}
						}
					}
				}
			}
			sapi.WorldManager.ResendMapChunk(cx, cz, onlyIfInRange: true);
			val.Value.MarkDirty();
		}
		return mapChunksRebuilt;
	}

	private void EnsureInsideMap(BlockPos pos)
	{
		pos.X = GameMath.Clamp(pos.X, 0, sapi.WorldManager.MapSizeX - 1);
		pos.Y = GameMath.Clamp(pos.Y, 0, sapi.WorldManager.MapSizeY - 1);
		pos.Z = GameMath.Clamp(pos.Z, 0, sapi.WorldManager.MapSizeZ - 1);
	}

	public void SelectionMode(bool on, IServerPlayer player)
	{
		if (player != null)
		{
			player.WorldData.AreaSelectionMode = on;
			player.BroadcastPlayerData();
		}
	}

	private TextCommandResult HandleHistoryChange(TextCommandCallingArgs args, bool redo)
	{
		WorldEditWorkspace workspace = GetWorkSpace(args.Caller.Player.PlayerUID);
		if (redo && workspace.revertableBlockAccess.CurrentHistoryState == 0)
		{
			return TextCommandResult.Error("Can't redo. Already on newest history state.");
		}
		if (!redo && workspace.revertableBlockAccess.CurrentHistoryState == workspace.revertableBlockAccess.AvailableHistoryStates)
		{
			return TextCommandResult.Error("Can't undo. Already on oldest available history state.");
		}
		int currentHistoryState = workspace.revertableBlockAccess.CurrentHistoryState;
		int steps = (int)args[0];
		workspace.revertableBlockAccess.ChangeHistoryState(steps * ((!redo) ? 1 : (-1)));
		workspace.ResendBlockHighlights();
		int quantityChanged = Math.Abs(currentHistoryState - workspace.revertableBlockAccess.CurrentHistoryState);
		return TextCommandResult.Success(string.Format("Performed {0} {1} times.", redo ? "redo" : "undo", quantityChanged));
	}

	public void OnInteractStart(IServerPlayer byPlayer, BlockSelection blockSel)
	{
		if (CanUseWorldEdit(byPlayer))
		{
			WorldEditWorkspace workspace = GetOrCreateWorkSpace(byPlayer);
			if (workspace.ToolsEnabled && workspace.ToolInstance != null)
			{
				workspace.ToolInstance.OnInteractStart(this, blockSel?.Clone());
			}
		}
	}

	public void OnAttackStart(IServerPlayer byPlayer, BlockSelection blockSel)
	{
		if (CanUseWorldEdit(byPlayer))
		{
			WorldEditWorkspace workspace = GetOrCreateWorkSpace(byPlayer);
			if (workspace.ToolsEnabled && workspace.ToolInstance != null)
			{
				workspace.ToolInstance.OnAttackStart(this, blockSel?.Clone());
			}
		}
	}

	private void OnDidBuildBlock(IServerPlayer byPlayer, int oldBlockId, BlockSelection blockSel, ItemStack withItemStack)
	{
		if (CanUseWorldEdit(byPlayer))
		{
			WorldEditWorkspace workspace = GetOrCreateWorkSpace(byPlayer);
			if (workspace.ToolsEnabled && workspace.ToolInstance != null)
			{
				workspace.ToolInstance.OnBuild(this, oldBlockId, blockSel.Clone(), withItemStack);
			}
		}
	}

	private void OnBreakBlock(IServerPlayer byBplayer, BlockSelection blockSel, ref float dropQuantityMultiplier, ref EnumHandling handling)
	{
		if (CanUseWorldEdit(byBplayer))
		{
			WorldEditWorkspace workspace = GetOrCreateWorkSpace(byBplayer);
			if (workspace.ToolsEnabled && workspace.ToolInstance != null)
			{
				workspace.ToolInstance.OnBreak(this, blockSel, ref handling);
			}
		}
	}

	public static void Good(IServerPlayer player, string message, params object[] args)
	{
		player.SendMessage(0, string.Format(message, args), EnumChatType.CommandSuccess);
	}

	public static void Bad(IServerPlayer player, string message, params object[] args)
	{
		player.SendMessage(0, string.Format(message, args), EnumChatType.CommandError);
	}

	private void RegisterCommands()
	{
		CommandArgumentParsers parsers = sapi.ChatCommands.Parsers;
		sapi.ChatCommands.GetOrCreate("we").IgnoreAdditionalArgs().HandleWith(onToolCommand)
			.RequiresPrivilege("worldedit")
			.WithPreCondition(loadWorkSpace)
			.WithDesc("Creative mode world editing tools.<br> If you want to enable the old commands you can do so with <a href=\"chattype:///worldconfigcreate bool legacywecommands true\">/worldconfigcreate bool legacywecommands true</a>")
			.BeginSub("import-rotation")
			.WithDesc("Set data import angle")
			.WithAlias("impr")
			.WithArgs(parsers.WordRange("angle", "-270", "-180", "-90", "0", "90", "180", "270"))
			.HandleWith(handleImpr)
			.EndSub()
			.BeginSub("import-flip")
			.WithDesc("Set data import flip mode")
			.WithAlias("impflip")
			.HandleWith(handleImpflip)
			.EndSub()
			.BeginSub("constrain")
			.WithDesc("Constrain all world edit operations")
			.WithAlias("ct")
			.WithArgs(parsers.WordRange("constraint type", "none", "selection"))
			.HandleWith(handleConstrain)
			.EndSub()
			.BeginSub("copy")
			.WithDesc("Copy marked position to server clipboard")
			.WithAlias("c")
			.HandleWith(handleMcopy)
			.EndSub()
			.BeginSub("testlaunch")
			.WithDesc("Copy marked position to movable chunks (like a ship) and delete original")
			.HandleWith(handleLaunch)
			.EndSub()
			.BeginSub("scp")
			.WithDesc("Copy a /we mark command text to your local clipboard")
			.WithAlias("selection-clipboard")
			.HandleWith(handlemPosCopy)
			.EndSub()
			.BeginSub("paste")
			.WithDesc("Paste server clipboard data")
			.WithAlias("p")
			.WithAlias("v")
			.HandleWith(handlemPaste)
			.EndSub()
			.BeginSub("cbi")
			.WithDesc("Information about marked area in server clipoard")
			.WithAlias("clipboard-info")
			.WithAlias("cbinfo")
			.HandleWith(handleCbInfo)
			.EndSub()
			.BeginSub("block")
			.WithDesc("Places a block below the caller")
			.WithAlias("b")
			.HandleWith(handleBlock)
			.EndSub()
			.BeginSub("relight")
			.WithDesc("Toggle server block relighting. Speeds up operations when doing large scale worldedits")
			.WithAlias("rl")
			.WithArgs(parsers.OptionalBool("on/off"))
			.HandleWith(handleRelight)
			.EndSub()
			.BeginSub("op")
			.RequiresPrivilege(Privilege.controlserver)
			.WithAlias("overload-protection")
			.WithAlias("sovp")
			.WithDesc("Toggle server overload protection")
			.WithArgs(parsers.OptionalBool("on/off"))
			.HandleWith(handleSovp)
			.EndSub()
			.BeginSub("undo")
			.WithDesc("Undo last world edit action")
			.WithAlias("u")
			.WithAlias("z")
			.WithArgs(parsers.OptionalInt("amount", 1))
			.HandleWith(handleUndo)
			.EndSub()
			.BeginSub("redo")
			.WithDesc("Redo last world edit action")
			.WithAlias("r")
			.WithAlias("y")
			.WithArgs(parsers.OptionalInt("amount", 1))
			.HandleWith(handleRedo)
			.EndSub()
			.BeginSub("on")
			.WithDesc("Enabled world edit tool mode")
			.HandleWith(handleToolModeOn)
			.EndSub()
			.BeginSub("off")
			.WithDesc("Disable world edit tool mode")
			.HandleWith(handleToolModeOff)
			.EndSub()
			.BeginSub("rebuild-rainmap")
			.RequiresPrivilege(Privilege.controlserver)
			.WithAlias("rebuildrainmap")
			.WithAlias("rrm")
			.WithDesc("Rebuild rainheightmap on all loaded chunks")
			.HandleWith(handleRebuildRainmap)
			.EndSub()
			.BeginSub("tool")
			.WithDesc("Select world edit tool mode")
			.WithAlias("t")
			.WithArgs(parsers.All("tool name"))
			.HandleWith(handleSetTool)
			.EndSub()
			.BeginSub("tool-offset")
			.WithDesc("Set tool offset mode")
			.WithAlias("tom")
			.WithAlias("to")
			.WithArgs(parsers.Int("Mode index number"))
			.HandleWith(handleTom)
			.EndSub()
			.BeginSub("pr")
			.WithDesc("Set player picking range (default survival mode value is 4.5)")
			.RequiresPlayer()
			.WithAlias("player-reach")
			.WithAlias("range")
			.WithArgs(parsers.DoubleRange("range", 0.0, 9999.0))
			.HandleWith(handleRange)
			.EndSub()
			.BeginSub("export")
			.WithDesc("Export marked area to server file system")
			.WithAlias("exp")
			.WithArgs(parsers.Word("file name"), parsers.OptionalWord("'c' to also copy mark command to client clipboard"))
			.HandleWith(handleMex)
			.EndSub()
			.BeginSub("export-client")
			.WithDesc("Export selected area to client file system")
			.WithAlias("expc")
			.WithArgs(parsers.Word("file name"), parsers.OptionalWord("'c' to also copy mark command to client clipboard"))
			.HandleWith(handleMexc)
			.EndSub()
			.BeginSub("relight-selection")
			.WithDesc("Relight selected area")
			.WithAlias("rls")
			.HandleWith(handleRelightMarked)
			.EndSub()
			.BeginSub("generate-multiblock-code")
			.WithDesc("Generate multiblock code of selected area")
			.WithAlias("gmc")
			.HandleWith(handleMgenCode)
			.EndSub()
			.BeginSub("generate-claim-code")
			.WithDesc("Generate CustomLandClaims code of selected area with the players position as the reference. So put your player on the start position (green marker when entire schematic is selected via magic select mode) of the schematic.")
			.WithAlias("gcc")
			.HandleWith(handleClaimCode)
			.EndSub()
			.BeginSub("import")
			.WithDesc("Import schematic by filename to selected area")
			.WithAlias("imp")
			.WithArgs(parsers.Word("file name"), parsers.OptionalWord("origin mode"))
			.HandleWith(handleImport)
			.EndSub()
			.BeginSub("importl")
			.WithDesc("Import large schematic by filename to selected area. This does not allow you to revert it but does speed up import time by a lot. If you select the import tool and set the Replace Mode: Replace all no air, it will be even faster")
			.WithAlias("impl")
			.WithArgs(parsers.Word("file name"), parsers.OptionalWord("origin mode"))
			.HandleWith(handleImportLarge)
			.EndSub()
			.BeginSub("resolve-meta")
			.WithAlias("rm")
			.WithDesc("Toggle resolve meta blocks mode during Worldedit import. Turn it off to spawn structures as they are. For example, in this mode, instead of traders, their meta spawners will spawn")
			.WithArgs(parsers.OptionalBool("on/off"))
			.HandleWith(handleToggleImpres)
			.EndSub()
			.BeginSub("start")
			.WithDesc("Mark start position for selection")
			.WithAlias("s")
			.WithAlias("1")
			.HandleWith(handleMarkStart)
			.EndSub()
			.BeginSub("end")
			.WithDesc("Mark end position for selection")
			.WithAlias("e")
			.WithAlias("2")
			.HandleWith(handleMarkEnd)
			.EndSub()
			.BeginSub("select")
			.WithDesc("Select area by coordinates")
			.WithAlias("mark")
			.WithArgs(parsers.WorldPosition("start position"), parsers.WorldPosition("end position"))
			.HandleWith(handleMark)
			.EndSub()
			.BeginSub("resize")
			.WithDesc("Resize the current selection")
			.WithAlias("res")
			.WithArgs(parsers.Word("direction", new string[5] { "north", "n", "z", "-x", "l (for look direction)" }), parsers.OptionalInt("amount", 1))
			.HandleWith(handleResize)
			.EndSub()
			.BeginSub("grow")
			.WithAlias("g")
			.WithDesc("Grow selection in given direction (gl for look direction)")
			.WithArgs(parsers.Word("direction", new string[5] { "north", "n", "z", "-x", "l (for look direction)" }), parsers.OptionalInt("amount", 1), parsers.OptionalBool("quiet"))
			.HandleWith(handleGrowSelection)
			.EndSub()
			.BeginSub("rotate")
			.WithDesc("Rotate selected area")
			.WithAlias("rot")
			.WithArgs(parsers.WordRange("angle", "-270", "-180", "-90", "0", "90", "180", "270"))
			.HandleWith(handleRotateSelection)
			.EndSub()
			.BeginSub("mirror")
			.WithDesc("Mirrors the current selection")
			.WithAlias("mir")
			.WithArgs(parsers.Word("direction", new string[5] { "north", "n", "z", "-x", "l (for look direction)" }))
			.HandleWith(handleMirrorSelection)
			.EndSub()
			.BeginSub("flip")
			.WithDesc("Flip selected area in place")
			.WithArgs(parsers.Word("direction", new string[5] { "north", "n", "z", "-x", "l (for look direction)" }))
			.HandleWith(handleFlipSelection)
			.EndSub()
			.BeginSub("repeat")
			.WithAlias("rep")
			.WithDesc("Repeat selected area in given direction")
			.WithArgs(parsers.Word("direction", new string[5] { "north", "n", "z", "-x", "l (for look direction)" }), parsers.OptionalInt("amount", 1), parsers.OptionalWordRange("selection behavior (sn=select new area, gn=grow to include new area)", "sn", "gn"))
			.HandleWith(handleRepeatSelection)
			.EndSub()
			.BeginSub("move")
			.WithAlias("m")
			.WithDesc("Move selected area in given direction")
			.WithArgs(parsers.Word("direction", new string[5] { "north", "n", "z", "-x", "l (for look direction)" }), parsers.OptionalInt("amount", 1), parsers.OptionalBool("quiet"))
			.HandleWith(handleMoveSelection)
			.EndSub()
			.BeginSub("mmby")
			.WithDesc("Move selected area by given amount")
			.WithArgs(parsers.IntDirection("direction"))
			.HandleWith(handleMoveSelectionBy)
			.EndSub()
			.BeginSub("shift")
			.WithDesc("Shift current selection by given amount (does not move blocks, only the selection)")
			.WithArgs(parsers.Word("direction", new string[5] { "north", "n", "z", "-x", "l (for look direction)" }), parsers.OptionalInt("amount", 1), parsers.OptionalBool("quiet"))
			.HandleWith(handleShiftSelection)
			.EndSub()
			.BeginSub("clear")
			.WithAlias("cs")
			.WithDesc("Clear current selection. Does not remove blocks, only the selection.")
			.HandleWith(handleClearSelection)
			.EndSub()
			.BeginSub("info")
			.WithAlias("info-selection")
			.WithAlias("is")
			.WithDesc("Info about your current selection")
			.HandleWith(handleSelectionInfo)
			.EndSub()
			.BeginSub("fill")
			.WithAlias("f")
			.WithDesc("Fill current selection with the block you are holding and remove all entities inside the area")
			.HandleWith(handleFillSelection)
			.EndSub()
			.BeginSub("delete")
			.WithAlias("del")
			.WithDesc("Delete all blocks and entities inside your current selection")
			.HandleWith(handleDeleteSelection)
			.EndSub()
			.BeginSub("pacify-water")
			.WithAlias("pw")
			.WithDesc("Pacify water in selected area. Turns all flowing water block into still water.")
			.WithArgs(parsers.OptionalWord("liquid code (default: water)"))
			.HandleWith(handlePacifyWater)
			.EndSub()
			.BeginSub("deletewater")
			.WithAlias("delete-water")
			.WithAlias("delw")
			.WithDesc("Deletes all water in selected area")
			.WithArgs(parsers.OptionalWord("liquidcode"))
			.HandleWith(handleClearWater)
			.EndSub()
			.BeginSub("delete-nearby")
			.WithDesc("Delete area (blocks and entities) around caller")
			.WithArgs(parsers.Int("horizontal size"), parsers.Int("height"))
			.HandleWith(handleDeleteArea)
			.EndSub()
			.BeginSub("fix-blockentities")
			.WithAlias("fixbe")
			.WithDesc("Fix incorrect block entities in selected areas")
			.HandleWith(validateArea)
			.EndSub()
			.BeginSub("replace-material")
			.WithAlias("replacemat")
			.WithAlias("repmat")
			.RequiresPlayer()
			.WithDesc("Replace a block material with another one, only if supported by the block. Hotbarslot 0: Search material, Active hand slot: Replace material")
			.HandleWith(onReplaceMaterial)
			.EndSub()
			.BeginSub("wipeworkspace")
			.WithDesc("Clear a players worldedit workspace data and settings")
			.WithArgs(parsers.PlayerUids("player"))
			.HandleWith(WipeWorkspace)
			.EndSub()
			.BeginSub("tool-rsp")
			.WithDesc("Enable disable the right setting panel")
			.WithAlias("rsp")
			.WithArgs(parsers.Bool("Mode"))
			.HandleWith(handleRsp)
			.EndSub()
			.BeginSub("tool-axislock")
			.WithDesc("Switch the axis lock for scroll wheel move and selection actions")
			.WithAlias("tal")
			.WithArgs(parsers.Int("Mode"))
			.HandleWith(handleTal)
			.EndSub()
			.BeginSub("tool-stepsize")
			.WithDesc("Set the stepsize for the scroll wheel actions")
			.WithAlias("step")
			.WithArgs(parsers.Int("amount"))
			.HandleWith(handleStep)
			.EndSub()
			.Validate();
		if (sapi.World.Config.GetBool("legacywecommands"))
		{
			sapi.ChatCommands.GetOrCreate("we").BeginSub("resolve-meta").WithAlias("impres")
				.BeginSub("copy")
				.WithAlias("mcopy")
				.EndSub()
				.BeginSub("scp")
				.WithAlias("mposcopy")
				.EndSub()
				.BeginSub("paste")
				.WithAlias("mpaste")
				.EndSub()
				.BeginSub("export")
				.WithAlias("mex")
				.EndSub()
				.BeginSub("export-client")
				.WithAlias("mexc")
				.EndSub()
				.BeginSub("relight-selection")
				.WithAlias("mre")
				.EndSub()
				.BeginSub("generate-multiblock-code")
				.WithAlias("mgencode")
				.EndSub()
				.BeginSub("start")
				.WithAlias("ms")
				.EndSub()
				.BeginSub("end")
				.WithAlias("me")
				.EndSub()
				.BeginSub("fill")
				.WithAlias("mfill")
				.EndSub()
				.BeginSub("delete")
				.WithAlias("mdelete")
				.EndSub()
				.BeginSub("pacify-water")
				.WithAlias("mpacifywater")
				.EndSub()
				.BeginSub("clear")
				.WithAlias("mc")
				.EndSub()
				.BeginSub("deletewater")
				.WithAlias("mdeletewater")
				.EndSub()
				.BeginSub("info")
				.WithAlias("minfo")
				.EndSub()
				.BeginSub("rotate")
				.WithAlias("mr")
				.EndSub()
				.BeginSubs("mmirn", "mmire", "mmirs", "mmirw", "mmiru", "mmird")
				.WithDesc("Mirror selected area in given direction")
				.WithArgs(parsers.OptionalWordRange("selection behavior (sn=select new area, gn=grow to include new area)", "sn", "gn"))
				.HandleWith(handleMirrorShorthand)
				.EndSub()
				.BeginSubs("mprepn", "mprepe", "mprepe", "mpreps", "mprepu", "mprepd")
				.WithDesc("Repeat selected area in given direction")
				.WithArgs(parsers.OptionalInt("amount", 1), parsers.OptionalWordRange("selection behavior (sn=select new area, gn=grow to include new area)", "sn", "gn"))
				.HandleWith(handleRepeatShorthand)
				.EndSub()
				.BeginSubs("mmn", "mme", "mms", "mms", "mmw", "mmu", "mmd")
				.WithDesc("Move selected area in given direction")
				.WithArgs(parsers.OptionalInt("amount", 1))
				.HandleWith(handleMoveSelectionShorthand)
				.EndSub()
				.BeginSubs("smn", "sme", "sms", "sms", "smw", "smu", "smd")
				.WithDesc("Shift current selection in given direction")
				.WithArgs(parsers.OptionalInt("amount", 1))
				.HandleWith(handleShiftSelectionShorthand)
				.EndSub()
				.BeginSubs("gn", "ge", "gs", "gw", "gu", "gd", "gl")
				.WithDesc("Grow selection in given direction (gl for look direction)")
				.WithArgs(parsers.OptionalInt("amount", 1), parsers.OptionalBool("quiet"))
				.HandleWith(handleGrowSelectionShorthand)
				.EndSub()
				.BeginSub("smby")
				.WithDesc("Shift current selection by given amount")
				.WithArgs(parsers.IntDirection("direction"))
				.HandleWith(handleShiftSelectionBy)
				.EndSub();
		}
	}

	private TextCommandResult WipeWorkspace(TextCommandCallingArgs args)
	{
		PlayerUidName[] players = (PlayerUidName[])args.Parsers[0].GetValue();
		LimitedList<string> results = new LimitedList<string>(10);
		if (players.Length == 0)
		{
			return TextCommandResult.Error(Lang.Get("No players found that match your selector"));
		}
		PlayerUidName[] array = players;
		foreach (PlayerUidName parsedplayer in array)
		{
			if (workspaces.ContainsKey(parsedplayer.Uid))
			{
				workspaces.Remove(parsedplayer.Uid);
				IBlockAccessorRevertable revertibleBlockAccess = sapi.World.GetBlockAccessorRevertable(synchronize: true, relight: true);
				WorldEditWorkspace newWorkspace = new WorldEditWorkspace(sapi.World, revertibleBlockAccess);
				newWorkspace.Init(sapi);
				revertibleBlockAccess.BeforeCommit += delegate(IBulkBlockAccessor ba)
				{
					RevertableBlockAccess_BeforeCommit(ba, newWorkspace);
				};
				workspaces[parsedplayer.Uid] = newWorkspace;
				newWorkspace.PlayerUID = parsedplayer.Uid;
				results.Add("Workspace for " + parsedplayer.Name + " deleted");
				SendPlayerWorkSpace(parsedplayer.Uid);
			}
			else
			{
				results.Add("No Workspace for " + parsedplayer.Name + " exists");
			}
		}
		if (players.Length <= 10)
		{
			return TextCommandResult.Success(string.Join(", ", results));
		}
		return TextCommandResult.Success(Lang.Get("Successfully executed commands on {0} players", players.Length));
	}

	private TextCommandResult handleConstrain(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Worldedit cosntrain mode " + ((workspace.WorldEditConstraint == EnumWorldEditConstraint.Selection) ? "enabled" : "disabled"));
		}
		workspace.WorldEditConstraint = (((string)args[0] == "selection") ? EnumWorldEditConstraint.Selection : EnumWorldEditConstraint.None);
		return TextCommandResult.Success("Constraint " + workspace.WorldEditConstraint.ToString() + " set.");
	}

	private TextCommandResult handleMirrorShorthand(TextCommandCallingArgs args)
	{
		char dirchar = args.SubCmdCode[args.SubCmdCode.Length - 1];
		return workspace.HandleMirrorCommand(BlockFacing.FromFirstLetter(dirchar), (string)args[0]);
	}

	private TextCommandResult handleMirrorSelection(TextCommandCallingArgs args)
	{
		BlockFacing facing = blockFacingFromArg((string)args[0], args);
		return workspace.HandleMirrorCommand(facing, "sn");
	}

	private TextCommandResult onReplaceMaterial(TextCommandCallingArgs args)
	{
		if (workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Error("Start marker or end marker not set");
		}
		IPlayer plr = args.Caller.Player;
		ItemSlot fromslot = plr.InventoryManager.GetHotbarInventory()[0];
		ItemSlot toslot = plr.InventoryManager.ActiveHotbarSlot;
		if (fromslot.Empty)
		{
			return TextCommandResult.Error("Not holding target block in inventory slot 0");
		}
		if (toslot.Empty || toslot.Itemstack.Block == null)
		{
			return TextCommandResult.Error("Not holding replace block in hands");
		}
		int corrected = 0;
		iterateOverSelection(delegate(BlockPos pos)
		{
			IMaterialExchangeable @interface = sapi.World.BlockAccessor.GetBlock(pos).GetInterface<IMaterialExchangeable>(sapi.World, pos);
			if (@interface != null && @interface.ExchangeWith(fromslot, toslot))
			{
				corrected++;
			}
		});
		return TextCommandResult.Success(corrected + " block materials exchanged");
	}

	private TextCommandResult handleClearWater(TextCommandCallingArgs args)
	{
		if (workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Error("Start marker or end marker not set");
		}
		string liquidCode = (args.Parsers[0].IsMissing ? "water" : (args[0] as string));
		int corrected = 0;
		iterateOverSelection(delegate(BlockPos pos)
		{
			if (sapi.World.BlockAccessor.GetBlock(pos, 2).LiquidCode == liquidCode)
			{
				sapi.World.BlockAccessor.SetBlock(0, pos, 2);
				corrected++;
			}
		});
		return TextCommandResult.Success(corrected + " water blocks removed");
	}

	private TextCommandResult handlePacifyWater(TextCommandCallingArgs args)
	{
		if (workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Error("Start marker or end marker not set");
		}
		string liquidCode = (args.Parsers[0].IsMissing ? "water" : (args[0] as string));
		int corrected = 0;
		Block stillWater = sapi.World.GetBlock(new AssetLocation(liquidCode + "-still-7"));
		iterateOverSelection(delegate(BlockPos pos)
		{
			Block block = sapi.World.BlockAccessor.GetBlock(pos, 2);
			if (block.Id != stillWater.Id && block.LiquidCode == liquidCode)
			{
				sapi.World.BlockAccessor.SetBlock(stillWater.Id, pos, 2);
				corrected++;
			}
		});
		return TextCommandResult.Success(corrected + " liquid blocks pacified");
	}

	private TextCommandResult validateArea(TextCommandCallingArgs args)
	{
		if (workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Error("Start marker or end marker not set");
		}
		int corrected = 0;
		iterateOverSelection(delegate(BlockPos pos)
		{
			Block block = sapi.World.BlockAccessor.GetBlock(pos, 1);
			BlockEntity blockEntity = sapi.World.BlockAccessor.GetBlockEntity(pos);
			string text = ((blockEntity == null) ? null : sapi.ClassRegistry.GetBlockEntityClass(blockEntity.GetType()));
			if (block.EntityClass != text)
			{
				sapi.World.Logger.Notification("Block {0} at {1}/{2}/{3} ought to have entity class {4} but has {5}", block.Code, pos.X, pos.Y, pos.Z, (block.EntityClass == null) ? "null" : block.EntityClass, (text == null) ? "null" : text);
				if (block.EntityClass == null)
				{
					sapi.World.BlockAccessor.RemoveBlockEntity(pos);
				}
				else
				{
					sapi.World.BlockAccessor.SpawnBlockEntity(block.EntityClass, pos);
				}
				corrected++;
			}
		});
		return TextCommandResult.Success(corrected + " block entities corrected. See log files for more detail.");
	}

	private void iterateOverSelection(Action<BlockPos> onPos)
	{
		BlockPos startMarker = workspace.StartMarker;
		BlockPos maxPos = workspace.EndMarker;
		IBlockAccessor worldmap = sapi.World.BlockAccessor;
		int minx = GameMath.Clamp(Math.Min(startMarker.X, maxPos.X), 0, worldmap.MapSizeX);
		int miny = GameMath.Clamp(Math.Min(startMarker.Y, maxPos.Y), 0, worldmap.MapSizeY);
		int minz = GameMath.Clamp(Math.Min(startMarker.Z, maxPos.Z), 0, worldmap.MapSizeZ);
		int maxx = GameMath.Clamp(Math.Max(startMarker.X, maxPos.X), 0, worldmap.MapSizeX);
		int maxy = GameMath.Clamp(Math.Max(startMarker.Y, maxPos.Y), 0, worldmap.MapSizeY);
		int maxz = GameMath.Clamp(Math.Max(startMarker.Z, maxPos.Z), 0, worldmap.MapSizeZ);
		BlockPos tmpPos = new BlockPos();
		for (int x = minx; x < maxx; x++)
		{
			for (int y = miny; y < maxy; y++)
			{
				for (int z = minz; z < maxz; z++)
				{
					tmpPos.Set(x, y, z);
					onPos(tmpPos);
				}
			}
		}
	}

	private TextCommandResult onToolCommand(TextCommandCallingArgs args)
	{
		if (args.RawArgs.Length > 0 && (workspace.ToolInstance == null || !workspace.ToolInstance.OnWorldEditCommand(this, args)))
		{
			return TextCommandResult.Error("No such function " + args.RawArgs.PopWord() + ". Maybe wrong tool selected?");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult loadWorkSpace(TextCommandCallingArgs args)
	{
		if (!CanUseWorldEdit(args.Caller.Player as IServerPlayer, showError: true))
		{
			return TextCommandResult.Error("Caller is not allowed to use world edit");
		}
		workspace = GetOrCreateWorkSpace(args.Caller.Player);
		return TextCommandResult.Success();
	}

	private TextCommandResult handleDeleteArea(TextCommandCallingArgs args)
	{
		int widthlength = (int)args[0];
		int height = (int)args[1];
		BlockPos asBlockPos = args.Caller.Pos.AsBlockPos;
		BlockPos start = asBlockPos.AddCopy(-widthlength, 0, -widthlength);
		BlockPos end = asBlockPos.AddCopy(widthlength, height, widthlength);
		int cleared = workspace.FillArea(null, start, end);
		int entitiesRemoved = RemoveEntitiesInArea(start, end);
		return TextCommandResult.Success(cleared + " Blocks and " + entitiesRemoved + " Entities removed");
	}

	private TextCommandResult handleDeleteSelection(TextCommandCallingArgs args)
	{
		if (workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Error("Start marker or end marker not set");
		}
		int cleared = workspace.FillArea(null, workspace.StartMarker, workspace.EndMarker);
		int entitiesRemoved = RemoveEntitiesInArea(workspace.StartMarker, workspace.EndMarker);
		return TextCommandResult.Success(cleared + " Blocks and " + entitiesRemoved + " Entities removed");
	}

	private int RemoveEntitiesInArea(BlockPos start, BlockPos end)
	{
		Entity[] entitiesInsideCuboid = sapi.World.GetEntitiesInsideCuboid(start, end, (Entity e) => !(e is EntityPlayer));
		Entity[] array = entitiesInsideCuboid;
		foreach (Entity entity in array)
		{
			workspace.revertableBlockAccess.StoreEntitySpawnToHistory(entity);
			entity.Die(EnumDespawnReason.Removed);
		}
		return entitiesInsideCuboid.Length;
	}

	private TextCommandResult handleFillSelection(TextCommandCallingArgs args)
	{
		if (workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Error("Start marker or end marker not set");
		}
		ItemStack stack = args.Caller.Player?.InventoryManager.ActiveHotbarSlot.Itemstack;
		if (stack == null || stack.Class == EnumItemClass.Item)
		{
			return TextCommandResult.Error("Please put the desired block in your active hotbar slot ");
		}
		int filled = workspace.FillArea(stack, workspace.StartMarker, workspace.EndMarker);
		int entitiesInArea = RemoveEntitiesInArea(workspace.StartMarker, workspace.EndMarker);
		return TextCommandResult.Success(filled + " marked blocks placed and " + entitiesInArea + "removed");
	}

	private TextCommandResult handleSelectionInfo(TextCommandCallingArgs args)
	{
		int sizeX = Math.Abs(workspace.StartMarker.X - workspace.EndMarker.X);
		int sizeY = Math.Abs(workspace.StartMarker.Y - workspace.EndMarker.Y);
		int sizeZ = Math.Abs(workspace.StartMarker.Z - workspace.EndMarker.Z);
		return TextCommandResult.Success($"Marked area is a cuboid of size {sizeX}x{sizeY}x{sizeZ} or a total of {(long)sizeX * (long)sizeY * sizeZ:n0} blocks");
	}

	private TextCommandResult handleClearSelection(TextCommandCallingArgs args)
	{
		workspace.StartMarker = null;
		workspace.EndMarker = null;
		workspace.StartMarkerExact = null;
		workspace.EndMarkerExact = null;
		workspace.ResendBlockHighlights();
		return TextCommandResult.Success("Marked positions cleared");
	}

	private TextCommandResult handleShiftSelectionBy(TextCommandCallingArgs args)
	{
		return workspace.HandleShiftCommand((Vec3i)args[0]);
	}

	private TextCommandResult handleShiftSelection(TextCommandCallingArgs args)
	{
		string direction = (string)args[0];
		int amount = (args.Parsers[1].IsMissing ? 1 : ((int)args[1]));
		BlockFacing facing = blockFacingFromArg(direction, args);
		if (facing == null)
		{
			return TextCommandResult.Error("Invalid direction, must be a cardinal, x/y/z or l/look");
		}
		bool quiet = !args.Parsers[2].IsMissing && (bool)args[2];
		return workspace.HandleShiftCommand(facing.Normali.Clone() * amount, quiet);
	}

	private TextCommandResult handleShiftSelectionShorthand(TextCommandCallingArgs args)
	{
		char dirchar = args.SubCmdCode[args.SubCmdCode.Length - 1];
		int amount = (args.Parsers[0].IsMissing ? 1 : ((int)args[0]));
		return workspace.HandleShiftCommand(BlockFacing.FromFirstLetter(dirchar).Normali.Clone() * amount);
	}

	private TextCommandResult handleGrowSelectionShorthand(TextCommandCallingArgs args)
	{
		BlockFacing facing = blockFacingFromArg(args.SubCmdCode[args.SubCmdCode.Length - 1].ToString() ?? "", args);
		bool quiet = !args.Parsers[1].IsMissing && (bool)args[1];
		return workspace.ModifyMarker(facing, (int)args[0], quiet);
	}

	private TextCommandResult handleMoveSelectionBy(TextCommandCallingArgs args)
	{
		return workspace.HandleMoveCommand((Vec3i)args[0]);
	}

	private TextCommandResult handleMoveSelection(TextCommandCallingArgs args)
	{
		string direction = (string)args[0];
		int amount = (args.Parsers[1].IsMissing ? 1 : ((int)args[1]));
		BlockFacing facing = blockFacingFromArg(direction, args);
		if (facing == null)
		{
			return TextCommandResult.Error("Invalid direction, must be a cardinal, x/y/z or l/look");
		}
		bool quiet = !args.Parsers[2].IsMissing && (bool)args[2];
		return workspace.HandleMoveCommand(facing.Normali.Clone() * amount, quiet);
	}

	private TextCommandResult handleMoveSelectionShorthand(TextCommandCallingArgs args)
	{
		char dirchar = args.SubCmdCode[args.SubCmdCode.Length - 1];
		return workspace.HandleMoveCommand(BlockFacing.FromFirstLetter(dirchar).Normali);
	}

	private TextCommandResult handleRepeatSelection(TextCommandCallingArgs args)
	{
		string direction = (string)args[0];
		BlockFacing facing = blockFacingFromArg(direction, args);
		if (facing == null)
		{
			return TextCommandResult.Error("Invalid direction, must be a cardinal, x/y/z or l/look");
		}
		return workspace.HandleRepeatCommand(facing.Normali, (int)args[1], (string)args[2]);
	}

	private TextCommandResult handleRepeatShorthand(TextCommandCallingArgs args)
	{
		char dirchar = args.SubCmdCode[args.SubCmdCode.Length - 1];
		return workspace.HandleRepeatCommand(BlockFacing.FromFirstLetter(dirchar).Normali, (int)args[0], (string)args[1]);
	}

	private TextCommandResult handleRotateSelection(TextCommandCallingArgs args)
	{
		return workspace.HandleRotateCommand(((string)args[0]).ToInt());
	}

	private TextCommandResult handleFlipSelection(TextCommandCallingArgs args)
	{
		string direction = (string)args[0];
		BlockFacing facing = blockFacingFromArg(direction, args);
		if (facing == null)
		{
			return TextCommandResult.Error("Invalid direction, must be a cardinal, x/y/z or l/look");
		}
		return workspace.HandleFlipCommand(facing.Axis);
	}

	private TextCommandResult handleResize(TextCommandCallingArgs args)
	{
		string direction = (string)args[0];
		BlockFacing facing = blockFacingFromArg(direction, args);
		if (facing == null)
		{
			return TextCommandResult.Error("Invalid direction, must be a cardinal, x/y/z or l/look");
		}
		int amount = (int)args[1];
		return workspace.ModifyMarker(facing, amount);
	}

	private BlockFacing blockFacingFromArg(string direction, TextCommandCallingArgs args)
	{
		BlockFacing facing = BlockFacing.FromFirstLetter(direction[0]);
		if (facing != null)
		{
			return facing;
		}
		if (direction != null)
		{
			Vec3f lookVec;
			switch (direction.Length)
			{
			case 1:
				switch (direction[0])
				{
				case 'e':
				case 'x':
					break;
				case 'w':
					goto IL_0127;
				case 'y':
					goto IL_012f;
				case 's':
				case 'z':
					goto IL_013f;
				case 'n':
					goto IL_0147;
				case 'l':
					goto IL_014f;
				default:
					goto end_IL_0022;
				}
				goto IL_011f;
			case 2:
				switch (direction[1])
				{
				case 'x':
					break;
				case 'y':
					goto IL_00d5;
				case 'z':
					goto IL_00f4;
				default:
					goto end_IL_0022;
				}
				if (direction == "+x")
				{
					goto IL_011f;
				}
				if (!(direction == "-x"))
				{
					break;
				}
				goto IL_0127;
			case 4:
				{
					if (!(direction == "look"))
					{
						break;
					}
					goto IL_014f;
				}
				IL_00f4:
				if (direction == "+z")
				{
					goto IL_013f;
				}
				if (!(direction == "-z"))
				{
					break;
				}
				goto IL_0147;
				IL_012f:
				return BlockFacing.UP;
				IL_00d5:
				if (!(direction == "+y"))
				{
					if (!(direction == "-y"))
					{
						break;
					}
					return BlockFacing.DOWN;
				}
				goto IL_012f;
				IL_0127:
				return BlockFacing.WEST;
				IL_011f:
				return BlockFacing.EAST;
				IL_0147:
				return BlockFacing.NORTH;
				IL_013f:
				return BlockFacing.SOUTH;
				IL_014f:
				lookVec = args.Caller.Entity.SidedPos.GetViewVector();
				return BlockFacing.FromVector(lookVec.X, lookVec.Y, lookVec.Z);
				end_IL_0022:
				break;
			}
		}
		return null;
	}

	private TextCommandResult handleGrowSelection(TextCommandCallingArgs args)
	{
		string direction = (string)args[0];
		BlockFacing facing = blockFacingFromArg(direction, args);
		bool quiet = !args.Parsers[2].IsMissing && (bool)args[2];
		return workspace.ModifyMarker(facing, (int)args[1], quiet);
	}

	private TextCommandResult handleMark(TextCommandCallingArgs args)
	{
		Vec3d start = args[0] as Vec3d;
		Vec3d end = args[1] as Vec3d;
		if (start.X - (double)(int)start.X < 0.1)
		{
			start.Add(0.5, 0.5, 0.5);
		}
		else
		{
			start.Add(0.0, 0.5, 0.0);
		}
		if (end.X - (double)(int)end.X < 0.1)
		{
			end.Add(0.5, 0.5, 0.5);
		}
		else
		{
			end.Add(0.0, 0.5, 0.0);
		}
		workspace.StartMarkerExact = start;
		workspace.EndMarkerExact = end;
		workspace.UpdateSelection();
		return TextCommandResult.Success("Start and end position marked");
	}

	private TextCommandResult handleMarkEnd(TextCommandCallingArgs args)
	{
		return workspace.SetEndPos(args.Caller.Pos);
	}

	private TextCommandResult handleMarkStart(TextCommandCallingArgs args)
	{
		return workspace.SetStartPos(args.Caller.Pos);
	}

	private TextCommandResult handleToggleImpres(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Meta block replacing for worldedit currently " + (ReplaceMetaBlocks ? "on" : "off"));
		}
		bool doReplace = (bool)args[0];
		ReplaceMetaBlocks = doReplace;
		return TextCommandResult.Success("Meta block replacing for worldedit now " + (doReplace ? "on" : "off"));
	}

	private TextCommandResult handleImport(TextCommandCallingArgs args)
	{
		if (workspace.StartMarker == null)
		{
			return TextCommandResult.Error("Please mark a start position");
		}
		string filename = (string)args[0];
		EnumOrigin origin = EnumOrigin.StartPos;
		if (!args.Parsers[1].IsMissing)
		{
			try
			{
				origin = (EnumOrigin)Enum.Parse(typeof(EnumOrigin), (string)args[1]);
			}
			catch
			{
			}
		}
		return workspace.ImportArea(filename, workspace.StartMarker, origin, isLarge: false);
	}

	private TextCommandResult handleImportLarge(TextCommandCallingArgs args)
	{
		if (workspace.StartMarker == null)
		{
			return TextCommandResult.Error("Please mark a start position");
		}
		string filename = (string)args[0];
		EnumOrigin origin = EnumOrigin.StartPos;
		if (!args.Parsers[1].IsMissing)
		{
			try
			{
				origin = (EnumOrigin)Enum.Parse(typeof(EnumOrigin), (string)args[1]);
			}
			catch
			{
			}
		}
		return workspace.ImportArea(filename, workspace.StartMarker, origin, isLarge: true);
	}

	private TextCommandResult handleMgenCode(TextCommandCallingArgs args)
	{
		if (workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Error("Please mark start and end position");
		}
		if (args.Caller.Player.CurrentBlockSelection == null)
		{
			return TextCommandResult.Error("Please look at a block");
		}
		return GenMarkedMultiblockCode(args.Caller.Player as IServerPlayer);
	}

	private TextCommandResult handleClaimCode(TextCommandCallingArgs args)
	{
		BlockPos pos = args.Caller.Player.Entity.Pos.AsBlockPos;
		if (workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Error("Please mark start and end position");
		}
		BlockPos start = workspace.StartMarker - pos;
		BlockPos end = workspace.EndMarker - pos;
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("CustomLandClaims: [");
		StringBuilder stringBuilder = sb;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(41, 6, stringBuilder);
		handler.AppendLiteral("\t\t\t{ x1: ");
		handler.AppendFormatted(start.X);
		handler.AppendLiteral(", y1: ");
		handler.AppendFormatted(start.Y);
		handler.AppendLiteral(", z1: ");
		handler.AppendFormatted(start.Z);
		handler.AppendLiteral(", x2: ");
		handler.AppendFormatted(end.X);
		handler.AppendLiteral(", y2: ");
		handler.AppendFormatted(end.Y);
		handler.AppendLiteral(", z2: ");
		handler.AppendFormatted(end.Z);
		handler.AppendLiteral(" }");
		stringBuilder.AppendLine(ref handler);
		sb.AppendLine("\t],");
		sapi.World.Logger.Notification("CustomLandClaims centered around player position: {0}:\n{1}", pos, sb.ToString());
		return TextCommandResult.Success("Json code written to server-main.log");
	}

	private TextCommandResult handleRelightMarked(TextCommandCallingArgs args)
	{
		if (workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Error("Please mark start and end position");
		}
		if (args.Caller.Player is IServerPlayer splr)
		{
			splr.SendMessage(args.Caller.FromChatGroupId, "Relighting marked area, this may lag the server for a while...", EnumChatType.Notification);
		}
		sapi.WorldManager.FullRelight(workspace.StartMarker, workspace.EndMarker);
		return TextCommandResult.Success("Ok, relighting complete");
	}

	private TextCommandResult handleMexc(TextCommandCallingArgs args)
	{
		return export(args, sendToClient: true);
	}

	private TextCommandResult handleMex(TextCommandCallingArgs args)
	{
		return export(args, sendToClient: false);
	}

	private TextCommandResult export(TextCommandCallingArgs args, bool sendToClient)
	{
		if (workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Error("Please mark start and end position");
		}
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		if ((args[1] as string)?.ToLowerInvariant() == "c")
		{
			BlockPos st = workspace.StartMarkerExact.AsBlockPos;
			BlockPos en = workspace.EndMarkerExact.AsBlockPos;
			serverChannel.SendPacket(new CopyToClipboardPacket
			{
				Text = string.Format("/we mark ={0} ={1} ={2} ={3} ={4} ={5}\n/we {7} {6}", st.X, st.InternalY, st.Z, en.X, en.InternalY, en.Z, args[0], sendToClient ? "expc" : "exp")
			}, player);
		}
		return workspace.ExportArea((string)args[0], workspace.StartMarker, workspace.EndMarker, sendToClient ? player : null);
	}

	private TextCommandResult handleRange(TextCommandCallingArgs args)
	{
		float pickingrange = (float)(double)args[0];
		args.Caller.Player.WorldData.PickingRange = pickingrange;
		((IServerPlayer)args.Caller.Player).BroadcastPlayerData();
		return TextCommandResult.Success("Picking range " + pickingrange + " set");
	}

	private TextCommandResult handleTom(TextCommandCallingArgs args)
	{
		EnumToolOffsetMode mode = EnumToolOffsetMode.Center;
		try
		{
			mode = (EnumToolOffsetMode)(int)args[0];
		}
		catch (Exception)
		{
		}
		workspace.ToolOffsetMode = mode;
		workspace.ResendBlockHighlights();
		return TextCommandResult.Success("Set tool offset mode " + mode);
	}

	private TextCommandResult handleTal(TextCommandCallingArgs args)
	{
		EnumFreeMovAxisLock mode = EnumFreeMovAxisLock.None;
		try
		{
			mode = (EnumFreeMovAxisLock)(int)args[0];
		}
		catch (Exception)
		{
		}
		workspace.ToolAxisLock = (int)mode;
		return TextCommandResult.Success("Set tool axis lock " + mode);
	}

	private TextCommandResult handleStep(TextCommandCallingArgs args)
	{
		int amount = 0;
		try
		{
			amount = (int)args[0];
		}
		catch (Exception)
		{
		}
		workspace.StepSize = amount;
		return TextCommandResult.Success("Set tool step size to " + amount);
	}

	private TextCommandResult handleRsp(TextCommandCallingArgs args)
	{
		bool enabled = true;
		try
		{
			enabled = (bool)args[0];
		}
		catch (Exception)
		{
		}
		workspace.Rsp = enabled;
		return TextCommandResult.Success("Set Right settings panel " + (enabled ? "on" : "off"));
	}

	private TextCommandResult handleSetTool(TextCommandCallingArgs args)
	{
		string toolname = null;
		string suppliedToolname = (string)args[0];
		if (suppliedToolname.Length > 0)
		{
			if (int.TryParse(suppliedToolname, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var toolId))
			{
				if (toolId < 0)
				{
					workspace.ToolsEnabled = false;
					workspace.SetTool(null, sapi);
					workspace.ResendBlockHighlights();
					SendPlayerWorkSpace(workspace.PlayerUID);
					return TextCommandResult.Success("World edit tools now disabled");
				}
				toolname = ToolRegistry.ToolTypes.GetKeyAtIndex(toolId);
			}
			else
			{
				foreach (string name in ToolRegistry.ToolTypes.Keys)
				{
					if (name.StartsWith(suppliedToolname, StringComparison.InvariantCultureIgnoreCase))
					{
						toolname = name;
						break;
					}
				}
			}
		}
		if (toolname == null)
		{
			return TextCommandResult.Error("No such tool '" + suppliedToolname + "' registered");
		}
		workspace.SetTool(toolname, sapi);
		workspace.ToolsEnabled = true;
		workspace.ResendBlockHighlights();
		SendPlayerWorkSpace(workspace.PlayerUID);
		return TextCommandResult.Success(toolname + " tool selected");
	}

	private TextCommandResult handleRebuildRainmap(TextCommandCallingArgs args)
	{
		if (args.Caller.Player is IServerPlayer splr)
		{
			splr.SendMessage(args.Caller.FromChatGroupId, "Ok, rebuilding rain map on all loaded chunks, this may take some time and lag the server", EnumChatType.Notification);
		}
		int rebuilt = RebuildRainMap();
		return TextCommandResult.Success($"Done, rebuilding {rebuilt} map chunks");
	}

	private TextCommandResult handleToolModeOff(TextCommandCallingArgs args)
	{
		workspace.DestroyPreview();
		workspace.ToolsEnabled = false;
		workspace.SetTool(null, sapi);
		workspace.ResendBlockHighlights();
		return TextCommandResult.Success("World edit tools now disabled");
	}

	private TextCommandResult handleToolModeOn(TextCommandCallingArgs args)
	{
		workspace.ToolsEnabled = true;
		if (workspace.ToolName != null)
		{
			workspace.SetTool(workspace.ToolName, sapi);
		}
		workspace.ResendBlockHighlights();
		return TextCommandResult.Success("World edit tools now enabled");
	}

	private TextCommandResult handleRedo(TextCommandCallingArgs args)
	{
		return HandleHistoryChange(args, redo: true);
	}

	private TextCommandResult handleUndo(TextCommandCallingArgs args)
	{
		return HandleHistoryChange(args, redo: false);
	}

	private TextCommandResult handleRelight(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Block relighting is currently " + (workspace.DoRelight ? "on" : "off"));
		}
		workspace.DoRelight = (bool)args[0];
		workspace.revertableBlockAccess.Relight = workspace.DoRelight;
		return TextCommandResult.Success("Block relighting now " + (workspace.DoRelight ? "on" : "off"));
	}

	private TextCommandResult handleSovp(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Server overload protection is currently " + (workspace.serverOverloadProtection ? "on" : "off"));
		}
		workspace.serverOverloadProtection = (bool)args[0];
		return TextCommandResult.Success("Server overload protection now " + (workspace.serverOverloadProtection ? "on" : "off"));
	}

	private TextCommandResult handleBlock(TextCommandCallingArgs args)
	{
		ItemStack stack = args.Caller.Player?.InventoryManager.ActiveHotbarSlot.Itemstack;
		if (stack == null || stack.Class == EnumItemClass.Item)
		{
			return TextCommandResult.Error("Please put the desired block in your active hotbar slot");
		}
		BlockPos centerPos = args.Caller.Pos.AsBlockPos;
		sapi.World.BlockAccessor.SetBlock(stack.Id, centerPos.DownCopy());
		return TextCommandResult.Success("Block placed");
	}

	private TextCommandResult handleCbInfo(TextCommandCallingArgs args)
	{
		if (workspace.clipboardBlockData == null)
		{
			return TextCommandResult.Error("No schematic in the clipboard");
		}
		workspace.clipboardBlockData.Init(workspace.revertableBlockAccess);
		workspace.clipboardBlockData.LoadMetaInformationAndValidate(workspace.revertableBlockAccess, workspace.world, "(from clipboard)");
		string sides = "";
		for (int i = 0; i < workspace.clipboardBlockData.PathwayStarts.Length; i++)
		{
			if (sides.Length > 0)
			{
				sides += ",";
			}
			sides = sides + workspace.clipboardBlockData.PathwaySides[i].Code + " (" + workspace.clipboardBlockData.PathwayOffsets[i].Length + " blocks)";
		}
		if (sides.Length > 0)
		{
			sides = "Found " + workspace.clipboardBlockData.PathwayStarts.Length + " pathways: " + sides;
		}
		return TextCommandResult.Success(Lang.Get("{0} blocks in clipboard. {1}", workspace.clipboardBlockData.BlockIds.Count, sides));
	}

	private TextCommandResult handlemPaste(TextCommandCallingArgs args)
	{
		if (workspace.clipboardBlockData == null)
		{
			return TextCommandResult.Error("No copied block data to paste");
		}
		workspace.PasteBlockData(workspace.clipboardBlockData, workspace.StartMarker, EnumOrigin.StartPos);
		return TextCommandResult.Success(workspace.clipboardBlockData.BlockIds.Count + " blocks pasted");
	}

	private TextCommandResult handleLaunch(TextCommandCallingArgs args)
	{
		if (workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Error("Please mark start and end position");
		}
		workspace.clipboardBlockData = workspace.CopyArea(workspace.StartMarker, workspace.EndMarker, notLiquids: true);
		workspace.FillArea(null, workspace.StartMarker, workspace.EndMarker, notLiquids: true);
		BlockPos startPos = workspace.StartMarker.Copy();
		startPos.Add(workspace.clipboardBlockData.PackedOffset);
		IMiniDimension miniDimension = workspace.CreateDimensionFromSchematic(workspace.clipboardBlockData, startPos, EnumOrigin.StartPos);
		if (miniDimension == null)
		{
			return TextCommandResult.Error("No more Mini Dimensions available");
		}
		Entity launched = EntityTestShip.CreateShip(sapi, miniDimension);
		launched.Pos.SetFrom(launched.ServerPos);
		workspace.world.SpawnEntity(launched);
		return TextCommandResult.Success(workspace.clipboardBlockData.BlockIds.Count + " blocks launched");
	}

	private TextCommandResult handlemPosCopy(TextCommandCallingArgs args)
	{
		if (workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Error("Please mark start and end position");
		}
		BlockPos s = workspace.StartMarkerExact.AsBlockPos;
		BlockPos e = workspace.EndMarkerExact.AsBlockPos;
		serverChannel.SendPacket(new CopyToClipboardPacket
		{
			Text = $"/we mark ={s.X} ={s.Y} ={s.Z} ={e.X} ={e.Y} ={e.Z}"
		}, args.Caller.Player as IServerPlayer);
		return TextCommandResult.Success("Ok, sent to client clipboard");
	}

	private TextCommandResult handleMcopy(TextCommandCallingArgs args)
	{
		if (workspace.StartMarker == null || workspace.EndMarker == null)
		{
			return TextCommandResult.Error("Please mark start and end position");
		}
		workspace.clipboardBlockData = workspace.CopyArea(workspace.StartMarker, workspace.EndMarker);
		if (workspace.ToolInstance is ImportTool it)
		{
			WorldEdit modSystem = sapi.ModLoader.GetModSystem<WorldEdit>();
			it.SetBlockDatas(modSystem, workspace.clipboardBlockData);
		}
		return TextCommandResult.Success(Lang.Get("{0} blocks and {1} entities copied", workspace.clipboardBlockData.BlockIds.Count, workspace.clipboardBlockData.EntitiesUnpacked.Count));
	}

	private TextCommandResult handleImpflip(TextCommandCallingArgs args)
	{
		workspace.ImportFlipped = !workspace.ImportFlipped;
		return TextCommandResult.Success("Ok, import data flip " + (workspace.ImportFlipped ? "on" : "off"));
	}

	private TextCommandResult handleImpr(TextCommandCallingArgs args)
	{
		workspace.ImportAngle = ((string)args[0]).ToInt();
		return TextCommandResult.Success(Lang.Get("Ok, set rotation to {0} degrees", workspace.ImportAngle));
	}
}
