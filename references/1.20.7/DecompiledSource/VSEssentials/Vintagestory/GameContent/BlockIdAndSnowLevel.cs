using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public struct BlockIdAndSnowLevel
{
	public Block Block;

	public float SnowLevel;

	public BlockIdAndSnowLevel(Block block, float snowLevel)
	{
		Block = block;
		SnowLevel = snowLevel;
	}
}
