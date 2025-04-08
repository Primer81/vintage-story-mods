using System;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents a three dimensional axis-aligned cuboid using two 3d coordinates. Used for collision and selection withes.
/// </summary>
public class Cuboidd : ICuboid<double, Cuboidd>, IEquatable<Cuboidd>
{
	private const double epsilon = 1.6E-05;

	public double X1;

	public double Y1;

	public double Z1;

	public double X2;

	public double Y2;

	public double Z2;

	/// <summary>
	/// MaxX-MinX
	/// </summary>
	public double Width => MaxX - MinX;

	/// <summary>
	/// MaxY-MinY
	/// </summary>
	public double Height => MaxY - MinY;

	/// <summary>
	/// MaxZ-MinZ
	/// </summary>
	public double Length => MaxZ - MinZ;

	public double MinX => Math.Min(X1, X2);

	public double MinY => Math.Min(Y1, Y2);

	public double MinZ => Math.Min(Z1, Z2);

	public double MaxX => Math.Max(X1, X2);

	public double MaxY => Math.Max(Y1, Y2);

	public double MaxZ => Math.Max(Z1, Z2);

	public Vec3d Start => new Vec3d(MinX, MinY, MinZ);

	public Vec3d End => new Vec3d(MaxX, MaxY, MaxZ);

	public Cuboidd()
	{
	}

	public Cuboidd(double x1, double y1, double z1, double x2, double y2, double z2)
	{
		Set(x1, y1, z1, x2, y2, z2);
	}

	public Cuboidd(Vec3d start, Vec3d end)
	{
		X1 = start.X;
		Y1 = start.Y;
		Z1 = start.Z;
		X2 = end.X;
		Y2 = end.Y;
		Z2 = end.Z;
	}

	/// <summary>
	/// Sets the minimum and maximum values of the cuboid
	/// </summary>
	public Cuboidd Set(double x1, double y1, double z1, double x2, double y2, double z2)
	{
		X1 = x1;
		Y1 = y1;
		Z1 = z1;
		X2 = x2;
		Y2 = y2;
		Z2 = z2;
		return this;
	}

	/// <summary>
	/// Sets the minimum and maximum values of the cuboid
	/// </summary>
	public Cuboidd Set(IVec3 min, IVec3 max)
	{
		Set(min.XAsDouble, min.YAsDouble, min.ZAsDouble, max.XAsDouble, max.YAsDouble, max.ZAsDouble);
		return this;
	}

	/// <summary>
	/// Sets the minimum and maximum values of the cuboid
	/// </summary>
	public Cuboidd Set(Cuboidf selectionBox)
	{
		X1 = selectionBox.X1;
		Y1 = selectionBox.Y1;
		Z1 = selectionBox.Z1;
		X2 = selectionBox.X2;
		Y2 = selectionBox.Y2;
		Z2 = selectionBox.Z2;
		return this;
	}

	public void Set(Cuboidd other)
	{
		X1 = other.X1;
		Y1 = other.Y1;
		Z1 = other.Z1;
		X2 = other.X2;
		Y2 = other.Y2;
		Z2 = other.Z2;
	}

	/// <summary>
	/// Sets the cuboid to the selectionBox, translated by vec
	/// </summary>
	public Cuboidd SetAndTranslate(Cuboidf selectionBox, Vec3d vec)
	{
		X1 = (double)selectionBox.X1 + vec.X;
		Y1 = (double)selectionBox.Y1 + vec.Y;
		Z1 = (double)selectionBox.Z1 + vec.Z;
		X2 = (double)selectionBox.X2 + vec.X;
		Y2 = (double)selectionBox.Y2 + vec.Y;
		Z2 = (double)selectionBox.Z2 + vec.Z;
		return this;
	}

	/// <summary>
	/// Sets the cuboid to the selectionBox, translated by (dX, dY, dZ)
	/// </summary>
	public Cuboidd SetAndTranslate(Cuboidf selectionBox, double dX, double dY, double dZ)
	{
		X1 = (double)selectionBox.X1 + dX;
		Y1 = (double)selectionBox.Y1 + dY;
		Z1 = (double)selectionBox.Z1 + dZ;
		X2 = (double)selectionBox.X2 + dX;
		Y2 = (double)selectionBox.Y2 + dY;
		Z2 = (double)selectionBox.Z2 + dZ;
		return this;
	}

