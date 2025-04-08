namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents a vector of 2 doubles. Go bug Tyron of you need more utility methods in this class.
/// </summary>
public class Size2i
{
	public int Width;

	public int Height;

	public Size2i()
	{
	}

	public Size2i(int width, int height)
	{
		Width = width;
		Height = height;
	}

	public Size2i Clone()
	{
		return new Size2i(Width, Height);
	}
}
