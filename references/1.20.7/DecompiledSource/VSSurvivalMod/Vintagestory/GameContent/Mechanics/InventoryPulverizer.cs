using Vintagestory.API.Common;

namespace Vintagestory.GameContent.Mechanics;

public class InventoryPulverizer : InventoryDisplayed
{
	public InventoryPulverizer(BlockEntity be, int size)
		: base(be, size, "pulverizer-0", null)
	{
		slots = GenEmptySlots(size);
		for (int i = 0; i < size; i++)
		{
			slots[i].MaxSlotStackSize = 1;
		}
	}

	public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
	{
		if (targetSlot == slots[slots.Length - 1])
		{
			return 0f;
		}
		return base.GetSuitability(sourceSlot, targetSlot, isMerge);
	}
}
