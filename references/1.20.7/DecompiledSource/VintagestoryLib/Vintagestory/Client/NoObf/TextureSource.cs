using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class TextureSource : ITexPositionSource
{
	private ClientMain game;

	public Size2i atlasSize;

	public Entity entity;

	public Block block;

	public Item item;

	private MiniDictionary textureCodeToIdMapping;

	public bool isDecalUv;

	public bool returnNullWhenMissing;

	internal CompositeShape blockShape;

	public TextureAtlasManager atlasMgr;

	public Size2i AtlasSize => atlasSize;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (textureCode == null)
			{
				return atlasMgr.UnknownTexturePos;
			}
			int textureSubId = textureCodeToIdMapping[textureCode];
			TextureAtlasPosition texPos;
			if (textureSubId == -1 && (returnNullWhenMissing || (textureSubId = textureCodeToIdMapping["all"]) == -1))
			{
				if (returnNullWhenMissing)
				{
					return null;
				}
				if (block != null)
				{
					game.Platform.Logger.Error(string.Concat("Missing mapping for texture code #", textureCode, " during shape tesselation of block ", block.Code, " using shape ", block.Shape.Base, ", or one of its alternates"));
				}
				if (item != null)
				{
					game.Platform.Logger.Error(string.Concat("Missing mapping for texture code #", textureCode, " during shape tesselation of item ", item.Code, " using shape ", item.Shape.Base));
				}
				if (entity != null)
				{
					game.Platform.Logger.Error(string.Concat("Missing mapping for texture code #", textureCode, " during shape tesselation of entity ", entity.Code, " using shape ", entity.Properties.Client.Shape.Base, ", or one of its alternates"));
				}
				texPos = atlasMgr.UnknownTexturePos;
			}
			else
			{
				texPos = atlasMgr.TextureAtlasPositionsByTextureSubId[textureSubId];
			}
			if (isDecalUv)
			{
				return new TextureAtlasPosition
				{
					atlasNumber = 0,
					atlasTextureId = 0,
					x1 = 0f,
					y1 = 0f,
					x2 = (texPos.x2 - texPos.x1) * (float)atlasMgr.Size.Width / (float)atlasSize.Width,
					y2 = (texPos.y2 - texPos.y1) * (float)atlasMgr.Size.Height / (float)atlasSize.Height
				};
			}
			return texPos;
		}
	}

	public TextureSource(ClientMain game, Size2i atlasSize, Block block, bool forInventory = false)
	{
		this.game = game;
		this.atlasSize = atlasSize;
		this.block = block;
		atlasMgr = game.BlockAtlasManager;
		try
		{
			IDictionary<string, CompositeTexture> textures = block.Textures;
			if (forInventory)
			{
				textures = block.TexturesInventory;
			}
			textureCodeToIdMapping = new MiniDictionary(textures.Count);
			foreach (KeyValuePair<string, CompositeTexture> val in textures)
			{
				textureCodeToIdMapping[val.Key] = val.Value.Baked.TextureSubId;
			}
		}
		catch (Exception)
		{
			game.Logger.Error("Unable to initialize TextureSource for block {0}. Will crash now.", block?.Code);
			throw;
		}
	}

	public TextureSource(ClientMain game, Size2i atlasSize, Item item)
	{
		this.game = game;
		this.atlasSize = atlasSize;
		this.item = item;
		atlasMgr = game.ItemAtlasManager;
		Dictionary<string, CompositeTexture> textures = item.Textures;
		textureCodeToIdMapping = new MiniDictionary(textures.Count);
		foreach (KeyValuePair<string, CompositeTexture> val in textures)
		{
			textureCodeToIdMapping[val.Key] = val.Value.Baked.TextureSubId;
		}
	}

	public TextureSource(ClientMain game, Size2i atlasSize, Entity entity, Dictionary<string, CompositeTexture> extraTextures = null, int altTextureNumber = 0)
	{
		this.game = game;
		this.atlasSize = atlasSize;
		this.entity = entity;
		atlasMgr = game.EntityAtlasManager;
		IDictionary<string, CompositeTexture> textures = entity.Properties.Client.Textures;
		textureCodeToIdMapping = new MiniDictionary(textures.Count);
		foreach (KeyValuePair<string, CompositeTexture> val2 in textures)
		{
			BakedCompositeTexture bct = val2.Value.Baked;
			if (bct.BakedVariants == null)
			{
				textureCodeToIdMapping[val2.Key] = bct.TextureSubId;
				continue;
			}
			BakedCompositeTexture bctVariant = bct.BakedVariants[altTextureNumber % bct.BakedVariants.Length];
			textureCodeToIdMapping[val2.Key] = bctVariant.TextureSubId;
		}
		if (extraTextures == null)
		{
			return;
		}
		foreach (KeyValuePair<string, CompositeTexture> val in extraTextures)
		{
			extraTextures[val.Key] = val.Value;
		}
	}

	public TextureSource(ClientMain game, Size2i atlasSize, Block block, int altTextureNumber)
		: this(game, atlasSize, block)
	{
		if (altTextureNumber == -1)
		{
			return;
		}
		foreach (KeyValuePair<string, CompositeTexture> val in block.Textures)
		{
			BakedCompositeTexture bct = val.Value.Baked;
			if (bct.BakedVariants != null)
			{
				BakedCompositeTexture bctVariant = bct.BakedVariants[altTextureNumber % bct.BakedVariants.Length];
				textureCodeToIdMapping[val.Key] = bctVariant.TextureSubId;
			}
		}
	}

	public void UpdateVariant(Block block, int altTextureNumber)
	{
		foreach (KeyValuePair<string, CompositeTexture> val in block.Textures)
		{
			BakedCompositeTexture[] variants = val.Value.Baked.BakedVariants;
			if (variants != null && variants.Length != 0)
			{
				textureCodeToIdMapping[val.Key] = variants[altTextureNumber % variants.Length].TextureSubId;
			}
		}
	}
}
