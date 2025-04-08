namespace Vintagestory.API.Common;

public class ItemSlotSkill : ItemSlot
{
	public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.Skill;

	public ItemSlotSkill(InventoryBase inventory)
		: base(inventory)
	{
		BackgroundIcon = "skill";
	}
}
