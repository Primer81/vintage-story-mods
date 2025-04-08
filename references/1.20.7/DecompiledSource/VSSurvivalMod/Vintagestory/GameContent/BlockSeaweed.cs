using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockSeaweed : BlockWaterPlant
{
	protected Block[] blocks;

	public override string RemapToLiquidsLayer => "water-still-7";

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		blocks = new Block[2]
		{
			api.World.BlockAccessor.GetBlock(CodeWithParts("section")),
			api.World.BlockAccessor.GetBlock(CodeWithParts("top"))
		};
	}

	public override bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos pos)
	{
		Block blockBelow = blockAccessor.GetBlockBelow(pos, 1, 1);
		if (blockBelow.Fertility <= 0)
		{
			if (blockBelow is BlockSeaweed)
			{
				return blockBelow.Variant["part"] == "section";
			}
			return false;
		}
		return true;
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		IBlockAccessor blockAccessor = api.World.BlockAccessor;
		int windData = ((blockAccessor.GetBlockBelow(pos, 1, 1) is BlockSeaweed) ? 1 : 0) + ((blockAccessor.GetBlockBelow(pos, 2, 1) is BlockSeaweed) ? 1 : 0) + ((blockAccessor.GetBlockBelow(pos, 3, 1) is BlockSeaweed) ? 1 : 0) + ((blockAccessor.GetBlockBelow(pos, 4, 1) is BlockSeaweed) ? 1 : 0);
		float[] sourceMeshXyz = sourceMesh.xyz;
		int[] sourceMeshFlags = sourceMesh.Flags;
		int sourceFlagsCount = sourceMesh.FlagsCount;
		for (int i = 0; i < sourceFlagsCount; i++)
		{
			float y = sourceMeshXyz[i * 3 + 1];
			VertexFlags.ReplaceWindData(ref sourceMeshFlags[i], windData + ((y > 0f) ? 1 : 0));
		}
	}

	public override bool TryPlaceBlockForWorldGenUnderwater(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, int minWaterDepth, int maxWaterDepth, BlockPatchAttributes attributes = null)
	{
		NatFloat height = attributes?.Height ?? NatFloat.createGauss(3f, 3f);
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
				PlaceSeaweed(blockAccessor, belowPos, depth, worldGenRand, height);
				return true;
			}
			if (!block.IsLiquid())
			{
				return false;
			}
		}
		return false;
	}

	internal void PlaceSeaweed(IBlockAccessor blockAccessor, BlockPos pos, int depth, IRandom random, NatFloat heightNatFloat)
	{
		int height = Math.Min(depth, (int)heightNatFloat.nextFloat(1f, random));
		while (height-- > 1)
		{
			pos.Up();
			blockAccessor.SetBlock(blocks[0].BlockId, pos);
		}
		pos.Up();
		if (blocks[1] == null)
		{
			blockAccessor.SetBlock(blocks[0].BlockId, pos);
		}
		else
		{
			blockAccessor.SetBlock(blocks[1].BlockId, pos);
		}
	}
}
