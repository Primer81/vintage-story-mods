using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public class GenRockStrataNew : ModStdWorldGen
{
	private ICoreServerAPI api;

	private int regionSize;

	private int regionChunkSize;

	public int rockBlockId;

	internal RockStrataConfig strata;

	internal SimplexNoise distort2dx;

	internal SimplexNoise distort2dz;

	internal MapLayerCustomPerlin[] strataNoises;

	private int regionMapSize;

	private Dictionary<int, LerpedWeightedIndex2DMap> ProvinceMapByRegion = new Dictionary<int, LerpedWeightedIndex2DMap>(10);

	private float[] rockGroupMaxThickness = new float[4];

	private int[] rockGroupCurrentThickness = new int[4];

	private IMapChunk mapChunk;

	private ushort[] heightMap;

	private int rdx;

	private int rdz;

	private LerpedWeightedIndex2DMap map;

	private float lerpMapInv;

	private float chunkInRegionX;

	private float chunkInRegionZ;

	private GeologicProvinces provinces = NoiseGeoProvince.provinces;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override double ExecuteOrder()
	{
		return 0.1;
	}

	internal void setApi(ICoreServerAPI api)
	{
		this.api = api;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		api.Event.InitWorldGenerator(initWorldGen, "standard");
		api.Event.ChunkColumnGeneration(GenChunkColumn, EnumWorldGenPass.Terrain, "standard");
		api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
	}

	public void initWorldGen()
	{
		initWorldGen(0);
	}

	public void initWorldGen(int seedDiff)
	{
		IAsset asset = api.Assets.Get("worldgen/rockstrata.json");
		strata = asset.ToObject<RockStrataConfig>();
		for (int j = 0; j < strata.Variants.Length; j++)
		{
			strata.Variants[j].Init(api.World);
		}
		LoadGlobalConfig(api);
		regionSize = api.WorldManager.RegionSize;
		regionChunkSize = regionSize / 32;
		int geoProvRegionNoiseSize = regionSize / TerraGenConfig.geoProvMapScale;
		regionMapSize = api.WorldManager.MapSizeX / (32 * geoProvRegionNoiseSize);
		rockBlockId = (ushort)api.WorldManager.GetBlockId(new AssetLocation("rock-granite"));
		distort2dx = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 9876 + seedDiff);
		distort2dz = new SimplexNoise(new double[4] { 14.0, 9.0, 6.0, 3.0 }, new double[4] { 0.01, 0.02, 0.04, 0.08 }, api.World.SeaLevel + 9877 + seedDiff);
		strataNoises = new MapLayerCustomPerlin[strata.Variants.Length];
		for (int i = 0; i < strataNoises.Length; i++)
		{
			RockStratum obj = strata.Variants[i];
			double[] ampls = (double[])obj.Amplitudes.Clone();
			double[] freq = (double[])obj.Frequencies.Clone();
			double[] th = (double[])obj.Thresholds.Clone();
			if (ampls.Length != freq.Length || ampls.Length != th.Length)
			{
				throw new ArgumentException($"Bug in rockstrata.json, variant {i}: The list of amplitudes ({ampls.Length} elements), frequencies ({freq.Length} elements) and thresholds ({th.Length} elements) are not of the same length!");
			}
			for (int k = 0; k < freq.Length; k++)
			{
				freq[k] /= TerraGenConfig.rockStrataOctaveScale;
				ampls[k] *= api.WorldManager.MapSizeY;
				th[k] *= api.WorldManager.MapSizeY;
			}
			strataNoises[i] = new MapLayerCustomPerlin(api.World.Seed + 23423 + i + seedDiff, ampls, freq, th);
		}
		api.Logger.VerboseDebug("Initialised GenRockStrata");
	}

	private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams = null)
	{
		int noiseSize = api.WorldManager.RegionSize / TerraGenConfig.rockStrataScale;
		int pad = 2;
		mapRegion.RockStrata = new IntDataMap2D[strata.Variants.Length];
		for (int i = 0; i < strata.Variants.Length; i++)
		{
			IntDataMap2D intmap = new IntDataMap2D();
			mapRegion.RockStrata[i] = intmap;
			intmap.Data = strataNoises[i].GenLayer(regionX * noiseSize - pad, regionZ * noiseSize - pad, noiseSize + 2 * pad, noiseSize + 2 * pad);
			intmap.Size = noiseSize + 2 * pad;
			intmap.TopLeftPadding = (intmap.BottomRightPadding = pad);
		}
	}

	internal void GenChunkColumn(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		preLoad(chunks, chunkX, chunkZ);
		for (int x = 0; x < 32; x++)
		{
			for (int z = 0; z < 32; z++)
			{
				genBlockColumn(chunks, chunkX, chunkZ, x, z);
			}
		}
	}

	public void preLoad(IServerChunk[] chunks, int chunkX, int chunkZ)
	{
		mapChunk = chunks[0].MapChunk;
		heightMap = mapChunk.WorldGenTerrainHeightMap;
		rdx = chunkX % regionChunkSize;
		rdz = chunkZ % regionChunkSize;
		map = GetOrLoadLerpedProvinceMap(chunks[0].MapChunk, chunkX, chunkZ);
		lerpMapInv = 1f / (float)TerraGenConfig.geoProvMapScale;
		chunkInRegionX = (float)(chunkX % regionChunkSize) * lerpMapInv * 32f;
		chunkInRegionZ = (float)(chunkZ % regionChunkSize) * lerpMapInv * 32f;
		provinces = NoiseGeoProvince.provinces;
	}

	public void genBlockColumn(IServerChunk[] chunks, int chunkX, int chunkZ, int lx, int lz)
	{
		ushort num = heightMap[lz * 32 + lx];
		int ylower = 1;
		int yupper = num;
		int rockBlockId = this.rockBlockId;
		rockGroupMaxThickness[0] = (rockGroupMaxThickness[1] = (rockGroupMaxThickness[2] = (rockGroupMaxThickness[3] = 0f)));
		rockGroupCurrentThickness[0] = (rockGroupCurrentThickness[1] = (rockGroupCurrentThickness[2] = (rockGroupCurrentThickness[3] = 0)));
		float[] indices = new float[provinces.Variants.Length];
		map.WeightsAt(chunkInRegionX + (float)lx * lerpMapInv, chunkInRegionZ + (float)lz * lerpMapInv, indices);
		for (int i = 0; i < indices.Length; i++)
		{
			float w = indices[i];
			if (w != 0f)
			{
				GeologicProvinceRockStrata[] localstrata = provinces.Variants[i].RockStrataIndexed;
				rockGroupMaxThickness[0] += localstrata[0].ScaledMaxThickness * w;
				rockGroupMaxThickness[1] += localstrata[1].ScaledMaxThickness * w;
				rockGroupMaxThickness[2] += localstrata[2].ScaledMaxThickness * w;
				rockGroupMaxThickness[3] += localstrata[3].ScaledMaxThickness * w;
			}
		}
		float distx = (float)distort2dx.Noise(chunkX * 32 + lx, chunkZ * 32 + lz);
		float distz = (float)distort2dz.Noise(chunkX * 32 + lx, chunkZ * 32 + lz);
		float thicknessDistort = GameMath.Clamp((distx + distz) / 30f, 0.9f, 1.1f);
		int rockStrataId = -1;
		RockStratum stratum = null;
		int grp = 0;
		float strataThickness = 0f;
		while (ylower <= yupper)
		{
			if ((strataThickness -= 1f) <= 0f)
			{
				rockStrataId++;
				if (rockStrataId >= strata.Variants.Length || rockStrataId >= mapChunk.MapRegion.RockStrata.Length)
				{
					break;
				}
				stratum = strata.Variants[rockStrataId];
				IntDataMap2D rockMap = mapChunk.MapRegion.RockStrata[rockStrataId];
				float step = (float)rockMap.InnerSize / (float)regionChunkSize;
				grp = (int)stratum.RockGroup;
				float val = rockGroupMaxThickness[grp] * thicknessDistort - (float)rockGroupCurrentThickness[grp];
				float nx = (float)rdx * step + step * ((float)lx + distx) / 32f;
				float nz = (float)rdz * step + step * ((float)lz + distz) / 32f;
				nx = Math.Max(nx, -1.499f);
				nz = Math.Max(nz, -1.499f);
				strataThickness = Math.Min(val, rockMap.GetIntLerpedCorrectly(nx, nz));
				if (stratum.RockGroup == EnumRockGroup.Sedimentary)
				{
					strataThickness -= (float)Math.Max(0, yupper - TerraGenConfig.seaLevel) * 0.5f;
				}
				if (strataThickness < 2f)
				{
					strataThickness = -1f;
					continue;
				}
				if (stratum.BlockId == rockBlockId)
				{
					int thickness = (int)strataThickness;
					rockGroupCurrentThickness[grp] += thickness;
					if (stratum.GenDir == EnumStratumGenDir.BottomUp)
					{
						ylower += thickness;
					}
					else
					{
						yupper -= thickness;
					}
					continue;
				}
			}
			rockGroupCurrentThickness[grp]++;
			if (stratum.GenDir == EnumStratumGenDir.BottomUp)
			{
				int chunkY = ylower / 32;
				int lY = ylower - chunkY * 32;
				int localIndex3D = (32 * lY + lz) * 32 + lx;
				IChunkBlocks chunkBlockData = chunks[chunkY].Data;
				if (chunkBlockData.GetBlockIdUnsafe(localIndex3D) == rockBlockId)
				{
					chunkBlockData.SetBlockUnsafe(localIndex3D, stratum.BlockId);
				}
				ylower++;
			}
			else
			{
				int chunkY2 = yupper / 32;
				int lY2 = yupper - chunkY2 * 32;
				int localIndex3D2 = (32 * lY2 + lz) * 32 + lx;
				IChunkBlocks chunkBlockData2 = chunks[chunkY2].Data;
				if (chunkBlockData2.GetBlockIdUnsafe(localIndex3D2) == rockBlockId)
				{
					chunkBlockData2.SetBlockUnsafe(localIndex3D2, stratum.BlockId);
				}
				yupper--;
			}
		}
	}

	private LerpedWeightedIndex2DMap GetOrLoadLerpedProvinceMap(IMapChunk mapchunk, int chunkX, int chunkZ)
	{
		int index2d = chunkZ / regionChunkSize * regionMapSize + chunkX / regionChunkSize;
		ProvinceMapByRegion.TryGetValue(index2d, out var map);
		if (map != null)
		{
			return map;
		}
		return CreateLerpedProvinceMap(mapchunk.MapRegion.GeologicProvinceMap, chunkX / regionChunkSize, chunkZ / regionChunkSize);
	}

	private LerpedWeightedIndex2DMap CreateLerpedProvinceMap(IntDataMap2D geoMap, int regionX, int regionZ)
	{
		int index2d = regionZ * regionMapSize + regionX;
		return ProvinceMapByRegion[index2d] = new LerpedWeightedIndex2DMap(geoMap.Data, geoMap.Size, TerraGenConfig.geoProvSmoothingRadius, geoMap.TopLeftPadding, geoMap.BottomRightPadding);
	}
}
