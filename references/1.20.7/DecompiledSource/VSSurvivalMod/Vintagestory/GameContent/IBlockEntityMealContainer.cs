using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public interface IBlockEntityMealContainer
{
	string RecipeCode { get; set; }

	InventoryBase inventory { get; }

	float QuantityServings { get; set; }

	ItemStack[] GetNonEmptyContentStacks(bool cloned = true);

	void MarkDirty(bool redrawonclient, IPlayer skipPlayer = null);
}
