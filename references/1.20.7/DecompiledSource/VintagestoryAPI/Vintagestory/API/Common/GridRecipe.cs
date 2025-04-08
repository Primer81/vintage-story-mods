using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

/// <summary>
/// Represents a crafting recipe to be made on the crafting grid.
/// </summary>
/// <example><code language="json">
///             {
///             	"ingredientPattern": "GS,S_",
///             	"ingredients": {
///             		"G": {
///             			"type": "item",
///             			"code": "drygrass"
///             		},
///             		"S": {
///             			"type": "item",
///             			"code": "stick"
///             		}
///             	},
///             	"width": 2,
///             	"height": 2,
///             	"output": {
///             		"type": "item",
///             		"code": "firestarter"
///             	}
///             }
/// </code></example>
[DocumentAsJson]
public class GridRecipe : IByteSerializable
{
	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>True</jsondefault>-->
	/// If set to false, the recipe will never be loaded.
	/// If loaded, you can use this field to disable recipes during runtime.
	/// </summary>
	[DocumentAsJson]
	public bool Enabled = true;

	/// <summary>
	/// <!--<jsonoptional>Required</jsonoptional>-->
	/// The pattern of the ingredient. Order for a 3x3 recipe:<br />
	/// 1 2 3<br />
	/// 4 5 6<br />
	/// 7 8 9<br />
	/// Order for a 2x2 recipe:<br />
	/// 1 2<br />
	/// 3 4<br />
	/// Commas seperate each horizontal row, and an underscore ( _ ) marks a space as empty.
	/// <br />Note: from game version 1.20.4, this becomes <b>null on server-side</b> after completion of recipe resolving during server start-up phase
	/// </summary>
	[DocumentAsJson]
	public string IngredientPattern;

	/// <summary>
	/// <!--<jsonoptional>Required</jsonoptional>-->
	/// The recipes ingredients in any order, including the code used in the ingredient pattern.
	/// <br />Note: from game version 1.20.4, this becomes <b>null on server-side</b> after completion of recipe resolving during server start-up phase
	/// </summary>
	[DocumentAsJson]
	public Dictionary<string, CraftingRecipeIngredient> Ingredients;

	/// <summary>
	/// <!--<jsonoptional>Recommended</jsonoptional><jsondefault>3</jsondefault>-->
	/// Required grid width for crafting this recipe 
	/// </summary>
	[DocumentAsJson]
	public int Width = 3;

	/// <summary>
	/// <!--<jsonoptional>Recommended</jsonoptional><jsondefault>3</jsondefault>-->
	/// Required grid height for crafting this recipe 
	/// </summary>
	[DocumentAsJson]
	public int Height = 3;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// Info used by the handbook. By default, all recipes for an object will appear in a single preview. This allows you to split grid recipe previews into multiple.
	/// </summary>
	[DocumentAsJson]
	public int RecipeGroup;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>True</jsondefault>-->
	/// Used by the handbook. If false, will not appear in the "Created by" section
	/// </summary>
	[DocumentAsJson]
	public bool ShowInCreatedBy = true;

	/// <summary>
	/// <!--<jsonoptional>Required</jsonoptional>-->
	/// The resulting stack when the recipe is created.
	/// </summary>
	[DocumentAsJson]
	public CraftingRecipeIngredient Output;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>False</jsondefault>-->
	/// Whether the order of input items should be respected
	/// </summary>
	[DocumentAsJson]
	public bool Shapeless;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>Asset Location</jsondefault>-->
	/// Name of the recipe. Used for logging, and some specific uses. Recipes for repairing objects must contain 'repair' in the name.
	/// </summary>
	[DocumentAsJson]
	public AssetLocation Name;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// Optional attribute data that you can attach any data to. Useful for code mods, but also required when using liquid ingredients.<br />
	/// See dough.json grid recipe file for example.
	/// </summary>
	[JsonConverter(typeof(JsonAttributesConverter))]
	[DocumentAsJson]
	public JsonObject Attributes;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// If set, only players with given trait can use this recipe. See config/traits.json for a list of traits.
	/// </summary>
	[DocumentAsJson]
	public string RequiresTrait;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>True</jsondefault>-->
	/// If true, the output item will have its durability averaged over the input items
	/// </summary>
	[DocumentAsJson]
	public bool AverageDurability = true;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>None</jsondefault>-->
	/// If set, it will copy over the itemstack attributes from given ingredient code
	/// </summary>
	[DocumentAsJson]
	public string CopyAttributesFrom;

