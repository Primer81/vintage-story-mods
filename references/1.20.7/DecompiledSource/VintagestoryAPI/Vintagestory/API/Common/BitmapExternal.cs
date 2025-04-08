using System;
using System.IO;
using SkiaSharp;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class BitmapExternal : BitmapRef
{
	public SKBitmap bmp;

	public override int Height => bmp.Height;

	public override int Width => bmp.Width;

	public override int[] Pixels => Array.ConvertAll(bmp.Pixels, (SKColor p) => (int)(uint)p);

	public nint PixelsPtrAndLock => bmp.GetPixels();

	public BitmapExternal()
	{
	}

	public BitmapExternal(int width, int height)
	{
		bmp = new SKBitmap(width, height);
	}

	public BitmapExternal(MemoryStream ms, ILogger logger, AssetLocation loc = null)
	{
		try
		{
			SKBitmap decode = SKBitmap.Decode(ms);
			bmp = new SKBitmap(decode.Width, decode.Height, SKColorType.Bgra8888, decode.Info.AlphaType);
			using SKCanvas canvas = new SKCanvas(bmp);
			canvas.DrawBitmap(decode, 0f, 0f);
		}
		catch (Exception e)
		{
			if (loc != null)
			{
				logger.Error("Failed loading bitmap from png file {0}. Will default to an empty 1x1 bitmap.", loc);
				logger.Error(e);
			}
			else
			{
				logger.Error("Failed loading bitmap. Will default to an empty 1x1 bitmap.");
				logger.Error(e);
			}
			bmp = new SKBitmap(1, 1);
			bmp.SetPixel(0, 0, SKColors.Orange);
		}
	}

	public BitmapExternal(string filePath)
	{
		try
		{
			SKBitmap decode = SKBitmap.Decode(filePath);
			bmp = new SKBitmap(decode.Width, decode.Height, SKColorType.Bgra8888, decode.Info.AlphaType);
			using SKCanvas canvas = new SKCanvas(bmp);
			canvas.DrawBitmap(decode, 0f, 0f);
		}
		catch (Exception)
		{
			bmp = new SKBitmap(1, 1);
			bmp.SetPixel(0, 0, SKColors.Orange);
		}
	}

	public BitmapExternal(Stream stream)
	{
		try
		{
			SKBitmap decode = SKBitmap.Decode(stream);
			bmp = new SKBitmap(decode.Width, decode.Height, SKColorType.Bgra8888, decode.Info.AlphaType);
			using SKCanvas canvas = new SKCanvas(bmp);
			canvas.DrawBitmap(decode, 0f, 0f);
		}
		catch (Exception)
		{
			bmp = new SKBitmap(1, 1);
			bmp.SetPixel(0, 0, SKColors.Orange);
		}
	}

	/// <summary>
	/// Create a BitmapExternal from a byte array
	/// </summary>
	/// <param name="data"></param>
	/// <param name="dataLength"></param>
	/// <param name="logger"></param>
	public BitmapExternal(byte[] data, int dataLength, ILogger logger)
	{
		try
		{
			if (RuntimeEnv.OS == OS.Mac)
			{
				SKBitmap decode = SKBitmap.Decode(new ReadOnlySpan<byte>(data, 0, dataLength));
				bmp = new SKBitmap(decode.Width, decode.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
				using SKCanvas canvas = new SKCanvas(bmp);
				canvas.DrawBitmap(decode, 0f, 0f);
				return;
			}
			bmp = Decode(new ReadOnlySpan<byte>(data, 0, dataLength));
		}
		catch (Exception ex)
		{
			logger.Error("Failed loading bitmap from data. Will default to an empty 1x1 bitmap.");
			logger.Error(ex);
			bmp = new SKBitmap(1, 1);
			bmp.SetPixel(0, 0, SKColors.Orange);
		}
	}

	public unsafe static SKBitmap Decode(ReadOnlySpan<byte> buffer)
	{
		fixed (byte* address = buffer)
		{
			using SKData data = SKData.Create((nint)address, buffer.Length);
			using SKCodec codec = SKCodec.Create(data);
			SKImageInfo bitmapInfo = codec.Info;
			bitmapInfo.AlphaType = SKAlphaType.Unpremul;
			return SKBitmap.Decode(codec, bitmapInfo);
		}
	}

	public override void Dispose()
	{
		bmp.Dispose();
	}

	public override void Save(string filename)
	{
		bmp.Save(filename);
	}

	/// <summary>
	/// Retrives the ARGB value from given coordinate
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <returns></returns>
	public override SKColor GetPixel(int x, int y)
	{
		return bmp.GetPixel(x, y);
	}

	/// <summary>
	/// Retrives the ARGB value from given coordinate using normalized coordinates (0..1)
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <returns></returns>
	public override SKColor GetPixelRel(float x, float y)
	{
		return bmp.GetPixel((int)Math.Min(bmp.Width - 1, x * (float)bmp.Width), (int)Math.Min(bmp.Height - 1, y * (float)bmp.Height));
	}

	public unsafe override void MulAlpha(int alpha = 255)
	{
		int len = Width * Height;
		float af = (float)alpha / 255f;
		byte* colp = (byte*)((IntPtr)bmp.GetPixels()).ToPointer();
		for (int i = 0; i < len; i++)
		{
			int a = colp[3];
			*colp = (byte)((float)(int)(*colp) * af);
			colp[1] = (byte)((float)(int)colp[1] * af);
			colp[2] = (byte)((float)(int)colp[2] * af);
			colp[3] = (byte)((float)a * af);
			colp += 4;
		}
	}

	public override int[] GetPixelsTransformed(int rot = 0, int mulAlpha = 255)
	{
		int[] bmpPixels = new int[Width * Height];
		int width = bmp.Width;
		int height = bmp.Height;
		FastBitmap fastBitmap = new FastBitmap();
		fastBitmap.bmp = bmp;
		int stride = fastBitmap.Stride;
		switch (rot)
		{
		case 0:
		{
			for (int y2 = 0; y2 < height; y2++)
			{
				fastBitmap.GetPixelRow(width, y2 * stride, bmpPixels, y2 * width);
			}
			break;
		}
		case 90:
		{
			for (int x = 0; x < width; x++)
			{
				int baseY = x * width;
				for (int y = 0; y < height; y++)
				{
					bmpPixels[y + baseY] = fastBitmap.GetPixel(width - x - 1, y * stride);
				}
			}
			break;
		}
		case 180:
		{
			for (int y4 = 0; y4 < height; y4++)
			{
				int baseX = y4 * width;
				int yStride = (height - y4 - 1) * stride;
				for (int x3 = 0; x3 < width; x3++)
				{
					bmpPixels[x3 + baseX] = fastBitmap.GetPixel(width - x3 - 1, yStride);
				}
			}
			break;
		}
		case 270:
		{
			for (int x2 = 0; x2 < width; x2++)
			{
				int baseY2 = x2 * width;
				for (int y3 = 0; y3 < height; y3++)
				{
					bmpPixels[y3 + baseY2] = fastBitmap.GetPixel(x2, (height - y3 - 1) * stride);
				}
			}
			break;
		}
		}
		if (mulAlpha != 255)
		{
			float alpaP = (float)mulAlpha / 255f;
			int clearAlpha = 16777215;
			for (int i = 0; i < bmpPixels.Length; i++)
			{
				int col = bmpPixels[i];
				uint curAlpha = (uint)col >> 24;
				col &= clearAlpha;
				bmpPixels[i] = col | ((int)((float)curAlpha * alpaP) << 24);
			}
		}
		return bmpPixels;
	}
}
