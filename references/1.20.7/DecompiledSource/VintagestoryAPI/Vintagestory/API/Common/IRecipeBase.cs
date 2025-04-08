using System.Collections.Generic;

namespace Vintagestory.API.Common;

public interface IRecipeBase<T>
{
	AssetLocation Name { get; set; }

	bool Enabled { get; set; }

	IRecipeIngredient[] Ingredients { get; }

	IRecipeOutput Output { get; }

	Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world);

	bool Resolve(IWorldAccessor world, string sourceForErrorLogging);

	T Clone();
}
