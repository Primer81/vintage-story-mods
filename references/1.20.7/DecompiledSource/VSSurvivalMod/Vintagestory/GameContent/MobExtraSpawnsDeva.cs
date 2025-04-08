using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class MobExtraSpawnsDeva
{
	public class DevaAreaMobConfig
	{
		public Dictionary<string, float> Quantities;

		public Dictionary<string, AssetLocation[]> VariantGroups;

		public Dictionary<string, EntityProperties[]> ResolvedVariantGroups;
	}

	public DevaAreaMobConfig devastationAreaSpawns;
}
