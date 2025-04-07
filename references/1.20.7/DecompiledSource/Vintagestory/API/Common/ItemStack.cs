#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class ItemStack : IItemStack
{
    //
    // Summary:
    //     Wether its a block Block or Item
    public EnumItemClass Class;

    //
    // Summary:
    //     The id of the block or item
    public int Id;

    protected int stacksize;

    private TreeAttribute stackAttributes = new TreeAttribute();

    private TreeAttribute tempAttributes = new TreeAttribute();

    protected Item item;

    protected Block block;

    //
    // Summary:
    //     The item/block base class this stack is holding
    public CollectibleObject Collectible
    {
        get
        {
            if (Class == EnumItemClass.Block)
            {
                return block;
            }

            return item;
        }
    }

    //
    // Summary:
    //     If this is a stack of items, this is the type of items it's holding, otherwise
    //     null
    public Item Item => item;

    //
    // Summary:
    //     If this is a stack of blocks, this is the type of block it's holding, otherwise
    //     null
    public Block Block => block;

    //
    // Summary:
    //     The amount of items/blocks in this stack
    public int StackSize
    {
        get
        {
            return stacksize;
        }
        set
        {
            stacksize = value;
        }
    }

    //
    // Summary:
    //     The id of the block or item
    int IItemStack.Id => Id;

    //
    // Summary:
    //     Attributes assigned to this particular itemstack which are saved and synchronized.
    public ITreeAttribute Attributes
    {
        get
        {
            return stackAttributes;
        }
        set
        {
            stackAttributes = (TreeAttribute)value;
        }
    }

    //
    // Summary:
    //     Temporary Attributes assigned to this particular itemstack, not synchronized,
    //     not saved! Modifiable.
    public ITreeAttribute TempAttributes
    {
        get
        {
            return tempAttributes;
        }
        set
        {
            tempAttributes = (TreeAttribute)value;
        }
    }

    //
    // Summary:
    //     The Attributes assigned to the underlying block/item. Should not be modified,
    //     as it applies to globally.
    public JsonObject ItemAttributes => Collectible.Attributes;

    //
    // Summary:
    //     Is it a Block or an Item?
    EnumItemClass IItemStack.Class => Class;

    //
    // Summary:
    //     Create a new empty itemstack
    public ItemStack()
    {
    }

    //
    // Summary:
    //     Create a new itemstack with given collectible id, itemclass, stacksize, attributes
    //     and a resolver to turn the collectibe + itemclass into an Item/Block
    //
    // Parameters:
    //   id:
    //
    //   itemClass:
    //
    //   stacksize:
    //
    //   stackAttributes:
    //
    //   resolver:
    public ItemStack(int id, EnumItemClass itemClass, int stacksize, TreeAttribute stackAttributes, IWorldAccessor resolver)
    {
        Id = id;
        Class = itemClass;
        this.stacksize = stacksize;
        this.stackAttributes = stackAttributes;
        if (Class == EnumItemClass.Block)
        {
            block = resolver.GetBlock(Id);
        }
        else
        {
            item = resolver.GetItem(Id);
        }
    }

    //
    // Summary:
    //     Create a new itemstack from a byte serialized stream (without resolving the block/item)
    //
    //
    // Parameters:
    //   reader:
    public ItemStack(BinaryReader reader)
    {
        FromBytes(reader);
    }

    //
    // Summary:
    //     Create a new itemstack from a byte serialized array(without resolving the block/item)
    //
    //
    // Parameters:
    //   data:
    public ItemStack(byte[] data)
    {
        using MemoryStream input = new MemoryStream(data);
        using BinaryReader stream = new BinaryReader(input);
        FromBytes(stream);
    }

    //
    // Summary:
    //     Create a new itemstack from a byte serialized stream (with resolving the block/item)
    //
    //
    // Parameters:
    //   reader:
    //
    //   resolver:
    public ItemStack(BinaryReader reader, IWorldAccessor resolver)
    {
        FromBytes(reader);
        if (Class == EnumItemClass.Block)
        {
            block = resolver.GetBlock(Id);
        }
        else
        {
            item = resolver.GetItem(Id);
        }
    }

    //
    // Summary:
    //     Create a new itemstack from given block/item and given stack size
    //
    // Parameters:
    //   collectible:
    //
    //   stacksize:
    public ItemStack(CollectibleObject collectible, int stacksize = 1)
    {
        if (collectible == null)
        {
            throw new Exception("Can't create itemstack without collectible!");
        }

        if (collectible is Block)
        {
            Class = EnumItemClass.Block;
            Id = collectible.Id;
            block = collectible as Block;
            this.stacksize = stacksize;
        }
        else
        {
            Class = EnumItemClass.Item;
            Id = collectible.Id;
            item = collectible as Item;
            this.stacksize = stacksize;
        }
    }

    //
    // Summary:
    //     Create a new itemstack from given item and given stack size
    //
    // Parameters:
    //   item:
    //
    //   stacksize:
    public ItemStack(Item item, int stacksize = 1)
    {
        if (item == null)
        {
            throw new Exception("Can't create itemstack without item!");
        }

        Class = EnumItemClass.Item;
        Id = item.ItemId;
        this.item = item;
        this.stacksize = stacksize;
    }

    //
    // Summary:
    //     Create a new itemstack from given block and given stack size
    //
    // Parameters:
    //   block:
    //
    //   stacksize:
    public ItemStack(Block block, int stacksize = 1)
    {
        if (block == null)
        {
            throw new Exception("Can't create itemstack without block!");
        }

        Class = EnumItemClass.Block;
        Id = block.BlockId;
        this.block = block;
        this.stacksize = stacksize;
    }

    //
    // Summary:
    //     Returns true if both stacks exactly match
    //
    // Parameters:
    //   worldForResolve:
    //
    //   sourceStack:
    //
    //   ignoreAttributeSubTrees:
    public bool Equals(IWorldAccessor worldForResolve, ItemStack sourceStack, params string[] ignoreAttributeSubTrees)
    {
        if (Collectible == null)
        {
            ResolveBlockOrItem(worldForResolve);
        }

        if (sourceStack != null && Collectible != null)
        {
            return Collectible.Equals(this, sourceStack, ignoreAttributeSubTrees);
        }

        return false;
    }

    //
    // Summary:
    //     Returns true if this item stack is a satisfactory replacement for given itemstack.
    //     It's basically an Equals() test, but ignores additional attributes of the sourceStack
    //
    //
    // Parameters:
    //   sourceStack:
    public bool Satisfies(ItemStack sourceStack)
    {
        if (sourceStack != null)
        {
            return Collectible.Satisfies(this, sourceStack);
        }

        return false;
    }

    //
    // Summary:
    //     Replace all the properties (id, class, attributes, stacksize, etc...) from this
    //     item stack by given stack
    //
    // Parameters:
    //   stack:
    public void SetFrom(ItemStack stack)
    {
        Id = stack.Collectible.Id;
        Class = stack.Class;
        item = stack.item;
        block = stack.block;
        stacksize = stack.stacksize;
        stackAttributes = stack.stackAttributes.Clone() as TreeAttribute;
        tempAttributes = stack.tempAttributes.Clone() as TreeAttribute;
    }

    //
    // Summary:
    //     Turn the itemstack into a simple string representation
    public override string ToString()
    {
        return stacksize + "x " + ((Class == EnumItemClass.Block) ? "Block" : "Item") + " Id " + Id + ", Code " + Collectible?.Code;
    }

    //
    // Summary:
    //     Serializes the itemstack into a series of bytes, including its stack attributes
    public byte[] ToBytes()
    {
        using MemoryStream memoryStream = new MemoryStream();
        using (BinaryWriter stream = new BinaryWriter(memoryStream))
        {
            ToBytes(stream);
        }

        return memoryStream.ToArray();
    }

    //
    // Summary:
    //     Serializes the itemstack into a series of bytes, including its stack attributes
    //
    //
    // Parameters:
    //   stream:
    public void ToBytes(BinaryWriter stream)
    {
        stream.Write((int)Class);
        stream.Write(Id);
        stream.Write(stacksize);
        stackAttributes.ToBytes(stream);
    }

    //
    // Summary:
    //     Reads all the itemstacks properties from a series of bytes, including its stack
    //     attributes
    //
    // Parameters:
    //   stream:
    public void FromBytes(BinaryReader stream)
    {
        Class = (EnumItemClass)stream.ReadInt32();
        Id = stream.ReadInt32();
        stacksize = stream.ReadInt32();
        stackAttributes.FromBytes(stream);
    }

    //
    // Summary:
    //     Sets the item/block based on the currently set itemclass + id
    //
    // Parameters:
    //   resolver:
    public bool ResolveBlockOrItem(IWorldAccessor resolver)
    {
        if (Class == EnumItemClass.Block)
        {
            block = resolver.GetBlock(Id);
            if (block == null)
            {
                return false;
            }
        }
        else
        {
            item = resolver.GetItem(Id);
            if (item == null)
            {
                return false;
            }
        }

        return true;
    }

    //
    // Summary:
    //     Returns true if searchText is found in the item/block name as supplied from GetName()
    //
    //
    // Parameters:
    //   world:
    //
    //   searchText:
    public bool MatchesSearchText(IWorldAccessor world, string searchText)
    {
        if (!GetName().CaseInsensitiveContains(searchText))
        {
            return GetDescription(world, new DummySlot(this)).CaseInsensitiveContains(searchText);
        }

        return true;
    }

    //
    // Summary:
    //     Returns a human readable name of the item/block
    public string GetName()
    {
        return Collectible.GetHeldItemName(this);
    }

    //
    // Summary:
    //     Returns a human readable description of the item/block
    //
    // Parameters:
    //   world:
    //
    //   inSlot:
    //
    //   debug:
    public string GetDescription(IWorldAccessor world, ItemSlot inSlot, bool debug = false)
    {
        StringBuilder stringBuilder = new StringBuilder();
        Collectible.GetHeldItemInfo(inSlot, stringBuilder, world, debug);
        return stringBuilder.ToString();
    }

    //
    // Summary:
    //     Creates a full copy of the item stack
    public ItemStack Clone()
    {
        ItemStack emptyClone = GetEmptyClone();
        emptyClone.stacksize = stacksize;
        return emptyClone;
    }

    //
    // Summary:
    //     Creates a full copy of the item stack, except for its stack size.
    public ItemStack GetEmptyClone()
    {
        ItemStack itemStack = new ItemStack
        {
            item = item,
            block = block,
            Id = Id,
            Class = Class
        };
        if (stackAttributes != null)
        {
            itemStack.Attributes = Attributes.Clone();
        }

        return itemStack;
    }

    //
    // Summary:
    //     This method should always be called when an itemstack got loaded from the savegame
    //     or when it got imported. When this method return false, you should discard the
    //     itemstack because it could not get resolved and a warning will be logged.
    //
    // Parameters:
    //   oldBlockMapping:
    //
    //   oldItemMapping:
    //
    //   worldForNewMapping:
    public bool FixMapping(Dictionary<int, AssetLocation> oldBlockMapping, Dictionary<int, AssetLocation> oldItemMapping, IWorldAccessor worldForNewMapping)
    {
        AssetLocation value;
        if (Class == EnumItemClass.Item)
        {
            if (oldItemMapping.TryGetValue(Id, out value) && value != null)
            {
                item = worldForNewMapping.GetItem(value);
                if (item == null)
                {
                    worldForNewMapping.Logger.Warning("Cannot fix itemstack mapping, item code {0} not found item registry. Will delete stack.", value);
                    return false;
                }

                Id = item.Id;
                return true;
            }
        }
        else if (oldBlockMapping.TryGetValue(Id, out value) && value != null)
        {
            block = worldForNewMapping.GetBlock(value);
            if (block == null)
            {
                worldForNewMapping.Logger.Warning("Cannot fix itemstack mapping, block code {0} not found block registry. Will delete stack.", value);
                return false;
            }

            Id = block.Id;
            return true;
        }

        worldForNewMapping.Logger.Warning("Cannot fix itemstack mapping, item/block id {0} not found in old mapping list. Will delete stack. ({1})", Id, Collectible);
        return false;
    }

    public override int GetHashCode()
    {
        return GetHashCode(null);
    }

    public int GetHashCode(string[] ignoredAttributes)
    {
        if (Class == EnumItemClass.Item)
        {
            return 0 ^ Id ^ Attributes.GetHashCode(ignoredAttributes);
        }

        return 0x20000 ^ Id ^ Attributes.GetHashCode(ignoredAttributes);
    }
}
#if false // Decompilation log
'181' items in cache
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
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
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
