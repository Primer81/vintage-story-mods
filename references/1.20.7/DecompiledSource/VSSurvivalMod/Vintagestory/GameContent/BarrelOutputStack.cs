using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BarrelOutputStack : JsonItemStack
{
	[DocumentAsJson]
	public float Litres;

	public override void FromBytes(BinaryReader reader, IClassRegistryAPI instancer)
	{
		base.FromBytes(reader, instancer);
		Litres = reader.ReadSingle();
	}

	public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(Litres);
	}

	public new BarrelOutputStack Clone()
	{
		BarrelOutputStack stack = new BarrelOutputStack
		{
			Code = Code.Clone(),
			ResolvedItemstack = ResolvedItemstack?.Clone(),
			StackSize = StackSize,
			Type = Type,
			Litres = Litres
		};
		if (Attributes != null)
		{
			stack.Attributes = Attributes.Clone();
		}
		return stack;
	}
}
