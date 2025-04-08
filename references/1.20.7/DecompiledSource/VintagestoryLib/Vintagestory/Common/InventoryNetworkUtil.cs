using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.Common;

public class InventoryNetworkUtil : IInventoryNetworkUtil
{
	protected InventoryBase inv;

	private bool pauseInvUpdates;

	private Queue<Packet_InventoryUpdate> pkts = new Queue<Packet_InventoryUpdate>();

	public ICoreAPI Api { get; set; }

	public bool PauseInventoryUpdates
	{
		get
		{
			return pauseInvUpdates;
		}
		set
		{
			bool num = !value && pauseInvUpdates;
			pauseInvUpdates = value;
			if (num)
			{
				while (pkts.Count > 0)
				{
					Packet_InventoryUpdate pkt = pkts.Dequeue();
					UpdateFromPacket(Api.World, pkt);
				}
			}
		}
	}

	public InventoryNetworkUtil(InventoryBase inv, ICoreAPI api)
	{
		this.inv = inv;
		Api = api;
	}

	public virtual void HandleClientPacket(IPlayer byPlayer, int packetId, byte[] data)
	{
		Packet_Client packet = new Packet_Client();
		Packet_ClientSerializer.DeserializeBuffer(data, data.Length, packet);
		HandleClientPacket(byPlayer, packetId, packet);
	}

	public virtual void HandleClientPacket(IPlayer byPlayer, int packetId, Packet_Client packet)
	{
		IWorldPlayerData plrData = byPlayer.WorldData;
		switch (packetId)
		{
		case 7:
		{
			Packet_ActivateInventorySlot p2 = packet.ActivateInventorySlot;
			EnumMouseButton button = (EnumMouseButton)p2.MouseButton;
			long lastChanged2 = p2.TargetLastChanged;
			if (inv.lastChangedSinceServerStart < lastChanged2)
			{
				SendInventoryContents(byPlayer, inv.InventoryID);
				break;
			}
			int targetSlotId = p2.TargetSlot;
			IInventory targetInv3 = inv;
			if (inv is ITabbedInventory)
			{
				((ITabbedInventory)inv).SetTab(packet.ActivateInventorySlot.TabIndex);
			}
			ItemSlot targetSlot = targetInv3[targetSlotId];
			if (targetSlot == null)
			{
				Api.World.Logger.Warning("{0} left-clicked slot {1} in {2}, but slot did not exist!", byPlayer?.PlayerName, targetSlotId, targetInv3.InventoryID);
				break;
			}
			string sourceInvId = "mouse-" + plrData.PlayerUID;
			ItemSlot sourceSlot = byPlayer.InventoryManager.GetInventory(sourceInvId)[0];
			ItemStackMoveOperation op2 = new ItemStackMoveOperation(Api.World, button, (EnumModifierKey)p2.Modifiers, (EnumMergePriority)p2.Priority);
			op2.WheelDir = p2.Dir;
			op2.ActingPlayer = byPlayer;
			if (button == EnumMouseButton.Wheel)
			{
				op2.RequestedQuantity = 1;
			}
			string mouseSlotContents = (sourceSlot.Empty ? "empty" : $"{sourceSlot.StackSize}x{sourceSlot.GetStackName()}");
			string targetSlotContents = (targetSlot.Empty ? "empty" : $"{targetSlot.StackSize}x{targetSlot.GetStackName()}");
			targetInv3.ActivateSlot(targetSlotId, sourceSlot, ref op2);
			string mouseSlotContentsAfter = (sourceSlot.Empty ? "empty" : $"{sourceSlot.StackSize}x{sourceSlot.GetStackName()}");
			if (mouseSlotContents != mouseSlotContentsAfter)
			{
				string targetSlotContentsAfter = (targetSlot.Empty ? "empty" : $"{targetSlot.StackSize}x{targetSlot.GetStackName()}");
				Api.World.Logger.Audit("{0} left clicked slot {1} in {2}. Before: (mouse: {3}, inv: {4}), after: (mouse: {5}, inv: {6})", op2.ActingPlayer?.PlayerName, targetSlotId, targetInv3.InventoryID, mouseSlotContents, targetSlotContents, mouseSlotContentsAfter, targetSlotContentsAfter);
			}
			break;
		}
		case 8:
		{
			string[] invIds2 = new string[2]
			{
				packet.MoveItemstack.SourceInventoryId,
				packet.MoveItemstack.TargetInventoryId
			};
			int[] slotIds2 = new int[2]
			{
				packet.MoveItemstack.SourceSlot,
				packet.MoveItemstack.TargetSlot
			};
			if (SendDirtyInventoryContents(byPlayer, invIds2[0], packet.MoveItemstack.SourceLastChanged) || SendDirtyInventoryContents(byPlayer, invIds2[1], packet.MoveItemstack.TargetLastChanged))
			{
				InventoryBase targetInv2 = (InventoryBase)byPlayer.InventoryManager.GetInventory(invIds2[1]);
				Api.World.Logger.Audit("Revert itemstack move command by {0} to move {1}x{4} from {2} to {3}", byPlayer.PlayerName, packet.MoveItemstack.Quantity, invIds2[0], invIds2[1], targetInv2[slotIds2[1]].GetStackName());
				break;
			}
			if (inv is ITabbedInventory)
			{
				((ITabbedInventory)inv).SetTab(packet.MoveItemstack.TabIndex);
			}
			ItemStackMoveOperation op = new ItemStackMoveOperation(Api.World, (EnumMouseButton)packet.MoveItemstack.MouseButton, (EnumModifierKey)packet.MoveItemstack.Modifiers, (EnumMergePriority)packet.MoveItemstack.Priority, packet.MoveItemstack.Quantity);
			op.ActingPlayer = byPlayer;
			AssetLocation collectibleCode = inv.GetSlotsIfExists(byPlayer, invIds2, slotIds2)[0]?.Itemstack?.Collectible.Code;
			if (inv.TryMoveItemStack(byPlayer, invIds2, slotIds2, ref op))
			{
				Api.World.Logger.Audit("{0} moved {1}x{4} from {2} to {3}", byPlayer.PlayerName, packet.MoveItemstack.Quantity, invIds2[0], invIds2[1], collectibleCode);
			}
			else
			{
				SendInventoryContents(byPlayer, invIds2[0]);
				SendInventoryContents(byPlayer, invIds2[1]);
			}
			break;
		}
		case 9:
		{
			Packet_FlipItemstacks p = packet.Flipitemstacks;
			string[] invIds = new string[2] { p.SourceInventoryId, p.TargetInventoryId };
			int[] slotIds = new int[2] { p.SourceSlot, p.TargetSlot };
			long[] lastChanged = new long[2] { p.SourceLastChanged, p.TargetLastChanged };
			if (!SendDirtyInventoryContents(byPlayer, invIds[0], lastChanged[0]) && !SendDirtyInventoryContents(byPlayer, invIds[1], lastChanged[1]))
			{
				InventoryBase sourceInv = (InventoryBase)byPlayer.InventoryManager.GetInventory(invIds[0]);
				InventoryBase targetInv = (InventoryBase)byPlayer.InventoryManager.GetInventory(invIds[1]);
				if (sourceInv is ITabbedInventory)
				{
					((ITabbedInventory)sourceInv).SetTab(packet.Flipitemstacks.SourceTabIndex);
				}
				if (targetInv is ITabbedInventory)
				{
					((ITabbedInventory)targetInv).SetTab(packet.Flipitemstacks.TargetTabIndex);
				}
				if (inv.TryFlipItemStack(byPlayer, invIds, slotIds, lastChanged))
				{
					NotifyPlayersItemstackMoved(byPlayer, invIds, slotIds);
				}
				else
				{
					RevertPlayerItemstackMove(byPlayer, invIds, slotIds);
				}
			}
			break;
		}
		}
	}

