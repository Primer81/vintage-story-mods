#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

//
// Summary:
//     The default item slot to item stacks
public class ItemSlot
{
    protected ItemStack itemstack;

    protected InventoryBase inventory;

    //
    // Summary:
    //     Icon name to be drawn in the slot background
    public string BackgroundIcon;

    //
    // Summary:
    //     If set will be used as the background color
    public string HexBackgroundColor;

    //
    // Summary:
    //     The upper holding limit of the slot itself. Standard slots are only limited by
    //     the item stacks maxstack size.
    public virtual int MaxSlotStackSize { get; set; } = 999999;


    //
    // Summary:
    //     Gets the inventory attached to this ItemSlot.
    public InventoryBase Inventory => inventory;

    public virtual bool DrawUnavailable { get; set; }

    //
    // Summary:
    //     The ItemStack contained within the slot.
    public ItemStack Itemstack
    {
        get
        {
            return itemstack;
        }
        set
        {
            itemstack = value;
        }
    }

    //
    // Summary:
    //     The number of items in the stack.
    public int StackSize
    {
        get
        {
            if (itemstack != null)
            {
                return itemstack.StackSize;
            }

            return 0;
        }
    }

    //
    // Summary:
    //     Whether or not the stack is empty.
    public virtual bool Empty => itemstack == null;

    //
    // Summary:
    //     The storage type of this slot.
    public virtual EnumItemStorageFlags StorageType { get; set; } = EnumItemStorageFlags.General | EnumItemStorageFlags.Metallurgy | EnumItemStorageFlags.Jewellery | EnumItemStorageFlags.Alchemy | EnumItemStorageFlags.Agriculture | EnumItemStorageFlags.Outfit;


    //
    // Summary:
    //     Can be used to interecept marked dirty calls.
    public event ActionConsumable MarkedDirty;

    //
    // Summary:
    //     Create a new instance of an item slot
    //
    // Parameters:
    //   inventory:
    public ItemSlot(InventoryBase inventory)
    {
        this.inventory = inventory;
    }

    //
    // Summary:
    //     Amount of space left, independent of item MaxStacksize
    public virtual int GetRemainingSlotSpace(ItemStack forItemstack)
    {
        return Math.Max(0, MaxSlotStackSize - StackSize);
    }

    //
    // Summary:
    //     Whether or not this slot can take the item from the source slot.
    //
    // Parameters:
    //   sourceSlot:
    //
    //   priority:
    public virtual bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
    {
        InventoryBase inventoryBase = inventory;
        if (inventoryBase != null && inventoryBase.PutLocked)
        {
            return false;
        }

        ItemStack itemStack = sourceSlot.Itemstack;
        if (itemStack == null)
        {
            return false;
        }

        if ((itemStack.Collectible.GetStorageFlags(itemStack) & StorageType) > (EnumItemStorageFlags)0 && (itemstack == null || itemstack.Collectible.GetMergableQuantity(itemstack, itemStack, priority) > 0))
        {
            return GetRemainingSlotSpace(itemStack) > 0;
        }

        return false;
    }

    //
    // Summary:
    //     Whether or not this slot can hold the item from the source slot.
    //
    // Parameters:
    //   sourceSlot:
    public virtual bool CanHold(ItemSlot sourceSlot)
    {
        InventoryBase inventoryBase = inventory;
        if (inventoryBase != null && inventoryBase.PutLocked)
        {
            return false;
        }

        if (sourceSlot?.Itemstack?.Collectible != null && (sourceSlot.Itemstack.Collectible.GetStorageFlags(sourceSlot.Itemstack) & StorageType) > (EnumItemStorageFlags)0)
        {
            return inventory.CanContain(this, sourceSlot);
        }

        return false;
    }

    //
    // Summary:
    //     Whether or not this slots item can be retrieved.
    public virtual bool CanTake()
    {
        InventoryBase inventoryBase = inventory;
        if (inventoryBase != null && inventoryBase.TakeLocked)
        {
            return false;
        }

        return itemstack != null;
    }

    //
    // Summary:
    //     Gets the entire contents of the stack, setting the base stack to null.
    public virtual ItemStack TakeOutWhole()
    {
        ItemStack itemStack = itemstack.Clone();
        itemstack.StackSize = 0;
        itemstack = null;
        OnItemSlotModified(itemStack);
        return itemStack;
    }

    //
    // Summary:
    //     Gets some of the contents of the stack.
    //
    // Parameters:
    //   quantity:
    //     The amount to get from the stack.
    //
    // Returns:
    //     The stack with the quantity take out (or as much as was available)
    public virtual ItemStack TakeOut(int quantity)
    {
        if (itemstack == null)
        {
            return null;
        }

        if (quantity >= itemstack.StackSize)
        {
            return TakeOutWhole();
        }

        ItemStack emptyClone = itemstack.GetEmptyClone();
        emptyClone.StackSize = quantity;
        itemstack.StackSize -= quantity;
        if (itemstack.StackSize <= 0)
        {
            itemstack = null;
        }

        return emptyClone;
    }

