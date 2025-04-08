using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class MultiChunkMapComponent : MapComponent
{
	public static int ChunkLen = 3;

	public static LoadedTexture tmpTexture;

	public float renderZ = 50f;

	public Vec2i chunkCoord;

	public LoadedTexture Texture;

	private static int[] emptyPixels;

	private Vec3d worldPos;

	private Vec2f viewPos = new Vec2f();

	private bool[,] chunkSet = new bool[ChunkLen, ChunkLen];

	private const int chunksize = 32;

	public float TTL = MaxTTL;

	public static float MaxTTL = 15f;

	private Vec2i tmpVec = new Vec2i();

	public bool AnyChunkSet
	{
		get
		{
			for (int dx = 0; dx < ChunkLen; dx++)
			{
				for (int dz = 0; dz < ChunkLen; dz++)
				{
					if (chunkSet[dx, dz])
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public bool IsChunkSet(int dx, int dz)
	{
		if (dx < 0 || dz < 0)
		{
			return false;
		}
		return chunkSet[dx, dz];
	}

	public MultiChunkMapComponent(ICoreClientAPI capi, Vec2i baseChunkCord)
		: base(capi)
	{
		chunkCoord = baseChunkCord;
		worldPos = new Vec3d(baseChunkCord.X * 32, 0.0, baseChunkCord.Y * 32);
		if (emptyPixels == null)
		{
			int num = ChunkLen * 32;
			emptyPixels = new int[num * num];
		}
	}

	public void setChunk(int dx, int dz, int[] pixels)
	{
		if (dx < 0 || dx >= ChunkLen || dz < 0 || dz >= ChunkLen)
		{
			throw new ArgumentOutOfRangeException("dx/dz must be within [0," + (ChunkLen - 1) + "]");
		}
		if (tmpTexture == null || tmpTexture.Disposed)
		{
			tmpTexture = new LoadedTexture(capi, 0, 32, 32);
		}
		if (Texture == null || Texture.Disposed)
		{
			int size = ChunkLen * 32;
			Texture = new LoadedTexture(capi, 0, size, size);
			capi.Render.LoadOrUpdateTextureFromRgba(emptyPixels, linearMag: false, 0, ref Texture);
		}
		capi.Render.LoadOrUpdateTextureFromRgba(pixels, linearMag: false, 0, ref tmpTexture);
		capi.Render.GlToggleBlend(blend: false);
		capi.Render.GLDisableDepthTest();
		capi.Render.RenderTextureIntoTexture(tmpTexture, 0f, 0f, 32f, 32f, Texture, 32 * dx, 32 * dz);
		capi.Render.BindTexture2d(Texture.TextureId);
		capi.Render.GlGenerateTex2DMipmaps();
		chunkSet[dx, dz] = true;
	}

	public void unsetChunk(int dx, int dz)
	{
		if (dx < 0 || dx >= ChunkLen || dz < 0 || dz >= ChunkLen)
		{
			throw new ArgumentOutOfRangeException("dx/dz must be within [0," + (ChunkLen - 1) + "]");
		}
		chunkSet[dx, dz] = false;
	}

	public override void Render(GuiElementMap map, float dt)
	{
		map.TranslateWorldPosToViewPos(worldPos, ref viewPos);
		capi.Render.Render2DTexture(Texture.TextureId, (int)(map.Bounds.renderX + (double)viewPos.X), (int)(map.Bounds.renderY + (double)viewPos.Y), (int)((float)Texture.Width * map.ZoomLevel), (int)((float)Texture.Height * map.ZoomLevel), renderZ);
	}

	public override void Dispose()
	{
		base.Dispose();
	}

	public void ActuallyDispose()
	{
		Texture.Dispose();
	}

	public bool IsVisible(HashSet<Vec2i> curVisibleChunks)
	{
		for (int dx = 0; dx < ChunkLen; dx++)
		{
			for (int dz = 0; dz < ChunkLen; dz++)
			{
				tmpVec.Set(chunkCoord.X + dx, chunkCoord.Y + dz);
				if (curVisibleChunks.Contains(tmpVec))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static void DisposeStatic()
	{
		tmpTexture?.Dispose();
		emptyPixels = null;
		tmpTexture = null;
	}
}
