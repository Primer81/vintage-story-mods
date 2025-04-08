namespace Vintagestory.API.Common;

/// <summary>
/// Types of nutrition for foods.
/// </summary>
[DocumentAsJson]
public enum EnumFoodCategory
{
	NoNutrition = -1,
	Fruit,
	Vegetable,
	Protein,
	Grain,
	Dairy,
	Unknown
}
