using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfAnimationChannelTarget
{
	[JsonProperty("node")]
	public long Node { get; set; }

	[JsonProperty("path")]
	public EnumGltfAnimationChannelTargetPath Path { get; set; }
}
