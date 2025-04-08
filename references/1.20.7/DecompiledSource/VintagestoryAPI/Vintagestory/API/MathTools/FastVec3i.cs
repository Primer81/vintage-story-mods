using System;
using System.IO;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents a vector of 3 ints, similar to a Vec3i or a BlockPos but this is a struct
/// </summary>
public struct FastVec3i
{
	/// <summary>
	/// The X-Component of the vector
	/// </summary>
	public int X;

	/// <summary>
	/// The Y-Component of the vector
	/// </summary>
	public int Y;

	/// <summary>
	/// The Z-Component of the vector
	/// </summary>
	public int Z;

	/// <summary>
	/// Synonum for X
	/// </summary>
	public int R
	{
		get
		{
			return X;
		}
		set
		{
			X = value;
		}
	}

	/// <summary>
	/// Synonum for Y
	/// </summary>
	public int G
	{
		get
		{
			return Y;
		}
		set
		{
			Y = value;
		}
	}

	/// <summary>
	/// Synonum for Z
	/// </summary>
	public int B
	{
		get
		{
			return Z;
		}
		set
		{
			Z = value;
		}
	}

	/// <summary>
	/// Returns the n-th coordinate
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public int this[int index]
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
	public FastVec3i(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	/// <summary>
	/// Create a new vector with given coordinates
	/// </summary>
	/// <param name="pos"></param>
	public FastVec3i(BlockPos pos)
	{
		X = pos.X;
		Y = pos.Y;
		Z = pos.Z;
	}

	/// <summary>
	/// Create a new vector with given coordinates
	/// </summary>
	/// <param name="values"></param>
	public FastVec3i(int[] values)
	{
		X = values[0];
		Y = values[1];
		Z = values[2];
	}

	public FastVec3i(Vec3i vec3i)
	{
		X = vec3i.X;
		Y = vec3i.Y;
		Z = vec3i.Z;
	}

	public bool Equals(Vec3i other)
	{
		if (other != null && X == other.X && Y == other.Y)
		{
			return Z == other.Z;
		}
		return false;
	}

	public bool Equals(BlockPos other)
	{
		if (other != null && X == other.X && Y == other.Y)
		{
			return Z == other.Z;
		}
		return false;
	}

	public bool Equals(FastVec3i other)
	{
		if (X == other.X && Y == other.Y)
		{
			return Z == other.Z;
		}
		return false;
	}

	/// <summary>
	/// Returns the length of this vector
	/// </summary>
	/// <returns></returns>
	public float Length()
	{
		return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
	}

	public void Negate()
	{
		X = -X;
		Y = -Y;
		Z = -Z;
	}

	/// <summary>
	/// Adds given x/y/z coordinates to the vector
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public FastVec3i Add(int x, int y, int z)
	{
		X += x;
		Y += y;
		Z += z;
		return this;
	}

	/// <summary>
	/// Multiplies each coordinate with given multiplier
	/// </summary>
	/// <param name="multiplier"></param>
	/// <returns></returns>
	public FastVec3i Mul(int multiplier)
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
	public FastVec3i Clone()
	{
		return (FastVec3i)MemberwiseClone();
	}

	/// <summary>
	/// Calculates the distance the two endpoints
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	public float Distance(FastVec3i vec)
	{
		return (float)Math.Sqrt((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z));
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
		return ((double)X - x) * ((double)X - x) + ((double)Y - y) * ((double)Y - y) + ((double)Z - z) * ((double)Z - z);
	}

	/// <summary>
	/// Calculates the distance the two endpoints
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	public float Distance(Vec3d vec)
	{
		return (float)Math.Sqrt(((double)X - vec.X) * ((double)X - vec.X) + ((double)Y - vec.Y) * ((double)Y - vec.Y) + ((double)Z - vec.Z) * ((double)Z - vec.Z));
	}

	/// <summary>
	/// Adds both vectors into a new vector. Both source vectors remain unchanged.
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	public FastVec3i AddCopy(FastVec3i vec)
	{
		return new FastVec3i(X + vec.X, Y + vec.Y, Z + vec.Z);
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
	public FastVec3i Set(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
		return this;
	}

	public FastVec3i Set(BlockPos pos)
	{
		X = pos.X;
		Y = pos.Y;
		Z = pos.Z;
		return this;
	}

	/// <summary>
	/// Sets the vector to the coordinates of given vector
	/// </summary>
	/// <param name="vec"></param>
	public FastVec3i Set(Vec3d vec)
	{
		X = (int)vec.X;
		Y = (int)vec.Y;
		Z = (int)vec.Z;
		return this;
	}

	public FastVec3i Set(int[] vec)
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
	public void Set(FastVec3i vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	public override int GetHashCode()
	{
		return ((391 + X) * 23 + Y) * 23 + Z;
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

	public static FastVec3i CreateFromBytes(BinaryReader reader)
	{
		return new FastVec3i(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
	}
}
