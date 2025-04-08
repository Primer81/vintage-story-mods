using System;

namespace Vintagestory.API.MathTools;

public struct Rectanglei
{
	private int X;

	private int Y;

	private int Width;

	private int Height;

	/// <summary>
	/// Same as X
	/// </summary>
	public int X1 => X;

	/// <summary>
	/// Same as Y
	/// </summary>
	public int Y1 => Y;

	/// <summary>
	/// Same as X + Width
	/// </summary>
	public int X2 => X + Width;

	/// <summary>
	/// Same as Y + Height
	/// </summary>
	public int Y2 => Y + Height;

	public int Bottom()
	{
		return Y + Height;
	}

	public Rectanglei(int X, int Y, int width, int height)
	{
		this.X = X;
		this.Y = Y;
		Width = width;
		Height = height;
	}

	public Rectanglei GrowBy(int size)
	{
		return new Rectanglei(X - size, Y - size, Width + size * 2, Height + size * 2);
	}

	[Obsolete("Rectanglei is a struct and there is no point cloning a struct")]
	public Rectanglei Clone()
	{
		return new Rectanglei(X, Y, Width, Height);
	}

	public bool PointInside(int x, int y)
	{
		if (x >= X && y >= Y && x <= X + Width)
		{
			return y <= Y + Height;
		}
		return false;
	}

	public bool PointInside(Vec2i pos)
	{
		if (pos.X >= X && pos.Y >= Y && pos.X <= X + Width)
		{
			return pos.Y <= Y + Height;
		}
		return false;
	}

	/// <summary>
	/// If the given cuboid intersects with this cubiod
	/// </summary>
	public bool Intersects(Rectanglei with)
	{
		if (with.X2 <= X1 || with.X1 >= X2)
		{
			return false;
		}
		if (with.Y2 <= Y1 || with.Y1 >= Y2)
		{
			return false;
		}
		return true;
	}

	/// <summary>
	/// If the given cuboid intersects  with or is adjacent to this cubiod
	/// </summary>
	public bool IntersectsOrTouches(Rectanglei with)
	{
		if (with.X2 < X1)
		{
			return false;
		}
		if (with.X1 >= X2)
		{
			return false;
		}
		if (with.Y2 < Y1)
		{
			return false;
		}
		if (with.Y1 >= Y2)
		{
			return false;
		}
		return true;
	}

	public bool IntersectsOrTouches(int withX1, int withY1, int withX2, int withY2)
	{
		if (withX2 < X1)
		{
			return false;
		}
		if (withX1 >= X2)
		{
			return false;
		}
		if (withY2 < Y1)
		{
			return false;
		}
		if (withY1 >= Y2)
		{
			return false;
		}
		return true;
	}

	/// <summary>
	/// Returns if the given point is inside the cuboid
	/// </summary>
	public bool Contains(Vec2i pos)
	{
		if (pos.X >= X1 && pos.X < X2 && pos.Y >= Y1)
		{
			return pos.Y < Y2;
		}
		return false;
	}

	/// <summary>
	/// Returns if the given point is inside the cuboid
	/// </summary>
	public bool Contains(int x, int y)
	{
		if (x >= X1 && x < X2 && y >= Y1)
		{
			return y < Y2;
		}
		return false;
	}
}
