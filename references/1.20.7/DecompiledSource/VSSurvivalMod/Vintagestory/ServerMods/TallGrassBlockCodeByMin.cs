using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods;

[JsonObject(MemberSerialization.OptIn)]
public class TallGrassBlockCodeByMin
{
	[JsonProperty]
	public int MinTemp;

	[JsonProperty]
	public float MinRain;

	[JsonProperty]
	public float MaxForest;

	[JsonProperty]
	public AssetLocation BlockCode;

	public int BlockId;
}