	/// <summary>
	/// A set of ingredients with their pattern codes resolved into a single object.
	/// </summary>
	public GridRecipeIngredient[] resolvedIngredients;

	private IWorldAccessor world;

	/// <summary>
	/// Turns Ingredients into IItemStacks
	/// </summary>
	/// <param name="world"></param>
	/// <returns>True on successful resolve</returns>
	public bool ResolveIngredients(IWorldAccessor world)
	{
		this.world = world;
		IngredientPattern = IngredientPattern.Replace(",", "").Replace("\t", "").Replace("\r", "")
			.Replace("\n", "")
			.DeDuplicate();
		if (IngredientPattern == null)
		{
			world.Logger.Error("Grid Recipe with output {0} has no ingredient pattern.", Output);
			return false;
		}
		if (Width * Height != IngredientPattern.Length)
		{
			world.Logger.Error("Grid Recipe with output {0} has and incorrect ingredient pattern length. Ignoring recipe.", Output);
			return false;
		}
		resolvedIngredients = new GridRecipeIngredient[Width * Height];
		for (int i = 0; i < IngredientPattern.Length; i++)
		{
			char charcode = IngredientPattern[i];
			if (charcode != ' ' && charcode != '_')
			{
				string code = charcode.ToString();
				if (!Ingredients.TryGetValue(code, out var craftingIngredient))
				{
					world.Logger.Error("Grid Recipe with output {0} contains an ingredient pattern code {1} but supplies no ingredient for it.", Output, code);
					return false;
				}
				if (!craftingIngredient.Resolve(world, "Grid recipe"))
				{
					world.Logger.Error("Grid Recipe with output {0} contains an ingredient that cannot be resolved: {1}", Output, craftingIngredient);
					return false;
				}
				GridRecipeIngredient ingredient = craftingIngredient.CloneTo<GridRecipeIngredient>();
				ingredient.PatternCode = code;
				resolvedIngredients[i] = ingredient;
			}
		}
		if (!Output.Resolve(world, "Grid recipe"))
		{
			world.Logger.Error("Grid Recipe '{0}': Output {1} cannot be resolved", Name, Output);
			return false;
		}
		return true;
	}

	public virtual void FreeRAMServer()
	{
		IngredientPattern = null;
		Ingredients = null;
	}

	/// <summary>
	/// Resolves Wildcards in the ingredients
	/// </summary>
	/// <param name="world"></param>
	/// <returns></returns>
	public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
	{
		Dictionary<string, string[]> mappings = new Dictionary<string, string[]>();
		foreach (KeyValuePair<string, CraftingRecipeIngredient> val in Ingredients)
		{
			if (val.Value.Name == null || val.Value.Name.Length == 0)
			{
				continue;
			}
			AssetLocation assetloc = val.Value.Code;
			int wildcardStartLen = assetloc.Path.IndexOf('*');
			if (wildcardStartLen == -1)
			{
				continue;
			}
			int wildcardEndLen = assetloc.Path.Length - wildcardStartLen - 1;
			List<string> codes = new List<string>();
			if (val.Value.Type == EnumItemClass.Block)
			{
				foreach (Block block in world.Blocks)
				{
					if (!block.IsMissing && (val.Value.SkipVariants == null || !WildcardUtil.MatchesVariants(assetloc, block.Code, val.Value.SkipVariants)) && WildcardUtil.Match(assetloc, block.Code, val.Value.AllowedVariants))
					{
						string code = block.Code.Path.Substring(wildcardStartLen);
						string codepart = code.Substring(0, code.Length - wildcardEndLen).DeDuplicate();
						codes.Add(codepart);
					}
				}
			}
			else
			{
				foreach (Item item in world.Items)
				{
					if (!(item?.Code == null) && !item.IsMissing && (val.Value.SkipVariants == null || !WildcardUtil.MatchesVariants(val.Value.Code, item.Code, val.Value.SkipVariants)) && WildcardUtil.Match(val.Value.Code, item.Code, val.Value.AllowedVariants))
					{
						string code2 = item.Code.Path.Substring(wildcardStartLen);
						string codepart2 = code2.Substring(0, code2.Length - wildcardEndLen).DeDuplicate();
						codes.Add(codepart2);
					}
				}
			}
			mappings[val.Value.Name] = codes.ToArray();
		}
		return mappings;
	}

