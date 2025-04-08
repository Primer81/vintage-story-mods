using Vintagestory.API.Config;

namespace Vintagestory.API.Common;

public class ItemSlotInput : ItemSlot
{
	public int outputSlotId;

	public bool IsCookingContainer
	{
		get
		{
			ItemStack itemStack = base.Itemstack;
			if (itemStack == null)
			{
				return false;
			}
			return (itemStack.ItemAttributes?.KeyExists("cookingContainerSlots")).GetValueOrDefault();
		}
	}

	public override int GetRemainingSlotSpace(ItemStack forItemstack)
	{
		if (IsCookingContainer)
		{
			return 0;
		}
		if (Empty && forItemstack != null && (forItemstack.ItemAttributes?.KeyExists("cookingContainerSlots")).GetValueOrDefault())
		{
			return 1;
		}
		return base.GetRemainingSlotSpace(forItemstack);
	}

	public ItemSlotInput(InventoryBase inventory, int outputSlotId)
		: base(inventory)
	{
		this.outputSlotId = outputSlotId;
	}

	public override int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
	{
		return base.TryPutInto(sinkSlot, ref op);
	}

	public override bool TryFlipWith(ItemSlot itemSlot)
	{
		if (!itemSlot.Empty)
		{
			ItemStack itemStack = itemSlot.Itemstack;
			if (itemStack != null && (itemStack.ItemAttributes?.KeyExists("cookingContainerSlots")).GetValueOrDefault() && itemSlot.StackSize > 1)
			{
				return false;
			}
		}
		return base.TryFlipWith(itemSlot);
	}

	public override bool CanHold(ItemSlot slot)
	{
		return CanBeStackedWithOutputSlotItem(slot);
	}

	public override bool CanTake()
	{
		return true;
	}

	public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
	{
		if (CanBeStackedWithOutputSlotItem(sourceSlot))
		{
			return base.CanTakeFrom(sourceSlot, priority);
		}
		return false;
	}

	public bool CanBeStackedWithOutputSlotItem(ItemSlot sourceSlot, bool notifySlot = true)
	{
		ItemSlot outslot = inventory[outputSlotId];
		if (outslot.Empty)
		{
			return true;
		}
		ItemStack compareStack = sourceSlot.Itemstack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack;
		if (compareStack == null)
		{
			compareStack = sourceSlot.Itemstack;
		}
		if (!outslot.Itemstack.Equals(inventory.Api.World, compareStack, GlobalConstants.IgnoredStackAttributes))
		{
			outslot.Inventory.PerformNotifySlot(outputSlotId);
			return false;
		}
		return true;
	}
}
