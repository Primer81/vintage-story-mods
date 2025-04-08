using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

internal class ServerSystemInventory : ServerSystem
{
	private List<InventoryBase> dirtySlots2Clear = new List<InventoryBase>();

	public ServerSystemInventory(ServerMain server)
		: base(server)
	{
		server.RegisterGameTickListener(SendDirtySlots, 30);
		server.RegisterGameTickListener(OnUsingTick, 20);
		server.RegisterGameTickListener(UpdateTransitionStates, 4000);
		server.PacketHandlers[7] = HandleActivateInventorySlot;
		server.PacketHandlers[10] = HandleCreateItemstack;
		server.PacketHandlers[8] = HandleMoveItemstack;
		server.PacketHandlers[9] = HandleFlipItemStacks;
		server.PacketHandlers[25] = HandleHandInteraction;
		server.PacketHandlers[27] = HandleToolMode;
		server.PacketHandlers[30] = HandleInvOpenClose;
	}

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		base.OnPlayerDisconnect(player);
	}

	private void HandleInvOpenClose(Packet_Client packet, ConnectedClient client)
	{
		ServerPlayer player = client.Player;
		string invId = packet.InvOpenedClosed.InventoryId;
		if (player.InventoryManager.GetInventory(invId, out var inv))
		{
			if (packet.InvOpenedClosed.Opened > 0)
			{
				player.InventoryManager.OpenInventory(inv);
			}
			else
			{
				player.InventoryManager.CloseInventory(inv);
			}
		}
	}

	private void HandleToolMode(Packet_Client packet, ConnectedClient client)
	{
		ServerPlayer player = client.Player;
		ItemSlot slot = player.InventoryManager.ActiveHotbarSlot;
		if (slot.Itemstack != null)
		{
			Packet_ToolMode pt = packet.ToolMode;
			BlockSelection sele = new BlockSelection
			{
				Position = new BlockPos(pt.X, pt.Y, pt.Z),
				Face = BlockFacing.ALLFACES[pt.Face],
				HitPosition = new Vec3d(CollectibleNet.DeserializeDouble(pt.HitX), CollectibleNet.DeserializeDouble(pt.HitY), CollectibleNet.DeserializeDouble(pt.HitZ)),
				SelectionBoxIndex = pt.SelectionBoxIndex
			};
			slot.Itemstack.Collectible.SetToolMode(slot, player, sele, packet.ToolMode.Mode);
		}
	}

	private void OnUsingTick(float dt)
	{
		foreach (ServerPlayer player in server.PlayersByUid.Values)
		{
			if (player.ConnectionState != EnumClientState.Playing)
			{
				continue;
			}
			ItemSlot slot = player.inventoryMgr.ActiveHotbarSlot;
			if (player.Entity.Controls.LeftMouseDown && player.WorldData.CurrentGameMode == EnumGameMode.Survival && player.CurrentBlockSelection?.Position != null && slot.Itemstack != null)
			{
				slot.Itemstack.Collectible.OnBlockBreaking(player, player.CurrentBlockSelection, slot, 99f, dt, player.blockBreakingCounter);
				player.blockBreakingCounter++;
			}
			else
			{
				player.blockBreakingCounter = 0;
			}
			if (!player.Entity.LeftHandItemSlot.Empty)
			{
				player.Entity.LeftHandItemSlot.Itemstack.Collectible.OnHeldIdle(player.Entity.LeftHandItemSlot, player.Entity);
			}
			if ((player.Entity.Controls.HandUse == EnumHandInteract.None || player.Entity.Controls.HandUse == EnumHandInteract.BlockInteract) && slot != null)
			{
				if (slot.Itemstack != null)
				{
					slot.Itemstack.Collectible.OnHeldIdle(slot, player.Entity);
				}
			}
			else if (slot != null && slot.Itemstack != null)
			{
				float secondsPassed = (float)(server.ElapsedMilliseconds - player.Entity.Controls.UsingBeginMS) / 1000f;
				int stackSize = slot.StackSize;
				callOnUsing(slot, player, player.CurrentUsingBlockSelection ?? player.CurrentBlockSelection, player.CurrentUsingEntitySelection ?? player.CurrentEntitySelection, ref secondsPassed);
				if (slot.StackSize <= 0)
				{
					slot.Itemstack = null;
				}
				if (stackSize != slot.StackSize)
				{
					slot.MarkDirty();
				}
			}
			else
			{
				player.Entity.Controls.HandUse = EnumHandInteract.None;
			}
		}
	}

	private void UpdateTransitionStates(float dt)
	{
		foreach (ConnectedClient client in server.Clients.Values)
		{
			if (!client.IsPlayingClient)
			{
				continue;
			}
			foreach (InventoryBase inv in client.Player.inventoryMgr.Inventories.Values)
			{
				if (!(inv is InventoryBasePlayer) || inv is InventoryPlayerCreative)
				{
					continue;
				}
				foreach (ItemSlot slot in inv)
				{
					slot.Itemstack?.Collectible?.UpdateAndGetTransitionStates(server, slot);
				}
			}
		}
	}

	private void SendDirtySlots(float dt)
	{
		foreach (ConnectedClient client in server.Clients.Values)
		{
			if (!client.IsPlayingClient)
			{
				continue;
			}
			foreach (InventoryBase inv in client.Player.inventoryMgr.Inventories.Values)
			{
				if (!inv.IsDirty)
				{
					continue;
				}
				if (inv is InventoryCharacter)
				{
					client.Player.BroadcastPlayerData();
				}
				foreach (int slotId in inv.dirtySlots)
				{
					Packet_Server slotUpdate = (inv.InvNetworkUtil as InventoryNetworkUtil).getSlotUpdatePacket(client.Player, slotId);
					if (slotUpdate != null)
					{
						server.SendPacket(client.Id, slotUpdate);
						ItemSlot slot = inv[slotId];
						if (slot != null && slot == client.Player.inventoryMgr.ActiveHotbarSlot)
						{
							client.Player.inventoryMgr.BroadcastHotbarSlot();
						}
					}
				}
				dirtySlots2Clear.Add(inv);
			}
		}
		foreach (InventoryBase item in dirtySlots2Clear)
		{
			item.dirtySlots.Clear();
		}
		dirtySlots2Clear.Clear();
	}

	public override void OnPlayerJoin(ServerPlayer player)
	{
		foreach (InventoryBase value in player.inventoryMgr.Inventories.Values)
		{
			value.AfterBlocksLoaded(server);
		}
		for (int i = 0; i < PlayerInventoryManager.defaultInventories.Length; i++)
		{
			string key = PlayerInventoryManager.defaultInventories[i] + "-" + player.WorldData.PlayerUID;
			if (!player.InventoryManager.Inventories.ContainsKey(key))
			{
				CreateNewInventory(player, PlayerInventoryManager.defaultInventories[i]);
			}
			if (player.WorldData.CurrentGameMode == EnumGameMode.Creative || PlayerInventoryManager.defaultInventories[i] != "creative")
			{
				player.inventoryMgr.Inventories[key].Open(player);
			}
		}
		OnPlayerSwitchGameMode(player);
	}

	private string CreateNewInventory(ServerPlayer player, string inventoryClassName)
	{
		string invId = inventoryClassName + "-" + player.PlayerUID;
		InventoryBasePlayer inv = (InventoryBasePlayer)ServerMain.ClassRegistry.CreateInventory(inventoryClassName, invId, server.api);
		player.SetInventory(inv);
		inv.AfterBlocksLoaded(server);
		return invId;
	}

	public override void OnPlayerSwitchGameMode(ServerPlayer player)
	{
		IInventory creativeInv = player.InventoryManager.GetOwnInventory("creative");
		IInventory backPackCraftingInv = player.InventoryManager.GetOwnInventory("craftinggrid");
		if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			creativeInv?.Open(player);
			backPackCraftingInv?.Close(player);
		}
		if (player.WorldData.CurrentGameMode == EnumGameMode.Guest || player.WorldData.CurrentGameMode == EnumGameMode.Survival)
		{
			creativeInv?.Close(player);
			backPackCraftingInv?.Open(player);
		}
	}

	private void HandleHandInteraction(Packet_Client packet, ConnectedClient client)
	{
		ServerPlayer player = client.Player;
		Packet_ClientHandInteraction p = packet.HandInteraction;
		if (p.EnumHandInteract >= 4)
		{
			server.OnHandleBlockInteract(packet, client);
			return;
		}
		string invId = p.InventoryId;
		ItemSlot slot = null;
		if (invId == null)
		{
			if (p.SlotId >= 10)
			{
				invId = "backpack-" + player.PlayerUID;
				if (player.InventoryManager.GetInventory(invId, out var invFound))
				{
					slot = invFound[p.SlotId - 10];
				}
			}
			else
			{
				slot = player.inventoryMgr.GetHotbarInventory()[p.SlotId];
			}
		}
		else
		{
			slot = player.InventoryManager.Inventories[invId][p.SlotId];
		}
		if (slot == null || slot.Itemstack == null)
		{
			return;
		}
		BlockSelection blockSel = null;
		EntitySelection entitySel = null;
		EnumHandInteract useType = (EnumHandInteract)p.UseType;
		if (useType == EnumHandInteract.None || p.MouseButton != 2)
		{
			return;
		}
		BlockPos pos = new BlockPos(p.X, p.Y, p.Z);
		BlockFacing facing = BlockFacing.ALLFACES[p.OnBlockFace];
		Vec3d hitPos = new Vec3d(CollectibleNet.DeserializeDoublePrecise(p.HitX), CollectibleNet.DeserializeDoublePrecise(p.HitY), CollectibleNet.DeserializeDoublePrecise(p.HitZ));
		if (p.X != 0 || p.Y != 0 || p.Z != 0)
		{
			blockSel = new BlockSelection
			{
				Position = pos,
				Face = facing,
				HitPosition = hitPos,
				SelectionBoxIndex = p.SelectionBoxIndex
			};
		}
		if (p.OnEntityId != 0L)
		{
			server.LoadedEntities.TryGetValue(p.OnEntityId, out var entity);
			if (entity == null)
			{
				return;
			}
			entitySel = new EntitySelection
			{
				Face = facing,
				HitPosition = hitPos,
				Entity = entity,
				Position = entity.ServerPos.XYZ
			};
		}
		player.CurrentUsingBlockSelection = blockSel;
		player.CurrentUsingEntitySelection = entitySel;
		EntityControls controls = player.Entity.Controls;
		float secondsPassed = (float)(server.ElapsedMilliseconds - controls.UsingBeginMS) / 1000f;
		switch ((EnumHandInteractNw)p.EnumHandInteract)
		{
		case EnumHandInteractNw.StartHeldItemUse:
		{
			EnumHandHandling handling = EnumHandHandling.NotHandled;
			slot.Itemstack.Collectible.OnHeldUseStart(slot, player.Entity, blockSel, entitySel, useType, p.FirstEvent > 0, ref handling);
			controls.HandUse = ((handling != 0) ? useType : EnumHandInteract.None);
			controls.UsingBeginMS = server.ElapsedMilliseconds;
			controls.UsingCount = 0;
			break;
		}
		case EnumHandInteractNw.CancelHeldItemUse:
		{
			int j = 0;
			while (controls.HandUse != 0 && controls.UsingCount < p.UsingCount && j++ < 5000)
			{
				callOnUsing(slot, player, blockSel, entitySel, ref secondsPassed, callStop: false);
			}
			if (j >= 5000)
			{
				ServerMain.Logger.Warning("CancelHeldItemUse packet: Excess (5000+) UseStep calls from {2} on item {0}, would require {1} more steps to complete. Will abort.", slot.Itemstack?.GetName(), p.UsingCount - controls.UsingCount, player.PlayerName);
			}
			EnumItemUseCancelReason cancelReason = (EnumItemUseCancelReason)p.CancelReason;
			if (slot.Itemstack == null)
			{
				controls.HandUse = EnumHandInteract.None;
			}
			else
			{
				controls.HandUse = slot.Itemstack.Collectible.OnHeldUseCancel(secondsPassed, slot, player.Entity, blockSel, entitySel, cancelReason);
			}
			break;
		}
		case EnumHandInteractNw.StopHeldItemUse:
			if (controls.HandUse != 0)
			{
				int i = 0;
				while (controls.HandUse != 0 && controls.UsingCount < p.UsingCount && i++ < 5000)
				{
					callOnUsing(slot, player, blockSel, entitySel, ref secondsPassed);
				}
				if (i >= 5000)
				{
					ServerMain.Logger.Warning("StopHeldItemUse packet: Excess (5000+) UseStep calls from {2} on item {0}, would require {1} more steps to complete. Will abort.", slot.Itemstack?.GetName(), p.UsingCount - controls.UsingCount, player.PlayerName);
				}
				controls.HandUse = EnumHandInteract.None;
				slot.Itemstack?.Collectible.OnHeldUseStop(secondsPassed, slot, player.Entity, blockSel, entitySel, useType);
			}
			break;
		}
		if (slot.StackSize <= 0)
		{
			slot.Itemstack = null;
		}
	}

	private void HandleFlipItemStacks(Packet_Client packet, ConnectedClient client)
	{
		ServerPlayer player = client.Player;
		string invId = packet.Flipitemstacks.SourceInventoryId;
		if (player.InventoryManager.GetInventory(invId, out var invFound))
		{
			(invFound.InvNetworkUtil as InventoryNetworkUtil).HandleClientPacket(player, packet.Id, packet);
		}
		if (player.inventoryMgr.IsVisibleHandSlot(invId, packet.Flipitemstacks.TargetSlot))
		{
			server.BroadcastHotbarSlot(player);
		}
	}

	private void HandleMoveItemstack(Packet_Client packet, ConnectedClient client)
	{
		ServerPlayer player = client.Player;
		string sinvId = packet.MoveItemstack.SourceInventoryId;
		string tinvId = packet.MoveItemstack.TargetInventoryId;
		if (player.InventoryManager.GetInventory(sinvId, out var inv))
		{
			(inv.InvNetworkUtil as InventoryNetworkUtil).HandleClientPacket(player, packet.Id, packet);
			if (player.inventoryMgr.IsVisibleHandSlot(sinvId, packet.MoveItemstack.SourceSlot))
			{
				server.BroadcastHotbarSlot(player);
			}
		}
		if (player.inventoryMgr.IsVisibleHandSlot(tinvId, packet.MoveItemstack.TargetSlot))
		{
			server.BroadcastHotbarSlot(player);
		}
	}

	private void HandleCreateItemstack(Packet_Client packet, ConnectedClient client)
	{
		ServerPlayer player = client.Player;
		Packet_CreateItemstack createpacket = packet.CreateItemstack;
		player.InventoryManager.GetInventory(createpacket.TargetInventoryId, out var inv);
		ItemSlot slot = inv?[createpacket.TargetSlot];
		if (player.WorldData.CurrentGameMode == EnumGameMode.Creative && slot != null)
		{
			ItemStack itemstack = (slot.Itemstack = StackConverter.FromPacket(createpacket.Itemstack, server));
			slot.MarkDirty();
			ServerMain.Logger.Audit("{0} creative mode created item stack {1}x{2}", player.PlayerName, itemstack.StackSize, itemstack.GetName());
		}
		else
		{
			Packet_Server revertPacket = (((InventoryBase)player.InventoryManager.Inventories[createpacket.TargetInventoryId]).InvNetworkUtil as InventoryNetworkUtil).getSlotUpdatePacket(player, createpacket.TargetSlot);
			if (revertPacket != null)
			{
				server.SendPacket(player.ClientId, revertPacket);
			}
		}
	}

	private void HandleActivateInventorySlot(Packet_Client packet, ConnectedClient client)
	{
		ServerPlayer player = client.Player;
		string invId = packet.ActivateInventorySlot.TargetInventoryId;
		if (player.InventoryManager.GetInventory(invId, out var invFound))
		{
			(invFound.InvNetworkUtil as InventoryNetworkUtil).HandleClientPacket(player, packet.Id, packet);
		}
		else
		{
			ServerMain.Logger.Warning("Got activate inventory slot packet on inventory " + invId + " but no such inventory currently opened?");
		}
		if (player.inventoryMgr.IsVisibleHandSlot(invId, packet.ActivateInventorySlot.TargetSlot))
		{
			server.BroadcastHotbarSlot(player);
		}
	}

	private void callOnUsing(ItemSlot slot, ServerPlayer player, BlockSelection blockSel, EntitySelection entitySel, ref float secondsPassed, bool callStop = true)
	{
		EntityControls controls = player.Entity.Controls;
		EnumHandInteract useType = controls.HandUse;
		if (!slot.Empty)
		{
			controls.UsingCount++;
			controls.HandUse = slot.Itemstack.Collectible.OnHeldUseStep(secondsPassed, slot, player.Entity, blockSel, entitySel);
			if (callStop && controls.HandUse == EnumHandInteract.None)
			{
				slot.Itemstack?.Collectible.OnHeldUseStop(secondsPassed, slot, player.Entity, blockSel, entitySel, useType);
			}
		}
		secondsPassed += 0.02f;
	}
}
