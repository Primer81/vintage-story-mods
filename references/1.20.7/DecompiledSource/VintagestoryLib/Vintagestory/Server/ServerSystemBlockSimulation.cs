using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Server;

public class ServerSystemBlockSimulation : ServerSystem
{
	private class BlockPosWithExtraObject
	{
		public BlockPos pos;

		public object extra;

		public BlockPosWithExtraObject(BlockPos pos, object extra)
		{
			this.pos = pos;
			this.extra = extra;
		}
	}

	private ConcurrentQueue<object> queuedTicks = new ConcurrentQueue<object>();

	private Dictionary<long, ServerChunk> chunksToBeTicked = new Dictionary<long, ServerChunk>();

	private object clientIdsLock = new object();

	private List<int> clientIds = new List<int>();

	private Random rand = new Random();

	private List<Packet_BlockEntity> blockEntitiesPacked = new List<Packet_BlockEntity>();

	private List<BlockPos> noblockEntities = new List<BlockPos>();

	private List<Packet_BlockEntity> playerBlockEntitiesPacked = new List<Packet_BlockEntity>();

	private HashSet<BlockPos> positionsDone = new HashSet<BlockPos>();

	private BlockPos tmpPos = new BlockPos();

	private FluidBlockPos tmpLiquidPos = new FluidBlockPos();

	public ServerSystemBlockSimulation(ServerMain server)
		: base(server)
	{
		server.RegisterGameTickListener(UpdateEvery100ms, 100);
		server.PacketHandlers[3] = HandleBlockPlaceOrBreak;
		server.PacketHandlers[22] = HandleBlockEntityPacket;
		server.OnHandleBlockInteract = HandleBlockInteract;
	}

	public override void OnBeginInitialization()
	{
		server.api.RegisterBlock(new Block
		{
			DrawType = EnumDrawType.Empty,
			MatterState = EnumMatterState.Gas,
			BlockMaterial = EnumBlockMaterial.Air,
			Code = new AssetLocation("air"),
			Sounds = new BlockSounds(),
			RenderPass = EnumChunkRenderPass.Liquid,
			Replaceable = 9999,
			MaterialDensity = 1,
			LightAbsorption = 0,
			CollisionBoxes = null,
			SelectionBoxes = null,
			RainPermeable = true,
			SideSolid = new SmallBoolArray(0),
			SideAo = new bool[6],
			AllSidesOpaque = false
		});
		Item noitem = new Item(0);
		server.api.RegisterItem(new Item
		{
			Code = new AssetLocation("air")
		});
		for (int i = 1; i < 4000; i++)
		{
			server.Items.Add(noitem);
		}
		server.api.eventapi.ChunkColumnLoaded += Event_ChunkColumnLoaded;
	}

	private void Event_ChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
	{
	}

