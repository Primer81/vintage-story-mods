using System.Collections.Generic;
using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class CookingRecipeIngredient
{
	[DocumentAsJson]
	public string Code;

	[DocumentAsJson]
	public int MinQuantity;

	[DocumentAsJson]
	public int MaxQuantity;

	[DocumentAsJson]
	public float PortionSizeLitres;

	[DocumentAsJson]
	public CookingRecipeStack[] ValidStacks;

	public IWorldAccessor world;

	public virtual void FromBytes(BinaryReader reader, IClassRegistryAPI instancer)
	{
		Code = reader.ReadString();
		MinQuantity = reader.ReadInt32();
		MaxQuantity = reader.ReadInt32();
		PortionSizeLitres = reader.ReadSingle();
		int q = reader.ReadInt32();
		ValidStacks = new CookingRecipeStack[q];
		for (int i = 0; i < q; i++)
		{
			ValidStacks[i] = new CookingRecipeStack();
			ValidStacks[i].FromBytes(reader, instancer);
		}
	}

	public virtual void ToBytes(BinaryWriter writer)
	{
		writer.Write(Code);
		writer.Write(MinQuantity);
		writer.Write(MaxQuantity);
		writer.Write(PortionSizeLitres);
		writer.Write(ValidStacks.Length);
		for (int i = 0; i < ValidStacks.Length; i++)
		{
			ValidStacks[i].ToBytes(writer);
		}
	}

	public CookingRecipeIngredient Clone()
	{
		CookingRecipeIngredient ingredient = new CookingRecipeIngredient
		{
			Code = Code,
			MinQuantity = MinQuantity,
			MaxQuantity = MaxQuantity,
			PortionSizeLitres = PortionSizeLitres
		};
		ingredient.ValidStacks = new CookingRecipeStack[ValidStacks.Length];
		for (int i = 0; i < ValidStacks.Length; i++)
		{
			ingredient.ValidStacks[i] = ValidStacks[i].Clone();
		}
		return ingredient;
	}

	public bool Matches(ItemStack inputStack)
	{
		return GetMatchingStack(inputStack) != null;
	}

	public CookingRecipeStack GetMatchingStack(ItemStack inputStack)
	{
		if (inputStack == null)
		{
			return null;
		}
		for (int i = 0; i < ValidStacks.Length; i++)
		{
			bool isWildCard = ValidStacks[i].Code.Path.Contains('*');
			if ((isWildCard && inputStack.Collectible.WildCardMatch(ValidStacks[i].Code)) || (!isWildCard && inputStack.Equals(world, ValidStacks[i].ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)) || (ValidStacks[i].CookedStack?.ResolvedItemstack != null && inputStack.Equals(world, ValidStacks[i].CookedStack.ResolvedItemstack, GlobalConstants.IgnoredStackAttributes)))
			{
				return ValidStacks[i];
			}
		}
		return null;
	}

	internal void Resolve(IWorldAccessor world, string sourceForErrorLogging)
	{
		this.world = world;
		List<CookingRecipeStack> resolvedStacks = new List<CookingRecipeStack>();
		for (int i = 0; i < ValidStacks.Length; i++)
		{
			CookingRecipeStack cstack = ValidStacks[i];
			if (cstack.Code.Path.Contains('*'))
			{
				resolvedStacks.Add(cstack);
				continue;
			}
			if (cstack.Resolve(world, sourceForErrorLogging))
			{
				resolvedStacks.Add(cstack);
			}
			cstack.CookedStack?.Resolve(world, sourceForErrorLogging);
		}
		ValidStacks = resolvedStacks.ToArray();
	}
}
