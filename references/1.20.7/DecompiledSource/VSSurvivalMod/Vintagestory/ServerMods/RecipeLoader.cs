using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Vintagestory.ServerMods;

public class RecipeLoader : ModSystem
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
		ICoreServerAPI sapi = api as ICoreServerAPI;
		if (sapi != null)
		{
			this.api = sapi;
			classExclusiveRecipes = sapi.World.Config.GetBool("classExclusiveRecipes", defaultValue: true);
			LoadAlloyRecipes();
			LoadRecipes("smithing recipe", "recipes/smithing", delegate(SmithingRecipe r)
			{
				sapi.RegisterSmithingRecipe(r);
			});
			sapi.World.Logger.StoryEvent(Lang.Get("Burning sparks..."));
			LoadRecipes("clay forming recipe", "recipes/clayforming", delegate(ClayFormingRecipe r)
			{
				sapi.RegisterClayFormingRecipe(r);
			});
			sapi.World.Logger.StoryEvent(Lang.Get("Molded forms..."));
			LoadRecipes("knapping recipe", "recipes/knapping", delegate(KnappingRecipe r)
			{
				sapi.RegisterKnappingRecipe(r);
			});
			sapi.World.Logger.StoryEvent(Lang.Get("Simple tools..."));
			LoadRecipes("barrel recipe", "recipes/barrel", delegate(BarrelRecipe r)
			{
				sapi.RegisterBarrelRecipe(r);
			});
		}
	}

	public void LoadAlloyRecipes()
	{
		Dictionary<AssetLocation, AlloyRecipe> alloys = api.Assets.GetMany<AlloyRecipe>(api.Server.Logger, "recipes/alloy");
		foreach (KeyValuePair<AssetLocation, AlloyRecipe> val in alloys)
		{
			if (val.Value.Enabled)
			{
				val.Value.Resolve(api.World, "alloy recipe " + val.Key);
				api.RegisterMetalAlloy(val.Value);
			}
		}
		api.World.Logger.Event("{0} metal alloys loaded", alloys.Count);
		api.World.Logger.StoryEvent(Lang.Get("Glimmers in the soil..."));
	}

	public void LoadRecipes<T>(string name, string path, Action<T> RegisterMethod) where T : IRecipeBase<T>
	{
		Dictionary<AssetLocation, JToken> many = api.Assets.GetMany<JToken>(api.Server.Logger, path);
		int recipeQuantity = 0;
		int quantityRegistered = 0;
		int quantityIgnored = 0;
		foreach (KeyValuePair<AssetLocation, JToken> val in many)
		{
			if (val.Value is JObject)
			{
				LoadGenericRecipe(name, val.Key, val.Value.ToObject<T>(val.Key.Domain), RegisterMethod, ref quantityRegistered, ref quantityIgnored);
				recipeQuantity++;
			}
			if (!(val.Value is JArray))
			{
				continue;
			}
			foreach (JToken token in val.Value as JArray)
			{
				LoadGenericRecipe(name, val.Key, token.ToObject<T>(val.Key.Domain), RegisterMethod, ref quantityRegistered, ref quantityIgnored);
				recipeQuantity++;
			}
		}
		api.World.Logger.Event("{0} {1}s loaded{2}", quantityRegistered, name, (quantityIgnored > 0) ? $" ({quantityIgnored} could not be resolved)" : "");
	}

	private void LoadGenericRecipe<T>(string className, AssetLocation path, T recipe, Action<T> RegisterMethod, ref int quantityRegistered, ref int quantityIgnored) where T : IRecipeBase<T>
	{
		if (!recipe.Enabled)
		{
			return;
		}
		if (recipe.Name == null)
		{
			recipe.Name = path;
		}
		ref T reference = ref recipe;
		T val4 = default(T);
		if (val4 == null)
		{
			val4 = reference;
			reference = ref val4;
		}
		Dictionary<string, string[]> nameToCodeMapping = reference.GetNameToCodeMapping(api.World);
		if (nameToCodeMapping.Count > 0)
		{
			List<T> subRecipes = new List<T>();
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
					T rec;
					if (first)
					{
						subRecipes.Add(rec = recipe.Clone());
					}
					else
					{
						rec = subRecipes[i];
					}
					if (rec.Ingredients != null)
					{
						IRecipeIngredient[] ingredients = rec.Ingredients;
						foreach (IRecipeIngredient ingred in ingredients)
						{
							if (ingred.Name == variantCode)
							{
								ingred.Code = ingred.Code.CopyWithPath(ingred.Code.Path.Replace("*", variants[i % variants.Length]));
							}
						}
					}
					rec.Output.FillPlaceHolder(val2.Key, variants[i % variants.Length]);
				}
				first = false;
			}
			if (subRecipes.Count == 0)
			{
				api.World.Logger.Warning("{1} file {0} make uses of wildcards, but no blocks or item matching those wildcards were found.", path, className);
			}
			{
				foreach (T item in subRecipes)
				{
					T subRecipe = item;
					ref T reference2 = ref subRecipe;
					val4 = default(T);
					if (val4 == null)
					{
						val4 = reference2;
						reference2 = ref val4;
					}
					if (!reference2.Resolve(api.World, className + " " + path))
					{
						quantityIgnored++;
						continue;
					}
					RegisterMethod(subRecipe);
					quantityRegistered++;
				}
				return;
			}
		}
		ref T reference3 = ref recipe;
		val4 = default(T);
		if (val4 == null)
		{
			val4 = reference3;
			reference3 = ref val4;
		}
		if (!reference3.Resolve(api.World, className + " " + path))
		{
			quantityIgnored++;
			return;
		}
		RegisterMethod(recipe);
		quantityRegistered++;
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
							ingred.Code.Path = ingred.Code.Path.Replace("*", variants[i % variants.Length]);
						}
						if (ingred.ReturnedStack?.Code != null)
						{
							ingred.ReturnedStack.Code.Path.Replace("{" + variantCode + "}", variants[i % variants.Length]);
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
