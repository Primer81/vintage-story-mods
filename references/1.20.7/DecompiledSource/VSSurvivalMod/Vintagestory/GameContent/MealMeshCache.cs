using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class MealMeshCache : ModSystem, ITexPositionSource
{
	private ICoreClientAPI capi;

	private Block mealtextureSourceBlock;

	private AssetLocation[] pieShapeLocByFillLevel = new AssetLocation[5]
	{
		new AssetLocation("block/food/pie/full-fill0"),
		new AssetLocation("block/food/pie/full-fill1"),
		new AssetLocation("block/food/pie/full-fill2"),
		new AssetLocation("block/food/pie/full-fill3"),
		new AssetLocation("block/food/pie/full-fill4")
	};

	private AssetLocation[] pieShapeBySize = new AssetLocation[4]
	{
		new AssetLocation("block/food/pie/quarter"),
		new AssetLocation("block/food/pie/half"),
		new AssetLocation("block/food/pie/threefourths"),
		new AssetLocation("block/food/pie/full")
	};

	protected Shape nowTesselatingShape;

	private BlockPie nowTesselatingBlock;

	private ItemStack[] contentStacks;

	private AssetLocation crustTextureLoc;

	private AssetLocation fillingTextureLoc;

	private AssetLocation topCrustTextureLoc;

	public AssetLocation[] pieMixedFillingTextures = new AssetLocation[5]
	{
		new AssetLocation("block/food/pie/fill-mixedfruit"),
		new AssetLocation("block/food/pie/fill-mixedvegetable"),
		new AssetLocation("block/food/pie/fill-mixedmeat"),
		new AssetLocation("grain-unused-placeholder"),
		new AssetLocation("block/food/pie/fill-mixedcheese")
	};

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			AssetLocation texturePath = crustTextureLoc;
			if (textureCode == "filling")
			{
				texturePath = fillingTextureLoc;
			}
			if (textureCode == "topcrust")
			{
				texturePath = topCrustTextureLoc;
			}
			if (texturePath == null)
			{
				capi.World.Logger.Warning("Missing texture path for pie mesh texture code {0}, seems like a missing texture definition or invalid pie block.", textureCode);
				return capi.BlockTextureAtlas.UnknownTexturePosition;
			}
			TextureAtlasPosition texpos = capi.BlockTextureAtlas[texturePath];
			if (texpos == null)
			{
				IAsset texAsset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
				if (texAsset != null)
				{
					BitmapRef bmp = texAsset.ToBitmap(capi);
					capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out var _, out texpos, () => bmp);
				}
				else
				{
					capi.World.Logger.Warning("Pie mesh texture {1} not found.", nowTesselatingBlock.Code, texturePath);
					texpos = capi.BlockTextureAtlas.UnknownTexturePosition;
				}
			}
			return texpos;
		}
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		capi = api;
		api.Event.LeaveWorld += Event_LeaveWorld;
		api.Event.BlockTexturesLoaded += Event_BlockTexturesLoaded;
	}

	private void Event_BlockTexturesLoaded()
	{
		mealtextureSourceBlock = capi.World.GetBlock(new AssetLocation("claypot-cooked"));
	}

	public override void Dispose()
	{
		if (capi == null || !capi.ObjectCache.TryGetValue("pieMeshRefs", out var objPi) || !(objPi is Dictionary<int, MultiTextureMeshRef> meshRefs))
		{
			return;
		}
		foreach (KeyValuePair<int, MultiTextureMeshRef> item in meshRefs)
		{
			item.Deconstruct(out var _, out var value);
			value.Dispose();
		}
		capi.ObjectCache.Remove("pieMeshRefs");
	}

	public MultiTextureMeshRef GetOrCreatePieMeshRef(ItemStack pieStack)
	{
		object obj;
		Dictionary<int, MultiTextureMeshRef> meshrefs = (Dictionary<int, MultiTextureMeshRef>)(capi.ObjectCache.TryGetValue("pieMeshRefs", out obj) ? (obj as Dictionary<int, MultiTextureMeshRef>) : (capi.ObjectCache["pieMeshRefs"] = new Dictionary<int, MultiTextureMeshRef>()));
		if (pieStack == null)
		{
			return null;
		}
		ItemStack[] contentStacks = (pieStack.Block as BlockPie).GetContents(capi.World, pieStack);
		string extrakey = "ct" + pieStack.Attributes.GetAsInt("topCrustType") + "-bl" + pieStack.Attributes.GetAsInt("bakeLevel") + "-ps" + pieStack.Attributes.GetAsInt("pieSize");
		int mealhashcode = GetMealHashCode(pieStack.Block, contentStacks, null, extrakey);
		if (!meshrefs.TryGetValue(mealhashcode, out var mealMeshRef))
		{
			MeshData mesh = GetPieMesh(pieStack);
			if (mesh == null)
			{
				return null;
			}
			mealMeshRef = (meshrefs[mealhashcode] = capi.Render.UploadMultiTextureMesh(mesh));
		}
		return mealMeshRef;
	}

	public MeshData GetPieMesh(ItemStack pieStack, ModelTransform transform = null)
	{
		nowTesselatingBlock = pieStack.Block as BlockPie;
		if (nowTesselatingBlock == null)
		{
			return null;
		}
		contentStacks = nowTesselatingBlock.GetContents(capi.World, pieStack);
		int pieSize = pieStack.Attributes.GetAsInt("pieSize");
		InPieProperties[] stackprops = contentStacks.Select((ItemStack stack) => stack?.ItemAttributes?["inPieProperties"]?.AsObject<InPieProperties>(null, stack.Collectible.Code.Domain)).ToArray();
		int bakeLevel = pieStack.Attributes.GetAsInt("bakeLevel");
		if (stackprops.Length == 0)
		{
			return null;
		}
		ItemStack cstack = contentStacks[1];
		bool equal = true;
		int i = 2;
		while (equal && i < contentStacks.Length - 1)
		{
			if (contentStacks[i] != null && cstack != null)
			{
				equal &= cstack.Equals(capi.World, contentStacks[i], GlobalConstants.IgnoredStackAttributes);
				cstack = contentStacks[i];
			}
			i++;
		}
		if (ContentsRotten(contentStacks))
		{
			crustTextureLoc = new AssetLocation("block/rot/rot");
			fillingTextureLoc = new AssetLocation("block/rot/rot");
			topCrustTextureLoc = new AssetLocation("block/rot/rot");
		}
		else
		{
			if (stackprops[0] != null)
			{
				crustTextureLoc = stackprops[0].Texture.Clone();
				crustTextureLoc.Path = crustTextureLoc.Path.Replace("{bakelevel}", (bakeLevel + 1).ToString() ?? "");
				fillingTextureLoc = new AssetLocation("block/transparent");
			}
			topCrustTextureLoc = new AssetLocation("block/transparent");
			if (stackprops[5] != null)
			{
				topCrustTextureLoc = stackprops[5].Texture.Clone();
				topCrustTextureLoc.Path = topCrustTextureLoc.Path.Replace("{bakelevel}", (bakeLevel + 1).ToString() ?? "");
			}
			if (contentStacks[1] != null)
			{
				EnumFoodCategory fillingFoodCat = contentStacks[1].Collectible.NutritionProps?.FoodCategory ?? (contentStacks[1].ItemAttributes?["nutritionPropsWhenInMeal"]?.AsObject<FoodNutritionProperties>()?.FoodCategory).GetValueOrDefault(EnumFoodCategory.Vegetable);
				fillingTextureLoc = ((!equal) ? pieMixedFillingTextures[(int)fillingFoodCat] : stackprops[1]?.Texture);
			}
		}
		int fillLevel = ((contentStacks[1] != null) ? 1 : 0) + ((contentStacks[2] != null) ? 1 : 0) + ((contentStacks[3] != null) ? 1 : 0) + ((contentStacks[4] != null) ? 1 : 0);
		AssetLocation shapeloc = ((fillLevel == 4) ? pieShapeBySize[pieSize - 1] : pieShapeLocByFillLevel[fillLevel]);
		shapeloc.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");
		Shape shape = Shape.TryGet(capi, shapeloc);
		int topCrustType = pieStack.Attributes.GetAsInt("topCrustType");
		string[] topCrusts = new string[3] { "origin/base/top crust full/*", "origin/base/top crust square/*", "origin/base/top crust diagonal/*" };
		string[] selectiveElements = new string[5]
		{
			"origin/base/crust regular/*",
			"origin/base/filling/*",
			"origin/base/base-quarter/*",
			"origin/base/fillingquarter/*",
			topCrusts[topCrustType]
		};
		capi.Tesselator.TesselateShape("pie", shape, out var mesh, this, null, 0, 0, 0, null, selectiveElements);
		if (transform != null)
		{
			mesh.ModelTransform(transform);
		}
		return mesh;
	}

	public MultiTextureMeshRef GetOrCreateMealInContainerMeshRef(Block containerBlock, CookingRecipe forRecipe, ItemStack[] contentStacks, Vec3f foodTranslate = null)
	{
		object obj;
		Dictionary<int, MultiTextureMeshRef> meshrefs = (Dictionary<int, MultiTextureMeshRef>)(capi.ObjectCache.TryGetValue("cookedMeshRefs", out obj) ? (obj as Dictionary<int, MultiTextureMeshRef>) : (capi.ObjectCache["cookedMeshRefs"] = new Dictionary<int, MultiTextureMeshRef>()));
		if (contentStacks == null)
		{
			return null;
		}
		int mealhashcode = GetMealHashCode(containerBlock, contentStacks, foodTranslate);
		if (!meshrefs.TryGetValue(mealhashcode, out var mealMeshRef))
		{
			MeshData mesh = GenMealInContainerMesh(containerBlock, forRecipe, contentStacks, foodTranslate);
			mealMeshRef = (meshrefs[mealhashcode] = capi.Render.UploadMultiTextureMesh(mesh));
		}
		return mealMeshRef;
	}

	public MeshData GenMealInContainerMesh(Block containerBlock, CookingRecipe forRecipe, ItemStack[] contentStacks, Vec3f foodTranslate = null)
	{
		CompositeShape cShape = containerBlock.Shape;
		AssetLocation loc = cShape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		Shape shape = Shape.TryGet(capi, loc);
		capi.Tesselator.TesselateShape("meal", shape, out var wholeMesh, capi.Tesselator.GetTextureSource(containerBlock), new Vec3f(cShape.rotateX, cShape.rotateY, cShape.rotateZ), 0, 0, 0);
		MeshData mealMesh = GenMealMesh(forRecipe, contentStacks, foodTranslate);
		if (mealMesh != null)
		{
			wholeMesh.AddMeshData(mealMesh);
		}
		return wholeMesh;
	}

	public MeshData GenMealMesh(CookingRecipe forRecipe, ItemStack[] contentStacks, Vec3f foodTranslate = null)
	{
		MealTextureSource source = new MealTextureSource(capi, mealtextureSourceBlock);
		if (forRecipe != null)
		{
			MeshData foodMesh = GenFoodMixMesh(contentStacks, forRecipe, foodTranslate);
			if (foodMesh != null)
			{
				return foodMesh;
			}
		}
		if (contentStacks != null && contentStacks.Length != 0)
		{
			if (ContentsRotten(contentStacks))
			{
				Shape contentShape = Shape.TryGet(capi, "shapes/block/food/meal/rot.json");
				capi.Tesselator.TesselateShape("rotcontents", contentShape, out var contentMesh, source, null, 0, 0, 0);
				if (foodTranslate != null)
				{
					contentMesh.Translate(foodTranslate);
				}
				return contentMesh;
			}
			JsonObject obj = contentStacks[0]?.ItemAttributes?["inContainerTexture"];
			if (obj != null && obj.Exists)
			{
				source.ForStack = contentStacks[0];
				CompositeShape cshape = contentStacks[0]?.ItemAttributes?["inBowlShape"].AsObject(new CompositeShape
				{
					Base = new AssetLocation("shapes/block/food/meal/pickled.json")
				});
				Shape contentShape2 = Shape.TryGet(capi, cshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));
				capi.Tesselator.TesselateShape("picklednmealcontents", contentShape2, out var contentMesh2, source, null, 0, 0, 0);
				return contentMesh2;
			}
		}
		return null;
	}

	public static bool ContentsRotten(ItemStack[] contentStacks)
	{
		for (int i = 0; i < contentStacks.Length; i++)
		{
			if (contentStacks[i]?.Collectible.Code.Path == "rot")
			{
				return true;
			}
		}
		return false;
	}

	public static bool ContentsRotten(InventoryBase inv)
	{
		foreach (ItemSlot item in inv)
		{
			if (item.Itemstack?.Collectible.Code.Path == "rot")
			{
				return true;
			}
		}
		return false;
	}

	public MeshData GenFoodMixMesh(ItemStack[] contentStacks, CookingRecipe recipe, Vec3f foodTranslate)
	{
		MeshData mergedmesh = null;
		MealTextureSource texSource = new MealTextureSource(capi, mealtextureSourceBlock);
		AssetLocation shapePath = recipe.Shape.Base.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
		bool num = ContentsRotten(contentStacks);
		if (num)
		{
			shapePath = new AssetLocation("shapes/block/food/meal/rot.json");
		}
		Shape shape = Shape.TryGet(capi, shapePath);
		Dictionary<CookingRecipeIngredient, int> usedIngredQuantities = new Dictionary<CookingRecipeIngredient, int>();
		if (num)
		{
			capi.Tesselator.TesselateShape("mealpart", shape, out mergedmesh, texSource, new Vec3f(recipe.Shape.rotateX, recipe.Shape.rotateY, recipe.Shape.rotateZ), 0, 0, 0);
		}
		else
		{
			HashSet<string> drawnMeshes = new HashSet<string>();
			for (int i = 0; i < contentStacks.Length; i++)
			{
				texSource.ForStack = contentStacks[i];
				CookingRecipeIngredient ingred = recipe.GetIngrendientFor(contentStacks[i], (from val in usedIngredQuantities
					where val.Key.MaxQuantity <= val.Value
					select val.Key).ToArray());
				if (ingred == null)
				{
					ingred = recipe.GetIngrendientFor(contentStacks[i]);
				}
				else
				{
					int cnt = 0;
					usedIngredQuantities.TryGetValue(ingred, out cnt);
					cnt++;
					usedIngredQuantities[ingred] = cnt;
				}
				if (ingred == null)
				{
					continue;
				}
				string[] selectiveElements = null;
				CookingRecipeStack recipestack = ingred.GetMatchingStack(contentStacks[i]);
				if (recipestack.ShapeElement != null)
				{
					selectiveElements = new string[1] { recipestack.ShapeElement };
				}
				texSource.customTextureMapping = recipestack.TextureMapping;
				if (!drawnMeshes.Contains(recipestack.ShapeElement + recipestack.TextureMapping))
				{
					drawnMeshes.Add(recipestack.ShapeElement + recipestack.TextureMapping);
					capi.Tesselator.TesselateShape("mealpart", shape, out var meshpart, texSource, new Vec3f(recipe.Shape.rotateX, recipe.Shape.rotateY, recipe.Shape.rotateZ), 0, 0, 0, null, selectiveElements);
					if (mergedmesh == null)
					{
						mergedmesh = meshpart;
					}
					else
					{
						mergedmesh.AddMeshData(meshpart);
					}
				}
			}
		}
		if (foodTranslate != null)
		{
			mergedmesh?.Translate(foodTranslate);
		}
		return mergedmesh;
	}

	private void Event_LeaveWorld()
	{
		if (capi == null || !capi.ObjectCache.TryGetValue("cookedMeshRefs", out var obj))
		{
			return;
		}
		foreach (KeyValuePair<int, MultiTextureMeshRef> item in obj as Dictionary<int, MultiTextureMeshRef>)
		{
			item.Value.Dispose();
		}
		capi.ObjectCache.Remove("cookedMeshRefs");
	}

	public int GetMealHashCode(ItemStack stack, Vec3f translate = null, string extraKey = "")
	{
		ItemStack[] contentStacks = (stack.Block as BlockContainer).GetContents(capi.World, stack);
		if (stack.Block is BlockPie)
		{
			extraKey = extraKey + "ct" + stack.Attributes.GetAsInt("topCrustType") + "-bl" + stack.Attributes.GetAsInt("bakeLevel") + "-ps" + stack.Attributes.GetAsInt("pieSize");
		}
		return GetMealHashCode(stack.Block, contentStacks, translate, extraKey);
	}

	protected int GetMealHashCode(Block block, ItemStack[] contentStacks, Vec3f translate = null, string extraKey = null)
	{
		string shapestring = block.Shape.ToString() + block.Code.ToShortString();
		if (translate != null)
		{
			shapestring = shapestring + translate.X + "/" + translate.Y + "/" + translate.Z;
		}
		string contentstring = "";
		for (int i = 0; i < contentStacks.Length; i++)
		{
			if (contentStacks[i] != null)
			{
				if (contentStacks[i].Collectible.Code.Path == "rot")
				{
					return (shapestring + "rotten").GetHashCode();
				}
				contentstring += contentStacks[i].Collectible.Code.ToShortString();
			}
		}
		return (shapestring + contentstring + extraKey).GetHashCode();
	}
}
