using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfScene
{
	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("nodes")]
	public long[] Nodes { get; set; }
}
