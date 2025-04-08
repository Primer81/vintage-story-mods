using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// Helper class for implementors of ITexPositionSource
/// </summary>
public class ContainedTextureSource : ITexPositionSource
{
	private ITextureAtlasAPI targetAtlas;

	private ICoreClientAPI capi;

	public Dictionary<string, AssetLocation> Textures = new Dictionary<string, AssetLocation>();

	private string sourceForErrorLogging;

	public Size2i AtlasSize => targetAtlas.Size;

	public TextureAtlasPosition this[string textureCode] => getOrCreateTexPos(Textures[textureCode]);

	public ContainedTextureSource(ICoreClientAPI capi, ITextureAtlasAPI targetAtlas, Dictionary<string, AssetLocation> textures, string sourceForErrorLogging)
	{
		this.capi = capi;
		this.targetAtlas = targetAtlas;
		Textures = textures;
		this.sourceForErrorLogging = sourceForErrorLogging;
	}

	protected TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
	{
		TextureAtlasPosition texpos = targetAtlas[texturePath];
		if (texpos == null)
		{
			IAsset texAsset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
			if (texAsset != null)
			{
				targetAtlas.GetOrInsertTexture(texturePath, out var _, out texpos, () => texAsset.ToBitmap(capi));
				if (texpos == null)
				{
					capi.World.Logger.Error("{0}, require texture {1} which exists, but unable to upload it or allocate space", sourceForErrorLogging, texturePath);
					texpos = targetAtlas.UnknownTexturePosition;
				}
			}
			else
			{
				capi.World.Logger.Error("{0}, require texture {1}, but no such texture found.", sourceForErrorLogging, texturePath);
				texpos = targetAtlas.UnknownTexturePosition;
			}
		}
		return texpos;
	}
}
