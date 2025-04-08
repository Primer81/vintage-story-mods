using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockCoalPile : Block, IBlockItemPile, IIgnitable
{
	private Cuboidf[][] CollisionBoxesByFillLevel;

	private WorldInteraction[] interactions;

	public BlockCoalPile()
	{
		CollisionBoxesByFillLevel = new Cuboidf[9][];
		CollisionBoxesByFillLevel[0] = new Cuboidf[0];
		for (int i = 1; i < CollisionBoxesByFillLevel.Length; i++)
		{
			CollisionBoxesByFillLevel[i] = new Cuboidf[1]
			{
				new Cuboidf(0f, 0f, 0f, 1f, (float)i * 0.125f, 1f)
			};
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "coalBlockInteractions", delegate
		{
			List<ItemStack> list = BlockBehaviorCanIgnite.CanIgniteStacks(api, withFirestarter: false);
			return new WorldInteraction[3]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-coalpile-addcoal",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = "shift",
					Itemstacks = new ItemStack[1]
					{
						new ItemStack(api.World.GetItem(new AssetLocation("charcoal")), 2)
					}
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-coalpile-removecoal",
					MouseButton = EnumMouseButton.Right,
					HotKeyCode = null
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-forge-ignite",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityForge { CanIgnite: not false, IsBurning: false }) ? wi.Itemstacks : null
				}
			};
		});
	}

	public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
	{
		if (!(world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCoalPile bea))
		{
			base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
			return;
		}
		decalModelData.Clear();
		bea.GetDecalMesh(decalTexSource, out decalModelData);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCoalPile bea)
		{
			return bea.inventory[0]?.Itemstack?.Clone();
		}
		return base.OnPickBlock(world, pos);
	}

	public int GetLayercount(IWorldAccessor world, BlockPos pos)
	{
		return GetLayercount(world.BlockAccessor, pos);
	}

	protected int GetLayercount(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityCoalPile { inventory: not null } bea)
		{
			return Math.Min(bea.Layers, CollisionBoxesByFillLevel.Length - 1);
		}
		return 0;
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return CollisionBoxesByFillLevel[GetLayercount(blockAccessor, pos)];
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return CollisionBoxesByFillLevel[GetLayercount(blockAccessor, pos)];
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return new BlockDropItemStack[0];
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[0];
	}

	public override bool CanAcceptFallOnto(IWorldAccessor world, BlockPos pos, Block fallingBlock, TreeAttribute blockEntityAttributes)
	{
		if (fallingBlock is BlockCoalPile)
		{
			return world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCoalPile;
		}
		return false;
	}

	public override bool OnFallOnto(IWorldAccessor world, BlockPos pos, Block block, TreeAttribute blockEntityAttributes)
	{
		if (block is BlockCoalPile && world.BlockAccessor.GetBlockEntity(pos) is BlockEntityCoalPile be)
		{
			return be.MergeWith(blockEntityAttributes);
		}
		return base.OnFallOnto(world, pos, block, blockEntityAttributes);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
		if (be is BlockEntityCoalPile)
		{
			return ((BlockEntityCoalPile)be).OnPlayerInteract(byPlayer);
		}
		return false;
	}

	public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
	{
		if (pos == null)
		{
			return base.GetLightHsv(blockAccessor, pos, stack);
		}
		BlockEntityCoalPile obj = blockAccessor.GetBlockEntity(pos) as BlockEntityCoalPile;
		if (obj != null && obj.IsBurning)
		{
			return new byte[3] { 0, 7, 8 };
		}
		return base.GetLightHsv(blockAccessor, pos, stack);
	}

	EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
	{
		if ((byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityCoalPile).IsBurning)
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
		if (!(byEntity.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityCoalPile { CanIgnite: not false }))
		{
			return EnumIgniteState.NotIgnitablePreventDefault;
		}
		if (secondsIgniting > 0.25f && (int)(30f * secondsIgniting) % 9 == 1)
		{
			Random rand = byEntity.World.Rand;
			Vec3d dpos = new Vec3d((double)((float)pos.X + 0.25f) + 0.5 * rand.NextDouble(), (float)pos.Y + 0.875f, (double)((float)pos.Z + 0.25f) + 0.5 * rand.NextDouble());
			Block blockFire = byEntity.World.GetBlock(new AssetLocation("fire"));
			AdvancedParticleProperties props = blockFire.ParticleProperties[blockFire.ParticleProperties.Length - 1];
			props.basePos = dpos;
			props.Quantity.avg = 1f;
			IPlayer byPlayer = null;
			if (byEntity is EntityPlayer)
			{
				byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			byEntity.World.SpawnParticles(props, byPlayer);
			props.Quantity.avg = 0f;
		}
		if (secondsIgniting >= 1.5f)
		{
			return EnumIgniteState.IgniteNow;
		}
		return EnumIgniteState.Ignitable;
	}

	public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
	{
		if (!(secondsIgniting < 1.45f))
		{
			handling = EnumHandling.PreventDefault;
			IPlayer byPlayer = null;
			if (byEntity is EntityPlayer)
			{
				byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			if (byPlayer != null)
			{
				(byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityCoalPile)?.TryIgnite();
			}
		}
	}

	public bool Construct(ItemSlot slot, IWorldAccessor world, BlockPos pos, IPlayer player)
	{
		if (!world.BlockAccessor.GetBlock(pos).IsReplacableBy(this))
		{
			return false;
		}
		if (!world.BlockAccessor.GetBlock(pos.DownCopy()).CanAttachBlockAt(world.BlockAccessor, this, pos.DownCopy(), BlockFacing.UP))
		{
			return false;
		}
		world.BlockAccessor.SetBlock(BlockId, pos);
		BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
		if (be is BlockEntityCoalPile)
		{
			BlockEntityCoalPile pile = (BlockEntityCoalPile)be;
			if (player == null || player.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				pile.inventory[0].Itemstack = slot.TakeOut((player != null && player.Entity.Controls.CtrlKey) ? pile.BulkTakeQuantity : pile.DefaultTakeQuantity);
				slot.MarkDirty();
			}
			else
			{
				pile.inventory[0].Itemstack = slot.Itemstack.Clone();
				pile.inventory[0].Itemstack.StackSize = Math.Min(pile.inventory[0].Itemstack.StackSize, pile.MaxStackSize);
			}
			pile.MarkDirty();
			world.BlockAccessor.MarkBlockDirty(pos);
			world.PlaySoundAt(pile.SoundLocation, pos, -0.5, player);
		}
		return true;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (!world.BlockAccessor.GetBlock(pos.DownCopy()).CanAttachBlockAt(world.BlockAccessor, this, pos.DownCopy(), BlockFacing.UP))
		{
			world.BlockAccessor.BreakBlock(pos, null);
			return;
		}
		Block block = world.BlockAccessor.GetBlock(neibpos);
		Block neibliqBlock = world.BlockAccessor.GetBlock(neibpos, 2);
		JsonObject attributes = block.Attributes;
		if (attributes == null || !attributes.IsTrue("smothersFire"))
		{
			JsonObject attributes2 = neibliqBlock.Attributes;
			if (attributes2 == null || !attributes2.IsTrue("smothersFire"))
			{
				goto IL_00b1;
			}
		}
		(api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityCoalPile)?.Extinguish();
		goto IL_00b1;
		IL_00b1:
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		if (blockAccessor.GetBlockEntity(pos) is BlockEntityCoalPile be)
		{
			return be.OwnStackSize == be.MaxStackSize;
		}
		return base.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
		if (world.Side != EnumAppSide.Server)
		{
			return;
		}
		BlockEntityCoalPile obj = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCoalPile;
		if (obj == null || !obj.IsBurning)
		{
			return;
		}
		long lastBurnMs = entity.Attributes.GetLong("lastCoalBurnTick", 0L);
		if (world.ElapsedMilliseconds - lastBurnMs > 1000)
		{
			entity.ReceiveDamage(new DamageSource
			{
				DamageTier = 0,
				Source = EnumDamageSource.Block,
				SourceBlock = this,
				SourcePos = pos.ToVec3d(),
				Type = EnumDamageType.Fire
			}, 1f);
			entity.Attributes.SetLong("lastCoalBurnTick", world.ElapsedMilliseconds);
			if (world.Rand.NextDouble() < 0.125)
			{
				entity.Ignite();
			}
		}
		if (lastBurnMs > world.ElapsedMilliseconds)
		{
			entity.Attributes.SetLong("lastCoalBurnTick", world.ElapsedMilliseconds);
		}
	}

	public override float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
	{
		if (creatureType == EnumAICreatureType.LandCreature || creatureType == EnumAICreatureType.Humanoid)
		{
			BlockEntityCoalPile blockEntity = GetBlockEntity<BlockEntityCoalPile>(pos);
			if (blockEntity == null || !blockEntity.IsBurning)
			{
				return 1f;
			}
			return 10000f;
		}
		return base.GetTraversalCost(pos, creatureType);
	}
}
