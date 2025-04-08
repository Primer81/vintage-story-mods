using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockIngotMold : Block
{
	private WorldInteraction[] interactionsLeft;

	private WorldInteraction[] interactionsRight;

	private Cuboidf[] oneMoldBoxes = new Cuboidf[1]
	{
		new Cuboidf(0f, 0f, 0f, 1f, 0.1875f, 1f)
	};

	private Cuboidf[] twoMoldBoxesNS = new Cuboidf[2]
	{
		new Cuboidf(0f, 0f, 0f, 0.5f, 0.1875f, 1f),
		new Cuboidf(0.5f, 0f, 0f, 1f, 0.1875f, 1f)
	};

	private Cuboidf[] twoMoldBoxesEW = new Cuboidf[2]
	{
		new Cuboidf(0f, 0f, 0f, 1f, 0.1875f, 0.5f),
		new Cuboidf(0f, 0f, 0.5f, 1f, 0.1875f, 1f)
	};

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		if (LastCodePart() == "raw")
		{
			return;
		}
		interactionsLeft = ObjectCacheUtil.GetOrCreate(api, "ingotmoldBlockInteractionsLeft", delegate
		{
			List<ItemStack> list2 = new List<ItemStack>();
			foreach (CollectibleObject current2 in api.World.Collectibles)
			{
				if (current2 is BlockSmeltedContainer)
				{
					list2.Add(new ItemStack(current2));
				}
			}
			return new WorldInteraction[4]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-pour",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list2.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { IsFullLeft: false })) ? null : wi.Itemstacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-takeingot",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { IsFullLeft: not false } blockEntityIngotMold6 && blockEntityIngotMold6.IsHardenedLeft
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-pickup",
					HotKeyCode = null,
					RequireFreeHand = true,
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { ContentsRight: null } blockEntityIngotMold5 && blockEntityIngotMold5.ContentsLeft == null
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-placemold",
					HotKeyCode = "shift",
					Itemstacks = new ItemStack[1]
					{
						new ItemStack(this)
					},
					MouseButton = EnumMouseButton.Right,
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { QuantityMolds: <2 })) ? null : wi.Itemstacks
				}
			};
		});
		interactionsRight = ObjectCacheUtil.GetOrCreate(api, "ingotmoldBlockInteractionsRight", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				if (current is BlockSmeltedContainer)
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[3]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-pour",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray(),
					GetMatchingStacks = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => (!(api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { QuantityMolds: >1, IsFullRight: false })) ? null : wi.Itemstacks
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-takeingot",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { QuantityMolds: >1, IsFullRight: not false } blockEntityIngotMold2 && blockEntityIngotMold2.IsHardenedRight
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-ingotmold-pickup",
					HotKeyCode = null,
					RequireFreeHand = true,
					MouseButton = EnumMouseButton.Right,
					ShouldApply = (WorldInteraction wi, BlockSelection bs, EntitySelection es) => api.World.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotMold { QuantityMolds: >1, ContentsRight: null } blockEntityIngotMold && blockEntityIngotMold.ContentsLeft == null
				}
			};
		});
	}

	public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
	{
		return true;
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
	{
		if (!(api.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityIngotMold { QuantityMolds: not 1 } betm))
		{
			return oneMoldBoxes;
		}
		int index = BlockFacing.HorizontalFromAngle(betm.MeshAngle).Index;
		if (index == 0 || index == 2)
		{
			return twoMoldBoxesEW;
		}
		return twoMoldBoxesNS;
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return GetSelectionBoxes(blockAccessor, pos);
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		if (blockSel == null)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
			return;
		}
		BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position.AddCopy(blockSel.Face.Opposite));
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (byPlayer != null && be is BlockEntityIngotMold && ((BlockEntityIngotMold)be).OnPlayerInteract(byPlayer, blockSel.Face, blockSel.HitPosition))
		{
			handling = EnumHandHandling.PreventDefault;
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (blockSel == null)
		{
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
		if (be is BlockEntityIngotMold)
		{
			return ((BlockEntityIngotMold)be).OnPlayerInteract(byPlayer, blockSel.Face, blockSel.HitPosition);
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
	{
		if (world.Rand.NextDouble() > 0.05)
		{
			base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
			return;
		}
		BlockEntityIngotMold be = GetBlockEntity<BlockEntityIngotMold>(pos);
		if ((be != null && be.TemperatureLeft > 300f) || (be != null && be.TemperatureRight > 300f))
		{
			entity.ReceiveDamage(new DamageSource
			{
				Source = EnumDamageSource.Block,
				SourceBlock = this,
				Type = EnumDamageType.Fire,
				SourcePos = pos.ToVec3d()
			}, 0.5f);
		}
		base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
	}

	public override float GetTraversalCost(BlockPos pos, EnumAICreatureType creatureType)
	{
		if (creatureType == EnumAICreatureType.LandCreature || creatureType == EnumAICreatureType.Humanoid)
		{
			BlockEntityIngotMold be = GetBlockEntity<BlockEntityIngotMold>(pos);
			if ((be != null && be.TemperatureLeft > 300f) || be.TemperatureRight > 300f)
			{
				return 10000f;
			}
		}
		return 0f;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!byPlayer.Entity.Controls.ShiftKey)
		{
			failureCode = "onlywhensneaking";
			return false;
		}
		if (!world.BlockAccessor.GetBlock(blockSel.Position.DownCopy()).CanAttachBlockAt(world.BlockAccessor, this, blockSel.Position.DownCopy(), BlockFacing.UP))
		{
			failureCode = "requiresolidground";
			return false;
		}
		return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return Drops;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		List<ItemStack> stacks = new List<ItemStack>();
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityIngotMold { QuantityMolds: var moldsAmount } bei)
		{
			if (bei.ShatteredLeft)
			{
				moldsAmount--;
			}
			if (bei.ShatteredRight)
			{
				moldsAmount--;
			}
			stacks.Add(new ItemStack(this, moldsAmount));
			ItemStack stackl = bei.GetStateAwareContentsLeft();
			if (stackl != null)
			{
				stacks.Add(stackl);
			}
			ItemStack stackr = bei.GetStateAwareContentsRight();
			if (stackr != null)
			{
				stacks.Add(stackr);
			}
		}
		else
		{
			stacks.Add(new ItemStack(this));
		}
		return stacks.ToArray();
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return ((selection.SelectionBoxIndex == 0) ? interactionsLeft : interactionsRight).Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		bool num = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
		if (num && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityIngotMold beim)
		{
			BlockPos targetPos = (blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
			double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + blockSel.HitPosition.X);
			double dz = byPlayer.Entity.Pos.Z - ((double)targetPos.Z + blockSel.HitPosition.Z);
			float roundRad = (float)(int)Math.Round((float)Math.Atan2(y, dz) / ((float)Math.PI / 2f)) * ((float)Math.PI / 2f);
			beim.MeshAngle = roundRad;
			beim.MarkDirty();
		}
		return num;
	}
}
