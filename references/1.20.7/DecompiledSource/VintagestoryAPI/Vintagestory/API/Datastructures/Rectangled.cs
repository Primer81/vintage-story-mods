namespace Vintagestory.API.Datastructures;

public class Rectangled
{
	public double X;

	public double Y;

	public double Width;

	public double Height;

	public double Bottom()
	{
		return Y + Height;
	}

	public Rectangled()
	{
	}

	public Rectangled(double width, double height)
	{
		Width = width;
		Height = height;
	}

	public Rectangled(double X, double Y, double width, double height)
	{
		this.X = X;
		this.Y = Y;
		Width = width;
		Height = height;
	}

	internal Rectangled Clone()
	{
		return new Rectangled(X, Y, Width, Height);
	}

	public bool PointInside(double x, double y)
	{
		if (x >= X && y >= Y && x < X + Width)
		{
			return y < Y + Height;
		}
		return false;
	}
}
