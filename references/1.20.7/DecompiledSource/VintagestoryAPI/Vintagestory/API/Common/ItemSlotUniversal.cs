namespace Vintagestory.API.Common;

/// <summary>
/// A universal item slot type that can hold anything.
/// </summary>
public class ItemSlotUniversal : ItemSlot
{
	public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.General | EnumItemStorageFlags.Backpack | EnumItemStorageFlags.Metallurgy | EnumItemStorageFlags.Jewellery | EnumItemStorageFlags.Alchemy | EnumItemStorageFlags.Agriculture | EnumItemStorageFlags.Currency | EnumItemStorageFlags.Outfit;

	public ItemSlotUniversal(InventoryBase inventory)
		: base(inventory)
	{
	}
}
