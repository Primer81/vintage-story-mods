using System;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using SkiaSharp;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class GameWindowNative : GameWindow
{
	public GameWindowNative(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
		: base(gameWindowSettings, nativeWindowSettings)
	{
		GL.ClearColor(0f, 0f, 0f, 1f);
		GL.Clear(ClearBufferMask.ColorBufferBit);
		base.Context.SwapBuffers();
		if (RuntimeEnv.OS == OS.Mac)
		{
			return;
		}
		try
		{
			SKCodec skCodec = SKCodec.Create(Path.Combine(GamePaths.AssetsPath, "gameicon.ico"));
			byte[] skCodecPixels = skCodec.Pixels;
			byte[] iconData = new byte[skCodecPixels.Length];
			for (int i = 0; i < skCodecPixels.Length; i += 4)
			{
				iconData[i] = skCodecPixels[i + 2];
				iconData[i + 1] = skCodecPixels[i + 1];
				iconData[i + 2] = skCodecPixels[i];
				iconData[i + 3] = skCodecPixels[i + 3];
			}
			base.Icon = new WindowIcon(new Image(skCodec.Info.Height, skCodec.Info.Width, iconData));
		}
		catch (Exception)
		{
		}
	}
}
