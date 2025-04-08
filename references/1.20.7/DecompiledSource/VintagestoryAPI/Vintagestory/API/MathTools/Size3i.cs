namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents a vector of 3 integers. Go bug Tyron of you need more utility methods in this class.
/// </summary>
public class Size3i
{
	public int Width;

	public int Height;

	public int Length;

	public Size3i()
	{
	}

	public Size3i(int width, int height, int length)
	{
		Width = width;
		Height = height;
		Length = length;
	}

	public Size3i Clone()
	{
		return new Size3i(Width, Height, Length);
	}

	public bool CanContain(Size3i obj)
	{
		if (Width >= obj.Width && Height >= obj.Height)
		{
			return Length >= obj.Length;
		}
		return false;
	}
}
