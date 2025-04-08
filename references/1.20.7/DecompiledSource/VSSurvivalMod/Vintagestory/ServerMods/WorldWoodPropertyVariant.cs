using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods;

[JsonObject(MemberSerialization.OptIn)]
public class WorldWoodPropertyVariant
{
	[JsonProperty]
	public AssetLocation Code;

	[JsonProperty]
	public EnumTreeType TreeType;
}
