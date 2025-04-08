using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockFirepit : Block, IIgnitable, ISmokeEmitter
{
	public bool IsExtinct;

	private AdvancedParticleProperties[] ringParticles;

	private Vec3f[] basePos;

	private WorldInteraction[] interactions;

	public int Stage => LastCodePart() switch
	{
		"construct1" => 1, 
		"construct2" => 2, 
		"construct3" => 3, 
		"construct4" => 4, 
		_ => 5, 
	};

	public string NextStageCodePart => LastCodePart() switch
	{
		"construct1" => "construct2", 
		"construct2" => "construct3", 
		"construct3" => "construct4", 
		"construct4" => "cold", 
		_ => "cold", 
	};

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		IsExtinct = LastCodePart() != "lit";
		if (!IsExtinct && api.Side == EnumAppSide.Client)
		{
			ringParticles = new AdvancedParticleProperties[ParticleProperties.Length * 4];
			basePos = new Vec3f[ringParticles.Length];
			Cuboidf[] spawnBoxes = new Cuboidf[4]
			{
				new Cuboidf(0.125f, 0f, 0.125f, 0.3125f, 0.5f, 0.875f),
				new Cuboidf(0.7125f, 0f, 0.125f, 0.875f, 0.5f, 0.875f),
				new Cuboidf(0.125f, 0f, 0.125f, 0.875f, 0.5f, 0.3125f),
				new Cuboidf(0.125f, 0f, 0.7125f, 0.875f, 0.5f, 0.875f)
			};
			for (int i = 0; i < ParticleProperties.Length; i++)
			{
				for (int j = 0; j < 4; j++)
				{
					AdvancedParticleProperties props = ParticleProperties[i].Clone();
					Cuboidf box = spawnBoxes[j];
					basePos[i * 4 + j] = new Vec3f(0f, 0f, 0f);
					props.PosOffset[0].avg = box.MidX;
					props.PosOffset[0].var = box.Width / 2f;
					props.PosOffset[1].avg = 0.1f;
					props.PosOffset[1].var = 0.05f;
					props.PosOffset[2].avg = box.MidZ;
					props.PosOffset[2].var = box.Length / 2f;
					props.Quantity.avg /= 4f;
					props.Quantity.var /= 4f;
					ringParticles[i * 4 + j] = props;
				}
			}
		}
		interactions = ObjectCacheUtil.GetOrCreate(api, "firepitInteractions-" + Stage, delegate
		{
			List<ItemStack> list = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: true);
			return new WorldInteraction[3]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-firepit-open",
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection) => Stage == 5
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-firepit-ignite",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "shift",
					Itemstacks = list.ToArray(),
					GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
					{
						BlockEntityFirepit blockEntityFirepit = api.World.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityFirepit;
						return (blockEntityFirepit?.fuelSlot != null && !blockEntityFirepit.fuelSlot.Empty && !blockEntityFirepit.IsBurning) ? wi.Itemstacks : null;
					}
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-firepit-refuel",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "shift"
				}
			};
		});
	}

	public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
	{
		if (world.Rand.NextDouble() < 0.05)
		{
			BlockEntityFirepit blockEntity = GetBlockEntity<BlockEntityFirepit>(pos);
			if (blockEntity != null && blockEntity.IsBurning)
			{
				entity.ReceiveDamage(new DamageSource
				{
					Source = EnumDamageSource.Block,
					SourceBlock = this,
					Type = EnumDamageType.Fire,
					SourcePos = pos.ToVec3d()
				}, 0.5f);
			}
		}
		base.OnEntityInside(world, entity, pos);
	}

	EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
	{
		if ((api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFirepit).IsBurning)
		{
			if (!(secondsIgniting > 2f))
			{
				return EnumIgniteState.Ignitable;
			}
			return EnumIgniteState.IgniteNow;
		}
		return EnumIgniteState.NotIgnitable;
	}

	public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
	{
		if (!(api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityFirepit bef))
		{
			return EnumIgniteState.NotIgnitable;
		}
		return bef.GetIgnitableState(secondsIgniting);
	}

	public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
	{
		if (api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityFirepit { canIgniteFuel: false } bef)
		{
			bef.canIgniteFuel = true;
			bef.extinguishedTotalHours = api.World.Calendar.TotalHours;
		}
		handling = EnumHandling.PreventDefault;
	}

	public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
	{
		bool isWindAffected2;
		bool result = base.ShouldReceiveClientParticleTicks(world, player, pos, out isWindAffected2);
		isWindAffected = true;
		return result;
	}

	public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
	{
		if (IsExtinct)
		{
			base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
		}
		else if (manager.BlockAccess.GetBlockEntity(pos) is BlockEntityFirepit { CurrentModel: EnumFirepitModel.Wide })
		{
			for (int i = 0; i < ringParticles.Length; i++)
			{
				AdvancedParticleProperties bps = ringParticles[i];
				bps.WindAffectednesAtPos = windAffectednessAtPos;
				bps.basePos.X = (float)pos.X + basePos[i].X;
				bps.basePos.Y = (float)pos.Y + basePos[i].Y;
				bps.basePos.Z = (float)pos.Z + basePos[i].Z;
				manager.Spawn(bps);
			}
		}
		else
		{
			base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
		{
			return false;
		}
		int stage = Stage;
		ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;
		if (stage == 5)
		{
			BlockEntityFirepit bef = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityFirepit;
			if (bef != null && stack?.Block != null && stack.Block.HasBehavior<BlockBehaviorCanIgnite>() && bef.GetIgnitableState(0f) == EnumIgniteState.Ignitable)
			{
				return false;
			}
			if (bef != null && stack != null && byPlayer.Entity.Controls.ShiftKey)
			{
				if (stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.MeltingPoint > 0)
				{
					ItemStackMoveOperation op2 = new ItemStackMoveOperation(world, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.DirectMerge, 1);
					byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(bef.inputSlot, ref op2);
					if (op2.MovedQuantity > 0)
					{
						(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
						return true;
					}
				}
				if (stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.BurnTemperature > 0)
				{
					ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.DirectMerge, 1);
					byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(bef.fuelSlot, ref op);
					if (op.MovedQuantity > 0)
					{
						(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
						JsonObject itemAttributes = stack.ItemAttributes;
						AssetLocation loc = ((itemAttributes != null && itemAttributes["placeSound"].Exists) ? AssetLocation.Create(stack.ItemAttributes["placeSound"].AsString(), stack.Collectible.Code.Domain) : null);
						if (loc != null)
						{
							api.World.PlaySoundAt(loc.WithPathPrefixOnce("sounds/"), blockSel.Position.X, blockSel.Position.InternalY, blockSel.Position.Z, byPlayer, 0.88f + (float)api.World.Rand.NextDouble() * 0.24f, 16f);
						}
						return true;
					}
				}
			}
			if (stack != null && (stack.Collectible.Attributes?.IsTrue("mealContainer")).GetValueOrDefault())
			{
				ItemSlot potSlot = null;
				if (bef?.inputStack?.Collectible is BlockCookedContainer)
				{
					potSlot = bef.inputSlot;
				}
				if (bef?.outputStack?.Collectible is BlockCookedContainer)
				{
					potSlot = bef.outputSlot;
				}
				if (potSlot != null)
				{
					BlockCookedContainer blockPot = potSlot.Itemstack.Collectible as BlockCookedContainer;
					ItemSlot targetSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
					if (byPlayer.InventoryManager.ActiveHotbarSlot.StackSize > 1)
					{
						targetSlot = new DummySlot(targetSlot.TakeOut(1));
						byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
						blockPot.ServeIntoStack(targetSlot, potSlot, world);
						if (!byPlayer.InventoryManager.TryGiveItemstack(targetSlot.Itemstack, slotNotifyEffect: true))
						{
							world.SpawnItemEntity(targetSlot.Itemstack, byPlayer.Entity.ServerPos.XYZ);
						}
					}
					else
					{
						blockPot.ServeIntoStack(targetSlot, potSlot, world);
					}
				}
				else if (!bef.inputSlot.Empty || byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(api.World, bef.inputSlot) == 0)
				{
					bef.OnPlayerRightClick(byPlayer, blockSel);
				}
				return true;
			}
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		if (stack != null && TryConstruct(world, blockSel.Position, stack.Collectible, byPlayer))
		{
			if (byPlayer != null && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);
			}
			return true;
		}
		return false;
	}

	public bool TryConstruct(IWorldAccessor world, BlockPos pos, CollectibleObject obj, IPlayer player)
	{
		int stage = Stage;
		JsonObject attributes = obj.Attributes;
		if (attributes == null || !attributes.IsTrue("firepitConstructable"))
		{
			return false;
		}
		switch (stage)
		{
		case 5:
			return false;
		case 4:
			if (IsFirewoodPile(world, pos.DownCopy()))
			{
				Block charcoalPitBlock = world.GetBlock(new AssetLocation("charcoalpit"));
				if (charcoalPitBlock != null)
				{
					world.BlockAccessor.SetBlock(charcoalPitBlock.BlockId, pos);
					(world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCharcoalPit)?.Init(player);
					(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
					return true;
				}
			}
			break;
		}
		Block block = world.GetBlock(CodeWithParts(NextStageCodePart));
		world.BlockAccessor.ExchangeBlock(block.BlockId, pos);
		world.BlockAccessor.MarkBlockDirty(pos);
		if (block.Sounds != null)
		{
			world.PlaySoundAt(block.Sounds.Place, pos, -0.5, player);
		}
		if (stage == 4)
		{
			BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
			if (be is BlockEntityFirepit)
			{
				((BlockEntityFirepit)be).inventory[0].Itemstack = new ItemStack(obj, 4);
			}
		}
		(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		return true;
	}

	public static bool IsFirewoodPile(IWorldAccessor world, BlockPos pos)
	{
		BlockEntityGroundStorage beg = world.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(pos);
		if (beg != null)
		{
			return beg.Inventory[0]?.Itemstack?.Collectible is ItemFirewood;
		}
		return false;
	}

	public static int GetFireWoodQuanity(IWorldAccessor world, BlockPos pos)
	{
		return (world.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(pos)?.Inventory[0]?.StackSize).GetValueOrDefault();
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
	{
		if (creatureType == EnumAICreatureType.LandCreature || creatureType == EnumAICreatureType.Humanoid)
		{
			BlockEntityFirepit blockEntity = GetBlockEntity<BlockEntityFirepit>(pos);
			if (blockEntity == null || !blockEntity.IsBurning)
			{
				return 1f;
			}
			return 10000f;
		}
		return base.GetTraversalCost(pos, creatureType);
	}

	public bool EmitsSmoke(BlockPos pos)
	{
		return (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFirepit)?.IsBurning ?? false;
	}
}
