using System.Collections.Generic;
using System.IO;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

[DocumentAsJson]
public class BarrelRecipe : IByteSerializable, IRecipeBase<BarrelRecipe>
{
	[DocumentAsJson]
	public int RecipeId;

	[DocumentAsJson]
	public BarrelRecipeIngredient[] Ingredients;

	[DocumentAsJson]
	public BarrelOutputStack Output;

	[DocumentAsJson]
	public string Code;

	[DocumentAsJson]
	public double SealHours;

	[DocumentAsJson]
	public AssetLocation Name { get; set; }

	[DocumentAsJson]
	public bool Enabled { get; set; } = true;


	IRecipeIngredient[] IRecipeBase<BarrelRecipe>.Ingredients => Ingredients;

	IRecipeOutput IRecipeBase<BarrelRecipe>.Output => Output;

	public bool Matches(ItemSlot[] inputSlots, out int outputStackSize)
	{
		outputStackSize = 0;
		List<KeyValuePair<ItemSlot, BarrelRecipeIngredient>> matched = pairInput(inputSlots);
		if (matched == null)
		{
			return false;
		}
		outputStackSize = getOutputSize(matched);
		return outputStackSize >= 0;
	}

	private List<KeyValuePair<ItemSlot, BarrelRecipeIngredient>> pairInput(ItemSlot[] inputStacks)
	{
		List<BarrelRecipeIngredient> ingredientList = new List<BarrelRecipeIngredient>(Ingredients);
		Queue<ItemSlot> inputSlotsList = new Queue<ItemSlot>();
		foreach (ItemSlot val in inputStacks)
		{
			if (!val.Empty)
			{
				inputSlotsList.Enqueue(val);
			}
		}
		if (inputSlotsList.Count != Ingredients.Length)
		{
			return null;
		}
		List<KeyValuePair<ItemSlot, BarrelRecipeIngredient>> matched = new List<KeyValuePair<ItemSlot, BarrelRecipeIngredient>>();
		while (inputSlotsList.Count > 0)
		{
			ItemSlot inputSlot = inputSlotsList.Dequeue();
			bool found = false;
			for (int i = 0; i < ingredientList.Count; i++)
			{
				BarrelRecipeIngredient ingred = ingredientList[i];
				if (ingred.SatisfiesAsIngredient(inputSlot.Itemstack))
				{
					matched.Add(new KeyValuePair<ItemSlot, BarrelRecipeIngredient>(inputSlot, ingred));
					found = true;
					ingredientList.RemoveAt(i);
					break;
				}
			}
			if (!found)
			{
				return null;
			}
		}
		if (ingredientList.Count > 0)
		{
			return null;
		}
		return matched;
	}

	private int getOutputSize(List<KeyValuePair<ItemSlot, BarrelRecipeIngredient>> matched)
	{
		int outQuantityMul = -1;
		foreach (KeyValuePair<ItemSlot, BarrelRecipeIngredient> val in matched)
		{
			ItemSlot inputSlot2 = val.Key;
			BarrelRecipeIngredient ingred2 = val.Value;
			if (!ingred2.ConsumeQuantity.HasValue)
			{
				outQuantityMul = inputSlot2.StackSize / ingred2.Quantity;
			}
		}
		if (outQuantityMul == -1)
		{
			return -1;
		}
		foreach (KeyValuePair<ItemSlot, BarrelRecipeIngredient> val2 in matched)
		{
			ItemSlot inputSlot = val2.Key;
			BarrelRecipeIngredient ingred = val2.Value;
			if (!ingred.ConsumeQuantity.HasValue)
			{
				if (inputSlot.StackSize % ingred.Quantity != 0)
				{
					return -1;
				}
				if (outQuantityMul != inputSlot.StackSize / ingred.Quantity)
				{
					return -1;
				}
			}
			else if (inputSlot.StackSize < ingred.Quantity * outQuantityMul)
			{
				return -1;
			}
		}
		return Output.StackSize * outQuantityMul;
	}

	public bool TryCraftNow(ICoreAPI api, double nowSealedHours, ItemSlot[] inputslots)
	{
		if (SealHours > 0.0 && nowSealedHours < SealHours)
		{
			return false;
		}
		List<KeyValuePair<ItemSlot, BarrelRecipeIngredient>> matched = pairInput(inputslots);
		ItemStack mixedStack = Output.ResolvedItemstack.Clone();
		mixedStack.StackSize = getOutputSize(matched);
		if (mixedStack.StackSize < 0)
		{
			return false;
		}
		TransitionableProperties[] props = mixedStack.Collectible.GetTransitionableProperties(api.World, mixedStack, null);
		TransitionableProperties perishProps = ((props != null && props.Length != 0) ? props[0] : null);
		if (perishProps != null)
		{
			CollectibleObject.CarryOverFreshness(api, inputslots, new ItemStack[1] { mixedStack }, perishProps);
		}
		ItemStack remainStack = null;
		foreach (KeyValuePair<ItemSlot, BarrelRecipeIngredient> val in matched)
		{
			if (val.Value.ConsumeQuantity.HasValue)
			{
				remainStack = val.Key.Itemstack;
				remainStack.StackSize -= val.Value.ConsumeQuantity.Value * (mixedStack.StackSize / Output.StackSize);
				if (remainStack.StackSize <= 0)
				{
					remainStack = null;
				}
				break;
			}
		}
		if (shouldBeInLiquidSlot(mixedStack))
		{
			inputslots[0].Itemstack = remainStack;
			inputslots[1].Itemstack = mixedStack;
		}
		else
		{
			inputslots[1].Itemstack = remainStack;
			inputslots[0].Itemstack = mixedStack;
		}
		inputslots[0].MarkDirty();
		inputslots[1].MarkDirty();
		return true;
	}

