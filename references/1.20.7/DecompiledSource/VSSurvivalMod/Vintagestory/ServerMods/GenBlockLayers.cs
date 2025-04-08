using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenBlockLayers : ModStdWorldGen
{
	private ICoreServerAPI api;

	private List<int> BlockLayersIds = new List<int>();

	private LCGRandom rnd;

	private int mapheight;

	private ClampedSimplexNoise grassDensity;

	private ClampedSimplexNoise grassHeight;

	private int boilingWaterBlockId;

	public int[] layersUnderWater = new int[0];

	public BlockLayerConfig blockLayerConfig;

	public SimplexNoise distort2dx;

	public SimplexNoise distort2dz;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.4;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		this.api.Event.InitWorldGenerator(InitWorldGen, "standard");
		this.api.Event.InitWorldGenerator(InitWorldGen, "superflat");
		if (TerraGenConfig.DoDecorationPass)
		{
			this.api.Event.ChunkColumnGeneration(OnChunkColumnGeneration, EnumWorldGenPass.Terrain, "standard");
		}
		distort2dx = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 20980);
		distort2dz = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 20981);
	}

	private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams = null)
	{
	}

	public void InitWorldGen()
	{
		LoadGlobalConfig(api);
		blockLayerConfig = BlockLayerConfig.GetInstance(api);
		rnd = new LCGRandom(api.WorldManager.Seed);
		grassDensity = new ClampedSimplexNoise(new double[1] { 4.0 }, new double[1] { 0.5 }, rnd.NextInt());
		grassHeight = new ClampedSimplexNoise(new double[1] { 1.5 }, new double[1] { 0.5 }, rnd.NextInt());
		mapheight = api.WorldManager.MapSizeY;
		boilingWaterBlockId = api.World.GetBlock(new AssetLocation("boilingwater-still-7"))?.Id ?? 0;
	}

	private void OnChunkColumnGeneration(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		this.rnd.InitPositionSeed(chunkX, chunkZ);
		IntDataMap2D forestMap = chunks[0].MapChunk.MapRegion.ForestMap;
		IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
		IntDataMap2D beachMap = chunks[0].MapChunk.MapRegion.BeachMap;
		ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;
		int regionChunkSize = api.WorldManager.RegionSize / 32;
		int rdx = chunkX % regionChunkSize;
		int rdz = chunkZ % regionChunkSize;
		float climateStep = (float)climateMap.InnerSize / (float)regionChunkSize;
		float forestStep = (float)forestMap.InnerSize / (float)regionChunkSize;
		float beachStep = (float)beachMap.InnerSize / (float)regionChunkSize;
		int forestUpLeft = forestMap.GetUnpaddedInt((int)((float)rdx * forestStep), (int)((float)rdz * forestStep));
		int forestUpRight = forestMap.GetUnpaddedInt((int)((float)rdx * forestStep + forestStep), (int)((float)rdz * forestStep));
		int forestBotLeft = forestMap.GetUnpaddedInt((int)((float)rdx * forestStep), (int)((float)rdz * forestStep + forestStep));
		int forestBotRight = forestMap.GetUnpaddedInt((int)((float)rdx * forestStep + forestStep), (int)((float)rdz * forestStep + forestStep));
		int beachUpLeft = beachMap.GetUnpaddedInt((int)((float)rdx * beachStep), (int)((float)rdz * beachStep));
		int beachUpRight = beachMap.GetUnpaddedInt((int)((float)rdx * beachStep + beachStep), (int)((float)rdz * beachStep));
		int beachBotLeft = beachMap.GetUnpaddedInt((int)((float)rdx * beachStep), (int)((float)rdz * beachStep + beachStep));
		int beachBotRight = beachMap.GetUnpaddedInt((int)((float)rdx * beachStep + beachStep), (int)((float)rdz * beachStep + beachStep));
		float transitionSize = blockLayerConfig.blockLayerTransitionSize;
		BlockPos herePos = new BlockPos();
		for (int x = 0; x < 32; x++)
		{
			for (int z = 0; z < 32; z++)
			{
				herePos.Set(chunkX * 32 + x, 1, chunkZ * 32 + z);
				double distx;
				double distz;
				int rnd = RandomlyAdjustPosition(herePos, out distx, out distz);
				double transitionRand = ((double)GameMath.MurmurHash3(herePos.X, 1, herePos.Z) / 2147483647.0 + 1.0) * (double)transitionSize;
				int posY = heightMap[z * 32 + x];
				if (posY >= mapheight)
				{
					continue;
				}
				int unpaddedColorLerped = climateMap.GetUnpaddedColorLerped((float)rdx * climateStep + climateStep * ((float)x + (float)distx) / 32f, (float)rdz * climateStep + climateStep * ((float)z + (float)distz) / 32f);
				int tempUnscaled = (unpaddedColorLerped >> 16) & 0xFF;
				float temp = Climate.GetScaledAdjustedTemperatureFloat(tempUnscaled, posY - TerraGenConfig.seaLevel + rnd);
				float tempRel = (float)Climate.GetAdjustedTemperature(tempUnscaled, posY - TerraGenConfig.seaLevel + rnd) / 255f;
				float rainRel = (float)Climate.GetRainFall((unpaddedColorLerped >> 8) & 0xFF, posY + rnd) / 255f;
				float forestRel = GameMath.BiLerp(forestUpLeft, forestUpRight, forestBotLeft, forestBotRight, (float)x / 32f, (float)z / 32f) / 255f;
				int prevY = posY;
				int rocky = chunks[0].MapChunk.WorldGenTerrainHeightMap[z * 32 + x];
				int chunkY = rocky / 32;
				int lY = rocky % 32;
				int index3d = (32 * lY + z) * 32 + x;
				int rockblockID = chunks[chunkY].Data.GetBlockIdUnsafe(index3d);
				Block hereblock = api.World.Blocks[rockblockID];
				if (hereblock.BlockMaterial != EnumBlockMaterial.Stone && hereblock.BlockMaterial != EnumBlockMaterial.Liquid)
				{
					continue;
				}
				if (rocky < TerraGenConfig.seaLevel)
				{
					int sealevelrise = (int)Math.Min(Math.Max(0f, (0.5f - rainRel) * 40f), TerraGenConfig.seaLevel - rocky);
					int curSealevel = chunks[0].MapChunk.WorldGenTerrainHeightMap[z * 32 + x];
					chunks[0].MapChunk.WorldGenTerrainHeightMap[z * 32 + x] = (ushort)Math.Max(rocky + sealevelrise - 1, curSealevel);
					while (sealevelrise-- > 0)
					{
						chunkY = rocky / 32;
						lY = rocky % 32;
						index3d = (32 * lY + z) * 32 + x;
						IChunkBlocks data = chunks[chunkY].Data;
						data.SetBlockUnsafe(index3d, rockblockID);
						data.SetFluid(index3d, 0);
						rocky++;
					}
				}
				herePos.Y = posY;
				int disty = (int)(distort2dx.Noise(-herePos.X, -herePos.Z) / 4.0);
				posY = PutLayers(transitionRand, x, z, disty, herePos, chunks, rainRel, temp, tempUnscaled, heightMap);
				if (prevY == TerraGenConfig.seaLevel - 1)
				{
					float beachRel = GameMath.BiLerp(beachUpLeft, beachUpRight, beachBotLeft, beachBotRight, (float)x / 32f, (float)z / 32f) / 255f;
					GenBeach(x, prevY, z, chunks, rainRel, temp, beachRel, rockblockID);
				}
				PlaceTallGrass(x, prevY, z, chunks, rainRel, tempRel, temp, forestRel);
				int foundAir = 0;
				while (posY >= TerraGenConfig.seaLevel - 1)
				{
					chunkY = posY / 32;
					lY = posY % 32;
					index3d = (32 * lY + z) * 32 + x;
					if (chunks[chunkY].Data.GetBlockIdUnsafe(index3d) == 0)
					{
						foundAir++;
					}
					else
					{
						if (foundAir >= 8)
						{
							break;
						}
						foundAir = 0;
					}
					posY--;
				}
			}
		}
	}

	public int RandomlyAdjustPosition(BlockPos herePos, out double distx, out double distz)
	{
		distx = distort2dx.Noise(herePos.X, herePos.Z);
		distz = distort2dz.Noise(herePos.X, herePos.Z);
		return (int)(distx / 5.0);
	}

	private int PutLayers(double posRand, int lx, int lz, int posyoffs, BlockPos pos, IServerChunk[] chunks, float rainRel, float temp, int unscaledTemp, ushort[] heightMap)
	{
		int i = 0;
		int j = 0;
		bool underWater = false;
		bool underIce = false;
		bool first = true;
		int startPosY = pos.Y;
		while (pos.Y > 0)
		{
			int chunkY = pos.Y / 32;
			int lY = pos.Y % 32;
			int index3d = (32 * lY + lz) * 32 + lx;
			int blockId = chunks[chunkY].Data.GetBlockIdUnsafe(index3d);
			if (blockId == 0)
			{
				blockId = chunks[chunkY].Data.GetFluid(index3d);
			}
			pos.Y--;
			if (blockId != 0)
			{
				if (blockId == GlobalConfig.waterBlockId || blockId == boilingWaterBlockId || blockId == GlobalConfig.saltWaterBlockId)
				{
					underWater = true;
					continue;
				}
				if (blockId == GlobalConfig.lakeIceBlockId)
				{
					underIce = true;
					continue;
				}
				if (heightMap != null && first)
				{
					chunks[0].MapChunk.TopRockIdMap[lz * 32 + lx] = blockId;
					if (underIce)
					{
						break;
					}
					LoadBlockLayers(posRand, rainRel, temp, unscaledTemp, startPosY + posyoffs, pos, blockId);
					first = false;
					if (!underWater)
					{
						heightMap[lz * 32 + lx] = (ushort)(pos.Y + 1);
					}
				}
				if (i >= BlockLayersIds.Count || (underWater && j >= layersUnderWater.Length))
				{
					return pos.Y;
				}
				IChunkBlocks data = chunks[chunkY].Data;
				data.SetBlockUnsafe(index3d, underWater ? layersUnderWater[j++] : BlockLayersIds[i++]);
				data.SetFluid(index3d, 0);
			}
			else if ((i > 0 && temp > -18f) || j > 0)
			{
				return pos.Y;
			}
		}
		return pos.Y;
	}

	private void GenBeach(int x, int posY, int z, IServerChunk[] chunks, float rainRel, float temp, float beachRel, int topRockId)
	{
		int sandBlockId = blockLayerConfig.BeachLayer.BlockId;
		if (blockLayerConfig.BeachLayer.BlockIdMapping != null && !blockLayerConfig.BeachLayer.BlockIdMapping.TryGetValue(topRockId, out sandBlockId))
		{
			return;
		}
		int index3d = (32 * (posY % 32) + z) * 32 + x;
		if (!((double)beachRel > 0.5))
		{
			return;
		}
		IChunkBlocks chunkdata = chunks[posY / 32].Data;
		if (chunkdata.GetBlockIdUnsafe(index3d) == 0)
		{
			return;
		}
		int fluidId = chunkdata.GetFluid(index3d);
		if (fluidId != GlobalConfig.waterBlockId && fluidId != GlobalConfig.lakeIceBlockId)
		{
			chunkdata.SetBlockUnsafe(index3d, sandBlockId);
			if (fluidId != 0)
			{
				chunkdata.SetFluid(index3d, 0);
			}
		}
	}

	private void PlaceTallGrass(int x, int posY, int z, IServerChunk[] chunks, float rainRel, float tempRel, float temp, float forestRel)
	{
		double num = (double)blockLayerConfig.Tallgrass.RndWeight * rnd.NextDouble() + (double)blockLayerConfig.Tallgrass.PerlinWeight * grassDensity.Noise(x, z, -0.5);
		double extraGrass = Math.Max(0.0, (double)(rainRel * tempRel) - 0.25);
		if (num <= GameMath.Clamp((double)forestRel - extraGrass, 0.05, 0.99) || posY >= mapheight - 1 || posY < 1)
		{
			return;
		}
		int blockId = chunks[posY / 32].Data[(32 * (posY % 32) + z) * 32 + x];
		if (api.World.Blocks[blockId].Fertility <= rnd.NextInt(100))
		{
			return;
		}
		double gheight = Math.Max(0.0, grassHeight.Noise(x, z) * (double)blockLayerConfig.Tallgrass.BlockCodeByMin.Length - 1.0);
		for (int i = (int)gheight + ((rnd.NextDouble() < gheight) ? 1 : 0); i < blockLayerConfig.Tallgrass.BlockCodeByMin.Length; i++)
		{
			TallGrassBlockCodeByMin bcbymin = blockLayerConfig.Tallgrass.BlockCodeByMin[i];
			if (forestRel <= bcbymin.MaxForest && rainRel >= bcbymin.MinRain && temp >= (float)bcbymin.MinTemp)
			{
				chunks[(posY + 1) / 32].Data[(32 * ((posY + 1) % 32) + z) * 32 + x] = bcbymin.BlockId;
				break;
			}
		}
	}

	private void LoadBlockLayers(double posRand, float rainRel, float temperature, int unscaledTemp, int posY, BlockPos pos, int firstBlockId)
	{
		float heightRel = ((float)posY - (float)TerraGenConfig.seaLevel) / ((float)mapheight - (float)TerraGenConfig.seaLevel);
		float fertilityRel = (float)Climate.GetFertilityFromUnscaledTemp((int)(rainRel * 255f), unscaledTemp, heightRel) / 255f;
		float num = TerraGenConfig.SoilThickness(rainRel, temperature, posY - TerraGenConfig.seaLevel, 1f);
		int depth = (int)num;
		if (num - (float)depth > rnd.NextFloat())
		{
			depth++;
		}
		if (temperature < -16f)
		{
			depth += 10;
		}
		BlockLayersIds.Clear();
		for (int j = 0; j < blockLayerConfig.Blocklayers.Length; j++)
		{
			BlockLayer bl = blockLayerConfig.Blocklayers[j];
			float yDist = bl.CalcYDistance(posY, mapheight);
			float trfDist = bl.CalcTrfDistance(temperature, rainRel, fertilityRel);
			if ((double)(trfDist + yDist) <= posRand)
			{
				int blockId = bl.GetBlockId(posRand, temperature, rainRel, fertilityRel, firstBlockId, pos, mapheight);
				if (blockId != 0)
				{
					BlockLayersIds.Add(blockId);
					if (bl.Thickness > 1)
					{
						for (int i = 1; (float)i < (float)bl.Thickness * (1f - trfDist * yDist); i++)
						{
							BlockLayersIds.Add(blockId);
							yDist = Math.Abs((float)posY-- / (float)mapheight - GameMath.Min((float)posY-- / (float)mapheight, bl.MaxY));
						}
					}
					posY--;
					temperature = Climate.GetScaledAdjustedTemperatureFloat(unscaledTemp, posY - TerraGenConfig.seaLevel);
					heightRel = ((float)posY - (float)TerraGenConfig.seaLevel) / ((float)api.WorldManager.MapSizeY - (float)TerraGenConfig.seaLevel);
					fertilityRel = (float)Climate.GetFertilityFromUnscaledTemp((int)(rainRel * 255f), unscaledTemp, heightRel) / 255f;
				}
			}
			if (BlockLayersIds.Count >= depth)
			{
				break;
			}
		}
		int lakeBedId = blockLayerConfig.LakeBedLayer.GetSuitable(temperature, rainRel, (float)posY / (float)api.WorldManager.MapSizeY, rnd, firstBlockId);
		if (lakeBedId == 0)
		{
			layersUnderWater = new int[0];
			return;
		}
		layersUnderWater = new int[1] { lakeBedId };
	}
}
