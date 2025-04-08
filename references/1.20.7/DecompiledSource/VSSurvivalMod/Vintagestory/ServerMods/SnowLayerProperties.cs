using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods;

[JsonObject(MemberSerialization.OptIn)]
public class SnowLayerProperties
{
	[JsonProperty]
	public int MaxTemp;

	[JsonProperty]
	public int TransitionSize;

	[JsonProperty]
	public AssetLocation BlockCode;

	public int BlockId;
}
