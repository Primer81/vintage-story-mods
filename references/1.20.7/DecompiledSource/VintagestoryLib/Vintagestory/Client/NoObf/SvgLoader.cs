using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Cairo;
using NanoSvg;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SvgLoader
{
	private readonly ICoreClientAPI capi;

	private nint rasterizer;

	public SvgLoader(ICoreClientAPI _capi)
	{
		capi = _capi;
		rasterizer = SvgNativeMethods.nsvgCreateRasterizer();
		if (rasterizer == IntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
	}

	public unsafe void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, int posx, int posy, int width = 0, int height = 0, int? color = 0)
	{
		byte[] array = rasterizeSvg(svgAsset, width, height, width, height, color);
		int len = intoSurface.Width * intoSurface.Height;
		nint ptr = intoSurface.DataPtr;
		fixed (byte* srcPointerbyte = array)
		{
			int* srcPointer = (int*)srcPointerbyte;
			int* dstPointer = (int*)((IntPtr)ptr).ToPointer();
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					int srcPixel = srcPointer[y * width + x];
					int dstPos = (posy + y) * intoSurface.Width + posx + x;
					if (dstPos >= 0 && dstPos < len)
					{
						int dstPixel = dstPointer[dstPos];
						dstPointer[dstPos] = ColorUtil.ColorOver(srcPixel, dstPixel);
					}
				}
			}
		}
		intoSurface.MarkDirty();
	}

	public unsafe void DrawSvg(IAsset svgAsset, ImageSurface intoSurface, Matrix matrix, int posx, int posy, int width = 0, int height = 0, int? color = 0)
	{
		byte[] array = rasterizeSvg(svgAsset, width, height, width, height, color);
		int len = intoSurface.Width * intoSurface.Height;
		nint ptr = intoSurface.DataPtr;
		fixed (byte* srcPointerbyte = array)
		{
			int* srcPointer = (int*)srcPointerbyte;
			int* dstPointer = (int*)((IntPtr)ptr).ToPointer();
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					int srcPixel = srcPointer[y * width + x];
					double rx = posx + x;
					double ry = posy + y;
					matrix.TransformPoint(ref rx, ref ry);
					int destx = (int)rx;
					int dstPos = (int)ry * intoSurface.Width + destx;
					if (dstPos >= 0 && dstPos < len)
					{
						int dstPixel = dstPointer[dstPos];
						dstPointer[dstPos] = ColorUtil.ColorOver(srcPixel, dstPixel);
					}
				}
			}
		}
		intoSurface.MarkDirty();
	}

	public unsafe LoadedTexture LoadSvg(IAsset svgAsset, int textureWidth, int textureHeight, int width = 0, int height = 0, int? color = 0)
	{
		int id = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, id);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
		fixed (byte* p = rasterizeSvg(svgAsset, textureWidth, textureHeight, width, height, color))
		{
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, textureWidth, textureHeight, 0, PixelFormat.Rgba, PixelType.UnsignedByte, (nint)p);
		}
		return new LoadedTexture(capi, id, width, height);
	}

	public unsafe byte[] rasterizeSvg(IAsset svgAsset, int textureWidth, int textureHeight, int width = 0, int height = 0, int? color = 0)
	{
		float scale = 1f;
		float dpi = 96f;
		byte[] cb = ((!color.HasValue) ? null : ColorUtil.ToRGBABytes(color.Value));
		int offX = 0;
		int offY = 0;
		if (rasterizer == IntPtr.Zero)
		{
			throw new ObjectDisposedException("SvgLoader is already disposed!");
		}
		if (svgAsset.Data == null)
		{
			throw new ArgumentNullException("Asset Data is null. Is the asset loaded?");
		}
		nint image = SvgNativeMethods.nsvgParse(svgAsset.ToText(), "px", dpi);
		if (image == IntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		if (SvgNativeMethods.nsvgImageGetSize(image) == IntPtr.Zero)
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}
		NsvgSize size = Marshal.PtrToStructure<NsvgSize>(SvgNativeMethods.nsvgImageGetSize(image));
		if (width == 0 && height == 0)
		{
			width = (int)(size.width * scale);
			height = (int)(size.height * scale);
		}
		else if (width == 0)
		{
			scale = (float)height / size.height;
			width = (int)(size.width * scale);
		}
		else if (height == 0)
		{
			scale = (float)width / size.width;
			height = (int)(size.height * scale);
		}
		else
		{
			float scaleX = (float)width / size.width;
			float scaleY = (float)height / size.height;
			scale = ((scaleX < scaleY) ? scaleX : scaleY);
			offX = (int)((float)textureWidth - size.width * scale) / 2;
			offY = (int)((float)textureHeight - size.height * scale) / 2;
		}
		byte[] buffer = new byte[textureWidth * textureHeight * 4];
		fixed (byte* p = buffer)
		{
			SvgNativeMethods.nsvgRasterize(rasterizer, image, offX, offY, scale, (nint)p, textureWidth, textureHeight, textureWidth * 4);
			if (cb != null)
			{
				for (int i = 0; i < buffer.Length - 1; i += 4)
				{
					float a = (float)(int)buffer[i + 3] / 255f;
					buffer[i] = (byte)(a * (float)(int)cb[0]);
					buffer[i + 1] = (byte)(a * (float)(int)cb[1]);
					buffer[i + 2] = (byte)(a * (float)(int)cb[2]);
				}
			}
		}
		SvgNativeMethods.nsvgDelete(image);
		return buffer;
	}

	~SvgLoader()
	{
		if (rasterizer != IntPtr.Zero)
		{
			SvgNativeMethods.nsvgDeleteRasterizer(rasterizer);
			rasterizer = IntPtr.Zero;
		}
	}
}
