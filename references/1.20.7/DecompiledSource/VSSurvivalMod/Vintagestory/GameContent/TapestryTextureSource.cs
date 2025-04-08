using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class TapestryTextureSource : ITexPositionSource
{
	public bool rotten;

	public string type;

	private int rotVariant;

	private ICoreClientAPI capi;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			AssetLocation texturePath;
			if (textureCode == "ropedcloth" || type == null || type == "")
			{
				texturePath = new AssetLocation("block/cloth/ropedcloth");
			}
			else
			{
				texturePath = new AssetLocation("block/cloth/tapestry/" + type);
			}
			AssetLocation cachedPath = texturePath.Clone();
			AssetLocation rotLoc = null;
			if (rotten)
			{
				rotLoc = new AssetLocation("block/cloth/tapestryoverlay/rotten" + rotVariant);
				cachedPath.Path = cachedPath.Path + "++" + rotLoc.Path;
			}
			capi.BlockTextureAtlas.GetOrInsertTexture(cachedPath, out var _, out var texpos, delegate
			{
				IAsset asset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
				if (asset == null)
				{
					capi.World.Logger.Warning("Tapestry type '{0}' defined texture '{1}', but no such texture found.", type, texturePath);
					return (IBitmap)null;
				}
				BitmapRef bitmapRef = asset.ToBitmap(capi);
				if (rotten)
				{
					BakedBitmap bakedBitmap = new BakedBitmap
					{
						Width = bitmapRef.Width,
						Height = bitmapRef.Height
					};
					bakedBitmap.TexturePixels = bitmapRef.Pixels;
					int[] array = capi.Assets.TryGet(rotLoc.WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"))?.ToBitmap(capi)?.Pixels;
					if (array == null)
					{
						throw new Exception(string.Concat("Texture file ", rotLoc, " is missing"));
					}
					for (int i = 0; i < bakedBitmap.TexturePixels.Length; i++)
					{
						bakedBitmap.TexturePixels[i] = ColorUtil.ColorOver(array[i], bakedBitmap.TexturePixels[i]);
					}
					return bakedBitmap;
				}
				return bitmapRef;
			});
			if (texpos == null)
			{
				return capi.BlockTextureAtlas.UnknownTexturePosition;
			}
			return texpos;
		}
	}

	public TapestryTextureSource(ICoreClientAPI capi, bool rotten, string type, int rotVariant = 0)
	{
		this.capi = capi;
		this.rotten = rotten;
		this.type = type;
		this.rotVariant = rotVariant;
	}
}
