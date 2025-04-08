using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class MetalAlloyIngredient : JsonItemStack
{
	[DocumentAsJson]
	public float MinRatio;

	[DocumentAsJson]
	public float MaxRatio;

	public override void FromBytes(BinaryReader reader, IClassRegistryAPI instancer)
	{
		base.FromBytes(reader, instancer);
		MinRatio = reader.ReadSingle();
		MaxRatio = reader.ReadSingle();
	}

	public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(MinRatio);
		writer.Write(MaxRatio);
	}

	public new MetalAlloyIngredient Clone()
	{
		MetalAlloyIngredient stack = new MetalAlloyIngredient
		{
			Code = Code,
			StackSize = StackSize,
			Type = Type,
			MinRatio = MinRatio,
			MaxRatio = MaxRatio
		};
		if (Attributes != null)
		{
			stack.Attributes = Attributes.Clone();
		}
		return stack;
	}
}
