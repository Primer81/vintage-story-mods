using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class CookingRecipe : IByteSerializable
{
	[DocumentAsJson]
	public string Code;

	[DocumentAsJson]
	public CookingRecipeIngredient[] Ingredients;

	[DocumentAsJson]
	public bool Enabled = true;

	[DocumentAsJson]
	public CompositeShape Shape;

	[DocumentAsJson]
	public TransitionableProperties PerishableProps;

	[DocumentAsJson]
	public JsonItemStack CooksInto;

	public static Dictionary<string, ICookingRecipeNamingHelper> NamingRegistry;

	static CookingRecipe()
	{
		NamingRegistry = new Dictionary<string, ICookingRecipeNamingHelper>();
		NamingRegistry["porridge"] = new VanillaCookingRecipeNames();
		NamingRegistry["meatystew"] = new VanillaCookingRecipeNames();
		NamingRegistry["vegetablestew"] = new VanillaCookingRecipeNames();
		NamingRegistry["soup"] = new VanillaCookingRecipeNames();
		NamingRegistry["jam"] = new VanillaCookingRecipeNames();
		NamingRegistry["scrambledeggs"] = new VanillaCookingRecipeNames();
		NamingRegistry["glueportion-pitch-hot"] = new VanillaCookingRecipeNames();
		NamingRegistry["glueportion-pitch-cold"] = new VanillaCookingRecipeNames();
	}

	public bool Matches(ItemStack[] inputStacks)
	{
		int useless = 0;
		return Matches(inputStacks, ref useless);
	}

	public int GetQuantityServings(ItemStack[] stacks)
	{
		int quantity = 0;
		Matches(stacks, ref quantity);
		return quantity;
	}

	public string GetOutputName(IWorldAccessor worldForResolve, ItemStack[] inputStacks)
	{
		if (inputStacks.Any((ItemStack stack) => stack?.Collectible.Code.Path == "rot"))
		{
			return Lang.Get("Rotten Food");
		}
		ICookingRecipeNamingHelper namer = null;
		if (NamingRegistry.TryGetValue(Code, out namer))
		{
			return namer.GetNameForIngredients(worldForResolve, Code, inputStacks);
		}
		return Lang.Get("meal-" + Code);
	}

	public bool Matches(ItemStack[] inputStacks, ref int quantityServings)
	{
		List<ItemStack> inputStacksList = new List<ItemStack>(inputStacks);
		List<CookingRecipeIngredient> ingredientList = new List<CookingRecipeIngredient>(Ingredients);
		int totalOutputQuantity = 99999;
		int[] curQuantities = new int[ingredientList.Count];
		for (int l = 0; l < curQuantities.Length; l++)
		{
			curQuantities[l] = 0;
		}
		while (inputStacksList.Count > 0)
		{
			ItemStack inputStack = inputStacksList[0];
			inputStacksList.RemoveAt(0);
			if (inputStack == null)
			{
				continue;
			}
			bool found = false;
			for (int k = 0; k < ingredientList.Count; k++)
			{
				CookingRecipeIngredient ingred = ingredientList[k];
				if (ingred.Matches(inputStack) && curQuantities[k] < ingred.MaxQuantity)
				{
					int stackPortion = inputStack.StackSize;
					JsonObject attributes = inputStack.Collectible.Attributes;
					if (attributes != null && attributes["waterTightContainerProps"].Exists)
					{
						WaterTightContainableProps props2 = BlockLiquidContainerBase.GetContainableProps(inputStack);
						GetIngrendientFor(inputStack);
						stackPortion = (int)((float)inputStack.StackSize / props2.ItemsPerLitre / GetIngrendientFor(inputStack).PortionSizeLitres);
					}
					totalOutputQuantity = Math.Min(totalOutputQuantity, stackPortion);
					curQuantities[k]++;
					found = true;
					break;
				}
			}
			if (!found)
			{
				return false;
			}
		}
		for (int j = 0; j < ingredientList.Count; j++)
		{
			if (curQuantities[j] < ingredientList[j].MinQuantity)
			{
				return false;
			}
		}
		quantityServings = totalOutputQuantity;
		foreach (ItemStack stack in inputStacks)
		{
			if (stack == null)
			{
				continue;
			}
			JsonObject attributes2 = stack.Collectible.Attributes;
			if (attributes2 != null && attributes2["waterTightContainerProps"].Exists)
			{
				WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(stack);
				GetIngrendientFor(stack);
				if (stack.StackSize != (int)((float)quantityServings * props.ItemsPerLitre * GetIngrendientFor(stack).PortionSizeLitres))
				{
					return false;
				}
			}
			else if (stack.StackSize != quantityServings)
			{
				return false;
			}
		}
		return true;
	}

	public CookingRecipeIngredient GetIngrendientFor(ItemStack stack, params CookingRecipeIngredient[] ingredsToskip)
	{
		if (stack == null)
		{
			return null;
		}
		for (int i = 0; i < Ingredients.Length; i++)
		{
			if (Ingredients[i].Matches(stack) && !ingredsToskip.Contains(Ingredients[i]))
			{
				return Ingredients[i];
			}
		}
		return null;
	}

	public void Resolve(IServerWorldAccessor world, string sourceForErrorLogging)
	{
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i].Resolve(world, sourceForErrorLogging);
		}
		CooksInto?.Resolve(world, sourceForErrorLogging);
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Code);
		writer.Write(Ingredients.Length);
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i].ToBytes(writer);
		}
		writer.Write(Shape == null);
		if (Shape != null)
		{
			writer.Write(Shape.Base.ToString());
		}
		PerishableProps.ToBytes(writer);
		writer.Write(CooksInto != null);
		if (CooksInto != null)
		{
			CooksInto.ToBytes(writer);
		}
	}

	public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		Code = reader.ReadString();
		Ingredients = new CookingRecipeIngredient[reader.ReadInt32()];
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i] = new CookingRecipeIngredient();
			Ingredients[i].FromBytes(reader, resolver.ClassRegistry);
			Ingredients[i].Resolve(resolver, "[FromBytes]");
		}
		if (!reader.ReadBoolean())
		{
			Shape = new CompositeShape
			{
				Base = new AssetLocation(reader.ReadString())
			};
		}
		PerishableProps = new TransitionableProperties();
		PerishableProps.FromBytes(reader, resolver.ClassRegistry);
		if (reader.ReadBoolean())
		{
			CooksInto = new JsonItemStack();
			CooksInto.FromBytes(reader, resolver.ClassRegistry);
			CooksInto.Resolve(resolver, "[FromBytes]");
		}
	}
}
