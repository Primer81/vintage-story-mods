using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ClientPlayerInventoryManager : PlayerInventoryManager
{
	public ItemSlot currentHoveredSlot;

	private ClientMain game;

	public override ItemSlot CurrentHoveredSlot
	{
		get
		{
			return currentHoveredSlot;
		}
		set
		{
			currentHoveredSlot = value;
			game.api.Input.TriggerOnMouseEnterSlot(value);
		}
	}

	public override int ActiveHotbarSlotNumber
	{
		get
		{
			return base.ActiveHotbarSlotNumber;
		}
		set
		{
			int beforeSlot = base.ActiveHotbarSlotNumber;
			if (value == beforeSlot)
			{
				return;
			}
			if (player == game.player && game.eventManager != null)
			{
				if (!game.eventManager.TriggerBeforeActiveSlotChanged(game, beforeSlot, value))
				{
					return;
				}
				game.SendPacketClient(ClientPackets.SelectedHotbarSlot(value));
			}
			base.ActiveHotbarSlotNumber = value;
			if (player == game.player)
			{
				game.eventManager?.TriggerAfterActiveSlotChanged(game, beforeSlot, value);
			}
		}
	}

	public ClientPlayerInventoryManager(OrderedDictionary<string, InventoryBase> AllInventories, IPlayer player, ClientMain game)
		: base(AllInventories, player)
	{
		this.game = game;
	}

	public void SetActiveHotbarSlotNumberFromServer(int slotid)
	{
		int beforeSlot = base.ActiveHotbarSlotNumber;
		base.ActiveHotbarSlotNumber = slotid;
		if (player == game.player)
		{
			game.eventManager?.TriggerAfterActiveSlotChanged(game, beforeSlot, slotid);
		}
	}

	public override void NotifySlot(IPlayer player, ItemSlot slot)
	{
	}

	public override bool DropItem(ItemSlot slot, bool fullStack = false)
	{
		if (slot?.Itemstack == null)
		{
			return false;
		}
		int quantity = ((!fullStack) ? 1 : slot.Itemstack.StackSize);
		EnumHandling handling = EnumHandling.PassThrough;
		slot.Itemstack.Collectible.OnHeldDropped(game, game.player, slot, quantity, ref handling);
		if (handling != 0)
		{
			return false;
		}
		if (quantity >= slot.Itemstack.StackSize && slot == game.player.inventoryMgr.ActiveHotbarSlot && game.EntityPlayer.Controls.HandUse != 0)
		{
			EnumHandInteract beforeUseType = game.EntityPlayer.Controls.HandUse;
			if (!game.EntityPlayer.TryStopHandAction(forceStop: true, EnumItemUseCancelReason.Dropped))
			{
				return false;
			}
			if (slot.StackSize <= 0)
			{
				slot.Itemstack = null;
				slot.MarkDirty();
			}
			game.SendHandInteraction(2, game.BlockSelection, game.EntitySelection, beforeUseType, EnumHandInteractNw.CancelHeldItemUse, firstEvent: false, EnumItemUseCancelReason.Dropped);
		}
		IInventory targetInv = GetOwnInventory("ground");
		ItemStackMoveOperation op = new ItemStackMoveOperation(game, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, quantity);
		op.ActingPlayer = game.player;
		slot.TryPutInto(targetInv[0], ref op);
		int tabIndex = 0;
		if (slot.Inventory is CreativeInventoryTab iti)
		{
			tabIndex = iti.TabIndex;
		}
		Packet_Client packet = new Packet_Client
		{
			Id = 8,
			MoveItemstack = new Packet_MoveItemstack
			{
				Quantity = quantity,
				SourceInventoryId = slot.Inventory.InventoryID,
				SourceSlot = slot.Inventory.GetSlotId(slot),
				SourceLastChanged = slot.Inventory.LastChanged,
				TargetInventoryId = targetInv.InventoryID,
				TargetSlot = 0,
				TargetLastChanged = targetInv.LastChanged,
				TabIndex = tabIndex
			}
		};
		game.SendPacketClient(packet);
		return true;
	}

	public override void BroadcastHotbarSlot()
	{
	}
}
