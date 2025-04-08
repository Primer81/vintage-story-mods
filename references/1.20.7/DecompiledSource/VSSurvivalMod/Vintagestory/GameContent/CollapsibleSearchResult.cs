using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class CollapsibleSearchResult
{
	public float NearestSupportDistance;

	public List<Vec4i> SupportPositions;

	public bool Unconnected;

	public float Instability;
}
