using System;
using Vintagestory.API.MathTools;

public class VectorTool
{
	public static void ToVectorInFixedSystem(double dx, double dy, double dz, double orientationx, double orientationy, Vec3d output)
	{
		if (dx == 0.0 && dy == 0.0 && dz == 0.0)
		{
			output.X = 0.0;
			output.Y = 0.0;
			output.Z = 0.0;
			return;
		}
		double x = dx * Math.Cos(orientationy) + dy * Math.Sin(orientationx) * Math.Sin(orientationy) - dz * Math.Cos(orientationx) * Math.Sin(orientationy);
		double y = dy * Math.Cos(orientationx) + dz * Math.Sin(orientationx);
		double z = dx * Math.Sin(orientationy) - dy * Math.Sin(orientationx) * Math.Cos(orientationy) + dz * Math.Cos(orientationx) * Math.Cos(orientationy);
		output.X = x;
		output.Y = y;
		output.Z = z;
	}
}
