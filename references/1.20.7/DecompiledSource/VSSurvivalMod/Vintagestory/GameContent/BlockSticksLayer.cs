using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockSticksLayer : Block
{
	public BlockFacing Orientation { get; set; }

	static BlockSticksLayer()
	{
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Orientation = BlockFacing.FromFirstLetter(Variant["facing"][0]);
	}

	protected AssetLocation OrientedAsset(string orientation)
	{
		return CodeWithVariants(new string[2] { "type", "facing" }, new string[2] { "wooden", orientation });
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			BlockFacing horVer = OrientForPlacement(world.BlockAccessor, byPlayer, blockSel);
			string orientation = ((horVer == BlockFacing.NORTH || horVer == BlockFacing.SOUTH) ? "ns" : "ew");
			AssetLocation newCode = OrientedAsset(orientation);
			world.BlockAccessor.SetBlock(world.BlockAccessor.GetBlock(newCode).BlockId, blockSel.Position);
			return true;
		}
		return false;
	}

	protected virtual BlockFacing OrientForPlacement(IBlockAccessor world, IPlayer player, BlockSelection bs)
	{
		BlockFacing[] facings = Block.SuggestedHVOrientation(player, bs);
		BlockFacing suggested = ((facings.Length != 0) ? facings[0] : null);
		if (suggested != null && player.Entity.Controls.ShiftKey)
		{
			return suggested;
		}
		BlockPos pos = bs.Position;
		Block block = world.GetBlock(pos.WestCopy());
		Block eastBlock = world.GetBlock(pos.EastCopy());
		Block northBlock = world.GetBlock(pos.NorthCopy());
		Block southBlock = world.GetBlock(pos.SouthCopy());
		int westConnect = ((block is BlockSticksLayer wb && wb.Orientation == BlockFacing.EAST) ? 1 : 0);
		int eastConnect = ((eastBlock is BlockSticksLayer eb && eb.Orientation == BlockFacing.EAST) ? 1 : 0);
		int northConnect = ((northBlock is BlockSticksLayer nb && nb.Orientation == BlockFacing.NORTH) ? 1 : 0);
		int southConnect = ((southBlock is BlockSticksLayer sb && sb.Orientation == BlockFacing.NORTH) ? 1 : 0);
		if (westConnect + eastConnect - northConnect - southConnect > 0)
		{
			return BlockFacing.EAST;
		}
		if (northConnect + southConnect - westConnect - eastConnect > 0)
		{
			return BlockFacing.NORTH;
		}
		BlockPos down = pos.DownCopy();
		if (!CanSupportThis(world, down, null))
		{
			int westSolid = (CanSupportThis(world, down.WestCopy(), BlockFacing.EAST) ? 1 : 0);
			int eastSolid = (CanSupportThis(world, down.EastCopy(), BlockFacing.WEST) ? 1 : 0);
			int northSolid = (CanSupportThis(world, down.NorthCopy(), BlockFacing.SOUTH) ? 1 : 0);
			int southSolid = (CanSupportThis(world, down.SouthCopy(), BlockFacing.NORTH) ? 1 : 0);
			if (westSolid + eastSolid == 2 && northSolid + southSolid < 2)
			{
				return BlockFacing.EAST;
			}
			if (westSolid + eastSolid < 2 && northSolid + southSolid == 2)
			{
				return BlockFacing.NORTH;
			}
		}
		return suggested;
	}

	public override bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection bs, ref string failureCode)
	{
		BlockPos pos = bs.Position;
		BlockPos down = pos.DownCopy();
		IBlockAccessor blockAccess = world.BlockAccessor;
		if (!CanSupportThis(blockAccess, down, null))
		{
			bool oneSolid = blockAccess.GetBlock(pos.WestCopy()) is BlockSticksLayer;
			if (!oneSolid)
			{
				oneSolid = blockAccess.GetBlock(pos.EastCopy()) is BlockSticksLayer;
			}
			if (!oneSolid)
			{
				oneSolid = blockAccess.GetBlock(pos.NorthCopy()) is BlockSticksLayer;
			}
			if (!oneSolid)
			{
				oneSolid = blockAccess.GetBlock(pos.SouthCopy()) is BlockSticksLayer;
			}
			if (!oneSolid)
			{
				oneSolid = CanSupportThis(blockAccess, down.WestCopy(), BlockFacing.EAST);
			}
			if (!oneSolid)
			{
				oneSolid = CanSupportThis(blockAccess, down.EastCopy(), BlockFacing.WEST);
			}
			if (!oneSolid)
			{
				oneSolid = CanSupportThis(blockAccess, down.NorthCopy(), BlockFacing.SOUTH);
			}
			if (!oneSolid)
			{
				oneSolid = CanSupportThis(blockAccess, down.SouthCopy(), BlockFacing.NORTH);
			}
			if (!oneSolid)
			{
				failureCode = "requiresolidground";
				return false;
			}
		}
		return base.CanPlaceBlock(world, byPlayer, bs, ref failureCode);
	}

	private bool CanSupportThis(IBlockAccessor blockAccess, BlockPos pos, BlockFacing sideToTest)
	{
		Block block = blockAccess.GetBlock(pos);
		if (block.SideSolid[BlockFacing.UP.Index])
		{
			return true;
		}
		if (sideToTest == null && block.FirstCodePart() == "roughhewnfence")
		{
			return true;
		}
		Cuboidf[] boxes = block.CollisionBoxes;
		if (boxes != null)
		{
			for (int i = 0; i < boxes.Length; i++)
			{
				if (boxes[i].Y2 == 1f)
				{
					if (sideToTest == null)
					{
						return true;
					}
					if ((sideToTest != BlockFacing.WEST || boxes[i].X1 == 0f) && (sideToTest != BlockFacing.EAST || boxes[i].X2 == 1f) && (sideToTest != BlockFacing.NORTH || boxes[i].Z1 == 0f) && (sideToTest != BlockFacing.SOUTH || boxes[i].Z2 == 1f))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(OrientedAsset("ew")));
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		return OrientedAsset((Orientation == BlockFacing.NORTH) ? "ew" : "ns");
	}
}