	public void RemoveRoundingErrors()
	{
		double x1Test = X1 * 16.0;
		double z1Test = Z1 * 16.0;
		double x2Test = X2 * 16.0;
		double z2Test = Z2 * 16.0;
		if (Math.Ceiling(x1Test) - x1Test < 1.6E-05)
		{
			X1 = Math.Ceiling(x1Test) / 16.0;
		}
		if (Math.Ceiling(z1Test) - z1Test < 1.6E-05)
		{
			Z1 = Math.Ceiling(z1Test) / 16.0;
		}
		if (x2Test - Math.Floor(x2Test) < 1.6E-05)
		{
			X2 = Math.Floor(x2Test) / 16.0;
		}
		if (z2Test - Math.Floor(z2Test) < 1.6E-05)
		{
			Z2 = Math.Floor(z2Test) / 16.0;
		}
	}

	/// <summary>
	/// Adds the given offset to the cuboid
	/// </summary>
	public Cuboidd Translate(IVec3 vec)
	{
		Translate(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble);
		return this;
	}

	/// <summary>
	/// Adds the given offset to the cuboid
	/// </summary>
	public Cuboidd Translate(double posX, double posY, double posZ)
	{
		X1 += posX;
		Y1 += posY;
		Z1 += posZ;
		X2 += posX;
		Y2 += posY;
		Z2 += posZ;
		return this;
	}

	public Cuboidd GrowBy(double dx, double dy, double dz)
	{
		X1 -= dx;
		Y1 -= dy;
		Z1 -= dz;
		X2 += dx;
		Y2 += dy;
		Z2 += dz;
		return this;
	}

	/// <summary>
	/// Returns if the given point is inside the cuboid
	/// </summary>
	public bool ContainsOrTouches(double x, double y, double z)
	{
		if (x >= X1 && x <= X2 && y >= Y1 && y <= Y2 && z >= Z1)
		{
			return z <= Z2;
		}
		return false;
	}

	/// <summary>
	/// Returns if the given point is inside the cuboid
	/// </summary>
	public bool Contains(double x, double y, double z)
	{
		if (x > X1 && x < X2 && y > Y1 && y < Y2 && z > Z1)
		{
			return z < Z2;
		}
		return false;
	}

	/// <summary>
	/// Returns if the given point is inside the cuboid
	/// </summary>
	public bool ContainsOrTouches(IVec3 vec)
	{
		return ContainsOrTouches(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble);
	}

	/// <summary>
	/// Grows the cuboid so that it includes the given block
	/// </summary>
	public Cuboidd GrowToInclude(int x, int y, int z)
	{
		X1 = Math.Min(X1, x);
		Y1 = Math.Min(Y1, y);
		Z1 = Math.Min(Z1, z);
		X2 = Math.Max(X2, x + 1);
		Y2 = Math.Max(Y2, y + 1);
		Z2 = Math.Max(Z2, z + 1);
		return this;
	}

	/// <summary>
	/// Grows the cuboid so that it includes the given block
	/// </summary>
	public Cuboidd GrowToInclude(IVec3 vec)
	{
		GrowToInclude(vec.XAsInt, vec.YAsInt, vec.ZAsInt);
		return this;
	}

	/// <summary>
	/// Returns the shortest distance between given point and any point inside the cuboid
	/// </summary>
	public double ShortestDistanceFrom(double x, double y, double z)
	{
		double num = x - GameMath.Clamp(x, X1, X2);
		double cy = y - GameMath.Clamp(y, Y1, Y2);
		double cz = z - GameMath.Clamp(z, Z1, Z2);
		return Math.Sqrt(num * num + cy * cy + cz * cz);
	}

	public Cuboidi ToCuboidi()
	{
		return new Cuboidi((int)X1, (int)Y1, (int)Z1, (int)X2, (int)Y2, (int)Z2);
	}

	/// <summary>
	/// Returns the shortest distance between given point and any point inside the cuboid
	/// </summary>
	public double ShortestVerticalDistanceFrom(double y)
	{
		return y - GameMath.Clamp(y, Y1, Y2);
	}

	/// <summary>
	/// Returns the shortest vertical distance to any point between this and given cuboid
	/// </summary>
	public double ShortestVerticalDistanceFrom(Cuboidd cuboid)
	{
		double val = cuboid.Y1 - GameMath.Clamp(cuboid.Y1, Y1, Y2);
		double dy = cuboid.Y2 - GameMath.Clamp(cuboid.Y2, Y1, Y2);
		return Math.Min(val, dy);
	}

