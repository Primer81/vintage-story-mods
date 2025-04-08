using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface IConditionalChiselable
{
	bool CanChisel(IWorldAccessor world, BlockPos pos, IPlayer player, out string errorCode);
}
