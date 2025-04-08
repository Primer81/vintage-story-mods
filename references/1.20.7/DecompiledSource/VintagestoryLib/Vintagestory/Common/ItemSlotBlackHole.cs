using Vintagestory.API.Common;

namespace Vintagestory.Common;

internal class ItemSlotBlackHole : ItemSlot
{
	public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.General | EnumItemStorageFlags.Backpack | EnumItemStorageFlags.Metallurgy | EnumItemStorageFlags.Jewellery | EnumItemStorageFlags.Alchemy | EnumItemStorageFlags.Agriculture | EnumItemStorageFlags.Outfit;

	public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
	{
		return base.CanTakeFrom(sourceSlot, priority);
	}

	public ItemSlotBlackHole(InventoryBase inventory)
		: base(inventory)
	{
	}

	public override void OnItemSlotModified(ItemStack sinkStack)
	{
		base.OnItemSlotModified(sinkStack);
		itemstack = null;
	}
}