	/// <summary>
	/// Returns the shortest distance to any point between this and given cuboid
	/// </summary>
	public double ShortestVerticalDistanceFrom(Cuboidf cuboid, EntityPos offset)
	{
		double num = offset.Y + (double)cuboid.Y1;
		double oY2 = offset.Y + (double)cuboid.Y2;
		double cy = num - GameMath.Clamp(num, Y1, Y2);
		if (num <= Y1 && oY2 >= Y2)
		{
			cy = 0.0;
		}
		double dy = oY2 - GameMath.Clamp(oY2, Y1, Y2);
		return Math.Min(cy, dy);
	}

	/// <summary>
	/// Returns the shortest distance to any point between this and given cuboid
	/// </summary>
	public double ShortestDistanceFrom(Cuboidd cuboid)
	{
		double num = cuboid.X1 - GameMath.Clamp(cuboid.X1, X1, X2);
		double cy = cuboid.Y1 - GameMath.Clamp(cuboid.Y1, Y1, Y2);
		double cz = cuboid.Z1 - GameMath.Clamp(cuboid.Z1, Z1, Z2);
		double dx = cuboid.X2 - GameMath.Clamp(cuboid.X2, X1, X2);
		double dy = cuboid.Y2 - GameMath.Clamp(cuboid.Y2, Y1, Y2);
		double dz = cuboid.Z2 - GameMath.Clamp(cuboid.Z2, Z1, Z2);
		return Math.Sqrt(Math.Min(num * num, dx * dx) + Math.Min(cy * cy, dy * dy) + Math.Min(cz * cz, dz * dz));
	}

	/// <summary>
	/// Returns the shortest distance to any point between this and given cuboid
	/// </summary>
	public double ShortestDistanceFrom(Cuboidf cuboid, BlockPos offset)
	{
		double num = (float)offset.X + cuboid.X1;
		double oY1 = (float)offset.Y + cuboid.Y1;
		double oZ1 = (float)offset.Z + cuboid.Z1;
		double oX2 = (float)offset.X + cuboid.X2;
		double oY2 = (float)offset.Y + cuboid.Y2;
		double oZ2 = (float)offset.Z + cuboid.Z2;
		double cx = num - GameMath.Clamp(num, X1, X2);
		double cy = oY1 - GameMath.Clamp(oY1, Y1, Y2);
		double cz = oZ1 - GameMath.Clamp(oZ1, Z1, Z2);
		if (num <= X1 && oX2 >= X2)
		{
			cx = 0.0;
		}
		if (oY1 <= Y1 && oY2 >= Y2)
		{
			cy = 0.0;
		}
		if (oZ1 <= Z1 && oZ2 >= Z2)
		{
			cz = 0.0;
		}
		double dx = oX2 - GameMath.Clamp(oX2, X1, X2);
		double dy = oY2 - GameMath.Clamp(oY2, Y1, Y2);
		double dz = oZ2 - GameMath.Clamp(oZ2, Z1, Z2);
		return Math.Sqrt(Math.Min(cx * cx, dx * dx) + Math.Min(cy * cy, dy * dy) + Math.Min(cz * cz, dz * dz));
	}

	/// <summary>
	/// Returns the shortest horizontal distance to any point between this and given cuboid
	/// </summary>
	public double ShortestHorizontalDistanceFrom(Cuboidf cuboid, BlockPos offset)
	{
		double num = (double)((float)offset.X + cuboid.X1) - GameMath.Clamp((float)offset.X + cuboid.X1, X1, X2);
		double cz = (double)((float)offset.Z + cuboid.Z1) - GameMath.Clamp((float)offset.Z + cuboid.Z1, Z1, Z2);
		double dx = (double)((float)offset.X + cuboid.X2) - GameMath.Clamp((float)offset.X + cuboid.X2, X1, X2);
		double dz = (double)((float)offset.Z + cuboid.Z2) - GameMath.Clamp((float)offset.Z + cuboid.Z2, Z1, Z2);
		return Math.Sqrt(Math.Min(num * num, dx * dx) + Math.Min(cz * cz, dz * dz));
	}

	/// <summary>
	/// Returns the shortest horizontal distance to any point between this and given coordinate
	/// </summary>
	/// <param name="x"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public double ShortestHorizontalDistanceFrom(double x, double z)
	{
		double num = x - GameMath.Clamp(x, X1, X2);
		double cz = z - GameMath.Clamp(z, Z1, Z2);
		return Math.Sqrt(num * num + cz * cz);
	}

	/// <summary>
	/// Returns the shortest distance between given point and any point inside the cuboid
	/// </summary>
	public double ShortestDistanceFrom(IVec3 vec)
	{
		return ShortestDistanceFrom(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble);
	}

