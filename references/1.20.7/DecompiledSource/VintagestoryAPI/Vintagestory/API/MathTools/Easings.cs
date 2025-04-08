using System;

namespace Vintagestory.API.MathTools;

public static class Easings
{
	public static double EaseOutBack(double x)
	{
		double c1 = 1.7015800476074219;
		double c2 = c1 + 1.0;
		return 1.0 + c2 * Math.Pow(x - 1.0, 3.0) + c1 * Math.Pow(x - 1.0, 2.0);
	}
}
