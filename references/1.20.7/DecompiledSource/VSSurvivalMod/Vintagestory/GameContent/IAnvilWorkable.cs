using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface IAnvilWorkable
{
	int GetRequiredAnvilTier(ItemStack stack);

	List<SmithingRecipe> GetMatchingRecipes(ItemStack stack);

	bool CanWork(ItemStack stack);

	ItemStack TryPlaceOn(ItemStack stack, BlockEntityAnvil beAnvil);

	ItemStack GetBaseMaterial(ItemStack stack);

	EnumHelveWorkableMode GetHelveWorkableMode(ItemStack stack, BlockEntityAnvil beAnvil);
}
