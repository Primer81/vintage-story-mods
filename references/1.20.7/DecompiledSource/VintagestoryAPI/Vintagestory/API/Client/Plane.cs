using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public struct Plane
{
	public double normalX;

	public double normalY;

	public double normalZ;

	public double D;

	private const float SQRT3 = 1.7320508f;

	/// <summary>
	/// Creates a Plane with normalised (length 1.0) normal vector
	/// </summary>
	public Plane(double x, double y, double z, double d)
	{
		double normaliser = Math.Sqrt(x * x + y * y + z * z);
		normalX = x / normaliser;
		normalY = y / normaliser;
		normalZ = z / normaliser;
		D = d / normaliser;
	}

	public double distanceOfPoint(double x, double y, double z)
	{
		return normalX * x + normalY * y + normalZ * z + D;
	}

	public bool AABBisOutside(Sphere sphere)
	{
		int sign = ((normalX > 0.0) ? 1 : (-1));
		double num = (double)sphere.x + (double)((float)sign * sphere.radius / 1.7320508f);
		sign = ((normalY > 0.0) ? 1 : (-1));
		double testY = (double)sphere.y + (double)((float)sign * sphere.radiusY / 1.7320508f);
		sign = ((normalZ > 0.0) ? 1 : (-1));
		double testZ = (double)sphere.z + (double)((float)sign * sphere.radiusZ / 1.7320508f);
		return num * normalX + testY * normalY + testZ * normalZ + D < 0.0;
	}
}
