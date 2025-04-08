using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCrowfoot : BlockSeaweed
{
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		blocks = new Block[3]
		{
			api.World.BlockAccessor.GetBlock(CodeWithParts("section")),
			api.World.BlockAccessor.GetBlock(CodeWithParts("tip")),
			api.World.BlockAccessor.GetBlock(CodeWithParts("top"))
		};
	}

	public override bool TryPlaceBlockForWorldGenUnderwater(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, int minWaterDepth, int maxWaterDepth, BlockPatchAttributes attributes = null)
	{
		BlockPos belowPos = pos.DownCopy();
		NatFloat height = attributes?.Height ?? NatFloat.createGauss(2f, 2f);
		float flowChance = (attributes?.FlowerChance).GetValueOrDefault(0.7f);
		for (int depth = 1; depth < maxWaterDepth; depth++)
		{
			belowPos.Down();
			Block block = blockAccessor.GetBlock(belowPos);
			if (block.Fertility > 0)
			{
				PlaceCrowfoot(blockAccessor, belowPos, depth, worldGenRand, height, flowChance);
				return true;
			}
			if (block is BlockWaterPlant || !block.IsLiquid())
			{
				return false;
			}
		}
		return false;
	}

	internal void PlaceCrowfoot(IBlockAccessor blockAccessor, BlockPos pos, int depth, IRandom random, NatFloat heightNatFloat, float flowChance)
	{
		int height = Math.Min(depth, (int)heightNatFloat.nextFloat(1f, random));
		bool spawnFlower = random.NextFloat() < flowChance && height == depth;
		while (height-- > 1)
		{
			pos.Up();
			blockAccessor.SetBlock(blocks[0].BlockId, pos);
		}
		pos.Up();
		if (spawnFlower)
		{
			blockAccessor.SetBlock(blocks[2].BlockId, pos);
		}
		else
		{
			blockAccessor.SetBlock(blocks[1].BlockId, pos);
		}
	}
}
