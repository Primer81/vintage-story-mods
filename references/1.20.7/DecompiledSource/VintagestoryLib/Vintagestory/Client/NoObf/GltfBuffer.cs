using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfBuffer
{
	[JsonProperty("byteLength")]
	public long ByteLength { get; set; }

	[JsonProperty("uri")]
	public string Uri { get; set; }
}
