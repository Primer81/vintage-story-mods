using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfAnimationChannel
{
	[JsonProperty("name")]
	public long Sampler { get; set; }

	[JsonProperty("target")]
	public GltfAnimationChannelTarget Target { get; set; }
}
