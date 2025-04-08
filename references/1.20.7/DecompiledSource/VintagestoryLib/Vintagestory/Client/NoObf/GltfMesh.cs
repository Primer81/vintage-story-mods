using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfMesh
{
	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("primitives")]
	public GltfPrimitive[] Primitives { get; set; }
}
