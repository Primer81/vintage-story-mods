namespace Vintagestory.API.Client.Tesselation;

public class TileSideFlagsEnum
{
	public const int None = 0;

	public const int North = 1;

	public const int East = 2;

	public const int South = 4;

	public const int West = 8;

	public const int Up = 16;

	public const int Down = 32;

	public const int All = 63;

	public static bool HasFlag(int nFlagA, int nFlagB)
	{
		return (nFlagA & nFlagB) != 0;
	}
}
