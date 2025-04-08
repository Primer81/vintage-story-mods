namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents a vector of 3 doubles. Go bug Tyron of you need more utility methods in this class.
/// </summary>
public class Size3d
{
	public double Width;

	public double Height;

	public double Length;

	public Size3d()
	{
	}

	public Size3d(double width, double height, double length)
	{
		Width = width;
		Height = height;
		Length = length;
	}

	public Size3d Clone()
	{
		return new Size3d(Width, Height, Length);
	}

	public bool CanContain(Size3d obj)
	{
		if (Width >= obj.Width && Height >= obj.Height)
		{
			return Length >= obj.Length;
		}
		return false;
	}
}
