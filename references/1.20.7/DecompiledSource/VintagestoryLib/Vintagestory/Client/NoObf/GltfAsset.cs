using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfAsset
{
	[JsonProperty("generator")]
	public string Generator { get; set; }

	[JsonProperty("version")]
	public string Version { get; set; }
}
