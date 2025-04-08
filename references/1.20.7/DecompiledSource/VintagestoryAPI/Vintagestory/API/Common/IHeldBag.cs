using System.Collections.Generic;

namespace Vintagestory.API.Common;

/// <summary>
/// Bag is a non-placed block container, usually one that is attached to an entity
/// </summary>
public interface IHeldBag
{
	/// <summary>
	/// Should true if this this bag is empty
	/// </summary>
	/// <param name="bagstack"></param>
	/// <returns></returns>
	bool IsEmpty(ItemStack bagstack);

	/// <summary>
	/// Amount of slots this bag provides
	/// </summary>
	/// <param name="bagstack"></param>
	/// <returns></returns>
	int GetQuantitySlots(ItemStack bagstack);

	/// <summary>
	/// Should return all contents of this bag
	/// </summary>
	/// <param name="bagstack"></param>
	/// <param name="world"></param>
	/// <returns></returns>
	ItemStack[] GetContents(ItemStack bagstack, IWorldAccessor world);

	List<ItemSlotBagContent> GetOrCreateSlots(ItemStack bagstack, InventoryBase parentinv, int bagIndex, IWorldAccessor world);

	/// <summary>
	/// Save given itemstack into this bag
	/// </summary>
	/// <param name="bagstack"></param>
	/// <param name="slot"></param>
	void Store(ItemStack bagstack, ItemSlotBagContent slot);

	/// <summary>
	/// Delete all contents of this bag
	/// </summary>
	/// <param name="bagstack"></param>
	void Clear(ItemStack bagstack);

	/// <summary>
	/// The Hex color the bag item slot should take, return null for default
	/// </summary>
	/// <param name="bagstack"></param>
	/// <returns></returns>
	string GetSlotBgColor(ItemStack bagstack);

	/// <summary>
	/// The types of items that can be stored in this bag
	/// </summary>
	/// <param name="bagstack"></param>
	/// <returns></returns>
	EnumItemStorageFlags GetStorageFlags(ItemStack bagstack);
}
