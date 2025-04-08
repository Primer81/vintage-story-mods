using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class GenTerra : ModStdWorldGen
{
	private struct ThreadLocalTempData
	{
		public double[] LerpedAmplitudes;

		public double[] LerpedThresholds;

		public float[] landformWeights;
	}

	private struct WeightedTaper
	{
		public float TerrainYPos;

		public float Weight;
	}

	private struct ColumnResult
	{
		public BitArray ColumnBlockSolidities;

		public int WaterBlockID;
	}

	private struct VectorXZ
	{
		public double X;

		public double Z;

		public static VectorXZ operator *(VectorXZ a, double b)
		{
			VectorXZ result = default(VectorXZ);
			result.X = a.X * b;
			result.Z = a.Z * b;
			return result;
		}
	}

	private ICoreServerAPI api;

	private const double terrainDistortionMultiplier = 4.0;

	private const double terrainDistortionThreshold = 40.0;

	private const double geoDistortionMultiplier = 10.0;

	private const double geoDistortionThreshold = 10.0;

	private const double maxDistortionAmount = 190.91883092036784;

	private int maxThreads;

	private LandformsWorldProperty landforms;

	private float[][] terrainYThresholds;

	private Dictionary<int, LerpedWeightedIndex2DMap> LandformMapByRegion = new Dictionary<int, LerpedWeightedIndex2DMap>(10);

	private int regionMapSize;

	private float noiseScale;

	private int terrainGenOctaves = 9;

	private NewNormalizedSimplexFractalNoise terrainNoise;

	private SimplexNoise distort2dx;

	private SimplexNoise distort2dz;

	private NormalizedSimplexNoise geoUpheavalNoise;

	private WeightedTaper[] taperMap;

	private ThreadLocal<ThreadLocalTempData> tempDataThreadLocal;

	private ColumnResult[] columnResults;

	private bool[] layerFullySolid;

	private bool[] layerFullyEmpty;

	private int[] borderIndicesByCardinal;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.0;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		api.Event.ServerRunPhase(EnumServerRunPhase.LoadGamePre, loadGamePre);
		api.Event.InitWorldGenerator(initWorldGen, "standard");
		api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Terrain, "standard");
	}

	private void loadGamePre()
	{
		if (!(api.WorldManager.SaveGame.WorldType != "standard"))
		{
			TerraGenConfig.seaLevel = (int)(0.4313725490196078 * (double)api.WorldManager.MapSizeY);
			api.WorldManager.SetSeaLevel(TerraGenConfig.seaLevel);
			Climate.Sealevel = TerraGenConfig.seaLevel;
		}
	}

	public void initWorldGen()
	{
		LoadGlobalConfig(api);
		LandformMapByRegion.Clear();
		maxThreads = Math.Clamp(Environment.ProcessorCount - (api.Server.IsDedicated ? 4 : 6), 1, api.Server.Config.HostedMode ? 4 : 10);
		regionMapSize = (int)Math.Ceiling((double)api.WorldManager.MapSizeX / (double)api.WorldManager.RegionSize);
		noiseScale = Math.Max(1f, (float)api.WorldManager.MapSizeY / 256f);
		terrainGenOctaves = TerraGenConfig.GetTerrainOctaveCount(api.WorldManager.MapSizeY);
		terrainNoise = NewNormalizedSimplexFractalNoise.FromDefaultOctaves(terrainGenOctaves, 0.00030618621784789723 / (double)noiseScale, 0.9, api.WorldManager.Seed);
		distort2dx = new SimplexNoise(new double[4] { 55.0, 40.0, 30.0, 10.0 }, scaleAdjustedFreqs(new double[4] { 0.2, 0.4, 0.8, 1.5384615384615383 }, noiseScale), api.World.Seed + 9876);
		distort2dz = new SimplexNoise(new double[4] { 55.0, 40.0, 30.0, 10.0 }, scaleAdjustedFreqs(new double[4] { 0.2, 0.4, 0.8, 1.5384615384615383 }, noiseScale), api.World.Seed + 9876 + 2);
		geoUpheavalNoise = new NormalizedSimplexNoise(new double[6] { 55.0, 40.0, 30.0, 15.0, 7.0, 4.0 }, scaleAdjustedFreqs(new double[6]
		{
			0.18181818181818182,
			0.4,
			48.0 / 55.0,
			1.6783216783216783,
			2.6666666666666665,
			4.8
		}, noiseScale), api.World.Seed + 9876 + 1);
		tempDataThreadLocal = new ThreadLocal<ThreadLocalTempData>(delegate
		{
			ThreadLocalTempData result = default(ThreadLocalTempData);
			result.LerpedAmplitudes = new double[terrainGenOctaves];
			result.LerpedThresholds = new double[terrainGenOctaves];
			result.landformWeights = new float[NoiseLandforms.landforms.LandFormsByIndex.Length];
			return result;
		});
		columnResults = new ColumnResult[1024];
		layerFullyEmpty = new bool[api.WorldManager.MapSizeY];
		layerFullySolid = new bool[api.WorldManager.MapSizeY];
		taperMap = new WeightedTaper[1024];
		for (int i = 0; i < 1024; i++)
		{
			columnResults[i].ColumnBlockSolidities = new BitArray(api.WorldManager.MapSizeY);
		}
		borderIndicesByCardinal = new int[8];
		borderIndicesByCardinal[Cardinal.NorthEast.Index] = 992;
		borderIndicesByCardinal[Cardinal.SouthEast.Index] = 0;
		borderIndicesByCardinal[Cardinal.SouthWest.Index] = 31;
		borderIndicesByCardinal[Cardinal.NorthWest.Index] = 1023;
		landforms = null;
	}

	private double[] scaleAdjustedFreqs(double[] vs, float horizontalScale)
	{
		for (int i = 0; i < vs.Length; i++)
		{
			vs[i] /= horizontalScale;
		}
		return vs;
	}

	private void OnChunkColumnGen(IChunkColumnGenerateRequest request)
	{
		if (request.RequiresChunkBorderSmoothing)
		{
			ushort[][] neibHeightMaps = request.NeighbourTerrainHeight;
			if (neibHeightMaps[Cardinal.North.Index] != null)
			{
				neibHeightMaps[Cardinal.NorthEast.Index] = null;
				neibHeightMaps[Cardinal.NorthWest.Index] = null;
			}
			if (neibHeightMaps[Cardinal.East.Index] != null)
			{
				neibHeightMaps[Cardinal.NorthEast.Index] = null;
				neibHeightMaps[Cardinal.SouthEast.Index] = null;
			}
			if (neibHeightMaps[Cardinal.South.Index] != null)
			{
				neibHeightMaps[Cardinal.SouthWest.Index] = null;
				neibHeightMaps[Cardinal.SouthEast.Index] = null;
			}
			if (neibHeightMaps[Cardinal.West.Index] != null)
			{
				neibHeightMaps[Cardinal.SouthWest.Index] = null;
				neibHeightMaps[Cardinal.NorthWest.Index] = null;
			}
			string sides = "";
			for (int k = 0; k < Cardinal.ALL.Length; k++)
			{
				if (neibHeightMaps[k] != null)
				{
					sides = sides + Cardinal.ALL[k].Code + "_";
				}
			}
			for (int dx = 0; dx < 32; dx++)
			{
				borderIndicesByCardinal[Cardinal.North.Index] = 992 + dx;
				borderIndicesByCardinal[Cardinal.South.Index] = dx;
				for (int dz = 0; dz < 32; dz++)
				{
					double sumWeight = 0.0;
					double ypos = 0.0;
					float maxWeight = 0f;
					borderIndicesByCardinal[Cardinal.East.Index] = dz * 32;
					borderIndicesByCardinal[Cardinal.West.Index] = dz * 32 + 32 - 1;
					for (int j = 0; j < Cardinal.ALL.Length; j++)
					{
						ushort[] neibMap = neibHeightMaps[j];
						if (neibMap != null)
						{
							float distToEdge = 0f;
							switch (j)
							{
							case 0:
								distToEdge = (float)dz / 32f;
								break;
							case 1:
								distToEdge = 1f - (float)dx / 32f + (float)dz / 32f;
								break;
							case 2:
								distToEdge = 1f - (float)dx / 32f;
								break;
							case 3:
								distToEdge = 1f - (float)dx / 32f + (1f - (float)dz / 32f);
								break;
							case 4:
								distToEdge = 1f - (float)dz / 32f;
								break;
							case 5:
								distToEdge = (float)dx / 32f + 1f - (float)dz / 32f;
								break;
							case 6:
								distToEdge = (float)dx / 32f;
								break;
							case 7:
								distToEdge = (float)dx / 32f + (float)dz / 32f;
								break;
							}
							float cardinalWeight = (float)Math.Pow(1f - GameMath.Clamp(distToEdge, 0f, 1f), 2.0);
							float neibYPos = (float)(int)neibMap[borderIndicesByCardinal[j]] + 0.5f;
							ypos += (double)neibYPos * Math.Max(0.0001, cardinalWeight);
							sumWeight += (double)cardinalWeight;
							maxWeight = Math.Max(maxWeight, cardinalWeight);
						}
					}
					taperMap[dz * 32 + dx] = new WeightedTaper
					{
						TerrainYPos = (float)(ypos / Math.Max(0.0001, sumWeight)),
						Weight = maxWeight
					};
				}
			}
		}
		if (landforms == null)
		{
			landforms = NoiseLandforms.landforms;
			terrainYThresholds = new float[landforms.LandFormsByIndex.Length][];
			for (int i = 0; i < landforms.LandFormsByIndex.Length; i++)
			{
				terrainYThresholds[i] = landforms.LandFormsByIndex[i].TerrainYThresholds;
			}
		}
		generate(request.Chunks, request.ChunkX, request.ChunkZ, request.RequiresChunkBorderSmoothing);
	}

	private void generate(IServerChunk[] chunks, int chunkX, int chunkZ, bool requiresChunkBorderSmoothing)
	{
		IMapChunk mapchunk = chunks[0].MapChunk;
		int upheavalMapUpLeft = 0;
		int upheavalMapUpRight = 0;
		int upheavalMapBotLeft = 0;
		int upheavalMapBotRight = 0;
		IntDataMap2D climateMap = chunks[0].MapChunk.MapRegion.ClimateMap;
		IntDataMap2D oceanMap = chunks[0].MapChunk.MapRegion.OceanMap;
		int regionChunkSize = api.WorldManager.RegionSize / 32;
		float cfac = (float)climateMap.InnerSize / (float)regionChunkSize;
		int rlX = chunkX % regionChunkSize;
		int rlZ = chunkZ % regionChunkSize;
		int climateUpLeft = climateMap.GetUnpaddedInt((int)((float)rlX * cfac), (int)((float)rlZ * cfac));
		int climateUpRight = climateMap.GetUnpaddedInt((int)((float)rlX * cfac + cfac), (int)((float)rlZ * cfac));
		int climateBotLeft = climateMap.GetUnpaddedInt((int)((float)rlX * cfac), (int)((float)rlZ * cfac + cfac));
		int climateBotRight = climateMap.GetUnpaddedInt((int)((float)rlX * cfac + cfac), (int)((float)rlZ * cfac + cfac));
		int oceanUpLeft = 0;
		int oceanUpRight = 0;
		int oceanBotLeft = 0;
		int oceanBotRight = 0;
		if (oceanMap != null && oceanMap.Data.Length != 0)
		{
			float ofac = (float)oceanMap.InnerSize / (float)regionChunkSize;
			oceanUpLeft = oceanMap.GetUnpaddedInt((int)((float)rlX * ofac), (int)((float)rlZ * ofac));
			oceanUpRight = oceanMap.GetUnpaddedInt((int)((float)rlX * ofac + ofac), (int)((float)rlZ * ofac));
			oceanBotLeft = oceanMap.GetUnpaddedInt((int)((float)rlX * ofac), (int)((float)rlZ * ofac + ofac));
			oceanBotRight = oceanMap.GetUnpaddedInt((int)((float)rlX * ofac + ofac), (int)((float)rlZ * ofac + ofac));
		}
		IntDataMap2D upheavalMap = chunks[0].MapChunk.MapRegion.UpheavelMap;
		if (upheavalMap != null)
		{
			float ufac = (float)upheavalMap.InnerSize / (float)regionChunkSize;
			upheavalMapUpLeft = upheavalMap.GetUnpaddedInt((int)((float)rlX * ufac), (int)((float)rlZ * ufac));
			upheavalMapUpRight = upheavalMap.GetUnpaddedInt((int)((float)rlX * ufac + ufac), (int)((float)rlZ * ufac));
			upheavalMapBotLeft = upheavalMap.GetUnpaddedInt((int)((float)rlX * ufac), (int)((float)rlZ * ufac + ufac));
			upheavalMapBotRight = upheavalMap.GetUnpaddedInt((int)((float)rlX * ufac + ufac), (int)((float)rlZ * ufac + ufac));
		}
		int rockID = GlobalConfig.defaultRockId;
		float oceanicityFac = (float)(api.WorldManager.MapSizeY / 256) * 0.33333f;
		float chunkPixelSize = mapchunk.MapRegion.LandformMap.InnerSize / regionChunkSize;
		float baseX = (float)(chunkX % regionChunkSize) * chunkPixelSize;
		float baseZ = (float)(chunkZ % regionChunkSize) * chunkPixelSize;
		LerpedWeightedIndex2DMap landLerpMap = GetOrLoadLerpedLandformMap(mapchunk, chunkX / regionChunkSize, chunkZ / regionChunkSize);
		float[] landformWeights = tempDataThreadLocal.Value.landformWeights;
		GetInterpolatedOctaves(landLerpMap.WeightsAt(baseX, baseZ, landformWeights), out var octNoiseX0, out var octThX0);
		GetInterpolatedOctaves(landLerpMap.WeightsAt(baseX + chunkPixelSize, baseZ, landformWeights), out var octNoiseX1, out var octThX1);
		GetInterpolatedOctaves(landLerpMap.WeightsAt(baseX, baseZ + chunkPixelSize, landformWeights), out var octNoiseX2, out var octThX2);
		GetInterpolatedOctaves(landLerpMap.WeightsAt(baseX + chunkPixelSize, baseZ + chunkPixelSize, landformWeights), out var octNoiseX3, out var octThX3);
		float[][] terrainYThresholds = this.terrainYThresholds;
		ushort[] rainheightmap = chunks[0].MapChunk.RainHeightMap;
		ushort[] terrainheightmap = chunks[0].MapChunk.WorldGenTerrainHeightMap;
		int mapsizeY = api.WorldManager.MapSizeY;
		int mapsizeYm2 = api.WorldManager.MapSizeY - 2;
		int taperThreshold = (int)((float)mapsizeY * 0.9f);
		double geoUpheavalAmplitude = 255.0;
		float chunkPixelBlockStep = chunkPixelSize * (1f / 32f);
		double verticalNoiseRelativeFrequency = 0.5 / TerraGenConfig.terrainNoiseVerticalScale;
		for (int y2 = 0; y2 < layerFullySolid.Length; y2++)
		{
			layerFullySolid[y2] = true;
		}
		for (int y = 0; y < layerFullyEmpty.Length; y++)
		{
			layerFullyEmpty[y] = true;
		}
		layerFullyEmpty[mapsizeY - 1] = false;
		Parallel.For(0, 1024, new ParallelOptions
		{
			MaxDegreeOfParallelism = maxThreads
		}, delegate(int chunkIndex2d)
		{
			int num = chunkIndex2d % 32;
			int num2 = chunkIndex2d / 32;
			int num3 = chunkX * 32 + num;
			int num4 = chunkZ * 32 + num2;
			BitArray columnBlockSolidities = columnResults[chunkIndex2d].ColumnBlockSolidities;
			columnBlockSolidities.SetAll(value: false);
			double[] lerpedAmplitudes = tempDataThreadLocal.Value.LerpedAmplitudes;
			double[] lerpedThresholds = tempDataThreadLocal.Value.LerpedThresholds;
			float[] landformWeights2 = tempDataThreadLocal.Value.landformWeights;
			landLerpMap.WeightsAt(baseX + (float)num * chunkPixelBlockStep, baseZ + (float)num2 * chunkPixelBlockStep, landformWeights2);
			for (int j = 0; j < lerpedAmplitudes.Length; j++)
			{
				lerpedAmplitudes[j] = GameMath.BiLerp(octNoiseX0[j], octNoiseX1[j], octNoiseX2[j], octNoiseX3[j], (float)num * (1f / 32f), (float)num2 * (1f / 32f));
				lerpedThresholds[j] = GameMath.BiLerp(octThX0[j], octThX1[j], octThX2[j], octThX3[j], (float)num * (1f / 32f), (float)num2 * (1f / 32f));
			}
			VectorXZ vectorXZ = NewDistortionNoise(num3, num4);
			VectorXZ vectorXZ2 = ApplyIsotropicDistortionThreshold(vectorXZ * 4.0, 40.0, 763.6753236814714);
			float upheavalStrength = GameMath.BiLerp(upheavalMapUpLeft, upheavalMapUpRight, upheavalMapBotLeft, upheavalMapBotRight, (float)num * (1f / 32f), (float)num2 * (1f / 32f));
			float num5 = GameMath.BiLerp(oceanUpLeft, oceanUpRight, oceanBotLeft, oceanBotRight, (float)num * (1f / 32f), (float)num2 * (1f / 32f)) * oceanicityFac;
			VectorXZ distGeo = ApplyIsotropicDistortionThreshold(vectorXZ * 10.0, 10.0, 1909.1883092036783);
			float num6 = num5 + ComputeOceanAndUpheavalDistY(upheavalStrength, num3, num4, distGeo);
			columnResults[chunkIndex2d].WaterBlockID = ((num5 > 1f) ? GlobalConfig.saltWaterBlockId : GlobalConfig.waterBlockId);
			NewNormalizedSimplexFractalNoise.ColumnNoise columnNoise = terrainNoise.ForColumn(verticalNoiseRelativeFrequency, lerpedAmplitudes, lerpedThresholds, (double)num3 + vectorXZ2.X, (double)num4 + vectorXZ2.Z);
			double boundMin = columnNoise.BoundMin;
			double boundMax = columnNoise.BoundMax;
			WeightedTaper weightedTaper = taperMap[chunkIndex2d];
			float ySlide = num6 - (float)(int)Math.Floor(num6);
			for (int k = 1; k <= mapsizeYm2; k++)
			{
				StartSampleDisplacedYThreshold((float)k + num6, mapsizeYm2, out var yBase2);
				double threshold = 0.0;
				for (int l = 0; l < landformWeights2.Length; l++)
				{
					float num7 = landformWeights2[l];
					if (num7 != 0f)
					{
						threshold += (double)(num7 * ContinueSampleDisplacedYThreshold(yBase2, ySlide, terrainYThresholds[l]));
					}
				}
				ComputeGeoUpheavalTaper(k, num6, taperThreshold, geoUpheavalAmplitude, mapsizeY, ref threshold);
				if (requiresChunkBorderSmoothing)
				{
					double num8 = (((float)k > weightedTaper.TerrainYPos) ? 1 : (-1));
					float num9 = Math.Abs((float)k - weightedTaper.TerrainYPos);
					double num10 = ((num9 > 10f) ? 0.0 : (distort2dx.Noise((double)(-(chunkX * 32 + num)) / 10.0, (double)k / 10.0, (double)(-(chunkZ * 32 + num2)) / 10.0) / Math.Max(1.0, (double)num9 / 2.0)));
					num10 *= (double)GameMath.Clamp(2f * (1f - weightedTaper.Weight), 0f, 1f) * 0.1;
					threshold = GameMath.Lerp(threshold, num8 + num10, weightedTaper.Weight);
				}
				if (threshold <= boundMin)
				{
					columnBlockSolidities[k] = true;
					layerFullyEmpty[k] = false;
				}
				else
				{
					if (!(threshold < boundMax))
					{
						layerFullySolid[k] = false;
						for (int m = k + 1; m <= mapsizeYm2; m++)
						{
							layerFullySolid[m] = false;
						}
						break;
					}
					double inverseCurvedThresholder = 0.0 - NormalizedSimplexNoise.NoiseValueCurveInverse(threshold);
					inverseCurvedThresholder = columnNoise.NoiseSign(k, inverseCurvedThresholder);
					if (inverseCurvedThresholder > 0.0)
					{
						columnBlockSolidities[k] = true;
						layerFullyEmpty[k] = false;
					}
					else
					{
						layerFullySolid[k] = false;
					}
				}
			}
		});
		IChunkBlocks chunkBlockData = chunks[0].Data;
		chunkBlockData.SetBlockBulk(0, 32, 32, GlobalConfig.mantleBlockId);
		int yBase;
		for (yBase = 1; yBase < mapsizeY - 1 && layerFullySolid[yBase]; yBase++)
		{
			if (yBase % 32 == 0)
			{
				chunkBlockData = chunks[yBase / 32].Data;
			}
			chunkBlockData.SetBlockBulk(yBase % 32 * 32 * 32, 32, 32, rockID);
		}
		int seaLevel = TerraGenConfig.seaLevel;
		int surfaceWaterId = 0;
		int yTop = mapsizeY - 2;
		while (yTop >= yBase && layerFullyEmpty[yTop])
		{
			yTop--;
		}
		if (yTop < seaLevel)
		{
			yTop = seaLevel;
		}
		yTop++;
		for (int lZ = 0; lZ < 32; lZ++)
		{
			int worldZ = chunkZ * 32 + lZ;
			int mapIndex = ChunkIndex2d(0, lZ);
			for (int lX = 0; lX < 32; lX++)
			{
				ColumnResult columnResult = columnResults[mapIndex];
				int waterID = columnResult.WaterBlockID;
				surfaceWaterId = waterID;
				if (yBase < seaLevel && waterID != GlobalConfig.saltWaterBlockId && !columnResult.ColumnBlockSolidities[seaLevel - 1])
				{
					int unscaledTemp = (GameMath.BiLerpRgbColor((float)lX * (1f / 32f), (float)lZ * (1f / 32f), climateUpLeft, climateUpRight, climateBotLeft, climateBotRight) >> 16) & 0xFF;
					float distort = (float)distort2dx.Noise(chunkX * 32 + lX, worldZ) / 20f;
					if (Climate.GetScaledAdjustedTemperatureFloat(unscaledTemp, 0) + distort < (float)TerraGenConfig.WaterFreezingTempOnGen)
					{
						surfaceWaterId = GlobalConfig.lakeIceBlockId;
					}
				}
				terrainheightmap[mapIndex] = (ushort)(yBase - 1);
				rainheightmap[mapIndex] = (ushort)(yBase - 1);
				chunkBlockData = chunks[yBase / 32].Data;
				for (int posY = yBase; posY < yTop; posY++)
				{
					int lY = posY % 32;
					if (columnResult.ColumnBlockSolidities[posY])
					{
						terrainheightmap[mapIndex] = (ushort)posY;
						rainheightmap[mapIndex] = (ushort)posY;
						chunkBlockData[ChunkIndex3d(lX, lY, lZ)] = rockID;
					}
					else if (posY < seaLevel)
					{
						int blockId;
						if (posY == seaLevel - 1)
						{
							rainheightmap[mapIndex] = (ushort)posY;
							blockId = surfaceWaterId;
						}
						else
						{
							blockId = waterID;
						}
						chunkBlockData.SetFluid(ChunkIndex3d(lX, lY, lZ), blockId);
					}
					if (lY == 31)
					{
						chunkBlockData = chunks[(posY + 1) / 32].Data;
					}
				}
				mapIndex++;
			}
		}
		ushort ymax = 0;
		for (int i = 0; i < rainheightmap.Length; i++)
		{
			ymax = Math.Max(ymax, rainheightmap[i]);
		}
		chunks[0].MapChunk.YMax = ymax;
	}

	private LerpedWeightedIndex2DMap GetOrLoadLerpedLandformMap(IMapChunk mapchunk, int regionX, int regionZ)
	{
		LandformMapByRegion.TryGetValue(regionZ * regionMapSize + regionX, out var map);
		if (map != null)
		{
			return map;
		}
		IntDataMap2D lmap = mapchunk.MapRegion.LandformMap;
		return LandformMapByRegion[regionZ * regionMapSize + regionX] = new LerpedWeightedIndex2DMap(lmap.Data, lmap.Size, TerraGenConfig.landFormSmoothingRadius, lmap.TopLeftPadding, lmap.BottomRightPadding);
	}

	private void GetInterpolatedOctaves(float[] indices, out double[] amps, out double[] thresholds)
	{
		amps = new double[terrainGenOctaves];
		thresholds = new double[terrainGenOctaves];
		for (int octave = 0; octave < terrainGenOctaves; octave++)
		{
			double amplitude = 0.0;
			double threshold = 0.0;
			for (int i = 0; i < indices.Length; i++)
			{
				float weight = indices[i];
				if (weight != 0f)
				{
					LandformVariant j = landforms.LandFormsByIndex[i];
					amplitude += j.TerrainOctaves[octave] * (double)weight;
					threshold += j.TerrainOctaveThresholds[octave] * (double)weight;
				}
			}
			amps[octave] = amplitude;
			thresholds[octave] = threshold;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void StartSampleDisplacedYThreshold(float distortedPosY, int mapSizeYm2, out int yBase)
	{
		yBase = GameMath.Clamp((int)Math.Floor(distortedPosY), 0, mapSizeYm2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private float ContinueSampleDisplacedYThreshold(int yBase, float ySlide, float[] thresholds)
	{
		return GameMath.Lerp(thresholds[yBase], thresholds[yBase + 1], ySlide);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private float ComputeOceanAndUpheavalDistY(float upheavalStrength, double worldX, double worldZ, VectorXZ distGeo)
	{
		float upheavalNoiseValue = (float)geoUpheavalNoise.Noise((worldX + distGeo.X) / 400.0, (worldZ + distGeo.Z) / 400.0) * 0.9f;
		float upheavalMultiplier = Math.Min(0f, 0.5f - upheavalNoiseValue);
		return upheavalStrength * upheavalMultiplier;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ComputeGeoUpheavalTaper(double posY, double distY, double taperThreshold, double geoUpheavalAmplitude, double mapSizeY, ref double threshold)
	{
		if (posY > taperThreshold && distY < -2.0)
		{
			double upheavalAmount = GameMath.Clamp(0.0 - distY, posY - mapSizeY, posY);
			double ceilingDelta = posY - taperThreshold;
			threshold += ceilingDelta * upheavalAmount / (40.0 * geoUpheavalAmplitude);
		}
	}

	private VectorXZ NewDistortionNoise(double worldX, double worldZ)
	{
		double noiseX = worldX / 400.0;
		double noiseZ = worldZ / 400.0;
		SimplexNoise.NoiseFairWarpVector(distort2dx, distort2dz, noiseX, noiseZ, out var distX, out var distZ);
		VectorXZ result = default(VectorXZ);
		result.X = distX;
		result.Z = distZ;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private VectorXZ ApplyIsotropicDistortionThreshold(VectorXZ dist, double threshold, double maximum)
	{
		double distMagnitudeSquared = dist.X * dist.X + dist.Z * dist.Z;
		double thresholdSquared = threshold * threshold;
		if (distMagnitudeSquared <= thresholdSquared)
		{
			dist.X = (dist.Z = 0.0);
		}
		else
		{
			double num = (distMagnitudeSquared - thresholdSquared) / distMagnitudeSquared;
			double num2 = maximum * maximum;
			double baseCurveReciprocalAtMaximum = num2 / (num2 - thresholdSquared);
			double num3 = num * baseCurveReciprocalAtMaximum;
			double num4 = num3 * num3;
			double expectedOutputMaximum = maximum - threshold;
			double forceDown = num4 * (expectedOutputMaximum / maximum);
			dist *= forceDown;
		}
		return dist;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ChunkIndex3d(int x, int y, int z)
	{
		return (y * 32 + z) * 32 + x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int ChunkIndex2d(int x, int z)
	{
		return z * 32 + x;
	}
}
