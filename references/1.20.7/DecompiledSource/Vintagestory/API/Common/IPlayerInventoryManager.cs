#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Text;

namespace Vintagestory.API.Common;

//
// Summary:
//     Let's you do various interesting things with the players inventory.
public interface IPlayerInventoryManager
{
    //
    // Summary:
    //     If the player currently holds a tool in his hands, this value will be set
    EnumTool? ActiveTool { get; }

    //
    // Summary:
    //     The players currently active hot bar slot
    int ActiveHotbarSlotNumber { get; set; }

    //
    // Summary:
    //     Returns the currently selected hotbar slot. Might return null if there is no
    //     hotbar!
    ItemSlot ActiveHotbarSlot { get; }

    //
    // Summary:
    //     Dictionary of all inventories currently available to the player (some may however
    //     not be opened)
    //     Note: for iterating through these, Inventories.Values will not be ordered. Instead
    //     use InventoriesOrdered if you want consistent ordering on server and client e.g.
    //     for shift-click operations
    Dictionary<string, IInventory> Inventories { get; }

    //
    // Summary:
    //     An iterable collection of all inventories currently available to the player,
    //     arranged in the same order (by creation order) on both server and client to prevent
    //     syncing / ghosting issues
    IEnumerable<InventoryBase> InventoriesOrdered { get; }

    //
    // Summary:
    //     List of inventories currently opened by the player
    List<IInventory> OpenedInventories { get; }

    //
    // Summary:
    //     Returns the slot that holds the currently dragged itemstack
    ItemSlot MouseItemSlot { get; }

    //
    // Summary:
    //     The slot the player currently hovers over with his mouse cursor
    ItemSlot CurrentHoveredSlot { get; set; }

    //
    // Summary:
    //     Drops the current contents of the mouse slot onto the ground
    //
    // Parameters:
    //   dropAll:
    //     If false, will only drop 1
    bool DropMouseSlotItems(bool dropAll);

    //
    // Summary:
    //     Drops the current contents of given slot onto the ground
    //
    // Parameters:
    //   slot:
    //
    //   fullStack:
    bool DropItem(ItemSlot slot, bool fullStack);

    //
    // Summary:
    //     Produces a visual cue on given slot, if it's currently part of an inventory and
    //     visible to the player If called on server side, the server will send a network
    //     packet to notify the client.
    //
    // Parameters:
    //   player:
    //
    //   slot:
    void NotifySlot(IPlayer player, ItemSlot slot);

    //
    // Summary:
    //     Returns the full inventory id for given inventory class name, e.g. GlobalConstants.creativeInvClassName
    //
    //
    // Parameters:
    //   inventoryClassName:
    string GetInventoryName(string inventoryClassName);

    //
    // Summary:
    //     Same as GetInventory() with playeruid appended to the inventoryClassName. Returns
    //     null if not found. You can use GlobalConstants.*ClassName to get the vanilla
    //     player inventories.
    //
    // Parameters:
    //   inventoryClassName:
    IInventory GetOwnInventory(string inventoryClassName);

    //
    // Summary:
    //     Retrieve a players inventory. Returns null if not found.
    //
    // Parameters:
    //   inventoryId:
    IInventory GetInventory(string inventoryId);

    //
    // Summary:
    //     Gets the itemstack that in the given slot number of the players hotbar
    //
    // Parameters:
    //   slotId:
    ItemStack GetHotbarItemstack(int slotId);

    //
    // Summary:
    //     Returns the hotbar inventory object. Obvious comment is being obvious.
    IInventory GetHotbarInventory();

    //
    // Summary:
    //     Returns true if the invID is found, and the found IInventory value is in invFound;
    //     similar to Dictionary.TryGetValue, invFound is undefined if the result is false
    //
    //
    // Parameters:
    //   invID:
    //
    //   invFound:
    bool GetInventory(string invID, out InventoryBase invFound);

    //
    // Summary:
    //     Returns a slot that would best fit the contents of the source slot. This checks
    //     all inventories currently opened by the player.
    //
    // Parameters:
    //   sourceSlot:
    //
    //   onlyPlayerInventory:
    //
    //   op:
    //
    //   skipSlots:
    ItemSlot GetBestSuitedSlot(ItemSlot sourceSlot, bool onlyPlayerInventory, ItemStackMoveOperation op = null, List<ItemSlot> skipSlots = null);

    [Obsolete("Use GetBestSuitedSlot(ItemSlot sourceSlot, bool onlyPlayerInventory, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null) instead")]
    ItemSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots);

    //
    // Summary:
    //     Tries to move away items from the source slot into any other slot of another
    //     inventory
    //
    // Parameters:
    //   sourceSlot:
    //
    //   op:
    //
    //   onlyPlayerInventory:
    //     Forces to place the item only into the players personal inventory
    //
    //   slotNotifyEffect:
    //
    // Returns:
    //     One or more client packets that may be sent to the server for synchronisation
    object[] TryTransferAway(ItemSlot sourceSlot, ref ItemStackMoveOperation op, bool onlyPlayerInventory, bool slotNotifyEffect = false);

    object[] TryTransferAway(ItemSlot sourceSlot, ref ItemStackMoveOperation op, bool onlyPlayerInventory, StringBuilder shiftClickDebugText, bool slotNotifyEffect = false);

    //
    // Summary:
    //     Tries to move items from source slot to target slot (useful for client side inventory
    //     utilities)
    //
    // Parameters:
    //   sourceSlot:
    //
    //   targetSlot:
    //
    //   op:
    //     If successfull, a client packet that may be sent to the server for synchronisation
    object TryTransferTo(ItemSlot sourceSlot, ItemSlot targetSlot, ref ItemStackMoveOperation op);

    //
    // Summary:
    //     Tries to add given itemstack to the players inventory
    //
    // Parameters:
    //   itemstack:
    //
    //   slotNotifyEffect:
    bool TryGiveItemstack(ItemStack itemstack, bool slotNotifyEffect = false);

    //
    // Summary:
    //     Notifies the inventory manager that the player has opened an inventory. Should
    //     always be called on both sides (client and server). Only then interaction with
    //     other inventories becomes possible
    //
    // Parameters:
    //   inventory:
    object OpenInventory(IInventory inventory);

    //
    // Summary:
    //     Notifies the inventory manager that the player has closed an inventory. Should
    //     always be called on both sides (client and server). After closing interaction
    //     with given inventory becomes no longer possible until reopened
    //
    // Parameters:
    //   inventory:
    object CloseInventory(IInventory inventory);

    //
    // Summary:
    //     Iterates over all inventory slots, returns true if your matcher returns true
    //
    //
    // Parameters:
    //   matcher:
    bool Find(System.Func<ItemSlot, bool> matcher);

    //
    // Summary:
    //     Shorthand for Inventories.ContainsValue(inventory)
    //
    // Parameters:
    //   inventory:
    bool HasInventory(IInventory inventory);

    //
    // Summary:
    //     Will discard all of the players inventory contents
    void DiscardAll();

    //
    // Summary:
    //     Will drop all of the players inventory contents
    void OnDeath();

    //
    // Summary:
    //     Drops the contents of given inventory
    //
    // Parameters:
    //   inv:
    void DropAllInventoryItems(IInventory inv);

    //
    // Summary:
    //     Server Side: Resends the hotbar slot contents to all other clients to make sure
    //     they render the correct held item Client side: No effect
    void BroadcastHotbarSlot();
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
