using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockCookedContainer : BlockCookedContainerBase, IInFirepitRendererSupplier, IContainedMeshSource, IContainedInteractable
{
	public static SimpleParticleProperties smokeHeld;

	public static SimpleParticleProperties foodSparks;

	private WorldInteraction[] interactions;

	private MealMeshCache meshCache;

	private float yoff = 2.5f;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		ICoreClientAPI capi = api as ICoreClientAPI;
		meshCache = api.ModLoader.GetModSystem<MealMeshCache>();
		interactions = ObjectCacheUtil.GetOrCreate(api, "cookedContainerBlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				JsonObject attributes = current.Attributes;
				if (attributes != null && attributes.IsTrue("mealContainer"))
				{
					List<ItemStack> handBookStacks = current.GetHandBookStacks(capi);
					if (handBookStacks != null)
					{
						list.AddRange(handBookStacks);
					}
				}
			}
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-cookedcontainer-takefood",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-cookedcontainer-pickup",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right
				}
			};
		});
	}

	static BlockCookedContainer()
	{
		smokeHeld = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(50, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.05f, 0.1f, -0.05f), new Vec3f(0.05f, 0.15f, 0.05f), 1.5f, 0f, 0.25f, 0.35f, EnumParticleModel.Quad);
		smokeHeld.SelfPropelled = true;
		smokeHeld.AddPos.Set(0.1, 0.1, 0.1);
		foodSparks = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(255, 83, 233, 255), new Vec3d(), new Vec3d(), new Vec3f(-3f, 1f, -3f), new Vec3f(3f, 8f, 3f), 0.5f, 1f, 0.25f, 0.25f);
		foodSparks.VertexFlags = 0;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		if (meshCache == null)
		{
			meshCache = capi.ModLoader.GetModSystem<MealMeshCache>();
		}
		CookingRecipe recipe = GetCookingRecipe(capi.World, itemstack);
		ItemStack[] contents = GetNonEmptyContents(capi.World, itemstack);
		MultiTextureMeshRef meshref = meshCache.GetOrCreateMealInContainerMeshRef(this, recipe, contents, new Vec3f(0f, yoff / 16f, 0f));
		if (meshref != null)
		{
			renderinfo.ModelRef = meshref;
		}
	}

	public virtual string GetMeshCacheKey(ItemStack itemstack)
	{
		return meshCache.GetMealHashCode(itemstack).ToString() ?? "";
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos forBlockPos = null)
	{
		return meshCache.GenMealInContainerMesh(this, GetCookingRecipe(api.World, itemstack), GetNonEmptyContents(api.World, itemstack), new Vec3f(0f, yoff / 16f, 0f));
	}

	public override TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
	{
		TransitionState[] result = base.UpdateAndGetTransitionStates(world, inslot);
		ItemStack[] stacks = GetNonEmptyContents(world, inslot.Itemstack);
		if (stacks == null || stacks.Length == 0 || MealMeshCache.ContentsRotten(stacks))
		{
			inslot.Itemstack.Attributes.RemoveAttribute("recipeCode");
			inslot.Itemstack.Attributes?.RemoveAttribute("quantityServings");
		}
		if ((stacks == null || stacks.Length == 0) && Attributes?["emptiedBlockCode"] != null)
		{
			Block block = world.GetBlock(new AssetLocation(Attributes["emptiedBlockCode"].AsString()));
			if (block != null)
			{
				inslot.Itemstack = new ItemStack(block);
				inslot.MarkDirty();
			}
		}
		return result;
	}

	public override string GetHeldItemName(ItemStack itemStack)
	{
		if (MealMeshCache.ContentsRotten(GetContents(api.World, itemStack)))
		{
			return Lang.Get("Pot of rotten food");
		}
		return base.GetHeldItemName(itemStack);
	}

	public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
	{
		if (byEntity.World.Side == EnumAppSide.Client && GetTemperature(byEntity.World, slot.Itemstack) > 50f && byEntity.World.Rand.NextDouble() < 0.07)
		{
			float sideWays = 0.35f;
			IClientWorldAccessor world = byEntity.World as IClientWorldAccessor;
			if (world.Player.Entity == byEntity && world.Player.CameraMode != 0)
			{
				sideWays = 0f;
			}
			Vec3d pos = byEntity.Pos.XYZ.Add(0.0, byEntity.LocalEyePos.Y - 0.5, 0.0).Ahead(0.33000001311302185, byEntity.Pos.Pitch, byEntity.Pos.Yaw).Ahead(sideWays, 0f, byEntity.Pos.Yaw + (float)Math.PI / 2f);
			smokeHeld.MinPos = pos.AddCopy(-0.05, 0.1, -0.05);
			byEntity.World.SpawnParticles(smokeHeld);
		}
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
		if (MealMeshCache.ContentsRotten(stacks))
		{
			for (int i = 0; i < stacks.Length; i++)
			{
				if (stacks[i] != null && stacks[i].StackSize > 0 && stacks[i].Collectible.Code.Path == "rot")
				{
					world.SpawnItemEntity(stacks[i], entityItem.ServerPos.XYZ);
				}
			}
		}
		else
		{
			ItemStack rndStack = stacks[world.Rand.Next(stacks.Length)];
			world.SpawnCubeParticles(entityItem.ServerPos.XYZ, rndStack, 0.3f, 25);
		}
		Block emptyPotBlock = world.GetBlock(new AssetLocation(Attributes["emptiedBlockCode"].AsString()));
		entityItem.Itemstack = new ItemStack(emptyPotBlock);
		entityItem.WatchedAttributes.MarkPathDirty("itemstack");
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack stack = base.OnPickBlock(world, pos);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCookedContainer bec)
		{
			ItemStack[] contentStacks = bec.GetNonEmptyContentStacks();
			SetContents(bec.RecipeCode, bec.QuantityServings, stack, contentStacks);
			float temp = ((contentStacks.Length != 0) ? contentStacks[0].Collectible.GetTemperature(world, contentStacks[0]) : 0f);
			SetTemperature(world, stack, temp, delayCooldown: false);
		}
		return stack;
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (!(api is ICoreClientAPI capi) || !capi.ObjectCache.TryGetValue("cookedMeshRefs", out var obj))
		{
			return;
		}
		foreach (KeyValuePair<int, MultiTextureMeshRef> item in obj as Dictionary<int, MultiTextureMeshRef>)
		{
			item.Value.Dispose();
		}
		capi.ObjectCache.Remove("cookedMeshRefs");
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		float temp = GetTemperature(world, inSlot.Itemstack);
		if (temp > 20f)
		{
			dsc.AppendLine(Lang.Get("Temperature: {0}°C", (int)temp));
		}
		CookingRecipe recipe = GetMealRecipe(world, inSlot.Itemstack);
		float servings = inSlot.Itemstack.Attributes.GetFloat("quantityServings");
		ItemStack[] stacks = GetNonEmptyContents(world, inSlot.Itemstack);
		if (recipe != null)
		{
			string outputName = recipe.GetOutputName(world, stacks);
			string message = ((recipe.CooksInto == null) ? "{0} servings of {1}" : "nonfood-portions");
			dsc.AppendLine(Lang.Get(message, Math.Round(servings, 1), outputName));
		}
		string nutriFacts = (api.World.GetBlock(new AssetLocation("bowl-meal")) as BlockMeal).GetContentNutritionFacts(api.World, inSlot, stacks, null);
		if (nutriFacts != null && recipe?.CooksInto == null)
		{
			dsc.AppendLine(nutriFacts);
		}
		ItemSlot slot = BlockCrock.GetDummySlotForFirstPerishableStack(api.World, stacks, null, inSlot.Inventory);
		slot.Itemstack?.Collectible.AppendPerishableInfoText(slot, dsc, world);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!hotbarSlot.Empty)
		{
			JsonObject attributes = hotbarSlot.Itemstack.Collectible.Attributes;
			if (attributes != null && attributes.IsTrue("mealContainer"))
			{
				if (!(world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityCookedContainer bec))
				{
					return false;
				}
				return bec.ServeInto(byPlayer, hotbarSlot);
			}
		}
		ItemStack stack = OnPickBlock(world, blockSel.Position);
		if (byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
		{
			world.BlockAccessor.SetBlock(0, blockSel.Position);
			world.PlaySoundAt(Sounds.Place, byPlayer, byPlayer);
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel != null)
		{
			BlockPos onBlockPos = blockSel.Position;
			Block block = byEntity.World.BlockAccessor.GetBlock(onBlockPos);
			if (block is BlockClayOven)
			{
				return;
			}
			Block selectedBlock = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
			if (selectedBlock != null && (selectedBlock.Attributes?.IsTrue("mealContainer")).GetValueOrDefault())
			{
				if (byEntity.Controls.ShiftKey)
				{
					ServeIntoBowl(selectedBlock, blockSel.Position, slot, byEntity.World);
					handHandling = EnumHandHandling.PreventDefault;
				}
				return;
			}
			float quantityServings = (float)slot.Itemstack.Attributes.GetDecimal("quantityServings");
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
		}
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (!(capi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityCookedContainer bem))
		{
			return base.GetRandomColor(capi, pos, facing, rndIndex);
		}
		ItemStack[] stacks = bem.GetNonEmptyContentStacks();
		if (stacks == null || stacks.Length == 0)
		{
			return base.GetRandomColor(capi, pos, facing, rndIndex);
		}
		ItemStack rndStack = stacks[capi.World.Rand.Next(stacks.Length)];
		if (capi.World.Rand.NextDouble() < 0.4)
		{
			return capi.BlockTextureAtlas.GetRandomColor(Textures["ceramic"].Baked.TextureSubId);
		}
		if (rndStack.Class == EnumItemClass.Block)
		{
			return rndStack.Block.GetRandomColor(capi, pos, facing, rndIndex);
		}
		return capi.ItemTextureAtlas.GetRandomColor(rndStack.Item.FirstTexture.Baked.TextureSubId, rndIndex);
	}

	public override int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
	{
		ItemStack[] stacks = GetNonEmptyContents(capi.World, stack);
		if (stacks.Length == 0)
		{
			return base.GetRandomColor(capi, stack);
		}
		ItemStack rndStack = stacks[capi.World.Rand.Next(stacks.Length)];
		if (capi.World.Rand.NextDouble() < 0.4)
		{
			return capi.BlockTextureAtlas.GetRandomColor(Textures["ceramic"].Baked.TextureSubId);
		}
		return rndStack.Collectible.GetRandomColor(capi, stack);
	}

	public IInFirepitRenderer GetRendererWhenInFirepit(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
	{
		return new PotInFirepitRenderer(api as ICoreClientAPI, stack, firepit.Pos, forOutputSlot);
	}

	public EnumFirepitModel GetDesiredFirepitModel(ItemStack stack, BlockEntityFirepit firepit, bool forOutputSlot)
	{
		return EnumFirepitModel.Wide;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override bool OnSmeltAttempt(InventoryBase inventorySmelting)
	{
		if (Attributes["isDirtyPot"].AsBool())
		{
			InventorySmelting inventory = (InventorySmelting)inventorySmelting;
			ItemSlot slot = inventory[1];
			int quantityServings = (int)((float)slot.Itemstack.Attributes.GetDecimal("quantityServings") + 0.001f);
			if (quantityServings > 0)
			{
				ItemStack[] myStacks = GetNonEmptyContents(api.World, slot.Itemstack);
				if (myStacks.Length != 0)
				{
					inventory.CookingSlots[0].Itemstack = myStacks[0];
					inventory.CookingSlots[0].Itemstack.StackSize = quantityServings;
				}
			}
			slot.Itemstack = new ItemStack(api.World.GetBlock(new AssetLocation(Attributes["emptiedBlockCode"].AsString())));
			return true;
		}
		return false;
	}
}
