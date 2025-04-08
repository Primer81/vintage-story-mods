using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BarrelRecipeIngredient : CraftingRecipeIngredient
{
	[DocumentAsJson]
	public int? ConsumeQuantity;

	[DocumentAsJson]
	public float Litres = -1f;

	[DocumentAsJson]
	public float? ConsumeLitres;

	public new BarrelRecipeIngredient Clone()
	{
		BarrelRecipeIngredient stack = new BarrelRecipeIngredient
		{
			Code = base.Code.Clone(),
			Type = Type,
			Name = base.Name,
			Quantity = Quantity,
			ConsumeQuantity = ConsumeQuantity,
			ConsumeLitres = ConsumeLitres,
			IsWildCard = IsWildCard,
			IsTool = IsTool,
			Litres = Litres,
			AllowedVariants = ((AllowedVariants == null) ? null : ((string[])AllowedVariants.Clone())),
			ResolvedItemstack = ResolvedItemstack?.Clone(),
			ReturnedStack = ReturnedStack?.Clone()
		};
		if (Attributes != null)
		{
			stack.Attributes = Attributes.Clone();
		}
		return stack;
	}

	public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		base.FromBytes(reader, resolver);
		if (reader.ReadBoolean())
		{
			ConsumeQuantity = reader.ReadInt32();
		}
		else
		{
			ConsumeQuantity = null;
		}
		if (reader.ReadBoolean())
		{
			ConsumeLitres = reader.ReadSingle();
		}
		else
		{
			ConsumeLitres = null;
		}
		Litres = reader.ReadSingle();
	}

	public override void ToBytes(BinaryWriter writer)
	{
		base.ToBytes(writer);
		if (ConsumeQuantity.HasValue)
		{
			writer.Write(value: true);
			writer.Write(ConsumeQuantity.Value);
		}
		else
		{
			writer.Write(value: false);
		}
		if (ConsumeLitres.HasValue)
		{
			writer.Write(value: true);
			writer.Write(ConsumeLitres.Value);
		}
		else
		{
			writer.Write(value: false);
		}
		writer.Write(Litres);
	}
}
