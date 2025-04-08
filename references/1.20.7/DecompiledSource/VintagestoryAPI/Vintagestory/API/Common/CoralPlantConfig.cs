using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class CoralPlantConfig
{
	/// <summary>
	/// Height distribution for plants inside the coral reef
	/// </summary>
	public NatFloat Height;

	/// <summary>
	/// chance for this plant to spawn in a reef if
	/// </summary>
	public float Chance;

	[JsonIgnore]
	public Block[] Block;
}
