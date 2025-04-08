using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfAnimation
{
	[JsonProperty("channels")]
	public GltfAnimationChannel[] Channels { get; set; }

	[JsonProperty("name")]
	private string Name { get; set; }

	[JsonProperty("samplers")]
	public GltfAnimationChannel[] Samplers { get; set; }
}
