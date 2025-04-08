using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfPrimitive
{
	[JsonProperty("attributes")]
	public GltfAttributes Attributes { get; set; }

	[JsonProperty("indices")]
	public long? Indices { get; set; }

	[JsonProperty("material")]
	public long? Material { get; set; }
}
