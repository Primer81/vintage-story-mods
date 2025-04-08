using System;
using ProtoBuf;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents a vector of 4 ints. Go bug Tyron if you need more utility methods in this class.
/// </summary>
[ProtoContract]
public class Vec4i : IEquatable<Vec4i>
{
	[ProtoMember(1)]
	public int X;

	[ProtoMember(2)]
	public int Y;

	[ProtoMember(3)]
	public int Z;

	[ProtoMember(4)]
	public int W;

	public Vec4i()
	{
	}

	public Vec4i(BlockPos pos, int w)
	{
		X = pos.X;
		Y = pos.InternalY;
		Z = pos.Z;
		W = w;
	}

	public Vec4i(int x, int y, int z, int w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public bool Equals(Vec4i other)
	{
		if (other != null && other.X == X && other.Y == Y && other.Z == Z)
		{
			return other.W == W;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((17 * 23 + X.GetHashCode()) * 23 + Y.GetHashCode()) * 23 + Z.GetHashCode()) * 23 + W.GetHashCode();
	}

	/// <summary>
	/// Returns the squared Euclidean horizontal distance to between this and given position
	/// </summary>
	/// <param name="x"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public float HorDistanceSqTo(double x, double z)
	{
		double num = x - (double)X;
		double dz = z - (double)Z;
		return (float)(num * num + dz * dz);
	}
}
public class Vec4i<T>
{
	public int X;

	public int Y;

	public int Z;

	public T Value;

	public Vec4i()
	{
	}

	public Vec4i(int x, int y, int z, T Value)
	{
		X = x;
		Y = y;
		Z = z;
		this.Value = Value;
	}
}
