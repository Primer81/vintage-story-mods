#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

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

//
// Summary:
//     Represents a crafting recipe to be made on the crafting grid.
[DocumentAsJson]
public class GridRecipe : IByteSerializable
{
    //
    // Summary:
    //     If set to false, the recipe will never be loaded. If loaded, you can use this
    //     field to disable recipes during runtime.
    [DocumentAsJson]
    public bool Enabled = true;

    //
    // Summary:
    //     The pattern of the ingredient. Order for a 3x3 recipe:
    //     1 2 3
    //     4 5 6
    //     7 8 9
    //     Order for a 2x2 recipe:
    //     1 2
    //     3 4
    //     Commas seperate each horizontal row, and an underscore ( _ ) marks a space as
    //     empty.
    //     Note: from game version 1.20.4, this becomes null on server-side after completion
    //     of recipe resolving during server start-up phase
    [DocumentAsJson]
    public string IngredientPattern;

    //
    // Summary:
    //     The recipes ingredients in any order, including the code used in the ingredient
    //     pattern.
    //     Note: from game version 1.20.4, this becomes null on server-side after completion
    //     of recipe resolving during server start-up phase
    [DocumentAsJson]
    public Dictionary<string, CraftingRecipeIngredient> Ingredients;

    //
    // Summary:
    //     Required grid width for crafting this recipe
    [DocumentAsJson]
    public int Width = 3;

    //
    // Summary:
    //     Required grid height for crafting this recipe
    [DocumentAsJson]
    public int Height = 3;

    //
    // Summary:
    //     Info used by the handbook. By default, all recipes for an object will appear
    //     in a single preview. This allows you to split grid recipe previews into multiple.
    [DocumentAsJson]
    public int RecipeGroup;

    //
    // Summary:
    //     Used by the handbook. If false, will not appear in the "Created by" section
    [DocumentAsJson]
    public bool ShowInCreatedBy = true;

    //
    // Summary:
    //     The resulting stack when the recipe is created.
    [DocumentAsJson]
    public CraftingRecipeIngredient Output;

    //
    // Summary:
    //     Whether the order of input items should be respected
    [DocumentAsJson]
    public bool Shapeless;

    //
    // Summary:
    //     Name of the recipe. Used for logging, and some specific uses. Recipes for repairing
    //     objects must contain 'repair' in the name.
    [DocumentAsJson]
    public AssetLocation Name;

    //
    // Summary:
    //     Optional attribute data that you can attach any data to. Useful for code mods,
    //     but also required when using liquid ingredients.
    //     See dough.json grid recipe file for example.
    [JsonConverter(typeof(JsonAttributesConverter))]
    [DocumentAsJson]
    public JsonObject Attributes;

    //
    // Summary:
    //     If set, only players with given trait can use this recipe. See config/traits.json
    //     for a list of traits.
    [DocumentAsJson]
    public string RequiresTrait;

    //
    // Summary:
    //     If true, the output item will have its durability averaged over the input items
    [DocumentAsJson]
    public bool AverageDurability = true;

    //
    // Summary:
    //     If set, it will copy over the itemstack attributes from given ingredient code
    [DocumentAsJson]
    public string CopyAttributesFrom;

    //
    // Summary:
    //     A set of ingredients with their pattern codes resolved into a single object.
    public GridRecipeIngredient[] resolvedIngredients;

    private IWorldAccessor world;