	/// <summary>
	/// Puts the crafted itemstack into the output slot and 
	/// consumes the required items from the input slots
	/// </summary>
	/// <param name="inputSlots"></param>
	/// <param name="byPlayer"></param>
	/// <param name="gridWidth"></param>
	public bool ConsumeInput(IPlayer byPlayer, ItemSlot[] inputSlots, int gridWidth)
	{
		if (Shapeless)
		{
			return ConsumeInputShapeLess(byPlayer, inputSlots);
		}
		int gridHeight = inputSlots.Length / gridWidth;
		if (gridWidth < Width || gridHeight < Height)
		{
			return false;
		}
		int i = 0;
		for (int col = 0; col <= gridWidth - Width; col++)
		{
			for (int row = 0; row <= gridHeight - Height; row++)
			{
				if (MatchesAtPosition(col, row, inputSlots, gridWidth))
				{
					return ConsumeInputAt(byPlayer, inputSlots, gridWidth, col, row);
				}
				i++;
			}
		}
		return false;
	}

	private bool ConsumeInputShapeLess(IPlayer byPlayer, ItemSlot[] inputSlots)
	{
		List<CraftingRecipeIngredient> exactMatchIngredients = new List<CraftingRecipeIngredient>();
		List<CraftingRecipeIngredient> wildcardIngredients = new List<CraftingRecipeIngredient>();
		for (int j = 0; j < resolvedIngredients.Length; j++)
		{
			CraftingRecipeIngredient ingredient2 = resolvedIngredients[j];
			if (ingredient2 == null)
			{
				continue;
			}
			if (ingredient2.IsWildCard || ingredient2.IsTool)
			{
				wildcardIngredients.Add(ingredient2.Clone());
				continue;
			}
			ItemStack stack = ingredient2.ResolvedItemstack;
			bool found = false;
			for (int m = 0; m < exactMatchIngredients.Count; m++)
			{
				if (exactMatchIngredients[m].ResolvedItemstack.Satisfies(stack))
				{
					exactMatchIngredients[m].ResolvedItemstack.StackSize += stack.StackSize;
					found = true;
					break;
				}
			}
			if (!found)
			{
				exactMatchIngredients.Add(ingredient2.Clone());
			}
		}
		for (int i = 0; i < inputSlots.Length; i++)
		{
			ItemStack inStack = inputSlots[i].Itemstack;
			if (inStack == null)
			{
				continue;
			}
			for (int l = 0; l < exactMatchIngredients.Count; l++)
			{
				if (exactMatchIngredients[l].ResolvedItemstack.Satisfies(inStack))
				{
					int quantity2 = Math.Min(exactMatchIngredients[l].ResolvedItemstack.StackSize, inStack.StackSize);
					inStack.Collectible.OnConsumedByCrafting(inputSlots, inputSlots[i], this, exactMatchIngredients[l], byPlayer, quantity2);
					exactMatchIngredients[l].ResolvedItemstack.StackSize -= quantity2;
					if (exactMatchIngredients[l].ResolvedItemstack.StackSize <= 0)
					{
						exactMatchIngredients.RemoveAt(l);
					}
					break;
				}
			}
			for (int k = 0; k < wildcardIngredients.Count; k++)
			{
				CraftingRecipeIngredient ingredient = wildcardIngredients[k];
				if (ingredient.Type != inStack.Class || !WildcardUtil.Match(ingredient.Code, inStack.Collectible.Code, ingredient.AllowedVariants))
				{
					continue;
				}
				int quantity = Math.Min(ingredient.Quantity, inStack.StackSize);
				inStack.Collectible.OnConsumedByCrafting(inputSlots, inputSlots[i], this, ingredient, byPlayer, quantity);
				if (ingredient.IsTool)
				{
					wildcardIngredients.RemoveAt(k);
					break;
				}
				ingredient.Quantity -= quantity;
				if (ingredient.Quantity <= 0)
				{
					wildcardIngredients.RemoveAt(k);
				}
				break;
			}
		}
		return exactMatchIngredients.Count == 0;
	}

