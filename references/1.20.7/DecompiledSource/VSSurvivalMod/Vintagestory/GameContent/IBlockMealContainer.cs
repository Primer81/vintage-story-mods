using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface IBlockMealContainer
{
	void SetContents(string recipeCode, ItemStack containerStack, ItemStack[] stacks, float quantityServings = 1f);

	string GetRecipeCode(IWorldAccessor world, ItemStack containerStack);

	ItemStack[] GetContents(IWorldAccessor world, ItemStack containerStack);

	ItemStack[] GetNonEmptyContents(IWorldAccessor world, ItemStack containerStack);

	float GetQuantityServings(IWorldAccessor world, ItemStack containerStack);

	void SetQuantityServings(IWorldAccessor world, ItemStack containerStack, float quantityServings);
}
