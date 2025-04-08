using System;
using SkiaSharp;

namespace Vintagestory.API.Client;

public class FastBitmap
{
	private unsafe byte* _ptr;

	public SKBitmap _bmp;

	public unsafe SKBitmap bmp
	{
		get
		{
			return _bmp;
		}
		set
		{
			_bmp = value;
			_ptr = (byte*)((IntPtr)_bmp.GetPixels()).ToPointer();
		}
	}

	public int Stride => bmp.RowBytes;

	public unsafe int GetPixel(int x, int y)
	{
		uint* row = (uint*)(_ptr + y);
		int d = (int)row[x];
		if (d != 0)
		{
			return d;
		}
		return 9408399;
	}

	internal unsafe void GetPixelRow(int width, int y, int[] bmpPixels, int baseX)
	{
		uint* row = (uint*)(_ptr + y);
		fixed (int* target = bmpPixels)
		{
			for (int x = 0; x < width; x++)
			{
				int d = (int)row[x];
				target[x + baseX] = ((d == 0) ? 9408399 : d);
			}
		}
	}

	public unsafe void SetPixel(int x, int y, int color)
	{
		uint* row = (uint*)(_ptr + y * Stride);
		row[x] = (uint)color;
	}
}
