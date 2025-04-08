using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockPeatPile : Block
{
	public Cuboidf[][] CollisionBoxesByFillLevel;

	public BlockPeatPile()
	{
		CollisionBoxesByFillLevel = new Cuboidf[9][];
		for (int i = 0; i <= 8; i++)
		{
			CollisionBoxesByFillLevel[i] = new Cuboidf[1]
			{
				new Cuboidf(0f, 0f, 0f, 1f, (float)i * 0.125f, 1f)
			};
		}
	}

	public int FillLevel(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockEntity be = blockAccessor.GetBlockEntity(pos);
		if (be is BlockEntityPeatPile)
		{
			return (int)Math.Ceiling((double)((BlockEntityPeatPile)be).OwnStackSize / 4.0);
		}
		return 1;
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return CollisionBoxesByFillLevel[FillLevel(blockAccessor, pos)];
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return CollisionBoxesByFillLevel[FillLevel(blockAccessor, pos)];
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
		if (be is BlockEntityPeatPile)
		{
			return ((BlockEntityPeatPile)be).OnPlayerInteract(byPlayer);
		}
		return false;
	}

	internal bool Construct(ItemSlot slot, IWorldAccessor world, BlockPos pos, IPlayer player)
	{
		Block belowBlock = world.BlockAccessor.GetBlock(pos.DownCopy());
		if (!belowBlock.CanAttachBlockAt(world.BlockAccessor, this, pos.DownCopy(), BlockFacing.UP) && (belowBlock != this || FillLevel(world.BlockAccessor, pos.DownCopy()) != 8))
		{
			return false;
		}
		world.BlockAccessor.SetBlock(BlockId, pos);
		BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
		if (be is BlockEntityPeatPile)
		{
			BlockEntityPeatPile pile = (BlockEntityPeatPile)be;
			if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
			{
				pile.inventory[0].Itemstack = slot.Itemstack.Clone();
				pile.inventory[0].Itemstack.StackSize = 1;
			}
			else
			{
				pile.inventory[0].Itemstack = slot.TakeOut(player.Entity.Controls.CtrlKey ? pile.BulkTakeQuantity : pile.DefaultTakeQuantity);
			}
			pile.MarkDirty();
			world.BlockAccessor.MarkBlockDirty(pos);
			world.PlaySoundAt(new AssetLocation("sounds/block/dirt"), pos, (double)(pile.inventory[0].Itemstack.StackSize / pile.MaxStackSize) - 0.5, player, randomizePitch: false);
		}
		if (CollisionTester.AabbIntersect(GetCollisionBoxes(world.BlockAccessor, pos)[0], pos.X, pos.Y, pos.Z, player.Entity.SelectionBox, player.Entity.SidedPos.XYZ))
		{
			player.Entity.SidedPos.Y += GetCollisionBoxes(world.BlockAccessor, pos)[0].Y2;
		}
		(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		return true;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return new BlockDropItemStack[0];
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[0];
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityPeatPile beg)
		{
			return beg.inventory.FirstNonEmptySlot?.Itemstack.Clone();
		}
		return null;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		Block belowBlock = world.BlockAccessor.GetBlock(pos.DownCopy());
		if (!belowBlock.CanAttachBlockAt(world.BlockAccessor, this, pos.DownCopy(), BlockFacing.UP) && (belowBlock != this || FillLevel(world.BlockAccessor, pos.DownCopy()) < 8))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[4]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-peatpile-add",
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = "shift",
				Itemstacks = new ItemStack[1]
				{
					new ItemStack(this)
				},
				GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
				{
					if (world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityPeatPile blockEntityPeatPile2 && blockEntityPeatPile2.inventory[0].Itemstack != null)
					{
						ItemStack itemStack2 = blockEntityPeatPile2.inventory[0].Itemstack.Clone();
						itemStack2.StackSize = blockEntityPeatPile2.DefaultTakeQuantity;
						return new ItemStack[1] { itemStack2 };
					}
					return (ItemStack[])null;
				}
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-peatpile-remove",
				MouseButton = EnumMouseButton.Right
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-peatpile-4add",
				MouseButton = EnumMouseButton.Right,
				HotKeyCodes = new string[2] { "ctrl", "shift" },
				Itemstacks = new ItemStack[1]
				{
					new ItemStack(this)
				},
				GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
				{
					if (world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityPeatPile blockEntityPeatPile && blockEntityPeatPile.inventory[0].Itemstack != null)
					{
						ItemStack itemStack = blockEntityPeatPile.inventory[0].Itemstack.Clone();
						itemStack.StackSize = blockEntityPeatPile.BulkTakeQuantity;
						return new ItemStack[1] { itemStack };
					}
					return (ItemStack[])null;
				}
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-peatpile-4remove",
				HotKeyCode = "ctrl",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
