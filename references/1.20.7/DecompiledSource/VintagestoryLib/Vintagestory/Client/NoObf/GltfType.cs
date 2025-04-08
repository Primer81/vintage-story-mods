using Newtonsoft.Json;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

[JsonObject(MemberSerialization.OptIn)]
public class GltfType
{
	public TextureAtlasPosition[] BaseTextures { get; set; }

	public TextureAtlasPosition[] PBRTextures { get; set; }

	public TextureAtlasPosition[] NormalTextures { get; set; }

	[JsonProperty("asset")]
	public GltfAsset Asset { get; set; }

	[JsonProperty("animations")]
	public GltfAnimation[] Animations { get; set; }

	[JsonProperty("scene")]
	public long Scene { get; set; }

	[JsonProperty("scenes")]
	public GltfScene[] Scenes { get; set; }

	[JsonProperty("nodes")]
	public GltfNode[] Nodes { get; set; }

	[JsonProperty("materials")]
	public GltfMaterial[] Materials { get; set; }

	[JsonProperty("meshes")]
	public GltfMesh[] Meshes { get; set; }

	[JsonProperty("textures")]
	public GltfTextureElement[] Textures { get; set; }

	[JsonProperty("images")]
	public GltfImage[] Images { get; set; }

	[JsonProperty("accessors")]
	public GltfAccessor[] Accessors { get; set; }

	[JsonProperty("bufferViews")]
	public GltfBufferView[] BufferViews { get; set; }

	[JsonProperty("samplers")]
	public GltfImageSampler[] Samplers { get; set; }

	[JsonProperty("buffers")]
	public GltfBuffer[] Buffers { get; set; }
}
