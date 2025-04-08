using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class TextureAtlas
{
	public Dictionary<int, QuadBoundsf> textureBounds;

	public bool Full;

	private TextureAtlasNode rootNode;

	private int[] atlasPixels;

	internal int textureId;

	public int width;

	public int height;

	private float subPixelPaddingx;

	private float subPixelPaddingy;

	public TextureAtlas(int width, int height, float subPixelPaddingx, float subPixelPaddingy)
	{
		atlasPixels = new int[width * height];
		this.subPixelPaddingx = subPixelPaddingx;
		this.subPixelPaddingy = subPixelPaddingy;
		this.width = width;
		this.height = height;
		rootNode = new TextureAtlasNode(0, 0, width, height);
	}

	public bool InsertTexture(int textureSubId, ICoreClientAPI capi, IAsset asset)
	{
		return InsertTexture(textureSubId, asset.ToBitmap(capi));
	}

	public bool InsertTexture(int textureSubId, IBitmap bmp, bool copyPixels = true)
	{
		if (copyPixels)
		{
			return InsertTexture(textureSubId, bmp.Width, bmp.Height, bmp.Pixels);
		}
		return InsertTexture(textureSubId, bmp.Width, bmp.Height, null);
	}

	public bool InsertTexture(int textureSubId, int width, int height, int[] pixels)
	{
		TextureAtlasNode node = rootNode.GetFreeNode(textureSubId, width, height);
		if (node != null)
		{
			node.textureSubId = textureSubId;
			int bX = node.bounds.x1;
			int bY = node.bounds.y1;
			int atlasWidth = AtlasWidth();
			if (pixels != null)
			{
				if (pixels.Length % 4 == 0)
				{
					int row = atlasWidth - width;
					int indexBase = bY * atlasWidth + bX - row;
					for (int i = 0; i < pixels.Length; i += 4)
					{
						if (i % width == 0)
						{
							indexBase += row;
						}
						atlasPixels[indexBase] = pixels[i];
						atlasPixels[indexBase + 1] = pixels[i + 1];
						atlasPixels[indexBase + 2] = pixels[i + 2];
						atlasPixels[indexBase + 3] = pixels[i + 3];
						indexBase += 4;
					}
				}
				else
				{
					for (int y = 0; y < height; y++)
					{
						int indexBase2 = (bY + y) * atlasWidth + bX;
						for (int x = 0; x < width; x++)
						{
							atlasPixels[indexBase2 + x] = pixels[y * width + x];
						}
					}
				}
			}
			return true;
		}
		return false;
	}

	public void UpdateTexture(TextureAtlasPosition tpos, int[] pixels)
	{
		int atlasWidth = AtlasWidth();
		int atlasHeight = AtlasHeight();
		int x = (int)(tpos.x1 * (float)atlasWidth);
		int y = (int)(tpos.y1 * (float)atlasHeight);
		int w = (int)Math.Round((tpos.x2 - tpos.x1 + 2f * subPixelPaddingx) * (float)atlasWidth);
		int h = (int)Math.Round((tpos.y2 - tpos.y1 + 2f * subPixelPaddingy) * (float)atlasHeight);
		for (int dx = 0; dx < w; dx++)
		{
			for (int dy = 0; dy < h; dy++)
			{
				atlasPixels[(dy + y) * atlasWidth + x + dx] = pixels[dy * w + dx];
			}
		}
	}

	public TextureAtlasPosition AllocateTextureSpace(int textureSubId, int width, int height)
	{
		TextureAtlasNode node = rootNode.GetFreeNode(textureSubId, width, height);
		if (node != null)
		{
			node.textureSubId = textureSubId;
			return new TextureAtlasPosition
			{
				x1 = (float)node.bounds.x1 / (float)AtlasWidth(),
				y1 = (float)node.bounds.y1 / (float)AtlasHeight(),
				x2 = (float)node.bounds.x2 / (float)AtlasWidth(),
				y2 = (float)node.bounds.y2 / (float)AtlasHeight()
			};
		}
		return null;
	}

	public bool FreeTextureSpace(int textureSubId)
	{
		return FreeTextureSpace(rootNode, textureSubId);
	}

	private bool FreeTextureSpace(TextureAtlasNode node, int textureSubId)
	{
		if (node.textureSubId == textureSubId)
		{
			node.textureSubId = null;
			return true;
		}
		if (node.left != null && FreeTextureSpace(node.left, textureSubId))
		{
			return true;
		}
		if (node.right != null && FreeTextureSpace(node.right, textureSubId))
		{
			return true;
		}
		return false;
	}

	public int AtlasWidth()
	{
		return width;
	}

	public int AtlasHeight()
	{
		return height;
	}

	public void Export(string filename, ClientMain game, int atlasTextureId)
	{
		ShaderProgramBase oldprog = ShaderProgramBase.CurrentShaderProgram;
		oldprog?.Stop();
		ShaderProgramGui prog = ShaderPrograms.Gui;
		prog.Use();
		FrameBufferRef fb = game.Platform.CreateFramebuffer(new FramebufferAttrs("PngExport", width, height)
		{
			Attachments = new FramebufferAttrsAttachment[1]
			{
				new FramebufferAttrsAttachment
				{
					AttachmentType = EnumFramebufferAttachment.ColorAttachment0,
					Texture = new RawTexture
					{
						Width = width,
						Height = height,
						PixelFormat = EnumTexturePixelFormat.Rgba,
						PixelInternalFormat = EnumTextureInternalFormat.Rgba8
					}
				}
			}
		});
		game.Platform.LoadFrameBuffer(fb);
		game.Platform.GlEnableDepthTest();
		game.Platform.GlDisableCullFace();
		game.Platform.GlToggleBlend(on: true);
		game.OrthoMode(width, height);
		float[] clearCol = new float[4];
		game.Platform.ClearFrameBuffer(fb, clearCol);
		game.api.renderapi.Render2DTexture(atlasTextureId, 0f, 0f, width, height, 50f);
		BitmapRef bitmap = game.Platform.GrabScreenshot(width, height, scaleScreenshot: false, flip: true, withAlpha: true);
		game.OrthoMode(game.Width, game.Height);
		game.Platform.UnloadFrameBuffer(fb);
		game.Platform.DisposeFrameBuffer(fb);
		if (File.Exists(filename + ".png"))
		{
			bitmap.Save(filename + "2.png");
		}
		else
		{
			bitmap.Save(filename + ".png");
		}
		prog.Stop();
		oldprog?.Use();
	}

	public int GetPixel(float x, float y)
	{
		int xi = (int)GameMath.Clamp(x * (float)width, 0f, width - 1);
		int yi = (int)GameMath.Clamp(y * (float)height, 0f, height - 1);
		return atlasPixels[yi * width + xi];
	}

	public LoadedTexture Upload(ClientMain game)
	{
		LoadedTexture tex = new LoadedTexture(game.api, 0, AtlasWidth(), AtlasHeight());
		game.Platform.LoadOrUpdateTextureFromBgra_DeferMipMap(atlasPixels, linearMag: false, 1, ref tex);
		textureId = tex.TextureId;
		return tex;
	}

	public void PopulateAtlasPositions(TextureAtlasPosition[] positions, int atlasNumber)
	{
		rootNode.PopulateAtlasPositions(positions, textureId, atlasNumber, AtlasWidth(), AtlasHeight(), subPixelPaddingx, subPixelPaddingy);
	}

	public void DrawToTexture(ClientPlatformAbstract platform, LoadedTexture texAtlas)
	{
		platform.LoadOrUpdateTextureFromBgra(atlasPixels, linearMag: false, 1, ref texAtlas);
	}

	public void DisposePixels()
	{
		atlasPixels = null;
	}

	public void ReinitPixels()
	{
		atlasPixels = new int[width * height];
	}
}
