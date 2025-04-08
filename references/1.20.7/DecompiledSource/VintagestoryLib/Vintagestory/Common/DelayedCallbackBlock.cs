using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class DelayedCallbackBlock
{
	public BlockPos Pos;

	public Action<IWorldAccessor, BlockPos, float> Handler;

	public long CallAtEllapsedMilliseconds;

	public long ListenerId;
}
