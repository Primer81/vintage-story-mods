using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockDeadCrop : Block, IDrawYAdjustable
{
	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityDeadCrop be)
		{
			return be.GetDrops(byPlayer, dropQuantityMultiplier);
		}
		return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityDeadCrop be)
		{
			return be.GetPlacedBlockName();
		}
		return base.GetPlacedBlockName(world, pos);
	}

	public float AdjustYPosition(BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		if (!(chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[5]] is BlockFarmland))
		{
			return 0f;
		}
		return -0.0625f;
	}

	public override int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
	{
		return ColorUtil.ColorFromRgba(90, 84, 67, 255);
	}
}
