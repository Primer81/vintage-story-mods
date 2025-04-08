using System.Runtime.CompilerServices;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client.Tesselation;

public class TileSideEnum
{
	public const int North = 0;

	public const int East = 1;

	public const int South = 2;

	public const int West = 3;

	public const int Up = 4;

	public const int Down = 5;

	public const int SideCount = 6;

	public static int[] Opposites = new int[6] { 2, 3, 0, 1, 5, 4 };

	public static int[] AxisByTileSide = new int[6] { 2, 0, 2, 0, 1, 1 };

	public static FastVec3i[] OffsetByTileSide = new FastVec3i[6]
	{
		new FastVec3i(0, 0, -1),
		new FastVec3i(1, 0, 0),
		new FastVec3i(0, 0, 1),
		new FastVec3i(-1, 0, 0),
		new FastVec3i(0, 1, 0),
		new FastVec3i(0, -1, 0)
	};

	public static int[] MoveIndex = new int[6];

	public static string[] Codes = new string[6] { "north", "east", "south", "west", "up", "down" };

	public static int ToFlags(int nValue)
	{
		return nValue switch
		{
			4 => 16, 
			5 => 32, 
			3 => 8, 
			1 => 2, 
			2 => 4, 
			0 => 1, 
			_ => 0, 
		};
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int GetOpposite(int tileSide)
	{
		return tileSide ^ (2 - tileSide / 4);
	}
}
