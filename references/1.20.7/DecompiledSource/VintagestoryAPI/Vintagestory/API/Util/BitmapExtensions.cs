using System;
using System.IO;
using SkiaSharp;

namespace Vintagestory.API.Util;

public static class BitmapExtensions
{
	public unsafe static void SetPixels(this SKBitmap bmp, int[] pixels)
	{
		if (bmp.Width * bmp.Height != pixels.Length)
		{
			throw new ArgumentException("Pixel array must be width*height length");
		}
		fixed (int* ptr = pixels)
		{
			bmp.SetPixels((nint)ptr);
		}
	}

	public static void Save(this SKBitmap bmp, string filename)
	{
		using Stream fileStream = File.OpenWrite(filename);
		bmp.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fileStream);
	}
}
