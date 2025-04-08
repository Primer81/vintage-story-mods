using SkiaSharp;

namespace Vintagestory.API.Common;

public interface IBitmap
{
	int Width { get; }

	int Height { get; }

	int[] Pixels { get; }

	SKColor GetPixel(int x, int y);

	SKColor GetPixelRel(float x, float y);

	int[] GetPixelsTransformed(int rot = 0, int alpha = 100);
}
