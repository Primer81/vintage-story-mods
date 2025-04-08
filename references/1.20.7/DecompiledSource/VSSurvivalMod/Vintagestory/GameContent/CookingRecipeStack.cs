using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class CookingRecipeStack : JsonItemStack
{
	[DocumentAsJson]
	public string ShapeElement;

	[DocumentAsJson]
	public string[] TextureMapping;

	[DocumentAsJson]
	public JsonItemStack CookedStack;

	public override void FromBytes(BinaryReader reader, IClassRegistryAPI instancer)
	{
		base.FromBytes(reader, instancer);
		if (!reader.ReadBoolean())
		{
			ShapeElement = reader.ReadString();
		}
		if (!reader.ReadBoolean())
		{
			TextureMapping = new string[2]
			{
				reader.ReadString(),
				reader.ReadString()
			};
		}
		if (!reader.ReadBoolean())
		{
			CookedStack = new JsonItemStack();
			CookedStack.FromBytes(reader, instancer);
		}
	}

	public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		writer.Write(ShapeElement == null);
		if (ShapeElement != null)
		{
			writer.Write(ShapeElement);
		}
		writer.Write(TextureMapping == null);
		if (TextureMapping != null)
		{
			writer.Write(TextureMapping[0]);
			writer.Write(TextureMapping[1]);
		}
		writer.Write(CookedStack == null);
		if (CookedStack != null)
		{
			CookedStack.ToBytes(writer);
		}
	}

	public new CookingRecipeStack Clone()
	{
		CookingRecipeStack stack = new CookingRecipeStack
		{
			Code = Code.Clone(),
			ResolvedItemstack = ResolvedItemstack?.Clone(),
			StackSize = StackSize,
			Type = Type,
			TextureMapping = (string[])TextureMapping?.Clone(),
			CookedStack = CookedStack?.Clone()
		};
		if (Attributes != null)
		{
			stack.Attributes = Attributes.Clone();
		}
		stack.ShapeElement = ShapeElement;
		return stack;
	}
}
