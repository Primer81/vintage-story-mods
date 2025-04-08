using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockContainer : Block
{
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
		if (stacks == null || stacks.Length == 0)
		{
			containerStack.Attributes.RemoveAttribute("contents");
			return;
		}
		TreeAttribute stacksTree = new TreeAttribute();
		for (int i = 0; i < stacks.Length; i++)
		{
			stacksTree[i.ToString() ?? ""] = new ItemstackAttribute(stacks[i]);
		}
		containerStack.Attributes["contents"] = stacksTree;
	}

	public virtual ItemStack[] GetContents(IWorldAccessor world, ItemStack itemstack)
	{
		ITreeAttribute treeAttr = itemstack?.Attributes?.GetTreeAttribute("contents");
		if (treeAttr == null)
		{
			return ResolveUcontents(world, itemstack);
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

	public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
	{
		ResolveUcontents(api.World, thisStack);
		if (otherStack.Collectible is BlockContainer)
		{
			ResolveUcontents(api.World, otherStack);
		}
		return base.Equals(thisStack, otherStack, ignoreAttributeSubTrees);
	}

	protected ItemStack[] ResolveUcontents(IWorldAccessor world, ItemStack itemstack)
	{
		if (itemstack != null && itemstack.Attributes.HasAttribute("ucontents"))
		{
			List<ItemStack> stacks = new List<ItemStack>();
			TreeAttribute[] value = (itemstack.Attributes["ucontents"] as TreeArrayAttribute).value;
			foreach (ITreeAttribute stackAttr in value)
			{
				stacks.Add(CreateItemStackFromJson(stackAttr, world, Code.Domain));
			}
			ItemStack[] stacksAsArray = stacks.ToArray();
			SetContents(itemstack, stacksAsArray);
			itemstack.Attributes.RemoveAttribute("ucontents");
			return stacksAsArray;
		}
		return new ItemStack[0];
	}

	public virtual ItemStack CreateItemStackFromJson(ITreeAttribute stackAttr, IWorldAccessor world, string defaultDomain)
	{
		AssetLocation loc = AssetLocation.Create(stackAttr.GetString("code"), defaultDomain);
		CollectibleObject collObj = ((!(stackAttr.GetString("type") == "item")) ? ((CollectibleObject)world.GetBlock(loc)) : ((CollectibleObject)world.GetItem(loc)));
		ItemStack stack = new ItemStack(collObj, (int)stackAttr.GetDecimal("quantity", 1.0));
		ITreeAttribute attr = (stackAttr["attributes"] as TreeAttribute)?.Clone();
		if (attr != null)
		{
			stack.Attributes = attr;
		}
		return stack;
	}

	public bool IsEmpty(ItemStack itemstack)
	{
		ITreeAttribute treeAttr = itemstack?.Attributes?.GetTreeAttribute("contents");
		if (treeAttr == null)
		{
			return true;
		}
		foreach (KeyValuePair<string, IAttribute> item in treeAttr)
		{
			if ((item.Value as ItemstackAttribute).value != null)
			{
				return false;
			}
		}
		return true;
	}

	public virtual ItemStack[] GetNonEmptyContents(IWorldAccessor world, ItemStack itemstack)
	{
		return GetContents(world, itemstack)?.Where((ItemStack stack) => stack != null).ToArray();
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack stack = base.OnPickBlock(world, pos);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer bec)
		{
			SetContents(stack, bec.GetContentStacks());
		}
		return stack;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		bool preventDefault = false;
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior obj in blockBehaviors)
		{
			EnumHandling handled = EnumHandling.PassThrough;
			obj.OnBlockBroken(world, pos, byPlayer, ref handled);
			if (handled == EnumHandling.PreventDefault)
			{
				preventDefault = true;
			}
			if (handled == EnumHandling.PreventSubsequent)
			{
				return;
			}
		}
		if (preventDefault)
		{
			return;
		}
		if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
		{
			ItemStack[] drops = new ItemStack[1] { OnPickBlock(world, pos) };
			for (int i = 0; i < drops.Length; i++)
			{
				world.SpawnItemEntity(drops[i], pos);
			}
			world.PlaySoundAt(Sounds.GetBreakSound(byPlayer), pos, -0.5, byPlayer);
		}
		if (EntityClass != null)
		{
			world.BlockAccessor.GetBlockEntity(pos)?.OnBlockBroken(byPlayer);
		}
		world.BlockAccessor.SetBlock(0, pos);
	}

	public override TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
	{
		if (inslot is ItemSlotCreative)
		{
			return base.UpdateAndGetTransitionStates(world, inslot);
		}
		ItemStack[] stacks = GetContents(world, inslot.Itemstack);
		if (stacks != null)
		{
			for (int i = 0; i < stacks.Length; i++)
			{
				ItemStack stack = stacks[i];
				if (stack != null)
				{
					ItemSlot dummySlot = GetContentInDummySlot(inslot, stack);
					stack.Collectible.UpdateAndGetTransitionStates(world, dummySlot);
					if (dummySlot.Itemstack == null)
					{
						stacks[i] = null;
					}
				}
			}
		}
		SetContents(inslot.Itemstack, stacks);
		return base.UpdateAndGetTransitionStates(world, inslot);
	}

	protected virtual ItemSlot GetContentInDummySlot(ItemSlot inslot, ItemStack itemstack)
	{
		DummyInventory dummyInv = new DummyInventory(api);
		DummySlot dummySlot = new DummySlot(itemstack, dummyInv);
		dummySlot.MarkedDirty += delegate
		{
			inslot.Inventory?.DidModifyItemSlot(inslot);
			return true;
		};
		dummyInv.OnAcquireTransitionSpeed += delegate(EnumTransitionType transType, ItemStack stack, float mulByConfig)
		{
			float num = mulByConfig;
			if (inslot.Inventory != null)
			{
				num = inslot.Inventory.InvokeTransitionSpeedDelegates(transType, stack, mulByConfig);
			}
			return num * GetContainingTransitionModifierContained(api.World, inslot, transType);
		};
		return dummySlot;
	}

	public override void SetTemperature(IWorldAccessor world, ItemStack itemstack, float temperature, bool delayCooldown = true)
	{
		ItemStack[] stacks = GetContents(world, itemstack);
		if (stacks != null)
		{
			for (int i = 0; i < stacks.Length; i++)
			{
				stacks[i]?.Collectible.SetTemperature(world, stacks[i], temperature, delayCooldown);
			}
		}
		base.SetTemperature(world, itemstack, temperature, delayCooldown);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}

	public string PerishableInfoCompactContainer(ICoreAPI api, ItemSlot inSlot)
	{
		IWorldAccessor world = api.World;
		ItemStack[] stacks = GetNonEmptyContents(world, inSlot.Itemstack);
		DummyInventory dummyInv = new DummyInventory(api);
		ItemSlot slot = BlockCrock.GetDummySlotForFirstPerishableStack(api.World, stacks, null, dummyInv);
		dummyInv.OnAcquireTransitionSpeed += delegate(EnumTransitionType transType, ItemStack stack, float mul)
		{
			float num = mul * GetContainingTransitionModifierContained(world, inSlot, transType);
			if (inSlot.Inventory != null)
			{
				num *= inSlot.Inventory.GetTransitionSpeedMul(transType, inSlot.Itemstack);
			}
			return num;
		};
		return BlockEntityShelf.PerishableInfoCompact(api, slot, 0f, withStackName: false).Replace("\r\n", "");
	}

	public override bool RequiresTransitionableTicking(IWorldAccessor world, ItemStack itemstack)
	{
		ItemStack[] stacks = GetNonEmptyContents(world, itemstack);
		for (int i = 0; i < stacks.Length; i++)
		{
			TransitionableProperties[] props = stacks[i].Collectible.GetTransitionableProperties(world, stacks[i], null);
			if (props != null && props.Length != 0)
			{
				return true;
			}
		}
		return base.RequiresTransitionableTicking(world, itemstack);
	}
}
