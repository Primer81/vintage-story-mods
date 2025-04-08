using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class KnappingRecipe : LayeredVoxelRecipe<KnappingRecipe>, IByteSerializable
{
	public override int QuantityLayers => 1;

	public override string RecipeCategoryCode => "knapping";

	public override KnappingRecipe Clone()
	{
		return new KnappingRecipe
		{
			Pattern = (string[][])Pattern.Clone(),
			Ingredient = base.Ingredient.Clone(),
			Output = Output.Clone(),
			Name = base.Name,
			RecipeId = RecipeId
		};
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
