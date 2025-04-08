using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfImage
{
	[JsonProperty("bufferView")]
	public long BufferView { get; set; }

	[JsonProperty("mimeType")]
	public string MimeType { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; }
}