	protected virtual bool SendDirtyInventoryContents(IPlayer owningPlayer, string inventoryId, long lastChangedClient)
	{
		InventoryBase targetInv = (InventoryBase)owningPlayer.InventoryManager.GetInventory(inventoryId);
		if (targetInv == null)
		{
			return false;
		}
		if (targetInv.lastChangedSinceServerStart > lastChangedClient)
		{
			SendInventoryContents(owningPlayer, inventoryId);
			return true;
		}
		return false;
	}

	protected virtual void RevertPlayerItemstackMove(IPlayer owningPlayer, string[] invIds, int[] slotIds)
	{
		ItemSlot[] slots = inv.GetSlotsIfExists(owningPlayer, invIds, slotIds);
		if (slots[0] != null && slots[1] != null)
		{
			byte[] data = getDoubleUpdatePacket(owningPlayer, invIds, slotIds);
			((ICoreServerAPI)Api).Network.SendArbitraryPacket(data, (IServerPlayer)owningPlayer);
		}
	}

	protected virtual void SendInventoryContents(IPlayer owningPlayer, string inventoryId)
	{
		InventoryBase targetInv = (InventoryBase)owningPlayer.InventoryManager.GetInventory(inventoryId);
		if (targetInv != null)
		{
			Packet_InventoryContents packet = (targetInv.InvNetworkUtil as InventoryNetworkUtil).ToPacket(owningPlayer);
			byte[] data = Packet_ServerSerializer.SerializeToBytes(new Packet_Server
			{
				Id = 30,
				InventoryContents = packet
			});
			((ICoreServerAPI)Api).Network.SendArbitraryPacket(data, (IServerPlayer)owningPlayer);
		}
	}

