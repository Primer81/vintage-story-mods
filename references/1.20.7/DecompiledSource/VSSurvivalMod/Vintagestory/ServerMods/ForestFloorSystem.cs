using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class ForestFloorSystem
{
	public const int Range = 16;

	public const int GridRowSize = 33;

	private ICoreServerAPI sapi;

	private IServerWorldAccessor worldAccessor;

	private IBlockAccessor api;

	[ThreadStatic]
	private static short[] outlineThreadSafe;

	private int[] forestBlocks;

	private List<BlockPatch> underTreePatches;

	private List<BlockPatch> onTreePatches;

	private GenVegetationAndPatches genPatchesSystem;

	private BlockPos tmpPos = new BlockPos();

	public ForestFloorSystem(ICoreServerAPI api)
	{
		sapi = api;
		worldAccessor = sapi.World;
		genPatchesSystem = sapi.ModLoader.GetModSystem<GenVegetationAndPatches>();
	}

	internal short[] GetOutline()
	{
		return outlineThreadSafe ?? (outlineThreadSafe = new short[1089]);
	}

	public void SetBlockPatches(BlockPatchConfig bpc)
	{
		forestBlocks = BlockForestFloor.InitialiseForestBlocks(worldAccessor);
		underTreePatches = new List<BlockPatch>();
		onTreePatches = new List<BlockPatch>();
		for (int i = 0; i < bpc.Patches.Length; i++)
		{
			BlockPatch blockPatch = bpc.Patches[i];
			if (blockPatch.Placement == EnumBlockPatchPlacement.UnderTrees || blockPatch.Placement == EnumBlockPatchPlacement.OnSurfacePlusUnderTrees)
			{
				underTreePatches.Add(blockPatch);
			}
			if (blockPatch.Placement == EnumBlockPatchPlacement.OnTrees)
			{
				onTreePatches.Add(blockPatch);
			}
		}
	}

	internal void ClearOutline()
	{
		short[] outline = GetOutline();
		for (int i = 0; i < outline.Length; i++)
		{
			outline[i] = 0;
		}
	}

	internal void CreateForestFloor(IBlockAccessor blockAccessor, TreeGenConfig config, BlockPos pos, IRandom rnd, int treesInChunkGenerated)
	{
		int grassLevelOffset = 0;
		ClimateCondition climate = blockAccessor.GetClimateAt(pos, EnumGetClimateMode.WorldGenValues);
		if (climate.Temperature > 24f && climate.Rainfall > 160f)
		{
			grassLevelOffset = 2;
		}
		short[] outline = GetOutline();
		api = blockAccessor;
		float forestness = climate.ForestDensity * climate.ForestDensity * 4f * (climate.Fertility + 0.25f);
		if ((double)climate.Fertility <= 0.25 || (double)forestness <= 0.4)
		{
			return;
		}
		for (int i = 0; i < outline.Length; i++)
		{
			outline[i] = (short)((float)outline[i] * forestness + 0.3f);
		}
		for (int pass = 0; pass < 7; pass++)
		{
			bool noChange = true;
			for (int x = 0; x < 16; x++)
			{
				for (int z2 = 0; z2 < 16; z2++)
				{
					if (x == 0 && z2 == 0)
					{
						continue;
					}
					int zBase2 = (16 + z2) * 33;
					int o2 = Math.Min((int)outline[zBase2 + (16 + x)], 162);
					if (o2 != 0)
					{
						int n2 = zBase2 + 33 + (16 + x);
						int n4 = zBase2 + (17 + x);
						if (outline[n2] < o2 - 18)
						{
							outline[n2] = (short)(o2 - 18);
							noChange = false;
						}
						if (outline[n4] < o2 - 18)
						{
							outline[n4] = (short)(o2 - 18);
							noChange = false;
						}
						zBase2 = (16 - z2) * 33;
						o2 = Math.Min((int)outline[zBase2 + (16 + x)], 162);
						n2 = zBase2 - 33 + (16 + x);
						n4 = zBase2 + (17 + x);
						if (outline[n2] < o2 - 18)
						{
							outline[n2] = (short)(o2 - 18);
							noChange = false;
						}
						if (outline[n4] < o2 - 18)
						{
							outline[n4] = (short)(o2 - 18);
							noChange = false;
						}
					}
				}
				for (int z = 0; z < 16; z++)
				{
					if (x != 0 || z != 0)
					{
						int zBase = (16 + z) * 33;
						int o = Math.Min((int)outline[zBase + (16 - x)], 162);
						int n1 = zBase + 33 + (16 - x);
						int n3 = zBase + (15 - x);
						if (outline[n1] < o - 18)
						{
							outline[n1] = (short)(o - 18);
							noChange = false;
						}
						if (outline[n3] < o - 18)
						{
							outline[n3] = (short)(o - 18);
							noChange = false;
						}
						zBase = (16 - z) * 33;
						o = Math.Min((int)outline[zBase + (16 - x)], 162);
						n1 = zBase - 33 + (16 - x);
						n3 = zBase + (15 - x);
						if (outline[n1] < o - 18)
						{
							outline[n1] = (short)(o - 18);
							noChange = false;
						}
						if (outline[n3] < o - 18)
						{
							outline[n3] = (short)(o - 18);
							noChange = false;
						}
					}
				}
			}
			if (noChange)
			{
				break;
			}
		}
		BlockPos currentPos = new BlockPos();
		for (int canopyIndex = 0; canopyIndex < outline.Length; canopyIndex++)
		{
			int intensity = outline[canopyIndex];
			if (intensity != 0)
			{
				int dz = canopyIndex / 33 - 16;
				int dx = canopyIndex % 33 - 16;
				currentPos.Set(pos.X + dx, pos.Y, pos.Z + dz);
				currentPos.Y = blockAccessor.GetTerrainMapheightAt(currentPos);
				if (currentPos.Y - pos.Y < 4)
				{
					CheckAndReplaceForestFloor(currentPos, intensity, grassLevelOffset);
				}
			}
		}
		GenPatches(blockAccessor, pos, forestness, config.Treetype, rnd);
	}

	private void GenPatches(IBlockAccessor blockAccessor, BlockPos pos, float forestNess, EnumTreeType treetype, IRandom rnd)
	{
		BlockPatchConfig bpc = genPatchesSystem.bpc;
		int radius = 5;
		int worldheight = blockAccessor.MapSizeY;
		int cnt = underTreePatches?.Count ?? 0;
		for (int j = 0; j < cnt; j++)
		{
			BlockPatch bPatch = underTreePatches[j];
			if (bPatch.TreeType != 0 && bPatch.TreeType != treetype)
			{
				continue;
			}
			float chance2 = 0.003f * forestNess * bPatch.Chance * bpc.ChanceMultiplier.nextFloat(1f, rnd);
			while (chance2-- > rnd.NextFloat())
			{
				int dx2 = rnd.NextInt(2 * radius) - radius;
				int dz2 = rnd.NextInt(2 * radius) - radius;
				tmpPos.Set(pos.X + dx2, 0, pos.Z + dz2);
				int y = blockAccessor.GetTerrainMapheightAt(tmpPos);
				if (y <= 0 || y >= worldheight - 8)
				{
					continue;
				}
				tmpPos.Y = y;
				ClimateCondition climate2 = blockAccessor.GetClimateAt(tmpPos, EnumGetClimateMode.WorldGenValues);
				if (climate2 == null || !bpc.IsPatchSuitableUnderTree(bPatch, worldheight, climate2, y))
				{
					continue;
				}
				int regionX2 = pos.X / blockAccessor.RegionSize;
				int regionZ2 = pos.Z / blockAccessor.RegionSize;
				if (bPatch.MapCode != null && rnd.NextInt(255) > genPatchesSystem.GetPatchDensity(bPatch.MapCode, tmpPos.X, tmpPos.Z, blockAccessor.GetMapRegion(regionX2, regionZ2)))
				{
					continue;
				}
				int firstBlockId = 0;
				bool found = true;
				if (bPatch.BlocksByRockType != null)
				{
					found = false;
					for (int dy2 = 1; dy2 < 5 && y - dy2 > 0; dy2++)
					{
						string lastCodePart = blockAccessor.GetBlock(tmpPos.X, y - dy2, tmpPos.Z).LastCodePart();
						if (genPatchesSystem.RockBlockIdsByType.TryGetValue(lastCodePart, out firstBlockId))
						{
							found = true;
							break;
						}
					}
				}
				if (found)
				{
					new LCGRandom(sapi.WorldManager.Seed + j).InitPositionSeed(tmpPos.X, tmpPos.Z);
					bPatch.Generate(blockAccessor, rnd, tmpPos.X, tmpPos.Y, tmpPos.Z, firstBlockId, isStoryPatch: false);
				}
			}
		}
		cnt = onTreePatches?.Count ?? 0;
		for (int i = 0; i < cnt; i++)
		{
			BlockPatch blockPatch = onTreePatches[i];
			float chance = 3f * forestNess * blockPatch.Chance * bpc.ChanceMultiplier.nextFloat(1f, rnd);
			while (chance-- > rnd.NextFloat())
			{
				int dx = 1 - rnd.NextInt(2) * 2;
				int dy = rnd.NextInt(5);
				int dz = 1 - rnd.NextInt(2) * 2;
				tmpPos.Set(pos.X + dx, pos.Y + dy, pos.Z + dz);
				if (api.GetBlock(tmpPos).Id != 0)
				{
					continue;
				}
				BlockFacing facing = null;
				for (int k = 0; k < 4; k++)
				{
					BlockFacing f = BlockFacing.HORIZONTALS[k];
					Block nblock = api.GetBlockOnSide(tmpPos, f);
					if (nblock is BlockLog && nblock.Variant["type"] != "resin")
					{
						facing = f;
						break;
					}
				}
				if (facing == null)
				{
					break;
				}
				ClimateCondition climate = blockAccessor.GetClimateAt(tmpPos, EnumGetClimateMode.WorldGenValues);
				if (climate != null && bpc.IsPatchSuitableUnderTree(blockPatch, worldheight, climate, tmpPos.Y))
				{
					int regionX = pos.X / blockAccessor.RegionSize;
					int regionZ = pos.Z / blockAccessor.RegionSize;
					if (blockPatch.MapCode == null || rnd.NextInt(255) <= genPatchesSystem.GetPatchDensity(blockPatch.MapCode, tmpPos.X, tmpPos.Z, blockAccessor.GetMapRegion(regionX, regionZ)))
					{
						int index = rnd.NextInt(blockPatch.Blocks.Length);
						blockPatch.Blocks[index].TryPlaceBlockForWorldGen(blockAccessor, tmpPos, facing, rnd);
					}
				}
			}
		}
	}

	private void CheckAndReplaceForestFloor(BlockPos pos, int intensity, int grassLevelOffset)
	{
		if (forestBlocks == null)
		{
			return;
		}
		Block soilBlock = api.GetBlock(pos);
		if (!(soilBlock is BlockForestFloor) && !(soilBlock is BlockSoil))
		{
			return;
		}
		if (soilBlock is BlockForestFloor bff)
		{
			int existingLevel = bff.CurrentLevel();
			intensity += existingLevel * 18 - 9;
			intensity = Math.Min(intensity, Math.Max(existingLevel * 18, (BlockForestFloor.MaxStage - 1) * 18));
		}
		int level = grassLevelOffset + intensity / 18;
		int forestFloorBlockId;
		if (level >= forestBlocks.Length - 1)
		{
			forestFloorBlockId = forestBlocks[(level <= forestBlocks.Length) ? 1u : 0u];
		}
		else
		{
			if (level == 0)
			{
				level = 1;
			}
			forestFloorBlockId = forestBlocks[forestBlocks.Length - level];
		}
		api.SetBlock(forestFloorBlockId, pos);
	}

	private int GetRandomBlock(BlockPatch blockPatch)
	{
		return blockPatch.Blocks[0].Id;
	}

	private float GetDistance(ClimateCondition climate, BlockPatch variant)
	{
		float tempDist = Math.Abs(climate.Temperature * 2f - (float)variant.MaxTemp - (float)variant.MinTemp) / (float)(variant.MaxTemp - variant.MinTemp);
		if (tempDist > 1f)
		{
			return 5f;
		}
		float fertDist = Math.Abs(climate.Fertility * 2f - variant.MaxFertility - variant.MinFertility) / (variant.MaxFertility - variant.MinFertility);
		if (fertDist > 1f)
		{
			return 5f;
		}
		float rainDist = Math.Abs(climate.Rainfall * 2f - variant.MaxRain - variant.MinRain) / (variant.MaxRain - variant.MinRain);
		if (rainDist > 1.3f)
		{
			return 5f;
		}
		float forestDist = Math.Abs((climate.ForestDensity + 0.2f) * 2f - variant.MaxForest - variant.MinForest) / (variant.MaxForest - variant.MinForest);
		return tempDist * tempDist + fertDist * fertDist + rainDist * rainDist + forestDist * forestDist;
	}
}
