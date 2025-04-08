using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfTextureElement
{
	[JsonProperty("source")]
	public long Source { get; set; }

	[JsonProperty("sampler")]
	public long Sampler { get; set; }
}
