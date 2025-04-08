using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class BitmapExtensions
{
	public static void SetPixels(this Bitmap bmp, int[] pixels)
	{
		if (bmp.Width * bmp.Height != pixels.Length)
		{
			throw new ArgumentException("Pixel array must be width*height length");
		}
		Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
		BitmapData bitmapData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
		Marshal.Copy(pixels, 0, bitmapData.Scan0, pixels.Length);
		bmp.UnlockBits(bitmapData);
	}
}
