using System.IO;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

/// <summary>
/// An ingredient for a grid recipe.
/// </summary>
public class GridRecipeIngredient : CraftingRecipeIngredient
{
	/// <summary>
	/// The character used in the grid recipe pattern that matches this ingredient. Generated when the recipe is loaded.
	/// </summary>
	public string PatternCode;

	public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(PatternCode);
	}

	public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		base.FromBytes(reader, resolver);
		PatternCode = reader.ReadString().DeDuplicate();
	}
}
