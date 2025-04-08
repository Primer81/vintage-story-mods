using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCookingContainer : Block, IInFirepitRendererSupplier
{
	public int MaxServingSize = 6;

	private Cuboidi attachmentArea;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		attachmentArea = Attributes?["attachmentArea"].AsObject<Cuboidi>();
		MaxServingSize = Attributes?["maxServingSize"].AsInt(6) ?? 6;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!hotbarSlot.Empty)
		{
			JsonObject attributes = hotbarSlot.Itemstack.Collectible.Attributes;
			if (attributes != null && attributes.IsTrue("handleCookingContainerInteract"))
			{
				EnumHandHandling handling = EnumHandHandling.NotHandled;
				hotbarSlot.Itemstack.Collectible.OnHeldInteractStart(hotbarSlot, byPlayer.Entity, blockSel, null, firstEvent: true, ref handling);
				if (handling == EnumHandHandling.PreventDefault || handling == EnumHandHandling.PreventDefaultAction)
				{
					return true;
				}
			}
		}
		ItemStack stack = OnPickBlock(world, blockSel.Position);
		if (byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
		{
			world.BlockAccessor.SetBlock(0, blockSel.Position);
			world.PlaySoundAt(Sounds.Place, byPlayer, byPlayer);
			return true;
		}
		return false;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			failureCode = "onlywhensneaking";
			return false;
		}
		if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode) && world.BlockAccessor.GetBlock(blockSel.Position.DownCopy()).CanAttachBlockAt(world.BlockAccessor, this, blockSel.Position.DownCopy(), BlockFacing.UP, attachmentArea))
		{
			DoPlaceBlock(world, byPlayer, blockSel, itemstack);
			return true;
		}
		return false;
	}

	public override float GetMeltingDuration(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
	{
		float duration = 0f;
		ItemStack[] stacks = GetCookingStacks(cookingSlotsProvider, clone: false);
		foreach (ItemStack stack in stacks)
		{
			int portionSize = stack.StackSize;
			if (stack.Collectible?.CombustibleProps == null)
			{
				JsonObject attributes = stack.Collectible.Attributes;
				if (attributes != null && attributes["waterTightContainerProps"].Exists)
				{
					WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(stack);
					portionSize = (int)((float)stack.StackSize / props.ItemsPerLitre);
				}
				duration += (float)(20 * portionSize);
			}
			else
			{
				float singleDuration = stack.Collectible.GetMeltingDuration(world, cookingSlotsProvider, inputSlot);
				duration += singleDuration * (float)portionSize / (float)stack.Collectible.CombustibleProps.SmeltedRatio;
			}
		}
		return Math.Max(40f, duration / 3f);
	}

	public override float GetMeltingPoint(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
	{
		float meltpoint = 0f;
		ItemStack[] stacks = GetCookingStacks(cookingSlotsProvider, clone: false);
		for (int i = 0; i < stacks.Length; i++)
		{
			meltpoint = Math.Max(meltpoint, stacks[i].Collectible.GetMeltingPoint(world, cookingSlotsProvider, inputSlot));
		}
		return Math.Max(100f, meltpoint);
	}

	public override bool CanSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemStack inputStack, ItemStack outputStack)
	{
		ItemStack[] stacks = GetCookingStacks(cookingSlotsProvider, clone: false);
		if (GetMatchingCookingRecipe(world, stacks) != null)
		{
			return true;
		}
		return false;
	}

	public override void DoSmelt(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot, ItemSlot outputSlot)
	{
		ItemStack[] stacks = GetCookingStacks(cookingSlotsProvider);
		CookingRecipe recipe = GetMatchingCookingRecipe(world, stacks);
		Block block = world.GetBlock(CodeWithVariant("type", "cooked"));
		if (recipe == null)
		{
			return;
		}
		int quantityServings = recipe.GetQuantityServings(stacks);
		if (recipe.CooksInto != null)
		{
			ItemStack outstack = recipe.CooksInto.ResolvedItemstack?.Clone();
			if (outstack != null)
			{
				outstack.StackSize *= quantityServings;
				stacks = new ItemStack[1] { outstack };
				block = world.GetBlock(new AssetLocation(Attributes["dirtiedBlockCode"].AsString()));
			}
		}
		else
		{
			for (int k = 0; k < stacks.Length; k++)
			{
				ItemStack cookedStack = recipe.GetIngrendientFor(stacks[k]).GetMatchingStack(stacks[k])?.CookedStack?.ResolvedItemstack.Clone();
				if (cookedStack != null)
				{
					stacks[k] = cookedStack;
				}
			}
		}
		ItemStack outputStack = new ItemStack(block);
		outputStack.Collectible.SetTemperature(world, outputStack, GetIngredientsTemperature(world, stacks));
		TransitionableProperties cookedPerishProps = recipe.PerishableProps.Clone();
		cookedPerishProps.TransitionedStack.Resolve(world, "cooking container perished stack");
		CollectibleObject.CarryOverFreshness(api, cookingSlotsProvider.Slots, stacks, cookedPerishProps);
		if (recipe.CooksInto != null)
		{
			for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
			{
				cookingSlotsProvider.Slots[i].Itemstack = ((i == 0) ? stacks[0] : null);
			}
			inputSlot.Itemstack = outputStack;
			return;
		}
		for (int j = 0; j < cookingSlotsProvider.Slots.Length; j++)
		{
			cookingSlotsProvider.Slots[j].Itemstack = null;
		}
		((BlockCookedContainer)block).SetContents(recipe.Code, quantityServings, outputStack, stacks);
		outputSlot.Itemstack = outputStack;
		inputSlot.Itemstack = null;
	}

	internal float PutMeal(BlockPos pos, ItemStack[] itemStack, string recipeCode, float quantityServings)
	{
		Block block = api.World.GetBlock(CodeWithVariant("type", "cooked"));
		api.World.BlockAccessor.SetBlock(block.Id, pos);
		float servingsToTransfer = Math.Min(quantityServings, Attributes["servingCapacity"].AsInt(1));
		BlockEntityCookedContainer be = api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityCookedContainer;
		be.RecipeCode = recipeCode;
		be.QuantityServings = quantityServings;
		for (int i = 0; i < itemStack.Length; i++)
		{
			be.inventory[i].Itemstack = itemStack[i];
		}
		be.MarkDirty(redrawOnClient: true);
		return servingsToTransfer;
	}

	public string GetOutputText(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot inputSlot)
	{
		if (inputSlot.Itemstack == null)
		{
			return null;
		}
		if (!(inputSlot.Itemstack.Collectible is BlockCookingContainer))
		{
			return null;
		}
		ItemStack[] stacks = GetCookingStacks(cookingSlotsProvider);
		CookingRecipe recipe = GetMatchingCookingRecipe(world, stacks);
		if (recipe != null)
		{
			double quantity = recipe.GetQuantityServings(stacks);
			string outputName = recipe.GetOutputName(world, stacks);
			string message;
			if (recipe.CooksInto != null)
			{
				ItemStack outStack = recipe.CooksInto.ResolvedItemstack;
				message = "mealcreation-nonfood";
				quantity *= (double)recipe.CooksInto.Quantity;
				outputName = outStack?.GetName().ToLowerInvariant();
				if (outStack != null && (outStack.Collectible.Attributes?["waterTightContainerProps"].Exists).GetValueOrDefault())
				{
					float litreFloat = (float)quantity / BlockLiquidContainerBase.GetContainableProps(outStack).ItemsPerLitre;
					string litres = ((!((double)litreFloat < 0.1)) ? Lang.Get("{0:0.##} L", litreFloat) : Lang.Get("{0} mL", (int)(litreFloat * 1000f)));
					return Lang.Get("mealcreation-nonfood-liquid", litres, outputName);
				}
			}
			else
			{
				message = ((quantity == 1.0) ? "mealcreation-makesingular" : "mealcreation-makeplural");
			}
			return Lang.Get(message, (int)quantity, outputName.ToLower());
		}
		return null;
	}

	public CookingRecipe GetMatchingCookingRecipe(IWorldAccessor world, ItemStack[] stacks)
	{
		List<CookingRecipe> recipes = world.Api.GetCookingRecipes();
		if (recipes == null)
		{
			return null;
		}
		bool isDirtyPot = Attributes["isDirtyPot"].AsBool();
		foreach (CookingRecipe recipe in recipes)
		{
			if ((!isDirtyPot || recipe.CooksInto != null) && recipe.Matches(stacks) && recipe.GetQuantityServings(stacks) <= MaxServingSize)
			{
				return recipe;
			}
		}
		return null;
	}

	public static float GetIngredientsTemperature(IWorldAccessor world, ItemStack[] ingredients)
	{
		bool haveStack = false;
		float lowestTemp = 0f;
		for (int i = 0; i < ingredients.Length; i++)
		{
			if (ingredients[i] != null)
			{
				float stackTemp = ingredients[i].Collectible.GetTemperature(world, ingredients[i]);
				lowestTemp = (haveStack ? Math.Min(lowestTemp, stackTemp) : stackTemp);
				haveStack = true;
			}
		}
		return lowestTemp;
	}

	public ItemStack[] GetCookingStacks(ISlotProvider cookingSlotsProvider, bool clone = true)
	{
		List<ItemStack> stacks = new List<ItemStack>(4);
		for (int i = 0; i < cookingSlotsProvider.Slots.Length; i++)
		{
			ItemStack stack = cookingSlotsProvider.Slots[i].Itemstack;
			if (stack != null)
			{
				stacks.Add(clone ? stack.Clone() : stack);
			}
		}
		return stacks.ToArray();
	}

	public IInFirepitRenderer GetRendererWhenInFirepit(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
	{
		return new PotInFirepitRenderer(api as ICoreClientAPI, stack, firepit.Pos, forOutputSlot);
	}

	public EnumFirepitModel GetDesiredFirepitModel(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
	{
		return EnumFirepitModel.Wide;
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		return capi.BlockTextureAtlas.GetRandomColor(Textures["ceramic"].Baked.TextureSubId, rndIndex);
	}

	public override int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
	{
		return capi.BlockTextureAtlas.GetRandomColor(Textures["ceramic"].Baked.TextureSubId);
	}
}
