using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfBufferView
{
	[JsonProperty("buffer")]
	public long Buffer { get; set; }

	[JsonProperty("byteLength")]
	public long ByteLength { get; set; }

	[JsonProperty("byteOffset")]
	public long ByteOffset { get; set; }
}
