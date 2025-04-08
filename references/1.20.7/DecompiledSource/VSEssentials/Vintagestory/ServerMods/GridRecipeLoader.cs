using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GridRecipeLoader : ModSystem
{
	private ICoreServerAPI api;

	private bool classExclusiveRecipes = true;

	public override double ExecuteOrder()
	{
		return 1.0;
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override void AssetsLoaded(ICoreAPI api)
	{
		if (api is ICoreServerAPI sapi)
		{
			this.api = sapi;
			classExclusiveRecipes = sapi.World.Config.GetBool("classExclusiveRecipes", defaultValue: true);
			LoadGridRecipes();
		}
	}

	public void LoadGridRecipes()
	{
		Dictionary<AssetLocation, JToken> files = api.Assets.GetMany<JToken>(api.Server.Logger, "recipes/grid");
		int recipeQuantity = 0;
		foreach (KeyValuePair<AssetLocation, JToken> val in files)
		{
			if (val.Value is JObject)
			{
				LoadRecipe(val.Key, val.Value.ToObject<GridRecipe>(val.Key.Domain));
				recipeQuantity++;
			}
			if (!(val.Value is JArray))
			{
				continue;
			}
			foreach (JToken token in val.Value as JArray)
			{
				LoadRecipe(val.Key, token.ToObject<GridRecipe>(val.Key.Domain));
				recipeQuantity++;
			}
		}
		api.World.Logger.Event("{0} crafting recipes loaded from {1} files", recipeQuantity, files.Count);
		api.World.Logger.StoryEvent(Lang.Get("Grand inventions..."));
	}

	public void LoadRecipe(AssetLocation loc, GridRecipe recipe)
	{
		if (!recipe.Enabled)
		{
			return;
		}
		if (!classExclusiveRecipes)
		{
			recipe.RequiresTrait = null;
		}
		if (recipe.Name == null)
		{
			recipe.Name = loc;
		}
		Dictionary<string, string[]> nameToCodeMapping = recipe.GetNameToCodeMapping(api.World);
		if (nameToCodeMapping.Count > 0)
		{
			List<GridRecipe> subRecipes = new List<GridRecipe>();
			int qCombs = 0;
			bool first = true;
			foreach (KeyValuePair<string, string[]> val3 in nameToCodeMapping)
			{
				qCombs = ((!first) ? (qCombs * val3.Value.Length) : val3.Value.Length);
				first = false;
			}
			first = true;
			foreach (KeyValuePair<string, string[]> val2 in nameToCodeMapping)
			{
				string variantCode = val2.Key;
				string[] variants = val2.Value;
				for (int i = 0; i < qCombs; i++)
				{
					GridRecipe rec;
					if (first)
					{
						subRecipes.Add(rec = recipe.Clone());
					}
					else
					{
						rec = subRecipes[i];
					}
					foreach (CraftingRecipeIngredient ingred in rec.Ingredients.Values)
					{
						if (ingred.Name == variantCode)
						{
							ingred.FillPlaceHolder(variantCode, variants[i % variants.Length]);
							ingred.Code.Path = ingred.Code.Path.Replace("*", variants[i % variants.Length]);
						}
						if (ingred.ReturnedStack?.Code != null)
						{
							ingred.ReturnedStack.Code.Path = ingred.ReturnedStack.Code.Path.Replace("{" + variantCode + "}", variants[i % variants.Length]);
						}
					}
					rec.Output.FillPlaceHolder(variantCode, variants[i % variants.Length]);
				}
				first = false;
			}
			{
				foreach (GridRecipe subRecipe in subRecipes)
				{
					if (subRecipe.ResolveIngredients(api.World))
					{
						api.RegisterCraftingRecipe(subRecipe);
					}
				}
				return;
			}
		}
		if (recipe.ResolveIngredients(api.World))
		{
			api.RegisterCraftingRecipe(recipe);
		}
	}
}
