using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class BlockTextureAtlasManager : TextureAtlasManager, IBlockTextureAtlasAPI, ITextureAtlasAPI
{
	private List<KeyValuePair<string, CompositeTexture>> replacements = new List<KeyValuePair<string, CompositeTexture>>();

	public BlockTextureAtlasManager(ClientMain game)
		: base(game)
	{
	}

	internal void CollectTextures(IList<Block> blocks, OrderedDictionary<AssetLocation, UnloadableShape> shapes)
	{
		Block snowLayerBlock = null;
		int snowTextureSubId = 0;
		AssetLocation snowlayerloc = new AssetLocation("snowlayer-1");
		Dictionary<AssetLocation, CompositeTexture> shapeTexturesCache = new Dictionary<AssetLocation, CompositeTexture>();
		foreach (Block block4 in blocks)
		{
			if (!(block4.Code == null))
			{
				block4.EnsureValidTextures(game.Logger);
				ResolveTextureCodes(block4, shapes, shapeTexturesCache);
			}
		}
		foreach (Block block3 in blocks)
		{
			if (game.disposed)
			{
				return;
			}
			if (!(block3.Code == null) && block3.DrawType == EnumDrawType.TopSoil)
			{
				collectTexturesForBlock(block3, shapes);
			}
		}
		foreach (Block block2 in blocks)
		{
			if (game.disposed)
			{
				return;
			}
			if (!(block2.Code == null) && block2.DrawType != EnumDrawType.TopSoil)
			{
				collectTexturesForBlock(block2, shapes);
				if (snowLayerBlock == null && block2.Code.Equals(snowlayerloc))
				{
					snowLayerBlock = block2;
					compose(snowLayerBlock, 0);
					snowTextureSubId = snowLayerBlock?.Textures[BlockFacing.UP.Code].Baked.TextureSubId ?? 0;
				}
			}
		}
		foreach (Block block in blocks)
		{
			if (block != null && !(block.Code == null))
			{
				compose(block, snowTextureSubId);
			}
		}
	}

	private void collectTexturesForBlock(Block block, OrderedDictionary<AssetLocation, UnloadableShape> shapes)
	{
		block.OnCollectTextures(game.api, this);
		if (block.Shape != null)
		{
			collectAndBakeTexturesFromShape(block, block.Shape, inv: false, shapes);
		}
		if (block.ShapeInventory != null)
		{
			collectAndBakeTexturesFromShape(block, block.ShapeInventory, inv: true, shapes);
		}
	}

	private void compose(Block block, int snowtextureSubid)
	{
		int blockId = block.BlockId;
		foreach (KeyValuePair<string, CompositeTexture> val in block.TexturesInventory)
		{
			val.Value.Baked.TextureSubId = textureNamesDict[val.Value.Baked.BakedName];
		}
		foreach (KeyValuePair<string, CompositeTexture> texture in block.Textures)
		{
			BakedCompositeTexture bct = texture.Value.Baked;
			bct.TextureSubId = textureNamesDict[bct.BakedName];
			if (bct.BakedVariants != null)
			{
				for (int j = 0; j < bct.BakedVariants.Length; j++)
				{
					bct.BakedVariants[j].TextureSubId = textureNamesDict[bct.BakedVariants[j].BakedName];
				}
			}
			if (bct.BakedTiles != null)
			{
				for (int i = 0; i < bct.BakedTiles.Length; i++)
				{
					bct.BakedTiles[i].TextureSubId = textureNamesDict[bct.BakedTiles[i].BakedName];
				}
			}
		}
		if (block.DrawType != EnumDrawType.JSON)
		{
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing facing in aLLFACES)
			{
				if (block.Textures.TryGetValue(facing.Code, out var faceTexture))
				{
					int textureSubid = faceTexture.Baked.TextureSubId;
					game.FastBlockTextureSubidsByBlockAndFace[blockId][facing.Index] = textureSubid;
				}
			}
			if (block.Textures.TryGetValue("specialSecondTexture", out var secondTexture))
			{
				game.FastBlockTextureSubidsByBlockAndFace[blockId][6] = secondTexture.Baked.TextureSubId;
			}
			else
			{
				game.FastBlockTextureSubidsByBlockAndFace[blockId][6] = game.FastBlockTextureSubidsByBlockAndFace[blockId][BlockFacing.UP.Index];
			}
		}
		if (block.DrawType == EnumDrawType.JSONAndSnowLayer || block.DrawType == EnumDrawType.CrossAndSnowlayer || block.DrawType == EnumDrawType.CrossAndSnowlayer_2 || block.DrawType == EnumDrawType.CrossAndSnowlayer_3 || block.DrawType == EnumDrawType.CrossAndSnowlayer_4)
		{
			game.FastBlockTextureSubidsByBlockAndFace[blockId][6] = snowtextureSubid;
		}
	}

	private void collectAndBakeTexturesFromShape(Block block, CompositeShape shape, bool inv, OrderedDictionary<AssetLocation, UnloadableShape> shapes)
	{
		if (shapes.TryGetValue(shape.Base, out var compositeShape))
		{
			IDictionary<string, CompositeTexture> targetDict = (inv ? block.TexturesInventory : block.Textures);
			CollectAndBakeTexturesFromShape(compositeShape, targetDict, shape.Base);
		}
		if (shape.BakedAlternates != null)
		{
			CompositeShape[] bakedAlternates = shape.BakedAlternates;
			foreach (CompositeShape val in bakedAlternates)
			{
				collectAndBakeTexturesFromShape(block, val, inv, shapes);
			}
		}
	}

	public override void CollectAndBakeTexturesFromShape(Shape compositeShape, IDictionary<string, CompositeTexture> targetDict, AssetLocation baseLoc)
	{
		Dictionary<string, AssetLocation> shapeTextures = compositeShape.Textures;
		if (shapeTextures == null)
		{
			return;
		}
		foreach (KeyValuePair<string, AssetLocation> val in shapeTextures)
		{
			if (!targetDict.ContainsKey(val.Key))
			{
				CompositeTexture ct = new CompositeTexture(val.Value);
				ct.Bake(game.AssetManager);
				AssetLocationAndSource locS = new AssetLocationAndSource(ct.Baked.BakedName, "Shape file ", baseLoc);
				if (val.Key == "specialSecondTexture")
				{
					locS.AddToAllAtlasses = true;
				}
				ct.Baked.TextureSubId = GetOrAddTextureLocation(locS);
				targetDict[val.Key] = ct;
			}
		}
	}

	public void ResolveTextureCodes(Block block, OrderedDictionary<AssetLocation, UnloadableShape> blockShapes, Dictionary<AssetLocation, CompositeTexture> basicTexturesCache)
	{
		blockShapes.TryGetValue(block.Shape.Base, out var baseShape);
		UnloadableShape inventoryShape = ((block.ShapeInventory == null) ? null : blockShapes[block.ShapeInventory.Base]);
		if (baseShape != null && !baseShape.Loaded)
		{
			baseShape.Load(game, new AssetLocationAndSource(block.Shape.Base));
		}
		if (inventoryShape != null && !inventoryShape.Loaded)
		{
			inventoryShape.Load(game, new AssetLocationAndSource(block.Shape.Base));
		}
		bool blockTexturesContainsAll = block.Textures.ContainsKey("all");
		bool invTexturesContainsAll = block.TexturesInventory.ContainsKey("all");
		if (blockTexturesContainsAll || invTexturesContainsAll)
		{
			LoadAllTextureCodes(block, baseShape);
		}
		if (block.Textures.Count > 0)
		{
			ResolveTextureDict((TextureDictionary)block.Textures);
		}
		if (block.TexturesInventory.Count > 0)
		{
			ResolveTextureDict((TextureDictionary)block.TexturesInventory);
		}
		if (baseShape?.Textures != null)
		{
			if (baseShape.TexturesResolved == null)
			{
				baseShape.ResolveTextures(basicTexturesCache);
			}
			foreach (KeyValuePair<string, CompositeTexture> val4 in baseShape.TexturesResolved)
			{
				string textureCode2 = val4.Key;
				CompositeTexture shapefileTexture = val4.Value;
				if (block.Textures.TryGetValue(textureCode2, out var tex2))
				{
					if (tex2.Base.Path == "inherit")
					{
						tex2.Base = shapefileTexture.Base;
					}
					if (tex2.BlendedOverlays == null)
					{
						continue;
					}
					BlendedOverlayTexture[] overlays = tex2.BlendedOverlays;
					for (int i = 0; i < overlays.Length; i++)
					{
						if (overlays[i].Base.Path == "inherit")
						{
							overlays[i].Base = shapefileTexture.Base;
						}
					}
				}
				else if (!blockTexturesContainsAll || !(textureCode2 == "all"))
				{
					block.Textures.Add(textureCode2, shapefileTexture);
				}
			}
		}
		replacements.Clear();
		foreach (KeyValuePair<string, CompositeTexture> val3 in block.Textures)
		{
			CompositeTexture tex = val3.Value;
			if (tex.IsBasic())
			{
				if (basicTexturesCache.TryGetValue(tex.Base, out var cachedTex))
				{
					if (tex != cachedTex)
					{
						replacements.Add(new KeyValuePair<string, CompositeTexture>(val3.Key, cachedTex));
						tex = cachedTex;
					}
				}
				else
				{
					basicTexturesCache.Add(tex.Base, tex);
				}
			}
			((TextureDictionary)block.TexturesInventory).AddIfNotPresent(val3.Key, tex);
		}
		foreach (KeyValuePair<string, CompositeTexture> val2 in replacements)
		{
			block.Textures[val2.Key] = val2.Value;
		}
		if (inventoryShape == null || inventoryShape.Textures == null)
		{
			return;
		}
		if (inventoryShape.TexturesResolved == null)
		{
			inventoryShape.ResolveTextures(basicTexturesCache);
		}
		foreach (KeyValuePair<string, CompositeTexture> val in inventoryShape.TexturesResolved)
		{
			string textureCode = val.Key;
			if (!invTexturesContainsAll || !(textureCode == "all"))
			{
				((TextureDictionary)block.TexturesInventory).AddIfNotPresent(textureCode, val.Value);
			}
		}
	}

	public void LoadAllTextureCodes(Block block, Shape blockShape)
	{
		LoadShapeTextureCodes(blockShape);
		if (block.DrawType == EnumDrawType.Cube)
		{
			textureCodes.Add("west");
			textureCodes.Add("east");
			textureCodes.Add("north");
			textureCodes.Add("south");
			textureCodes.Add("up");
			textureCodes.Add("down");
		}
	}

	public TextureAtlasPosition GetPosition(Block block, string textureName, bool returnNullWhenMissing = false)
	{
		return new TextureSource(game, base.Size, block)
		{
			returnNullWhenMissing = returnNullWhenMissing
		}[textureName];
	}
}