	protected virtual void NotifyPlayersItemstackMoved(IPlayer player, string[] invIds, int[] slotIds)
	{
		byte[] serialized = getDoubleUpdatePacket(player, invIds, slotIds);
		((ICoreServerAPI)Api).Network.BroadcastArbitraryPacket(serialized, (IServerPlayer)player);
	}

	public static byte[] getDoubleUpdatePacket(IPlayer player, string[] invIds, int[] slotIds)
	{
		IInventory inventory = player.InventoryManager.GetInventory(invIds[0]);
		IInventory inv2 = player.InventoryManager.GetInventory(invIds[1]);
		ItemStack itemstack1 = inventory[slotIds[0]].Itemstack;
		ItemStack itemstack2 = inv2[slotIds[1]].Itemstack;
		Packet_InventoryDoubleUpdate packet = new Packet_InventoryDoubleUpdate
		{
			ClientId = player.ClientId,
			InventoryId1 = invIds[0],
			InventoryId2 = invIds[1],
			SlotId1 = slotIds[0],
			SlotId2 = slotIds[1],
			ItemStack1 = ((itemstack1 != null) ? StackConverter.ToPacket(itemstack1) : null),
			ItemStack2 = ((itemstack2 != null) ? StackConverter.ToPacket(itemstack2) : null)
		};
		return Packet_ServerSerializer.SerializeToBytes(new Packet_Server
		{
			Id = 32,
			InventoryDoubleUpdate = packet
		});
	}

	internal virtual Packet_ItemStack[] CreatePacketItemStacks()
	{
		Packet_ItemStack[] itemstacks = new Packet_ItemStack[inv.CountForNetworkPacket];
		for (int i = 0; i < inv.CountForNetworkPacket; i++)
		{
			IItemStack stack = inv[i].Itemstack;
			if (stack != null)
			{
				MemoryStream ms = new MemoryStream();
				BinaryWriter writer = new BinaryWriter(ms);
				stack.Attributes.ToBytes(writer);
				itemstacks[i] = new Packet_ItemStack
				{
					ItemClass = (int)stack.Class,
					ItemId = stack.Id,
					StackSize = stack.StackSize,
					Attributes = ms.ToArray()
				};
			}
			else
			{
				itemstacks[i] = new Packet_ItemStack
				{
					ItemClass = -1,
					ItemId = 0,
					StackSize = 0
				};
			}
		}
		return itemstacks;
	}

	public virtual Packet_InventoryContents ToPacket(IPlayer player)
	{
		Packet_InventoryContents packet_InventoryContents = new Packet_InventoryContents();
		packet_InventoryContents.ClientId = player.ClientId;
		packet_InventoryContents.InventoryId = inv.InventoryID;
		packet_InventoryContents.InventoryClass = inv.ClassName;
		Packet_ItemStack[] Itemstacks = CreatePacketItemStacks();
		packet_InventoryContents.SetItemstacks(Itemstacks, Itemstacks.Length, Itemstacks.Length);
		return packet_InventoryContents;
	}

	public virtual Packet_Server getSlotUpdatePacket(IPlayer player, int slotId)
	{
		ItemSlot slot = inv[slotId];
		if (slot == null)
		{
			return null;
		}
		ItemStack itemstack = slot.Itemstack;
		Packet_ItemStack pstack = null;
		if (itemstack != null)
		{
			pstack = StackConverter.ToPacket(itemstack);
		}
		Packet_InventoryUpdate packet = new Packet_InventoryUpdate
		{
			ClientId = player.ClientId,
			InventoryId = inv.InventoryID,
			ItemStack = pstack,
			SlotId = slotId
		};
		return new Packet_Server
		{
			Id = 31,
			InventoryUpdate = packet
		};
	}

	public virtual object DidOpen(IPlayer player)
	{
		if (inv.Api.Side != EnumAppSide.Client)
		{
			return null;
		}
		Packet_InvOpenClose packet = new Packet_InvOpenClose
		{
			InventoryId = inv.InventoryID,
			Opened = 1
		};
		return new Packet_Client
		{
			Id = 30,
			InvOpenedClosed = packet
		};
	}

	public virtual object DidClose(IPlayer player)
	{
		if (inv.Api.Side != EnumAppSide.Client)
		{
			return null;
		}
		Packet_InvOpenClose packet = new Packet_InvOpenClose
		{
			InventoryId = inv.InventoryID,
			Opened = 0
		};
		return new Packet_Client
		{
			Id = 30,
			InvOpenedClosed = packet
		};
	}

