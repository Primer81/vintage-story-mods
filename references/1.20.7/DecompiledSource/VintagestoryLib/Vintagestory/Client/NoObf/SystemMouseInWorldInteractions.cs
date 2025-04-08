using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class SystemMouseInWorldInteractions : ClientSystem
{
	internal long lastbuildMilliseconds;

	internal long lastbreakMilliseconds;

	internal long lastbreakNotifyMilliseconds;

	private bool isSurvivalBreaking;

	private int survivalBreakingCounter;

	private BlockDamage curBlockDmg;

	public bool prevMouseLeft;

	public bool prevMouseRight;

	private float accum;

	private float stepPacketAccum;

	public override string Name => "miw";

	public SystemMouseInWorldInteractions(ClientMain game)
		: base(game)
	{
		game.RegisterGameTickListener(OnEverySecond, 1000);
		game.RegisterGameTickListener(OnGameTick, 20);
		game.eventManager.RegisterRenderer(OnRenderOpaque, EnumRenderStage.Opaque, Name + "-op", 0.9);
		game.eventManager.RegisterRenderer(OnRenderOit, EnumRenderStage.OIT, Name + "-oit", 0.9);
		game.eventManager.RegisterRenderer(OnRenderOrtho, EnumRenderStage.Ortho, Name + "-2d", 0.9);
		game.eventManager.RegisterRenderer(OnFinalizeFrame, EnumRenderStage.Done, Name + "-done", 0.9);
	}

	private void OnEverySecond(float dt)
	{
		List<BlockPos> removeQueue = new List<BlockPos>();
		foreach (KeyValuePair<BlockPos, BlockDamage> var in game.damagedBlocks)
		{
			BlockDamage damagedBlock = var.Value;
			if (game.ElapsedMilliseconds - damagedBlock.LastBreakEllapsedMs > 1000)
			{
				damagedBlock.LastBreakEllapsedMs = game.ElapsedMilliseconds;
				damagedBlock.RemainingResistance += 0.1f * damagedBlock.Block.GetResistance(game.BlockAccessor, damagedBlock.Position);
				game.eventManager?.TriggerBlockUnbreaking(damagedBlock);
				if (damagedBlock.RemainingResistance >= damagedBlock.Block.GetResistance(game.BlockAccessor, damagedBlock.Position))
				{
					removeQueue.Add(var.Key);
				}
			}
		}
		foreach (BlockPos pos in removeQueue)
		{
			game.damagedBlocks.Remove(pos);
		}
		ScreenManager.FrameProfiler.Mark("miw-1s");
	}

	public void OnFinalizeFrame(float dt)
	{
		if (game.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
		{
			ScreenManager.FrameProfiler.Mark("finaframe-beg");
			if (game.MouseGrabbed || game.mouseWorldInteractAnyway || game.player.worlddata.AreaSelectionMode)
			{
				UpdatePicking(dt);
			}
			prevMouseLeft = game.InWorldMouseState.Left;
			prevMouseRight = game.InWorldMouseState.Right;
			ScreenManager.FrameProfiler.Mark("finaframe-miw");
		}
	}

	public override void OnMouseUp(MouseEvent args)
	{
		if (game.Player.WorldData.CurrentGameMode != EnumGameMode.Spectator)
		{
			if (game.player.worlddata.CurrentGameMode == EnumGameMode.Creative)
			{
				lastbuildMilliseconds = 0L;
			}
			StopBlockBreakSurvival();
		}
	}

	private void OnGameTick(float dt)
	{
		ItemStack stack = game.player.inventoryMgr?.ActiveHotbarSlot?.Itemstack;
		if (game.EntityPlayer.Controls.HandUse == EnumHandInteract.None)
		{
			stack?.Collectible.OnHeldIdle(game.player.inventoryMgr.ActiveHotbarSlot, game.EntityPlayer);
		}
		if (!game.EntityPlayer.LeftHandItemSlot.Empty)
		{
			game.EntityPlayer.LeftHandItemSlot.Itemstack.Collectible.OnHeldIdle(game.EntityPlayer.LeftHandItemSlot, game.EntityPlayer);
		}
		if (game.Player.WorldData.CurrentGameMode == EnumGameMode.Spectator || game.EntityPlayer.Controls.HandUse == EnumHandInteract.None)
		{
			return;
		}
		if (!game.MouseGrabbed && !game.mouseWorldInteractAnyway)
		{
			if (game.EntityPlayer.Controls.HandUsingBlockSel != null)
			{
				ClientPlayer player = game.player;
				EntityPlayer entityPlr = game.EntityPlayer;
				Block block = game.BlockAccessor.GetBlock(entityPlr.Controls.HandUsingBlockSel.Position);
				EnumHandInteract beforeUseType = entityPlr.Controls.HandUse;
				if (block != null)
				{
					float secondsPassed = (float)(game.ElapsedMilliseconds - entityPlr.Controls.UsingBeginMS) / 1000f;
					entityPlr.Controls.HandUse = ((!block.OnBlockInteractCancel(secondsPassed, game, player, entityPlr.Controls.HandUsingBlockSel, EnumItemUseCancelReason.ReleasedMouse)) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
					game.SendHandInteraction(2, game.BlockSelection, game.EntitySelection, beforeUseType, EnumHandInteractNw.CancelBlockUse, firstEvent: false);
				}
			}
		}
		else
		{
			accum += dt;
			if ((double)accum > 0.25)
			{
				accum = 0f;
				game.SendHandInteraction(2, game.BlockSelection, game.EntitySelection, game.EntityPlayer.Controls.HandUse, EnumHandInteractNw.StepHeldItemUse, firstEvent: false);
			}
			HandleHandInteraction(dt);
			ScreenManager.FrameProfiler.Mark("miw-handlehandinteraction");
		}
	}

	private void OnRenderOrtho(float dt)
	{
		if (game.player.inventoryMgr.ActiveHotbarSlot.Itemstack != null)
		{
			game.player.inventoryMgr.ActiveHotbarSlot.Itemstack.Collectible.OnHeldRenderOrtho(game.player.inventoryMgr.ActiveHotbarSlot, game.player);
		}
	}

	private void OnRenderOit(float dt)
	{
		if (game.player.inventoryMgr.ActiveHotbarSlot.Itemstack != null)
		{
			game.player.inventoryMgr.ActiveHotbarSlot.Itemstack.Collectible.OnHeldRenderOit(game.player.inventoryMgr.ActiveHotbarSlot, game.player);
		}
	}

	private void OnRenderOpaque(float dt)
	{
		if (game.player.inventoryMgr.ActiveHotbarSlot.Itemstack != null)
		{
			game.player.inventoryMgr.ActiveHotbarSlot.Itemstack.Collectible.OnHeldRenderOpaque(game.player.inventoryMgr.ActiveHotbarSlot, game.player);
		}
	}

	internal void UpdatePicking(float dt)
	{
		UpdateCurrentSelection();
		if (game.MouseGrabbed || game.mouseWorldInteractAnyway)
		{
			if (game.EntityPlayer.Controls.HandUse == EnumHandInteract.None)
			{
				if ((game.InWorldMouseState.Left ? 1 : 0) + (game.InWorldMouseState.Middle ? 1 : 0) + (game.InWorldMouseState.Right ? 1 : 0) > 1)
				{
					ResetMouseInteractions();
				}
				else if (game.BlockSelection == null)
				{
					HandleMouseInteractionsNoBlockSelected(dt);
				}
				else
				{
					HandleMouseInteractionsBlockSelected(dt);
				}
			}
		}
		else
		{
			ResetMouseInteractions();
		}
	}

	private void HandleHandInteraction(float dt)
	{
		ClientPlayer player = game.player;
		EntityPlayer entityPlr = game.EntityPlayer;
		ItemSlot slot = game.player.inventoryMgr.ActiveHotbarSlot;
		float secondsPassed = (float)(game.ElapsedMilliseconds - entityPlr.Controls.UsingBeginMS) / 1000f;
		bool success = false;
		if (entityPlr.Controls.HandUse == EnumHandInteract.BlockInteract)
		{
			Block block = game.BlockAccessor.GetBlock(entityPlr.Controls.HandUsingBlockSel.Position);
			if (game.BlockSelection?.Position == null || !game.BlockSelection.Position.Equals(entityPlr.Controls.HandUsingBlockSel.Position))
			{
				entityPlr.Controls.HandUse = ((!block.OnBlockInteractCancel(secondsPassed, game, player, entityPlr.Controls.HandUsingBlockSel, EnumItemUseCancelReason.MovedAway)) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
				game.SendHandInteraction(2, entityPlr.Controls.HandUsingBlockSel, null, EnumHandInteract.BlockInteract, EnumHandInteractNw.CancelBlockUse, firstEvent: false, EnumItemUseCancelReason.MovedAway);
				return;
			}
			EnumHandInteract beforeUseType = entityPlr.Controls.HandUse;
			if (!game.InWorldMouseState.Right)
			{
				entityPlr.Controls.HandUse = ((!block.OnBlockInteractCancel(secondsPassed, game, player, game.BlockSelection, EnumItemUseCancelReason.ReleasedMouse)) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
			}
			if (entityPlr.Controls.HandUse != 0)
			{
				entityPlr.Controls.HandUse = (block.OnBlockInteractStep(secondsPassed, game, player, game.BlockSelection) ? EnumHandInteract.BlockInteract : EnumHandInteract.None);
				entityPlr.Controls.UsingCount++;
				stepPacketAccum += dt;
				if ((double)stepPacketAccum > 0.15)
				{
					game.SendHandInteraction(2, entityPlr.Controls.HandUsingBlockSel, null, EnumHandInteract.BlockInteract, EnumHandInteractNw.StepBlockUse, firstEvent: false);
					stepPacketAccum = 0f;
				}
			}
			if (entityPlr.Controls.HandUse == EnumHandInteract.None)
			{
				block.OnBlockInteractStop(secondsPassed, game, player, game.BlockSelection);
				success = true;
			}
			if (entityPlr.Controls.HandUse == EnumHandInteract.None)
			{
				game.SendHandInteraction(2, game.BlockSelection, game.EntitySelection, beforeUseType, success ? EnumHandInteractNw.StopBlockUse : EnumHandInteractNw.CancelBlockUse, firstEvent: false);
			}
		}
		else if (slot?.Itemstack == null)
		{
			entityPlr.Controls.HandUse = EnumHandInteract.None;
		}
		else
		{
			EnumHandInteract beforeUseType2 = entityPlr.Controls.HandUse;
			if ((!game.InWorldMouseState.Right && beforeUseType2 == EnumHandInteract.HeldItemInteract) || (!game.InWorldMouseState.Left && beforeUseType2 == EnumHandInteract.HeldItemAttack))
			{
				entityPlr.Controls.HandUse = slot.Itemstack.Collectible.OnHeldUseCancel(secondsPassed, slot, game.EntityPlayer, game.BlockSelection, game.EntitySelection, EnumItemUseCancelReason.ReleasedMouse);
			}
			if (entityPlr.Controls.HandUse != 0)
			{
				entityPlr.Controls.HandUse = slot.Itemstack.Collectible.OnHeldUseStep(secondsPassed, slot, game.EntityPlayer, game.BlockSelection, game.EntitySelection);
				entityPlr.Controls.UsingCount++;
			}
			if (entityPlr.Controls.HandUse == EnumHandInteract.None)
			{
				slot.Itemstack?.Collectible.OnHeldUseStop(secondsPassed, slot, game.EntityPlayer, game.BlockSelection, game.EntitySelection, beforeUseType2);
				success = true;
			}
			if (slot.StackSize <= 0)
			{
				slot.Itemstack = null;
				slot.MarkDirty();
			}
			if (entityPlr.Controls.HandUse == EnumHandInteract.None)
			{
				game.SendHandInteraction(2, game.BlockSelection, game.EntitySelection, beforeUseType2, (!success) ? EnumHandInteractNw.CancelHeldItemUse : EnumHandInteractNw.StopHeldItemUse, firstEvent: false);
			}
		}
	}

	private void UpdateCurrentSelection()
	{
		if (game.EntityPlayer == null)
		{
			return;
		}
		bool renderMeta = ClientSettings.RenderMetaBlocks;
		BlockFilter bfilter = (BlockPos pos, Block block) => block == null || renderMeta || block.RenderPass != EnumChunkRenderPass.Meta || (block.GetInterface<IMetaBlock>(game.api.World, pos)?.IsSelectable(pos) ?? false);
		EntityFilter efilter = (Entity e) => e.IsInteractable && e.EntityId != game.EntityPlayer.EntityId;
		bool prevLiqSel = game.LiquidSelectable;
		if (!game.InWorldMouseState.Left && game.InWorldMouseState.Right && game.player?.inventoryMgr?.ActiveHotbarSlot?.Itemstack?.Collectible != null && game.player.inventoryMgr.ActiveHotbarSlot.Itemstack.Collectible.LiquidSelectable)
		{
			game.forceLiquidSelectable = true;
		}
		game.EntityPlayer.PreviousBlockSelection = game.EntityPlayer.BlockSelection?.Position.Copy();
		if (!game.MouseGrabbed)
		{
			Ray ray = game.pickingRayUtil.GetPickingRayByMouseCoordinates(game);
			if (ray == null)
			{
				game.forceLiquidSelectable = prevLiqSel;
				return;
			}
			game.RayTraceForSelection(ray, ref game.EntityPlayer.BlockSelection, ref game.EntityPlayer.EntitySelection, bfilter, efilter);
		}
		else
		{
			game.RayTraceForSelection(game.player, ref game.EntityPlayer.BlockSelection, ref game.EntityPlayer.EntitySelection, bfilter, efilter);
		}
		game.forceLiquidSelectable = prevLiqSel;
		if (game.EntityPlayer.BlockSelection != null)
		{
			bool firstTick = game.EntityPlayer.PreviousBlockSelection == null || game.EntityPlayer.BlockSelection.Position != game.EntityPlayer.PreviousBlockSelection;
			game.EntityPlayer.BlockSelection.Block.OnBeingLookedAt(game.player, game.EntityPlayer.BlockSelection, firstTick);
		}
	}

	private void ResetMouseInteractions()
	{
		isSurvivalBreaking = false;
		survivalBreakingCounter = 0;
	}

	private void HandleMouseInteractionsNoBlockSelected(float dt)
	{
		StopBlockBreakSurvival();
		if (!((float)(game.InWorldEllapsedMs - lastbuildMilliseconds) / 1000f >= BuildRepeatDelay(game)))
		{
			return;
		}
		if (game.InWorldMouseState.Left || game.InWorldMouseState.Right || game.InWorldMouseState.Middle)
		{
			lastbuildMilliseconds = game.InWorldEllapsedMs;
		}
		else
		{
			lastbuildMilliseconds = 0L;
		}
		if (game.InWorldMouseState.Left)
		{
			EnumHandling inworldhandling2 = EnumHandling.PassThrough;
			game.api.inputapi.TriggerInWorldAction(EnumEntityAction.InWorldLeftMouseDown, on: true, ref inworldhandling2);
			if (inworldhandling2 != 0)
			{
				return;
			}
			EnumHandHandling handling = EnumHandHandling.NotHandled;
			TryBeginAttackWithActiveSlotItem(null, game.EntitySelection, ref handling);
			if (handling != EnumHandHandling.PreventDefaultAnimation && handling != EnumHandHandling.PreventDefault)
			{
				StartAttackAnimation();
			}
			if (game.EntitySelection != null && handling != EnumHandHandling.PreventDefaultAction && handling != EnumHandHandling.PreventDefault)
			{
				game.TryAttackEntity(game.EntitySelection);
			}
		}
		if (game.InWorldMouseState.Right)
		{
			EnumHandling inworldhandling = EnumHandling.PassThrough;
			game.api.inputapi.TriggerInWorldAction(EnumEntityAction.InWorldRightMouseDown, on: true, ref inworldhandling);
			if (inworldhandling == EnumHandling.PassThrough && !TryBeginUseActiveSlotItem(null, game.EntitySelection) && game.EntitySelection != null)
			{
				EntitySelection esel = game.EntitySelection;
				game.EntitySelection.Entity.OnInteract(game.EntityPlayer, game.player.inventoryMgr.ActiveHotbarSlot, esel.HitPosition, EnumInteractMode.Interact);
				game.SendPacketClient(ClientPackets.EntityInteraction(1, esel.Entity.EntityId, esel.Face, esel.HitPosition, esel.SelectionBoxIndex));
			}
		}
	}

	private void HandleMouseInteractionsBlockSelected(float dt)
	{
		BlockSelection blockSelection = game.BlockSelection;
		Block selectedBlock = blockSelection.Block ?? game.WorldMap.RelaxedBlockAccess.GetBlock(blockSelection.Position);
		ItemSlot selectedHotbarSlot = game.player.inventoryMgr.ActiveHotbarSlot;
		if ((float)(game.InWorldEllapsedMs - lastbuildMilliseconds) / 1000f >= BuildRepeatDelay(game))
		{
			if (game.InWorldMouseState.Left || game.InWorldMouseState.Right || game.InWorldMouseState.Middle)
			{
				lastbuildMilliseconds = game.InWorldEllapsedMs;
			}
			else
			{
				lastbuildMilliseconds = 0L;
				ResetMouseInteractions();
			}
			if (game.InWorldMouseState.Left)
			{
				EnumHandling handling2 = EnumHandling.PassThrough;
				game.api.inputapi.TriggerInWorldAction(EnumEntityAction.InWorldLeftMouseDown, on: true, ref handling2);
				if (handling2 != 0)
				{
					return;
				}
				EnumHandHandling handled = EnumHandHandling.NotHandled;
				TryBeginUseActiveSlotItem(blockSelection, null, EnumHandInteract.HeldItemAttack, ref handled);
				if (handled != EnumHandHandling.PreventDefaultAnimation && handled != EnumHandHandling.PreventDefault)
				{
					StartAttackAnimation();
				}
				if (handled == EnumHandHandling.PreventDefaultAction || handled == EnumHandHandling.PreventDefault)
				{
					isSurvivalBreaking = false;
					survivalBreakingCounter = 0;
				}
				else if (game.player.worlddata.CurrentGameMode == EnumGameMode.Creative)
				{
					game.damagedBlocks.TryGetValue(blockSelection.Position, out var blockDamage);
					if (blockDamage == null)
					{
						blockDamage = new BlockDamage
						{
							Block = selectedBlock,
							Facing = blockSelection.Face,
							Position = blockSelection.Position,
							ByPlayer = game.player
						};
					}
					game.damagedBlocks.Remove(blockSelection.Position);
					game.eventManager?.TriggerBlockBroken(blockDamage);
					game.OnPlayerTryDestroyBlock(blockSelection);
					UpdateCurrentSelection();
					game.PlaySound(selectedBlock.GetSounds(game.BlockAccessor, blockSelection)?.GetBreakSound(game.player), randomizePitch: true);
				}
				else
				{
					InitBlockBreakSurvival(blockSelection, dt);
				}
			}
			if (game.InWorldMouseState.Right)
			{
				EnumHandling handling = EnumHandling.PassThrough;
				game.api.inputapi.TriggerInWorldAction(EnumEntityAction.InWorldRightMouseDown, on: true, ref handling);
				if (handling != 0)
				{
					return;
				}
				bool haveHeldItemstack = selectedHotbarSlot.Itemstack != null;
				bool canPlaceBlock = haveHeldItemstack && selectedHotbarSlot.Itemstack.Class == EnumItemClass.Block && (game.player.worlddata.CurrentGameMode == EnumGameMode.Survival || game.player.worlddata.CurrentGameMode == EnumGameMode.Creative);
				bool canInteractWithBlock = game.player.worlddata.CurrentGameMode != EnumGameMode.Spectator;
				if ((canInteractWithBlock && !game.Player.Entity.Controls.ShiftKey && TryBeginUseBlock(selectedBlock, blockSelection)) || (haveHeldItemstack && (!game.Player.Entity.Controls.ShiftKey || selectedHotbarSlot.Itemstack.Collectible.HeldPriorityInteract) && TryBeginUseActiveSlotItem(blockSelection, null)))
				{
					return;
				}
				string failureCode = null;
				if ((canInteractWithBlock && game.Player.Entity.Controls.ShiftKey && selectedBlock.PlacedPriorityInteract && TryBeginUseBlock(selectedBlock, blockSelection)) || (canPlaceBlock && OnBlockBuild(blockSelection, selectedBlock, ref failureCode)) || (haveHeldItemstack && game.Player.Entity.Controls.ShiftKey && TryBeginUseActiveSlotItem(blockSelection, null)) || (canInteractWithBlock && game.Player.Entity.Controls.ShiftKey && TryBeginUseBlock(selectedBlock, blockSelection)))
				{
					return;
				}
				if (failureCode != null && failureCode != "__ignore__")
				{
					game.eventManager?.TriggerIngameError(this, failureCode, Lang.Get("placefailure-" + failureCode));
				}
			}
			if (game.PickBlock && game.player.worlddata.CurrentGameMode == EnumGameMode.Creative)
			{
				OnBlockPick(blockSelection.Position, selectedBlock);
			}
		}
		long ellapsedMs = game.ElapsedMilliseconds;
		if (isSurvivalBreaking && game.InWorldMouseState.Left && game.player.worlddata.CurrentGameMode == EnumGameMode.Survival && ellapsedMs - lastbreakMilliseconds >= 40)
		{
			ContinueBreakSurvival(blockSelection, selectedBlock, dt);
			lastbreakMilliseconds = ellapsedMs;
			if (ellapsedMs - lastbreakNotifyMilliseconds > 80)
			{
				lastbreakNotifyMilliseconds = ellapsedMs;
			}
		}
	}

	private void StartAttackAnimation()
	{
		game.HandSetAttackDestroy = true;
	}

	private void OnBlockPick(BlockPos pos, Block block)
	{
		if (game.player.worlddata.CurrentGameMode != EnumGameMode.Creative)
		{
			return;
		}
		IInventory hotbarInv = game.player.inventoryMgr.GetHotbarInventory();
		if (hotbarInv == null)
		{
			return;
		}
		ItemStack blockStack = block.OnPickBlock(game, pos);
		int firstFreeSlotId = -1;
		for (int i = 0; i < hotbarInv.Count; i++)
		{
			if ((hotbarInv[i].StorageType & (EnumItemStorageFlags.Backpack | EnumItemStorageFlags.Offhand)) == 0)
			{
				IItemStack itemstack = hotbarInv[i].Itemstack;
				if (firstFreeSlotId == -1 && hotbarInv[i].Empty && hotbarInv[i].CanTakeFrom(new DummySlot(blockStack)))
				{
					firstFreeSlotId = i;
				}
				if (itemstack != null && itemstack.Equals(game, blockStack, GlobalConstants.IgnoredStackAttributes))
				{
					game.player.inventoryMgr.ActiveHotbarSlotNumber = i;
					return;
				}
			}
		}
		ItemSlot selectedHotbarSlot = game.player.inventoryMgr.ActiveHotbarSlot;
		if ((selectedHotbarSlot.Itemstack != null || !selectedHotbarSlot.CanTakeFrom(new DummySlot(blockStack))) && firstFreeSlotId != -1)
		{
			selectedHotbarSlot = hotbarInv[firstFreeSlotId];
			game.player.inventoryMgr.ActiveHotbarSlotNumber = firstFreeSlotId;
		}
		if (selectedHotbarSlot.CanHold(new DummySlot(blockStack)))
		{
			selectedHotbarSlot.Itemstack = blockStack;
			selectedHotbarSlot.MarkDirty();
			game.SendPacketClient(new Packet_Client
			{
				Id = 10,
				CreateItemstack = new Packet_CreateItemstack
				{
					Itemstack = StackConverter.ToPacket(blockStack),
					TargetInventoryId = selectedHotbarSlot.Inventory.InventoryID,
					TargetSlot = game.player.inventoryMgr.ActiveHotbarSlotNumber,
					TargetLastChanged = ((InventoryBase)hotbarInv).lastChangedSinceServerStart
				}
			});
		}
	}

	private bool OnBlockBuild(BlockSelection blockSelection, Block onBlock, ref string failureCode)
	{
		ItemSlot selectedHotbarSlot = game.player.inventoryMgr.ActiveHotbarSlot;
		Block newBlock = game.Blocks[selectedHotbarSlot.Itemstack.Id];
		BlockPos buildPos = blockSelection.Position;
		if (onBlock == null || !onBlock.IsReplacableBy(newBlock))
		{
			buildPos = buildPos.Offset(blockSelection.Face);
			blockSelection.DidOffset = true;
		}
		if (game.OnPlayerTryPlace(blockSelection, ref failureCode))
		{
			game.PlaySound(newBlock.GetSounds(game.BlockAccessor, blockSelection)?.Place, randomizePitch: true);
			game.HandSetAttackBuild = true;
			return true;
		}
		if (blockSelection.DidOffset)
		{
			buildPos.Offset(blockSelection.Face.Opposite);
			blockSelection.DidOffset = false;
		}
		return false;
	}

	private void loadOrCreateBlockDamage(BlockSelection blockSelection, Block block)
	{
		BlockDamage prevDmg = curBlockDmg;
		EnumTool? tool = game.player.inventoryMgr?.ActiveHotbarSlot?.Itemstack?.Collectible?.Tool;
		curBlockDmg = game.loadOrCreateBlockDamage(blockSelection, block, tool, game.player);
		if (prevDmg != null && !prevDmg.Position.Equals(blockSelection.Position))
		{
			curBlockDmg.LastBreakEllapsedMs = game.ElapsedMilliseconds;
		}
	}

	private void InitBlockBreakSurvival(BlockSelection blockSelection, float dt)
	{
		Block block = blockSelection.Block ?? game.BlockAccessor.GetBlock(blockSelection.Position);
		loadOrCreateBlockDamage(blockSelection, block);
		curBlockDmg.LastBreakEllapsedMs = game.ElapsedMilliseconds;
		curBlockDmg.BeginBreakEllapsedMs = game.ElapsedMilliseconds;
		isSurvivalBreaking = true;
	}

	private void StopBlockBreakSurvival()
	{
		curBlockDmg = null;
		isSurvivalBreaking = false;
		survivalBreakingCounter = 0;
	}

	private void ContinueBreakSurvival(BlockSelection blockSelection, Block block, float dt)
	{
		loadOrCreateBlockDamage(blockSelection, block);
		long elapsedMs = game.ElapsedMilliseconds;
		int diff = (int)(elapsedMs - curBlockDmg.LastBreakEllapsedMs);
		long decorBreakPoint = curBlockDmg.BeginBreakEllapsedMs + 225;
		if (elapsedMs >= decorBreakPoint && curBlockDmg.LastBreakEllapsedMs < decorBreakPoint && game.BlockAccessor.GetChunkAtBlockPos(blockSelection.Position) is WorldChunk c && game.tryAccess(blockSelection, EnumBlockAccessFlags.BuildOrBreak))
		{
			BlockPos pos = blockSelection.Position;
			int chunksize = 32;
			c.BreakDecor(game, pos, blockSelection.Face);
			game.WorldMap.MarkChunkDirty(pos.X / chunksize, pos.Y / chunksize, pos.Z / chunksize, priority: true);
			game.SendPacketClient(ClientPackets.BlockInteraction(blockSelection, 2, 0));
		}
		curBlockDmg.RemainingResistance = block.OnGettingBroken(game.player, blockSelection, game.player.inventoryMgr.ActiveHotbarSlot, curBlockDmg.RemainingResistance, (float)diff / 1000f, survivalBreakingCounter);
		survivalBreakingCounter++;
		curBlockDmg.Facing = blockSelection.Face;
		if (curBlockDmg.Position != blockSelection.Position || curBlockDmg.Block != block)
		{
			curBlockDmg.RemainingResistance = block.GetResistance(game.BlockAccessor, blockSelection.Position);
			curBlockDmg.Block = block;
			curBlockDmg.Position = blockSelection.Position;
		}
		if (curBlockDmg.RemainingResistance <= 0f)
		{
			game.eventManager?.TriggerBlockBroken(curBlockDmg);
			game.OnPlayerTryDestroyBlock(blockSelection);
			game.damagedBlocks.Remove(blockSelection.Position);
			UpdateCurrentSelection();
		}
		else
		{
			game.eventManager?.TriggerBlockBreaking(curBlockDmg);
		}
		curBlockDmg.LastBreakEllapsedMs = elapsedMs;
	}

	internal float BuildRepeatDelay(ClientMain game)
	{
		return 0.25f;
	}

	private bool TryBeginUseActiveSlotItem(BlockSelection blockSel, EntitySelection entitySel)
	{
		EnumHandHandling handling = EnumHandHandling.NotHandled;
		bool num = TryBeginUseActiveSlotItem(blockSel, entitySel, EnumHandInteract.HeldItemInteract, ref handling);
		if (num && (handling == EnumHandHandling.PreventDefaultAction || handling == EnumHandHandling.Handled))
		{
			game.HandSetAttackBuild = true;
		}
		return num;
	}

	private bool TryBeginAttackWithActiveSlotItem(BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		return TryBeginUseActiveSlotItem(blockSel, entitySel, EnumHandInteract.HeldItemAttack, ref handling);
	}

	private bool TryBeginUseActiveSlotItem(BlockSelection blockSel, EntitySelection entitySel, EnumHandInteract useType, ref EnumHandHandling handling)
	{
		ItemSlot slot = game.player.inventoryMgr.ActiveHotbarSlot;
		if (((game.InWorldMouseState.Right && useType == EnumHandInteract.HeldItemInteract) || (game.InWorldMouseState.Left && useType == EnumHandInteract.HeldItemAttack)) && slot != null && slot.Itemstack != null)
		{
			EntityControls controls = game.EntityPlayer.Controls;
			bool firstEvent = (useType == EnumHandInteract.HeldItemInteract && !prevMouseRight) || (useType == EnumHandInteract.HeldItemAttack && !prevMouseLeft);
			slot.Itemstack.Collectible.OnHeldUseStart(slot, game.EntityPlayer, blockSel, entitySel, useType, firstEvent, ref handling);
			if (handling == EnumHandHandling.NotHandled)
			{
				controls.HandUse = EnumHandInteract.None;
			}
			else
			{
				controls.HandUse = useType;
			}
			if (handling != 0)
			{
				controls.UsingCount = 0;
				controls.UsingBeginMS = game.ElapsedMilliseconds;
				if (controls.UsingHeldItemTransformAfter != null)
				{
					controls.UsingHeldItemTransformAfter.Clear();
				}
				if (controls.UsingHeldItemTransformBefore != null)
				{
					controls.UsingHeldItemTransformBefore.Clear();
				}
				if (controls.LeftUsingHeldItemTransformBefore != null)
				{
					controls.LeftUsingHeldItemTransformBefore.Clear();
				}
				if (slot.StackSize <= 0)
				{
					slot.Itemstack = null;
					slot.MarkDirty();
				}
				game.SendHandInteraction(2, blockSel, entitySel, useType, EnumHandInteractNw.StartHeldItemUse, firstEvent);
				return true;
			}
		}
		return false;
	}

	private bool TryBeginUseBlock(Block selectedBlock, BlockSelection blockSelection)
	{
		if (!game.tryAccess(blockSelection, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		if (selectedBlock.OnBlockInteractStart(game, game.player, blockSelection))
		{
			EntityControls controls = game.EntityPlayer.Controls;
			controls.HandUse = EnumHandInteract.BlockInteract;
			game.api.Network.SendPlayerPositionPacket();
			controls.UsingCount = 0;
			controls.UsingBeginMS = game.ElapsedMilliseconds;
			controls.HandUsingBlockSel = blockSelection.Clone();
			if (controls.UsingHeldItemTransformAfter != null)
			{
				controls.UsingHeldItemTransformAfter.Clear();
			}
			if (controls.UsingHeldItemTransformBefore != null)
			{
				controls.UsingHeldItemTransformBefore.Clear();
			}
			if (controls.LeftUsingHeldItemTransformBefore != null)
			{
				controls.LeftUsingHeldItemTransformBefore.Clear();
			}
			game.SendHandInteraction(2, blockSelection, null, EnumHandInteract.BlockInteract, EnumHandInteractNw.StartBlockUse, firstEvent: false);
			return true;
		}
		return false;
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