    //
    // Summary:
    //     Turns Ingredients into IItemStacks
    //
    // Parameters:
    //   world:
    //
    // Returns:
    //     True on successful resolve
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
            char c = IngredientPattern[i];
            if (c != ' ' && c != '_')
            {
                string text = c.ToString();
                if (!Ingredients.TryGetValue(text, out var value))
                {
                    world.Logger.Error("Grid Recipe with output {0} contains an ingredient pattern code {1} but supplies no ingredient for it.", Output, text);
                    return false;
                }

                if (!value.Resolve(world, "Grid recipe"))
                {
                    world.Logger.Error("Grid Recipe with output {0} contains an ingredient that cannot be resolved: {1}", Output, value);
                    return false;
                }

                GridRecipeIngredient gridRecipeIngredient = value.CloneTo<GridRecipeIngredient>();
                gridRecipeIngredient.PatternCode = text;
                resolvedIngredients[i] = gridRecipeIngredient;
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

    //
    // Summary:
    //     Resolves Wildcards in the ingredients
    //
    // Parameters:
    //   world:
    public Dictionary<string, string[]> GetNameToCodeMapping(IWorldAccessor world)
    {
        Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();
        foreach (KeyValuePair<string, CraftingRecipeIngredient> ingredient in Ingredients)
        {
            if (ingredient.Value.Name == null || ingredient.Value.Name.Length == 0)
            {
                continue;
            }

            AssetLocation code = ingredient.Value.Code;
            int num = code.Path.IndexOf('*');
            if (num == -1)
            {
                continue;
            }

            int num2 = code.Path.Length - num - 1;
            List<string> list = new List<string>();
            if (ingredient.Value.Type == EnumItemClass.Block)
            {
                foreach (Block block in world.Blocks)
                {
                    if (!block.IsMissing && (ingredient.Value.SkipVariants == null || !WildcardUtil.MatchesVariants(code, block.Code, ingredient.Value.SkipVariants)) && WildcardUtil.Match(code, block.Code, ingredient.Value.AllowedVariants))
                    {
                        string text = block.Code.Path.Substring(num);
                        string item = text.Substring(0, text.Length - num2).DeDuplicate();
                        list.Add(item);
                    }
                }
            }
            else
            {
                foreach (Item item3 in world.Items)
                {
                    if (!(item3?.Code == null) && !item3.IsMissing && (ingredient.Value.SkipVariants == null || !WildcardUtil.MatchesVariants(ingredient.Value.Code, item3.Code, ingredient.Value.SkipVariants)) && WildcardUtil.Match(ingredient.Value.Code, item3.Code, ingredient.Value.AllowedVariants))
                    {
                        string text2 = item3.Code.Path.Substring(num);
                        string item2 = text2.Substring(0, text2.Length - num2).DeDuplicate();
                        list.Add(item2);
                    }
                }
            }

            dictionary[ingredient.Value.Name] = list.ToArray();
        }

        return dictionary;
    }

    //
    // Summary:
    //     Puts the crafted itemstack into the output slot and consumes the required items
    //     from the input slots
    //
    // Parameters:
    //   inputSlots:
    //
    //   byPlayer:
    //
    //   gridWidth:
    public bool ConsumeInput(IPlayer byPlayer, ItemSlot[] inputSlots, int gridWidth)
    {
        if (Shapeless)
        {
            return ConsumeInputShapeLess(byPlayer, inputSlots);
        }

        int num = inputSlots.Length / gridWidth;
        if (gridWidth < Width || num < Height)
        {
            return false;
        }

        int num2 = 0;
        for (int i = 0; i <= gridWidth - Width; i++)
        {
            for (int j = 0; j <= num - Height; j++)
            {
                if (MatchesAtPosition(i, j, inputSlots, gridWidth))
                {
                    return ConsumeInputAt(byPlayer, inputSlots, gridWidth, i, j);
                }

                num2++;
            }
        }

        return false;
    }

    private bool ConsumeInputShapeLess(IPlayer byPlayer, ItemSlot[] inputSlots)
    {
        List<CraftingRecipeIngredient> list = new List<CraftingRecipeIngredient>();
        List<CraftingRecipeIngredient> list2 = new List<CraftingRecipeIngredient>();
        for (int i = 0; i < resolvedIngredients.Length; i++)
        {
            CraftingRecipeIngredient craftingRecipeIngredient = resolvedIngredients[i];
            if (craftingRecipeIngredient == null)
            {
                continue;
            }

            if (craftingRecipeIngredient.IsWildCard || craftingRecipeIngredient.IsTool)
            {
                list2.Add(craftingRecipeIngredient.Clone());
                continue;
            }

            ItemStack resolvedItemstack = craftingRecipeIngredient.ResolvedItemstack;
            bool flag = false;
            for (int j = 0; j < list.Count; j++)
            {
                if (list[j].ResolvedItemstack.Satisfies(resolvedItemstack))
                {
                    list[j].ResolvedItemstack.StackSize += resolvedItemstack.StackSize;
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                list.Add(craftingRecipeIngredient.Clone());
            }
        }

        for (int k = 0; k < inputSlots.Length; k++)
        {
            ItemStack itemstack = inputSlots[k].Itemstack;
            if (itemstack == null)
            {
                continue;
            }

            for (int l = 0; l < list.Count; l++)
            {
                if (list[l].ResolvedItemstack.Satisfies(itemstack))
                {
                    int num = Math.Min(list[l].ResolvedItemstack.StackSize, itemstack.StackSize);
                    itemstack.Collectible.OnConsumedByCrafting(inputSlots, inputSlots[k], this, list[l], byPlayer, num);
                    list[l].ResolvedItemstack.StackSize -= num;
                    if (list[l].ResolvedItemstack.StackSize <= 0)
                    {
                        list.RemoveAt(l);
                    }

                    break;
                }
            }

            for (int m = 0; m < list2.Count; m++)
            {
                CraftingRecipeIngredient craftingRecipeIngredient2 = list2[m];
                if (craftingRecipeIngredient2.Type != itemstack.Class || !WildcardUtil.Match(craftingRecipeIngredient2.Code, itemstack.Collectible.Code, craftingRecipeIngredient2.AllowedVariants))
                {
                    continue;
                }

                int num2 = Math.Min(craftingRecipeIngredient2.Quantity, itemstack.StackSize);
                itemstack.Collectible.OnConsumedByCrafting(inputSlots, inputSlots[k], this, craftingRecipeIngredient2, byPlayer, num2);
                if (craftingRecipeIngredient2.IsTool)
                {
                    list2.RemoveAt(m);
                    break;
                }

                craftingRecipeIngredient2.Quantity -= num2;
                if (craftingRecipeIngredient2.Quantity <= 0)
                {
                    list2.RemoveAt(m);
                }

                break;
            }
        }

        return list.Count == 0;
    }

    private bool ConsumeInputAt(IPlayer byPlayer, ItemSlot[] inputSlots, int gridWidth, int colStart, int rowStart)
    {
        int num = inputSlots.Length / gridWidth;
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < num; j++)
            {
                ItemSlot elementInGrid = GetElementInGrid(j, i, inputSlots, gridWidth);
                CraftingRecipeIngredient elementInGrid2 = GetElementInGrid(j - rowStart, i - colStart, resolvedIngredients, Width);
                if (elementInGrid2 != null)
                {
                    if (elementInGrid.Itemstack == null)
                    {
                        return false;
                    }

                    int quantity = (elementInGrid2.IsWildCard ? elementInGrid2.Quantity : elementInGrid2.ResolvedItemstack.StackSize);
                    elementInGrid.Itemstack.Collectible.OnConsumedByCrafting(inputSlots, elementInGrid, this, elementInGrid2, byPlayer, quantity);
                }
            }
        }

        return true;
    }