	private bool ConsumeInputAt(IPlayer byPlayer, ItemSlot[] inputSlots, int gridWidth, int colStart, int rowStart)
	{
		int gridHeight = inputSlots.Length / gridWidth;
		for (int col = 0; col < gridWidth; col++)
		{
			for (int row = 0; row < gridHeight; row++)
			{
				ItemSlot slot = GetElementInGrid(row, col, inputSlots, gridWidth);
				CraftingRecipeIngredient ingredient = GetElementInGrid(row - rowStart, col - colStart, resolvedIngredients, Width);
				if (ingredient != null)
				{
					if (slot.Itemstack == null)
					{
						return false;
					}
					int quantity = (ingredient.IsWildCard ? ingredient.Quantity : ingredient.ResolvedItemstack.StackSize);
					slot.Itemstack.Collectible.OnConsumedByCrafting(inputSlots, slot, this, ingredient, byPlayer, quantity);
				}
			}
		}
		return true;
	}

	/// <summary>
	/// Check if this recipe matches given ingredients
	/// </summary>
	/// <param name="forPlayer">The player for trait testing. Can be null.</param>
	/// <param name="ingredients"></param>
	/// <param name="gridWidth"></param>
	/// <returns></returns>
	public bool Matches(IPlayer forPlayer, ItemSlot[] ingredients, int gridWidth)
	{
		if (!forPlayer.Entity.Api.Event.TriggerMatchesRecipe(forPlayer, this, ingredients, gridWidth))
		{
			return false;
		}
		if (Shapeless)
		{
			return MatchesShapeLess(ingredients, gridWidth);
		}
		int gridHeight = ingredients.Length / gridWidth;
		if (gridWidth < Width || gridHeight < Height)
		{
			return false;
		}
		for (int col = 0; col <= gridWidth - Width; col++)
		{
			for (int row = 0; row <= gridHeight - Height; row++)
			{
				if (MatchesAtPosition(col, row, ingredients, gridWidth))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool MatchesShapeLess(ItemSlot[] suppliedSlots, int gridWidth)
	{
		int gridHeight = suppliedSlots.Length / gridWidth;
		if (gridWidth < Width || gridHeight < Height)
		{
			return false;
		}
		List<KeyValuePair<ItemStack, CraftingRecipeIngredient>> ingredientStacks = new List<KeyValuePair<ItemStack, CraftingRecipeIngredient>>();
		List<ItemStack> suppliedStacks = new List<ItemStack>();
		for (int j = 0; j < suppliedSlots.Length; j++)
		{
			if (suppliedSlots[j].Itemstack == null)
			{
				continue;
			}
			bool found2 = false;
			for (int j2 = 0; j2 < suppliedStacks.Count; j2++)
			{
				if (suppliedStacks[j2].Satisfies(suppliedSlots[j].Itemstack))
				{
					suppliedStacks[j2].StackSize += suppliedSlots[j].Itemstack.StackSize;
					found2 = true;
					break;
				}
			}
			if (!found2)
			{
				suppliedStacks.Add(suppliedSlots[j].Itemstack.Clone());
			}
		}
		for (int k = 0; k < resolvedIngredients.Length; k++)
		{
			CraftingRecipeIngredient ingredient = resolvedIngredients[k];
			if (ingredient == null)
			{
				continue;
			}
			if (ingredient.IsWildCard)
			{
				bool foundw = false;
				int m = 0;
				while (!foundw && m < suppliedStacks.Count)
				{
					ItemStack inputStack = suppliedStacks[m];
					foundw = ingredient.Type == inputStack.Class && WildcardUtil.Match(ingredient.Code, inputStack.Collectible.Code, ingredient.AllowedVariants) && inputStack.StackSize >= ingredient.Quantity;
					foundw &= inputStack.Collectible.MatchesForCrafting(inputStack, this, ingredient);
					m++;
				}
				if (!foundw)
				{
					return false;
				}
				suppliedStacks.RemoveAt(m - 1);
				continue;
			}
			ItemStack stack = ingredient.ResolvedItemstack;
			bool found3 = false;
			for (int n = 0; n < ingredientStacks.Count; n++)
			{
				if (ingredientStacks[n].Key.Equals(world, stack, GlobalConstants.IgnoredStackAttributes) && ingredient.RecipeAttributes == null)
				{
					ingredientStacks[n].Key.StackSize += stack.StackSize;
					found3 = true;
					break;
				}
			}
			if (!found3)
			{
				ingredientStacks.Add(new KeyValuePair<ItemStack, CraftingRecipeIngredient>(stack.Clone(), ingredient));
			}
		}
		if (ingredientStacks.Count != suppliedStacks.Count)
		{
			return false;
		}
		bool equals = true;
		int i = 0;
		while (equals && i < ingredientStacks.Count)
		{
			bool found = false;
			int l = 0;
			while (!found && l < suppliedStacks.Count)
			{
				found = ingredientStacks[i].Key.Satisfies(suppliedStacks[l]) && ingredientStacks[i].Key.StackSize <= suppliedStacks[l].StackSize && suppliedStacks[l].Collectible.MatchesForCrafting(suppliedStacks[l], this, ingredientStacks[i].Value);
				if (found)
				{
					suppliedStacks.RemoveAt(l);
				}
				l++;
			}
			equals = equals && found;
			i++;
		}
		return equals;
	}

	public bool MatchesAtPosition(int colStart, int rowStart, ItemSlot[] inputSlots, int gridWidth)
	{
		int gridHeight = inputSlots.Length / gridWidth;
		for (int col = 0; col < gridWidth; col++)
		{
			for (int row = 0; row < gridHeight; row++)
			{
				ItemStack inputStack = GetElementInGrid(row, col, inputSlots, gridWidth)?.Itemstack;
				CraftingRecipeIngredient ingredient = GetElementInGrid(row - rowStart, col - colStart, resolvedIngredients, Width);
				if ((inputStack == null) ^ (ingredient == null))
				{
					return false;
				}
				if (inputStack != null)
				{
					if (!ingredient.SatisfiesAsIngredient(inputStack))
					{
						return false;
					}
					if (!inputStack.Collectible.MatchesForCrafting(inputStack, this, ingredient))
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	/// <summary>
	/// Returns only the first matching itemstack, there may be multiple
	/// </summary>
	/// <param name="patternCode"></param>
	/// <param name="inputSlots"></param>
	/// <returns></returns>
	public ItemStack GetInputStackForPatternCode(string patternCode, ItemSlot[] inputSlots)
	{
		GridRecipeIngredient ingredient = resolvedIngredients.FirstOrDefault((GridRecipeIngredient ig) => ig?.PatternCode == patternCode);
		if (ingredient == null)
		{
			return null;
		}
		foreach (ItemSlot slot in inputSlots)
		{
			if (!slot.Empty)
			{
				ItemStack inputStack = slot.Itemstack;
				if (inputStack != null && ingredient.SatisfiesAsIngredient(inputStack) && inputStack.Collectible.MatchesForCrafting(inputStack, this, ingredient))
				{
					return inputStack;
				}
			}
		}
		return null;
	}

	public void GenerateOutputStack(ItemSlot[] inputSlots, ItemSlot outputSlot)
	{
		ItemStack itemStack2 = (outputSlot.Itemstack = Output.ResolvedItemstack.Clone());
		ItemStack outstack = itemStack2;
		if (CopyAttributesFrom != null)
		{
			ItemStack instack = GetInputStackForPatternCode(CopyAttributesFrom, inputSlots);
			if (instack != null)
			{
				ITreeAttribute attr = instack.Attributes.Clone();
				attr.MergeTree(outstack.Attributes);
				outstack.Attributes = attr;
			}
		}
		outputSlot.Itemstack.Collectible.OnCreatedByCrafting(inputSlots, outputSlot, this);
	}

	public T GetElementInGrid<T>(int row, int col, T[] stacks, int gridwidth)
	{
		int gridHeight = stacks.Length / gridwidth;
		if (row < 0 || col < 0 || row >= gridHeight || col >= gridwidth)
		{
			return default(T);
		}
		return stacks[row * gridwidth + col];
	}

	public int GetGridIndex<T>(int row, int col, T[] stacks, int gridwidth)
	{
		int gridHeight = stacks.Length / gridwidth;
		if (row < 0 || col < 0 || row >= gridHeight || col >= gridwidth)
		{
			return -1;
		}
		return row * gridwidth + col;
	}

	/// <summary>
	/// Serialized the recipe
	/// </summary>
	/// <param name="writer"></param>
	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Width);
		writer.Write(Height);
		Output.ToBytes(writer);
		writer.Write(Shapeless);
		for (int i = 0; i < resolvedIngredients.Length; i++)
		{
			if (resolvedIngredients[i] == null)
			{
				writer.Write(value: true);
				continue;
			}
			writer.Write(value: false);
			resolvedIngredients[i].ToBytes(writer);
		}
		writer.Write(Name.ToShortString());
		writer.Write(Attributes == null);
		if (Attributes != null)
		{
			writer.Write(Attributes.Token.ToString());
		}
		writer.Write(RequiresTrait != null);
		if (RequiresTrait != null)
		{
			writer.Write(RequiresTrait);
		}
		writer.Write(RecipeGroup);
		writer.Write(AverageDurability);
		writer.Write(CopyAttributesFrom != null);
		if (CopyAttributesFrom != null)
		{
			writer.Write(CopyAttributesFrom);
		}
		writer.Write(ShowInCreatedBy);
		writer.Write(Ingredients.Count);
		foreach (KeyValuePair<string, CraftingRecipeIngredient> val in Ingredients)
		{
			writer.Write(val.Key);
			val.Value.ToBytes(writer);
		}
		writer.Write(IngredientPattern);
	}

	/// <summary>
	/// Deserializes the recipe
	/// </summary>
	/// <param name="reader"></param>
	/// <param name="resolver"></param>
	public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
	{
		Width = reader.ReadInt32();
		Height = reader.ReadInt32();
		Output = new CraftingRecipeIngredient();
		Output.FromBytes(reader, resolver);
		Shapeless = reader.ReadBoolean();
		resolvedIngredients = new GridRecipeIngredient[Width * Height];
		for (int j = 0; j < resolvedIngredients.Length; j++)
		{
			if (!reader.ReadBoolean())
			{
				resolvedIngredients[j] = new GridRecipeIngredient();
				resolvedIngredients[j].FromBytes(reader, resolver);
			}
		}
		Name = new AssetLocation(reader.ReadString());
		if (!reader.ReadBoolean())
		{
			string json = reader.ReadString();
			Attributes = new JsonObject(JToken.Parse(json));
		}
		if (reader.ReadBoolean())
		{
			RequiresTrait = reader.ReadString();
		}
		RecipeGroup = reader.ReadInt32();
		AverageDurability = reader.ReadBoolean();
		if (reader.ReadBoolean())
		{
			CopyAttributesFrom = reader.ReadString();
		}
		ShowInCreatedBy = reader.ReadBoolean();
		int cnt = reader.ReadInt32();
		Ingredients = new Dictionary<string, CraftingRecipeIngredient>();
		for (int i = 0; i < cnt; i++)
		{
			string key = reader.ReadString();
			CraftingRecipeIngredient ing = new CraftingRecipeIngredient();
			ing.FromBytes(reader, resolver);
			Ingredients[key] = ing;
		}
		IngredientPattern = reader.ReadString();
	}

	/// <summary>
	/// Creates a deep copy
	/// </summary>
	/// <returns></returns>
	public GridRecipe Clone()
	{
		GridRecipe recipe = new GridRecipe();
		recipe.RecipeGroup = RecipeGroup;
		recipe.Width = Width;
		recipe.Height = Height;
		recipe.IngredientPattern = IngredientPattern;
		recipe.Ingredients = new Dictionary<string, CraftingRecipeIngredient>();
		if (Ingredients != null)
		{
			foreach (KeyValuePair<string, CraftingRecipeIngredient> val in Ingredients)
			{
				recipe.Ingredients[val.Key] = val.Value.Clone();
			}
		}
		if (resolvedIngredients != null)
		{
			recipe.resolvedIngredients = new GridRecipeIngredient[resolvedIngredients.Length];
			for (int i = 0; i < resolvedIngredients.Length; i++)
			{
				recipe.resolvedIngredients[i] = resolvedIngredients[i]?.CloneTo<GridRecipeIngredient>();
			}
		}
		recipe.Shapeless = Shapeless;
		recipe.Output = Output.Clone();
		recipe.Name = Name;
		recipe.Attributes = Attributes?.Clone();
		recipe.RequiresTrait = RequiresTrait;
		recipe.AverageDurability = AverageDurability;
		recipe.CopyAttributesFrom = CopyAttributesFrom;
		recipe.ShowInCreatedBy = ShowInCreatedBy;
		return recipe;
	}
}
