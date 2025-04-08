using System;

namespace Vintagestory.API.Common;

public class ItemSlotCharacter : ItemSlot
{
	private EnumCharacterDressType type;

	public override EnumItemStorageFlags StorageType => EnumItemStorageFlags.Outfit;

	public ItemSlotCharacter(EnumCharacterDressType type, InventoryBase inventory)
		: base(inventory)
	{
		this.type = type;
	}

	public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
	{
		if (!IsDressType(sourceSlot.Itemstack, type))
		{
			return false;
		}
		return base.CanTakeFrom(sourceSlot, priority);
	}

	public override bool CanHold(ItemSlot itemstackFromSourceSlot)
	{
		if (!IsDressType(itemstackFromSourceSlot.Itemstack, type))
		{
			return false;
		}
		return base.CanHold(itemstackFromSourceSlot);
	}

	/// <summary>
	/// Checks to see what dress type the given item is.
	/// </summary>
	/// <param name="itemstack"></param>
	/// <param name="dressType"></param>
	/// <returns></returns>
	public static bool IsDressType(IItemStack itemstack, EnumCharacterDressType dressType)
	{
		if (itemstack == null || itemstack.Collectible.Attributes == null)
		{
			return false;
		}
		string stackDressType = itemstack.Collectible.Attributes["clothescategory"].AsString() ?? itemstack.Collectible.Attributes["attachableToEntity"]["categoryCode"].AsString();
		if (stackDressType != null)
		{
			return dressType.ToString().Equals(stackDressType, StringComparison.InvariantCultureIgnoreCase);
		}
		return false;
	}
}
