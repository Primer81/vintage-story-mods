using System;
using ProtoBuf;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents a vector of 2 ints. Go bug Tyron if you need more utility methods in this class.
/// </summary>
[ProtoContract]
public class Vec2i : IEquatable<Vec2i>
{
	[ProtoMember(1)]
	public int X;

	[ProtoMember(2)]
	public int Y;

	public static Vec2i Zero => new Vec2i(0, 0);

	public int this[int index]
	{
		get
		{
			if (index != 0)
			{
				return Y;
			}
			return X;
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
			}
		}
	}

	public Vec2i()
	{
	}

	public Vec2i(int x, int y)
	{
		X = x;
		Y = y;
	}

	public Vec2i(Vec3d pos)
	{
		X = (int)pos.X;
		Y = (int)pos.Z;
	}

	public bool Equals(Vec2i other)
	{
		if (other != null && X == other.X)
		{
			return Y == other.Y;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is Vec2i)
		{
			Vec2i pos = (Vec2i)obj;
			if (X == pos.X)
			{
				return Y == pos.Y;
			}
			return false;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (17 * 23 + X.GetHashCode()) * 23 + Y.GetHashCode();
	}

	public int ManhattenDistance(Vec2i point)
	{
		return Math.Abs(X - point.X) + Math.Abs(Y - point.Y);
	}

	public int ManhattenDistance(int x, int y)
	{
		return Math.Abs(X - x) + Math.Abs(Y - y);
	}

	public Vec2i Set(int x, int y)
	{
		X = x;
		Y = y;
		return this;
	}

	public Vec2i Set(Vec2i vec)
	{
		X = vec.X;
		Y = vec.Y;
		return this;
	}

	public Vec2i Copy()
	{
		return new Vec2i(X, Y);
	}

	public override string ToString()
	{
		return X + " / " + Y;
	}

	public Vec2i Add(int dx, int dy)
	{
		X += dx;
		Y += dy;
		return this;
	}

	/// <summary>
	/// 27 lowest bits for X Coordinate, then 27 bits for Z coordinate
	/// </summary>
	/// <returns></returns>
	public ulong ToChunkIndex()
	{
		return (ulong)(((long)Y << 27) | (uint)X);
	}

	public static Vec2i operator -(Vec2i left, Vec2i right)
	{
		return new Vec2i(left.X - right.X, left.Y - right.Y);
	}

	public static Vec2i operator +(Vec2i left, Vec2i right)
	{
		return new Vec2i(left.X + right.X, left.Y + right.Y);
	}

	public static Vec2i operator -(Vec2i left, int right)
	{
		return new Vec2i(left.X - right, left.Y - right);
	}

	public static Vec2i operator -(int left, Vec2i right)
	{
		return new Vec2i(left - right.X, left - right.Y);
	}

	public static Vec2i operator +(Vec2i left, int right)
	{
		return new Vec2i(left.X + right, left.Y + right);
	}

	public static Vec2i operator *(Vec2i left, int right)
	{
		return new Vec2i(left.X * right, left.Y * right);
	}

	public static Vec2i operator *(int left, Vec2i right)
	{
		return new Vec2i(left * right.X, left * right.Y);
	}

	public static Vec2i operator *(Vec2i left, double right)
	{
		return new Vec2i((int)((double)left.X * right), (int)((double)left.Y * right));
	}

	public static Vec2i operator *(double left, Vec2i right)
	{
		return new Vec2i((int)(left * (double)right.X), (int)(left * (double)right.Y));
	}

	public static double operator *(Vec2i left, Vec2i right)
	{
		return left.X * right.X + left.Y * right.Y;
	}

	public static Vec2i operator /(Vec2i left, int right)
	{
		return new Vec2i(left.X / right, left.Y / right);
	}

	public static Vec2i operator /(Vec2i left, float right)
	{
		return new Vec2i((int)((float)left.X / right), (int)((float)left.Y / right));
	}

	public static bool operator ==(Vec2i left, Vec2i right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(Vec2i left, Vec2i right)
	{
		return !(left == right);
	}
}
