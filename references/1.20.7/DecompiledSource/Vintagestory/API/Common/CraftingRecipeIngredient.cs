#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

//
// Summary:
//     A crafting recipe ingredient
[DocumentAsJson]
public class CraftingRecipeIngredient : IRecipeIngredient
{
    //
    // Summary:
    //     Is the itemstack an item or a block?
    [DocumentAsJson]
    public EnumItemClass Type;

    //
    // Summary:
    //     The quantity of the itemstack required for the recipe.
    [DocumentAsJson]
    public int Quantity = 1;

    //
    // Summary:
    //     What attributes this itemstack must have to be a valid ingredient
    [JsonProperty]
    [JsonConverter(typeof(JsonAttributesConverter))]
    public JsonObject Attributes;

    //
    // Summary:
    //     Optional attribute data that you can attach any data to. Used for some specific
    //     instances in code mods.
    [JsonProperty]
    [JsonConverter(typeof(JsonAttributesConverter))]
    public JsonObject RecipeAttributes;

    //
    // Summary:
    //     Whether this crafting recipe ingredient should be regarded as a tool required
    //     to build this item. If true, the recipe will not consume the item but reduce
    //     its durability.
    [DocumentAsJson]
    public bool IsTool;

    //
    // Summary:
    //     If Vintagestory.API.Common.CraftingRecipeIngredient.IsTool is set, this is the
    //     durability cost when the recipe is created.
    [DocumentAsJson]
    public int ToolDurabilityCost = 1;

    //
    // Summary:
    //     When using a wildcard in the item/block code, setting this field will limit the
    //     allowed variants
    [DocumentAsJson]
    public string[] AllowedVariants;

    //
    // Summary:
    //     When using a wildcard in the item/block code, setting this field will skip these
    //     variants
    [DocumentAsJson]
    public string[] SkipVariants;

    //
    // Summary:
    //     If set, the crafting recipe will give back the consumed stack to be player upon
    //     crafting. Can also be used to produce multiple outputs for a recipe.
    [DocumentAsJson]
    public JsonItemStack ReturnedStack;

    //
    // Summary:
    //     The itemstack made from Code, Quantity and Attributes, populated by the engine
    public ItemStack ResolvedItemstack;

    //
    // Summary:
    //     Whether this recipe contains a wildcard, populated by the engine
    public bool IsWildCard;

    //
    // Summary:
    //     The code of the item or block
    [DocumentAsJson]
    public AssetLocation Code { get; set; }

    //
    // Summary:
    //     Attaches a name to a wildcard in an ingredient. This is used to substitute the
    //     value into the output. Only required if using a wildcard.
    [DocumentAsJson]
    public string Name { get; set; }

    //
    // Summary:
    //     Turns Type, Code and Attributes into an IItemStack
    //
    // Parameters:
    //   resolver:
    //
    //   sourceForErrorLogging:
    public bool Resolve(IWorldAccessor resolver, string sourceForErrorLogging)
    {
        if (ReturnedStack != null)
        {
            ReturnedStack.Resolve(resolver, sourceForErrorLogging + " recipe with output ", Code);
        }

        if (Code.Path.Contains('*'))
        {
            IsWildCard = true;
            return true;
        }

        if (Type == EnumItemClass.Block)
        {
            Block block = resolver.GetBlock(Code);
            if (block == null || block.IsMissing)
            {
                resolver.Logger.Warning("Failed resolving crafting recipe ingredient with code {0} in {1}", Code, sourceForErrorLogging);
                return false;
            }

            ResolvedItemstack = new ItemStack(block, Quantity);
        }
        else
        {
            Item item = resolver.GetItem(Code);
            if (item == null || item.IsMissing)
            {
                resolver.Logger.Warning("Failed resolving crafting recipe ingredient with code {0} in {1}", Code, sourceForErrorLogging);
                return false;
            }

            ResolvedItemstack = new ItemStack(item, Quantity);
        }

        if (Attributes != null)
        {
            IAttribute attribute = Attributes.ToAttribute();
            if (attribute is ITreeAttribute)
            {
                ResolvedItemstack.Attributes = (ITreeAttribute)attribute;
            }
        }

        return true;
    }

    //
    // Summary:
    //     Checks whether or not the input satisfies as an ingredient for the recipe.
    //
    // Parameters:
    //   inputStack:
    //
    //   checkStacksize:
    public bool SatisfiesAsIngredient(ItemStack inputStack, bool checkStacksize = true)
    {
        if (inputStack == null)
        {
            return false;
        }

        if (IsWildCard)
        {
            if (Type != inputStack.Class)
            {
                return false;
            }

            if (!WildcardUtil.Match(Code, inputStack.Collectible.Code, AllowedVariants))
            {
                return false;
            }

            if (SkipVariants != null && WildcardUtil.Match(Code, inputStack.Collectible.Code, SkipVariants))
            {
                return false;
            }

            if (checkStacksize && inputStack.StackSize < Quantity)
            {
                return false;
            }
        }
        else
        {
            if (!ResolvedItemstack.Satisfies(inputStack))
            {
                return false;
            }

            if (checkStacksize && inputStack.StackSize < ResolvedItemstack.StackSize)
            {
                return false;
            }
        }

        return true;
    }

