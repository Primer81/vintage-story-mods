using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class CollectibleBehaviorContainer : CollectibleBehavior
{
	public CollectibleBehaviorContainer(CollectibleObject collObj)
		: base(collObj)
	{
	}

	public virtual float GetContainingTransitionModifierContained(IWorldAccessor world, ItemSlot inSlot, EnumTransitionType transType)
	{
		return 1f;
	}

	public virtual float GetContainingTransitionModifierPlaced(IWorldAccessor world, BlockPos pos, EnumTransitionType transType)
	{
		return 1f;
	}

	public virtual void SetContents(ItemStack containerStack, ItemStack[] stacks)
	{
		TreeAttribute stacksTree = new TreeAttribute();
		for (int i = 0; i < stacks.Length; i++)
		{
			stacksTree[i.ToString() ?? ""] = new ItemstackAttribute(stacks[i]);
		}
		containerStack.Attributes["contents"] = stacksTree;
	}

	public virtual ItemStack[] GetContents(IWorldAccessor world, ItemStack itemstack)
	{
		ITreeAttribute treeAttr = itemstack.Attributes.GetTreeAttribute("contents");
		if (treeAttr == null)
		{
			return new ItemStack[0];
		}
		ItemStack[] stacks = new ItemStack[treeAttr.Count];
		foreach (KeyValuePair<string, IAttribute> val in treeAttr)
		{
			ItemStack stack = (val.Value as ItemstackAttribute).value;
			stack?.ResolveBlockOrItem(world);
			if (int.TryParse(val.Key, out var index))
			{
				stacks[index] = stack;
			}
		}
		return stacks;
	}

	public virtual ItemStack[] GetNonEmptyContents(IWorldAccessor world, ItemStack itemstack)
	{
		return (from stack in GetContents(world, itemstack)
			where stack != null
			select stack).ToArray();
	}
}
