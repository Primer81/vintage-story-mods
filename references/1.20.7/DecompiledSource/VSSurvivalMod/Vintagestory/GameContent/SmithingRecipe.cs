using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class SmithingRecipe : LayeredVoxelRecipe<SmithingRecipe>, IByteSerializable
{
	public override int QuantityLayers => 6;

	public override string RecipeCategoryCode => "smithing";

	protected override bool RotateRecipe => true;

	public override SmithingRecipe Clone()
	{
		return new SmithingRecipe
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
