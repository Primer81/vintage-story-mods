using System;

namespace Vintagestory.Common;

public class DelayedCallback
{
	public Action<float> Handler;

	public long CallAtEllapsedMilliseconds;

	public long ListenerId;
}
