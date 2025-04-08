namespace Vintagestory.API.Common;

/// <summary>
/// Return false to stop walking the inventory
/// </summary>
/// <param name="slot"></param>
/// <returns></returns>
public delegate bool OnInventorySlot(ItemSlot slot);