    //
    // Summary:
    //     Attempts to place item in this slot into the target slot.
    //
    // Parameters:
    //   world:
    //
    //   sinkSlot:
    //
    //   quantity:
    //
    // Returns:
    //     Amount of moved items
    public virtual int TryPutInto(IWorldAccessor world, ItemSlot sinkSlot, int quantity = 1)
    {
        ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, quantity);
        return TryPutInto(sinkSlot, ref op);
    }

    //
    // Summary:
    //     Returns the quantity of items that were not merged (left over in the source slot)
    //
    //
    // Parameters:
    //   sinkSlot:
    //
    //   op:
    //
    // Returns:
    //     Amount of moved items
    public virtual int TryPutInto(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
    {
        if (!sinkSlot.CanTakeFrom(this) || !CanTake() || itemstack == null)
        {
            return 0;
        }

        InventoryBase inventoryBase = sinkSlot.inventory;
        if (inventoryBase != null && !inventoryBase.CanContain(sinkSlot, this))
        {
            return 0;
        }

        if (sinkSlot.Itemstack == null)
        {
            int num = Math.Min(sinkSlot.GetRemainingSlotSpace(itemstack), op.RequestedQuantity);
            if (num > 0)
            {
                sinkSlot.Itemstack = TakeOut(num);
                op.MovedQuantity = (op.MovableQuantity = Math.Min(sinkSlot.StackSize, num));
                sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
                OnItemSlotModified(sinkSlot.Itemstack);
            }

            return op.MovedQuantity;
        }

        ItemStackMergeOperation itemStackMergeOperation = (ItemStackMergeOperation)(op = op.ToMergeOperation(sinkSlot, this));
        int requestedQuantity = op.RequestedQuantity;
        op.RequestedQuantity = Math.Min(sinkSlot.GetRemainingSlotSpace(itemstack), op.RequestedQuantity);
        sinkSlot.Itemstack.Collectible.TryMergeStacks(itemStackMergeOperation);
        if (itemStackMergeOperation.MovedQuantity > 0)
        {
            sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
            OnItemSlotModified(sinkSlot.Itemstack);
        }

        op.RequestedQuantity = requestedQuantity;
        return itemStackMergeOperation.MovedQuantity;
    }

    //
    // Summary:
    //     Attempts to flip the ItemSlots.
    //
    // Parameters:
    //   itemSlot:
    //
    // Returns:
    //     Whether or no the flip was successful.
    public virtual bool TryFlipWith(ItemSlot itemSlot)
    {
        if (itemSlot.StackSize > MaxSlotStackSize)
        {
            return false;
        }

        bool num = (itemSlot.Empty || CanHold(itemSlot)) && (Empty || CanTake());
        bool flag = (Empty || itemSlot.CanHold(this)) && (itemSlot.Empty || itemSlot.CanTake());
        if (num && flag)
        {
            itemSlot.FlipWith(this);
            itemSlot.OnItemSlotModified(itemstack);
            OnItemSlotModified(itemSlot.itemstack);
            return true;
        }

        return false;
    }

    //
    // Summary:
    //     Forces a flip with the given ItemSlot
    //
    // Parameters:
    //   withSlot:
    protected virtual void FlipWith(ItemSlot withSlot)
    {
        if (withSlot.StackSize > MaxSlotStackSize)
        {
            if (Empty)
            {
                itemstack = withSlot.TakeOut(MaxSlotStackSize);
            }
        }
        else
        {
            ItemStack itemStack = withSlot.itemstack;
            withSlot.itemstack = itemstack;
            itemstack = itemStack;
        }
    }

    //
    // Summary:
    //     Called when a player has clicked on this slot. The source slot is the mouse cursor
    //     slot. This handles the logic of either taking, putting or exchanging items.
    //
    // Parameters:
    //   sourceSlot:
    //
    //   op:
    public virtual void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
    {
        if (Empty && sourceSlot.Empty)
        {
            return;
        }

        switch (op.MouseButton)
        {
            case EnumMouseButton.Left:
                ActivateSlotLeftClick(sourceSlot, ref op);
                break;
            case EnumMouseButton.Middle:
                ActivateSlotMiddleClick(sourceSlot, ref op);
                break;
            case EnumMouseButton.Right:
                ActivateSlotRightClick(sourceSlot, ref op);
                break;
            case EnumMouseButton.Wheel:
                if (op.WheelDir > 0)
                {
                    sourceSlot.TryPutInto(this, ref op);
                }
                else
                {
                    TryPutInto(sourceSlot, ref op);
                }

                break;
        }
    }

    //
    // Summary:
    //     Activates the left click functions of the given slot.
    //
    // Parameters:
    //   sourceSlot:
    //
    //   op:
    protected virtual void ActivateSlotLeftClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
    {
        if (Empty)
        {
            if (CanHold(sourceSlot))
            {
                int val = Math.Min(sourceSlot.StackSize, MaxSlotStackSize);
                val = Math.Min(val, GetRemainingSlotSpace(sourceSlot.itemstack));
                itemstack = sourceSlot.TakeOut(val);
                op.MovedQuantity = itemstack.StackSize;
                OnItemSlotModified(itemstack);
            }

            return;
        }

        if (sourceSlot.Empty)
        {
            op.RequestedQuantity = StackSize;
            TryPutInto(sourceSlot, ref op);
            return;
        }

        int mergableQuantity = itemstack.Collectible.GetMergableQuantity(itemstack, sourceSlot.itemstack, op.CurrentPriority);
        if (mergableQuantity > 0)
        {
            int requestedQuantity = op.RequestedQuantity;
            op.RequestedQuantity = GameMath.Min(mergableQuantity, sourceSlot.itemstack.StackSize, GetRemainingSlotSpace(sourceSlot.itemstack));
            ItemStackMergeOperation op2 = (ItemStackMergeOperation)(op = op.ToMergeOperation(this, sourceSlot));
            itemstack.Collectible.TryMergeStacks(op2);
            sourceSlot.OnItemSlotModified(itemstack);
            OnItemSlotModified(itemstack);
            op.RequestedQuantity = requestedQuantity;
        }
        else
        {
            TryFlipWith(sourceSlot);
        }
    }

    //
    // Summary:
    //     Activates the middle click functions of the given slot.
    //
    // Parameters:
    //   sinkSlot:
    //
    //   op:
    protected virtual void ActivateSlotMiddleClick(ItemSlot sinkSlot, ref ItemStackMoveOperation op)
    {
        if (!Empty)
        {
            IPlayer actingPlayer = op.ActingPlayer;
            if (actingPlayer != null && (actingPlayer.WorldData?.CurrentGameMode).GetValueOrDefault() == EnumGameMode.Creative)
            {
                sinkSlot.Itemstack = Itemstack.Clone();
                op.MovedQuantity = Itemstack.StackSize;
                sinkSlot.OnItemSlotModified(sinkSlot.Itemstack);
            }
        }
    }

    //
    // Summary:
    //     Activates the right click functions of the given slot.
    //
    // Parameters:
    //   sourceSlot:
    //
    //   op:
    protected virtual void ActivateSlotRightClick(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
    {
        if (Empty)
        {
            if (CanHold(sourceSlot))
            {
                itemstack = sourceSlot.TakeOut(1);
                sourceSlot.OnItemSlotModified(itemstack);
                OnItemSlotModified(itemstack);
            }
        }
        else if (sourceSlot.Empty)
        {
            op.RequestedQuantity = (int)Math.Ceiling((float)itemstack.StackSize / 2f);
            TryPutInto(sourceSlot, ref op);
        }
        else
        {
            op.RequestedQuantity = 1;
            sourceSlot.TryPutInto(this, ref op);
            if (op.MovedQuantity <= 0)
            {
                TryFlipWith(sourceSlot);
            }
        }
    }

    //
    // Summary:
    //     The event fired when the slot is modified.
    //
    // Parameters:
    //   sinkStack:
    public virtual void OnItemSlotModified(ItemStack sinkStack)
    {
        if (inventory != null)
        {
            inventory.DidModifyItemSlot(this, sinkStack);
            if (itemstack?.Collectible != null)
            {
                itemstack.Collectible.UpdateAndGetTransitionStates(inventory.Api.World, this);
            }
        }
    }

    //
    // Summary:
    //     Marks the slot as dirty which queues it up for saving and resends it to the clients.
    //     Does not sync from client to server.
    public virtual void MarkDirty()
    {
        if ((this.MarkedDirty == null || !this.MarkedDirty()) && inventory != null)
        {
            inventory.DidModifyItemSlot(this);
            if (itemstack?.Collectible != null)
            {
                itemstack.Collectible.UpdateAndGetTransitionStates(inventory.Api.World, this);
            }
        }
    }

    //
    // Summary:
    //     Gets the name of the itemstack- if it exists.
    //
    // Returns:
    //     The name of the itemStack or null.
    public virtual string GetStackName()
    {
        return itemstack?.GetName();
    }

    //
    // Summary:
    //     Gets the StackDescription for the item.
    //
    // Parameters:
    //   world:
    //     The world the item resides in.
    //
    //   extendedDebugInfo:
    //     Whether or not we have Extended Debug Info enabled.
    public virtual string GetStackDescription(IClientWorldAccessor world, bool extendedDebugInfo)
    {
        return itemstack?.GetDescription(world, this, extendedDebugInfo);
    }

    public override string ToString()
    {
        if (Empty)
        {
            return base.ToString();
        }

        return base.ToString() + " (" + itemstack.ToString() + ")";
    }

    public virtual WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op = null, List<ItemSlot> skipSlots = null)
    {
        return inventory.GetBestSuitedSlot(sourceSlot, op, skipSlots);
    }
}
#if false // Decompilation log
'180' items in cache
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
Could not find by name: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
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
