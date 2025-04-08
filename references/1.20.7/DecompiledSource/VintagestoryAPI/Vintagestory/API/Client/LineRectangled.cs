using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Client;

public class LineRectangled : Rectangled
{
	public double Ascent;

	public double AscentOrHeight
	{
		get
		{
			if (!(Ascent > 0.0))
			{
				return Height;
			}
			return Ascent;
		}
	}

	public LineRectangled(double X, double Y, double width, double height)
		: base(X, Y, width, height)
	{
	}

	public LineRectangled()
	{
	}
}