	public virtual void UpdateFromPacket(IWorldAccessor resolver, Packet_InventoryContents packet)
	{
		for (int i = 0; i < packet.ItemstacksCount; i++)
		{
			ItemSlot slot = inv[i];
			if (UpdateSlotStack(slot, ItemStackFromPacket(resolver, packet.Itemstacks[i])))
			{
				inv.DidModifyItemSlot(slot);
			}
		}
	}

	public virtual void UpdateFromPacket(IWorldAccessor resolver, Packet_InventoryUpdate packet)
	{
		if (PauseInventoryUpdates)
		{
			pkts.Enqueue(packet);
			return;
		}
		ItemSlot slot = inv[packet.SlotId];
		if (slot != null)
		{
			UpdateSlotStack(slot, ItemStackFromPacket(resolver, packet.ItemStack));
			inv.DidModifyItemSlot(slot);
		}
	}

	public virtual void UpdateFromPacket(IWorldAccessor resolver, Packet_InventoryDoubleUpdate packet)
	{
		if (packet.InventoryId1 == inv.InventoryID)
		{
			ItemSlot slot2 = inv[packet.SlotId1];
			UpdateSlotStack(slot2, ItemStackFromPacket(resolver, packet.ItemStack1));
			inv.DidModifyItemSlot(slot2);
		}
		if (packet.InventoryId2 == inv.InventoryID)
		{
			ItemSlot slot = inv[packet.SlotId2];
			UpdateSlotStack(slot, ItemStackFromPacket(resolver, packet.ItemStack2));
			inv.DidModifyItemSlot(slot);
		}
	}

	protected ItemStack ItemStackFromPacket(IWorldAccessor resolver, Packet_ItemStack pItemStack)
	{
		if (pItemStack == null || ((pItemStack.ItemClass == -1) | (pItemStack.ItemId == 0)))
		{
			return null;
		}
		return StackConverter.FromPacket(pItemStack, resolver);
	}

	private bool UpdateSlotStack(ItemSlot slot, ItemStack newStack)
	{
		if (slot.Itemstack != null && newStack != null && slot.Itemstack.Collectible == newStack.Collectible)
		{
			newStack.TempAttributes = slot.Itemstack?.TempAttributes;
		}
		bool result = newStack == null != (slot.Itemstack == null) || (newStack != null && !newStack.Equals(Api.World, slot.Itemstack, GlobalConstants.IgnoredStackAttributes));
		slot.Itemstack = newStack;
		return result;
	}

	public object GetActivateSlotPacket(int slotId, ItemStackMoveOperation op)
	{
		Packet_ActivateInventorySlot activateSlotPacket = new Packet_ActivateInventorySlot
		{
			MouseButton = (int)op.MouseButton,
			TargetInventoryId = inv.InventoryID,
			TargetSlot = slotId,
			TargetLastChanged = inv.lastChangedSinceServerStart,
			Modifiers = (int)op.Modifiers,
			Priority = (int)op.CurrentPriority,
			Dir = op.WheelDir
		};
		if (inv is ITabbedInventory)
		{
			activateSlotPacket.TabIndex = ((ITabbedInventory)inv).CurrentTab.Index;
		}
		return new Packet_Client
		{
			Id = 7,
			ActivateInventorySlot = activateSlotPacket
		};
	}

	public object GetFlipSlotsPacket(IInventory sourceInv, int sourceSlotId, int targetSlotId)
	{
		Packet_Client p = new Packet_Client
		{
			Id = 9,
			Flipitemstacks = new Packet_FlipItemstacks
			{
				SourceInventoryId = sourceInv.InventoryID,
				SourceLastChanged = ((InventoryBase)sourceInv).lastChangedSinceServerStart,
				SourceSlot = sourceSlotId,
				TargetInventoryId = inv.InventoryID,
				TargetLastChanged = inv.lastChangedSinceServerStart,
				TargetSlot = targetSlotId
			}
		};
		if (sourceInv is ITabbedInventory)
		{
			p.Flipitemstacks.SourceTabIndex = (sourceInv as ITabbedInventory).CurrentTab.Index;
		}
		if (sourceInv is CreativeInventoryTab)
		{
			p.Flipitemstacks.SourceTabIndex = (sourceInv as CreativeInventoryTab).TabIndex;
		}
		if (inv is ITabbedInventory)
		{
			p.Flipitemstacks.TargetTabIndex = (inv as ITabbedInventory).CurrentTab.Index;
		}
		if (inv is CreativeInventoryTab)
		{
			p.Flipitemstacks.TargetTabIndex = (inv as CreativeInventoryTab).TabIndex;
		}
		return p;
	}
}
