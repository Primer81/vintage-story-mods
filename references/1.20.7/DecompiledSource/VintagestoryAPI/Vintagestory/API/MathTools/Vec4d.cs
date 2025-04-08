namespace Vintagestory.API.MathTools;

public class Vec4d
{
	public double X;

	public double Y;

	public double Z;

	public double W;

	/// <summary>
	/// Returns the n-th coordinate
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public double this[int index]
	{
		get
		{
			return index switch
			{
				2 => Z, 
				1 => Y, 
				0 => X, 
				_ => W, 
			};
		}
		set
		{
			switch (index)
			{
			case 0:
				X = value;
				break;
			case 1:
				Y = value;
				break;
			case 2:
				Z = value;
				break;
			default:
				W = value;
				break;
			}
		}
	}

	public Vec3d XYZ => new Vec3d(X, Y, Z);

	public Vec4d()
	{
	}

	public Vec4d(double x, double y, double z, double w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public void Set(double x, double y, double z, double w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public float SquareDistanceTo(float x, float y, float z)
	{
		double num = X - (double)x;
		double dy = Y - (double)y;
		double dz = Z - (double)z;
		return (float)(num * num + dy * dy + dz * dz);
	}

	public float SquareDistanceTo(double x, double y, double z)
	{
		double num = X - x;
		double dy = Y - y;
		double dz = Z - z;
		return (float)(num * num + dy * dy + dz * dz);
	}

	public float SquareDistanceTo(Vec3d pos)
	{
		double num = X - pos.X;
		double dy = Y - pos.Y;
		double dz = Z - pos.Z;
		return (float)(num * num + dy * dy + dz * dz);
	}

	public float HorizontalSquareDistanceTo(Vec3d pos)
	{
		double num = X - pos.X;
		double dz = Z - pos.Z;
		return (float)(num * num + dz * dz);
	}

	public float HorizontalSquareDistanceTo(double x, double z)
	{
		double num = X - x;
		double dz = Z - z;
		return (float)(num * num + dz * dz);
	}
}