    //
    // Summary:
    //     Check if this recipe matches given ingredients
    //
    // Parameters:
    //   forPlayer:
    //     The player for trait testing. Can be null.
    //
    //   ingredients:
    //
    //   gridWidth:
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

        int num = ingredients.Length / gridWidth;
        if (gridWidth < Width || num < Height)
        {
            return false;
        }

        for (int i = 0; i <= gridWidth - Width; i++)
        {
            for (int j = 0; j <= num - Height; j++)
            {
                if (MatchesAtPosition(i, j, ingredients, gridWidth))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool MatchesShapeLess(ItemSlot[] suppliedSlots, int gridWidth)
    {
        int num = suppliedSlots.Length / gridWidth;
        if (gridWidth < Width || num < Height)
        {
            return false;
        }

        List<KeyValuePair<ItemStack, CraftingRecipeIngredient>> list = new List<KeyValuePair<ItemStack, CraftingRecipeIngredient>>();
        List<ItemStack> list2 = new List<ItemStack>();
        for (int i = 0; i < suppliedSlots.Length; i++)
        {
            if (suppliedSlots[i].Itemstack == null)
            {
                continue;
            }

            bool flag = false;
            for (int j = 0; j < list2.Count; j++)
            {
                if (list2[j].Satisfies(suppliedSlots[i].Itemstack))
                {
                    list2[j].StackSize += suppliedSlots[i].Itemstack.StackSize;
                    flag = true;
                    break;
                }
            }

            if (!flag)
            {
                list2.Add(suppliedSlots[i].Itemstack.Clone());
            }
        }

        for (int k = 0; k < resolvedIngredients.Length; k++)
        {
            CraftingRecipeIngredient craftingRecipeIngredient = resolvedIngredients[k];
            if (craftingRecipeIngredient == null)
            {
                continue;
            }

            if (craftingRecipeIngredient.IsWildCard)
            {
                bool flag2 = false;
                int num2 = 0;
                while (!flag2 && num2 < list2.Count)
                {
                    ItemStack itemStack = list2[num2];
                    flag2 = craftingRecipeIngredient.Type == itemStack.Class && WildcardUtil.Match(craftingRecipeIngredient.Code, itemStack.Collectible.Code, craftingRecipeIngredient.AllowedVariants) && itemStack.StackSize >= craftingRecipeIngredient.Quantity;
                    flag2 &= itemStack.Collectible.MatchesForCrafting(itemStack, this, craftingRecipeIngredient);
                    num2++;
                }

                if (!flag2)
                {
                    return false;
                }

                list2.RemoveAt(num2 - 1);
                continue;
            }

            ItemStack resolvedItemstack = craftingRecipeIngredient.ResolvedItemstack;
            bool flag3 = false;
            for (int l = 0; l < list.Count; l++)
            {
                if (list[l].Key.Equals(world, resolvedItemstack, GlobalConstants.IgnoredStackAttributes) && craftingRecipeIngredient.RecipeAttributes == null)
                {
                    list[l].Key.StackSize += resolvedItemstack.StackSize;
                    flag3 = true;
                    break;
                }
            }

            if (!flag3)
            {
                list.Add(new KeyValuePair<ItemStack, CraftingRecipeIngredient>(resolvedItemstack.Clone(), craftingRecipeIngredient));
            }
        }

        if (list.Count != list2.Count)
        {
            return false;
        }

        bool flag4 = true;
        int num3 = 0;
        while (flag4 && num3 < list.Count)
        {
            bool flag5 = false;
            int num4 = 0;
            while (!flag5 && num4 < list2.Count)
            {
                flag5 = list[num3].Key.Satisfies(list2[num4]) && list[num3].Key.StackSize <= list2[num4].StackSize && list2[num4].Collectible.MatchesForCrafting(list2[num4], this, list[num3].Value);
                if (flag5)
                {
                    list2.RemoveAt(num4);
                }

                num4++;
            }

            flag4 = flag4 && flag5;
            num3++;
        }

        return flag4;
    }

    public bool MatchesAtPosition(int colStart, int rowStart, ItemSlot[] inputSlots, int gridWidth)
    {
        int num = inputSlots.Length / gridWidth;
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < num; j++)
            {
                ItemStack itemStack = GetElementInGrid(j, i, inputSlots, gridWidth)?.Itemstack;
                CraftingRecipeIngredient elementInGrid = GetElementInGrid(j - rowStart, i - colStart, resolvedIngredients, Width);
                if ((itemStack == null) ^ (elementInGrid == null))
                {
                    return false;
                }

                if (itemStack != null)
                {
                    if (!elementInGrid.SatisfiesAsIngredient(itemStack))
                    {
                        return false;
                    }

                    if (!itemStack.Collectible.MatchesForCrafting(itemStack, this, elementInGrid))
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    //
    // Summary:
    //     Returns only the first matching itemstack, there may be multiple
    //
    // Parameters:
    //   patternCode:
    //
    //   inputSlots:
    public ItemStack GetInputStackForPatternCode(string patternCode, ItemSlot[] inputSlots)
    {
        GridRecipeIngredient gridRecipeIngredient = resolvedIngredients.FirstOrDefault((GridRecipeIngredient ig) => ig?.PatternCode == patternCode);
        if (gridRecipeIngredient == null)
        {
            return null;
        }

        foreach (ItemSlot itemSlot in inputSlots)
        {
            if (!itemSlot.Empty)
            {
                ItemStack itemstack = itemSlot.Itemstack;
                if (itemstack != null && gridRecipeIngredient.SatisfiesAsIngredient(itemstack) && itemstack.Collectible.MatchesForCrafting(itemstack, this, gridRecipeIngredient))
                {
                    return itemstack;
                }
            }
        }

        return null;
    }

    public void GenerateOutputStack(ItemSlot[] inputSlots, ItemSlot outputSlot)
    {
        ItemStack itemStack2 = (outputSlot.Itemstack = Output.ResolvedItemstack.Clone());
        ItemStack itemStack3 = itemStack2;
        if (CopyAttributesFrom != null)
        {
            ItemStack inputStackForPatternCode = GetInputStackForPatternCode(CopyAttributesFrom, inputSlots);
            if (inputStackForPatternCode != null)
            {
                ITreeAttribute treeAttribute = inputStackForPatternCode.Attributes.Clone();
                treeAttribute.MergeTree(itemStack3.Attributes);
                itemStack3.Attributes = treeAttribute;
            }
        }

        outputSlot.Itemstack.Collectible.OnCreatedByCrafting(inputSlots, outputSlot, this);
    }

    public T GetElementInGrid<T>(int row, int col, T[] stacks, int gridwidth)
    {
        int num = stacks.Length / gridwidth;
        if (row < 0 || col < 0 || row >= num || col >= gridwidth)
        {
            return default(T);
        }

        return stacks[row * gridwidth + col];
    }

    public int GetGridIndex<T>(int row, int col, T[] stacks, int gridwidth)
    {
        int num = stacks.Length / gridwidth;
        if (row < 0 || col < 0 || row >= num || col >= gridwidth)
        {
            return -1;
        }

        return row * gridwidth + col;
    }

    //
    // Summary:
    //     Serialized the recipe
    //
    // Parameters:
    //   writer:
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
        foreach (KeyValuePair<string, CraftingRecipeIngredient> ingredient in Ingredients)
        {
            writer.Write(ingredient.Key);
            ingredient.Value.ToBytes(writer);
        }

        writer.Write(IngredientPattern);
    }

    //
    // Summary:
    //     Deserializes the recipe
    //
    // Parameters:
    //   reader:
    //
    //   resolver:
    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        Width = reader.ReadInt32();
        Height = reader.ReadInt32();
        Output = new CraftingRecipeIngredient();
        Output.FromBytes(reader, resolver);
        Shapeless = reader.ReadBoolean();
        resolvedIngredients = new GridRecipeIngredient[Width * Height];
        for (int i = 0; i < resolvedIngredients.Length; i++)
        {
            if (!reader.ReadBoolean())
            {
                resolvedIngredients[i] = new GridRecipeIngredient();
                resolvedIngredients[i].FromBytes(reader, resolver);
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
        int num = reader.ReadInt32();
        Ingredients = new Dictionary<string, CraftingRecipeIngredient>();
        for (int j = 0; j < num; j++)
        {
            string key = reader.ReadString();
            CraftingRecipeIngredient craftingRecipeIngredient = new CraftingRecipeIngredient();
            craftingRecipeIngredient.FromBytes(reader, resolver);
            Ingredients[key] = craftingRecipeIngredient;
        }

        IngredientPattern = reader.ReadString();
    }

    //
    // Summary:
    //     Creates a deep copy
    public GridRecipe Clone()
    {
        GridRecipe gridRecipe = new GridRecipe();
        gridRecipe.RecipeGroup = RecipeGroup;
        gridRecipe.Width = Width;
        gridRecipe.Height = Height;
        gridRecipe.IngredientPattern = IngredientPattern;
        gridRecipe.Ingredients = new Dictionary<string, CraftingRecipeIngredient>();
        if (Ingredients != null)
        {
            foreach (KeyValuePair<string, CraftingRecipeIngredient> ingredient in Ingredients)
            {
                gridRecipe.Ingredients[ingredient.Key] = ingredient.Value.Clone();
            }
        }

        if (resolvedIngredients != null)
        {
            gridRecipe.resolvedIngredients = new GridRecipeIngredient[resolvedIngredients.Length];
            for (int i = 0; i < resolvedIngredients.Length; i++)
            {
                gridRecipe.resolvedIngredients[i] = resolvedIngredients[i]?.CloneTo<GridRecipeIngredient>();
            }
        }

        gridRecipe.Shapeless = Shapeless;
        gridRecipe.Output = Output.Clone();
        gridRecipe.Name = Name;
        gridRecipe.Attributes = Attributes?.Clone();
        gridRecipe.RequiresTrait = RequiresTrait;
        gridRecipe.AverageDurability = AverageDurability;
        gridRecipe.CopyAttributesFrom = CopyAttributesFrom;
        gridRecipe.ShowInCreatedBy = ShowInCreatedBy;
        return gridRecipe;
    }
}
#if false // Decompilation log
'182' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Newtonsoft.Json.dll'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
