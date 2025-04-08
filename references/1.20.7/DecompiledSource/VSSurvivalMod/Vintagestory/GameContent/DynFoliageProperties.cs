using System.Collections.Generic;
using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

public class DynFoliageProperties
{
	public string TexturesBasePath;

	public Dictionary<string, CompositeTexture> Textures;

	public CompositeTexture LeafParticlesTexture;

	public CompositeTexture BlossomParticlesTexture;

	public string SeasonColorMap = "seasonalFoliage";

	public string ClimateColorMap = "climatePlantTint";

	public void Rebase(DynFoliageProperties props)
	{
		if (TexturesBasePath == null)
		{
			TexturesBasePath = props.TexturesBasePath;
		}
		if (Textures == null)
		{
			Textures = new Dictionary<string, CompositeTexture>();
			foreach (KeyValuePair<string, CompositeTexture> val in props.Textures)
			{
				Textures[val.Key] = val.Value.Clone();
			}
		}
		LeafParticlesTexture = props.LeafParticlesTexture?.Clone();
		BlossomParticlesTexture = props.BlossomParticlesTexture?.Clone();
	}

	public TextureAtlasPosition GetOrLoadTexture(ICoreClientAPI capi, string key)
	{
		if (Textures.TryGetValue(key, out var ctex))
		{
			if (ctex.Baked != null)
			{
				int textureSubId = ctex.Baked.TextureSubId;
				if (textureSubId > 0)
				{
					return capi.BlockTextureAtlas.Positions[textureSubId];
				}
			}
			ctex.Bake(capi.Assets);
			capi.BlockTextureAtlas.GetOrInsertTexture(ctex.Baked.BakedName, out var newSubId, out var texPos);
			ctex.Baked.TextureSubId = newSubId;
			return texPos;
		}
		return null;
	}
}
