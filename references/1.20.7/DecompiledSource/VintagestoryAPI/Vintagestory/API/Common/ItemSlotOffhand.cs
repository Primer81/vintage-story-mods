namespace Vintagestory.API.Common;

/// <summary>
/// A slot that only accepts collectibles designated for the off-hand slot
/// </summary>
public class ItemSlotOffhand : ItemSlot
{
	public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.Offhand;

	public ItemSlotOffhand(InventoryBase inventory)
		: base(inventory)
	{
		BackgroundIcon = "offhand";
	}
}