	public override void OnLoadAssets()
	{
		server.api.Logger.VerboseDebug("Block simulation resolving collectibles");
		server.LoadCollectibles(server.Items, server.Blocks);
		IList<Block> serverBlocks = server.Blocks;
		for (int i = 0; i < serverBlocks.Count; i++)
		{
			Block block = serverBlocks[i];
			if (block == null)
			{
				continue;
			}
			AssetLocation blockName = block.Code;
			if (block.Drops != null)
			{
				BlockDropItemStack[] drops = block.Drops;
				for (int k = 0; k < drops.Length; k++)
				{
					drops[k].Resolve(server, "Block ", blockName);
				}
			}
			if (block.CreativeInventoryStacks != null)
			{
				for (int j = 0; j < block.CreativeInventoryStacks.Length; j++)
				{
					CreativeTabAndStackList list = block.CreativeInventoryStacks[j];
					for (int l = 0; l < list.Stacks.Length; l++)
					{
						list.Stacks[l].Resolve(server, "Creative inventory stack of block ", blockName);
					}
				}
			}
			if (block.CombustibleProps?.SmeltedStack != null)
			{
				block.CombustibleProps.SmeltedStack.Resolve(server, "Smeltedstack of Block ", blockName);
			}
			if (block.NutritionProps?.EatenStack != null)
			{
				block.NutritionProps.EatenStack.Resolve(server, "Eatenstack of Block ", blockName);
			}
			if (block.TransitionableProps != null)
			{
				TransitionableProperties[] transitionableProps = block.TransitionableProps;
				foreach (TransitionableProperties props in transitionableProps)
				{
					if (props.Type != EnumTransitionType.None)
					{
						props.TransitionedStack?.Resolve(server, props.Type.ToString() + " Transition stack of Block ", blockName);
					}
				}
			}
			if (block.GrindingProps?.GroundStack != null)
			{
				block.GrindingProps.GroundStack.Resolve(server, "Grinded stack of Block ", blockName);
				if (block.GrindingProps.usedObsoleteNotation)
				{
					server.api.Logger.Warning("Block code {0}: Property GrindedStack is obsolete, please use GroundStack instead", block.Code);
				}
			}
			if (block.CrushingProps?.CrushedStack != null)
			{
				block.CrushingProps.CrushedStack.Resolve(server, "Crushed stack of Block ", blockName);
			}
		}
		server.api.Logger.VerboseDebug("Resolved blocks stacks");
		((List<Item>)server.Items).ForEach(delegate(Item item)
		{
			if (item != null)
			{
				AssetLocation code = item.Code;
				if (code != null)
				{
					CreativeTabAndStackList[] creativeInventoryStacks = item.CreativeInventoryStacks;
					if (creativeInventoryStacks != null)
					{
						for (int n = 0; n < creativeInventoryStacks.Length; n++)
						{
							JsonItemStack[] stacks = creativeInventoryStacks[n].Stacks;
							for (int num = 0; num < stacks.Length; num++)
							{
								stacks[num].Resolve(server, "Creative inventory stack of Item ", code);
							}
						}
					}
					if (item.CombustibleProps != null && item.CombustibleProps.SmeltedStack != null)
					{
						item.CombustibleProps.SmeltedStack.Resolve(server, "Combustible props for Item ", code);
					}
					if (item.NutritionProps?.EatenStack != null)
					{
						item.NutritionProps.EatenStack.Resolve(server, "Eatenstack of Item ", code);
					}
					if (item.TransitionableProps != null)
					{
						TransitionableProperties[] transitionableProps2 = item.TransitionableProps;
						foreach (TransitionableProperties transitionableProperties in transitionableProps2)
						{
							if (transitionableProperties.Type != EnumTransitionType.None)
							{
								transitionableProperties.TransitionedStack?.Resolve(server, transitionableProperties.Type.ToString() + " Transition stack of Item ", code);
							}
						}
					}
					if (item.GrindingProps?.GroundStack != null)
					{
						item.GrindingProps.GroundStack.Resolve(server, "Grinded stack of item ", code);
						if (item.GrindingProps.usedObsoleteNotation)
						{
							server.api.Logger.Warning("Item code {0}: Property GrindedStack is obsolete, please use GroundStack instead", item.Code);
						}
					}
					if (item.CrushingProps?.CrushedStack != null)
					{
						item.CrushingProps.CrushedStack.Resolve(server, "Crushed stack of item ", code);
					}
				}
			}
		});
		server.api.Logger.VerboseDebug("Resolved items stacks");
	}

	public override void OnBeginConfiguration()
	{
		IChatCommandApi chatCommands = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		_ = server.api;
		chatCommands.Get("debug").BeginSub("bt").WithDesc("Block ticking debug subsystem")
			.BeginSub("at")
			.WithDesc("Tick a block at given position")
			.WithArgs(parsers.WorldPosition("position"))
			.HandleWith(onTickBlockCmd)
			.EndSub()
			.BeginSub("qi")
			.WithDesc("Queue info")
			.HandleWith(onTickQueueCmd)
			.EndSub()
			.BeginSub("qc")
			.WithDesc("Clear tick queue")
			.HandleWith(onTickQueueClearCmd)
			.EndSub()
			.EndSub();
		base.OnBeginConfiguration();
	}

	private TextCommandResult onTickQueueClearCmd(TextCommandCallingArgs args)
	{
		queuedTicks = new ConcurrentQueue<object>();
		return TextCommandResult.Success("Queue is now cleared");
	}

	private TextCommandResult onTickQueueCmd(TextCommandCallingArgs args)
	{
		return TextCommandResult.Success(queuedTicks.Count + " elements in queue");
	}

	private TextCommandResult onTickBlockCmd(TextCommandCallingArgs args)
	{
		try
		{
			BlockPos blockPos = (args[0] as Vec3d).AsBlockPos;
			Block block = server.Api.World.BlockAccessor.GetBlock(blockPos);
			if (tryTickBlock(block, blockPos))
			{
				return TextCommandResult.Success(string.Concat("Accepted tick [block=", block.Code, "] at [", blockPos.ToString(), "]"));
			}
			return TextCommandResult.Success(string.Concat("Declined tick [block=", block.Code, "] at [", blockPos.ToString(), "]"));
		}
		catch (Exception e)
		{
			ServerMain.Logger.Error(e);
			return TextCommandResult.Success("An unexpected error occurred trying to tick block: " + e.Message);
		}
	}

