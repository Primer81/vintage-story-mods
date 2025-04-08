using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class GameTickListenerBlock : GameTickListenerBase
{
	public BlockPos Pos;

	public Action<IWorldAccessor, BlockPos, float> Handler;
}
