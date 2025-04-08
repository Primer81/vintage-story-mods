using System;
using System.IO;
using CompactExifLib;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Desktop;
using SkiaSharp;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.ClientNative;

public class Screenshot
{
	public GameWindow d_GameWindow;

	public string SaveScreenshot(ClientPlatformAbstract platform, Size2i size, string path = null, string filename = null, bool withAlpha = false, bool flip = true, string metadataStr = null)
	{
		string text = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
		if (path == null)
		{
			if (!Directory.Exists(GamePaths.Screenshots))
			{
				Directory.CreateDirectory(GamePaths.Screenshots);
			}
			path = GamePaths.Screenshots;
		}
		if (filename == null)
		{
			filename = Path.Combine(path, text + ".png");
		}
		if (!GameDatabase.HaveWriteAccessFolder(path))
		{
			throw new Exception("No write access to " + path);
		}
		using (SKBitmap bitmap = GrabScreenshot(size, ClientSettings.ScaleScreenshot, flip, withAlpha))
		{
			bitmap.Save(filename);
		}
		if (metadataStr != null)
		{
			ExifData exifData = new ExifData(filename);
			exifData.SetTagValue(ExifTag.Make, metadataStr, StrCoding.UsAscii);
			exifData.SetTagValue(ExifTag.ImageDescription, metadataStr, StrCoding.Utf8);
			exifData.Save(filename);
		}
		return Path.GetFileName(filename);
	}

	public SKBitmap GrabScreenshot(Size2i size, bool scaleScreenshot, bool flip, bool withAlpha)
	{
		SKBitmap bitmap = new SKBitmap(new SKImageInfo(size.Width, size.Height, SKColorType.Bgra8888, (!withAlpha) ? SKAlphaType.Opaque : SKAlphaType.Unpremul));
		GL.ReadPixels(0, 0, size.Width, size.Height, PixelFormat.Bgra, PixelType.UnsignedByte, bitmap.GetPixels());
		if (scaleScreenshot)
		{
			bitmap = bitmap.Resize(new SKImageInfo(d_GameWindow.ClientSize.X, d_GameWindow.ClientSize.Y), SKFilterQuality.High);
		}
		if (!flip)
		{
			return bitmap;
		}
		SKBitmap rotated = new SKBitmap(bitmap.Width, bitmap.Height, bitmap.ColorType, (!withAlpha) ? SKAlphaType.Opaque : SKAlphaType.Unpremul);
		using SKCanvas surface = new SKCanvas(rotated);
		surface.Translate(bitmap.Width, bitmap.Height);
		surface.RotateDegrees(180f);
		surface.Scale(-1f, 1f, (float)bitmap.Width / 2f, 0f);
		surface.DrawBitmap(bitmap, 0f, 0f);
		return rotated;
	}
}
