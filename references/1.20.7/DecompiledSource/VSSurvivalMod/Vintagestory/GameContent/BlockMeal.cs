using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockMeal : BlockContainer, IBlockMealContainer, IContainedMeshSource, IContainedInteractable, IContainedCustomName
{
	private MealMeshCache meshCache;

	protected bool displayContentsInfo = true;

	protected virtual bool PlacedBlockEating => true;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		meshCache = api.ModLoader.GetModSystem<MealMeshCache>();
	}

	public override string GetHeldTpUseAnimation(ItemSlot activeHotbarSlot, Entity forEntity)
	{
		return "eat";
	}

	public virtual float[] GetNutritionHealthMul(BlockPos pos, ItemSlot slot, EntityAgent forEntity)
	{
		return new float[2] { 1f, 1f };
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
			BlockCookedContainer.smokeHeld.MinPos = pos.AddCopy(-0.05, 0.1, -0.05);
			byEntity.World.SpawnParticles(BlockCookedContainer.smokeHeld);
		}
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (!tryHeldBeginEatMeal(slot, byEntity, ref handHandling))
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
		}
	}

	public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		return tryHeldContinueEatMeal(secondsUsed, slot, byEntity);
	}

	public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		tryFinishEatMeal(secondsUsed, slot, byEntity, handleAllServingsConsumed: true);
	}

	protected virtual bool tryHeldBeginEatMeal(ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handHandling)
	{
		if (!byEntity.Controls.ShiftKey && GetContentNutritionProperties(api.World, slot, byEntity) != null)
		{
			byEntity.World.RegisterCallback(delegate
			{
				if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
				{
					byEntity.PlayEntitySound("eat", (byEntity as EntityPlayer)?.Player);
				}
			}, 500);
			handHandling = EnumHandHandling.PreventDefault;
			return true;
		}
		return false;
	}

	protected bool tryPlacedBeginEatMeal(ItemSlot slot, IPlayer byPlayer)
	{
		if (GetContentNutritionProperties(api.World, slot, byPlayer.Entity) != null)
		{
			api.World.RegisterCallback(delegate
			{
				if (byPlayer.Entity.Controls.HandUse == EnumHandInteract.BlockInteract)
				{
					byPlayer.Entity.PlayEntitySound("eat", byPlayer);
				}
			}, 500);
			byPlayer.Entity.StartAnimation("eat");
			return true;
		}
		return false;
	}

	protected virtual bool tryHeldContinueEatMeal(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
	{
		if (GetContentNutritionProperties(byEntity.World, slot, byEntity) == null)
		{
			return false;
		}
		Vec3d pos = byEntity.Pos.AheadCopy(0.4000000059604645).XYZ.Add(byEntity.LocalEyePos);
		pos.Y -= 0.4000000059604645;
		IPlayer player = (byEntity as EntityPlayer).Player;
		if (secondsUsed > 0.5f && (int)(30f * secondsUsed) % 7 == 1)
		{
			ItemStack[] contents = GetNonEmptyContents(byEntity.World, slot.Itemstack);
			if (contents.Length != 0)
			{
				ItemStack rndStack = contents[byEntity.World.Rand.Next(contents.Length)];
				byEntity.World.SpawnCubeParticles(pos, rndStack, 0.3f, 4, 1f, player);
			}
		}
		if (byEntity.World is IClientWorldAccessor)
		{
			ModelTransform tf = new ModelTransform();
			tf.Origin.Set(1.1f, 0.5f, 0.5f);
			tf.EnsureDefaultValues();
			tf.Translation.X -= Math.Min(1.7f, secondsUsed * 4f * 1.8f) / FpHandTransform.ScaleXYZ.X;
			tf.Translation.Y += Math.Min(0.4f, secondsUsed * 1.8f) / FpHandTransform.ScaleXYZ.X;
			tf.Scale = 1f + Math.Min(0.5f, secondsUsed * 4f * 1.8f) / FpHandTransform.ScaleXYZ.X;
			tf.Rotation.X += Math.Min(40f, secondsUsed * 350f * 0.75f) / FpHandTransform.ScaleXYZ.X;
			if (secondsUsed > 0.5f)
			{
				tf.Translation.Y += GameMath.Sin(30f * secondsUsed) / 10f / FpHandTransform.ScaleXYZ.Y;
			}
			byEntity.Controls.UsingHeldItemTransformBefore = tf;
			return secondsUsed <= 1.5f;
		}
		return true;
	}

	protected bool tryPlacedContinueEatMeal(float secondsUsed, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			return false;
		}
		if (GetContentNutritionProperties(api.World, slot, byPlayer.Entity) == null)
		{
			return false;
		}
		ItemStack stack = slot.Itemstack;
		if (api.Side == EnumAppSide.Client)
		{
			ModelTransform tf = new ModelTransform();
			tf.Origin.Set(1.1f, 0.5f, 0.5f);
			tf.EnsureDefaultValues();
			if (ItemClass == EnumItemClass.Item)
			{
				if (secondsUsed > 0.5f)
				{
					tf.Translation.X = GameMath.Sin(30f * secondsUsed) / 10f;
				}
				tf.Translation.Z += 0f - Math.Min(1.6f, secondsUsed * 4f * 1.57f);
				tf.Translation.Y += Math.Min(0.15f, secondsUsed * 2f);
				tf.Rotation.Y -= Math.Min(85f, secondsUsed * 350f * 1.5f);
				tf.Rotation.X += Math.Min(40f, secondsUsed * 350f * 0.75f);
				tf.Rotation.Z += Math.Min(30f, secondsUsed * 350f * 0.75f);
			}
			else
			{
				tf.Translation.X -= Math.Min(1.7f, secondsUsed * 4f * 1.8f) / FpHandTransform.ScaleXYZ.X;
				tf.Translation.Y += Math.Min(0.4f, secondsUsed * 1.8f) / FpHandTransform.ScaleXYZ.X;
				tf.Scale = 1f + Math.Min(0.5f, secondsUsed * 4f * 1.8f) / FpHandTransform.ScaleXYZ.X;
				tf.Rotation.X += Math.Min(40f, secondsUsed * 350f * 0.75f) / FpHandTransform.ScaleXYZ.X;
				if (secondsUsed > 0.5f)
				{
					tf.Translation.Y += GameMath.Sin(30f * secondsUsed) / 10f / FpHandTransform.ScaleXYZ.Y;
				}
			}
			byPlayer.Entity.Controls.UsingHeldItemTransformBefore = tf;
			if (secondsUsed > 0.5f && (int)(30f * secondsUsed) % 7 == 1)
			{
				ItemStack[] contents = GetNonEmptyContents(api.World, stack);
				if (contents.Length != 0)
				{
					ItemStack rndStack = contents[api.World.Rand.Next(contents.Length)];
					api.World.SpawnCubeParticles(blockSel.Position.ToVec3d().Add(0.5, 0.125, 0.5), rndStack, 0.2f, 4, 0.5f);
				}
			}
			return secondsUsed <= 1.5f;
		}
		return true;
	}

	protected virtual bool tryFinishEatMeal(float secondsUsed, ItemSlot slot, EntityAgent byEntity, bool handleAllServingsConsumed)
	{
		FoodNutritionProperties[] multiProps = GetContentNutritionProperties(byEntity.World, slot, byEntity);
		if (byEntity.World.Side == EnumAppSide.Client || multiProps == null || (double)secondsUsed < 1.45)
		{
			return false;
		}
		ItemStack foodSourceStack = slot.Itemstack;
		slot.MarkDirty();
		IPlayer player = (byEntity as EntityPlayer).Player;
		float servingsLeft = GetQuantityServings(byEntity.World, foodSourceStack);
		ItemStack[] stacks = GetNonEmptyContents(api.World, foodSourceStack);
		if (stacks.Length == 0)
		{
			servingsLeft = 0f;
		}
		else
		{
			string recipeCode = GetRecipeCode(api.World, foodSourceStack);
			servingsLeft = Consume(byEntity.World, player, slot, stacks, servingsLeft, recipeCode == null || recipeCode == "");
		}
		if (servingsLeft <= 0f)
		{
			if (handleAllServingsConsumed)
			{
				if (Attributes["eatenBlock"].Exists)
				{
					Block block = byEntity.World.GetBlock(new AssetLocation(Attributes["eatenBlock"].AsString()));
					if (slot.Empty || slot.StackSize == 1)
					{
						slot.Itemstack = new ItemStack(block);
					}
					else if (player == null || !player.InventoryManager.TryGiveItemstack(new ItemStack(block), slotNotifyEffect: true))
					{
						byEntity.World.SpawnItemEntity(new ItemStack(block), byEntity.SidedPos.XYZ);
					}
				}
				else
				{
					slot.TakeOut(1);
					slot.MarkDirty();
				}
			}
		}
		else if (slot.Empty || slot.StackSize == 1)
		{
			(foodSourceStack.Collectible as BlockMeal).SetQuantityServings(byEntity.World, foodSourceStack, servingsLeft);
			slot.Itemstack = foodSourceStack;
		}
		else
		{
			ItemStack splitStack = slot.TakeOut(1);
			(foodSourceStack.Collectible as BlockMeal).SetQuantityServings(byEntity.World, splitStack, servingsLeft);
			ItemStack originalStack = slot.Itemstack;
			slot.Itemstack = splitStack;
			if (player == null || !player.InventoryManager.TryGiveItemstack(originalStack, slotNotifyEffect: true))
			{
				byEntity.World.SpawnItemEntity(originalStack, byEntity.SidedPos.XYZ);
			}
		}
		return true;
	}

	public bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			return false;
		}
		return tryPlacedBeginEatMeal(slot, byPlayer);
	}

	public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			return false;
		}
		return tryPlacedContinueEatMeal(secondsUsed, slot, byPlayer, blockSel);
	}

	public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (tryFinishEatMeal(secondsUsed, slot, byPlayer.Entity, handleAllServingsConsumed: true))
		{
			be.MarkDirty(redrawOnClient: true);
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!PlacedBlockEating)
		{
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		ItemStack stack = OnPickBlock(world, blockSel.Position);
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			if (byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
			{
				world.BlockAccessor.SetBlock(0, blockSel.Position);
				world.PlaySoundAt(Sounds.Place, byPlayer, byPlayer);
				return true;
			}
			return false;
		}
		BlockEntityMeal bemeal = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMeal;
		DummySlot dummySlot = new DummySlot(stack, bemeal.inventory);
		dummySlot.MarkedDirty += () => true;
		return tryPlacedBeginEatMeal(dummySlot, byPlayer);
	}

	public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!PlacedBlockEating)
		{
			return base.OnBlockInteractStep(secondsUsed, world, byPlayer, blockSel);
		}
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			return false;
		}
		ItemStack stack = OnPickBlock(world, blockSel.Position);
		return tryPlacedContinueEatMeal(secondsUsed, new DummySlot(stack), byPlayer, blockSel);
	}

	public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (!PlacedBlockEating)
		{
			base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel);
		}
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			return;
		}
		BlockEntityMeal bemeal = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityMeal;
		ItemStack stack = OnPickBlock(world, blockSel.Position);
		DummySlot dummySlot = new DummySlot(stack, bemeal.inventory);
		dummySlot.MarkedDirty += () => true;
		if (tryFinishEatMeal(secondsUsed, dummySlot, byPlayer.Entity, handleAllServingsConsumed: false))
		{
			float servingsLeft = GetQuantityServings(world, stack);
			if (bemeal.QuantityServings <= 0f)
			{
				Block block = world.GetBlock(new AssetLocation(Attributes["eatenBlock"].AsString()));
				world.BlockAccessor.SetBlock(block.BlockId, blockSel.Position);
			}
			else
			{
				bemeal.QuantityServings = servingsLeft;
				bemeal.MarkDirty(redrawOnClient: true);
			}
		}
	}

	public virtual float Consume(IWorldAccessor world, IPlayer eatingPlayer, ItemSlot inSlot, ItemStack[] contentStacks, float remainingServings, bool mulwithStackSize)
	{
		float[] nmul = GetNutritionHealthMul(null, inSlot, eatingPlayer.Entity);
		FoodNutritionProperties[] multiProps = GetContentNutritionProperties(world, inSlot, contentStacks, eatingPlayer.Entity, mulwithStackSize, nmul[0], nmul[1]);
		if (multiProps == null)
		{
			return remainingServings;
		}
		float totalHealth = 0f;
		EntityBehaviorHunger ebh = eatingPlayer.Entity.GetBehavior<EntityBehaviorHunger>();
		float satiablePoints = ebh.MaxSaturation - ebh.Saturation;
		float mealSatpoints = 0f;
		foreach (FoodNutritionProperties nutriProps2 in multiProps)
		{
			if (nutriProps2 != null)
			{
				mealSatpoints += nutriProps2.Satiety;
			}
		}
		float servingsNeeded = GameMath.Clamp(satiablePoints / Math.Max(1f, mealSatpoints), 0f, 1f);
		float servingsToEat = Math.Min(remainingServings, servingsNeeded);
		float temp = inSlot.Itemstack.Collectible.GetTemperature(world, inSlot.Itemstack);
		EntityBehaviorBodyTemperature bh = eatingPlayer.Entity.GetBehavior<EntityBehaviorBodyTemperature>();
		if (bh != null && Math.Abs(temp - bh.CurBodyTemperature) > 10f)
		{
			float intensity = Math.Min(1f, (temp - bh.CurBodyTemperature) / 30f);
			bh.CurBodyTemperature += GameMath.Clamp(mealSatpoints * servingsToEat / 80f * intensity, 0f, 5f);
		}
		foreach (FoodNutritionProperties nutriProps in multiProps)
		{
			if (nutriProps != null)
			{
				float mul = servingsToEat;
				float sat = mul * nutriProps.Satiety;
				float satLossDelay = Math.Min(1.3f, mul * 3f) * 10f + sat / 70f * 60f;
				eatingPlayer.Entity.ReceiveSaturation(sat, nutriProps.FoodCategory, satLossDelay);
				if (nutriProps.EatenStack?.ResolvedItemstack != null && (eatingPlayer == null || !eatingPlayer.InventoryManager.TryGiveItemstack(nutriProps.EatenStack.ResolvedItemstack.Clone(), slotNotifyEffect: true)))
				{
					world.SpawnItemEntity(nutriProps.EatenStack.ResolvedItemstack.Clone(), eatingPlayer.Entity.SidedPos.XYZ);
				}
				totalHealth += mul * nutriProps.Health;
			}
		}
		if (totalHealth != 0f)
		{
			eatingPlayer.Entity.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Internal,
				Type = ((totalHealth > 0f) ? EnumDamageType.Heal : EnumDamageType.Poison)
			}, Math.Abs(totalHealth));
		}
		return Math.Max(0f, remainingServings - servingsToEat);
	}

	public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
	{
		return null;
	}

	public static FoodNutritionProperties[] GetContentNutritionProperties(IWorldAccessor world, ItemSlot inSlot, ItemStack[] contentStacks, EntityAgent forEntity, bool mulWithStacksize = false, float nutritionMul = 1f, float healthMul = 1f)
	{
		List<FoodNutritionProperties> foodProps = new List<FoodNutritionProperties>();
		if (contentStacks == null)
		{
			return foodProps.ToArray();
		}
		for (int i = 0; i < contentStacks.Length; i++)
		{
			if (contentStacks[i] != null)
			{
				CollectibleObject obj = contentStacks[i].Collectible;
				FoodNutritionProperties stackProps = ((obj.CombustibleProps == null || obj.CombustibleProps.SmeltedStack == null) ? obj.GetNutritionProperties(world, contentStacks[i], forEntity) : obj.CombustibleProps.SmeltedStack.ResolvedItemstack.Collectible.GetNutritionProperties(world, obj.CombustibleProps.SmeltedStack.ResolvedItemstack, forEntity));
				JsonObject attributes = obj.Attributes;
				if (attributes != null && attributes["nutritionPropsWhenInMeal"].Exists)
				{
					stackProps = obj.Attributes?["nutritionPropsWhenInMeal"].AsObject<FoodNutritionProperties>();
				}
				if (stackProps != null)
				{
					float mul = ((!mulWithStacksize) ? 1 : contentStacks[i].StackSize);
					FoodNutritionProperties props = stackProps.Clone();
					DummySlot slot = new DummySlot(contentStacks[i], inSlot.Inventory);
					float spoilState = contentStacks[i].Collectible.UpdateAndGetTransitionState(world, slot, EnumTransitionType.Perish)?.TransitionLevel ?? 0f;
					float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, forEntity);
					float healthLoss = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, forEntity);
					props.Satiety *= satLossMul * nutritionMul * mul;
					props.Health *= healthLoss * healthMul * mul;
					foodProps.Add(props);
				}
			}
		}
		return foodProps.ToArray();
	}

	public FoodNutritionProperties[] GetContentNutritionProperties(IWorldAccessor world, ItemSlot inSlot, EntityAgent forEntity)
	{
		ItemStack[] stacks = GetNonEmptyContents(world, inSlot.Itemstack);
		if (stacks == null || stacks.Length == 0)
		{
			return null;
		}
		float[] nmul = GetNutritionHealthMul(null, inSlot, forEntity);
		return GetContentNutritionProperties(world, inSlot, stacks, forEntity, GetRecipeCode(world, inSlot.Itemstack) == null, nmul[0], nmul[1]);
	}

	public virtual string GetContentNutritionFacts(IWorldAccessor world, ItemSlot inSlotorFirstSlot, ItemStack[] contentStacks, EntityAgent forEntity, bool mulWithStacksize = false, float nutritionMul = 1f, float healthMul = 1f)
	{
		FoodNutritionProperties[] props = GetContentNutritionProperties(world, inSlotorFirstSlot, contentStacks, forEntity, mulWithStacksize, nutritionMul, healthMul);
		Dictionary<EnumFoodCategory, float> totalSaturation = new Dictionary<EnumFoodCategory, float>();
		float totalHealth = 0f;
		for (int i = 0; i < props.Length; i++)
		{
			FoodNutritionProperties prop = props[i];
			if (prop != null)
			{
				totalSaturation.TryGetValue(prop.FoodCategory, out var sat);
				DummySlot slot = new DummySlot(contentStacks[i], inSlotorFirstSlot.Inventory);
				float spoilState = contentStacks[i].Collectible.UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish)?.TransitionLevel ?? 0f;
				float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, forEntity);
				float healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, forEntity);
				totalHealth += prop.Health * healthLossMul;
				totalSaturation[prop.FoodCategory] = sat + prop.Satiety * satLossMul;
			}
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(Lang.Get("Nutrition Facts"));
		foreach (KeyValuePair<EnumFoodCategory, float> val in totalSaturation)
		{
			sb.AppendLine(Lang.Get("nutrition-facts-line-satiety", Lang.Get("foodcategory-" + val.Key.ToString().ToLowerInvariant()), Math.Round(val.Value)));
		}
		if (totalHealth != 0f)
		{
			sb.AppendLine("- " + Lang.Get("Health: {0}{1} hp", (totalHealth > 0f) ? "+" : "", totalHealth));
		}
		return sb.ToString();
	}

	public string GetContentNutritionFacts(IWorldAccessor world, ItemSlot inSlot, EntityAgent forEntity, bool mulWithStacksize = false)
	{
		float[] nmul = GetNutritionHealthMul(null, inSlot, forEntity);
		return GetContentNutritionFacts(world, inSlot, GetNonEmptyContents(world, inSlot.Itemstack), forEntity, mulWithStacksize, nmul[0], nmul[1]);
	}

	public void SetContents(string recipeCode, ItemStack containerStack, ItemStack[] stacks, float quantityServings = 1f)
	{
		base.SetContents(containerStack, stacks);
		containerStack.Attributes.SetString("recipeCode", recipeCode);
		containerStack.Attributes.SetFloat("quantityServings", quantityServings);
		if (stacks.Length != 0)
		{
			SetTemperature(api.World, containerStack, stacks[0].Collectible.GetTemperature(api.World, stacks[0]));
		}
	}

	public float GetQuantityServings(IWorldAccessor world, ItemStack byItemStack)
	{
		return (float)byItemStack.Attributes.GetDecimal("quantityServings");
	}

	public void SetQuantityServings(IWorldAccessor world, ItemStack byItemStack, float value)
	{
		if (value <= 0f)
		{
			byItemStack.Attributes.RemoveAttribute("recipeCode");
			byItemStack.Attributes.RemoveAttribute("quantityServings");
			byItemStack.Attributes.RemoveAttribute("contents");
		}
		else
		{
			byItemStack.Attributes.SetFloat("quantityServings", value);
		}
	}

	public string GetRecipeCode(IWorldAccessor world, ItemStack containerStack)
	{
		return containerStack.Attributes.GetString("recipeCode");
	}

	public CookingRecipe GetCookingRecipe(IWorldAccessor world, ItemStack containerStack)
	{
		string recipecode = GetRecipeCode(world, containerStack);
		return api.GetCookingRecipe(recipecode);
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		MultiTextureMeshRef meshref = meshCache.GetOrCreateMealInContainerMeshRef(this, GetCookingRecipe(capi.World, itemstack), GetNonEmptyContents(capi.World, itemstack));
		if (meshref != null)
		{
			renderinfo.ModelRef = meshref;
		}
	}

	public virtual MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos forBlockPos = null)
	{
		ICoreClientAPI capi = api as ICoreClientAPI;
		return meshCache.GenMealInContainerMesh(this, GetCookingRecipe(capi.World, itemstack), GetNonEmptyContents(capi.World, itemstack));
	}

	public virtual string GetMeshCacheKey(ItemStack itemstack)
	{
		return meshCache.GetMealHashCode(itemstack).ToString() ?? "";
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		ItemStack stack = base.OnPickBlock(world, pos);
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityMeal bem)
		{
			SetContents(bem.RecipeCode, stack, bem.GetNonEmptyContentStacks(), bem.QuantityServings);
		}
		return stack;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return new BlockDropItemStack[1]
		{
			new BlockDropItemStack(handbookStack)
		};
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
		CookingRecipe recipe = GetCookingRecipe(world, inSlot.Itemstack);
		ItemStack[] stacks = GetNonEmptyContents(world, inSlot.Itemstack);
		ItemSlot slot = BlockCrock.GetDummySlotForFirstPerishableStack(world, stacks, null, inSlot.Inventory);
		slot.Itemstack?.Collectible.AppendPerishableInfoText(slot, dsc, world);
		float servings = GetQuantityServings(world, inSlot.Itemstack);
		if (recipe != null)
		{
			if (Math.Round(servings, 1) < 0.05)
			{
				dsc.AppendLine(Lang.Get("{1}% serving of {0}", recipe.GetOutputName(world, stacks).UcFirst(), Math.Round(servings * 100f, 0)));
			}
			else
			{
				dsc.AppendLine(Lang.Get("{0} serving of {1}", Math.Round(servings, 1), recipe.GetOutputName(world, stacks).UcFirst()));
			}
		}
		else if (inSlot.Itemstack.Attributes.HasAttribute("quantityServings"))
		{
			dsc.AppendLine(Lang.Get("{0} servings left", Math.Round(servings, 1)));
		}
		else if (displayContentsInfo)
		{
			dsc.AppendLine(Lang.Get("Contents:"));
			if (stacks != null && stacks.Length != 0)
			{
				dsc.AppendLine(stacks[0].StackSize + "x " + stacks[0].GetName());
			}
		}
		if (!MealMeshCache.ContentsRotten(stacks))
		{
			string facts = GetContentNutritionFacts(world, inSlot, null, recipe == null);
			if (facts != null)
			{
				dsc.Append(facts);
			}
		}
	}

	public string GetContainedName(ItemSlot inSlot, int quantity)
	{
		return GetHeldItemName(inSlot.Itemstack);
	}

	public string GetContainedInfo(ItemSlot inSlot)
	{
		CookingRecipe recipe = GetCookingRecipe(api.World, inSlot.Itemstack);
		if (recipe == null)
		{
			return GetHeldItemName(inSlot.Itemstack) + PerishableInfoCompactContainer(api, inSlot);
		}
		ItemStack[] stacks = GetNonEmptyContents(api.World, inSlot.Itemstack);
		return recipe.GetOutputName(api.World, stacks).UcFirst() + PerishableInfoCompactContainer(api, inSlot);
	}

	public override void OnGroundIdle(EntityItem entityItem)
	{
		base.OnGroundIdle(entityItem);
		IWorldAccessor world = entityItem.World;
		if (world.Side != EnumAppSide.Server || !entityItem.Swimming || !(world.Rand.NextDouble() < 0.01))
		{
			return;
		}
		ItemStack[] stacks = GetNonEmptyContents(world, entityItem.Itemstack);
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
		string eatenBlock = Attributes["eatenBlock"].AsString();
		if (eatenBlock != null)
		{
			Block block = world.GetBlock(new AssetLocation(eatenBlock));
			entityItem.Itemstack = new ItemStack(block);
			entityItem.WatchedAttributes.MarkPathDirty("itemstack");
		}
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (!(capi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer bem))
		{
			return base.GetRandomColor(capi, pos, facing, rndIndex);
		}
		ItemStack[] stacks = bem.GetNonEmptyContentStacks(cloned: false);
		if (stacks != null && stacks.Length != 0)
		{
			return GetRandomContentColor(capi, stacks);
		}
		return base.GetRandomColor(capi, pos, facing, rndIndex);
	}

	public override int GetRandomColor(ICoreClientAPI capi, ItemStack stack)
	{
		ItemStack[] stacks = GetNonEmptyContents(capi.World, stack);
		return GetRandomContentColor(capi, stacks);
	}

	public override TransitionState[] UpdateAndGetTransitionStates(IWorldAccessor world, ItemSlot inslot)
	{
		TransitionState[] result = base.UpdateAndGetTransitionStates(world, inslot);
		ItemStack[] stacks = GetNonEmptyContents(world, inslot.Itemstack);
		if (MealMeshCache.ContentsRotten(stacks))
		{
			inslot.Itemstack.Attributes?.RemoveAttribute("recipeCode");
			inslot.Itemstack.Attributes?.RemoveAttribute("quantityServings");
		}
		if (stacks == null || stacks.Length == 0)
		{
			inslot.Itemstack.Attributes?.RemoveAttribute("recipeCode");
			inslot.Itemstack.Attributes?.RemoveAttribute("quantityServings");
		}
		string eaten = Attributes["eatenBlock"].AsString();
		if ((stacks == null || stacks.Length == 0) && eaten != null)
		{
			Block block = world.GetBlock(new AssetLocation(eaten));
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
			return Lang.Get("Bowl of rotten food");
		}
		return base.GetHeldItemName(itemStack);
	}

	public virtual int GetRandomContentColor(ICoreClientAPI capi, ItemStack[] stacks)
	{
		ItemStack rndStack = stacks[capi.World.Rand.Next(stacks.Length)];
		return rndStack.Collectible.GetRandomColor(capi, rndStack);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[2]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-meal-pickup",
				MouseButton = EnumMouseButton.Right
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-meal-eat",
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = "shift"
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
