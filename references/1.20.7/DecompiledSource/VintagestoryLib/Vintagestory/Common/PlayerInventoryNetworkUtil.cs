using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Common;

public class PlayerInventoryNetworkUtil : InventoryNetworkUtil
{
	public PlayerInventoryNetworkUtil(InventoryBase inv, ICoreAPI api)
		: base(inv, api)
	{
	}

	public override void UpdateFromPacket(IWorldAccessor world, Packet_InventoryUpdate packet)
	{
		ItemStack prevStack = null;
		ItemSlot slot = inv[packet.SlotId];
		if (IsOwnHotbarSlotClient(slot))
		{
			prevStack = slot.Itemstack;
			if (prevStack != null)
			{
				ItemStack newStackPreview = ItemStackFromPacket(world, packet.ItemStack);
				if (newStackPreview == null || prevStack.Collectible != newStackPreview.Collectible)
				{
					IClientPlayer plr = (world as IClientWorldAccessor).Player;
					prevStack.Collectible.OnHeldInteractCancel(0f, slot, plr.Entity, plr.CurrentBlockSelection, plr.CurrentEntitySelection, EnumItemUseCancelReason.Destroyed);
				}
			}
		}
		base.UpdateFromPacket(world, packet);
	}

	private bool IsOwnHotbarSlotClient(ItemSlot slot)
	{
		return (base.Api as ICoreClientAPI)?.World.Player.InventoryManager.ActiveHotbarSlot == slot;
	}
}
