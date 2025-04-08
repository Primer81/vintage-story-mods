using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.NoObf;

public class EntityTextureAtlasManager : TextureAtlasManager, ITextureAtlasAPI
{
	public EntityTextureAtlasManager(ClientMain game)
		: base(game)
	{
	}

	internal void CollectTextures(List<EntityProperties> entityClasses)
	{
		CompositeTexture unknown = new CompositeTexture(new AssetLocation("unknown"));
		foreach (EntityProperties entityType in entityClasses)
		{
			if (game.disposed)
			{
				return;
			}
			if (entityType == null || entityType.Client == null)
			{
				continue;
			}
			EntityClientProperties clientConf = entityType.Client;
			IDictionary<string, CompositeTexture> collectedTextures = new FastSmallDictionary<string, CompositeTexture>(1);
			if (clientConf.Textures == null && clientConf.LoadedShape?.Textures == null)
			{
				clientConf.Textures["all"] = unknown;
			}
			if (clientConf.LoadedShape?.Textures != null)
			{
				LoadShapeTextures(collectedTextures, clientConf.LoadedShape, clientConf.Shape);
			}
			if (clientConf.LoadedAlternateShapes != null)
			{
				for (int k = 0; k < clientConf.LoadedAlternateShapes.Length; k++)
				{
					Shape shape = clientConf.LoadedAlternateShapes[k];
					CompositeShape cshape = clientConf.Shape.Alternates[k];
					if (shape?.Textures != null)
					{
						LoadShapeTextures(collectedTextures, shape, cshape);
					}
				}
			}
			ResolveTextureCodes(clientConf, clientConf.LoadedShape);
			if (clientConf.Textures != null)
			{
				foreach (KeyValuePair<string, CompositeTexture> val2 in clientConf.Textures)
				{
					val2.Value.Bake(game.AssetManager);
					if (val2.Value.Baked.BakedVariants != null)
					{
						for (int j = 0; j < val2.Value.Baked.BakedVariants.Length; j++)
						{
							GetOrAddTextureLocation(new AssetLocationAndSource(val2.Value.Baked.BakedVariants[j].BakedName, "Entity type ", entityType.Code));
						}
					}
					GetOrAddTextureLocation(new AssetLocationAndSource(val2.Value.Base, "Entity type ", entityType.Code));
					collectedTextures[val2.Key] = val2.Value;
				}
			}
			clientConf.Textures = collectedTextures;
		}
		foreach (EntityProperties entityClass in entityClasses)
		{
			if (entityClass == null || entityClass.Client == null)
			{
				continue;
			}
			foreach (KeyValuePair<string, CompositeTexture> val in entityClass.Client.Textures)
			{
				BakedCompositeTexture bct = val.Value.Baked;
				bct.TextureSubId = textureNamesDict[val.Value.Baked.BakedName];
				if (bct.BakedVariants != null)
				{
					for (int i = 0; i < bct.BakedVariants.Length; i++)
					{
						bct.BakedVariants[i].TextureSubId = textureNamesDict[bct.BakedVariants[i].BakedName];
					}
				}
			}
		}
	}

	private void LoadShapeTextures(IDictionary<string, CompositeTexture> collectedTextures, Shape shape, CompositeShape cshape)
	{
		foreach (KeyValuePair<string, AssetLocation> val in shape.Textures)
		{
			CompositeTexture ctex = new CompositeTexture
			{
				Base = val.Value
			};
			ctex.Bake(game.AssetManager);
			if (ctex.Baked.BakedVariants != null)
			{
				for (int i = 0; i < ctex.Baked.BakedVariants.Length; i++)
				{
					GetOrAddTextureLocation(new AssetLocationAndSource(ctex.Baked.BakedVariants[i].BakedName, "Shape file ", cshape.Base));
				}
			}
			else
			{
				GetOrAddTextureLocation(new AssetLocationAndSource(val.Value, "Shape file ", cshape.Base));
				collectedTextures[val.Key] = ctex;
			}
		}
	}

	public void ResolveTextureCodes(EntityClientProperties typeClient, Shape shape)
	{
		if (typeClient.Textures.ContainsKey("all"))
		{
			LoadShapeTextureCodes(shape);
		}
		ResolveTextureDict((FastSmallDictionary<string, CompositeTexture>)typeClient.Textures);
	}
}