	public bool shouldBeInLiquidSlot(ItemStack stack)
	{
		if (stack == null)
		{
			return false;
		}
		return (stack.ItemAttributes?["waterTightContainerProps"].Exists).GetValueOrDefault();
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Code);
		writer.Write(Ingredients.Length);
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i].ToBytes(writer);
		}
		Output.ToBytes(writer);
		writer.Write(SealHours);
	}

	public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		Code = reader.ReadString();
		Ingredients = new BarrelRecipeIngredient[reader.ReadInt32()];
		for (int i = 0; i < Ingredients.Length; i++)
		{
			Ingredients[i] = new BarrelRecipeIngredient();
			Ingredients[i].FromBytes(reader, resolver);
			Ingredients[i].Resolve(resolver, "Barrel Recipe (FromBytes)");
		}
		Output = new BarrelOutputStack();
		Output.FromBytes(reader, resolver.ClassRegistry);
		Output.Resolve(resolver, "Barrel Recipe (FromBytes)");
		SealHours = reader.ReadDouble();
	}

	public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
	{
		Dictionary<string, string[]> mappings = new Dictionary<string, string[]>();
		if (Ingredients == null || Ingredients.Length == 0)
		{
			return mappings;
		}
		BarrelRecipeIngredient[] ingredients = Ingredients;
		foreach (BarrelRecipeIngredient ingred in ingredients)
		{
			if (!ingred.Code.Path.Contains('*'))
			{
				continue;
			}
			int wildcardStartLen = ingred.Code.Path.IndexOf('*');
			int wildcardEndLen = ingred.Code.Path.Length - wildcardStartLen - 1;
			List<string> codes = new List<string>();
			if (ingred.Type == EnumItemClass.Block)
			{
				foreach (Block block in world.Blocks)
				{
					if (!block.IsMissing && WildcardUtil.Match(ingred.Code, block.Code))
					{
						string code = block.Code.Path.Substring(wildcardStartLen);
						string codepart = code.Substring(0, code.Length - wildcardEndLen);
						if (ingred.AllowedVariants == null || ingred.AllowedVariants.Contains(codepart))
						{
							codes.Add(codepart);
						}
					}
				}
			}
			else
			{
				foreach (Item item in world.Items)
				{
					if (!(item.Code == null) && !item.IsMissing && WildcardUtil.Match(ingred.Code, item.Code))
					{
						string code2 = item.Code.Path.Substring(wildcardStartLen);
						string codepart2 = code2.Substring(0, code2.Length - wildcardEndLen);
						if (ingred.AllowedVariants == null || ingred.AllowedVariants.Contains(codepart2))
						{
							codes.Add(codepart2);
						}
					}
				}
			}
			mappings[ingred.Name ?? ("wildcard" + mappings.Count)] = codes.ToArray();
		}
		return mappings;
	}

	public bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
	{
		bool ok = true;
		for (int i = 0; i < Ingredients.Length; i++)
		{
			BarrelRecipeIngredient ingred = Ingredients[i];
			bool iOk = ingred.Resolve(world, sourceForErrorLogging);
			ok = ok && iOk;
			if (!iOk)
			{
				continue;
			}
			WaterTightContainableProps lprops2 = BlockLiquidContainerBase.GetContainableProps(ingred.ResolvedItemstack);
			if (lprops2 == null)
			{
				continue;
			}
			if (ingred.Litres < 0f)
			{
				if (ingred.Quantity > 0)
				{
					world.Logger.Warning("Barrel recipe {0}, ingredient {1} does not define a litres attribute but a quantity, will assume quantity=litres for backwards compatibility.", sourceForErrorLogging, ingred.Code);
					ingred.Litres = ingred.Quantity;
					ingred.ConsumeLitres = ingred.ConsumeQuantity;
				}
				else
				{
					ingred.Litres = 1f;
				}
			}
			ingred.Quantity = (int)(lprops2.ItemsPerLitre * ingred.Litres);
			if (ingred.ConsumeLitres.HasValue)
			{
				ingred.ConsumeQuantity = (int)(lprops2.ItemsPerLitre * ingred.ConsumeLitres).Value;
			}
		}
		ok &= Output.Resolve(world, sourceForErrorLogging);
		if (ok)
		{
			WaterTightContainableProps lprops = BlockLiquidContainerBase.GetContainableProps(Output.ResolvedItemstack);
			if (lprops != null)
			{
				if (Output.Litres < 0f)
				{
					if (Output.Quantity > 0)
					{
						world.Logger.Warning("Barrel recipe {0}, output {1} does not define a litres attribute but a stacksize, will assume stacksize=litres for backwards compatibility.", sourceForErrorLogging, Output.Code);
						Output.Litres = Output.Quantity;
					}
					else
					{
						Output.Litres = 1f;
					}
				}
				Output.Quantity = (int)(lprops.ItemsPerLitre * Output.Litres);
			}
		}
		return ok;
	}

	public BarrelRecipe Clone()
	{
		BarrelRecipeIngredient[] ingredients = new BarrelRecipeIngredient[Ingredients.Length];
		for (int i = 0; i < Ingredients.Length; i++)
		{
			ingredients[i] = Ingredients[i].Clone();
		}
		return new BarrelRecipe
		{
			SealHours = SealHours,
			Output = Output.Clone(),
			Code = Code,
			Enabled = Enabled,
			Name = Name,
			RecipeId = RecipeId,
			Ingredients = ingredients
		};
	}
}
