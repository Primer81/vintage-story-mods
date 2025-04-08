using System;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents a vector of 4 unsigned shorts. Go bug Tyron if you need more utility methods in this class.
/// </summary>
public class Vec4us : IEquatable<Vec4us>
{
	public ushort X;

	public ushort Y;

	public ushort Z;

	public ushort W;

	public Vec4us()
	{
	}

	public Vec4us(ushort x, ushort y, ushort z, ushort w)
	{
		X = x;
		Y = y;
		Z = z;
		W = w;
	}

	public bool Equals(Vec4us other)
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
public class Vec4us<T>
{
	public ushort X;

	public ushort Y;

	public ushort Z;

	public T Value;

	public Vec4us()
	{
	}

	public Vec4us(ushort x, ushort y, ushort z, T Value)
	{
		X = x;
		Y = y;
		Z = z;
		this.Value = Value;
	}
}