	public override void OnBeginModsAndConfigReady()
	{
		IList<Block> serverBlocks = server.Blocks;
		for (int i = 0; i < serverBlocks.Count; i++)
		{
			serverBlocks[i]?.OnLoadedNative(server.api);
		}
		server.api.Logger.Debug("Block simulation loaded blocks");
		((List<Item>)server.Items).ForEach(delegate(Item item)
		{
			item?.OnLoadedNative(server.api);
		});
		server.api.Logger.Debug("Block simulation loaded items");
	}

	public override void OnPlayerJoin(ServerPlayer player)
	{
		lock (clientIdsLock)
		{
			clientIds.Add(player.ClientId);
		}
	}

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		lock (clientIdsLock)
		{
			clientIds.Remove(player.ClientId);
		}
	}

	private void HandleBlockEntityPacket(Packet_Client packet, ConnectedClient client)
	{
		Packet_BlockEntityPacket p = packet.BlockEntityPacket;
		server.WorldMap.GetBlockEntity(new BlockPos(p.X, p.Y, p.Z))?.OnReceivedClientPacket(client.Player, p.Packetid, p.Data);
	}

	internal void HandleBlockPlaceOrBreak(Packet_Client packet, ConnectedClient client)
	{
		Packet_ClientBlockPlaceOrBreak p = packet.BlockPlaceOrBreak;
		BlockSelection blockSel = new BlockSelection
		{
			DidOffset = (p.DidOffset > 0),
			Face = BlockFacing.ALLFACES[p.OnBlockFace],
			Position = new BlockPos(p.X, p.Y, p.Z),
			HitPosition = new Vec3d(CollectibleNet.DeserializeDouble(p.HitX), CollectibleNet.DeserializeDouble(p.HitY), CollectibleNet.DeserializeDouble(p.HitZ)),
			SelectionBoxIndex = p.SelectionBoxIndex
		};
		if (client.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator)
		{
			return;
		}
		EnumWorldAccessResponse resp;
		if ((resp = server.WorldMap.TestBlockAccess(client.Player, blockSel, EnumBlockAccessFlags.BuildOrBreak, out var claimant)) != 0)
		{
			RevertBlockInteractions(client.Player, blockSel.Position);
			string code = "noprivilege-buildbreak-" + resp.ToString().ToLowerInvariant();
			if (claimant == null)
			{
				claimant = "?";
			}
			else if (claimant.StartsWithOrdinal("custommessage-"))
			{
				code = "noprivilege-buildbreak-" + claimant.Substring("custommessage-".Length);
			}
			client.Player.SendIngameError(code, null, claimant);
			return;
		}
		if (p.Mode == 2)
		{
			if (server.BlockAccessor.GetChunkAtBlockPos(blockSel.Position) is WorldChunk c)
			{
				c.BreakDecor(server, blockSel.Position, blockSel.Face);
				c.MarkModified();
			}
			return;
		}
		Block potentiallyIce = server.WorldMap.RelaxedBlockAccess.GetBlock(blockSel.Position, 2);
		int oldBlockId = ((!potentiallyIce.SideSolid.Any) ? server.WorldMap.RelaxedBlockAccess.GetBlock(blockSel.Position).Id : potentiallyIce.BlockId);
		ItemStack placedBlockStack = client.Player.inventoryMgr.ActiveHotbarSlot?.Itemstack;
		if (!TryModifyBlockInWorld(client.Player, p))
		{
			RevertBlockInteractions(client.Player, blockSel.Position);
			return;
		}
		server.TriggerNeighbourBlocksUpdate(blockSel.Position);
		switch (p.Mode)
		{
		case 1:
			server.EventManager.TriggerDidPlaceBlock(client.Player, oldBlockId, blockSel, placedBlockStack);
			break;
		case 0:
			server.EventManager.TriggerDidBreakBlock(client.Player, oldBlockId, blockSel);
			break;
		}
	}

	internal void HandleBlockInteract(Packet_Client packet, ConnectedClient client)
	{
		ServerPlayer player = client.Player;
		Packet_ClientHandInteraction p = packet.HandInteraction;
		if (client.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator || p.UseType == 0 || p.MouseButton != 2)
		{
			return;
		}
		BlockPos pos = new BlockPos(p.X, p.Y, p.Z);
		BlockFacing facing = BlockFacing.ALLFACES[p.OnBlockFace];
		Vec3d hitPos = new Vec3d(CollectibleNet.DeserializeDoublePrecise(p.HitX), CollectibleNet.DeserializeDoublePrecise(p.HitY), CollectibleNet.DeserializeDoublePrecise(p.HitZ));
		BlockSelection blockSel = new BlockSelection
		{
			Position = pos,
			Face = facing,
			HitPosition = hitPos,
			SelectionBoxIndex = p.SelectionBoxIndex
		};
		EnumWorldAccessResponse resp;
		if ((resp = server.WorldMap.TestBlockAccess(client.Player, blockSel, EnumBlockAccessFlags.Use)) != 0)
		{
			RevertBlockInteractions(client.Player, blockSel.Position);
			string code = "noprivilege-use-" + resp.ToString().ToLowerInvariant();
			string param = server.WorldMap.GetBlockingLandClaimant(client.Player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak);
			client.Player.SendIngameError(code, null, param);
			return;
		}
		Block block = server.BlockAccessor.GetBlock(pos);
		EntityControls controls = player.Entity.Controls;
		float secondsPassed = (float)(server.ElapsedMilliseconds - controls.UsingBeginMS) / 1000f;
		switch ((EnumHandInteractNw)p.EnumHandInteract)
		{
		default:
			return;
		case EnumHandInteractNw.StartBlockUse:
			controls.HandUse = (block.OnBlockInteractStart(server, player, blockSel) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
			controls.UsingBeginMS = server.ElapsedMilliseconds;
			controls.UsingCount = 0;
			server.EventManager.TriggerDidUseBlock(client.Player, blockSel);
			return;
		case EnumHandInteractNw.CancelBlockUse:
		{
			while (controls.HandUse != 0 && controls.UsingCount < p.UsingCount)
			{
				callOnUsingBlock(player, block, blockSel, ref secondsPassed, callStop: false);
			}
			EnumItemUseCancelReason cancelReason = (EnumItemUseCancelReason)p.CancelReason;
			controls.HandUse = ((!block.OnBlockInteractCancel(secondsPassed, server, player, blockSel, cancelReason)) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
			return;
		}
		case EnumHandInteractNw.StopBlockUse:
			while (controls.HandUse != 0 && controls.UsingCount < p.UsingCount)
			{
				callOnUsingBlock(player, block, blockSel, ref secondsPassed);
			}
			if (controls.HandUse != 0)
			{
				controls.HandUse = EnumHandInteract.None;
				block.OnBlockInteractStop(secondsPassed, server, player, blockSel);
			}
			return;
		case EnumHandInteractNw.StepBlockUse:
			break;
		}
		while (controls.HandUse != 0 && controls.UsingCount < p.UsingCount)
		{
			callOnUsingBlock(player, block, blockSel, ref secondsPassed);
		}
	}

	private void callOnUsingBlock(ServerPlayer player, Block block, BlockSelection blockSel, ref float secondsPassed, bool callStop = true)
	{
		EntityControls controls = player.Entity.Controls;
		controls.HandUse = (block.OnBlockInteractStep(secondsPassed, server, player, blockSel) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
		controls.UsingCount++;
		if (callStop && controls.HandUse == EnumHandInteract.None)
		{
			block.OnBlockInteractStop(secondsPassed, server, player, blockSel);
		}
		secondsPassed += 0.02f;
	}

	private void RevertBlockInteractions(IServerPlayer targetPlayer, BlockPos pos)
	{
		RevertBlockInteraction2(targetPlayer, pos, sendPlayerData: false);
		RevertBlockInteraction2(targetPlayer, pos.AddCopy(BlockFacing.NORTH), sendPlayerData: false);
		RevertBlockInteraction2(targetPlayer, pos.AddCopy(BlockFacing.EAST), sendPlayerData: false);
		RevertBlockInteraction2(targetPlayer, pos.AddCopy(BlockFacing.SOUTH), sendPlayerData: false);
		RevertBlockInteraction2(targetPlayer, pos.AddCopy(BlockFacing.UP), sendPlayerData: false);
		RevertBlockInteraction2(targetPlayer, pos.AddCopy(BlockFacing.DOWN), sendPlayerData: false);
		server.SendOwnPlayerData(targetPlayer);
	}

	private void RevertBlockInteraction2(IServerPlayer targetPlayer, BlockPos pos, bool sendPlayerData = true)
	{
		server.SendSetBlock(targetPlayer, server.WorldMap.RawRelaxedBlockAccess.GetBlockId(pos), pos.X, pos.InternalY, pos.Z);
		BlockEntity be = server.WorldMap.RawRelaxedBlockAccess.GetBlockEntity(pos);
		if (be != null)
		{
			server.SendBlockEntity(targetPlayer, be);
		}
		if (sendPlayerData)
		{
			server.SendOwnPlayerData(targetPlayer);
		}
	}

	private bool TryModifyBlockInWorld(ServerPlayer player, Packet_ClientBlockPlaceOrBreak cmd)
	{
		Vec3d hitPosition = new Vec3d(CollectibleNet.DeserializeDouble(cmd.HitX), CollectibleNet.DeserializeDouble(cmd.HitY), CollectibleNet.DeserializeDouble(cmd.HitZ));
		Vec3d target = new Vec3d((double)cmd.X + hitPosition.X, (double)cmd.Y + hitPosition.Y, (double)cmd.Z + hitPosition.Z);
		Vec3d source = player.Entity.Pos.XYZ.Add(player.Entity.LocalEyePos);
		bool pickRangeAllowed = server.PlayerHasPrivilege(player.ClientId, Privilege.pickingrange);
		ItemSlot hotbarSlot = player.inventoryMgr.ActiveHotbarSlot;
		if (server.Config.AntiAbuse != 0 && !pickRangeAllowed && source.SquareDistanceTo(target) > (player.WorldData.PickingRange + 0.7f) * (player.WorldData.PickingRange + 0.7f))
		{
			ServerMain.Logger.Notification("Client {0} tried to use/place a block out of range", player.PlayerName);
			hotbarSlot.MarkDirty();
			return false;
		}
		BlockSelection blockSel = new BlockSelection
		{
			Face = BlockFacing.ALLFACES[cmd.OnBlockFace],
			Position = new BlockPos(cmd.X, cmd.Y, cmd.Z),
			HitPosition = hitPosition,
			SelectionBoxIndex = cmd.SelectionBoxIndex,
			DidOffset = (cmd.DidOffset > 0)
		};
		if (cmd.Mode == 1)
		{
			if (hotbarSlot == null || hotbarSlot.Itemstack == null)
			{
				ServerMain.Logger.Notification("Client {0} tried to place a block but rejected because the client hand is empty", player.PlayerName);
				return false;
			}
			if (hotbarSlot.Itemstack.Class != 0)
			{
				ServerMain.Logger.Notification("Client {0} tried to place a block but rejected because the itemstck in client hand is not a block", player.PlayerName);
				return false;
			}
			int newBlockID = hotbarSlot.Itemstack.Id;
			BlockPos pos = new BlockPos(cmd.X, cmd.Y, cmd.Z);
			Block newBlock = server.Blocks[newBlockID];
			if (newBlock == null)
			{
				ServerMain.Logger.Notification("Client {0} tried to place a block of id, which does not exist", player.PlayerName, newBlockID);
				return false;
			}
			Block oldBlock = server.WorldMap.RawRelaxedBlockAccess.GetBlock(cmd.X, cmd.Y, cmd.Z, (!newBlock.ForFluidsLayer) ? 1 : 2);
			if (!oldBlock.IsReplacableBy(newBlock))
			{
				JsonObject obj = newBlock.Attributes?["ignoreServerReplaceableTest"];
				if ((obj == null || !obj.Exists || !obj.AsBool()) && server.Blocks[newBlockID].decorBehaviorFlags == 0)
				{
					ServerMain.Logger.Notification("Client {0} tried to place a block but rejected because the client tried to overwrite an existing, non-replacable block {1}, id {2}", player.PlayerName, oldBlock.Code, oldBlock.Id);
					return false;
				}
			}
			if (IsAnyPlayerInBlock(pos, newBlock, player))
			{
				ServerMain.Logger.Notification("Client {0} tried to place a block but rejected because it would intersect with another player", player.PlayerName);
				return false;
			}
			string failureCode = "";
			if (!newBlock.TryPlaceBlock(server, player, hotbarSlot.Itemstack, blockSel, ref failureCode))
			{
				ServerMain.Logger.Notification("Client {0} tried to place a block but rejected because OnPlaceBlock returns false. Failure code {1}", player.PlayerName, failureCode);
				return false;
			}
			if (server.WorldMap.GetChunk(blockSel.Position) is ServerChunk serverchunk)
			{
				serverchunk.BlocksPlaced++;
				serverchunk.DirtyForSaving = true;
			}
			if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				hotbarSlot.Itemstack.StackSize--;
				if (hotbarSlot.Itemstack.StackSize <= 0)
				{
					hotbarSlot.Itemstack = null;
					server.BroadcastHotbarSlot(player);
				}
				hotbarSlot.MarkDirty();
			}
		}
		else
		{
			Block potentiallyIce = server.WorldMap.RelaxedBlockAccess.GetBlock(cmd.X, cmd.Y, cmd.Z, 2);
			int blockid = ((!potentiallyIce.SideSolid.Any) ? server.WorldMap.RelaxedBlockAccess.GetBlock(cmd.X, cmd.Y, cmd.Z).Id : potentiallyIce.BlockId);
			BlockPos pos2 = new BlockPos(cmd.X, cmd.Y, cmd.Z);
			Block block = (blockSel.Block = server.Blocks[blockid]);
			IItemStack heldItemstack = hotbarSlot.Itemstack;
			int miningTier = 0;
			if (heldItemstack != null)
			{
				miningTier = heldItemstack.Collectible.ToolTier;
			}
			if (player.WorldData.CurrentGameMode != EnumGameMode.Creative && block.RequiredMiningTier > miningTier)
			{
				ServerMain.Logger.Notification("Client {0} tried to break a block but rejected because his tools mining tier is too low", player.PlayerName);
				return false;
			}
			float dropMul = 1f;
			EnumHandling handling = EnumHandling.PassThrough;
			server.EventManager.TriggerBreakBlock(player, blockSel, ref dropMul, ref handling);
			if (handling == EnumHandling.PassThrough)
			{
				if (heldItemstack != null)
				{
					heldItemstack.Collectible.OnBlockBrokenWith(server, player.Entity, hotbarSlot, blockSel, dropMul);
				}
				else
				{
					block.OnBlockBroken(server, pos2, player, dropMul);
				}
				if (server.WorldMap.GetChunk(blockSel.Position) is ServerChunk serverchunk2)
				{
					serverchunk2.BlocksRemoved++;
					serverchunk2.DirtyForSaving = true;
				}
			}
			else
			{
				server.WorldMap.MarkBlockModified(blockSel.Position);
				server.WorldMap.MarkBlockEntityDirty(blockSel.Position);
			}
			if (hotbarSlot.Itemstack == null && heldItemstack != null)
			{
				server.BroadcastHotbarSlot(player);
			}
		}
		player.client.IsInventoryDirty = true;
		return true;
	}

	internal bool IsAnyPlayerInBlock(BlockPos pos, Block block, IPlayer ignorePlayer)
	{
		Cuboidf[] collisionboxes = block.GetCollisionBoxes(server.BlockAccessor, pos);
		if (collisionboxes == null)
		{
			return false;
		}
		IPlayer[] allOnlinePlayers = server.AllOnlinePlayers;
		foreach (IPlayer player in allOnlinePlayers)
		{
			if (player.Entity == null || player.ClientId == ignorePlayer?.ClientId)
			{
				continue;
			}
			for (int i = 0; i < collisionboxes.Length; i++)
			{
				if (CollisionTester.AabbIntersect(collisionboxes[i], pos.X, pos.Y, pos.Z, player.Entity.SelectionBox, player.Entity.Pos.XYZ))
				{
					return true;
				}
			}
		}
		return false;
	}

	public override int GetUpdateInterval()
	{
		return server.Config.BlockTickInterval;
	}

	private void UpdateEvery100ms(float t1)
	{
		HandleDirtyAndUpdatedBlocks();
		SendDirtyBlockEntities();
	}

	private void HandleDirtyAndUpdatedBlocks()
	{
		int i = 0;
		while (server.UpdatedBlocks.Count > 0 && i++ < 500)
		{
			BlockPos pos4 = server.UpdatedBlocks.Dequeue();
			server.TriggerNeighbourBlocksUpdate(pos4);
		}
		if (server.DirtyBlocks.Count > 0)
		{
			Vec4i vec;
			while (server.DirtyBlocks.Count > 0 && server.DirtyBlocks.TryDequeue(out vec))
			{
				server.SendSetBlock(server.BlockAccessor.GetBlock(vec.X, vec.Y, vec.Z).Id, vec.X, vec.Y, vec.Z, vec.W, exchangeOnly: true);
			}
		}
		if (server.ModifiedBlocks.Count > 0)
		{
			List<BlockPos> dirtyBlocks = new List<BlockPos>();
			BlockPos pos3;
			while (server.ModifiedBlocks.Count > 0 && server.ModifiedBlocks.TryDequeue(out pos3))
			{
				server.WorldMap.RelaxedBlockAccess.GetBlock(pos3).OnNeighbourBlockChange(server, pos3, pos3);
				dirtyBlocks.Add(pos3);
			}
			server.SendSetBlocksPacket(dirtyBlocks, 47);
		}
		if (server.ModifiedBlocksNoRelight.Count > 0)
		{
			List<BlockPos> dirtyBlocksNoRelight = new List<BlockPos>();
			BlockPos pos2;
			while (server.ModifiedBlocksNoRelight.Count > 0 && server.ModifiedBlocksNoRelight.TryDequeue(out pos2))
			{
				server.WorldMap.RelaxedBlockAccess.GetBlock(pos2).OnNeighbourBlockChange(server, pos2, pos2);
				dirtyBlocksNoRelight.Add(pos2);
			}
			server.SendSetBlocksPacket(dirtyBlocksNoRelight, 63);
		}
		if (server.ModifiedBlocksMinimal.Count > 0)
		{
			server.SendSetBlocksPacket(server.ModifiedBlocksMinimal, 70);
			server.ModifiedBlocksMinimal.Clear();
		}
		if (server.ModifiedDecors.Count > 0)
		{
			List<BlockPos> decorPositions = new List<BlockPos>();
			BlockPos pos;
			while (server.ModifiedDecors.Count > 0 && server.ModifiedDecors.TryDequeue(out pos))
			{
				decorPositions.Add(pos);
			}
			server.SendSetDecorsPackets(decorPositions);
		}
	}

	private void SendDirtyBlockEntities()
	{
		if (server.DirtyBlockEntities.Count == 0)
		{
			return;
		}
		blockEntitiesPacked.Clear();
		noblockEntities.Clear();
		positionsDone.Clear();
		ConcurrentQueue<BlockPos> DirtyBlockEntities = server.DirtyBlockEntities;
		if (DirtyBlockEntities.Count > 0)
		{
			using FastMemoryStream reusableStream = new FastMemoryStream();
			BlockPos pos;
			while (DirtyBlockEntities.Count > 0 && server.DirtyBlockEntities.TryDequeue(out pos))
			{
				if (positionsDone.Add(pos))
				{
					BlockEntity blockEntity = server.WorldMap.GetBlockEntity(pos);
					if (blockEntity != null)
					{
						blockEntitiesPacked.Add(BlockEntityToPacket(blockEntity, reusableStream));
					}
					else
					{
						noblockEntities.Add(pos);
					}
				}
			}
		}
		if (blockEntitiesPacked.Count <= 0 && noblockEntities.Count <= 0)
		{
			return;
		}
		foreach (ConnectedClient client in server.Clients.Values)
		{
			if (client.State == EnumClientState.Offline)
			{
				continue;
			}
			playerBlockEntitiesPacked.Clear();
			foreach (Packet_BlockEntity be in blockEntitiesPacked)
			{
				long index3d = server.WorldMap.ChunkIndex3D(be.PosX / 32, be.PosY / 32, be.PosZ / 32);
				if (client.DidSendChunk(index3d))
				{
					playerBlockEntitiesPacked.Add(be);
				}
			}
			if (playerBlockEntitiesPacked.Count + noblockEntities.Count > 0)
			{
				SendBlockEntitiesPacket(client, playerBlockEntitiesPacked, noblockEntities);
			}
		}
	}

	public override void OnServerTick(float dt)
	{
		if (server.RunPhase != EnumServerRunPhase.RunGame)
		{
			return;
		}
		int blockTickCount = 0;
		while (!queuedTicks.IsEmpty && blockTickCount < server.Config.MaxMainThreadBlockTicks)
		{
			if (!queuedTicks.TryDequeue(out var tickItem))
			{
				continue;
			}
			Block block = null;
			try
			{
				if (tickItem is FluidBlockPos)
				{
					BlockPos pos = (BlockPos)tickItem;
					block = server.api.World.BlockAccessor.GetBlock(pos, 2);
					block.OnServerGameTick(server.api.World, pos);
				}
				else if (tickItem is BlockPos)
				{
					BlockPos pos2 = (BlockPos)tickItem;
					block = server.api.World.BlockAccessor.GetBlock(pos2);
					block.OnServerGameTick(server.api.World, pos2);
				}
				else
				{
					BlockPosWithExtraObject tickContext = (BlockPosWithExtraObject)tickItem;
					block = server.api.World.BlockAccessor.GetBlock(tickContext.pos);
					block.OnServerGameTick(server.api.World, tickContext.pos, tickContext.extra);
				}
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error("Exception thrown in block.OnServerGameTick() for block code '{0}':", block?.Code);
				ServerMain.Logger.Error(e);
			}
			blockTickCount++;
		}
	}

	public override void OnSeparateThreadTick()
	{
		if (server.RunPhase != EnumServerRunPhase.RunGame)
		{
			return;
		}
		chunksToBeTicked.Clear();
		int range = server.Config.BlockTickChunkRange;
		lock (clientIdsLock)
		{
			foreach (int clientid in clientIds)
			{
				if (!server.Clients.TryGetValue(clientid, out var client) || client.State != EnumClientState.Playing)
				{
					continue;
				}
				ChunkPos playerChunkPos = server.WorldMap.ChunkPosFromChunkIndex3D(client.Entityplayer.InChunkIndex3d);
				for (int dx = -range; dx <= range; dx++)
				{
					for (int dy = -range; dy <= range; dy++)
					{
						for (int dz = -range; dz <= range; dz++)
						{
							int cx = playerChunkPos.X + dx;
							int cy = playerChunkPos.Y + dy;
							int cz = playerChunkPos.Z + dz;
							if (server.WorldMap.IsValidChunkPos(cx, cy, cz))
							{
								long index3d = server.WorldMap.ChunkIndex3D(cx, cy, cz, playerChunkPos.Dimension);
								ServerChunk chunk = server.WorldMap.GetServerChunk(index3d);
								if (chunk != null)
								{
									chunksToBeTicked[index3d] = chunk;
									chunk.MarkFresh();
									chunk.MapChunk.MarkFresh();
								}
							}
						}
					}
				}
			}
		}
		foreach (KeyValuePair<long, ServerChunk> val in chunksToBeTicked)
		{
			try
			{
				tickChunk(val.Key, val.Value);
			}
			catch (Exception e)
			{
				ServerMain.Logger.Warning("Exception thrown when trying to tick a chunk.");
				ServerMain.Logger.Warning(e);
			}
		}
	}

	private void tickChunk(long index3d, ServerChunk chunk)
	{
		ChunkPos cpos = server.WorldMap.ChunkPosFromChunkIndex3D(index3d);
		int baseX = 32 * cpos.X;
		int baseY = 32 * cpos.Y;
		int baseZ = 32 * cpos.Z;
		tmpPos.SetDimension(cpos.Dimension);
		tmpLiquidPos.SetDimension(cpos.Dimension);
		chunk.Unpack();
		float samples = (int)((float)server.Config.RandomBlockTicksPerChunk * server.Calendar.SpeedOfTime / 60f);
		int cnt = (int)samples + ((server.rand.Value.NextDouble() < (double)(samples - (float)(int)samples)) ? 1 : 0);
		for (int i = 0; i < cnt; i++)
		{
			int randX = rand.Next(32);
			int randZ = rand.Next(32);
			int randY = rand.Next(32);
			int cIndex = server.WorldMap.ChunkSizedIndex3D(randX, randY, randZ);
			int blockId = chunk.Data.GetFluid(cIndex);
			if (blockId != 0)
			{
				tryTickBlock(server.WorldMap.Blocks[blockId], tmpLiquidPos.Set(baseX + randX, baseY + randY, baseZ + randZ));
				continue;
			}
			blockId = chunk.Data[cIndex];
			if (blockId != 0)
			{
				tryTickBlock(server.WorldMap.Blocks[blockId], tmpPos.Set(baseX + randX, baseY + randY, baseZ + randZ));
			}
		}
	}

	private bool tryTickBlock(Block block, BlockPos atPos)
	{
		if (!block.ShouldReceiveServerGameTicks(server.api.World, atPos, rand, out var extra))
		{
			return false;
		}
		if (extra == null)
		{
			queuedTicks.Enqueue(atPos.Copy());
		}
		else
		{
			queuedTicks.Enqueue(new BlockPosWithExtraObject(atPos.Copy(), extra));
		}
		return true;
	}

	private Packet_BlockEntity BlockEntityToPacket(BlockEntity blockEntity, FastMemoryStream ms)
	{
		ms.Reset();
		BinaryWriter writer = new BinaryWriter(ms);
		TreeAttribute tree = new TreeAttribute();
		blockEntity.ToTreeAttributes(tree);
		tree.ToBytes(writer);
		string classname = ServerMain.ClassRegistry.blockEntityTypeToClassnameMapping[blockEntity.GetType()];
		byte[] data = ms.ToArray();
		Packet_BlockEntity packet_BlockEntity = new Packet_BlockEntity();
		packet_BlockEntity.Classname = classname;
		packet_BlockEntity.PosX = blockEntity.Pos.X;
		packet_BlockEntity.PosY = blockEntity.Pos.InternalY;
		packet_BlockEntity.PosZ = blockEntity.Pos.Z;
		packet_BlockEntity.SetData(data);
		if (server.doNetBenchmark)
		{
			int now = 0;
			server.packetBenchmarkBlockEntitiesBytes.TryGetValue(classname, out now);
			server.packetBenchmarkBlockEntitiesBytes[classname] = now + data.Length;
		}
		return packet_BlockEntity;
	}

	private void SendBlockEntitiesPacket(ConnectedClient client, List<Packet_BlockEntity> blockEntitiesPacked, List<BlockPos> noBlockEntities)
	{
		Packet_BlockEntity[] blockentitiespackets = new Packet_BlockEntity[blockEntitiesPacked.Count + noBlockEntities.Count];
		int i = 0;
		foreach (Packet_BlockEntity packed in blockEntitiesPacked)
		{
			blockentitiespackets[i++] = packed;
		}
		for (int j = 0; j < noBlockEntities.Count; j++)
		{
			BlockPos pos = noBlockEntities[j];
			blockentitiespackets[i++] = new Packet_BlockEntity
			{
				Classname = null,
				Data = null,
				PosX = pos.X,
				PosY = pos.InternalY,
				PosZ = pos.Z
			};
		}
		Packet_BlockEntities packet = new Packet_BlockEntities();
		packet.SetBlockEntitites(blockentitiespackets);
		server.SendPacket(client.Id, new Packet_Server
		{
			Id = 48,
			BlockEntities = packet
		});
	}
}
