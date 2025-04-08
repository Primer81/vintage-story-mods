using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockIngotPile : Block
{
	public Cuboidf[][] CollisionBoxesByFillLevel;

	public BlockIngotPile()
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
		if (be is BlockEntityIngotPile)
		{
			return (int)Math.Ceiling((double)((BlockEntityIngotPile)be).OwnStackSize / 8.0);
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
		if (be is BlockEntityIngotPile)
		{
			return ((BlockEntityIngotPile)be).OnPlayerInteract(byPlayer);
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
		if (be is BlockEntityIngotPile)
		{
			BlockEntityIngotPile pile = (BlockEntityIngotPile)be;
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
			world.PlaySoundAt(new AssetLocation("sounds/block/ingot"), pos, (double)(pile.inventory[0].Itemstack.StackSize / pile.MaxStackSize) - 0.5, player, randomizePitch: false);
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

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		Block belowBlock = world.BlockAccessor.GetBlock(pos.DownCopy());
		if (!belowBlock.CanAttachBlockAt(world.BlockAccessor, this, pos.DownCopy(), BlockFacing.UP) && (belowBlock != this || FillLevel(world.BlockAccessor, pos.DownCopy()) < 8))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		if (!(capi.World.BlockAccessor.GetBlockEntity(pos) is BlockEntityIngotPile be))
		{
			return base.GetRandomColor(capi, pos, facing, rndIndex);
		}
		if (be.MetalType == null)
		{
			return base.GetRandomColor(capi, pos, facing, rndIndex);
		}
		return capi.BlockTextureAtlas.GetRandomColor(Textures[be.MetalType].Baked.TextureSubId, rndIndex);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
		if (be is BlockEntityIngotPile)
		{
			ItemStack stack = ((BlockEntityIngotPile)be).inventory[0].Itemstack;
			if (stack != null)
			{
				ItemStack itemStack = stack.Clone();
				itemStack.StackSize = 1;
				return itemStack;
			}
		}
		return new ItemStack(this);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return new WorldInteraction[4]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-ingotpile-add",
				MouseButton = EnumMouseButton.Right,
				HotKeyCode = "shift",
				Itemstacks = new ItemStack[1]
				{
					new ItemStack(this)
				},
				GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
				{
					if (world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotPile blockEntityIngotPile2 && blockEntityIngotPile2.MaxStackSize > blockEntityIngotPile2.inventory[0].StackSize && blockEntityIngotPile2.inventory[0].Itemstack != null)
					{
						ItemStack itemStack2 = blockEntityIngotPile2.inventory[0].Itemstack.Clone();
						itemStack2.StackSize = blockEntityIngotPile2.DefaultTakeQuantity;
						return new ItemStack[1] { itemStack2 };
					}
					return (ItemStack[])null;
				}
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-ingotpile-remove",
				MouseButton = EnumMouseButton.Right
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-ingotpile-4add",
				MouseButton = EnumMouseButton.Right,
				HotKeyCodes = new string[2] { "ctrl", "shift" },
				Itemstacks = new ItemStack[1]
				{
					new ItemStack(this)
				},
				GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
				{
					if (world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntityIngotPile blockEntityIngotPile && blockEntityIngotPile.MaxStackSize > blockEntityIngotPile.inventory[0].StackSize && blockEntityIngotPile.inventory[0].Itemstack != null)
					{
						ItemStack itemStack = blockEntityIngotPile.inventory[0].Itemstack.Clone();
						itemStack.StackSize = blockEntityIngotPile.BulkTakeQuantity;
						return new ItemStack[1] { itemStack };
					}
					return (ItemStack[])null;
				}
			},
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-ingotpile-4remove",
				HotKeyCode = "ctrl",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
