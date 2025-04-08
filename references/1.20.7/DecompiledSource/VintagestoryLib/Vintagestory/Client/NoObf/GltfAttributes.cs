using Newtonsoft.Json;

namespace Vintagestory.Client.NoObf;

public class GltfAttributes
{
	[JsonProperty("POSITION")]
	public long? Position { get; set; }

	[JsonProperty("NORMAL")]
	public long? Normal { get; set; }

	[JsonProperty("TANGENT")]
	public long? Tangent { get; set; }

	[JsonProperty("TEXCOORD_0")]
	public long? Texcoord0 { get; set; }

	[JsonProperty("TEXCOORD_1")]
	public long? Texcoord1 { get; set; }

	[JsonProperty("COLOR_0")]
	public long? VertexColor { get; set; }

	[JsonProperty("COLOR_1")]
	public long? GlowLevel { get; set; }

	[JsonProperty("COLOR_2")]
	public long? Reflective { get; set; }

	[JsonProperty("COLOR_3")]
	public long? BMWindLeaves { get; set; }

	[JsonProperty("COLOR_4")]
	public long? BMWindLeavesWeakBend { get; set; }

	[JsonProperty("COLOR_5")]
	public long? BMWindNormal { get; set; }

	[JsonProperty("COLOR_6")]
	public long? BMWindWater { get; set; }

	[JsonProperty("COLOR_7")]
	public long? BMWindWeakBend { get; set; }

	[JsonProperty("COLOR_8")]
	public long? BMWindWeakWind { get; set; }

	[JsonProperty("JOINTS_0")]
	public long? Joints0 { get; set; }

	[JsonProperty("WEIGHTS_0")]
	public long? Weights0 { get; set; }
}
