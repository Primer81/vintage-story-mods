using Vintagestory.API.Common;

namespace Vintagestory.Common;

internal class ItemSlotGround : ItemSlot
{
	public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.General | EnumItemStorageFlags.Backpack | EnumItemStorageFlags.Metallurgy | EnumItemStorageFlags.Jewellery | EnumItemStorageFlags.Alchemy | EnumItemStorageFlags.Agriculture | EnumItemStorageFlags.Outfit;

	public override bool Empty => true;

	public ItemSlotGround(InventoryBase inventory)
		: base(inventory)
	{
	}

	public override bool CanTake()
	{
		return false;
	}

	public override ItemStack TakeOut(int quantity)
	{
		return null;
	}

	public override int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		return 0;
	}

	public override bool TryFlipWith(ItemSlot itemSlot)
	{
		return false;
	}
}
