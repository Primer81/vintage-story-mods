using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class AlloyRecipe : IByteSerializable
{
	[DocumentAsJson]
	public MetalAlloyIngredient[] Ingredients;

	[DocumentAsJson]
	public JsonItemStack Output;

	[DocumentAsJson]
	public bool Enabled = true;

	public bool Matches(ItemStack[] inputStacks, bool useSmeltedWhereApplicable = true)
	{
		List<MatchedSmeltableStackAlloy> mergedStacks = mergeAndCompareStacks(inputStacks, useSmeltedWhereApplicable);
		if (mergedStacks == null)
		{
			return false;
		}
		double totalOutputStacksize = 0.0;
		foreach (MatchedSmeltableStackAlloy matchedstack in mergedStacks)
		{
			totalOutputStacksize += matchedstack.stackSize;
		}
		foreach (MatchedSmeltableStackAlloy item in mergedStacks)
		{
			int rationInt = (int)Math.Round(item.stackSize / totalOutputStacksize * 10000.0);
			int min = (int)Math.Round(item.ingred.MinRatio * 10000f);
			int max = (int)Math.Round(item.ingred.MaxRatio * 10000f);
			if (rationInt < min || rationInt > max)
			{
				return false;
			}
		}
		return true;
	}

	public void Resolve(IServerWorldAccessor world, string sourceForErrorLogging)
	{
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i].Resolve(world, sourceForErrorLogging);
		}
		Output.Resolve(world, sourceForErrorLogging);
	}

	public double GetTotalOutputQuantity(ItemStack[] stacks, bool useSmeltedWhereAppicable = true)
	{
		List<MatchedSmeltableStackAlloy> mergedStacks = mergeAndCompareStacks(stacks, useSmeltedWhereAppicable);
		if (mergedStacks == null)
		{
			return 0.0;
		}
		double totalOutputStacksize = 0.0;
		foreach (MatchedSmeltableStackAlloy matchedstack in mergedStacks)
		{
			totalOutputStacksize += matchedstack.stackSize;
		}
		return totalOutputStacksize;
	}

	private List<MatchedSmeltableStackAlloy> mergeAndCompareStacks(ItemStack[] inputStacks, bool useSmeltedWhereApplicable)
	{
		List<MatchedSmeltableStackAlloy> mergedStacks = new List<MatchedSmeltableStackAlloy>();
		List<MetalAlloyIngredient> ingredients = new List<MetalAlloyIngredient>(Ingredients);
		for (int i = 0; i < inputStacks.Length; i++)
		{
			if (inputStacks[i] == null)
			{
				continue;
			}
			ItemStack stack = inputStacks[i];
			float stackSize = stack.StackSize;
			if (useSmeltedWhereApplicable && stack.Collectible.CombustibleProps?.SmeltedStack != null)
			{
				stackSize /= (float)stack.Collectible.CombustibleProps.SmeltedRatio;
				stack = stack.Collectible.CombustibleProps.SmeltedStack.ResolvedItemstack;
			}
			bool exists = false;
			for (int j = 0; j < mergedStacks.Count; j++)
			{
				if (stack.Class == mergedStacks[j].stack.Class && stack.Id == mergedStacks[j].stack.Id)
				{
					mergedStacks[j].stackSize = Math.Round(mergedStacks[j].stackSize + (double)stackSize, 2);
					exists = true;
					break;
				}
			}
			if (!exists)
			{
				MetalAlloyIngredient ingred = getIgrendientFor(stack, ingredients);
				if (ingred == null)
				{
					return null;
				}
				mergedStacks.Add(new MatchedSmeltableStackAlloy
				{
					stack = stack.Clone(),
					ingred = ingred,
					stackSize = stackSize
				});
			}
		}
		if (ingredients.Count > 0)
		{
			return null;
		}
		return mergedStacks;
	}

	private MetalAlloyIngredient getIgrendientFor(ItemStack stack, List<MetalAlloyIngredient> ingredients)
	{
		if (stack == null)
		{
			return null;
		}
		for (int i = 0; i < ingredients.Count; i++)
		{
			ItemStack ingredientstack = ingredients[i].ResolvedItemstack;
			if (ingredientstack.Class == stack.Class && ingredientstack.Id == stack.Id)
			{
				MetalAlloyIngredient result = ingredients[i];
				ingredients.Remove(ingredients[i]);
				return result;
			}
		}
		return null;
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Ingredients.Length);
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i].ToBytes(writer);
		}
		Output.ToBytes(writer);
	}

	public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		Ingredients = new MetalAlloyIngredient[reader.ReadInt32()];
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i] = new MetalAlloyIngredient();
			Ingredients[i].FromBytes(reader, resolver.ClassRegistry);
			Ingredients[i].Resolve(resolver, "[FromBytes]");
		}
		Output = new JsonItemStack();
		Output.FromBytes(reader, resolver.ClassRegistry);
		Output.Resolve(resolver, "[FromBytes]");
	}
}
