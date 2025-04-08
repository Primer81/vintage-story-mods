using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class BlockSchematicStructure : BlockSchematic
{
	public Dictionary<int, AssetLocation> BlockCodesTmpForRemap = new Dictionary<int, AssetLocation>();

	public string FromFileName;

	public Block[,,] blocksByPos;

	public Dictionary<BlockPos, Block> FluidBlocksByPos;

	public BlockLayerConfig blockLayerConfig;

	private int mapheight;

	private PlaceBlockDelegate handler;

	internal GenBlockLayers genBlockLayers;

	public int MaxYDiff = 3;

	public int MaxBelowSealevel = 20;

	public int? StoryLocationMaxAmount;

	public int OffsetY { get; set; } = -1;


	public static bool SatisfiesMinSpawnDistance(int minSpawnDistance, BlockPos pos, BlockPos spawnPos)
	{
		if (minSpawnDistance <= 0)
		{
			return true;
		}
		return spawnPos.HorDistanceSqTo(pos.X, pos.Z) > (float)(minSpawnDistance * minSpawnDistance);
	}

	public override void Init(IBlockAccessor blockAccessor)
	{
		base.Init(blockAccessor);
		mapheight = blockAccessor.MapSizeY;
		blocksByPos = new Block[SizeX + 1, SizeY + 1, SizeZ + 1];
		FluidBlocksByPos = new Dictionary<BlockPos, Block>();
		for (int i = 0; i < Indices.Count; i++)
		{
			uint num = Indices[i];
			int storedBlockid = BlockIds[i];
			int dx = (int)(num & 0x3FF);
			int dy = (int)((num >> 20) & 0x3FF);
			int dz = (int)((num >> 10) & 0x3FF);
			Block block = blockAccessor.GetBlock(BlockCodes[storedBlockid]);
			if (block != null)
			{
				if (block.ForFluidsLayer)
				{
					FluidBlocksByPos.Add(new BlockPos(dx, dy, dz), block);
				}
				else
				{
					blocksByPos[dx, dy, dz] = block;
				}
			}
		}
		handler = null;
		switch (ReplaceMode)
		{
		case EnumReplaceMode.ReplaceAll:
			handler = PlaceReplaceAll;
			break;
		case EnumReplaceMode.Replaceable:
			handler = PlaceReplaceable;
			break;
		case EnumReplaceMode.ReplaceAllNoAir:
			handler = PlaceReplaceAllNoAir;
			break;
		case EnumReplaceMode.ReplaceOnlyAir:
			handler = PlaceReplaceOnlyAir;
			break;
		}
	}

	public int PlaceRespectingBlockLayers(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos startPos, int climateUpLeft, int climateUpRight, int climateBotLeft, int climateBotRight, Dictionary<int, Dictionary<int, int>> replaceBlocks, int[] replaceWithBlockLayersBlockids, bool replaceMetaBlocks = true, bool replaceBlockEntities = false, bool suppressSoilIfAirBelow = false, bool displaceWater = false)
	{
		Unpack(worldForCollectibleResolve.Api);
		if (genBlockLayers == null)
		{
			genBlockLayers = worldForCollectibleResolve.Api.ModLoader.GetModSystem<GenBlockLayers>();
		}
		BlockPos curPos = new BlockPos();
		BlockPos localCurrentPos = new BlockPos();
		int placed = 0;
		int chunkBaseX = startPos.X / 32 * 32;
		int chunkBaseZ = startPos.Z / 32 * 32;
		curPos.Set(SizeX / 2 + startPos.X, startPos.Y, SizeZ / 2 + startPos.Z);
		IMapChunk mapchunk = blockAccessor.GetMapChunkAtBlockPos(curPos);
		int centerrockblockid = mapchunk.TopRockIdMap[curPos.Z % 32 * 32 + curPos.X % 32];
		IWorldAccessor worldAccessor;
		if (!(blockAccessor is IWorldGenBlockAccessor wgba))
		{
			worldAccessor = worldForCollectibleResolve;
		}
		else
		{
			IWorldAccessor worldgenWorldAccessor2 = wgba.WorldgenWorldAccessor;
			worldAccessor = worldgenWorldAccessor2;
		}
		IWorldAccessor worldgenWorldAccessor = worldAccessor;
		resolveReplaceRemapsForBlockEntities(blockAccessor, worldForCollectibleResolve, replaceBlocks, centerrockblockid);
		Dictionary<BlockPos, Block> layerBlockForBlockEntities = new Dictionary<BlockPos, Block>();
		for (int x = 0; x < SizeX; x++)
		{
			for (int z = 0; z < SizeZ; z++)
			{
				curPos.Set(x + startPos.X, startPos.Y, z + startPos.Z);
				if (!blockAccessor.IsValidPos(curPos))
				{
					continue;
				}
				mapchunk = blockAccessor.GetMapChunkAtBlockPos(curPos);
				int rockblockid = mapchunk.TopRockIdMap[curPos.Z % 32 * 32 + curPos.X % 32];
				int depth = mapchunk.WorldGenTerrainHeightMap[curPos.Z % 32 * 32 + curPos.X % 32] - (SizeY + startPos.Y);
				int maxY = -1;
				int underWaterDepth = -1;
				Block aboveLiqBlock = blockAccessor.GetBlock(curPos.X, curPos.Y + SizeY, curPos.Z, 2);
				if (aboveLiqBlock != null && aboveLiqBlock.IsLiquid())
				{
					underWaterDepth++;
				}
				bool highestBlockinCol = true;
				for (int y2 = SizeY - 1; y2 >= 0; y2--)
				{
					depth++;
					curPos.Set(x + startPos.X, y2 + startPos.Y, z + startPos.Z);
					if (!blockAccessor.IsValidPos(curPos))
					{
						continue;
					}
					localCurrentPos.Set(x, y2, z);
					Block block = blocksByPos[x, y2, z];
					FluidBlocksByPos.TryGetValue(localCurrentPos, out var fluidBlock);
					if (block == null)
					{
						block = fluidBlock;
					}
					aboveLiqBlock = blockAccessor.GetBlock(curPos.X, curPos.Y, curPos.Z, 2);
					if (aboveLiqBlock != null && aboveLiqBlock.IsLiquid())
					{
						underWaterDepth++;
					}
					if (block == null || (replaceMetaBlocks && (block.Id == BlockSchematic.UndergroundBlockId || block.Id == BlockSchematic.AbovegroundBlockId)))
					{
						continue;
					}
					if (block.Replaceable < 1000 && depth >= 0 && (replaceWithBlockLayersBlockids.Contains(block.BlockId) || block.CustomBlockLayerHandler))
					{
						if (suppressSoilIfAirBelow && (y2 == 0 || blocksByPos[x, y2 - 1, z] == null) && blockAccessor.GetBlock(curPos.X, curPos.Y - 1, curPos.Z, 1).Replaceable > 3000)
						{
							for (int yy = y2 + 1; yy < SizeY; yy++)
							{
								Block placedBlock = blocksByPos[x, yy, z];
								if (placedBlock == null || !replaceWithBlockLayersBlockids.Contains(placedBlock.BlockId))
								{
									break;
								}
								blockAccessor.SetBlock(0, new BlockPos(curPos.X, startPos.Y + yy, curPos.Z), 1);
							}
							continue;
						}
						if (depth == 0 && replaceWithBlockLayersBlockids.Length > 1)
						{
							Block aboveBlock2 = blockAccessor.GetBlock(curPos.X, curPos.Y + 1, curPos.Z, 1);
							if (aboveBlock2.SideSolid[BlockFacing.DOWN.Index] && aboveBlock2.BlockMaterial != EnumBlockMaterial.Wood && aboveBlock2.BlockMaterial != EnumBlockMaterial.Snow && aboveBlock2.BlockMaterial != EnumBlockMaterial.Ice)
							{
								depth++;
							}
						}
						int climate = GameMath.BiLerpRgbColor(GameMath.Clamp((float)(curPos.X - chunkBaseX) / 32f, 0f, 1f), GameMath.Clamp((float)(curPos.Z - chunkBaseZ) / 32f, 0f, 1f), climateUpLeft, climateUpRight, climateBotLeft, climateBotRight);
						Block layerBlock = GetBlockLayerBlock((climate >> 8) & 0xFF, (climate >> 16) & 0xFF, curPos.Y - 1, rockblockid, depth, block, worldForCollectibleResolve.Blocks, curPos, underWaterDepth);
						if (block.CustomBlockLayerHandler && layerBlock != block)
						{
							layerBlockForBlockEntities[curPos.Copy()] = layerBlock;
						}
						else
						{
							block = layerBlock;
						}
					}
					if (replaceBlocks != null && replaceBlocks.TryGetValue(block.Id, out var replaceByBlock) && replaceByBlock.TryGetValue(centerrockblockid, out var newBlockId))
					{
						block = blockAccessor.GetBlock(newBlockId);
					}
					if (block.ForFluidsLayer)
					{
						blockAccessor.SetBlock(0, curPos, 1);
					}
					int p = handler(blockAccessor, curPos, block, replaceMeta: true);
					if (fluidBlock != null && !block.Equals(fluidBlock))
					{
						handler(blockAccessor, curPos, fluidBlock, replaceMeta: true);
					}
					if (p > 0)
					{
						if (displaceWater)
						{
							blockAccessor.SetBlock(0, curPos, 2);
						}
						else if (block.Id != 0 && !block.SideSolid.All)
						{
							aboveLiqBlock = blockAccessor.GetBlock(curPos.X, curPos.Y + 1, curPos.Z, 2);
							if (aboveLiqBlock.Id != 0)
							{
								blockAccessor.SetBlock(aboveLiqBlock.BlockId, curPos, 2);
							}
						}
						if (highestBlockinCol)
						{
							Block aboveBlock = blockAccessor.GetBlock(curPos.X, curPos.Y + 1, curPos.Z, 1);
							if (aboveBlock.Id > 0)
							{
								aboveBlock.OnNeighbourBlockChange(worldgenWorldAccessor, curPos.UpCopy(), curPos);
							}
							highestBlockinCol = false;
						}
						placed += p;
						if (!block.RainPermeable)
						{
							if (IsFillerOrPath(block))
							{
								int lx2 = curPos.X % 32;
								int lz2 = curPos.Z % 32;
								if (mapchunk.RainHeightMap[lz2 * 32 + lx2] == curPos.Y)
								{
									mapchunk.RainHeightMap[lz2 * 32 + lx2]--;
								}
							}
							else
							{
								maxY = Math.Max(curPos.Y, maxY);
							}
						}
					}
					if (block.GetLightHsv(blockAccessor, curPos)[2] > 0 && blockAccessor is IWorldGenBlockAccessor)
					{
						Block oldBlock = blockAccessor.GetBlock(curPos);
						((IWorldGenBlockAccessor)blockAccessor).ScheduleBlockLightUpdate(curPos, oldBlock.BlockId, block.BlockId);
					}
				}
				if (maxY >= 0)
				{
					int lx = curPos.X % 32;
					int lz = curPos.Z % 32;
					int y = mapchunk.RainHeightMap[lz * 32 + lx];
					mapchunk.RainHeightMap[lz * 32 + lx] = (ushort)Math.Max(y, maxY);
				}
			}
		}
		PlaceDecors(blockAccessor, startPos);
		PlaceEntitiesAndBlockEntities(blockAccessor, worldForCollectibleResolve, startPos, BlockCodesTmpForRemap, ItemCodes, replaceBlockEntities, replaceBlocks, centerrockblockid, layerBlockForBlockEntities, replaceMetaBlocks);
		return placed;
	}

	private void resolveReplaceRemapsForBlockEntities(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid)
	{
		if (replaceBlocks == null)
		{
			BlockCodesTmpForRemap = BlockCodes;
			return;
		}
		foreach (KeyValuePair<int, AssetLocation> val in BlockCodes)
		{
			Block origBlock = worldForCollectibleResolve.GetBlock(val.Value);
			if (origBlock != null)
			{
				BlockCodesTmpForRemap[val.Key] = val.Value;
				if (replaceBlocks.TryGetValue(origBlock.Id, out var replaceByBlock) && replaceByBlock.TryGetValue(centerrockblockid, out var newBlockId))
				{
					BlockCodesTmpForRemap[val.Key] = blockAccessor.GetBlock(newBlockId).Code;
				}
			}
		}
	}

	public virtual int PlaceReplacingBlocks(IBlockAccessor blockAccessor, IWorldAccessor worldForCollectibleResolve, BlockPos startPos, EnumReplaceMode mode, Dictionary<int, Dictionary<int, int>> replaceBlocks, int? rockBlockId, bool replaceMetaBlocks = true)
	{
		Unpack(worldForCollectibleResolve.Api);
		BlockPos curPos = new BlockPos();
		int placed = 0;
		curPos.Set(SizeX / 2 + startPos.X, startPos.Y, SizeZ / 2 + startPos.Z);
		IMapChunk mapchunk = blockAccessor.GetMapChunkAtBlockPos(curPos);
		int centerrockblockid = rockBlockId ?? mapchunk.TopRockIdMap[curPos.Z % 32 * 32 + curPos.X % 32];
		resolveReplaceRemapsForBlockEntities(blockAccessor, worldForCollectibleResolve, replaceBlocks, centerrockblockid);
		PlaceBlockDelegate handler = null;
		switch (ReplaceMode)
		{
		case EnumReplaceMode.ReplaceAll:
			handler = PlaceReplaceAll;
			break;
		case EnumReplaceMode.Replaceable:
			handler = PlaceReplaceable;
			break;
		case EnumReplaceMode.ReplaceAllNoAir:
			handler = PlaceReplaceAllNoAir;
			break;
		case EnumReplaceMode.ReplaceOnlyAir:
			handler = PlaceReplaceOnlyAir;
			break;
		}
		for (int i = 0; i < Indices.Count; i++)
		{
			uint index = Indices[i];
			int storedBlockid = BlockIds[i];
			int dx = (int)(index & 0x3FF);
			int dy = (int)((index >> 20) & 0x3FF);
			int dz = (int)((index >> 10) & 0x3FF);
			AssetLocation blockCode = BlockCodes[storedBlockid];
			Block newBlock = blockAccessor.GetBlock(blockCode);
			if (newBlock == null || (replaceMetaBlocks && (newBlock.Id == BlockSchematic.UndergroundBlockId || newBlock.Id == BlockSchematic.AbovegroundBlockId)))
			{
				continue;
			}
			curPos.Set(dx + startPos.X, dy + startPos.Y, dz + startPos.Z);
			if (blockAccessor.IsValidPos(curPos))
			{
				if (replaceBlocks.TryGetValue(newBlock.Id, out var replaceByBlock) && replaceByBlock.TryGetValue(centerrockblockid, out var newBlockId))
				{
					newBlock = blockAccessor.GetBlock(newBlockId);
				}
				if (newBlock.ForFluidsLayer && index != Indices[i - 1])
				{
					blockAccessor.SetBlock(0, curPos, 1);
				}
				placed += handler(blockAccessor, curPos, newBlock, replaceMeta: true);
				if (newBlock.LightHsv[2] > 0 && blockAccessor is IWorldGenBlockAccessor)
				{
					Block oldBlock = blockAccessor.GetBlock(curPos);
					((IWorldGenBlockAccessor)blockAccessor).ScheduleBlockLightUpdate(curPos, oldBlock.BlockId, newBlock.BlockId);
				}
			}
		}
		if (!(blockAccessor is IBlockAccessorRevertable))
		{
			PlaceDecors(blockAccessor, startPos);
			PlaceEntitiesAndBlockEntities(blockAccessor, worldForCollectibleResolve, startPos, BlockCodesTmpForRemap, ItemCodes, replaceBlockEntities: false, null, centerrockblockid, null, GenStructures.ReplaceMetaBlocks);
		}
		return placed;
	}

	internal Block GetBlockLayerBlock(int unscaledRain, int unscaledTemp, int posY, int rockBlockId, int forDepth, Block defaultBlock, IList<Block> blocks, BlockPos pos, int underWaterDepth)
	{
		if (blockLayerConfig == null)
		{
			return defaultBlock;
		}
		posY -= forDepth;
		float distx = (float)genBlockLayers.distort2dx.Noise(pos.X, pos.Z);
		float temperature = Climate.GetScaledAdjustedTemperatureFloat(unscaledTemp, posY - TerraGenConfig.seaLevel + (int)(distx / 5f));
		float rainRel = (float)Climate.GetRainFall(unscaledRain, posY) / 255f;
		float heightRel = ((float)posY - (float)TerraGenConfig.seaLevel) / ((float)mapheight - (float)TerraGenConfig.seaLevel);
		float fertilityRel = (float)Climate.GetFertilityFromUnscaledTemp((int)(rainRel * 255f), unscaledTemp, heightRel) / 255f;
		double posRand = (double)GameMath.MurmurHash3(pos.X, 1, pos.Z) / 2147483647.0;
		posRand = (posRand + 1.0) * (double)blockLayerConfig.blockLayerTransitionSize;
		for (int i = 0; i < blockLayerConfig.Blocklayers.Length; i++)
		{
			if (underWaterDepth < 0)
			{
				BlockLayer bl = blockLayerConfig.Blocklayers[i];
				float yDist = bl.CalcYDistance(posY, mapheight);
				if (!((double)(bl.CalcTrfDistance(temperature, rainRel, fertilityRel) + yDist) > posRand))
				{
					int blockId = bl.GetBlockId(posRand, temperature, rainRel, fertilityRel, rockBlockId, pos, mapheight);
					if (blockId != 0 && forDepth-- <= 0)
					{
						return blocks[blockId];
					}
				}
			}
			else if (i < blockLayerConfig.LakeBedLayer.BlockCodeByMin.Length)
			{
				LakeBedBlockCodeByMin lbbc = blockLayerConfig.LakeBedLayer.BlockCodeByMin[i];
				if (lbbc.Suitable(temperature, rainRel, (float)posY / (float)mapheight, (float)posRand) && underWaterDepth-- <= 0)
				{
					return blocks[lbbc.GetBlockForMotherRock(rockBlockId)];
				}
			}
		}
		return defaultBlock;
	}

	public override BlockSchematic ClonePacked()
	{
		return new BlockSchematicStructure
		{
			SizeX = SizeX,
			SizeY = SizeY,
			SizeZ = SizeZ,
			OffsetY = OffsetY,
			MaxYDiff = MaxYDiff,
			MaxBelowSealevel = MaxBelowSealevel,
			GameVersion = GameVersion,
			FromFileName = FromFileName,
			BlockCodes = new Dictionary<int, AssetLocation>(BlockCodes),
			ItemCodes = new Dictionary<int, AssetLocation>(ItemCodes),
			Indices = new List<uint>(Indices),
			BlockIds = new List<int>(BlockIds),
			BlockEntities = new Dictionary<uint, string>(BlockEntities),
			Entities = new List<string>(Entities),
			DecorIndices = new List<uint>(DecorIndices),
			DecorIds = new List<long>(DecorIds),
			ReplaceMode = ReplaceMode,
			EntranceRotation = EntranceRotation,
			OriginalPos = OriginalPos
		};
	}

	public void Unpack(ICoreAPI api)
	{
		if (blocksByPos == null)
		{
			Init(api.World.BlockAccessor);
			LoadMetaInformationAndValidate(api.World.BlockAccessor, api.World, FromFileName);
		}
	}

	public void Unpack(ICoreAPI api, int orientation)
	{
		if (orientation > 0 && blocksByPos == null)
		{
			TransformWhilePacked(api.World, EnumOrigin.BottomCenter, orientation * 90, null, PathwayBlocksUnpacked != null);
		}
		Unpack(api);
	}
}