    public CraftingRecipeIngredient Clone()
    {
        return CloneTo<CraftingRecipeIngredient>();
    }

    public T CloneTo<T>() where T : CraftingRecipeIngredient, new()
    {
        T val = new T
        {
            Code = Code.Clone(),
            Type = Type,
            Name = Name,
            Quantity = Quantity,
            IsWildCard = IsWildCard,
            IsTool = IsTool,
            ToolDurabilityCost = ToolDurabilityCost,
            AllowedVariants = ((AllowedVariants == null) ? null : ((string[])AllowedVariants.Clone())),
            SkipVariants = ((SkipVariants == null) ? null : ((string[])SkipVariants.Clone())),
            ResolvedItemstack = ResolvedItemstack?.Clone(),
            ReturnedStack = ReturnedStack?.Clone(),
            RecipeAttributes = RecipeAttributes?.Clone()
        };
        if (Attributes != null)
        {
            val.Attributes = Attributes.Clone();
        }

        return val;
    }

    public override string ToString()
    {
        return Type.ToString() + " code " + Code;
    }

    //
    // Summary:
    //     Fills in the placeholder ingredients for the crafting recipe.
    //
    // Parameters:
    //   key:
    //
    //   value:
    public void FillPlaceHolder(string key, string value)
    {
        Code = Code.CopyWithPath(Code.Path.Replace("{" + key + "}", value));
        Attributes?.FillPlaceHolder(key, value);
        RecipeAttributes?.FillPlaceHolder(key, value);
    }

    public virtual void ToBytes(BinaryWriter writer)
    {
        writer.Write(IsWildCard);
        writer.Write((int)Type);
        writer.Write(Code.ToShortString());
        writer.Write(Quantity);
        if (!IsWildCard)
        {
            writer.Write(ResolvedItemstack != null);
            ResolvedItemstack?.ToBytes(writer);
        }

        writer.Write(IsTool);
        writer.Write(ToolDurabilityCost);
        writer.Write(AllowedVariants != null);
        if (AllowedVariants != null)
        {
            writer.Write(AllowedVariants.Length);
            for (int i = 0; i < AllowedVariants.Length; i++)
            {
                writer.Write(AllowedVariants[i]);
            }
        }

        writer.Write(SkipVariants != null);
        if (SkipVariants != null)
        {
            writer.Write(SkipVariants.Length);
            for (int j = 0; j < SkipVariants.Length; j++)
            {
                writer.Write(SkipVariants[j]);
            }
        }

        writer.Write(ReturnedStack?.ResolvedItemstack != null);
        if (ReturnedStack?.ResolvedItemstack != null)
        {
            ReturnedStack.ToBytes(writer);
        }

        if (RecipeAttributes != null)
        {
            writer.Write(value: true);
            writer.Write(RecipeAttributes.ToString());
        }
        else
        {
            writer.Write(value: false);
        }
    }

    public virtual void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        IsWildCard = reader.ReadBoolean();
        Type = (EnumItemClass)reader.ReadInt32();
        Code = new AssetLocation(reader.ReadString());
        Quantity = reader.ReadInt32();
        if (!IsWildCard && reader.ReadBoolean())
        {
            ResolvedItemstack = new ItemStack(reader, resolver);
        }

        IsTool = reader.ReadBoolean();
        ToolDurabilityCost = reader.ReadInt32();
        if (reader.ReadBoolean())
        {
            AllowedVariants = new string[reader.ReadInt32()];
            for (int i = 0; i < AllowedVariants.Length; i++)
            {
                AllowedVariants[i] = reader.ReadString();
            }
        }

        if (reader.ReadBoolean())
        {
            SkipVariants = new string[reader.ReadInt32()];
            for (int j = 0; j < SkipVariants.Length; j++)
            {
                SkipVariants[j] = reader.ReadString();
            }
        }

        if (reader.ReadBoolean())
        {
            ReturnedStack = new JsonItemStack();
            ReturnedStack.FromBytes(reader, resolver.ClassRegistry);
            ReturnedStack.ResolvedItemstack.ResolveBlockOrItem(resolver);
        }

        if (reader.ReadBoolean())
        {
            RecipeAttributes = new JsonObject(JToken.Parse(reader.ReadString()));
        }
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
