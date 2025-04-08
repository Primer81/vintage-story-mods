using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class TVec2i : Vec2i
{
	public string IntComp;

	public TVec2i(int x, int y, string intcomp)
		: base(x, y)
	{
		IntComp = intcomp;
	}
}
