using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class RecipeRegistrySystem : ModSystem
{
	public static bool canRegister = true;

	public List<CookingRecipe> CookingRecipes = new List<CookingRecipe>();

	public List<BarrelRecipe> BarrelRecipes = new List<BarrelRecipe>();

	public List<AlloyRecipe> MetalAlloys = new List<AlloyRecipe>();

	public List<SmithingRecipe> SmithingRecipes = new List<SmithingRecipe>();

	public List<KnappingRecipe> KnappingRecipes = new List<KnappingRecipe>();

	public List<ClayFormingRecipe> ClayFormingRecipes = new List<ClayFormingRecipe>();

	public override double ExecuteOrder()
	{
		return 0.6;
	}

	public override void StartPre(ICoreAPI api)
	{
		canRegister = true;
	}

	public override void Start(ICoreAPI api)
	{
		CookingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<CookingRecipe>>("cookingrecipes").Recipes;
		MetalAlloys = api.RegisterRecipeRegistry<RecipeRegistryGeneric<AlloyRecipe>>("alloyrecipes").Recipes;
		SmithingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<SmithingRecipe>>("smithingrecipes").Recipes;
		KnappingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<KnappingRecipe>>("knappingrecipes").Recipes;
		ClayFormingRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<ClayFormingRecipe>>("clayformingrecipes").Recipes;
		BarrelRecipes = api.RegisterRecipeRegistry<RecipeRegistryGeneric<BarrelRecipe>>("barrelrecipes").Recipes;
	}

	public override void AssetsLoaded(ICoreAPI api)
	{
		if (!(api is ICoreServerAPI sapi))
		{
			return;
		}
		Dictionary<AssetLocation, JToken> recipes = sapi.Assets.GetMany<JToken>(sapi.Server.Logger, "recipes/cooking");
		foreach (KeyValuePair<AssetLocation, JToken> val in recipes)
		{
			if (val.Value is JObject)
			{
				loadRecipe(sapi, val.Key, val.Value);
			}
			if (!(val.Value is JArray))
			{
				continue;
			}
			foreach (JToken token in val.Value as JArray)
			{
				loadRecipe(sapi, val.Key, token);
			}
		}
		sapi.World.Logger.Event("{0} cooking recipes loaded", recipes.Count);
		sapi.World.Logger.StoryEvent(Lang.Get("Taste and smell..."));
	}

	private void loadRecipe(ICoreServerAPI sapi, AssetLocation loc, JToken jrec)
	{
		CookingRecipe recipe = jrec.ToObject<CookingRecipe>(loc.Domain);
		if (recipe.Enabled)
		{
			recipe.Resolve(sapi.World, "cooking recipe " + loc);
			RegisterCookingRecipe(recipe);
		}
	}

	public void RegisterCookingRecipe(CookingRecipe recipe)
	{
		if (!canRegister)
		{
			throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
		}
		CookingRecipes.Add(recipe);
	}

	public void RegisterBarrelRecipe(BarrelRecipe recipe)
	{
		if (!canRegister)
		{
			throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
		}
		if (recipe.Code == null)
		{
			throw new ArgumentException("Barrel recipes must have a non-null code! (choose freely)");
		}
		BarrelRecipeIngredient[] ingredients = recipe.Ingredients;
		foreach (BarrelRecipeIngredient ingred in ingredients)
		{
			if (ingred.ConsumeQuantity.HasValue && ingred.ConsumeQuantity > ingred.Quantity)
			{
				throw new ArgumentException("Barrel recipe with code {0} has an ingredient with ConsumeQuantity > Quantity. Not a valid recipe!");
			}
		}
		BarrelRecipes.Add(recipe);
	}

	public void RegisterMetalAlloy(AlloyRecipe alloy)
	{
		if (!canRegister)
		{
			throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
		}
		MetalAlloys.Add(alloy);
	}

	public void RegisterClayFormingRecipe(ClayFormingRecipe recipe)
	{
		if (!canRegister)
		{
			throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
		}
		recipe.RecipeId = ClayFormingRecipes.Count + 1;
		ClayFormingRecipes.Add(recipe);
	}

	public void RegisterSmithingRecipe(SmithingRecipe recipe)
	{
		if (!canRegister)
		{
			throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
		}
		recipe.RecipeId = SmithingRecipes.Count + 1;
		SmithingRecipes.Add(recipe);
	}

	public void RegisterKnappingRecipe(KnappingRecipe recipe)
	{
		if (!canRegister)
		{
			throw new InvalidOperationException("Coding error: Can no long register cooking recipes. Register them during AssetsLoad/AssetsFinalize and with ExecuteOrder < 99999");
		}
		recipe.RecipeId = KnappingRecipes.Count + 1;
		KnappingRecipes.Add(recipe);
	}
}
