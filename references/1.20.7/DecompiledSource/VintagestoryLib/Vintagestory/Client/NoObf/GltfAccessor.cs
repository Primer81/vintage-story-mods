using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL;

namespace Vintagestory.Client.NoObf;

public class GltfAccessor
{
	[JsonProperty("bufferView")]
	public long BufferView { get; set; }

	[JsonProperty("componentType")]
	public VertexAttribPointerType ComponentType { get; set; }

	[JsonProperty("count")]
	public int Count { get; set; }

	[JsonProperty("max", NullValueHandling = NullValueHandling.Ignore)]
	public double[] Max { get; set; }

	[JsonProperty("min", NullValueHandling = NullValueHandling.Ignore)]
	public double[] Min { get; set; }

	[JsonProperty("type")]
	public EnumGltfAccessorType Type { get; set; }
}
