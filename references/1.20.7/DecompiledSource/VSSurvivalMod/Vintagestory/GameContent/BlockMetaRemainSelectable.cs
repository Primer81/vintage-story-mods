using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockMetaRemainSelectable : Block, IMetaBlock
{
	public bool IsSelectable(BlockPos pos)
	{
		return true;
	}
}
