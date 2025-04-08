using System;
using System.Collections.Generic;
using System.Linq;

namespace Vintagestory.API.Common;

/// <summary>
/// Creates a new base recipe type. Almost all recipe types extend from this.
/// </summary>
/// <typeparam name="T">The resulting recipe type.</typeparam>
[DocumentAsJson]
public abstract class RecipeBase<T> : IRecipeBase<T>
{
	/// <summary>
	/// The ID of the recipe. Automatically generated when the recipe is loaded.
	/// </summary>
	public int RecipeId;

	/// <summary>
	/// <!--<jsonoptional>Required</jsonoptional>-->
	/// An array of ingredients for this recipe. If only using a single ingredient, see <see cref="P:Vintagestory.API.Common.RecipeBase`1.Ingredient" />.<br />
	/// Required if not using <see cref="P:Vintagestory.API.Common.RecipeBase`1.Ingredient" />.
	/// </summary>
	[DocumentAsJson]
	public CraftingRecipeIngredient[] Ingredients;

	/// <summary>
	/// <!--<jsonoptional>Required</jsonoptional>-->
	/// The output when the recipe is successful. 
	/// </summary>
	[DocumentAsJson]
	public JsonItemStack Output;

	/// <summary>
	/// <!--<jsonoptional>Required</jsonoptional>-->
	/// A single ingredient for this recipe. If you need to use more than one ingredient, see <see cref="F:Vintagestory.API.Common.RecipeBase`1.Ingredients" />.<br />
	/// Required if not using <see cref="F:Vintagestory.API.Common.RecipeBase`1.Ingredients" />.
	/// </summary>
	[DocumentAsJson]
	public CraftingRecipeIngredient Ingredient
	{
		get
		{
			if (Ingredients == null || Ingredients.Length == 0)
			{
				return null;
			}
			return Ingredients[0];
		}
		set
		{
			Ingredients = new CraftingRecipeIngredient[1] { value };
		}
	}

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>Asset Path</jsondefault>-->
	/// Adds a name to this recipe. Used for logging, and determining helve hammer workability for smithing recipes.
	/// </summary>
	[DocumentAsJson]
	public AssetLocation Name { get; set; }

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>True</jsondefault>-->
	/// Should this recipe be loaded by the game?
	/// </summary>
	[DocumentAsJson]
	public bool Enabled { get; set; } = true;


	IRecipeIngredient[] IRecipeBase<T>.Ingredients => ((IEnumerable<CraftingRecipeIngredient>)Ingredients).Select((System.Func<CraftingRecipeIngredient, IRecipeIngredient>)((CraftingRecipeIngredient i) => i)).ToArray();

	IRecipeOutput IRecipeBase<T>.Output => Output;

	public abstract T Clone();

	public abstract Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world);

	public abstract bool Resolve(IWorldAccessor world, string sourceForErrorLogging);
}
