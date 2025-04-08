using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Datastructures;

namespace Vintagestory.GameContent;

public class BlockCoral : BlockWaterPlant
{
	private Block saltwater;

	public override bool skipPlantCheck { get; set; } = true;


	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		saltwater = api.World.BlockAccessor.GetBlock(new AssetLocation("saltwater-still-7"));
	}

	public override bool TryPlaceBlockForWorldGenUnderwater(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, int minWaterDepth, int maxWaterDepth, BlockPatchAttributes attributes = null)
	{
		if (attributes == null)
		{
			return false;
		}
		int minStepsGrow = attributes.CoralMinSize.GetValueOrDefault(800);
		int randomStepsGrow = attributes.CoralRandomSize.GetValueOrDefault(400);
		float seaWeedChance = attributes.CoralPlantsChance.GetValueOrDefault(0.03f);
		float replaceOtherPatches = attributes.CoralReplaceOtherPatches.GetValueOrDefault(0.03f);
		NaturalShape naturalShape = new NaturalShape(worldGenRand);
		int grow = ((randomStepsGrow != 0) ? worldGenRand.NextInt(randomStepsGrow) : 0);
		naturalShape.Grow(minStepsGrow + grow);
		foreach (BlockPos tmpPos in naturalShape.GetPositions(pos))
		{
			for (int depth = 1; depth < maxWaterDepth; depth++)
			{
				tmpPos.Down();
				Block block = blockAccessor.GetBlock(tmpPos);
				if (block is BlockCoral)
				{
					break;
				}
				if (block is BlockWaterPlant)
				{
					if (!(worldGenRand.NextFloat() < replaceOtherPatches))
					{
						break;
					}
					do
					{
						blockAccessor.SetBlock(saltwater.BlockId, tmpPos);
						tmpPos.Down();
						block = blockAccessor.GetBlock(tmpPos);
					}
					while (block is BlockWaterPlant);
				}
				if (block.IsLiquid())
				{
					continue;
				}
				if (depth < minWaterDepth)
				{
					break;
				}
				if (attributes != null && attributes.CoralPlants?.Count > 0 && worldGenRand.NextFloat() <= seaWeedChance)
				{
					Block coralBase = GetRandomBlock(worldGenRand, attributes.CoralBaseBlock);
					blockAccessor.SetBlock(coralBase.BlockId, tmpPos);
					if (depth + 1 >= minWaterDepth)
					{
						SpawnSeaPlantWeighted(blockAccessor, worldGenRand, attributes, tmpPos, depth);
					}
				}
				else
				{
					PlaceCoral(blockAccessor, tmpPos, worldGenRand, depth, minWaterDepth, attributes);
				}
				break;
			}
		}
		return true;
	}

	private static void SpawnSeaPlantWeighted(IBlockAccessor blockAccessor, IRandom worldGenRand, BlockPatchAttributes attributes, BlockPos tmpPos, int depth)
	{
		float totalWeight = attributes.CoralPlants.Sum<KeyValuePair<string, CoralPlantConfig>>((KeyValuePair<string, CoralPlantConfig> c) => c.Value.Chance);
		float chancePlant = worldGenRand.NextFloat() * totalWeight;
		float chanceSum = 0f;
		foreach (CoralPlantConfig conf in attributes.CoralPlants.Values)
		{
			chanceSum += conf.Chance;
			if (chancePlant < chanceSum)
			{
				int nextInt = worldGenRand.NextInt(conf.Block.Length);
				if (conf.Block[nextInt] is BlockSeaweed swb)
				{
					swb.PlaceSeaweed(blockAccessor, tmpPos, depth - 1, worldGenRand, conf.Height);
				}
				break;
			}
		}
	}

	public void PlaceCoral(IBlockAccessor blockAccessor, BlockPos pos, IRandom worldGenRand, int depth, int minDepth, BlockPatchAttributes attributes)
	{
		float verticalGrowChance = (attributes?.CoralVerticalGrowChance).GetValueOrDefault(0.6f);
		float shelveChance = (attributes?.CoralShelveChance).GetValueOrDefault(0.3f);
		float structureChance = (attributes?.CoralStructureChance).GetValueOrDefault(0.5f);
		int coralBaseHeight = attributes?.CoralBaseHeight ?? 2;
		pos.Add(0, -(coralBaseHeight - 1), 0);
		for (int j = 0; j < coralBaseHeight; j++)
		{
			Block coralBase = GetRandomBlock(worldGenRand, attributes.CoralBaseBlock);
			blockAccessor.SetBlock(coralBase.BlockId, pos);
			pos.Up();
		}
		depth--;
		if (depth <= 0)
		{
			return;
		}
		float num = worldGenRand.NextFloat();
		bool canSpawnOntop = true;
		if (num < shelveChance)
		{
			List<int> sides2 = GetSolidSides(blockAccessor, pos);
			if (sides2.Count > 0)
			{
				int nextInt2 = worldGenRand.NextInt(sides2.Count);
				Block[] shelfBlocks = GetRandomShelve(worldGenRand, attributes.CoralShelveBlock);
				GetRandomShelve(worldGenRand, attributes.CoralShelveBlock);
				blockAccessor.SetBlock(shelfBlocks[sides2[nextInt2]].BlockId, pos);
				canSpawnOntop = false;
			}
		}
		if (canSpawnOntop)
		{
			if (worldGenRand.NextFloat() < structureChance)
			{
				Block coralstructure = GetRandomBlock(worldGenRand, attributes.CoralStructureBlock);
				blockAccessor.SetBlock(coralstructure.BlockId, pos);
				pos.Up();
				depth--;
			}
			if (depth > 0)
			{
				Block coral = GetRandomBlock(worldGenRand, attributes.CoralBlock);
				blockAccessor.SetBlock(coral.BlockId, pos);
			}
		}
		if (depth - minDepth == 0)
		{
			return;
		}
		pos.Up();
		depth--;
		for (int i = 0; i < depth - minDepth; i++)
		{
			if (worldGenRand.NextFloat() > verticalGrowChance)
			{
				pos.Up();
				depth--;
				continue;
			}
			List<int> sides = GetSolidSides(blockAccessor, pos);
			if (sides.Count != 0)
			{
				int nextInt = worldGenRand.NextInt(sides.Count);
				Block[] shelfBlocksId = GetRandomShelve(worldGenRand, attributes.CoralShelveBlock);
				blockAccessor.SetBlock(shelfBlocksId[sides[nextInt]].BlockId, pos);
				pos.Up();
				depth--;
			}
		}
	}

	private static Block[] GetRandomShelve(IRandom worldGenRand, Block[][] blocks)
	{
		return blocks[worldGenRand.NextInt(blocks.Length)];
	}

	private static Block GetRandomBlock(IRandom worldGenRand, Block[] blocks)
	{
		return blocks[worldGenRand.NextInt(blocks.Length)];
	}

	private static List<int> GetSolidSides(IBlockAccessor blockAccessor, BlockPos pos)
	{
		List<int> sides = new List<int>();
		BlockPos tmpPos = pos.NorthCopy();
		if (blockAccessor.GetBlock(tmpPos).SideSolid[BlockFacing.SOUTH.Index])
		{
			sides.Add(BlockFacing.NORTH.Index);
		}
		tmpPos.Z += 2;
		if (blockAccessor.GetBlock(tmpPos).SideSolid[BlockFacing.NORTH.Index])
		{
			sides.Add(BlockFacing.SOUTH.Index);
		}
		tmpPos.Z--;
		tmpPos.X++;
		if (blockAccessor.GetBlock(tmpPos).SideSolid[BlockFacing.WEST.Index])
		{
			sides.Add(BlockFacing.EAST.Index);
		}
		tmpPos.X -= 2;
		if (blockAccessor.GetBlock(tmpPos).SideSolid[BlockFacing.EAST.Index])
		{
			sides.Add(BlockFacing.WEST.Index);
		}
		return sides;
	}
}
