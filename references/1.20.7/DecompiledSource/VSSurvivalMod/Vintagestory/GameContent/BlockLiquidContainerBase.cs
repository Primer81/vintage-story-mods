using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public abstract class BlockLiquidContainerBase : BlockContainer, ILiquidSource, ILiquidInterface, ILiquidSink
{
	public enum EnumLiquidDirection
	{
		Fill,
		Pour
	}

	protected float capacityLitresFromAttributes = 10f;

	private Dictionary<string, ItemStack[]> recipeLiquidContents = new Dictionary<string, ItemStack[]>();

	protected WorldInteraction[] interactions;

	public virtual float CapacityLitres => capacityLitresFromAttributes;

	public virtual int ContainerSlotId => 0;

	public virtual float TransferSizeLitres => 1f;

	public virtual bool CanDrinkFrom => Attributes["canDrinkFrom"].AsBool();

	public virtual bool IsTopOpened => Attributes["isTopOpened"].AsBool();

	public virtual bool AllowHeldLiquidTransfer => Attributes["allowHeldLiquidTransfer"].AsBool();

	public override void OnHandbookRecipeRender(ICoreClientAPI capi, GridRecipe gridRecipe, ItemSlot dummyslot, double x, double y, double z, double size)
	{
		int rindex = dummyslot.BackgroundIcon.ToInt();
		JsonObject rprops = gridRecipe.resolvedIngredients[rindex].RecipeAttributes;
		if (rprops == null || !rprops.Exists || rprops == null || !rprops["requiresContent"].Exists)
		{
			rprops = gridRecipe.Attributes?["liquidContainerProps"];
		}
		if (rprops == null || !rprops.Exists)
		{
			base.OnHandbookRecipeRender(capi, gridRecipe, dummyslot, x, y, z, size);
			return;
		}
		string contentCode = gridRecipe.Attributes["liquidContainerProps"]["requiresContent"]["code"].AsString();
		string contentType = gridRecipe.Attributes["liquidContainerProps"]["requiresContent"]["type"].AsString();
		float litres = gridRecipe.Attributes["liquidContainerProps"]["requiresLitres"].AsFloat();
		string key = contentType + "-" + contentCode;
		if (!recipeLiquidContents.TryGetValue(key, out var stacks))
		{
			if (contentCode.Contains('*'))
			{
				EnumItemClass contentClass = ((!(contentType == "block")) ? EnumItemClass.Item : EnumItemClass.Block);
				List<ItemStack> lstacks = new List<ItemStack>();
				AssetLocation loc = AssetLocation.Create(contentCode, Code.Domain);
				foreach (CollectibleObject obj in api.World.Collectibles)
				{
					if (obj.ItemClass == contentClass && WildcardUtil.Match(loc, obj.Code))
					{
						ItemStack stack = new ItemStack(obj);
						WaterTightContainableProps props = GetContainableProps(stack);
						if (props != null)
						{
							stack.StackSize = (int)(props.ItemsPerLitre * litres);
							lstacks.Add(stack);
						}
					}
				}
				stacks = lstacks.ToArray();
			}
			else
			{
				stacks = (recipeLiquidContents[key] = new ItemStack[1]);
				if (contentType == "item")
				{
					stacks[0] = new ItemStack(capi.World.GetItem(new AssetLocation(contentCode)));
				}
				else
				{
					stacks[0] = new ItemStack(capi.World.GetBlock(new AssetLocation(contentCode)));
				}
				WaterTightContainableProps props2 = GetContainableProps(stacks[0]);
				stacks[0].StackSize = (int)(props2.ItemsPerLitre * litres);
			}
		}
		ItemStack filledContainerStack = dummyslot.Itemstack.Clone();
		int index = (int)(capi.ElapsedMilliseconds / 1000) % stacks.Length;
		SetContent(filledContainerStack, stacks[index]);
		dummyslot.Itemstack = filledContainerStack;
		capi.Render.RenderItemstackToGui(dummyslot, x, y, z, (float)size * 0.58f, -1);
	}

	public virtual int GetContainerSlotId(BlockPos pos)
	{
		return ContainerSlotId;
	}

	public virtual int GetContainerSlotId(ItemStack containerStack)
	{
		return ContainerSlotId;
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		JsonObject attributes = Attributes;
		if (attributes != null && attributes["capacityLitres"].Exists)
		{
			capacityLitresFromAttributes = Attributes["capacityLitres"].AsInt(10);
		}
		else
		{
			LiquidTopOpenContainerProps props = Attributes?["liquidContainerProps"]?.AsObject<LiquidTopOpenContainerProps>(null, Code.Domain);
			if (props != null)
			{
				capacityLitresFromAttributes = props.CapacityLitres;
			}
		}
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "liquidContainerBase", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				if (current is BlockLiquidContainerBase { IsTopOpened: not false, AllowHeldLiquidTransfer: not false })
				{
					list.Add(new ItemStack(current));
				}
			}
			ItemStack[] itemstacks = list.ToArray();
			return new WorldInteraction[3]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-bucket-rightclick",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = itemstacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-bucket-rightclick-sneak",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "shift",
					Itemstacks = itemstacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-bucket-rightclick-sprint",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "ctrl",
					Itemstacks = itemstacks
				}
			};
		});
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(interactions);
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[3]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-fill",
				MouseButton = EnumMouseButton.Right,
				ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => GetCurrentLitres(inSlot.Itemstack) < CapacityLitres
			},
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-empty",
				HotKeyCode = "ctrl",
				MouseButton = EnumMouseButton.Right,
				ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => GetCurrentLitres(inSlot.Itemstack) > 0f
			},
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-place",
				HotKeyCode = "shift",
				MouseButton = EnumMouseButton.Right,
				ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => true
			}
		};
	}

	public bool SetCurrentLitres(ItemStack containerStack, float litres)
	{
		WaterTightContainableProps props = GetContentProps(containerStack);
		if (props == null)
		{
			return false;
		}
		ItemStack contentStack = GetContent(containerStack);
		contentStack.StackSize = (int)(litres * props.ItemsPerLitre);
		SetContent(containerStack, contentStack);
		return true;
	}

	public float GetCurrentLitres(ItemStack containerStack)
	{
		WaterTightContainableProps props = GetContentProps(containerStack);
		if (props == null)
		{
			return 0f;
		}
		return (float)GetContent(containerStack).StackSize / props.ItemsPerLitre;
	}

	public float GetCurrentLitres(BlockPos pos)
	{
		WaterTightContainableProps props = GetContentProps(pos);
		if (props == null)
		{
			return 0f;
		}
		return (float)GetContent(pos).StackSize / props.ItemsPerLitre;
	}

	public bool IsFull(ItemStack containerStack)
	{
		return GetCurrentLitres(containerStack) >= CapacityLitres;
	}

	public bool IsFull(BlockPos pos)
	{
		return GetCurrentLitres(pos) >= CapacityLitres;
	}

	public WaterTightContainableProps GetContentProps(ItemStack containerStack)
	{
		return GetContainableProps(GetContent(containerStack));
	}

	public static int GetTransferStackSize(ILiquidInterface containerBlock, ItemStack contentStack, IPlayer player = null)
	{
		return GetTransferStackSize(containerBlock, contentStack, player != null && (player.Entity?.Controls.ShiftKey).GetValueOrDefault());
	}

	public static int GetTransferStackSize(ILiquidInterface containerBlock, ItemStack contentStack, bool maxCapacity)
	{
		if (contentStack == null)
		{
			return 0;
		}
		float litres = containerBlock.TransferSizeLitres;
		WaterTightContainableProps liqProps = GetContainableProps(contentStack);
		int stacksize = (int)(liqProps.ItemsPerLitre * litres);
		if (maxCapacity)
		{
			stacksize = (int)(containerBlock.CapacityLitres * liqProps.ItemsPerLitre);
		}
		return stacksize;
	}

	public static WaterTightContainableProps GetContainableProps(ItemStack stack)
	{
		try
		{
			JsonObject obj = stack?.ItemAttributes?["waterTightContainerProps"];
			if (obj != null && obj.Exists)
			{
				return obj.AsObject<WaterTightContainableProps>(null, stack.Collectible.Code.Domain);
			}
			return null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	public WaterTightContainableProps GetContentProps(BlockPos pos)
	{
		if (!(api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer becontainer))
		{
			return null;
		}
		int slotid = GetContainerSlotId(pos);
		if (slotid >= becontainer.Inventory.Count)
		{
			return null;
		}
		ItemStack stack = becontainer.Inventory[slotid]?.Itemstack;
		if (stack == null)
		{
			return null;
		}
		return GetContainableProps(stack);
	}

	public void SetContent(ItemStack containerStack, ItemStack content)
	{
		if (content == null)
		{
			SetContents(containerStack, null);
			return;
		}
		SetContents(containerStack, new ItemStack[1] { content });
	}

	public void SetContent(BlockPos pos, ItemStack content)
	{
		if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer beContainer)
		{
			new DummySlot(content).TryPutInto(api.World, beContainer.Inventory[GetContainerSlotId(pos)], content.StackSize);
			beContainer.Inventory[GetContainerSlotId(pos)].MarkDirty();
			beContainer.MarkDirty(redrawOnClient: true);
		}
	}

	public ItemStack GetContent(ItemStack containerStack)
	{
		ItemStack[] stacks = GetContents(api.World, containerStack);
		int id = GetContainerSlotId(containerStack);
		if (stacks == null || stacks.Length == 0)
		{
			return null;
		}
		return stacks[Math.Min(stacks.Length - 1, id)];
	}

	public ItemStack GetContent(BlockPos pos)
	{
		if (!(api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer becontainer))
		{
			return null;
		}
		return becontainer.Inventory[GetContainerSlotId(pos)].Itemstack;
	}

	public override ItemStack CreateItemStackFromJson(ITreeAttribute stackAttr, IWorldAccessor world, string domain)
	{
		ItemStack stack = base.CreateItemStackFromJson(stackAttr, world, domain);
		if (stackAttr.HasAttribute("makefull"))
		{
			WaterTightContainableProps props = GetContainableProps(stack);
			stack.StackSize = (int)(CapacityLitres * props.ItemsPerLitre);
		}
		return stack;
	}

	public ItemStack TryTakeContent(ItemStack containerStack, int quantityItems)
	{
		ItemStack stack = GetContent(containerStack);
		if (stack == null)
		{
			return null;
		}
		ItemStack itemStack = stack.Clone();
		itemStack.StackSize = quantityItems;
		stack.StackSize -= quantityItems;
		if (stack.StackSize <= 0)
		{
			SetContent(containerStack, null);
			return itemStack;
		}
		SetContent(containerStack, stack);
		return itemStack;
	}

	public ItemStack TryTakeContent(BlockPos pos, int quantityItem)
	{
		if (!(api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer becontainer))
		{
			return null;
		}
		ItemStack stack = becontainer.Inventory[GetContainerSlotId(pos)].Itemstack;
		if (stack == null)
		{
			return null;
		}
		ItemStack itemStack = stack.Clone();
		itemStack.StackSize = quantityItem;
		stack.StackSize -= quantityItem;
		if (stack.StackSize <= 0)
		{
			becontainer.Inventory[GetContainerSlotId(pos)].Itemstack = null;
		}
		else
		{
			becontainer.Inventory[GetContainerSlotId(pos)].Itemstack = stack;
		}
		becontainer.Inventory[GetContainerSlotId(pos)].MarkDirty();
		becontainer.MarkDirty(redrawOnClient: true);
		return itemStack;
	}

	public ItemStack TryTakeLiquid(ItemStack containerStack, float desiredLitres)
	{
		WaterTightContainableProps props = GetContainableProps(GetContent(containerStack));
		if (props == null)
		{
			return null;
		}
		return TryTakeContent(containerStack, (int)(desiredLitres * props.ItemsPerLitre));
	}

	public virtual int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres)
	{
		if (liquidStack == null)
		{
			return 0;
		}
		WaterTightContainableProps props = GetContainableProps(liquidStack);
		if (props == null)
		{
			return 0;
		}
		float epsilon = 1E-05f;
		int desiredItems = (int)(props.ItemsPerLitre * desiredLitres + epsilon);
		int availItems = liquidStack.StackSize;
		ItemStack stack = GetContent(containerStack);
		ILiquidSink sink = containerStack.Collectible as ILiquidSink;
		if (stack == null)
		{
			if (!props.Containable)
			{
				return 0;
			}
			int placeableItems = (int)(sink.CapacityLitres * props.ItemsPerLitre + epsilon);
			ItemStack placedstack = liquidStack.Clone();
			placedstack.StackSize = GameMath.Min(availItems, desiredItems, placeableItems);
			SetContent(containerStack, placedstack);
			return Math.Min(desiredItems, placeableItems);
		}
		if (!stack.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes))
		{
			return 0;
		}
		int placeableItems2 = (int)(sink.CapacityLitres * props.ItemsPerLitre - (float)stack.StackSize);
		int moved = GameMath.Min(availItems, placeableItems2, desiredItems);
		stack.StackSize += moved;
		return moved;
	}

	public virtual int TryPutLiquid(BlockPos pos, ItemStack liquidStack, float desiredLitres)
	{
		if (liquidStack == null)
		{
			return 0;
		}
		WaterTightContainableProps props = GetContainableProps(liquidStack);
		int desiredItems = (int)(props.ItemsPerLitre * desiredLitres);
		float availItems = liquidStack.StackSize;
		float maxItems = CapacityLitres * props.ItemsPerLitre;
		ItemStack stack = GetContent(pos);
		if (stack == null)
		{
			if (props == null || !props.Containable)
			{
				return 0;
			}
			int placeableItems = (int)GameMath.Min(desiredItems, maxItems, availItems);
			int movedItems = Math.Min(desiredItems, placeableItems);
			ItemStack placedstack = liquidStack.Clone();
			placedstack.StackSize = movedItems;
			SetContent(pos, placedstack);
			return movedItems;
		}
		if (!stack.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes))
		{
			return 0;
		}
		int movedItems2 = Math.Min((int)Math.Min(availItems, maxItems - (float)stack.StackSize), desiredItems);
		stack.StackSize += movedItems2;
		api.World.BlockAccessor.GetBlockEntity(pos).MarkDirty(redrawOnClient: true);
		(api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityContainer).Inventory[GetContainerSlotId(pos)].MarkDirty();
		return movedItems2;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (!hotbarSlot.Empty)
		{
			JsonObject attributes = hotbarSlot.Itemstack.Collectible.Attributes;
			if (attributes != null && attributes.IsTrue("handleLiquidContainerInteract"))
			{
				EnumHandHandling handling = EnumHandHandling.NotHandled;
				hotbarSlot.Itemstack.Collectible.OnHeldInteractStart(hotbarSlot, byPlayer.Entity, blockSel, null, firstEvent: true, ref handling);
				if (handling == EnumHandHandling.PreventDefault || handling == EnumHandHandling.PreventDefaultAction)
				{
					return true;
				}
			}
		}
		if (hotbarSlot.Empty || !(hotbarSlot.Itemstack.Collectible is ILiquidInterface))
		{
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		CollectibleObject obj = hotbarSlot.Itemstack.Collectible;
		bool singleTake = byPlayer.WorldData.EntityControls.ShiftKey;
		bool singlePut = byPlayer.WorldData.EntityControls.CtrlKey;
		ILiquidSource objLso = obj as ILiquidSource;
		if (objLso != null && !singleTake)
		{
			if (!objLso.AllowHeldLiquidTransfer)
			{
				return false;
			}
			ItemStack contentStackToMove = objLso.GetContent(hotbarSlot.Itemstack);
			float litres = (singlePut ? objLso.TransferSizeLitres : objLso.CapacityLitres);
			int moved2 = TryPutLiquid(blockSel.Position, contentStackToMove, litres);
			if (moved2 > 0)
			{
				SplitStackAndPerformAction(byPlayer.Entity, hotbarSlot, delegate(ItemStack stack)
				{
					objLso.TryTakeContent(stack, moved2);
					return moved2;
				});
				DoLiquidMovedEffects(byPlayer, contentStackToMove, moved2, EnumLiquidDirection.Pour);
				return true;
			}
		}
		ILiquidSink objLsi = obj as ILiquidSink;
		if (objLsi != null && !singlePut)
		{
			if (!objLsi.AllowHeldLiquidTransfer)
			{
				return false;
			}
			ItemStack owncontentStack = GetContent(blockSel.Position);
			if (owncontentStack == null)
			{
				return base.OnBlockInteractStart(world, byPlayer, blockSel);
			}
			ItemStack liquidStackForParticles = owncontentStack.Clone();
			float litres2 = (singleTake ? objLsi.TransferSizeLitres : objLsi.CapacityLitres);
			int moved = SplitStackAndPerformAction(byPlayer.Entity, hotbarSlot, (ItemStack stack) => objLsi.TryPutLiquid(stack, owncontentStack, litres2));
			if (moved > 0)
			{
				TryTakeContent(blockSel.Position, moved);
				DoLiquidMovedEffects(byPlayer, liquidStackForParticles, moved, EnumLiquidDirection.Fill);
				return true;
			}
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public void DoLiquidMovedEffects(IPlayer player, ItemStack contentStack, int moved, EnumLiquidDirection dir)
	{
		if (player != null)
		{
			WaterTightContainableProps props = GetContainableProps(contentStack);
			float litresMoved = (float)moved / props.ItemsPerLitre;
			(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			api.World.PlaySoundAt((dir == EnumLiquidDirection.Fill) ? props.FillSound : props.PourSound, player.Entity, player, randomizePitch: true, 16f, GameMath.Clamp(litresMoved / 5f, 0.35f, 1f));
			api.World.SpawnCubeParticles(player.Entity.Pos.AheadCopy(0.25).XYZ.Add(0.0, player.Entity.SelectionBox.Y2 / 2f, 0.0), contentStack, 0.75f, (int)litresMoved * 2, 0.45f);
		}
	}

	protected override void tryEatBegin(ItemSlot slot, EntityAgent byEntity, ref EnumHandHandling handling, string eatSound = "eat", int eatSoundRepeats = 1)
	{
		base.tryEatBegin(slot, byEntity, ref handling, "drink", 4);
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null || byEntity.Controls.ShiftKey)
		{
			if (byEntity.Controls.ShiftKey)
			{
				base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			}
			if (handHandling != EnumHandHandling.PreventDefaultAction && CanDrinkFrom && GetNutritionProperties(byEntity.World, itemslot.Itemstack, byEntity) != null)
			{
				tryEatBegin(itemslot, byEntity, ref handHandling, "drink", 4);
			}
			else if (!byEntity.Controls.ShiftKey)
			{
				base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			}
			return;
		}
		if (AllowHeldLiquidTransfer)
		{
			IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
			ItemStack contentStack = GetContent(itemslot.Itemstack);
			WaterTightContainableProps props = ((contentStack == null) ? null : GetContentProps(contentStack));
			Block targetedBlock = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
			if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
			{
				byEntity.World.BlockAccessor.MarkBlockDirty(blockSel.Position.AddCopy(blockSel.Face));
				byPlayer?.InventoryManager.ActiveHotbarSlot?.MarkDirty();
				return;
			}
			if (!TryFillFromBlock(itemslot, byEntity, blockSel.Position))
			{
				if (targetedBlock is BlockLiquidContainerTopOpened targetCntBlock)
				{
					if (targetCntBlock.TryPutLiquid(blockSel.Position, contentStack, targetCntBlock.CapacityLitres) > 0)
					{
						TryTakeContent(itemslot.Itemstack, 1);
						byEntity.World.PlaySoundAt(props.FillSpillSound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
					}
				}
				else if (byEntity.Controls.CtrlKey)
				{
					SpillContents(itemslot, byEntity, blockSel);
				}
			}
		}
		if (CanDrinkFrom && GetNutritionProperties(byEntity.World, itemslot.Itemstack, byEntity) != null)
		{
			tryEatBegin(itemslot, byEntity, ref handHandling, "drink", 4);
		}
		else if (AllowHeldLiquidTransfer || CanDrinkFrom)
		{
			handHandling = EnumHandHandling.PreventDefaultAction;
		}
	}

	protected override bool tryEatStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, ItemStack spawnParticleStack = null)
	{
		return base.tryEatStep(secondsUsed, slot, byEntity, GetContent(slot.Itemstack));
	}

	protected override void tryEatStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity)
	{
		FoodNutritionProperties nutriProps = GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);
		if (byEntity.World is IServerWorldAccessor && nutriProps != null && secondsUsed >= 0.95f)
		{
			float drinkCapLitres = 1f;
			float litresEach = GetCurrentLitres(slot.Itemstack);
			float litresTotal = litresEach * (float)slot.StackSize;
			if (litresEach > drinkCapLitres)
			{
				nutriProps.Satiety /= litresEach;
				nutriProps.Health /= litresEach;
			}
			float spoilState = UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish)?.TransitionLevel ?? 0f;
			float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, byEntity);
			float healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, byEntity);
			byEntity.ReceiveSaturation(nutriProps.Satiety * satLossMul, nutriProps.FoodCategory);
			IPlayer player = null;
			if (byEntity is EntityPlayer)
			{
				player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			float litresToDrink = Math.Min(drinkCapLitres, litresTotal);
			TryTakeLiquid(slot.Itemstack, litresToDrink / (float)slot.Itemstack.StackSize);
			float healthChange = nutriProps.Health * healthLossMul;
			float intox = byEntity.WatchedAttributes.GetFloat("intoxication");
			byEntity.WatchedAttributes.SetFloat("intoxication", Math.Min(1.1f, intox + nutriProps.Intoxication));
			if (healthChange != 0f)
			{
				byEntity.ReceiveDamage(new DamageSource
				{
					Source = EnumDamageSource.Internal,
					Type = ((healthChange > 0f) ? EnumDamageType.Heal : EnumDamageType.Poison)
				}, Math.Abs(healthChange));
			}
			slot.MarkDirty();
			player.InventoryManager.BroadcastHotbarSlot();
		}
	}

	public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
	{
		ItemStack contentStack = GetContent(itemstack);
		WaterTightContainableProps props = ((contentStack == null) ? null : GetContainableProps(contentStack));
		if (props?.NutritionPropsPerLitre != null)
		{
			FoodNutritionProperties nutriProps = props.NutritionPropsPerLitre.Clone();
			float litre = (float)contentStack.StackSize / props.ItemsPerLitre;
			nutriProps.Health *= litre;
			nutriProps.Satiety *= litre;
			nutriProps.EatenStack = new JsonItemStack();
			nutriProps.EatenStack.ResolvedItemstack = itemstack.Clone();
			nutriProps.EatenStack.ResolvedItemstack.StackSize = 1;
			(nutriProps.EatenStack.ResolvedItemstack.Collectible as BlockLiquidContainerBase).SetContent(nutriProps.EatenStack.ResolvedItemstack, null);
			return nutriProps;
		}
		return base.GetNutritionProperties(world, itemstack, forEntity);
	}

	public bool TryFillFromBlock(ItemSlot itemslot, EntityAgent byEntity, BlockPos pos)
	{
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		Block block = byEntity.World.BlockAccessor.GetBlock(pos, 3);
		JsonObject attributes = block.Attributes;
		if (attributes != null && !attributes["waterTightContainerProps"].Exists)
		{
			return false;
		}
		WaterTightContainableProps props = block.Attributes?["waterTightContainerProps"]?.AsObject<WaterTightContainableProps>();
		if (props?.WhenFilled == null || !props.Containable)
		{
			return false;
		}
		props.WhenFilled.Stack.Resolve(byEntity.World, "liquidcontainerbase");
		if (GetCurrentLitres(itemslot.Itemstack) >= CapacityLitres)
		{
			return false;
		}
		ItemStack contentStack = props.WhenFilled.Stack.ResolvedItemstack;
		if (contentStack == null)
		{
			return false;
		}
		contentStack = contentStack.Clone();
		GetContainableProps(contentStack);
		contentStack.StackSize = 999999;
		int moved = SplitStackAndPerformAction(byEntity, itemslot, (ItemStack stack) => TryPutLiquid(stack, contentStack, CapacityLitres));
		if (moved > 0)
		{
			DoLiquidMovedEffects(byPlayer, contentStack, moved, EnumLiquidDirection.Fill);
		}
		return true;
	}

	public virtual void TryFillFromBlock(EntityItem byEntityItem, BlockPos pos)
	{
		IWorldAccessor world = byEntityItem.World;
		Block block = world.BlockAccessor.GetBlock(pos);
		JsonObject attributes = block.Attributes;
		if (attributes != null && !attributes["waterTightContainerProps"].Exists)
		{
			return;
		}
		WaterTightContainableProps props = block.Attributes?["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
		if (props?.WhenFilled == null || !props.Containable)
		{
			return;
		}
		if (props.WhenFilled.Stack.ResolvedItemstack == null)
		{
			props.WhenFilled.Stack.Resolve(world, "liquidcontainerbase");
		}
		ItemStack whenFilledStack = props.WhenFilled.Stack.ResolvedItemstack;
		ItemStack contentStack = GetContent(byEntityItem.Itemstack);
		if (contentStack == null || (contentStack.Equals(world, whenFilledStack, GlobalConstants.IgnoredStackAttributes) && GetCurrentLitres(byEntityItem.Itemstack) < CapacityLitres))
		{
			whenFilledStack.StackSize = 999999;
			if (SplitStackAndPerformAction(byEntityItem, byEntityItem.Slot, (ItemStack stack) => TryPutLiquid(stack, whenFilledStack, CapacityLitres)) > 0)
			{
				world.PlaySoundAt(props.FillSound, pos, -0.4);
			}
		}
	}

	private bool SpillContents(ItemSlot containerSlot, EntityAgent byEntity, BlockSelection blockSel)
	{
		BlockPos pos = blockSel.Position;
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		IBlockAccessor blockAcc = byEntity.World.BlockAccessor;
		BlockPos secondPos = blockSel.Position.AddCopy(blockSel.Face);
		ItemStack contentStack = GetContent(containerSlot.Itemstack);
		WaterTightContainableProps props = GetContentProps(containerSlot.Itemstack);
		if (props == null || !props.AllowSpill || props.WhenSpilled == null)
		{
			return false;
		}
		if (!byEntity.World.Claims.TryAccess(byPlayer, secondPos, EnumBlockAccessFlags.BuildOrBreak))
		{
			return false;
		}
		WaterTightContainableProps.EnumSpilledAction action = props.WhenSpilled.Action;
		float currentlitres = GetCurrentLitres(containerSlot.Itemstack);
		if (currentlitres > 0f && currentlitres < 10f)
		{
			action = WaterTightContainableProps.EnumSpilledAction.DropContents;
		}
		if (action == WaterTightContainableProps.EnumSpilledAction.PlaceBlock)
		{
			Block waterBlock = byEntity.World.GetBlock(props.WhenSpilled.Stack.Code);
			if (props.WhenSpilled.StackByFillLevel != null)
			{
				props.WhenSpilled.StackByFillLevel.TryGetValue((int)currentlitres, out var fillLevelStack);
				if (fillLevelStack != null)
				{
					waterBlock = byEntity.World.GetBlock(fillLevelStack.Code);
				}
			}
			if (!blockAcc.GetBlock(pos).DisplacesLiquids(blockAcc, pos))
			{
				blockAcc.SetBlock(waterBlock.BlockId, pos, 2);
				blockAcc.TriggerNeighbourBlockUpdate(pos);
				waterBlock.OnNeighbourBlockChange(byEntity.World, pos, secondPos);
				blockAcc.MarkBlockDirty(pos);
			}
			else
			{
				if (blockAcc.GetBlock(secondPos).DisplacesLiquids(blockAcc, pos))
				{
					return false;
				}
				blockAcc.SetBlock(waterBlock.BlockId, secondPos, 2);
				blockAcc.TriggerNeighbourBlockUpdate(secondPos);
				waterBlock.OnNeighbourBlockChange(byEntity.World, secondPos, pos);
				blockAcc.MarkBlockDirty(secondPos);
			}
		}
		if (action == WaterTightContainableProps.EnumSpilledAction.DropContents)
		{
			props.WhenSpilled.Stack.Resolve(byEntity.World, "liquidcontainerbasespill");
			ItemStack stack2 = props.WhenSpilled.Stack.ResolvedItemstack.Clone();
			stack2.StackSize = contentStack.StackSize;
			byEntity.World.SpawnItemEntity(stack2, blockSel.Position.ToVec3d().Add(blockSel.HitPosition));
		}
		int moved = SplitStackAndPerformAction(byEntity, containerSlot, delegate(ItemStack stack)
		{
			SetContent(stack, null);
			return contentStack.StackSize;
		});
		DoLiquidMovedEffects(byPlayer, contentStack, moved, EnumLiquidDirection.Pour);
		return true;
	}

	public int SplitStackAndPerformAction(Entity byEntity, ItemSlot slot, System.Func<ItemStack, int> action)
	{
		if (slot.Itemstack == null)
		{
			return 0;
		}
		if (slot.Itemstack.StackSize == 1)
		{
			int num = action(slot.Itemstack);
			if (num > 0)
			{
				_ = slot.Itemstack.Collectible.MaxStackSize;
				EntityPlayer obj = byEntity as EntityPlayer;
				if (obj == null)
				{
					return num;
				}
				obj.WalkInventory(delegate(ItemSlot pslot)
				{
					if (pslot.Empty || pslot is ItemSlotCreative || pslot.StackSize == pslot.Itemstack.Collectible.MaxStackSize)
					{
						return true;
					}
					int mergableQuantity = slot.Itemstack.Collectible.GetMergableQuantity(slot.Itemstack, pslot.Itemstack, EnumMergePriority.DirectMerge);
					if (mergableQuantity == 0)
					{
						return true;
					}
					BlockLiquidContainerBase obj3 = slot.Itemstack.Collectible as BlockLiquidContainerBase;
					BlockLiquidContainerBase blockLiquidContainerBase = pslot.Itemstack.Collectible as BlockLiquidContainerBase;
					if ((obj3?.GetContent(slot.Itemstack)?.StackSize).GetValueOrDefault() != (blockLiquidContainerBase?.GetContent(pslot.Itemstack)?.StackSize).GetValueOrDefault())
					{
						return true;
					}
					slot.Itemstack.StackSize += mergableQuantity;
					pslot.TakeOut(mergableQuantity);
					slot.MarkDirty();
					pslot.MarkDirty();
					return true;
				});
			}
			return num;
		}
		ItemStack containerStack = slot.Itemstack.Clone();
		containerStack.StackSize = 1;
		int num2 = action(containerStack);
		if (num2 > 0)
		{
			slot.TakeOut(1);
			EntityPlayer obj2 = byEntity as EntityPlayer;
			if (obj2 == null || !obj2.Player.InventoryManager.TryGiveItemstack(containerStack, slotNotifyEffect: true))
			{
				api.World.SpawnItemEntity(containerStack, byEntity.SidedPos.XYZ);
			}
			slot.MarkDirty();
		}
		return num2;
	}

	public override void OnGroundIdle(EntityItem entityItem)
	{
		base.OnGroundIdle(entityItem);
		IWorldAccessor world = entityItem.World;
		if (world.Side != EnumAppSide.Server)
		{
			return;
		}
		if (entityItem.Swimming && world.Rand.NextDouble() < 0.03)
		{
			TryFillFromBlock(entityItem, entityItem.SidedPos.AsBlockPos);
		}
		if (!entityItem.Swimming || !(world.Rand.NextDouble() < 0.01))
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
		SetContent(entityItem.Itemstack, null);
	}

	public override void AddExtraHeldItemInfoPostMaterial(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
	{
		GetContentInfo(inSlot, dsc, world);
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		float litres = GetCurrentLitres(pos);
		StringBuilder sb = new StringBuilder();
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityContainer becontainer)
		{
			if (litres <= 0f)
			{
				sb.AppendLine(Lang.Get("Empty"));
			}
			else
			{
				ItemSlot slot = becontainer.Inventory[GetContainerSlotId(pos)];
				ItemStack contentStack = slot.Itemstack;
				string incontainername = Lang.Get(contentStack.Collectible.Code.Domain + ":incontainer-" + contentStack.Class.ToString().ToLowerInvariant() + "-" + contentStack.Collectible.Code.Path);
				sb.AppendLine(Lang.Get("Contents:"));
				sb.AppendLine(" " + Lang.Get("{0} litres of {1}", litres, incontainername));
				string perishableInfo = PerishableInfoCompact(api, slot, 0f, withStackName: false);
				if (perishableInfo.Length > 2)
				{
					sb.AppendLine(perishableInfo.Substring(2));
				}
			}
		}
		StringBuilder sb2 = new StringBuilder();
		BlockBehavior[] blockBehaviors = BlockBehaviors;
		foreach (BlockBehavior bh in blockBehaviors)
		{
			sb2.Append(bh.GetPlacedBlockInfo(world, pos, forPlayer));
		}
		if (sb2.Length > 0)
		{
			sb.AppendLine();
			sb.Append(sb2.ToString());
		}
		return sb.ToString();
	}

	public virtual void GetContentInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world)
	{
		float litres = GetCurrentLitres(inSlot.Itemstack);
		ItemStack contentStack = GetContent(inSlot.Itemstack);
		if (litres <= 0f)
		{
			dsc.AppendLine(Lang.Get("Empty"));
			return;
		}
		string incontainerrname = Lang.Get(contentStack.Collectible.Code.Domain + ":incontainer-" + contentStack.Class.ToString().ToLowerInvariant() + "-" + contentStack.Collectible.Code.Path);
		dsc.AppendLine(Lang.Get("{0} litres of {1}", litres, incontainerrname));
		ItemSlot dummyslot = GetContentInDummySlot(inSlot, contentStack);
		TransitionState[] states = contentStack.Collectible.UpdateAndGetTransitionStates(api.World, dummyslot);
		if (states != null && !dummyslot.Empty)
		{
			bool nowSpoiling = false;
			TransitionState[] array = states;
			foreach (TransitionState state in array)
			{
				nowSpoiling |= AppendPerishableInfoText(dummyslot, dsc, world, state, nowSpoiling) > 0f;
			}
		}
	}

	public override void TryMergeStacks(ItemStackMergeOperation op)
	{
		op.MovableQuantity = GetMergableQuantity(op.SinkSlot.Itemstack, op.SourceSlot.Itemstack, op.CurrentPriority);
		if (op.MovableQuantity == 0 || !op.SinkSlot.CanTakeFrom(op.SourceSlot, op.CurrentPriority))
		{
			return;
		}
		ItemStack sinkContent = GetContent(op.SinkSlot.Itemstack);
		ItemStack sourceContent = GetContent(op.SourceSlot.Itemstack);
		if (sinkContent == null && sourceContent == null)
		{
			base.TryMergeStacks(op);
			return;
		}
		if (sinkContent == null || sourceContent == null)
		{
			op.MovableQuantity = 0;
			return;
		}
		if (!sinkContent.Equals(op.World, sourceContent, GlobalConstants.IgnoredStackAttributes))
		{
			op.MovableQuantity = 0;
			return;
		}
		float sourceLitres = GetCurrentLitres(op.SourceSlot.Itemstack) * (float)op.SourceSlot.StackSize;
		float sinkLitres = GetCurrentLitres(op.SinkSlot.Itemstack) * (float)op.SinkSlot.StackSize;
		float valueOrDefault = ((float)op.SourceSlot.StackSize * (op.SourceSlot.Itemstack.Collectible as BlockLiquidContainerBase)?.CapacityLitres).GetValueOrDefault();
		float sinkCapLitres = ((float)op.SinkSlot.StackSize * (op.SinkSlot.Itemstack.Collectible as BlockLiquidContainerBase)?.CapacityLitres).GetValueOrDefault();
		if (valueOrDefault == 0f || sinkCapLitres == 0f)
		{
			base.TryMergeStacks(op);
			return;
		}
		if (GetCurrentLitres(op.SourceSlot.Itemstack) == GetCurrentLitres(op.SinkSlot.Itemstack))
		{
			if (op.MovableQuantity > 0)
			{
				base.TryMergeStacks(op);
			}
			else
			{
				op.MovedQuantity = 0;
			}
			return;
		}
		if (op.CurrentPriority == EnumMergePriority.DirectMerge)
		{
			float movableLitres = Math.Min(sinkCapLitres - sinkLitres, sourceLitres);
			int moved = TryPutLiquid(op.SinkSlot.Itemstack, sourceContent, movableLitres / (float)op.SinkSlot.StackSize);
			DoLiquidMovedEffects(op.ActingPlayer, sinkContent, moved, EnumLiquidDirection.Pour);
			moved *= op.SinkSlot.StackSize;
			TryTakeContent(op.SourceSlot.Itemstack, (int)(0.51f + (float)moved / (float)op.SourceSlot.StackSize));
			op.SourceSlot.MarkDirty();
			op.SinkSlot.MarkDirty();
		}
		op.MovableQuantity = 0;
	}

	public override bool MatchesForCrafting(ItemStack inputStack, GridRecipe gridRecipe, CraftingRecipeIngredient ingredient)
	{
		JsonObject rprops = ingredient.RecipeAttributes;
		if (rprops == null || !rprops.Exists || rprops == null || !rprops["requiresContent"].Exists)
		{
			rprops = gridRecipe.Attributes?["liquidContainerProps"];
		}
		if (rprops == null || !rprops.Exists)
		{
			return base.MatchesForCrafting(inputStack, gridRecipe, ingredient);
		}
		string contentCode = rprops["requiresContent"]["code"].AsString();
		string contentType = rprops["requiresContent"]["type"].AsString();
		ItemStack contentStack = GetContent(inputStack);
		if (contentStack == null)
		{
			return false;
		}
		float litres = rprops["requiresLitres"].AsFloat();
		int q = (int)(GetContainableProps(contentStack).ItemsPerLitre * litres) / inputStack.StackSize;
		bool num = contentStack.Class.ToString().ToLowerInvariant() == contentType.ToLowerInvariant();
		bool b = WildcardUtil.Match(new AssetLocation(contentCode), contentStack.Collectible.Code);
		bool c = contentStack.StackSize >= q;
		return num && b && c;
	}

	public override void OnConsumedByCrafting(ItemSlot[] allInputSlots, ItemSlot stackInSlot, GridRecipe gridRecipe, CraftingRecipeIngredient fromIngredient, IPlayer byPlayer, int quantity)
	{
		JsonObject rprops = fromIngredient.RecipeAttributes;
		if (rprops == null || !rprops.Exists || rprops == null || !rprops["requiresContent"].Exists)
		{
			rprops = gridRecipe.Attributes?["liquidContainerProps"];
		}
		if (rprops == null || !rprops.Exists)
		{
			base.OnConsumedByCrafting(allInputSlots, stackInSlot, gridRecipe, fromIngredient, byPlayer, quantity);
			return;
		}
		ItemStack content = GetContent(stackInSlot.Itemstack);
		float litres = rprops["requiresLitres"].AsFloat();
		int q = (int)(GetContainableProps(content).ItemsPerLitre * litres / (float)stackInSlot.StackSize);
		if (rprops.IsTrue("consumeContainer"))
		{
			stackInSlot.Itemstack.StackSize -= quantity;
			if (stackInSlot.Itemstack.StackSize <= 0)
			{
				stackInSlot.Itemstack = null;
				stackInSlot.MarkDirty();
			}
		}
		else
		{
			TryTakeContent(stackInSlot.Itemstack, q);
		}
	}

	public static string PerishableInfoCompact(ICoreAPI Api, ItemSlot contentSlot, float ripenRate, bool withStackName = true)
	{
		StringBuilder dsc = new StringBuilder();
		if (withStackName)
		{
			dsc.Append(contentSlot.Itemstack.GetName());
		}
		TransitionState[] transitionStates = contentSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(Api.World, contentSlot);
		if (transitionStates != null)
		{
			for (int i = 0; i < transitionStates.Length; i++)
			{
				string comma = ", ";
				TransitionState state = transitionStates[i];
				TransitionableProperties prop = state.Props;
				float perishRate = contentSlot.Itemstack.Collectible.GetTransitionRateMul(Api.World, contentSlot, prop.Type);
				if (perishRate <= 0f)
				{
					continue;
				}
				float transitionLevel = state.TransitionLevel;
				float freshHoursLeft = state.FreshHoursLeft / perishRate;
				switch (prop.Type)
				{
				case EnumTransitionType.Perish:
				{
					dsc.Append(comma);
					if (transitionLevel > 0f)
					{
						dsc.Append(Lang.Get("{0}% spoiled", (int)Math.Round(transitionLevel * 100f)));
						break;
					}
					double hoursPerday2 = Api.World.Calendar.HoursPerDay;
					if ((double)freshHoursLeft / hoursPerday2 >= (double)Api.World.Calendar.DaysPerYear)
					{
						dsc.Append(Lang.Get("fresh for {0} years", Math.Round((double)freshHoursLeft / hoursPerday2 / (double)Api.World.Calendar.DaysPerYear, 1)));
					}
					else if ((double)freshHoursLeft > hoursPerday2)
					{
						dsc.Append(Lang.Get("fresh for {0} days", Math.Round((double)freshHoursLeft / hoursPerday2, 1)));
					}
					else
					{
						dsc.Append(Lang.Get("fresh for {0} hours", Math.Round(freshHoursLeft, 1)));
					}
					break;
				}
				case EnumTransitionType.Ripen:
				{
					dsc.Append(comma);
					if (transitionLevel > 0f)
					{
						dsc.Append(Lang.Get("{1:0.#} days left to ripen ({0}%)", (int)Math.Round(transitionLevel * 100f), (state.TransitionHours - state.TransitionedHours) / Api.World.Calendar.HoursPerDay / ripenRate));
						break;
					}
					double hoursPerday = Api.World.Calendar.HoursPerDay;
					if ((double)freshHoursLeft / hoursPerday >= (double)Api.World.Calendar.DaysPerYear)
					{
						dsc.Append(Lang.Get("will ripen in {0} years", Math.Round((double)freshHoursLeft / hoursPerday / (double)Api.World.Calendar.DaysPerYear, 1)));
					}
					else if ((double)freshHoursLeft > hoursPerday)
					{
						dsc.Append(Lang.Get("will ripen in {0} days", Math.Round((double)freshHoursLeft / hoursPerday, 1)));
					}
					else
					{
						dsc.Append(Lang.Get("will ripen in {0} hours", Math.Round(freshHoursLeft, 1)));
					}
					break;
				}
				}
			}
		}
		return dsc.ToString();
	}
}
