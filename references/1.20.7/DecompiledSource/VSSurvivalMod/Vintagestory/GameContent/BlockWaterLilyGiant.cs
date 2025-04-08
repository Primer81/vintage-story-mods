using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockWaterLilyGiant : BlockWaterLily
{
	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (blockAccessor.GetBlockBelow(pos, 4, 2).Id != 0)
		{
			return false;
		}
		bool canPlace = true;
		BlockPos tmpPos = pos.Copy();
		for (int x = -2; x < 3; x++)
		{
			for (int z = -2; z < 3; z++)
			{
				tmpPos.Set(pos.X + x, pos.Y, pos.Z + z);
				Block block4 = blockAccessor.GetBlock(tmpPos, 1);
				Block block2 = blockAccessor.GetBlock(tmpPos.Down(), 1);
				if (block4 == null || block4.Id != 0 || block2 == null || block2.Id != 0)
				{
					canPlace = false;
				}
			}
		}
		if (!canPlace)
		{
			return false;
		}
		if (!CanPlantStay(blockAccessor, pos))
		{
			return false;
		}
		Block block3 = blockAccessor.GetBlock(pos);
		if (block3.IsReplacableBy(this))
		{
			if (block3.EntityClass != null)
			{
				blockAccessor.RemoveBlockEntity(pos);
			}
			blockAccessor.SetBlock(BlockId, pos);
			if (EntityClass != null)
			{
				blockAccessor.SpawnBlockEntity(EntityClass, pos);
			}
			return true;
		}
		return false;
	}
}