	/// <summary>
	/// Returns a new x coordinate that's ensured to be outside this cuboid. Used for collision detection.
	/// </summary>
	public double pushOutX(Cuboidd from, double motx, ref EnumPushDirection direction)
	{
		direction = EnumPushDirection.None;
		if (from.Z2 > Z1 && from.Z1 < Z2 && from.Y2 > Y1 && from.Y1 < Y2)
		{
			if (motx > 0.0 && from.X2 <= X1 && X1 - from.X2 < motx)
			{
				direction = EnumPushDirection.Positive;
				motx = X1 - from.X2;
			}
			else if (motx < 0.0 && from.X1 >= X2 && X2 - from.X1 > motx)
			{
				direction = EnumPushDirection.Negative;
				motx = X2 - from.X1;
			}
		}
		return motx;
	}

	/// <summary>
	/// Returns a new y coordinate that's ensured to be outside this cuboid. Used for collision detection.
	/// </summary>
	public double pushOutY(Cuboidd from, double moty, ref EnumPushDirection direction)
	{
		direction = EnumPushDirection.None;
		if (from.X2 > X1 && from.X1 < X2 && from.Z2 > Z1 && from.Z1 < Z2)
		{
			if (moty > 0.0 && from.Y2 <= Y1 && Y1 - from.Y2 < moty)
			{
				direction = EnumPushDirection.Positive;
				moty = Y1 - from.Y2;
			}
			else if (moty < 0.0 && from.Y1 >= Y2 && Y2 - from.Y1 > moty)
			{
				direction = EnumPushDirection.Negative;
				moty = Y2 - from.Y1;
			}
		}
		return moty;
	}

	/// <summary>
	/// Returns a new z coordinate that's ensured to be outside this cuboid. Used for collision detection.
	/// </summary>
	public double pushOutZ(Cuboidd from, double motz, ref EnumPushDirection direction)
	{
		direction = EnumPushDirection.None;
		if (from.X2 > X1 && from.X1 < X2 && from.Y2 > Y1 && from.Y1 < Y2)
		{
			if (motz > 0.0 && from.Z2 <= Z1 && Z1 - from.Z2 < motz)
			{
				direction = EnumPushDirection.Positive;
				motz = Z1 - from.Z2;
			}
			else if (motz < 0.0 && from.Z1 >= Z2 && Z2 - from.Z1 > motz)
			{
				direction = EnumPushDirection.Negative;
				motz = Z2 - from.Z1;
			}
		}
		return motz;
	}

	/// <summary>
	/// Performs a 3-dimensional rotation on the cuboid and returns a new axis-aligned cuboid resulting from this rotation. Not sure it it makes any sense to use this for other rotations than 90 degree intervals.
	/// </summary>
	public Cuboidd RotatedCopy(double degX, double degY, double degZ, Vec3d origin)
	{
		double radX = degX * 0.01745329238474369;
		double radY = degY * 0.01745329238474369;
		double radZ = degZ * 0.01745329238474369;
		double[] matrix = Mat4d.Create();
		Mat4d.RotateX(matrix, matrix, radX);
		Mat4d.RotateY(matrix, matrix, radY);
		Mat4d.RotateZ(matrix, matrix, radZ);
		(new double[4])[3] = 1.0;
		double[] min = new double[4]
		{
			X1 - origin.X,
			Y1 - origin.Y,
			Z1 - origin.Z,
			1.0
		};
		double[] max = new double[4]
		{
			X2 - origin.X,
			Y2 - origin.Y,
			Z2 - origin.Z,
			1.0
		};
		min = Mat4d.MulWithVec4(matrix, min);
		max = Mat4d.MulWithVec4(matrix, max);
		if (max[0] < min[0])
		{
			double tmp = max[0];
			max[0] = min[0];
			min[0] = tmp;
		}
		if (max[1] < min[1])
		{
			double tmp = max[1];
			max[1] = min[1];
			min[1] = tmp;
		}
		if (max[2] < min[2])
		{
			double tmp = max[2];
			max[2] = min[2];
			min[2] = tmp;
		}
		return new Cuboidd(min[0] + origin.X, min[1] + origin.Y, min[2] + origin.Z, max[0] + origin.X, max[1] + origin.Y, max[2] + origin.Z);
	}

	/// <summary>
	/// Performs a 3-dimensional rotation on the cuboid and returns a new axis-aligned cuboid resulting from this rotation. Not sure it makes any sense to use this for other rotations than 90 degree intervals.
	/// </summary>
	public Cuboidd RotatedCopy(IVec3 vec, Vec3d origin)
	{
		return RotatedCopy(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble, origin);
	}

