using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCookedContainerBase : BlockContainer, IBlockMealContainer, IContainedInteractable, IContainedCustomName
{
	public virtual string ContainerNameShort => Lang.Get("pot");

	public virtual string ContainerNameShortPlural => Lang.Get("pots");

	public void SetContents(string recipeCode, float servings, ItemStack containerStack, ItemStack[] stacks)
	{
		base.SetContents(containerStack, stacks);
		containerStack.Attributes.SetFloat("quantityServings", servings);
		containerStack.Attributes.SetString("recipeCode", recipeCode);
	}

	public void SetContents(string recipeCode, ItemStack containerStack, ItemStack[] stacks, float quantityServings = 1f)
	{
		base.SetContents(containerStack, stacks);
		if (recipeCode == null)
		{
			containerStack.Attributes.RemoveAttribute("recipeCode");
		}
		else
		{
			containerStack.Attributes.SetString("recipeCode", recipeCode);
		}
		containerStack.Attributes.SetFloat("quantityServings", quantityServings);
	}

	public float GetQuantityServings(IWorldAccessor world, ItemStack byItemStack)
	{
		return (float)byItemStack.Attributes.GetDecimal("quantityServings");
	}

	public void SetQuantityServings(IWorldAccessor world, ItemStack byItemStack, float value)
	{
		if (value <= 0f)
		{
			SetRecipeCode(world, byItemStack, null);
		}
		else
		{
			byItemStack.Attributes.SetFloat("quantityServings", value);
		}
	}

	public CookingRecipe GetCookingRecipe(IWorldAccessor world, ItemStack containerStack)
	{
		return api.GetCookingRecipe(GetRecipeCode(world, containerStack));
	}

	public string GetRecipeCode(IWorldAccessor world, ItemStack containerStack)
	{
		return containerStack.Attributes.GetString("recipeCode");
	}

	public void SetRecipeCode(IWorldAccessor world, ItemStack containerStack, string code)
	{
		if (code == null)
		{
			containerStack.Attributes.RemoveAttribute("recipeCode");
			containerStack.Attributes.RemoveAttribute("quantityServings");
			containerStack.Attributes.RemoveAttribute("contents");
		}
		else
		{
			containerStack.Attributes.SetString("recipeCode", code);
		}
	}

	internal float GetServings(IWorldAccessor world, ItemStack byItemStack)
	{
		return (float)byItemStack.Attributes.GetDecimal("quantityServings");
	}

	internal void SetServings(IWorldAccessor world, ItemStack byItemStack, float value)
	{
		byItemStack.Attributes.SetFloat("quantityServings", value);
	}

	internal void SetServingsMaybeEmpty(IWorldAccessor world, ItemSlot potslot, float value)
	{
		SetQuantityServings(world, potslot.Itemstack, value);
		if (!(value <= 0f))
		{
			return;
		}
		string emptyCode = Attributes["emptiedBlockCode"].AsString();
		if (emptyCode != null)
		{
			Block emptyPotBlock = world.GetBlock(new AssetLocation(emptyCode));
			if (emptyPotBlock != null)
			{
				potslot.Itemstack = new ItemStack(emptyPotBlock);
			}
		}
	}

	public CookingRecipe GetMealRecipe(IWorldAccessor world, ItemStack containerStack)
	{
		string recipecode = GetRecipeCode(world, containerStack);
		return api.GetCookingRecipe(recipecode);
	}

	public void ServeIntoBowl(Block selectedBlock, BlockPos pos, ItemSlot potslot, IWorldAccessor world)
	{
		if (world.Side == EnumAppSide.Client)
		{
			return;
		}
		string code = selectedBlock.Attributes["mealBlockCode"].AsString();
		Block mealblock = api.World.GetBlock(new AssetLocation(code));
		world.BlockAccessor.SetBlock(mealblock.BlockId, pos);
		if (api.World.BlockAccessor.GetBlockEntity(pos) is IBlockEntityMealContainer bemeal && !tryMergeServingsIntoBE(bemeal, potslot))
		{
			bemeal.RecipeCode = GetRecipeCode(world, potslot.Itemstack);
			ItemStack[] myStacks = GetNonEmptyContents(api.World, potslot.Itemstack);
			for (int i = 0; i < myStacks.Length; i++)
			{
				bemeal.inventory[i].Itemstack = myStacks[i].Clone();
			}
			float quantityServings = GetServings(world, potslot.Itemstack);
			float servingsToTransfer = (bemeal.QuantityServings = Math.Min(quantityServings, selectedBlock.Attributes["servingCapacity"].AsFloat(1f)));
			SetServingsMaybeEmpty(world, potslot, quantityServings - servingsToTransfer);
			potslot.MarkDirty();
			bemeal.MarkDirty(redrawonclient: true);
		}
	}

	public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority)
	{
		if (sourceStack?.Block is IBlockMealContainer || (sourceStack?.Collectible?.Attributes?.IsTrue("mealContainer")).GetValueOrDefault())
		{
			return Math.Max(1, Math.Min(MaxStackSize - sinkStack.StackSize, sourceStack.StackSize));
		}
		return base.GetMergableQuantity(sinkStack, sourceStack, priority);
	}

	public override void TryMergeStacks(ItemStackMergeOperation op)
	{
		if (op.SourceSlot?.Itemstack?.Block is IBlockMealContainer || (op.SourceSlot?.Itemstack?.Collectible?.Attributes?.IsTrue("mealContainer")).GetValueOrDefault())
		{
			if (op.CurrentPriority != EnumMergePriority.DirectMerge)
			{
				if (Math.Min(MaxStackSize - op.SinkSlot.Itemstack.StackSize, op.SourceSlot.Itemstack.StackSize) > 0)
				{
					base.TryMergeStacks(op);
				}
				return;
			}
			ItemStack bufferStack = null;
			if (op.SourceSlot.Itemstack.StackSize > 1)
			{
				bufferStack = op.SourceSlot.TakeOut(op.SourceSlot.Itemstack.StackSize - 1);
			}
			if (ServeIntoStack(op.SourceSlot, op.SinkSlot, op.World))
			{
				if (!op.ActingPlayer.Entity.TryGiveItemStack(bufferStack))
				{
					op.World.SpawnItemEntity(bufferStack, op.ActingPlayer.Entity.Pos.AsBlockPos);
				}
				return;
			}
			new DummySlot(bufferStack).TryPutInto(op.World, op.SourceSlot);
			if (Math.Min(MaxStackSize - op.SinkSlot.Itemstack.StackSize, op.SourceSlot.Itemstack.StackSize) > 0)
			{
				base.TryMergeStacks(op);
			}
		}
		else
		{
			base.TryMergeStacks(op);
		}
	}

	private bool tryMergeServingsIntoBE(IBlockEntityMealContainer bemeal, ItemSlot potslot)
	{
		ItemStack[] myStacks = GetNonEmptyContents(api.World, potslot.Itemstack);
		string hisRecipeCode = bemeal.RecipeCode;
		ItemStack[] hisStacks = bemeal.GetNonEmptyContentStacks();
		float hisServings = bemeal.QuantityServings;
		string ownRecipeCode = GetRecipeCode(api.World, potslot.Itemstack);
		float servingCapacity = (bemeal as BlockEntity).Block.Attributes["servingCapacity"].AsFloat(1f);
		if (hisStacks == null || hisServings == 0f)
		{
			return false;
		}
		if (myStacks.Length != hisStacks.Length)
		{
			return true;
		}
		if (ownRecipeCode != hisRecipeCode)
		{
			return true;
		}
		float remainingPlaceableServings = servingCapacity - hisServings;
		if (remainingPlaceableServings <= 0f)
		{
			return true;
		}
		for (int j = 0; j < myStacks.Length; j++)
		{
			if (!myStacks[j].Equals(api.World, hisStacks[j], GlobalConstants.IgnoredStackAttributes))
			{
				return true;
			}
		}
		for (int i = 0; i < hisStacks.Length; i++)
		{
			ItemStackMergeOperation op = new ItemStackMergeOperation(api.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.ConfirmedMerge, myStacks[i].StackSize);
			op.SourceSlot = new DummySlot(myStacks[i]);
			op.SinkSlot = new DummySlot(hisStacks[i]);
			hisStacks[i].Collectible.TryMergeStacks(op);
		}
		float quantityServings = GetServings(api.World, potslot.Itemstack);
		float movedservings = Math.Min(remainingPlaceableServings, quantityServings);
		bemeal.QuantityServings = hisServings + movedservings;
		SetServingsMaybeEmpty(api.World, potslot, quantityServings - movedservings);
		potslot.MarkDirty();
		bemeal.MarkDirty(redrawonclient: true);
		return true;
	}

	public bool ServeIntoStack(ItemSlot bowlSlot, ItemSlot potslot, IWorldAccessor world)
	{
		if (world.Side == EnumAppSide.Client)
		{
			return true;
		}
		float quantityServings = GetServings(world, potslot.Itemstack);
		string ownRecipeCode = GetRecipeCode(world, potslot.Itemstack);
		float servingCapacity = bowlSlot.Itemstack.Block.Attributes["servingCapacity"].AsFloat(1f);
		if (bowlSlot.Itemstack.Block is IBlockMealContainer)
		{
			IBlockMealContainer mealcont = bowlSlot.Itemstack.Block as IBlockMealContainer;
			ItemStack[] myStacks = GetNonEmptyContents(api.World, potslot.Itemstack);
			string hisRecipeCode = mealcont.GetRecipeCode(world, bowlSlot.Itemstack);
			ItemStack[] hisStacks = mealcont.GetNonEmptyContents(world, bowlSlot.Itemstack);
			float hisServings = mealcont.GetQuantityServings(world, bowlSlot.Itemstack);
			if (hisStacks != null && hisServings > 0f)
			{
				if (myStacks.Length != hisStacks.Length)
				{
					return false;
				}
				if (ownRecipeCode != hisRecipeCode)
				{
					return false;
				}
				float remainingPlaceableServings = servingCapacity - hisServings;
				if (remainingPlaceableServings <= 0f)
				{
					return false;
				}
				for (int j = 0; j < myStacks.Length; j++)
				{
					if (!myStacks[j].Equals(world, hisStacks[j], GlobalConstants.IgnoredStackAttributes))
					{
						return false;
					}
				}
				for (int i = 0; i < hisStacks.Length; i++)
				{
					ItemStackMergeOperation op = new ItemStackMergeOperation(world, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.ConfirmedMerge, myStacks[i].StackSize);
					op.SourceSlot = new DummySlot(myStacks[i]);
					op.SinkSlot = new DummySlot(hisStacks[i]);
					hisStacks[i].Collectible.TryMergeStacks(op);
				}
				float movedservings = Math.Min(remainingPlaceableServings, quantityServings);
				mealcont.SetQuantityServings(world, bowlSlot.Itemstack, hisServings + movedservings);
				SetServingsMaybeEmpty(world, potslot, quantityServings - movedservings);
				potslot.Itemstack.Attributes.RemoveAttribute("sealed");
				potslot.MarkDirty();
				bowlSlot.MarkDirty();
				return true;
			}
		}
		ItemStack[] stacks = GetContents(api.World, potslot.Itemstack);
		string code = bowlSlot.Itemstack.Block.Attributes["mealBlockCode"].AsString();
		if (code == null)
		{
			return false;
		}
		Block block = api.World.GetBlock(new AssetLocation(code));
		float servingsToTransfer = Math.Min(quantityServings, servingCapacity);
		ItemStack stack = new ItemStack(block);
		(block as IBlockMealContainer).SetContents(ownRecipeCode, stack, stacks, servingsToTransfer);
		SetServingsMaybeEmpty(world, potslot, quantityServings - servingsToTransfer);
		potslot.Itemstack.Attributes.RemoveAttribute("sealed");
		potslot.MarkDirty();
		bowlSlot.Itemstack = stack;
		bowlSlot.MarkDirty();
		return true;
	}

	public string GetContainedName(ItemSlot inSlot, int quantity)
	{
		if (quantity != 1)
		{
			return Lang.Get("{0} {1}", quantity, ContainerNameShortPlural);
		}
		return Lang.Get("{0} {1}", quantity, ContainerNameShort);
	}

	public string GetContainedInfo(ItemSlot inSlot)
	{
		IWorldAccessor world = api.World;
		CookingRecipe recipe = GetMealRecipe(world, inSlot.Itemstack);
		float servings = inSlot.Itemstack.Attributes.GetFloat("quantityServings");
		ItemStack[] stacks = GetNonEmptyContents(world, inSlot.Itemstack);
		if (stacks.Length == 0)
		{
			return Lang.Get("Empty {0}", ContainerNameShort);
		}
		if (recipe != null)
		{
			string outputName = recipe.GetOutputName(world, stacks);
			string message;
			if (recipe.CooksInto != null)
			{
				message = "contained-nonfood-portions";
				int index = outputName.IndexOf('\n');
				if (index > 0)
				{
					outputName = outputName.Substring(0, index);
				}
			}
			else
			{
				message = ((servings == 1f) ? "contained-food-servings-singular" : "contained-food-servings-plural");
			}
			return Lang.Get(message, Math.Round(servings, 1), outputName, ContainerNameShort, PerishableInfoCompactContainer(api, inSlot));
		}
		StringBuilder sb = new StringBuilder();
		ItemStack[] array = stacks;
		foreach (ItemStack stack in array)
		{
			if (sb.Length > 0)
			{
				sb.Append(", ");
			}
			sb.Append(stack.GetName());
		}
		string str = Lang.Get("contained-foodstacks-insideof", sb.ToString(), ContainerNameShort);
		sb.Clear();
		sb.Append(str);
		sb.Append(PerishableInfoCompactContainer(api, inSlot));
		return sb.ToString();
	}

	public bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot targetSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (targetSlot.Empty)
		{
			return false;
		}
		JsonObject attributes = targetSlot.Itemstack.Collectible.Attributes;
		if (((attributes != null && attributes.IsTrue("mealContainer")) || targetSlot.Itemstack.Block is IBlockMealContainer) && GetServings(api.World, slot.Itemstack) > 0f)
		{
			if (targetSlot.StackSize > 1)
			{
				targetSlot = new DummySlot(targetSlot.TakeOut(1));
				byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
				ServeIntoStack(targetSlot, slot, api.World);
				if (!byPlayer.InventoryManager.TryGiveItemstack(targetSlot.Itemstack, slotNotifyEffect: true))
				{
					api.World.SpawnItemEntity(targetSlot.Itemstack, byPlayer.Entity.ServerPos.XYZ);
				}
			}
			else
			{
				ServeIntoStack(targetSlot, slot, api.World);
			}
			slot.MarkDirty();
			be.MarkDirty(redrawOnClient: true);
			return true;
		}
		return false;
	}

	public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		return false;
	}

	public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
	}
}
