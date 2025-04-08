using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class MSShapeFromAttrCacheHelper : ModSystem
{
	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public static bool IsInCache(ICoreClientAPI capi, Block block, IShapeTypeProps cprops, string overrideTextureCode)
	{
		IDictionary<string, CompositeTexture> blockTextures = (block as BlockShapeFromAttributes).blockTextures;
		Shape shape = cprops.ShapeResolved;
		if (shape == null)
		{
			return false;
		}
		if (shape.Textures != null)
		{
			foreach (KeyValuePair<string, AssetLocation> val3 in shape.Textures)
			{
				if (capi.BlockTextureAtlas[val3.Value] == null)
				{
					return false;
				}
			}
		}
		if (blockTextures != null)
		{
			foreach (KeyValuePair<string, CompositeTexture> val2 in blockTextures)
			{
				if (val2.Value.Baked == null)
				{
					val2.Value.Bake(capi.Assets);
				}
				if (capi.BlockTextureAtlas[val2.Value.Baked.BakedName] == null)
				{
					return false;
				}
			}
		}
		if (cprops.Textures != null)
		{
			foreach (KeyValuePair<string, CompositeTexture> val in cprops.Textures)
			{
				BakedCompositeTexture baked = val.Value.Baked ?? CompositeTexture.Bake(capi.Assets, val.Value);
				if (capi.BlockTextureAtlas[baked.BakedName] == null)
				{
					return false;
				}
			}
		}
		if (overrideTextureCode != null && cprops.TextureFlipCode != null && (block as BlockShapeFromAttributes).OverrideTextureGroups[cprops.TextureFlipGroupCode].TryGetValue(overrideTextureCode, out var ctex))
		{
			if (ctex.Baked == null)
			{
				ctex.Bake(capi.Assets);
			}
			if (capi.BlockTextureAtlas[ctex.Baked.BakedName] == null)
			{
				return false;
			}
		}
		return true;
	}
}
