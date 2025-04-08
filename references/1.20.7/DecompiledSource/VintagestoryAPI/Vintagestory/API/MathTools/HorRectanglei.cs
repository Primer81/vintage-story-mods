using System;

namespace Vintagestory.API.MathTools;

public class HorRectanglei
{
	public int X1;

	public int Z1;

	public int X2;

	public int Z2;

	public int MinX => Math.Min(X1, X2);

	public int MaxX => Math.Max(X1, X2);

	public int MaxZ => Math.Max(Z1, Z2);

	public int MinZ => Math.Min(Z1, Z2);

	public HorRectanglei()
	{
	}

	public HorRectanglei(int x1, int z1, int x2, int z2)
	{
		X1 = x1;
		Z1 = z1;
		X2 = x2;
		Z2 = z2;
	}
}
