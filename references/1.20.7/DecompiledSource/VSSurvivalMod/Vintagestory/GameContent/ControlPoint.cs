using System;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class ControlPoint
{
	public AssetLocation Code;

	public object ControlData;

	public event Action<ControlPoint> Activate;

	public void Trigger()
	{
		this.Activate?.Invoke(this);
	}
}
