using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface ICookingRecipeNamingHelper
{
	string GetNameForIngredients(IWorldAccessor worldForResolve, string recipeCode, ItemStack[] stacks);
}
