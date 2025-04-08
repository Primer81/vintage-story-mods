using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockPie : BlockMeal, IBakeableCallback
{
	private MealMeshCache ms;

	private WorldInteraction[] interactions;

	private ModelTransform oneSliceTranformGui = new ModelTransform
	{
		Origin = new Vec3f(0.375f, 0.1f, 0.375f),
		Scale = 2.82f,
		Rotation = new Vec3f(-27f, 132f, -5f)
	}.EnsureDefaultValues();

	private ModelTransform oneSliceTranformTp = new ModelTransform
	{
		Translation = new Vec3f(-0.82f, -0.34f, -0.57f),
		Origin = new Vec3f(0.5f, 0.13f, 0.5f),
		Scale = 0.7f,
		Rotation = new Vec3f(-49f, 29f, -112f)
	}.EnsureDefaultValues();

	public string State => Variant["state"];

	protected override bool PlacedBlockEating => false;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		InteractionHelpYOffset = 0.375f;
		interactions = ObjectCacheUtil.GetOrCreate(api, "pieInteractions-", delegate
		{
			ItemStack[] knifeStacks = BlockUtil.GetKnifeStacks(api);
			List<ItemStack> list = new List<ItemStack>();
			List<ItemStack> list2 = new List<ItemStack>();
			if (list.Count == 0 && list2.Count == 0)
			{
				foreach (CollectibleObject current in api.World.Collectibles)
				{
					if (current is ItemDough)
					{
						list2.Add(new ItemStack(current, 2));
					}
					if (current.Attributes?["inPieProperties"]?.AsObject<InPieProperties>(null, current.Code.Domain) != null && !(current is ItemDough))
					{
						list.Add(new ItemStack(current, 2));
					}
				}
			}
			return new WorldInteraction[4]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-pie-cut",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = knifeStacks,
					GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
					{
						BlockEntityPie blockEntityPie4 = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityPie;
						return (blockEntityPie4?.Inventory[0]?.Itemstack != null && (blockEntityPie4.Inventory[0].Itemstack.Collectible as BlockPie).State != "raw" && blockEntityPie4.SlicesLeft > 1) ? wi.Itemstacks : null;
					}
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-pie-addfilling",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
					{
						BlockEntityPie blockEntityPie3 = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityPie;
						return (blockEntityPie3?.Inventory[0]?.Itemstack != null && (blockEntityPie3.Inventory[0].Itemstack.Collectible as BlockPie).State == "raw" && !blockEntityPie3.HasAllFilling) ? wi.Itemstacks : null;
					}
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-pie-addcrust",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list2.ToArray(),
					GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
					{
						BlockEntityPie blockEntityPie2 = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityPie;
						return (blockEntityPie2?.Inventory[0]?.Itemstack != null && (blockEntityPie2.Inventory[0].Itemstack.Collectible as BlockPie).State == "raw" && blockEntityPie2.HasAllFilling && !blockEntityPie2.HasCrust) ? wi.Itemstacks : null;
					}
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-pie-changecruststyle",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = knifeStacks,
					GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
					{
						BlockEntityPie blockEntityPie = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityPie;
						return (blockEntityPie?.Inventory[0]?.Itemstack != null && (blockEntityPie.Inventory[0].Itemstack.Collectible as BlockPie).State == "raw" && blockEntityPie.HasCrust) ? wi.Itemstacks : null;
					}
				}
			};
		});
		ms = api.ModLoader.GetModSystem<MealMeshCache>();
		displayContentsInfo = false;
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (canEat(slot))
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (!canEat(slot))
		{
			return false;
		}
		return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (canEat(slot))
		{
			base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
		}
	}

	protected bool canEat(ItemSlot slot)
	{
		if (slot.Itemstack.Attributes.GetAsInt("pieSize") == 1)
		{
			return State != "raw";
		}
		return false;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
		if (itemstack.Attributes.GetAsInt("pieSize") == 1)
		{
			if (target == EnumItemRenderTarget.Gui)
			{
				renderinfo.Transform = oneSliceTranformGui;
			}
			if (target == EnumItemRenderTarget.HandTp)
			{
				renderinfo.Transform = oneSliceTranformTp;
			}
		}
		renderinfo.ModelRef = ms.GetOrCreatePieMeshRef(itemstack);
	}

	public override MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos = null)
	{
		return ms.GetPieMesh(itemstack);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		BlockEntityPie bec = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPie;
		if (bec?.Inventory[0]?.Itemstack != null)
		{
			return bec.Inventory[0].Itemstack.Clone();
		}
		return base.OnPickBlock(world, pos);
	}

	public void OnBaked(ItemStack oldStack, ItemStack newStack)
	{
		newStack.Attributes["contents"] = oldStack.Attributes["contents"];
		newStack.Attributes.SetInt("pieSize", oldStack.Attributes.GetAsInt("pieSize"));
		newStack.Attributes.SetInt("topCrustType", oldStack.Attributes.GetAsInt("topCrustType"));
		newStack.Attributes.SetInt("bakeLevel", oldStack.Attributes.GetAsInt("bakeLevel") + 1);
		ItemStack[] stacks = GetContents(api.World, newStack);
		for (int i = 0; i < stacks.Length; i++)
		{
			CombustibleProperties props = stacks[i]?.Collectible?.CombustibleProps;
			if (props != null)
			{
				ItemStack cookedStack = props.SmeltedStack?.ResolvedItemstack.Clone();
				TransitionState state = UpdateAndGetTransitionState(api.World, new DummySlot(cookedStack), EnumTransitionType.Perish);
				if (state != null)
				{
					TransitionState smeltedState = cookedStack.Collectible.UpdateAndGetTransitionState(api.World, new DummySlot(cookedStack), EnumTransitionType.Perish);
					float nowTransitionedHours = state.TransitionedHours / (state.TransitionHours + state.FreshHours) * 0.8f * (smeltedState.TransitionHours + smeltedState.FreshHours) - 1f;
					cookedStack.Collectible.SetTransitionState(cookedStack, EnumTransitionType.Perish, Math.Max(0f, nowTransitionedHours));
				}
			}
		}
		TransitionableProperties perishProps = newStack.Collectible.GetTransitionableProperties(api.World, newStack, null).FirstOrDefault((TransitionableProperties p) => p.Type == EnumTransitionType.Perish);
		perishProps.TransitionedStack.Resolve(api.World, "pie perished stack");
		DummyInventory inv = new DummyInventory(api, 4);
		inv[0].Itemstack = stacks[0];
		inv[1].Itemstack = stacks[1];
		inv[2].Itemstack = stacks[2];
		inv[3].Itemstack = stacks[3];
		CollectibleObject.CarryOverFreshness(api, inv.Slots, stacks, perishProps);
		SetContents(newStack, stacks);
	}

	public void TryPlacePie(EntityAgent byEntity, BlockSelection blockSel)
	{
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		InPieProperties pieprops = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.ItemAttributes["inPieProperties"]?.AsObject<InPieProperties>();
		if (pieprops != null && pieprops.PartType == EnumPiePartType.Crust)
		{
			BlockPos abovePos = blockSel.Position.UpCopy();
			if (api.World.BlockAccessor.GetBlock(abovePos).Replaceable >= 6000)
			{
				api.World.BlockAccessor.SetBlock(Id, abovePos);
				(api.World.BlockAccessor.GetBlockEntity(abovePos) as BlockEntityPie).OnPlaced(byPlayer);
			}
		}
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		BlockEntityPie bec = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPie;
		if (bec?.Inventory[0]?.Itemstack != null)
		{
			return GetHeldItemName(bec.Inventory[0].Itemstack);
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		ItemStack[] cStacks = GetContents(api.World, itemStack);
		if (cStacks.Length <= 1)
		{
			return Lang.Get("pie-empty");
		}
		ItemStack cstack = cStacks[1];
		if (cstack == null)
		{
			return Lang.Get("pie-empty");
		}
		bool equal = true;
		int i = 2;
		while (equal && i < cStacks.Length - 1)
		{
			if (cStacks[i] != null)
			{
				equal &= cstack.Equals(api.World, cStacks[i], GlobalConstants.IgnoredStackAttributes);
				cstack = cStacks[i];
			}
			i++;
		}
		string state = Variant["state"];
		if (MealMeshCache.ContentsRotten(cStacks))
		{
			return Lang.Get("pie-single-rotten");
		}
		if (equal)
		{
			return Lang.Get("pie-single-" + cstack.Collectible.Code.ToShortString() + "-" + state);
		}
		return Lang.Get("pie-mixed-" + (cStacks[1].Collectible.NutritionProps?.FoodCategory ?? (cStacks[1].ItemAttributes?["nutritionPropsWhenInMeal"]?.AsObject<FoodNutritionProperties>()?.FoodCategory).GetValueOrDefault(EnumFoodCategory.Vegetable)).ToString().ToLowerInvariant() + "-" + state);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		int pieSie = inSlot.Itemstack.Attributes.GetAsInt("pieSize");
		ItemStack pieStack = inSlot.Itemstack;
		float servingsLeft = GetQuantityServings(world, inSlot.Itemstack);
		if (!inSlot.Itemstack.Attributes.HasAttribute("quantityServings"))
		{
			servingsLeft = 1f;
		}
		if (pieSie == 1)
		{
			dsc.AppendLine(Lang.Get("pie-slice-single", servingsLeft));
		}
		else
		{
			dsc.AppendLine(Lang.Get("pie-slices", pieSie));
		}
		TransitionableProperties[] propsm = pieStack.Collectible.GetTransitionableProperties(api.World, pieStack, null);
		if (propsm != null && propsm.Length != 0)
		{
			pieStack.Collectible.AppendPerishableInfoText(inSlot, dsc, api.World);
		}
		ItemStack[] stacks = GetContents(api.World, pieStack);
		EntityPlayer forEntity = (world as IClientWorldAccessor)?.Player?.Entity;
		float[] nmul = GetNutritionHealthMul(null, inSlot, forEntity);
		dsc.AppendLine(GetContentNutritionFacts(api.World, inSlot, stacks, null, mulWithStacksize: true, servingsLeft * nmul[0], servingsLeft * nmul[1]));
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		BlockEntityPie bep = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPie;
		if (bep?.Inventory == null || bep.Inventory.Count < 1 || bep.Inventory.Empty)
		{
			return "";
		}
		BlockMeal mealblock = api.World.GetBlock(new AssetLocation("bowl-meal")) as BlockMeal;
		ItemStack pieStack = bep.Inventory[0].Itemstack;
		ItemStack[] stacks = GetContents(api.World, pieStack);
		StringBuilder sb = new StringBuilder();
		TransitionableProperties[] propsm = pieStack.Collectible.GetTransitionableProperties(api.World, pieStack, null);
		if (propsm != null && propsm.Length != 0)
		{
			pieStack.Collectible.AppendPerishableInfoText(bep.Inventory[0], sb, api.World);
		}
		float servingsLeft = GetQuantityServings(world, bep.Inventory[0].Itemstack);
		if (!bep.Inventory[0].Itemstack.Attributes.HasAttribute("quantityServings"))
		{
			servingsLeft = (float)bep.SlicesLeft / 4f;
		}
		float[] nmul = GetNutritionHealthMul(pos, null, forPlayer.Entity);
		return sb.ToString() + mealblock.GetContentNutritionFacts(api.World, bep.Inventory[0], stacks, null, mulWithStacksize: true, nmul[0] * servingsLeft, nmul[1] * servingsLeft);
	}

	protected override TransitionState[] UpdateAndGetTransitionStatesNative(IWorldAccessor world, ItemSlot inslot)
	{
		return base.UpdateAndGetTransitionStatesNative(world, inslot);
	}

	public override TransitionState UpdateAndGetTransitionState(IWorldAccessor world, ItemSlot inslot, EnumTransitionType type)
	{
		ItemStack[] cstacks = GetContents(world, inslot.Itemstack);
		UnspoilContents(world, cstacks);
		SetContents(inslot.Itemstack, cstacks);
		return base.UpdateAndGetTransitionState(world, inslot, type);
	}

	public override TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
	{
		ItemStack[] cstacks = GetContents(world, inslot.Itemstack);
		UnspoilContents(world, cstacks);
		SetContents(inslot.Itemstack, cstacks);
		return base.UpdateAndGetTransitionStatesNative(world, inslot);
	}

	public override string GetContentNutritionFacts(IWorldAccessor world, ItemSlot inSlotorFirstSlot, ItemStack[] contentStacks, EntityAgent forEntity, bool mulWithStacksize = false, float nutritionMul = 1f, float healthMul = 1f)
	{
		UnspoilContents(world, contentStacks);
		return base.GetContentNutritionFacts(world, inSlotorFirstSlot, contentStacks, forEntity, mulWithStacksize, nutritionMul, healthMul);
	}

	protected void UnspoilContents(IWorldAccessor world, ItemStack[] cstacks)
	{
		foreach (ItemStack cstack in cstacks)
		{
			if (cstack == null)
			{
				continue;
			}
			if (!(cstack.Attributes["transitionstate"] is ITreeAttribute))
			{
				cstack.Attributes["transitionstate"] = new TreeAttribute();
			}
			ITreeAttribute attr = (ITreeAttribute)cstack.Attributes["transitionstate"];
			if (attr.HasAttribute("createdTotalHours"))
			{
				attr.SetDouble("createdTotalHours", world.Calendar.TotalHours);
				attr.SetDouble("lastUpdatedTotalHours", world.Calendar.TotalHours);
				float[] transitionedHours = (attr["transitionedHours"] as FloatArrayAttribute)?.value;
				int j = 0;
				while (transitionedHours != null && j < transitionedHours.Length)
				{
					transitionedHours[j] = 0f;
					j++;
				}
			}
		}
	}

	public override float[] GetNutritionHealthMul(BlockPos pos, ItemSlot slot, EntityAgent forEntity)
	{
		float satLossMul = 1f;
		if (slot == null && pos != null)
		{
			slot = (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPie).Inventory[0];
		}
		if (slot != null)
		{
			satLossMul = GlobalConstants.FoodSpoilageSatLossMul(slot.Itemstack.Collectible.UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish)?.TransitionLevel ?? 0f, slot.Itemstack, forEntity);
		}
		return new float[2]
		{
			Attributes["nutritionMul"].AsFloat(1f) * satLossMul,
			satLossMul
		};
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!(world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityPie).OnInteract(byPlayer))
		{
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		return true;
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
	}

	public override int GetRandomContentColor(ICoreClientAPI capi, ItemStack[] stacks)
	{
		ItemStack[] cstacks = GetContents(capi.World, stacks[0]);
		if (cstacks.Length == 0)
		{
			return 0;
		}
		ItemStack rndStack = cstacks[capi.World.Rand.Next(stacks.Length)];
		return rndStack.Collectible.GetRandomColor(capi, rndStack);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		WorldInteraction[] baseinteractions = base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
		baseinteractions = baseinteractions.RemoveEntry(1);
		return interactions.Append(baseinteractions);
	}
}
