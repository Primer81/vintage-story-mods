using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfAnimationSampler
{
	[JsonProperty("input")]
	public long Input { get; set; }

	[JsonProperty("interpolation")]
	public EnumGltfAnimationSamplerInterpolation Interpolation { get; set; }

	[JsonProperty("output")]
	public long Output { get; set; }
}
