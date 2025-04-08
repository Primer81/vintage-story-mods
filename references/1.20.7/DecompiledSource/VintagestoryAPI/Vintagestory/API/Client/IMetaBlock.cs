using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface IMetaBlock
{
	bool IsSelectable(BlockPos pos);
}
