using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class CuboidWithMaterial : Cuboidi
{
	public byte Material;

	public int this[int index] => index switch
	{
		0 => X1, 
		1 => Y1, 
		2 => Z1, 
		3 => X2, 
		4 => Y2, 
		5 => Z2, 
		_ => throw new ArgumentOutOfRangeException("Must be index 0..5"), 
	};

	public Cuboidf ToCuboidf()
	{
		return new Cuboidf((float)X1 / 16f, (float)Y1 / 16f, (float)Z1 / 16f, (float)X2 / 16f, (float)Y2 / 16f, (float)Z2 / 16f);
	}

	public bool ContainsOrTouches(CuboidWithMaterial neib, int axis)
	{
		switch (axis)
		{
		case 0:
			if (neib.Z2 <= Z2 && neib.Z1 >= Z1 && neib.Y2 <= Y2)
			{
				return neib.Y1 >= Y1;
			}
			return false;
		case 1:
			if (neib.X2 <= X2 && neib.X1 >= X1 && neib.Z2 <= Z2)
			{
				return neib.Z1 >= Z1;
			}
			return false;
		case 2:
			if (neib.X2 <= X2 && neib.X1 >= X1 && neib.Y2 <= Y2)
			{
				return neib.Y1 >= Y1;
			}
			return false;
		default:
			throw new ArgumentOutOfRangeException("axis must be 0, 1 or 2");
		}
	}
}
