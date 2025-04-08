using System.Collections.Generic;

namespace Vintagestory.API.MathTools;

public class FastVec3iComparer : IComparer<FastVec3i>
{
	int IComparer<FastVec3i>.Compare(FastVec3i a, FastVec3i b)
	{
		if (a.X != b.X)
		{
			if (a.X >= b.X)
			{
				return 1;
			}
			return -1;
		}
		if (a.Z != b.Z)
		{
			if (a.Z >= b.Z)
			{
				return 1;
			}
			return -1;
		}
		if (a.Y != b.Y)
		{
			if (a.Y >= b.Y)
			{
				return 1;
			}
			return -1;
		}
		return 0;
	}
}
