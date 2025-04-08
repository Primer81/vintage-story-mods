using Newtonsoft.Json;
using OpenTK.Graphics.OpenGL;

namespace Vintagestory.Client.NoObf;

public class GltfImageSampler
{
	[JsonProperty("magFilter")]
	public TextureMagFilter MagFilter { get; set; }

	[JsonProperty("minFilter")]
	public TextureMinFilter MinFilter { get; set; }

	[JsonProperty("wrapS")]
	public TextureWrapMode WrapS { get; set; }

	[JsonProperty("wrapT")]
	public TextureWrapMode WrapT { get; set; }
}
