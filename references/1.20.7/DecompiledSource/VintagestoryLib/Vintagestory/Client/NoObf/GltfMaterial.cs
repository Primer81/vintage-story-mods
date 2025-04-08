using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfMaterial
{
	[JsonProperty("doubleSided")]
	public bool DoubleSided { get; set; }

	[JsonProperty("emissiveFactor")]
	public float[] EmissiveFactor { get; set; }

	[JsonProperty("name")]
	public string Name { get; set; }

	[JsonProperty("normalTexture")]
	public GltfMatTexture NormalTexture { get; set; }

	[JsonProperty("pbrMetallicRoughness")]
	public GltfPbrMetallicRoughness PbrMetallicRoughness { get; set; }
}
