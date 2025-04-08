using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class VanillaCookingRecipeNames : ICookingRecipeNamingHelper
{
	public string GetNameForIngredients(IWorldAccessor worldForResolve, string recipeCode, ItemStack[] stacks)
	{
		OrderedDictionary<ItemStack, int> quantitiesByStack = new OrderedDictionary<ItemStack, int>();
		quantitiesByStack = mergeStacks(worldForResolve, stacks);
		CookingRecipe recipe = worldForResolve.Api.GetCookingRecipe(recipeCode);
		if (recipeCode == null || recipe == null || quantitiesByStack.Count == 0)
		{
			return Lang.Get("unknown");
		}
		int max = 1;
		string MealFormat = "meal";
		string topping = string.Empty;
		ItemStack PrimaryIngredient = null;
		ItemStack SecondaryIngredient = null;
		List<string> OtherIngredients = new List<string>();
		List<string> MashedNames = new List<string>();
		List<string> GarnishedNames = new List<string>();
		new List<string>();
		string everythingelse = "";
		switch (recipeCode)
		{
		case "soup":
			max = 0;
			foreach (KeyValuePair<ItemStack, int> val7 in quantitiesByStack)
			{
				CookingRecipeIngredient ingred3 = recipe.GetIngrendientFor(val7.Key);
				if (val7.Key.Collectible.Code.Path.Contains("waterportion"))
				{
					continue;
				}
				if (ingred3?.Code == "topping")
				{
					topping = "honeyportion";
				}
				else if (max < val7.Value)
				{
					max = val7.Value;
					if (PrimaryIngredient != null)
					{
						SecondaryIngredient = PrimaryIngredient;
					}
					PrimaryIngredient = val7.Key;
				}
				else
				{
					OtherIngredients.Add(ingredientName(val7.Key, InsturmentalCase: true));
				}
			}
			max = max switch
			{
				2 => 3, 
				3 => 4, 
				_ => 2, 
			};
			break;
		case "porridge":
			max = 0;
			foreach (KeyValuePair<ItemStack, int> val6 in quantitiesByStack)
			{
				CookingRecipeIngredient ingred2 = recipe.GetIngrendientFor(val6.Key);
				if (getFoodCat(val6.Key) == EnumFoodCategory.Grain)
				{
					max++;
					if (PrimaryIngredient == null)
					{
						PrimaryIngredient = val6.Key;
					}
					else if (SecondaryIngredient == null && val6.Key != PrimaryIngredient)
					{
						SecondaryIngredient = val6.Key;
					}
				}
				else if (ingred2?.Code == "topping")
				{
					topping = "honeyportion";
				}
				else
				{
					MashedNames.Add(ingredientName(val6.Key, InsturmentalCase: true));
				}
			}
			break;
		case "meatystew":
			max = 0;
			foreach (KeyValuePair<ItemStack, int> val5 in quantitiesByStack)
			{
				CookingRecipeIngredient ingred = recipe.GetIngrendientFor(val5.Key);
				if (getFoodCat(val5.Key) == EnumFoodCategory.Protein)
				{
					if (PrimaryIngredient != val5.Key && SecondaryIngredient != val5.Key)
					{
						if (PrimaryIngredient == null)
						{
							PrimaryIngredient = val5.Key;
						}
						else if (SecondaryIngredient == null)
						{
							SecondaryIngredient = val5.Key;
						}
						else
						{
							OtherIngredients.Add(ingredientName(val5.Key, InsturmentalCase: true));
						}
						max += val5.Value;
					}
				}
				else if (ingred?.Code == "topping")
				{
					topping = "honeyportion";
				}
				else
				{
					OtherIngredients.Add(ingredientName(val5.Key, InsturmentalCase: true));
				}
			}
			recipeCode = "stew";
			break;
		case "vegetablestew":
			max = 0;
			foreach (KeyValuePair<ItemStack, int> val4 in quantitiesByStack)
			{
				if (getFoodCat(val4.Key) == EnumFoodCategory.Vegetable)
				{
					if (PrimaryIngredient != val4.Key && SecondaryIngredient != val4.Key)
					{
						if (PrimaryIngredient == null)
						{
							PrimaryIngredient = val4.Key;
						}
						else if (SecondaryIngredient == null)
						{
							SecondaryIngredient = val4.Key;
						}
						else
						{
							GarnishedNames.Add(ingredientName(val4.Key, InsturmentalCase: true));
						}
						max += val4.Value;
					}
				}
				else
				{
					GarnishedNames.Add(ingredientName(val4.Key, InsturmentalCase: true));
				}
			}
			if (PrimaryIngredient == null)
			{
				foreach (KeyValuePair<ItemStack, int> val3 in quantitiesByStack)
				{
					PrimaryIngredient = val3.Key;
					max += val3.Value;
				}
			}
			recipeCode = "stew";
			break;
		case "scrambledeggs":
			max = 0;
			foreach (KeyValuePair<ItemStack, int> val2 in quantitiesByStack)
			{
				if (val2.Key.Collectible.FirstCodePart() == "egg")
				{
					PrimaryIngredient = val2.Key;
					max += val2.Value;
				}
				else
				{
					GarnishedNames.Add(ingredientName(val2.Key, InsturmentalCase: true));
				}
			}
			recipeCode = "scrambledeggs";
			break;
		case "jam":
		{
			ItemStack[] fruits = new ItemStack[2];
			int i = 0;
			foreach (KeyValuePair<ItemStack, int> val in quantitiesByStack)
			{
				FoodNutritionProperties nutritionProps = val.Key.Collectible.NutritionProps;
				if (nutritionProps != null && nutritionProps.FoodCategory == EnumFoodCategory.Fruit)
				{
					fruits[i++] = val.Key;
					if (i == 2)
					{
						break;
					}
				}
			}
			if (fruits[1] != null)
			{
				string jamName = fruits[0].Collectible.LastCodePart() + "-" + fruits[1].Collectible.LastCodePart() + "-jam";
				if (Lang.HasTranslation(jamName))
				{
					return Lang.Get(jamName);
				}
				string firstFruitInJam = ((fruits[0].Collectible.Code.Domain == "game") ? "" : (fruits[0].Collectible.Code.Domain + ":")) + fruits[0].Collectible.LastCodePart() + "-in-jam-name";
				string secondFruitInJam = ((fruits[1].Collectible.Code.Domain == "game") ? "" : (fruits[1].Collectible.Code.Domain + ":")) + fruits[1].Collectible.LastCodePart() + "-in-jam-name";
				return Lang.Get("mealname-mixedjam", Lang.HasTranslation(firstFruitInJam) ? Lang.Get(firstFruitInJam) : fruits[0].GetName(), Lang.HasTranslation(secondFruitInJam) ? Lang.Get(secondFruitInJam) : fruits[1].GetName());
			}
			if (fruits[0] != null)
			{
				string jamName2 = fruits[0].Collectible.LastCodePart() + "-jam";
				if (Lang.HasTranslation(jamName2))
				{
					return Lang.Get(jamName2);
				}
				string fruitInJam = ((fruits[0].Collectible.Code.Domain == "game") ? "" : (fruits[0].Collectible.Code.Domain + ":")) + fruits[0].Collectible.Code.Domain + ":" + fruits[0].Collectible.LastCodePart() + "-in-jam-name";
				return Lang.Get("mealname-singlejam", Lang.HasTranslation(fruitInJam) ? Lang.Get(fruitInJam) : fruits[0].GetName());
			}
			return Lang.Get("unknown");
		}
		case "glueportion-pitch-hot":
		case "glueportion-pitch-cold":
		{
			ItemStack stack = stacks[0];
			if (stack == null)
			{
				return Lang.Get("unknown");
			}
			if (stack.Collectible.Code.PathStartsWith("glueportion"))
			{
				return stack.Collectible.GetHeldItemName(stack) + "\n\n" + stack.Collectible.GetItemDescText();
			}
			ItemStack outstack = recipe.CooksInto?.ResolvedItemstack;
			if (outstack != null)
			{
				return outstack.Collectible.GetHeldItemName(outstack);
			}
			return Lang.Get("unknown");
		}
		}
		MealFormat = max switch
		{
			3 => MealFormat + "-hearty-" + recipeCode, 
			4 => MealFormat + "-hefty-" + recipeCode, 
			_ => MealFormat + "-normal-" + recipeCode, 
		};
		if (topping == "honeyportion")
		{
			MealFormat += "-honey";
		}
		string mainIngredients = ((SecondaryIngredient == null || !(recipeCode != "scrambledeggs")) ? ((PrimaryIngredient == null) ? "" : getMainIngredientName(PrimaryIngredient, recipeCode)) : Lang.Get("multi-main-ingredients-format", getMainIngredientName(PrimaryIngredient, recipeCode), getMainIngredientName(SecondaryIngredient, recipeCode, secondary: true)));
		switch (recipeCode)
		{
		case "porridge":
			everythingelse = ((MashedNames.Count <= 0) ? "" : getMealAddsString("meal-adds-porridge-mashed", MashedNames));
			break;
		case "stew":
			everythingelse = ((OtherIngredients.Count <= 0) ? ((GarnishedNames.Count <= 0) ? "" : getMealAddsString("meal-adds-vegetablestew-garnish", GarnishedNames)) : getMealAddsString("meal-adds-meatystew-boiled", OtherIngredients));
			break;
		case "scrambledeggs":
			if (GarnishedNames.Count > 0)
			{
				everythingelse = getMealAddsString("meal-adds-vegetablestew-garnish", GarnishedNames);
			}
			return Lang.Get(MealFormat, everythingelse).Trim().UcFirst();
		case "soup":
			if (OtherIngredients.Count > 0)
			{
				everythingelse = getMealAddsString("meal-adds-generic", OtherIngredients);
			}
			break;
		}
		return Lang.Get(MealFormat, mainIngredients, everythingelse).Trim().UcFirst();
	}

	private EnumFoodCategory getFoodCat(ItemStack stack)
	{
		FoodNutritionProperties props = stack.Collectible.NutritionProps;
		if (props == null)
		{
			props = stack.Collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack?.Collectible?.NutritionProps;
		}
		return props?.FoodCategory ?? EnumFoodCategory.Dairy;
	}

	private string ingredientName(ItemStack stack, bool InsturmentalCase = false)
	{
		string code = stack.Collectible.Code?.Domain + ":recipeingredient-" + stack.Class.ToString().ToLowerInvariant() + "-" + stack.Collectible.Code?.Path;
		if (InsturmentalCase)
		{
			code += "-insturmentalcase";
		}
		if (Lang.HasTranslation(code))
		{
			return Lang.GetMatching(code);
		}
		code = stack.Collectible.Code?.Domain + ":recipeingredient-" + stack.Class.ToString().ToLowerInvariant() + "-" + stack.Collectible.FirstCodePart();
		if (InsturmentalCase)
		{
			code += "-insturmentalcase";
		}
		return Lang.GetMatching(code);
	}

	private string getMainIngredientName(ItemStack itemstack, string code, bool secondary = false)
	{
		string t = (secondary ? "secondary" : "primary");
		string langcode = $"meal-ingredient-{code}-{t}-{getInternalName(itemstack)}";
		if (Lang.HasTranslation(langcode))
		{
			return Lang.GetMatching(langcode);
		}
		langcode = $"meal-ingredient-{code}-{t}-{itemstack.Collectible.FirstCodePart()}";
		return Lang.GetMatching(langcode);
	}

	private string getInternalName(ItemStack itemstack)
	{
		return itemstack.Collectible.Code.Path;
	}

	private string getMealAddsString(string code, List<string> ingredients1, List<string> ingredients2 = null)
	{
		object[] args;
		if (ingredients2 == null)
		{
			object[] array = new object[1];
			string key = $"meal-ingredientlist-{ingredients1.Count}";
			args = ingredients1.ToArray();
			array[0] = Lang.Get(key, args);
			return Lang.Get(code, array);
		}
		object[] array2 = new object[2];
		string key2 = $"meal-ingredientlist-{ingredients1.Count}";
		args = ingredients1.ToArray();
		array2[0] = Lang.Get(key2, args);
		string key3 = $"meal-ingredientlist-{ingredients2.Count}";
		args = ingredients2.ToArray();
		array2[1] = Lang.Get(key3, args);
		return Lang.Get(code, array2);
	}

	private OrderedDictionary<ItemStack, int> mergeStacks(IWorldAccessor worldForResolve, ItemStack[] stacks)
	{
		OrderedDictionary<ItemStack, int> dict = new OrderedDictionary<ItemStack, int>();
		List<ItemStack> stackslist = new List<ItemStack>(stacks);
		while (stackslist.Count > 0)
		{
			ItemStack stack = stackslist[0];
			stackslist.RemoveAt(0);
			if (stack == null)
			{
				continue;
			}
			int cnt = 1;
			while (true)
			{
				ItemStack foundstack = stackslist.FirstOrDefault((ItemStack otherstack) => otherstack?.Equals(worldForResolve, stack, GlobalConstants.IgnoredStackAttributes) ?? false);
				if (foundstack == null)
				{
					break;
				}
				stackslist.Remove(foundstack);
				cnt++;
			}
			dict[stack] = cnt;
		}
		return dict;
	}
}
