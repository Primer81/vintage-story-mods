using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockRails : Block
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		BlockFacing targetFacing = Block.SuggestedHVOrientation(byPlayer, blockSel)[0];
		Block blockToPlace = null;
		for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
		{
			BlockFacing facing = BlockFacing.HORIZONTALS[i];
			if (TryAttachPlaceToHoriontal(world, byPlayer, blockSel.Position, facing, targetFacing))
			{
				return true;
			}
		}
		if (blockToPlace == null)
		{
			blockToPlace = ((targetFacing.Axis != EnumAxis.Z) ? world.GetBlock(CodeWithParts("flat_we")) : world.GetBlock(CodeWithParts("flat_ns")));
		}
		blockToPlace.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
		return true;
	}

	private bool TryAttachPlaceToHoriontal(IWorldAccessor world, IPlayer byPlayer, BlockPos position, BlockFacing toFacing, BlockFacing targetFacing)
	{
		BlockPos neibPos = position.AddCopy(toFacing);
		Block neibBlock = world.BlockAccessor.GetBlock(neibPos);
		if (!(neibBlock is BlockRails))
		{
			return false;
		}
		BlockFacing fromFacing = toFacing.Opposite;
		BlockFacing[] neibDirFacings = getFacingsFromType(neibBlock.Variant["type"]);
		if (world.BlockAccessor.GetBlock(neibPos.AddCopy(neibDirFacings[0])) is BlockRails && world.BlockAccessor.GetBlock(neibPos.AddCopy(neibDirFacings[1])) is BlockRails)
		{
			return false;
		}
		BlockFacing neibFreeFace = getOpenedEndedFace(neibDirFacings, world, position.AddCopy(toFacing));
		if (neibFreeFace == null)
		{
			return false;
		}
		Block blockToPlace = getRailBlock(world, "curved_", toFacing, targetFacing);
		if (blockToPlace != null)
		{
			if (!placeIfSuitable(world, byPlayer, blockToPlace, position))
			{
				return false;
			}
			return true;
		}
		string dirs = neibBlock.Variant["type"].Split('_')[1];
		BlockFacing neibKeepFace = ((dirs[0] == neibFreeFace.Code[0]) ? BlockFacing.FromFirstLetter(dirs[1]) : BlockFacing.FromFirstLetter(dirs[0]));
		Block block = getRailBlock(world, "curved_", neibKeepFace, fromFacing);
		if (block == null)
		{
			return false;
		}
		block.DoPlaceBlock(world, byPlayer, new BlockSelection
		{
			Position = position.AddCopy(toFacing),
			Face = BlockFacing.UP
		}, null);
		return false;
	}

	private bool placeIfSuitable(IWorldAccessor world, IPlayer byPlayer, Block block, BlockPos pos)
	{
		string failureCode = "";
		BlockSelection blockSel = new BlockSelection
		{
			Position = pos,
			Face = BlockFacing.UP
		};
		if (block.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			block.DoPlaceBlock(world, byPlayer, blockSel, null);
			return true;
		}
		return false;
	}

	private Block getRailBlock(IWorldAccessor world, string prefix, BlockFacing dir0, BlockFacing dir1)
	{
		ReadOnlySpan<char> readOnlySpan = prefix;
		char reference = dir0.Code[0];
		ReadOnlySpan<char> readOnlySpan2 = new ReadOnlySpan<char>(in reference);
		char reference2 = dir1.Code[0];
		Block block = world.GetBlock(CodeWithParts(string.Concat(readOnlySpan, readOnlySpan2, new ReadOnlySpan<char>(in reference2))));
		if (block != null)
		{
			return block;
		}
		ReadOnlySpan<char> readOnlySpan3 = prefix;
		reference2 = dir1.Code[0];
		ReadOnlySpan<char> readOnlySpan4 = new ReadOnlySpan<char>(in reference2);
		reference = dir0.Code[0];
		return world.GetBlock(CodeWithParts(string.Concat(readOnlySpan3, readOnlySpan4, new ReadOnlySpan<char>(in reference))));
	}

	private BlockFacing getOpenedEndedFace(BlockFacing[] dirFacings, IWorldAccessor world, BlockPos blockPos)
	{
		if (!(world.BlockAccessor.GetBlock(blockPos.AddCopy(dirFacings[0])) is BlockRails))
		{
			return dirFacings[0];
		}
		if (!(world.BlockAccessor.GetBlock(blockPos.AddCopy(dirFacings[1])) is BlockRails))
		{
			return dirFacings[1];
		}
		return null;
	}

	private BlockFacing[] getFacingsFromType(string type)
	{
		string codes = type.Split('_')[1];
		return new BlockFacing[2]
		{
			BlockFacing.FromFirstLetter(codes[0]),
			BlockFacing.FromFirstLetter(codes[1])
		};
	}
}
