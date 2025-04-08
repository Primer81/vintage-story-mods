using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class ConstructionIgredient : CraftingRecipeIngredient
{
	public string StoreWildCard;

	public new ConstructionIgredient Clone()
	{
		ConstructionIgredient constructionIgredient = CloneTo<ConstructionIgredient>();
		constructionIgredient.StoreWildCard = StoreWildCard;
		return constructionIgredient;
	}
}
