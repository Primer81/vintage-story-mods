using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods;

public class GenDeposits : GenPartial
{
	public DepositVariant[] Deposits;

	public int depositChunkRange = 3;

	private int regionSize;

	private float chanceMultiplier;

	private IBlockAccessor blockAccessor;

	public LCGRandom depositRand;

	private BlockPos tmpPos = new BlockPos();

	private NormalizedSimplexNoise depositShapeDistortNoise;

	private Dictionary<BlockPos, DepositVariant> subDepositsToPlace = new Dictionary<BlockPos, DepositVariant>();

	private MapLayerBase verticalDistortTop;

	private MapLayerBase verticalDistortBottom;

	public bool addHandbookAttributes = true;

	protected override int chunkRange => depositChunkRange;

	public override double ExecuteOrder()
	{
		return 0.2;
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	internal void setApi(ICoreServerAPI api)
	{
		base.api = api;
		blockAccessor = api.World.BlockAccessor;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.ChunkColumnGeneration(GenChunkColumn, EnumWorldGenPass.TerrainFeatures, "standard");
			api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
			api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
		}
	}

	public override void AssetsFinalize(ICoreAPI api)
	{
		initAssets(api as ICoreServerAPI, blockCallbacks: true);
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		blockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: true);
	}

	public void reloadWorldGen()
	{
		initAssets(api, blockCallbacks: true);
		initWorldGen();
	}

	public void initAssets(ICoreServerAPI api, bool blockCallbacks)
	{
		chanceMultiplier = api.Assets.Get("worldgen/deposits.json").ToObject<Deposits>().ChanceMultiplier;
		IOrderedEnumerable<KeyValuePair<AssetLocation, DepositVariant[]>> orderedEnumerable = from d in api.Assets.GetMany<DepositVariant[]>(api.World.Logger, "worldgen/deposits/")
			orderby d.Key.ToString()
			select d;
		List<DepositVariant> variants = new List<DepositVariant>();
		foreach (KeyValuePair<AssetLocation, DepositVariant[]> val in orderedEnumerable)
		{
			DepositVariant[] value = val.Value;
			foreach (DepositVariant depo in value)
			{
				depo.fromFile = val.Key.ToString();
				depo.WithBlockCallback &= blockCallbacks;
				variants.Add(depo);
				if (depo.ChildDeposits != null)
				{
					DepositVariant[] childDeposits = depo.ChildDeposits;
					foreach (DepositVariant obj in childDeposits)
					{
						obj.fromFile = val.Key.ToString();
						obj.parentDeposit = depo;
						obj.WithBlockCallback &= blockCallbacks;
					}
				}
			}
		}
		Deposits = variants.ToArray();
		depositShapeDistortNoise = NormalizedSimplexNoise.FromDefaultOctaves(3, 0.10000000149011612, 0.8999999761581421, 1L);
		regionSize = api.WorldManager.RegionSize;
		depositRand = new LCGRandom(api.WorldManager.Seed + 34613);
		for (int i = 0; i < Deposits.Length; i++)
		{
			DepositVariant obj2 = Deposits[i];
			obj2.addHandbookAttributes = addHandbookAttributes;
			obj2.Init(api, depositRand, depositShapeDistortNoise);
		}
	}

	public override void initWorldGen()
	{
		base.initWorldGen();
		int seed = api.WorldManager.Seed;
		Dictionary<string, MapLayerBase> maplayersByCode = new Dictionary<string, MapLayerBase>();
		for (int i = 0; i < Deposits.Length; i++)
		{
			DepositVariant variant = Deposits[i];
			if (variant.WithOreMap)
			{
				variant.OreMapLayer = getOrCreateMapLayer(seed, variant.Code, maplayersByCode, variant.OreMapScale, variant.OreMapContrast, variant.OreMapSub);
			}
			if (variant.ChildDeposits == null)
			{
				continue;
			}
			for (int j = 0; j < variant.ChildDeposits.Length; j++)
			{
				DepositVariant childVariant = variant.ChildDeposits[j];
				if (childVariant.WithOreMap)
				{
					childVariant.OreMapLayer = getOrCreateMapLayer(seed, childVariant.Code, maplayersByCode, variant.OreMapScale, variant.OreMapContrast, variant.OreMapSub);
				}
			}
		}
		verticalDistortBottom = GenMaps.GetDepositVerticalDistort(seed + 12);
		verticalDistortTop = GenMaps.GetDepositVerticalDistort(seed + 28);
		api.Logger?.VerboseDebug("Initialised GenDeposits");
	}

	private MapLayerBase getOrCreateMapLayer(int seed, string oremapCode, Dictionary<string, MapLayerBase> maplayersByCode, float scaleMul, float contrastMul, float sub)
	{
		if (!maplayersByCode.TryGetValue(oremapCode, out var ml))
		{
			NoiseOre noiseOre = new NoiseOre(seed + oremapCode.GetNonRandomizedHashCode());
			ml = (maplayersByCode[oremapCode] = GenMaps.GetOreMap(seed + oremapCode.GetNonRandomizedHashCode() + 1, noiseOre, scaleMul, contrastMul, sub));
		}
		return ml;
	}

	public void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ, ITreeAttribute chunkGenParams = null)
	{
		int pad = 2;
		TerraGenConfig.depositVerticalDistortScale = 2;
		int noiseSize = api.WorldManager.RegionSize / TerraGenConfig.depositVerticalDistortScale;
		IntDataMap2D oreMapVerticalDistortBottom = mapRegion.OreMapVerticalDistortBottom;
		oreMapVerticalDistortBottom.Size = noiseSize + 2 * pad;
		oreMapVerticalDistortBottom.BottomRightPadding = (oreMapVerticalDistortBottom.TopLeftPadding = pad);
		oreMapVerticalDistortBottom.Data = verticalDistortBottom.GenLayer(regionX * noiseSize - pad, regionZ * noiseSize - pad, noiseSize + 2 * pad, noiseSize + 2 * pad);
		IntDataMap2D oreMapVerticalDistortTop = mapRegion.OreMapVerticalDistortTop;
		oreMapVerticalDistortTop.Size = noiseSize + 2 * pad;
		oreMapVerticalDistortTop.BottomRightPadding = (oreMapVerticalDistortTop.TopLeftPadding = pad);
		oreMapVerticalDistortTop.Data = verticalDistortTop.GenLayer(regionX * noiseSize - pad, regionZ * noiseSize - pad, noiseSize + 2 * pad, noiseSize + 2 * pad);
		for (int i = 0; i < Deposits.Length; i++)
		{
			Deposits[i].OnMapRegionGen(mapRegion, regionX, regionZ);
		}
	}

	protected override void GenChunkColumn(IChunkColumnGenerateRequest request)
	{
		if (blockAccessor is IWorldGenBlockAccessor wgba)
		{
			wgba.BeginColumn();
		}
		base.GenChunkColumn(request);
	}

	public override void GeneratePartial(IServerChunk[] chunks, int chunkX, int chunkZ, int chunkdX, int chunkdZ)
	{
		LCGRandom chunkRand = base.chunkRand;
		int fromChunkx = chunkX + chunkdX;
		int fromChunkz = chunkZ + chunkdZ;
		int fromBaseX = fromChunkx * 32;
		int fromBaseZ = fromChunkz * 32;
		subDepositsToPlace.Clear();
		float scaleAdjustMul = (float)api.WorldManager.MapSizeY / 256f;
		for (int i = 0; i < Deposits.Length; i++)
		{
			DepositVariant variant = Deposits[i];
			float quantityFactor = (variant.WithOreMap ? variant.GetOreMapFactor(fromChunkx, fromChunkz) : 1f);
			float qModified = variant.TriesPerChunk * quantityFactor * chanceMultiplier * (variant.ScaleWithWorldheight ? scaleAdjustMul : 1f);
			int quantity = (int)qModified;
			quantity += (((float)chunkRand.NextInt(100) < 100f * (qModified - (float)quantity)) ? 1 : 0);
			while (quantity-- > 0)
			{
				tmpPos.Set(fromBaseX + chunkRand.NextInt(32), -99, fromBaseZ + chunkRand.NextInt(32));
				long crseed = chunkRand.NextInt(10000000);
				depositRand.SetWorldSeed(crseed);
				depositRand.InitPositionSeed(fromChunkx, fromChunkz);
				GenDeposit(chunks, chunkX, chunkZ, tmpPos, variant);
			}
		}
		foreach (KeyValuePair<BlockPos, DepositVariant> val in subDepositsToPlace)
		{
			depositRand.SetWorldSeed(chunkRand.NextInt(10000000));
			depositRand.InitPositionSeed(fromChunkx, fromChunkz);
			val.Value.GeneratorInst.GenDeposit(blockAccessor, chunks, chunkX, chunkZ, val.Key, ref subDepositsToPlace);
		}
	}

	public virtual void GenDeposit(IServerChunk[] chunks, int chunkX, int chunkZ, BlockPos depoCenterPos, DepositVariant variant)
	{
		int lx = GameMath.Mod(depoCenterPos.X, 32);
		int lz = GameMath.Mod(depoCenterPos.Z, 32);
		if (variant.Climate != null)
		{
			IMapChunk originMapchunk = api.WorldManager.GetMapChunk(depoCenterPos.X / 32, depoCenterPos.Z / 32);
			if (originMapchunk == null)
			{
				return;
			}
			depoCenterPos.Y = originMapchunk.RainHeightMap[lz * 32 + lx];
			IntDataMap2D climateMap = blockAccessor.GetMapRegion(depoCenterPos.X / regionSize, depoCenterPos.Z / regionSize)?.ClimateMap;
			if (climateMap == null)
			{
				return;
			}
			float normXInRegionClimate = (float)((double)depoCenterPos.X / (double)regionSize % 1.0);
			float normZInRegionClimate = (float)((double)depoCenterPos.Z / (double)regionSize % 1.0);
			int climate = climateMap.GetUnpaddedColorLerpedForNormalizedPos(normXInRegionClimate, normZInRegionClimate);
			float rainRel = (float)Climate.GetRainFall((climate >> 8) & 0xFF, depoCenterPos.Y) / 255f;
			if (rainRel < variant.Climate.MinRain || rainRel > variant.Climate.MaxRain)
			{
				return;
			}
			float temp = Climate.GetScaledAdjustedTemperatureFloat((climate >> 16) & 0xFF, depoCenterPos.Y - TerraGenConfig.seaLevel);
			if (temp < variant.Climate.MinTemp || temp > variant.Climate.MaxTemp)
			{
				return;
			}
			double seaLevel = TerraGenConfig.seaLevel;
			double yRel = (((double)depoCenterPos.Y > seaLevel) ? (1.0 + ((double)depoCenterPos.Y - seaLevel) / ((double)api.World.BlockAccessor.MapSizeY - seaLevel)) : ((double)depoCenterPos.Y / seaLevel));
			if (yRel < (double)variant.Climate.MinY || yRel > (double)variant.Climate.MaxY)
			{
				return;
			}
		}
		variant.GeneratorInst?.GenDeposit(blockAccessor, chunks, chunkX, chunkZ, depoCenterPos, ref subDepositsToPlace);
	}
}
