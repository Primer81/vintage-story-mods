using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ServerPlayerInventoryManager : PlayerInventoryManager
{
	private ServerMain server;

	public override ItemSlot CurrentHoveredSlot
	{
		get
		{
			throw new NotImplementedException("This information is not available on the server");
		}
		set
		{
			throw new NotImplementedException("This information is not available on the server");
		}
	}

	public ServerPlayerInventoryManager(OrderedDictionary<string, InventoryBase> AllInventories, IPlayer player, ServerMain server)
		: base(AllInventories, player)
	{
		this.server = server;
	}

	public override void BroadcastHotbarSlot()
	{
		server.BroadcastHotbarSlot(player as ServerPlayer);
	}

	public override bool DropItem(ItemSlot slot, bool fullStack = false)
	{
		if (slot?.Itemstack == null)
		{
			return false;
		}
		int quantity = ((!fullStack) ? 1 : slot.Itemstack.StackSize);
		EnumHandling handling = EnumHandling.PassThrough;
		slot.Itemstack.Collectible.OnHeldDropped(server, player, slot, quantity, ref handling);
		if (handling != 0)
		{
			return false;
		}
		if (quantity >= slot.Itemstack.StackSize && slot == base.ActiveHotbarSlot && player.Entity.Controls.HandUse != 0)
		{
			if (!player.Entity.TryStopHandAction(forceStop: true, EnumItemUseCancelReason.Dropped))
			{
				return false;
			}
			if (slot.StackSize <= 0)
			{
				slot.Itemstack = null;
				slot.MarkDirty();
			}
		}
		IInventory targetInv = GetOwnInventory("ground");
		ItemStackMoveOperation op = new ItemStackMoveOperation(server, EnumMouseButton.Left, EnumModifierKey.SHIFT, EnumMergePriority.AutoMerge, quantity);
		op.ActingPlayer = player;
		slot.TryPutInto(targetInv[0], ref op);
		slot.MarkDirty();
		return true;
	}

	public override void NotifySlot(IPlayer toPlayer, ItemSlot slot)
	{
		if (slot.Inventory != null)
		{
			server.SendPacket(toPlayer as IServerPlayer, new Packet_Server
			{
				Id = 66,
				NotifySlot = new Packet_NotifySlot
				{
					InventoryId = slot.Inventory.InventoryID,
					SlotId = slot.Inventory.GetSlotId(slot)
				}
			});
		}
	}
}
