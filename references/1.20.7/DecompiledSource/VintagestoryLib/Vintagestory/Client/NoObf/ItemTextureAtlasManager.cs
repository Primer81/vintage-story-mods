using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ItemTextureAtlasManager : TextureAtlasManager, IItemTextureAtlasAPI, ITextureAtlasAPI
{
	public ItemTextureAtlasManager(ClientMain game)
		: base(game)
	{
	}

	internal void CollectTextures(IList<Item> items, Dictionary<AssetLocation, UnloadableShape> shapes)
	{
		CompositeTexture unknown = new CompositeTexture(new AssetLocation("unknown"));
		AssetManager assetManager = game.Platform.AssetManager;
		foreach (Item item2 in items)
		{
			if (game.disposed)
			{
				return;
			}
			if (item2.Code == null)
			{
				continue;
			}
			ResolveTextureCodes(item2, shapes);
			if (item2.FirstTexture == null)
			{
				item2.Textures["all"] = unknown;
			}
			foreach (KeyValuePair<string, CompositeTexture> val3 in item2.Textures)
			{
				val3.Value.Bake(game.Platform.AssetManager);
				if (!ContainsKey(val3.Value.Baked.BakedName))
				{
					SetTextureLocation(new AssetLocationAndSource(val3.Value.Baked.BakedName, "Item ", item2.Code));
				}
			}
			if (!(item2.Shape?.Base != null) || !shapes.TryGetValue(item2.Shape.Base, out var shape))
			{
				continue;
			}
			Dictionary<string, AssetLocation> shapeTextures = shape.Textures;
			if (shapeTextures == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, AssetLocation> val2 in shapeTextures)
			{
				if (!ContainsKey(val2.Value))
				{
					SetTextureLocation(new AssetLocationAndSource(val2.Value, "Shape file ", item2.Shape.Base));
				}
				if (!item2.Textures.ContainsKey(val2.Key))
				{
					CompositeTexture ct = new CompositeTexture
					{
						Base = val2.Value.Clone()
					};
					item2.Textures[val2.Key] = ct;
					ct.Bake(assetManager);
				}
			}
		}
		foreach (Item item in items)
		{
			if (item == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, CompositeTexture> val in item.Textures)
			{
				val.Value.Baked.TextureSubId = textureNamesDict[val.Value.Baked.BakedName];
			}
		}
	}

	public TextureAtlasPosition GetPosition(Item item, string textureName = null, bool returnNullWhenMissing = false)
	{
		if (item.Shape == null || item.Shape.VoxelizeTexture)
		{
			CompositeTexture texture = item.FirstTexture;
			if (item.Shape?.Base != null && !item.Textures.TryGetValue(item.Shape.Base.Path.ToString(), out texture))
			{
				texture = item.FirstTexture;
			}
			int textureSubId = texture.Baked.TextureSubId;
			return TextureAtlasPositionsByTextureSubId[textureSubId];
		}
		return new TextureSource(game, base.Size, item)
		{
			returnNullWhenMissing = returnNullWhenMissing
		}[textureName];
	}

	public void ResolveTextureCodes(Item item, Dictionary<AssetLocation, UnloadableShape> itemShapes)
	{
		if (item.Shape?.Base == null)
		{
			return;
		}
		if (!itemShapes.TryGetValue(item.Shape.Base, out var baseShape))
		{
			game.Logger.VerboseDebug(string.Concat("Not found item shape ", item.Shape.Base, ", for item ", item.Code));
			return;
		}
		item.CheckTextures(game.Logger);
		if (baseShape.Textures == null)
		{
			return;
		}
		foreach (KeyValuePair<string, AssetLocation> val in baseShape.Textures)
		{
			string textureCode = val.Key;
			if (item.Textures.TryGetValue(textureCode, out var tex))
			{
				if (tex.Base.Path == "inherit")
				{
					tex.Base = val.Value.Clone();
				}
				if (tex.BlendedOverlays == null)
				{
					continue;
				}
				BlendedOverlayTexture[] BlendedOverlays = tex.BlendedOverlays;
				for (int i = 0; i < BlendedOverlays.Length; i++)
				{
					if (BlendedOverlays[i].Base.Path == "inherit")
					{
						BlendedOverlays[i].Base = val.Value.Clone();
					}
				}
			}
			else
			{
				item.Textures[textureCode] = new CompositeTexture
				{
					Base = val.Value.Clone()
				};
			}
		}
	}
}
