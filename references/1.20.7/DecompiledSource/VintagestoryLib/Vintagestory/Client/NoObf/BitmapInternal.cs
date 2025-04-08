using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf;

public class BitmapInternal
{
	internal int[] argb;

	internal int width;

	internal int height;

	public static BitmapInternal Create(int width, int height)
	{
		return new BitmapInternal
		{
			width = width,
			height = height,
			argb = new int[width * height]
		};
	}

	public static BitmapInternal CreateFromBitmap(ClientPlatformAbstract platform, BitmapRef bitmapref)
	{
		return new BitmapInternal
		{
			width = bitmapref.Width,
			height = bitmapref.Height,
			argb = bitmapref.Pixels
		};
	}

	public void SetPixel(int x, int y, int color)
	{
		argb[x + y * width] = color;
	}

	public int GetPixel(int x, int y)
	{
		return argb[x + y * width];
	}

	public BitmapRef ToBitmap(ClientPlatformAbstract platform)
	{
		BitmapRef bmp = platform.CreateBitmap(width, height);
		platform.SetBitmapPixelsArgb(bmp, argb);
		return bmp;
	}
}
