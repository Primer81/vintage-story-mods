using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

[JsonObject(MemberSerialization.OptIn)]
public class DepositVariant : WorldPropertyVariant
{
	public string fromFile;

	[JsonProperty]
	public new string Code;

	[JsonProperty]
	public float TriesPerChunk;

	[JsonProperty]
	public string Generator;

	[JsonProperty]
	public bool WithOreMap;

	[JsonProperty]
	public float OreMapScale = 1f;

	[JsonProperty]
	public float OreMapContrast = 1f;

	[JsonProperty]
	public float OreMapSub;

	[JsonProperty]
	public string HandbookPageCode;

	[JsonProperty]
	public bool WithBlockCallback;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject Attributes;

	[JsonProperty]
	public ClimateConditions Climate;

	[JsonProperty]
	public DepositVariant[] ChildDeposits;

	[JsonProperty]
	public bool ScaleWithWorldheight = true;

	public DepositGeneratorBase GeneratorInst;

	public MapLayerBase OreMapLayer;

	private int noiseSizeOre;

	private int regionSize;

	private const int chunksize = 32;

	private ICoreServerAPI api;

	internal DepositVariant parentDeposit;

	public bool addHandbookAttributes;

	public void InitWithoutGenerator(ICoreServerAPI api)
	{
		this.api = api;
		regionSize = api.WorldManager.RegionSize;
		noiseSizeOre = regionSize / TerraGenConfig.oreMapScale;
	}

	public void Init(ICoreServerAPI api, LCGRandom depositRand, NormalizedSimplexNoise noiseGen)
	{
		this.api = api;
		InitWithoutGenerator(api);
		if (Generator == null)
		{
			api.World.Logger.Error("Error in deposit variant in file {0}: No generator defined! Must define a generator.", fromFile, Generator);
		}
		else
		{
			GeneratorInst = DepositGeneratorRegistry.CreateGenerator(Generator, Attributes, api, this, depositRand, noiseGen);
			if (GeneratorInst == null)
			{
				api.World.Logger.Error("Error in deposit variant in file {0}: No generator with code '{1}' found!", fromFile, Generator);
			}
		}
		if (Code == null)
		{
			api.World.Logger.Error("Error in deposit variant in file {0}: Deposit has no code! Defaulting to 'unknown'", fromFile);
			Code = "unknown";
		}
	}

	public void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ)
	{
		if (OreMapLayer != null && !mapRegion.OreMaps.ContainsKey(Code))
		{
			IntDataMap2D map = new IntDataMap2D();
			map.Size = noiseSizeOre + 1;
			map.BottomRightPadding = 1;
			map.Data = OreMapLayer.GenLayer(regionX * noiseSizeOre, regionZ * noiseSizeOre, noiseSizeOre + 1, noiseSizeOre + 1);
			mapRegion.OreMaps[Code] = map;
		}
		if (ChildDeposits == null)
		{
			return;
		}
		for (int i = 0; i < ChildDeposits.Length; i++)
		{
			DepositVariant childVariant = ChildDeposits[i];
			if (childVariant.OreMapLayer != null && !mapRegion.OreMaps.ContainsKey(childVariant.Code))
			{
				IntDataMap2D map = new IntDataMap2D();
				map.Size = noiseSizeOre + 1;
				map.BottomRightPadding = 1;
				map.Data = childVariant.OreMapLayer.GenLayer(regionX * noiseSizeOre, regionZ * noiseSizeOre, noiseSizeOre + 1, noiseSizeOre + 1);
				mapRegion.OreMaps[childVariant.Code] = map;
			}
		}
	}

	public float GetOreMapFactor(int chunkx, int chunkz)
	{
		IMapRegion originMapRegion = api?.WorldManager.GetMapRegion(chunkx * 32 / regionSize, chunkz * 32 / regionSize);
		if (originMapRegion == null)
		{
			return 0f;
		}
		int lx = (chunkx * 32 + 16) % regionSize;
		int lz = (chunkz * 32 + 16) % regionSize;
		originMapRegion.OreMaps.TryGetValue(Code, out var map);
		if (map != null)
		{
			float posXInRegionOre = GameMath.Clamp((float)lx / (float)regionSize * (float)noiseSizeOre, 0f, noiseSizeOre - 1);
			float posZInRegionOre = GameMath.Clamp((float)lz / (float)regionSize * (float)noiseSizeOre, 0f, noiseSizeOre - 1);
			return (float)(map.GetUnpaddedColorLerped(posXInRegionOre, posZInRegionOre) & 0xFF) / 255f;
		}
		return 0f;
	}

	public DepositVariant Clone()
	{
		DepositVariant var = new DepositVariant
		{
			fromFile = fromFile,
			Code = Code,
			TriesPerChunk = TriesPerChunk,
			Generator = Generator,
			WithOreMap = WithOreMap,
			WithBlockCallback = WithBlockCallback,
			Attributes = Attributes?.Clone(),
			Climate = Climate?.Clone(),
			ChildDeposits = ((ChildDeposits == null) ? null : ((DepositVariant[])ChildDeposits.Clone())),
			OreMapLayer = OreMapLayer,
			ScaleWithWorldheight = ScaleWithWorldheight
		};
		DepositVariant[] childDeposits = ChildDeposits;
		for (int i = 0; i < childDeposits.Length; i++)
		{
			childDeposits[i].parentDeposit = var;
		}
		var.GeneratorInst = DepositGeneratorRegistry.CreateGenerator(Generator, Attributes, api, var, GeneratorInst.DepositRand, GeneratorInst.DistortNoiseGen);
		return var;
	}

	public virtual void GetPropickReading(BlockPos pos, int oreDist, int[] blockColumn, out double ppt, out double totalFactor)
	{
		GeneratorInst.GetPropickReading(pos, oreDist, blockColumn, out ppt, out totalFactor);
	}
}
