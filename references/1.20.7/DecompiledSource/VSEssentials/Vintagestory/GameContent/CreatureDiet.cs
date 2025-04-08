using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class CreatureDiet
{
	public EnumFoodCategory[] FoodCategories;

	public string[] FoodTags;

	public string[] SkipFoodTags;

	public bool Matches(EnumFoodCategory foodSourceCategory, params string[] foodSourceTags)
	{
		if (SkipFoodTags != null && foodSourceTags != null)
		{
			for (int j = 0; j < foodSourceTags.Length; j++)
			{
				if (SkipFoodTags.Contains(foodSourceTags[j]))
				{
					return false;
				}
			}
		}
		if (FoodCategories != null && FoodCategories.Contains(foodSourceCategory))
		{
			return true;
		}
		if (FoodTags != null && foodSourceTags != null)
		{
			for (int i = 0; i < foodSourceTags.Length; i++)
			{
				if (FoodTags.Contains(foodSourceTags[i]))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool Matches(ItemStack itemstack)
	{
		CollectibleObject collectible = itemstack.Collectible;
		EnumFoodCategory foodCat = collectible.NutritionProps?.FoodCategory ?? EnumFoodCategory.NoNutrition;
		string[] foodTags = collectible.Attributes?["foodTags"].AsArray<string>();
		return Matches(foodCat, foodTags);
	}
}
