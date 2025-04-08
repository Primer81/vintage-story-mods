using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockCactus : BlockPlant
{
	private Random rand = new Random();

	private Block[][] blocksByHeight;

	private Block topFlowering;

	private Block topRipe;

	public override bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos pos)
	{
		Block block = blockAccessor.GetBlock(pos.DownCopy());
		if (block.Fertility <= 0)
		{
			return block is BlockCactus;
		}
		return true;
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (!CanPlantStay(blockAccessor, pos))
		{
			return false;
		}
		if (blocksByHeight == null)
		{
			topFlowering = blockAccessor.GetBlock(CodeWithParts("topflowering"));
			topRipe = blockAccessor.GetBlock(CodeWithParts("topripe"));
			blocksByHeight = new Block[3][]
			{
				new Block[1] { blockAccessor.GetBlock(CodeWithParts("topempty")) },
				new Block[2]
				{
					blockAccessor.GetBlock(CodeWithParts("segment")),
					blockAccessor.GetBlock(CodeWithParts("topempty"))
				},
				new Block[3]
				{
					blockAccessor.GetBlock(CodeWithParts("segment")),
					blockAccessor.GetBlock(CodeWithParts("branchysegment")),
					blockAccessor.GetBlock(CodeWithParts("topempty"))
				}
			};
		}
		int height = rand.Next(3);
		Block[] blocks = blocksByHeight[height];
		if (blocks.Length == 3)
		{
			if (rand.Next(6) == 0)
			{
				blocks[2] = topRipe;
			}
			else if (rand.Next(10) == 0)
			{
				blocks[2] = topFlowering;
			}
			else
			{
				blocks[2] = blocksByHeight[0][0];
			}
		}
		for (int i = 0; i < blocks.Length && blockAccessor.GetBlock(pos).IsReplacableBy(blocks[i]); i++)
		{
			blockAccessor.SetBlock(blocks[i].BlockId, pos);
			pos = pos.Up();
		}
		return true;
	}
}
