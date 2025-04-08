using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class BlockOffsetAndNumber : Vec4i
{
	public int BlockNumber => W;

	public BlockOffsetAndNumber()
	{
	}

	public BlockOffsetAndNumber(int x, int y, int z, int w)
		: base(x, y, z, w)
	{
	}
}
