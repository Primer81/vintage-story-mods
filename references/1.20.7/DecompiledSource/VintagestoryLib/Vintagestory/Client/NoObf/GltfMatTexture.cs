using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfMatTexture
{
	[JsonProperty("index")]
	public long Index { get; set; }

	[JsonProperty("texCoord")]
	public long TexCoord { get; set; }
}
