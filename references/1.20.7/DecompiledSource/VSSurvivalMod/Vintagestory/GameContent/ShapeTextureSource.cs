using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ShapeTextureSource : ITexPositionSource
{
	private ICoreClientAPI capi;

	private Shape shape;

	private string filenameForLogging;

	public Dictionary<string, CompositeTexture> textures = new Dictionary<string, CompositeTexture>();

	public TextureAtlasPosition firstTexPos;

	private HashSet<AssetLocation> missingTextures = new HashSet<AssetLocation>();

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			int textureSubId;
			TextureAtlasPosition texPos;
			if (textures.TryGetValue(textureCode, out var ctex))
			{
				capi.BlockTextureAtlas.GetOrInsertTexture(ctex, out textureSubId, out texPos);
			}
			else
			{
				shape.Textures.TryGetValue(textureCode, out var texturePath);
				if (texturePath == null)
				{
					if (!missingTextures.Contains(texturePath))
					{
						capi.Logger.Warning("Shape {0} has an element using texture code {1}, but no such texture exists", filenameForLogging, textureCode);
						missingTextures.Add(texturePath);
					}
					return capi.BlockTextureAtlas.UnknownTexturePosition;
				}
				capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out textureSubId, out texPos);
			}
			if (texPos == null)
			{
				return capi.BlockTextureAtlas.UnknownTexturePosition;
			}
			if (firstTexPos == null)
			{
				firstTexPos = texPos;
			}
			return texPos;
		}
	}

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public ShapeTextureSource(ICoreClientAPI capi, Shape shape, string filenameForLogging)
	{
		this.capi = capi;
		this.shape = shape;
		this.filenameForLogging = filenameForLogging;
	}
}
