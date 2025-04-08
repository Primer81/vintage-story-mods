using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods;

[JsonObject(MemberSerialization.OptIn)]
public class BlockLayerCodeByMin
{
	[JsonProperty]
	public AssetLocation BlockCode;

	[JsonProperty]
	public float MinTemp = -30f;

	[JsonProperty]
	public float MinRain;

	[JsonProperty]
	public float MinFertility;

	[JsonProperty]
	public float MaxFertility = 1f;

	[JsonProperty]
	public float MinY;

	[JsonProperty]
	public float MaxY = 1f;

	public int BlockId;

	public Dictionary<int, int> BlockIdMapping;
}
