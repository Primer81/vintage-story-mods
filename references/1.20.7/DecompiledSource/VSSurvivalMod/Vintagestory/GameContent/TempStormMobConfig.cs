using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class TempStormMobConfig
{
	public class SpawnsByStormStrength
	{
		[JsonProperty]
		public float QuantityMul;

		[JsonProperty]
		public Dictionary<string, AssetLocation[]> variantGroups;

		[JsonProperty]
		public Dictionary<string, float> variantQuantityMuls;

		public Dictionary<string, EntityProperties[]> resolvedVariantGroups;

		[JsonProperty]
		public Dictionary<string, TempStormSpawnPattern> spawnPatterns;
	}

	public class TempStormSpawnPattern
	{
		public float Weight;

		public Dictionary<string, float> GroupWeights;
	}

	public class RareStormSpawns
	{
		public RareStormSpawnsVariant[] Variants;
	}

	public class RareStormSpawnsVariant
	{
		public AssetLocation Code;

		public string GroupCode;

		public float ChancePerStorm;

		public EntityProperties ResolvedCode;
	}

	[JsonProperty]
	public SpawnsByStormStrength spawnsByStormStrength;

	[JsonProperty]
	public RareStormSpawns rareSpawns;
}
