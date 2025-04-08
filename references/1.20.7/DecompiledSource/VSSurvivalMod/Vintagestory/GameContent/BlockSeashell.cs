using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockSeashell : Block
{
	private static string[] colors = new string[4] { "latte", "plain", "darkpurple", "cinnamon" };

	private static string[] rarecolors = new string[2] { "seafoam", "turquoise" };

	private static string[] types = new string[7] { "scallop", "sundial", "turritella", "clam", "conch", "seastar", "volute" };

	private static Dictionary<string, string> tmpDict = new Dictionary<string, string>();

	public override bool TryPlaceBlockForWorldGenUnderwater(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldgenRandom, int minWaterDepth, int maxWaterDepth, BlockPatchAttributes attributes = null)
	{
		int depth;
		for (depth = 1; depth < maxWaterDepth; depth++)
		{
			pos.Down();
			Block block = blockAccessor.GetBlock(pos);
			if (block is BlockWaterPlant)
			{
				return false;
			}
			if (block is BlockSeashell)
			{
				return false;
			}
			if (!block.IsLiquid())
			{
				break;
			}
		}
		if (depth >= maxWaterDepth)
		{
			return false;
		}
		pos.Up();
		if (blockAccessor.GetBlock(pos).IsReplacableBy(this))
		{
			blockAccessor.SetBlock(BlockId, pos);
			return true;
		}
		return false;
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (!HasBeachyGround(blockAccessor, pos))
		{
			return false;
		}
		tmpDict["type"] = types[worldGenRand.NextInt(types.Length)];
		if (worldGenRand.NextInt(100) < 8)
		{
			tmpDict["color"] = rarecolors[worldGenRand.NextInt(rarecolors.Length)];
		}
		else
		{
			tmpDict["color"] = colors[worldGenRand.NextInt(colors.Length)];
		}
		Block block = blockAccessor.GetBlock(CodeWithVariants(tmpDict));
		if (block == null)
		{
			return false;
		}
		if (blockAccessor.GetBlock(pos).IsReplacableBy(this))
		{
			blockAccessor.SetBlock(block.BlockId, pos);
			return true;
		}
		return false;
	}

	internal virtual bool HasBeachyGround(IBlockAccessor blockAccessor, BlockPos pos)
	{
		Block blockBelow = blockAccessor.GetBlockBelow(pos, 1, 1);
		if (blockBelow.SideSolid[BlockFacing.UP.Index])
		{
			if (blockBelow.BlockMaterial != EnumBlockMaterial.Sand)
			{
				return blockBelow.BlockMaterial == EnumBlockMaterial.Gravel;
			}
			return true;
		}
		return false;
	}
}
