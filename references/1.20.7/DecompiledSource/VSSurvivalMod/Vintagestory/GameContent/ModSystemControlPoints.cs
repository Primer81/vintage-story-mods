using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class ModSystemControlPoints : ModSystem
{
	protected Dictionary<AssetLocation, ControlPoint> controlPoints = new Dictionary<AssetLocation, ControlPoint>();

	public ControlPoint this[AssetLocation code]
	{
		get
		{
			if (!controlPoints.TryGetValue(code, out var cpoint))
			{
				return controlPoints[code] = new ControlPoint();
			}
			return cpoint;
		}
		set
		{
			controlPoints[code] = value;
		}
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override double ExecuteOrder()
	{
		return 0.0;
	}
}
