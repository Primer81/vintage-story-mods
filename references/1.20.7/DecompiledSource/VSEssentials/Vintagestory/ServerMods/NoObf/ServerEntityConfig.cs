using Newtonsoft.Json;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.ServerMods.NoObf;

[DocumentAsJson]
public class ServerEntityConfig
{
	[JsonProperty(ItemConverterType = typeof(JsonAttributesConverter))]
	public JsonObject[] Behaviors;

	[JsonProperty]
	[JsonConverter(typeof(JsonAttributesConverter))]
	public JsonObject Attributes;

	[JsonProperty]
	public SpawnConditions SpawnConditions;
}
