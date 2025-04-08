namespace Vintagestory.API.Common;

public class ItemSlotBagContent : ItemSlotSurvival
{
	public int BagIndex;

	public int SlotIndex;

	public EnumItemStorageFlags storageType;

	public override EnumItemStorageFlags StorageType => storageType;

	public ItemSlotBagContent(InventoryBase inventory, int BagIndex, int SlotIndex, EnumItemStorageFlags storageType)
		: base(inventory)
	{
		this.BagIndex = BagIndex;
		this.storageType = storageType;
		this.SlotIndex = SlotIndex;
	}
}
