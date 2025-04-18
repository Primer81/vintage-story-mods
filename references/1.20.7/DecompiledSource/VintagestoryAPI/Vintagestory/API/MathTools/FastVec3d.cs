using System;
using System.IO;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents a vector of 3 doubles
/// </summary>
public struct FastVec3d
{
	/// <summary>
	/// The X-Component of the vector
	/// </summary>
	public double X;

	/// <summary>
	/// The Y-Component of the vector
	/// </summary>
	public double Y;

	/// <summary>
	/// The Z-Component of the vector
	/// </summary>
	public double Z;

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
				1 => Y, 
				0 => X, 
				_ => Z, 
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
			default:
				Z = value;
				break;
			}
		}
	}

	/// <summary>
	/// Create a new vector with given coordinates
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	public FastVec3d(double x, double y, double z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	/// <summary>
	/// Create a new vector with given coordinates
	/// </summary>
	/// <param name="vec"></param>
	public FastVec3d(Vec4d vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	/// <summary>
	/// Create a new vector with given coordinates
	/// </summary>
	/// <param name="values"></param>
	public FastVec3d(double[] values)
	{
		X = values[0];
		Y = values[1];
		Z = values[2];
	}

	public FastVec3d(Vec3i vec3i)
	{
		X = vec3i.X;
		Y = vec3i.Y;
		Z = vec3i.Z;
	}

	public FastVec3d(BlockPos pos)
	{
		X = pos.X;
		Y = pos.Y;
		Z = pos.Z;
	}

	/// <summary>
	/// Returns the length of this vector
	/// </summary>
	/// <returns></returns>
	public double Length()
	{
		return Math.Sqrt(X * X + Y * Y + Z * Z);
	}

	public void Negate()
	{
		X = 0.0 - X;
		Y = 0.0 - Y;
		Z = 0.0 - Z;
	}

	/// <summary>
	/// Returns the dot product with given vector
	/// </summary>
	/// <param name="a"></param>
	/// <returns></returns>
	public double Dot(Vec3f a)
	{
		return X * (double)a.X + Y * (double)a.Y + Z * (double)a.Z;
	}

	/// <summary>
	/// Returns the dot product with given vector
	/// </summary>
	/// <param name="a"></param>
	/// <returns></returns>
	public double Dot(Vec3d a)
	{
		return X * a.X + Y * a.Y + Z * a.Z;
	}

	/// <summary>
	/// Returns the dot product with given vector
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
	public double Dot(float[] pos)
	{
		return X * (double)pos[0] + Y * (double)pos[1] + Z * (double)pos[2];
	}

	/// <summary>
	/// Returns the dot product with given vector
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
	public double Dot(double[] pos)
	{
		return X * pos[0] + Y * pos[1] + Z * pos[2];
	}

	public double[] ToDoubleArray()
	{
		return new double[3] { X, Y, Z };
	}

	/// <summary>
	/// Adds given x/y/z coordinates to the vector
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public FastVec3d Add(double x, double y, double z)
	{
		X += x;
		Y += y;
		Z += z;
		return this;
	}

	public FastVec3d Add(double d)
	{
		X += d;
		Y += d;
		Z += d;
		return this;
	}

	/// <summary>
	/// Adds given vector's x/y/z coordinates to the vector
	/// </summary>
	public FastVec3d Add(Vec3i vec)
	{
		X += vec.X;
		Y += vec.Y;
		Z += vec.Z;
		return this;
	}

	/// <summary>
	/// Adds given BlockPos's x/y/z coordinates to the vector
	/// </summary>
	public FastVec3d Add(BlockPos pos)
	{
		X += pos.X;
		Y += pos.Y;
		Z += pos.Z;
		return this;
	}

	/// <summary>
	/// Multiplies each coordinate with given multiplier
	/// </summary>
	/// <param name="multiplier"></param>
	/// <returns></returns>
	public FastVec3d Mul(double multiplier)
	{
		X *= multiplier;
		Y *= multiplier;
		Z *= multiplier;
		return this;
	}

	/// <summary>
	/// Creates a copy of the vetor
	/// </summary>
	/// <returns></returns>
	public FastVec3d Clone()
	{
		return (FastVec3d)MemberwiseClone();
	}

	/// <summary>
	/// Turns the vector into a unit vector with length 1, but only if length is non-zero
	/// </summary>
	/// <returns></returns>
	public FastVec3d Normalize()
	{
		double length = Length();
		if (length > 0.0)
		{
			X /= length;
			Y /= length;
			Z /= length;
		}
		return this;
	}

	/// <summary>
	/// Calculates the distance the two endpoints
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	public double Distance(FastVec3d vec)
	{
		return Math.Sqrt((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z));
	}

	/// <summary>
	/// Calculates the square distance the two endpoints
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public double DistanceSq(double x, double y, double z)
	{
		return (X - x) * (X - x) + (Y - y) * (Y - y) + (Z - z) * (Z - z);
	}

	/// <summary>
	/// Calculates the distance the two endpoints
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	public double Distance(Vec3d vec)
	{
		return Math.Sqrt((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z));
	}

	/// <summary>
	/// Adds given coordinates to a new vectors and returns it. The original calling vector remains unchanged
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public FastVec3d AddCopy(double x, double y, double z)
	{
		return new FastVec3d(X + x, Y + y, Z + z);
	}

	/// <summary>
	/// Adds both vectors into a new vector. Both source vectors remain unchanged.
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	public FastVec3d AddCopy(FastVec3d vec)
	{
		return new FastVec3d(X + vec.X, Y + vec.Y, Z + vec.Z);
	}

	/// <summary>
	/// Substracts val from each coordinate if the coordinate if positive, otherwise it is added. If 0, the value is unchanged. The value must be a positive number
	/// </summary>
	/// <param name="val"></param>
	/// <returns></returns>
	public void ReduceBy(double val)
	{
		X = ((X > 0.0) ? Math.Max(0.0, X - val) : Math.Min(0.0, X + val));
		Y = ((Y > 0.0) ? Math.Max(0.0, Y - val) : Math.Min(0.0, Y + val));
		Z = ((Z > 0.0) ? Math.Max(0.0, Z - val) : Math.Min(0.0, Z + val));
	}

	/// <summary>
	/// Creates a new vectors that is the normalized version of this vector. 
	/// </summary>
	/// <returns></returns>
	public FastVec3d NormalizedCopy()
	{
		double length = Length();
		return new FastVec3d(X / length, Y / length, Z / length);
	}

	/// <summary>
	/// Creates a new double precision vector with the same coordinates
	/// </summary>
	/// <returns></returns>
	public Vec3d ToVec3d()
	{
		return new Vec3d(X, Y, Z);
	}

	/// <summary>
	/// Sets the vector to this coordinates
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	public FastVec3d Set(double x, double y, double z)
	{
		X = x;
		Y = y;
		Z = z;
		return this;
	}

	/// <summary>
	/// Sets the vector to the coordinates of given vector
	/// </summary>
	/// <param name="vec"></param>
	public FastVec3d Set(Vec3d vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
		return this;
	}

	public FastVec3d Set(double[] vec)
	{
		X = vec[0];
		Y = vec[1];
		Z = vec[2];
		return this;
	}

	/// <summary>
	/// Sets the vector to the coordinates of given vector
	/// </summary>
	/// <param name="vec"></param>
	public void Set(FastVec3d vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	/// <summary>
	/// Simple string represenation of the x/y/z components
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return "x=" + X + ", y=" + Y + ", z=" + Z;
	}

	public void Write(BinaryWriter writer)
	{
		writer.Write(X);
		writer.Write(Y);
		writer.Write(Z);
	}

	public static FastVec3d CreateFromBytes(BinaryReader reader)
	{
		return new FastVec3d(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
	}
}
