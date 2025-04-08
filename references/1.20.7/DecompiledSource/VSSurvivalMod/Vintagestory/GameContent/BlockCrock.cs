using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockCrock : BlockCookedContainerBase, IBlockMealContainer, IContainedMeshSource
{
	private string[] vegetableLabels = new string[13]
	{
		"carrot", "cabbage", "onion", "parsnip", "turnip", "pumpkin", "soybean", "bellpepper", "cassava", "mushroom",
		"redmeat", "poultry", "porridge"
	};

	public override string ContainerNameShort => Lang.Get("crock");

	public override string ContainerNameShortPlural => Lang.Get("crocks");

	public override float GetContainingTransitionModifierContained(IWorldAccessor world, ItemSlot inSlot, EnumTransitionType transType)
	{
		float mul = 1f;
		if (transType == EnumTransitionType.Perish)
		{
			mul = ((!inSlot.Itemstack.Attributes.GetBool("sealed")) ? (mul * 0.85f) : ((inSlot.Itemstack.Attributes.GetString("recipeCode") == null) ? (mul * 0.25f) : (mul * 0.1f)));
		}
		return mul;
	}

	public override float GetContainingTransitionModifierPlaced(IWorldAccessor world, BlockPos pos, EnumTransitionType transType)
	{
		float mul = 1f;
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCrock becrock))
		{
			return mul;
		}
		if (transType == EnumTransitionType.Perish)
		{
			mul = ((!becrock.Sealed) ? (mul * 0.85f) : ((becrock.RecipeCode == null) ? (mul * 0.25f) : (mul * 0.1f)));
		}
		return mul;
	}

	public AssetLocation LabelForContents(string recipeCode, ItemStack[] contents)
	{
		string label;
		if (recipeCode != null && recipeCode.Length > 0)
		{
			AssetLocation code = getMostCommonMealIngredient(contents);
			if (code != null && (label = CodeToLabel(code)) != null)
			{
				return AssetLocation.Create("shapes/block/clay/crock/label-" + label + ".json", Code.Domain);
			}
			return AssetLocation.Create("shapes/block/clay/crock/label-meal.json", Code.Domain);
		}
		if (contents == null || contents.Length == 0 || contents[0] == null)
		{
			return AssetLocation.Create("shapes/block/clay/crock/label-empty.json", Code.Domain);
		}
		if (MealMeshCache.ContentsRotten(contents))
		{
			return AssetLocation.Create("shapes/block/clay/crock/label-rot.json", Code.Domain);
		}
		label = CodeToLabel(contents[0].Collectible.Code) ?? "empty";
		return AssetLocation.Create("shapes/block/clay/crock/label-" + label + ".json", Code.Domain);
	}

	public string CodeToLabel(AssetLocation loc)
	{
		string type = null;
		string[] array = vegetableLabels;
		foreach (string label in array)
		{
			if (loc.Path.Contains(label))
			{
				type = label;
				break;
			}
		}
		return type;
	}

	private AssetLocation getMostCommonMealIngredient(ItemStack[] contents)
	{
		Dictionary<AssetLocation, int> sdf = new Dictionary<AssetLocation, int>();
		foreach (ItemStack stack in contents)
		{
			sdf.TryGetValue(stack.Collectible.Code, out var cnt);
			sdf[stack.Collectible.Code] = 1 + cnt;
		}
		AssetLocation key = sdf.Aggregate((KeyValuePair<AssetLocation, int> l, KeyValuePair<AssetLocation, int> r) => (l.Value <= r.Value) ? r : l).Key;
		if (sdf[key] < 3)
		{
			return null;
		}
		return key;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack stack = new ItemStack(world.GetBlock(CodeWithVariant("side", "east")));
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCrock becrock)
		{
			ItemStack[] contents = becrock.GetContentStacks();
			for (int i = 0; i < contents.Length; i++)
			{
				if (contents[i] != null)
				{
					SetContents(stack, contents);
					if (becrock.RecipeCode != null)
					{
						stack.Attributes.SetString("recipeCode", becrock.RecipeCode);
						stack.Attributes.SetFloat("quantityServings", becrock.QuantityServings);
						stack.Attributes.SetBool("sealed", becrock.Sealed);
					}
				}
			}
		}
		return stack;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		ItemStack[] contents = GetNonEmptyContents(capi.World, itemstack);
		string recipeCode = itemstack.Attributes.GetString("recipeCode");
		AssetLocation loc = LabelForContents(recipeCode, contents);
		if (!(loc == null))
		{
			Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.GetOrCreate(capi, "blockcrockGuiMeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());
			string key = Code.ToShortString() + loc.ToShortString();
			if (!meshrefs.TryGetValue(key, out var meshref))
			{
				MeshData mesh = GenMesh(capi, loc, new Vec3f(0f, 270f, 0f));
				meshref = (meshrefs[key] = capi.Render.UploadMultiTextureMesh(mesh));
			}
			renderinfo.ModelRef = meshref;
		}
	}

	public virtual string GetMeshCacheKey(ItemStack itemstack)
	{
		ItemStack[] contents = GetNonEmptyContents(api.World, itemstack);
		string recipeCode = itemstack.Attributes.GetString("recipeCode");
		AssetLocation loc = LabelForContents(recipeCode, contents);
		return Code.ToShortString() + loc.ToShortString();
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos forBlockPos = null)
	{
		ItemStack[] contents = GetNonEmptyContents(api.World, itemstack);
		string recipeCode = itemstack.Attributes.GetString("recipeCode");
		return GenMesh(api as ICoreClientAPI, LabelForContents(recipeCode, contents));
	}

	public MeshData GenMesh(ICoreClientAPI capi, AssetLocation labelLoc, Vec3f rot = null)
	{
		ITesselatorAPI tesselator = capi.Tesselator;
		Shape baseshape = Vintagestory.API.Common.Shape.TryGet(capi, AssetLocation.Create("shapes/block/clay/crock/base.json", Code.Domain));
		Shape labelshape = Vintagestory.API.Common.Shape.TryGet(capi, labelLoc);
		tesselator.TesselateShape(this, baseshape, out var mesh, rot);
		tesselator.TesselateShape(this, labelshape, out var labelmesh, rot);
		mesh.AddMeshData(labelmesh);
		return mesh;
	}

	public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe)
	{
		foreach (ItemSlot slot in allInputslots)
		{
			if (slot.Itemstack?.Collectible is BlockCrock)
			{
				outputSlot.Itemstack.Attributes = slot.Itemstack.Attributes.Clone();
				outputSlot.Itemstack.Attributes.SetBool("sealed", value: true);
			}
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel?.Position == null)
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		Block block = api.World.BlockAccessor.GetBlock(blockSel.Position);
		float quantityServings = (float)slot.Itemstack.Attributes.GetDecimal("quantityServings");
		if (block != null && (block.Attributes?.IsTrue("mealContainer")).GetValueOrDefault())
		{
			if (byEntity.Controls.ShiftKey)
			{
				if (quantityServings > 0f)
				{
					ServeIntoBowl(block, blockSel.Position, slot, byEntity.World);
				}
				handHandling = EnumHandHandling.PreventDefault;
			}
			return;
		}
		if (block is BlockGroundStorage)
		{
			if (!byEntity.Controls.ShiftKey)
			{
				return;
			}
			BlockEntityGroundStorage begs = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityGroundStorage;
			ItemSlot gsslot = begs.GetSlotAt(blockSel);
			if (gsslot == null || gsslot.Empty)
			{
				return;
			}
			JsonObject itemAttributes = gsslot.Itemstack.ItemAttributes;
			if (itemAttributes != null && itemAttributes.IsTrue("mealContainer"))
			{
				if (quantityServings > 0f)
				{
					ServeIntoStack(gsslot, slot, byEntity.World);
					gsslot.MarkDirty();
					begs.updateMeshes();
					begs.MarkDirty(redrawOnClient: true);
				}
				handHandling = EnumHandHandling.PreventDefault;
				return;
			}
		}
		if (block is BlockCookingContainer && slot.Itemstack.Attributes.HasAttribute("recipeCode"))
		{
			handHandling = EnumHandHandling.PreventDefault;
			((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			string recipeCode = slot.Itemstack.Attributes.GetString("recipeCode");
			float movedServings = (block as BlockCookingContainer).PutMeal(blockSel.Position, GetNonEmptyContents(api.World, slot.Itemstack), recipeCode, quantityServings);
			quantityServings -= movedServings;
			if (quantityServings > 0f)
			{
				slot.Itemstack.Attributes.SetFloat("quantityServings", quantityServings);
				return;
			}
			slot.Itemstack.Attributes.RemoveAttribute("recipeCode");
			slot.Itemstack.Attributes.RemoveAttribute("quantityServings");
			slot.Itemstack.Attributes.RemoveAttribute("contents");
		}
		else if (block is BlockBarrel)
		{
			if (api.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBarrel bebarrel)
			{
				ItemStack stack = bebarrel.Inventory[0].Itemstack;
				ItemStack[] ownContentStacks = GetNonEmptyContents(api.World, slot.Itemstack);
				if (ownContentStacks == null || ownContentStacks.Length == 0)
				{
					if (stack != null)
					{
						JsonObject attributes = stack.Collectible.Attributes;
						if (attributes != null && attributes.IsTrue("crockable"))
						{
							float servingCapacity = slot.Itemstack.Block.Attributes["servingCapacity"].AsFloat(1f);
							ItemStack foodstack = bebarrel.Inventory[0].TakeOut((int)servingCapacity * 4);
							float servingSize = (float)foodstack.StackSize / 4f;
							foodstack.StackSize = Math.Max(0, foodstack.StackSize / 4);
							SetContents(null, slot.Itemstack, new ItemStack[1] { foodstack }, servingSize);
							bebarrel.MarkDirty(redrawOnClient: true);
							slot.MarkDirty();
						}
					}
				}
				else if (ownContentStacks.Length == 1 && slot.Itemstack.Attributes.GetString("recipeCode") == null)
				{
					ItemStack foodstack2 = ownContentStacks[0].Clone();
					foodstack2.StackSize = (int)((float)foodstack2.StackSize * quantityServings);
					new DummySlot(foodstack2).TryPutInto(api.World, bebarrel.Inventory[0], foodstack2.StackSize);
					foodstack2.StackSize = (int)((float)foodstack2.StackSize / quantityServings);
					if (foodstack2.StackSize <= 0)
					{
						SetContents(slot.Itemstack, new ItemStack[0]);
					}
					else
					{
						SetContents(slot.Itemstack, new ItemStack[1] { foodstack2 });
					}
					bebarrel.MarkDirty(redrawOnClient: true);
					slot.MarkDirty();
				}
			}
			handHandling = EnumHandHandling.PreventDefault;
			((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		}
		else
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!hotbarSlot.Empty)
		{
			JsonObject attributes = hotbarSlot.Itemstack.Collectible.Attributes;
			if (attributes != null && attributes.IsTrue("mealContainer") && (!(hotbarSlot.Itemstack.Collectible is BlockCrock) || hotbarSlot.StackSize == 1))
			{
				if (!(world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityCrock bec))
				{
					return false;
				}
				if (hotbarSlot.Itemstack.Attributes.GetDecimal("quantityServings") == 0.0)
				{
					bec.ServeInto(byPlayer, hotbarSlot);
					return true;
				}
				if (bec.QuantityServings == 0f)
				{
					ServeIntoBowl(this, blockSel.Position, hotbarSlot, world);
					bec.Sealed = false;
					bec.MarkDirty(redrawOnClient: true);
					return true;
				}
			}
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		BlockMeal mealblock = world.GetBlock(new AssetLocation("bowl-meal")) as BlockMeal;
		CookingRecipe recipe = GetCookingRecipe(world, inSlot.Itemstack);
		ItemStack[] stacks = GetNonEmptyContents(world, inSlot.Itemstack);
		if (stacks == null || stacks.Length == 0)
		{
			dsc.AppendLine(Lang.Get("Empty"));
			if (inSlot.Itemstack.Attributes.GetBool("sealed"))
			{
				dsc.AppendLine("<font color=\"lightgreen\">" + Lang.Get("Sealed.") + "</font>");
			}
			return;
		}
		DummyInventory dummyInv = new DummyInventory(api);
		ItemSlot mealSlot = GetDummySlotForFirstPerishableStack(api.World, stacks, null, dummyInv);
		dummyInv.OnAcquireTransitionSpeed += delegate(EnumTransitionType transType, ItemStack stack, float mul)
		{
			float num = mul * GetContainingTransitionModifierContained(world, inSlot, transType);
			if (inSlot.Inventory != null)
			{
				num *= inSlot.Inventory.GetTransitionSpeedMul(transType, inSlot.Itemstack);
			}
			return num;
		};
		if (recipe != null)
		{
			double servings = inSlot.Itemstack.Attributes.GetDecimal("quantityServings");
			if (recipe != null)
			{
				if (servings == 1.0)
				{
					dsc.AppendLine(Lang.Get("{0} serving of {1}", Math.Round(servings, 1), recipe.GetOutputName(world, stacks)));
				}
				else
				{
					dsc.AppendLine(Lang.Get("{0} servings of {1}", Math.Round(servings, 1), recipe.GetOutputName(world, stacks)));
				}
			}
			string facts = mealblock.GetContentNutritionFacts(world, inSlot, null);
			if (facts != null)
			{
				dsc.Append(facts);
			}
		}
		else if (inSlot.Itemstack.Attributes.HasAttribute("quantityServings"))
		{
			double servings2 = inSlot.Itemstack.Attributes.GetDecimal("quantityServings");
			dsc.AppendLine(Lang.Get("{0} servings left", Math.Round(servings2, 1)));
		}
		else
		{
			dsc.AppendLine(Lang.Get("Contents:"));
			ItemStack[] array = stacks;
			foreach (ItemStack stack2 in array)
			{
				if (stack2 != null)
				{
					dsc.AppendLine(stack2.StackSize + "x  " + stack2.GetName());
				}
			}
		}
		mealSlot.Itemstack?.Collectible.AppendPerishableInfoText(mealSlot, dsc, world);
		if (inSlot.Itemstack.Attributes.GetBool("sealed"))
		{
			dsc.AppendLine("<font color=\"lightgreen\">" + Lang.Get("Sealed.") + "</font>");
		}
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCrock becrock))
		{
			return "";
		}
		BlockMeal mealblock = world.GetBlock(new AssetLocation("bowl-meal")) as BlockMeal;
		CookingRecipe recipe = api.GetCookingRecipe(becrock.RecipeCode);
		ItemStack[] stacks = (from slot in becrock.inventory
			where !slot.Empty
			select slot.Itemstack).ToArray();
		if (stacks == null || stacks.Length == 0)
		{
			return Lang.Get("Empty");
		}
		StringBuilder dsc = new StringBuilder();
		if (recipe != null)
		{
			ItemSlot slot2 = GetDummySlotForFirstPerishableStack(api.World, stacks, forPlayer.Entity, becrock.inventory);
			if (recipe != null)
			{
				if (becrock.QuantityServings == 1f)
				{
					dsc.AppendLine(Lang.Get("{0} serving of {1}", Math.Round(becrock.QuantityServings, 1), recipe.GetOutputName(world, stacks)));
				}
				else
				{
					dsc.AppendLine(Lang.Get("{0} servings of {1}", Math.Round(becrock.QuantityServings, 1), recipe.GetOutputName(world, stacks)));
				}
			}
			string facts = mealblock.GetContentNutritionFacts(world, new DummySlot(OnPickBlock(world, pos)), null);
			if (facts != null)
			{
				dsc.Append(facts);
			}
			slot2.Itemstack?.Collectible.AppendPerishableInfoText(slot2, dsc, world);
		}
		else
		{
			dsc.AppendLine("Contents:");
			ItemStack[] array = stacks;
			foreach (ItemStack stack in array)
			{
				if (stack != null)
				{
					dsc.AppendLine(stack.StackSize + "x  " + stack.GetName());
				}
			}
			becrock.inventory[0].Itemstack?.Collectible.AppendPerishableInfoText(becrock.inventory[0], dsc, api.World);
		}
		if (becrock.Sealed)
		{
			dsc.AppendLine("<font color=\"lightgreen\">" + Lang.Get("Sealed.") + "</font>");
		}
		return dsc.ToString();
	}

	public static ItemSlot GetDummySlotForFirstPerishableStack(IWorldAccessor world, ItemStack[] stacks, Entity forEntity, InventoryBase slotInventory)
	{
		ItemStack stack = null;
		if (stacks != null)
		{
			for (int i = 0; i < stacks.Length; i++)
			{
				if (stacks[i] != null)
				{
					TransitionableProperties[] props = stacks[i].Collectible.GetTransitionableProperties(world, stacks[i], forEntity);
					if (props != null && props.Length != 0)
					{
						stack = stacks[i];
						break;
					}
				}
			}
		}
		DummySlot dummySlot = new DummySlot(stack, slotInventory);
		dummySlot.MarkedDirty += () => true;
		return dummySlot;
	}

	public override TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
	{
		TransitionState[] result = base.UpdateAndGetTransitionStates(world, inslot);
		ItemStack[] stacks = GetNonEmptyContents(world, inslot.Itemstack);
		if (MealMeshCache.ContentsRotten(stacks))
		{
			inslot.Itemstack.Attributes.RemoveAttribute("recipeCode");
			inslot.Itemstack.Attributes?.RemoveAttribute("quantityServings");
		}
		if (stacks == null || stacks.Length == 0)
		{
			inslot.Itemstack.Attributes.RemoveAttribute("recipeCode");
			ITreeAttribute attributes = inslot.Itemstack.Attributes;
			if (attributes == null)
			{
				return result;
			}
			attributes.RemoveAttribute("quantityServings");
		}
		return result;
	}

	public override void OnGroundIdle(EntityItem entityItem)
	{
		base.OnGroundIdle(entityItem);
		IWorldAccessor world = entityItem.World;
		if (world.Side != EnumAppSide.Server || !entityItem.Swimming || !(world.Rand.NextDouble() < 0.01))
		{
			return;
		}
		ItemStack[] stacks = GetContents(world, entityItem.Itemstack);
		if (!MealMeshCache.ContentsRotten(stacks))
		{
			return;
		}
		for (int i = 0; i < stacks.Length; i++)
		{
			if (stacks[i] != null && stacks[i].StackSize > 0 && stacks[i].Collectible.Code.Path == "rot")
			{
				world.SpawnItemEntity(stacks[i], entityItem.ServerPos.XYZ);
			}
		}
		entityItem.Itemstack.Attributes.RemoveAttribute("sealed");
		entityItem.Itemstack.Attributes.RemoveAttribute("recipeCode");
		entityItem.Itemstack.Attributes.RemoveAttribute("quantityServings");
		entityItem.Itemstack.Attributes.RemoveAttribute("contents");
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (!(api is ICoreClientAPI capi) || !capi.ObjectCache.TryGetValue("blockcrockGuiMeshRefs", out var obj))
		{
			return;
		}
		foreach (KeyValuePair<string, MultiTextureMeshRef> item in obj as Dictionary<string, MultiTextureMeshRef>)
		{
			item.Value.Dispose();
		}
		capi.ObjectCache.Remove("blockcrockGuiMeshRefs");
	}
}
