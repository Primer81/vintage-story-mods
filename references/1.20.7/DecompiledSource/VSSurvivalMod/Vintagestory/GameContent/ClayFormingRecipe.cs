using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class ClayFormingRecipe : LayeredVoxelRecipe<ClayFormingRecipe>, IByteSerializable
{
	public override int QuantityLayers => 16;

	public override string RecipeCategoryCode => "clay forming";

	public override ClayFormingRecipe Clone()
	{
		ClayFormingRecipe recipe = new ClayFormingRecipe();
		recipe.Pattern = new string[Pattern.Length][];
		for (int i = 0; i < recipe.Pattern.Length; i++)
		{
			recipe.Pattern[i] = (string[])Pattern[i].Clone();
		}
		recipe.Ingredient = base.Ingredient.Clone();
		recipe.Output = Output.Clone();
		recipe.Name = base.Name;
		return recipe;
	}

	void IByteSerializable.ToBytes(BinaryWriter writer)
	{
		ToBytes(writer);
	}

	void IByteSerializable.FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		FromBytes(reader, resolver);
	}
}
