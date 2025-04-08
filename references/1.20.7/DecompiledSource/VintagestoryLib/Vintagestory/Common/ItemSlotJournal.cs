using Vintagestory.API.Common;

namespace Vintagestory.Common;

public class ItemSlotJournal : ItemSlot
{
	public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.Currency;

	public ItemSlotJournal(InventoryBase inventory)
		: base(inventory)
	{
	}
}
