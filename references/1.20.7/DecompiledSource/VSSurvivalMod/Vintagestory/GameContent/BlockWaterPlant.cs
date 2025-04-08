using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockWaterPlant : BlockPlant
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		Block blockToPlace = this;
		Block block = world.BlockAccessor.GetBlock(blockSel.Position, 2);
		if (block.IsLiquid() && block.LiquidLevel == 7 && block.LiquidCode.Contains("water"))
		{
			blockToPlace = world.GetBlock(CodeWithParts("water"));
			if (blockToPlace == null)
			{
				blockToPlace = this;
			}
		}
		else if (LastCodePart() != "free")
		{
			failureCode = "requirefullwater";
			return false;
		}
		if ((blockToPlace != null && skipPlantCheck) || CanPlantStay(world.BlockAccessor, blockSel.Position))
		{
			world.BlockAccessor.SetBlock(blockToPlace.BlockId, blockSel.Position);
			return true;
		}
		return false;
	}

	public override bool TryPlaceBlockForWorldGenUnderwater(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, int minWaterDepth, int maxWaterDepth, BlockPatchAttributes attributes = null)
	{
		BlockPos belowPos = pos.DownCopy();
		for (int depth = 1; depth < maxWaterDepth; depth++)
		{
			belowPos.Down();
			Block block = blockAccessor.GetBlock(belowPos);
			if (block is BlockWaterPlant)
			{
				return false;
			}
			if (block.Fertility > 0)
			{
				blockAccessor.SetBlock(BlockId, belowPos.Up());
				return true;
			}
			if (!block.IsLiquid())
			{
				return false;
			}
		}
		return false;
	}
}
