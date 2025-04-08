using System;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents a vector of 4 shorts. Go bug Tyron if you need more utility methods in this class.
/// </summary>
public class Vec4s : IEquatable<Vec4s>
{
	public short X;

	public short Y;

	public short Z;

	public short W;

	public Vec4s()
	{
	}

	public Vec4s(short x, short y, short z, short w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public bool Equals(Vec4s other)
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
}
public class Vec4s<T>
{
	public short X;

	public short Y;

	public short Z;

	public T Value;

	public Vec4s()
	{
	}

	public Vec4s(short x, short y, short z, T Value)
	{
		X = x;
		Y = y;
		Z = z;
		this.Value = Value;
	}
}
