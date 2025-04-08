namespace Vintagestory.API.Datastructures;

public class Rectanglef
{
	public float X;

	public float Y;

	public float Width;

	public float Height;

	public float Bottom()
	{
		return Y + Height;
	}

	public Rectanglef()
	{
	}

	public Rectanglef(float x, float y, float width, float height)
	{
		X = x;
		Y = y;
		Width = width;
		Height = height;
	}

	public static Rectanglef Create(float x, float y, float width, float height)
	{
		return new Rectanglef
		{
			X = x,
			Y = y,
			Width = width,
			Height = height
		};
	}
}
