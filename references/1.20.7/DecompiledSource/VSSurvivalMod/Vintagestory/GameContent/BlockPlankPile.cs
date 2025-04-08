using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockPlankPile : Block
{
	private Cuboidf[][] CollisionBoxesByFillLevel;

	public BlockPlankPile()
	{
		CollisionBoxesByFillLevel = new Cuboidf[17][];
		for (int i = 0; i <= 16; i++)
		{
			CollisionBoxesByFillLevel[i] = new Cuboidf[1]
			{
				new Cuboidf(0f, 0f, 0f, 1f, (float)i * 0.125f / 2f, 1f)
			};
		}
	}

	public int FillLevel(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockEntity be = blockAccessor.GetBlockEntity(pos);
		if (be is BlockEntityPlankPile)
		{
			return ((BlockEntityPlankPile)be).OwnStackSize / 3;
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
		if (be is BlockEntityPlankPile)
		{
			return ((BlockEntityPlankPile)be).OnPlayerInteract(byPlayer);
		}
		return false;
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		(world.BlockAccessor.GetBlockEntity(pos) as BlockEntityItemPile)?.OnBlockBroken(byPlayer);
		base.OnBlockBroken(world, pos, byPlayer);
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
		BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
		if (be is BlockEntityPlankPile)
		{
			ItemStack stack = ((BlockEntityPlankPile)be).inventory[0].Itemstack;
			if (stack != null)
			{
				ItemStack itemStack = stack.Clone();
				itemStack.StackSize = 1;
				return itemStack;
			}
		}
		return new ItemStack(this);
	}

	internal bool Construct(ItemSlot slot, IWorldAccessor world, BlockPos pos, IPlayer player)
	{
		Block belowBlock = world.BlockAccessor.GetBlock(pos.DownCopy());
		if (!belowBlock.CanAttachBlockAt(world.BlockAccessor, this, pos.DownCopy(), BlockFacing.UP) && (belowBlock != this || FillLevel(world.BlockAccessor, pos.DownCopy()) != 16))
		{
			return false;
		}
		if (!world.BlockAccessor.GetBlock(pos).IsReplacableBy(this))
		{
			return false;
		}
		world.BlockAccessor.SetBlock(BlockId, pos);
		BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
		if (be is BlockEntityPlankPile)
		{
			BlockEntityPlankPile pile = (BlockEntityPlankPile)be;
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
			world.PlaySoundAt(new AssetLocation("sounds/block/planks"), pos, (double)(pile.inventory[0].Itemstack.StackSize / pile.MaxStackSize) - 0.5, player, randomizePitch: false);
		}
		return true;
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		Block belowBlock = world.BlockAccessor.GetBlock(pos.DownCopy());
		if (!belowBlock.CanAttachBlockAt(world.BlockAccessor, belowBlock, pos.DownCopy(), BlockFacing.UP))
		{
			int level = FillLevel(world.BlockAccessor, pos.DownCopy());
			if (belowBlock != this || level < 16)
			{
				world.BlockAccessor.BreakBlock(pos, null);
			}
		}
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[4]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-plankpile-add",
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = "shift",
				Itemstacks = new ItemStack[1]
				{
					new ItemStack(this)
				},
				GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
				{
					BlockEntityPlankPile blockEntityPlankPile2 = world.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityPlankPile;
					ItemStack itemStack2 = blockEntityPlankPile2?.inventory[0].Itemstack?.Clone();
					if (itemStack2 == null)
					{
						return (ItemStack[])null;
					}
					itemStack2.StackSize = blockEntityPlankPile2.BulkTakeQuantity;
					return (blockEntityPlankPile2 != null && blockEntityPlankPile2.MaxStackSize > blockEntityPlankPile2.inventory[0].StackSize) ? new ItemStack[1] { itemStack2 } : null;
				}
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-plankpile-remove",
				MouseButton = EnumMouseButton.Right
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-plankpile-4add",
				MouseButton = EnumMouseButton.Right,
				HotKeyCodes = new string[2] { "ctrl", "shift" },
				Itemstacks = new ItemStack[1]
				{
					new ItemStack(this)
				},
				GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
				{
					BlockEntityPlankPile blockEntityPlankPile = world.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityPlankPile;
					ItemStack itemStack = blockEntityPlankPile?.inventory[0].Itemstack?.Clone();
					if (itemStack == null)
					{
						return (ItemStack[])null;
					}
					itemStack.StackSize = blockEntityPlankPile.BulkTakeQuantity;
					return (blockEntityPlankPile != null && blockEntityPlankPile.MaxStackSize > blockEntityPlankPile.inventory[0].StackSize) ? new ItemStack[1] { itemStack } : null;
				}
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-plankpile-4remove",
				HotKeyCode = "ctrl",
				MouseButton = EnumMouseButton.Right
			}
		};
	}
}
