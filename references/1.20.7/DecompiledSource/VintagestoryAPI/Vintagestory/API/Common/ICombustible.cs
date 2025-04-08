using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface ICombustible
{
	float GetBurnDuration(IWorldAccessor world, BlockPos pos);
}
