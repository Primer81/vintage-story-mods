using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfNode
{
	[JsonProperty("mesh")]
	public long Mesh { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("rotation")]
	public double[] Rotation { get; set; }

	[JsonProperty("scale")]
	public double[] Scale { get; set; }

	[JsonProperty("translation")]
	public double[] Translation { get; set; }
}
