using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public class GridRecipeAndUnnamedIngredients
{
	public GridRecipe Recipe;

	public Dictionary<int, ItemStack[]> unnamedIngredients;
}
