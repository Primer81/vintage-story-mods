using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.NoObf;

public class GameServerConfigResponse : ServerCtrlResponse
{
	public ServerConfigPart ServerConfig;

	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject WorldConfig;

	public string[] Mods;
}
