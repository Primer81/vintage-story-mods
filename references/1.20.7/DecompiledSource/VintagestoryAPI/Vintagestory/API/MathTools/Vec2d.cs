using System;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents a vector of 2 doubles. Go bug Tyron of you need more utility methods in this class.
/// </summary>
public class Vec2d
{
	public double X;

	public double Y;

	public Vec2d()
	{
	}

	public Vec2d(double x, double y)
	{
		X = x;
		Y = y;
	}

	public Vec2d Set(double x, double z)
	{
		X = x;
		Y = z;
		return this;
	}

	public double Dot(Vec2d a)
	{
		return X * a.X + Y * a.Y;
	}

	public double Dot(double x, double y)
	{
		return X * x + Y * y;
	}

	public double Length()
	{
		return GameMath.Sqrt(X * X + Y * Y);
	}

	public double LengthSq()
	{
		return X * X + Y * Y;
	}

	public Vec2d Normalize()
	{
		double length = Length();
		if (length > 0.0)
		{
			X /= length;
			Y /= length;
		}
		return this;
	}

	public double DistanceTo(Vec2d pos)
	{
		return DistanceTo(pos.X, pos.Y);
	}

	public double DistanceTo(double targetX, double targetY)
	{
		double num = X - targetX;
		double dy = Y - targetY;
		return Math.Sqrt(num * num + dy * dy);
	}

	public override bool Equals(object obj)
	{
		if (obj is Vec2d d && X == d.X)
		{
			return Y == d.Y;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(X, Y);
	}

	public static Vec2d operator -(Vec2d left, Vec2d right)
	{
		return new Vec2d(left.X - right.X, left.Y - right.Y);
	}

	public static Vec2d operator +(Vec2d left, Vec2d right)
	{
		return new Vec2d(left.X + right.X, left.Y + right.Y);
	}

	public static Vec2d operator +(Vec2d left, Vec2i right)
	{
		return new Vec2d(left.X + (double)right.X, left.Y + (double)right.Y);
	}

	public static Vec2d operator -(Vec2d left, float right)
	{
		return new Vec2d(left.X - (double)right, left.Y - (double)right);
	}

	public static Vec2d operator -(float left, Vec2d right)
	{
		return new Vec2d((double)left - right.X, (double)left - right.Y);
	}

	public static Vec2d operator +(Vec2d left, float right)
	{
		return new Vec2d(left.X + (double)right, left.Y + (double)right);
	}

	public static Vec2d operator *(Vec2d left, float right)
	{
		return new Vec2d(left.X * (double)right, left.Y * (double)right);
	}

	public static Vec2d operator *(float left, Vec2d right)
	{
		return new Vec2d((double)left * right.X, (double)left * right.Y);
	}

	public static Vec2d operator *(Vec2d left, double right)
	{
		return new Vec2d(left.X * right, left.Y * right);
	}

	public static Vec2d operator *(double left, Vec2d right)
	{
		return new Vec2d(left * right.X, left * right.Y);
	}

	public static double operator *(Vec2d left, Vec2d right)
	{
		return left.X * right.X + left.Y * right.Y;
	}

	public static Vec2d operator /(Vec2d left, float right)
	{
		return new Vec2d(left.X / (double)right, left.Y / (double)right);
	}

	public static bool operator ==(Vec2d left, Vec2d right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(Vec2d left, Vec2d right)
	{
		return !(left == right);
	}
}
