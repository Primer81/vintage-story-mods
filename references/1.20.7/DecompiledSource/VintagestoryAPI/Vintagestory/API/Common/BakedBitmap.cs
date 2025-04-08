using SkiaSharp;

namespace Vintagestory.API.Common;

public class BakedBitmap : IBitmap
{
	public int[] TexturePixels;

	public int Width;

	public int Height;

	public int[] Pixels => TexturePixels;

	int IBitmap.Width => Width;

	int IBitmap.Height => Height;

	public SKColor GetPixel(int x, int y)
	{
		return new SKColor((uint)TexturePixels[Width * y + x]);
	}

	public int GetPixelArgb(int x, int y)
	{
		return TexturePixels[Width * y + x];
	}

	public SKColor GetPixelRel(float x, float y)
	{
		return new SKColor((uint)TexturePixels[Width * (int)(y * (float)Height) + (int)(x * (float)Width)]);
	}

	public int[] GetPixelsTransformed(int rot = 0, int alpha = 100)
	{
		int[] bmpPixels = new int[Width * Height];
		switch (rot)
		{
		case 0:
		{
			for (int x2 = 0; x2 < Width; x2++)
			{
				for (int y2 = 0; y2 < Height; y2++)
				{
					bmpPixels[x2 + y2 * Width] = GetPixelArgb(x2, y2);
				}
			}
			break;
		}
		case 90:
		{
			for (int x = 0; x < Width; x++)
			{
				for (int y = 0; y < Height; y++)
				{
					bmpPixels[y + x * Width] = GetPixelArgb(Width - x - 1, y);
				}
			}
			break;
		}
		case 180:
		{
			for (int x4 = 0; x4 < Width; x4++)
			{
				for (int y4 = 0; y4 < Height; y4++)
				{
					bmpPixels[x4 + y4 * Width] = GetPixelArgb(Width - x4 - 1, Height - y4 - 1);
				}
			}
			break;
		}
		case 270:
		{
			for (int x3 = 0; x3 < Width; x3++)
			{
				for (int y3 = 0; y3 < Height; y3++)
				{
					bmpPixels[y3 + x3 * Width] = GetPixelArgb(x3, Height - y3 - 1);
				}
			}
			break;
		}
		}
		if (alpha != 100)
		{
			float af = (float)alpha / 100f;
			for (int i = 0; i < bmpPixels.Length; i++)
			{
				int current = bmpPixels[i];
				int currAlpha = (current >> 24) & 0xFF;
				bmpPixels[i] = current | ((int)((float)currAlpha * af) << 24);
			}
		}
		return bmpPixels;
	}
}
