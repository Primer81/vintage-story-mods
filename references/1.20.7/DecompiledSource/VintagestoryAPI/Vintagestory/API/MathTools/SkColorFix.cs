using SkiaSharp;

namespace Vintagestory.API.MathTools;

public static class SkColorFix
{
	public static int ToInt(this SKColor skcolor)
	{
		return skcolor.Blue | (skcolor.Green << 8) | (skcolor.Red << 16) | (skcolor.Alpha << 24);
	}
}
