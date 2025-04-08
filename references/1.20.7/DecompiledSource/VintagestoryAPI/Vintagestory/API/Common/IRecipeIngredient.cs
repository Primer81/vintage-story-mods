namespace Vintagestory.API.Common;

public interface IRecipeIngredient
{
	string Name { get; }

	AssetLocation Code { get; set; }
}