	public Cuboidd Offset(double dx, double dy, double dz)
	{
		X1 += dx;
		Y1 += dy;
		Z1 += dz;
		X2 += dx;
		Y2 += dy;
		Z2 += dz;
		return this;
	}

	/// <summary>
	/// Returns a new cuboid offseted by given position
	/// </summary>
	public Cuboidd OffsetCopy(double x, double y, double z)
	{
		return new Cuboidd(X1 + x, Y1 + y, Z1 + z, X2 + x, Y2 + y, Z2 + z);
	}

	/// <summary>
	/// Returns a new cuboid offseted by given position
	/// </summary>
	public Cuboidd OffsetCopy(IVec3 vec)
	{
		return OffsetCopy(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble);
	}

	/// <summary>
	/// If the given cuboid intersects with this cuboid
	/// </summary>
	public bool Intersects(Cuboidd other)
	{
		if (X2 > other.X1 && X1 < other.X2 && Y2 > other.Y1 && Y1 < other.Y2 && Z2 > other.Z1 && Z1 < other.Z2)
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// If the given cuboid intersects with this cuboid
	/// </summary>
	public bool Intersects(Cuboidf other)
	{
		if (X2 > (double)other.X1 && X1 < (double)other.X2 && Y2 > (double)other.Y1 && Y1 < (double)other.Y2 && Z2 > (double)other.Z1 && Z1 < (double)other.Z2)
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// If the given cuboid intersects with this cuboid
	/// </summary>
	public bool Intersects(Cuboidf other, Vec3d offset)
	{
		if (X2 > (double)other.X1 + offset.X && X1 < (double)other.X2 + offset.X && Z2 > (double)other.Z1 + offset.Z && Z1 < (double)other.Z2 + offset.Z && Y2 > (double)other.Y1 + offset.Y && Y1 < Math.Round((double)other.Y2 + offset.Y, 5))
		{
			return true;
		}
		return false;
	}

	public bool Intersects(Cuboidf other, double offsetx, double offsety, double offsetz)
	{
		if (X2 > (double)other.X1 + offsetx && X1 < (double)other.X2 + offsetx && Z2 > (double)other.Z1 + offsetz && Z1 < (double)other.Z2 + offsetz && Y2 > (double)other.Y1 + offsety && Y1 < Math.Round((double)other.Y2 + offsety, 5))
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// If the given cuboid intersects with this cuboid
	/// </summary>
	public bool IntersectsOrTouches(Cuboidd other)
	{
		if (X2 >= other.X1 && X1 <= other.X2 && Y2 >= other.Y1 && Y1 <= other.Y2 && Z2 >= other.Z1 && Z1 <= other.Z2)
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// If the given cuboid intersects with this cuboid
	/// </summary>
	public bool IntersectsOrTouches(Cuboidf other, Vec3d offset)
	{
		if (X2 >= (double)other.X1 + offset.X && X1 <= (double)other.X2 + offset.X && Z2 >= (double)other.Z1 + offset.Z && Z1 <= (double)other.Z2 + offset.Z && Y2 >= (double)other.Y1 + offset.Y && Y1 <= Math.Round((double)other.Y2 + offset.Y, 5))
		{
			return true;
		}
		return false;
	}

	/// <summary>
	/// If the given cuboid intersects with this cuboid
	/// </summary>
	public bool IntersectsOrTouches(Cuboidf other, double offsetX, double offsetY, double offsetZ)
	{
		return !(X2 < (double)other.X1 + offsetX) && !(X1 > (double)other.X2 + offsetX) && !(Y2 < (double)other.Y1 + offsetY) && !(Y1 > (double)other.Y2 + offsetY) && !(Z2 < (double)other.Z1 + offsetZ) && !(Z1 > (double)other.Z2 + offsetZ);
	}

	public Cuboidf ToFloat()
	{
		return new Cuboidf((float)X1, (float)Y1, (float)Z1, (float)X2, (float)Y2, (float)Z2);
	}

	/// <summary>
	/// Creates a copy of the cuboid
	/// </summary>
	public Cuboidd Clone()
	{
		return (Cuboidd)MemberwiseClone();
	}

	public bool Equals(Cuboidd other)
	{
		if (other.X1 == X1 && other.Y1 == Y1 && other.Z1 == Z1 && other.X2 == X2 && other.Y2 == Y2)
		{
			return other.Z2 == Z2;
		}
		return false;
	}
}
