namespace Vintagestory.API.Common;

/// <summary>
/// A slot that can hold mobile containers
/// </summary>
public class ItemSlotBackpack : ItemSlot
{
	public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.Backpack;

	public override int MaxSlotStackSize => 1;

	public ItemSlotBackpack(InventoryBase inventory)
		: base(inventory)
	{
		BackgroundIcon = "basket";
	}
}
