#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

//
// Summary:
//     Basic class representing an item inventory
public abstract class InventoryBase : IInventory, IReadOnlyCollection<ItemSlot>, IEnumerable<ItemSlot>, IEnumerable
{
    [CompilerGenerated]
    private sealed class _003CGetEnumerator_003Ed__104 : IEnumerator<ItemSlot>, IEnumerator, IDisposable
    {
        private int _003C_003E1__state;

        private ItemSlot _003C_003E2__current;

        public InventoryBase _003C_003E4__this;

        private int _003Ci_003E5__2;

        ItemSlot IEnumerator<ItemSlot>.Current
        {
            [DebuggerHidden]
            get
            {
                return _003C_003E2__current;
            }
        }

        object IEnumerator.Current
        {
            [DebuggerHidden]
            get
            {
                return _003C_003E2__current;
            }
        }

        [DebuggerHidden]
        public _003CGetEnumerator_003Ed__104(int _003C_003E1__state)
        {
            this._003C_003E1__state = _003C_003E1__state;
        }

        [DebuggerHidden]
        void IDisposable.Dispose()
        {
            _003C_003E1__state = -2;
        }

        private bool MoveNext()
        {
            int num = _003C_003E1__state;
            InventoryBase inventoryBase = _003C_003E4__this;
            switch (num)
            {
                default:
                    return false;
                case 0:
                    _003C_003E1__state = -1;
                    _003Ci_003E5__2 = 0;
                    break;
                case 1:
                    _003C_003E1__state = -1;
                    _003Ci_003E5__2++;
                    break;
            }

            if (_003Ci_003E5__2 < inventoryBase.Count)
            {
                _003C_003E2__current = inventoryBase[_003Ci_003E5__2];
                _003C_003E1__state = 1;
                return true;
            }

            return false;
        }

        bool IEnumerator.MoveNext()
        {
            //ILSpy generated this explicit interface implementation from .override directive in MoveNext
            return this.MoveNext();
        }

        [DebuggerHidden]
        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }
    }

    //
    // Summary:
    //     The world in which the inventory is operating in. Gives inventories access to
    //     block types, item types and the ability to drop items on the ground.
    public ICoreAPI Api;

    //
    // Summary:
    //     Optional field that can be used to define in-world position of the inventory.
    //     Is set by most container block entities. Might be null!
    public BlockPos Pos;

    //
    // Summary:
    //     Is this inventory generally better suited to hold items? (e.g. set to 3 for armor
    //     in armor inventory, 2 for any item in hotbar inventory, 1 for any item in normal
    //     inventory)
    protected float baseWeight;

    //
    // Summary:
    //     The name of the class for the invnentory.
    protected string className;

    //
    // Summary:
    //     the ID of the instance for the inventory.
    protected string instanceID;

    //
    // Summary:
    //     (Not implemented!) The time it was last changed since the server was started.
    public long lastChangedSinceServerStart;

    //
    // Summary:
    //     The players that had opened the inventory.
    public HashSet<string> openedByPlayerGUIds;

    //
    // Summary:
    //     The network utility for the inventory
    public IInventoryNetworkUtil InvNetworkUtil;

    //
    // Summary:
    //     Slots that have been recently modified. This list is used on the server to update
    //     the clients (then cleared) and on the client to redraw itemstacks in guis (then
    //     cleared)
    public HashSet<int> dirtySlots = new HashSet<int>();

    //
    // Summary:
    //     Optional field, if set, will check against the collectible dimensions and deny
    //     placecment if too large
    public virtual Size3f MaxContentDimensions { get; set; }

    //
    // Summary:
    //     The internal name of the inventory instance.
    public string InventoryID => className + "-" + instanceID;

    //
    // Summary:
    //     The class name of the inventory.
    public string ClassName => className;

    //
    // Summary:
    //     Milliseconds since server startup when the inventory was last changed
    public long LastChanged => lastChangedSinceServerStart;

    //
    // Summary:
    //     Returns the number of slots in this inventory.
    public abstract int Count { get; }

    public virtual int CountForNetworkPacket => Count;

    //
    // Summary:
    //     Gets or sets the slot at the given slot number. Returns null for invalid slot
    //     number (below 0 or above Count-1). The setter allows for replacing slots with
    //     custom ones, though caution is advised.
    public abstract ItemSlot this[int slotId] { get; set; }

    //
    // Summary:
    //     True if this inventory has to be resent to the client or when the client has
    //     to redraw them
    public virtual bool IsDirty => dirtySlots.Count > 0;

    //
    // Summary:
    //     The slots that have been modified server side and need to be resent to the client
    //     or need to be redrawn on the client
    public HashSet<int> DirtySlots => dirtySlots;

    //
    // Summary:
    //     Called by item slot, if true, player cannot take items from this inventory
    public virtual bool TakeLocked { get; set; }

    //
    // Summary:
    //     Called by item slot, if true, player cannot put items into this inventory
    public virtual bool PutLocked { get; set; }

    //
    // Summary:
    //     If true, the inventory will be removed from the list of available inventories
    //     once closed (i.e. is not a personal inventory that the player carries with him)
    public virtual bool RemoveOnClose => true;

    //
    // Summary:
    //     Convenience method to check if this inventory contains anything
    public virtual bool Empty
    {
        get
        {
            using (IEnumerator<ItemSlot> enumerator = GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (!enumerator.Current.Empty)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    //
    // Summary:
    //     Returns the first slot that is not empty or null
    public ItemSlot FirstNonEmptySlot
    {
        get
        {
            using (IEnumerator<ItemSlot> enumerator = GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    ItemSlot current = enumerator.Current;
                    if (!current.Empty)
                    {
                        return current;
                    }
                }
            }

            return null;
        }
    }

    //
    // Summary:
    //     If opening or closing should produce a log line in the audit log. Since when
    //     items are moved the source and destination is logged already
    public virtual bool AuditLogAccess { get; set; }

    //
    // Summary:
    //     Called whenever a slot has been modified
    public event Action<int> SlotModified;

    //
    // Summary:
    //     Called whenever a slot notification event has been fired. Is used by the slot
    //     grid gui element to visually wiggle the slot contents
    public event Action<int> SlotNotified;

    //
    // Summary:
    //     Called whenever this inventory was opened
    public event OnInventoryOpenedDelegate OnInventoryOpened;

    //
    // Summary:
    //     Called whenever this inventory was closed
    public event OnInventoryClosedDelegate OnInventoryClosed;

    //
    // Summary:
    //     If set, the value is returned when GetTransitionSpeedMul() is called instead
    //     of the default value.
    public event CustomGetTransitionSpeedMulDelegate OnAcquireTransitionSpeed;

    //
    // Summary:
    //     Create a new instance of an inventory. You may choose any value for className
    //     and instanceID, but if more than one of these inventories can be opened at the
    //     same time, make sure for both of them to have a different id
    //
    // Parameters:
    //   className:
    //
    //   instanceID:
    //
    //   api:
    public InventoryBase(string className, string instanceID, ICoreAPI api)
    {
        openedByPlayerGUIds = new HashSet<string>();
        this.instanceID = instanceID;
        this.className = className;
        Api = api;
        if (api != null)
        {
            InvNetworkUtil = api.ClassRegistry.CreateInvNetworkUtil(this, api);
        }
    }

    //
    // Summary:
    //     Create a new instance of an inventory. InvetoryID must have the format [className]-[instanceId].
    //     You may choose any value for className and instanceID, but if more than one of
    //     these inventories can be opened at the same time, make sure for both of them
    //     to have a different id
    //
    // Parameters:
    //   inventoryID:
    //
    //   api:
    public InventoryBase(string inventoryID, ICoreAPI api)
    {
        openedByPlayerGUIds = new HashSet<string>();
        if (inventoryID != null)
        {
            string[] array = inventoryID.Split('-', 2);
            className = array[0];
            instanceID = array[1];
        }

        Api = api;
        if (api != null)
        {
            InvNetworkUtil = api.ClassRegistry.CreateInvNetworkUtil(this, api);
        }
    }

    //
    // Summary:
    //     You can initialize an InventoryBase with null as parameters and use LateInitialize
    //     to set these values later. This is sometimes required during chunk loading.
    //
    // Parameters:
    //   inventoryID:
    //
    //   api:
    public virtual void LateInitialize(string inventoryID, ICoreAPI api)
    {
        Api = api;
        string[] array = inventoryID.Split('-', 2);
        className = array[0];
        instanceID = array[1];
        if (InvNetworkUtil == null)
        {
            InvNetworkUtil = api.ClassRegistry.CreateInvNetworkUtil(this, api);
        }
        else
        {
            InvNetworkUtil.Api = api;
        }

        AfterBlocksLoaded(api.World);
    }

    //
    // Summary:
    //     The event fired after all the blocks have loaded.
    //
    // Parameters:
    //   world:
    public virtual void AfterBlocksLoaded(IWorldAccessor world)
    {
        ResolveBlocksOrItems();
    }

    //
    // Summary:
    //     Tells the invnetory to update blocks and items within the invnetory.
    public virtual void ResolveBlocksOrItems()
    {
        using IEnumerator<ItemSlot> enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            ItemSlot current = enumerator.Current;
            if (current.Itemstack != null && !current.Itemstack.ResolveBlockOrItem(Api.World))
            {
                current.Itemstack = null;
            }
        }
    }

    //
    // Summary:
    //     Will return -1 if the slot is not found in this inventory
    //
    // Parameters:
    //   slot:
    public virtual int GetSlotId(ItemSlot slot)
    {
        for (int i = 0; i < Count; i++)
        {
            if (this[i] == slot)
            {
                return i;
            }
        }

        return -1;
    }

    [Obsolete("Use GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null) instead")]
    public WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, List<ItemSlot> skipSlots)
    {
        return GetBestSuitedSlot(sourceSlot, null, skipSlots);
    }

    //
    // Summary:
    //     Gets the best sorted slot for the given item.
    //
    // Parameters:
    //   sourceSlot:
    //     The source item slot.
    //
    //   op:
    //     Can be null. If provided allows the inventory to make a better guess at suitability
    //
    //
    //   skipSlots:
    //     The slots to skip.
    //
    // Returns:
    //     A weighted slot set.
    public virtual WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op = null, List<ItemSlot> skipSlots = null)
    {
        WeightedSlot weightedSlot = new WeightedSlot();
        if (PutLocked || sourceSlot.Inventory == this)
        {
            return weightedSlot;
        }

        using (IEnumerator<ItemSlot> enumerator1 = GetEnumerator())
        {
            while (enumerator1.MoveNext())
            {
                ItemSlot current = enumerator1.Current;
                if ((skipSlots == null || !skipSlots.Contains(current)) && current.Itemstack != null && current.CanTakeFrom(sourceSlot))
                {
                    float suitability = GetSuitability(sourceSlot, current, isMerge: true);
                    if (weightedSlot.slot == null || weightedSlot.weight < suitability)
                    {
                        weightedSlot.slot = current;
                        weightedSlot.weight = suitability;
                    }
                }
            }
        }

        using IEnumerator<ItemSlot> enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            ItemSlot current2 = enumerator.Current;
            if ((skipSlots == null || !skipSlots.Contains(current2)) && current2.Itemstack == null && current2.CanTakeFrom(sourceSlot))
            {
                float suitability2 = GetSuitability(sourceSlot, current2, isMerge: false);
                if (weightedSlot.slot == null || weightedSlot.weight < suitability2)
                {
                    weightedSlot.slot = current2;
                    weightedSlot.weight = suitability2;
                }
            }
        }

        return weightedSlot;
    }

    //
    // Summary:
    //     How well a stack fits into this inventory. By default 1 for new itemstacks and
    //     3 for an itemstack merge. Chests and other stationary container also add a +1
    //     to the suitability if the source slot is from the players inventory.
    //
    // Parameters:
    //   sourceSlot:
    //
    //   targetSlot:
    //
    //   isMerge:
    public virtual float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
    {
        float num = ((targetSlot is ItemSlotBackpack && (sourceSlot.Itemstack.Collectible.GetStorageFlags(sourceSlot.Itemstack) & EnumItemStorageFlags.Backpack) > (EnumItemStorageFlags)0) ? 2 : 0);
        return baseWeight + num + (float)((!isMerge) ? 1 : 3);
    }

    public virtual bool CanContain(ItemSlot sinkSlot, ItemSlot sourceSlot)
    {
        return MaxContentDimensions?.CanContain(sourceSlot.Itemstack.Collectible.Dimensions) ?? true;
    }

    //
    // Summary:
    //     Attempts to flip the contents of both slots
    //
    // Parameters:
    //   targetSlotId:
    //
    //   itemSlot:
    public object TryFlipItems(int targetSlotId, ItemSlot itemSlot)
    {
        ItemSlot itemSlot2 = this[targetSlotId];
        if (itemSlot2 != null && itemSlot2.TryFlipWith(itemSlot))
        {
            return InvNetworkUtil.GetFlipSlotsPacket(itemSlot.Inventory, itemSlot.Inventory.GetSlotId(itemSlot), targetSlotId);
        }

        return null;
    }

    //
    // Summary:
    //     Determines whether or not the player can access the invnetory.
    //
    // Parameters:
    //   player:
    //     The player attempting access.
    //
    //   position:
    //     The postion of the entity.
    public virtual bool CanPlayerAccess(IPlayer player, EntityPos position)
    {
        return true;
    }

    //
    // Summary:
    //     Determines whether or not the player can modify the invnetory.
    //
    // Parameters:
    //   player:
    //     The player attempting access.
    //
    //   position:
    //     The postion of the entity.
    public virtual bool CanPlayerModify(IPlayer player, EntityPos position)
    {
        if (CanPlayerAccess(player, position))
        {
            return HasOpened(player);
        }

        return false;
    }

    //
    // Summary:
    //     The event fired when the search is applied to the item.
    //
    // Parameters:
    //   text:
    public virtual void OnSearchTerm(string text)
    {
    }

    //
    // Summary:
    //     Call when a player has clicked on this slot. The source slot is the mouse cursor
    //     slot. This handles the logic of either taking, putting or exchanging items.
    //
    // Parameters:
    //   slotId:
    //
    //   sourceSlot:
    //
    //   op:
    //
    // Returns:
    //     The appropriate packet needed to reflect the changes on the opposing side
    public virtual object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
    {
        object activateSlotPacket = InvNetworkUtil.GetActivateSlotPacket(slotId, op);
        if (op.ShiftDown)
        {
            sourceSlot = this[slotId];
            string text = sourceSlot.Itemstack?.GetName();
            string text2 = sourceSlot.Inventory?.InventoryID;
            StringBuilder stringBuilder = new StringBuilder();
            op.RequestedQuantity = sourceSlot.StackSize;
            op.ActingPlayer.InventoryManager.TryTransferAway(sourceSlot, ref op, onlyPlayerInventory: false, stringBuilder);
            Api.World.Logger.Audit("{0} shift clicked slot {1} in {2}. Moved {3}x{4} to ({5})", op.ActingPlayer?.PlayerName, slotId, text2, op.MovedQuantity, text, stringBuilder.ToString());
        }
        else
        {
            this[slotId].ActivateSlot(sourceSlot, ref op);
        }

        return activateSlotPacket;
    }

    //
    // Summary:
    //     Called when one of the containing slots has been modified
    //
    // Parameters:
    //   slot:
    public virtual void OnItemSlotModified(ItemSlot slot)
    {
    }

    //
    // Summary:
    //     Called when one of the containing slots has been modified
    //
    // Parameters:
    //   slot:
    //
    //   extractedStack:
    //     If non null the itemstack that was taken out
    public virtual void DidModifyItemSlot(ItemSlot slot, ItemStack extractedStack = null)
    {
        int slotId = GetSlotId(slot);
        if (slotId < 0)
        {
            throw new ArgumentException($"Supplied slot is not part of this inventory ({InventoryID})!");
        }

        MarkSlotDirty(slotId);
        OnItemSlotModified(slot);
        this.SlotModified?.Invoke(slotId);
        slot.Itemstack?.Collectible?.OnModifiedInInventorySlot(Api.World, slot, extractedStack);
    }

    //
    // Summary:
    //     Called when one of the containing slot was notified via NotifySlot
    //
    // Parameters:
    //   slotId:
    public virtual void PerformNotifySlot(int slotId)
    {
        ItemSlot itemSlot = this[slotId];
        if (itemSlot != null && itemSlot.Inventory == this)
        {
            this.SlotNotified?.Invoke(slotId);
        }
    }

    //
    // Summary:
    //     Called when the game is loaded or loaded from server
    //
    // Parameters:
    //   tree:
    public abstract void FromTreeAttributes(ITreeAttribute tree);

    //
    // Summary:
    //     Called when the game is saved or sent to client
    public abstract void ToTreeAttributes(ITreeAttribute tree);

    //
    // Summary:
    //     Attempts to flip the inventory slots.
    //
    // Parameters:
    //   owningPlayer:
    //     The player owner of the invnetory slots.
    //
    //   invIds:
    //     The IDs of the player inventory.
    //
    //   slotIds:
    //     The IDs of the target inventory.
    //
    //   lastChanged:
    //     The times these ids were last changed.
    public virtual bool TryFlipItemStack(IPlayer owningPlayer, string[] invIds, int[] slotIds, long[] lastChanged)
    {
        ItemSlot[] slotsIfExists = GetSlotsIfExists(owningPlayer, invIds, slotIds);
        if (slotsIfExists[0] == null || slotsIfExists[1] == null)
        {
            return false;
        }

        return ((InventoryBase)owningPlayer.InventoryManager.GetInventory(invIds[1])).TryFlipItems(slotIds[1], slotsIfExists[0]) != null;
    }

    //
    // Summary:
    //     Attempts to move the item stack from the inventory to another slot.
    //
    // Parameters:
    //   player:
    //     The player moving the items
    //
    //   invIds:
    //     The player inventory IDs
    //
    //   slotIds:
    //     The target Ids
    //
    //   op:
    //     The operation type.
    public virtual bool TryMoveItemStack(IPlayer player, string[] invIds, int[] slotIds, ref ItemStackMoveOperation op)
    {
        ItemSlot[] slotsIfExists = GetSlotsIfExists(player, invIds, slotIds);
        if (slotsIfExists[0] == null || slotsIfExists[1] == null)
        {
            return false;
        }

        slotsIfExists[0].TryPutInto(slotsIfExists[1], ref op);
        return op.MovedQuantity == op.RequestedQuantity;
    }

    //
    // Summary:
    //     Attempts to get specified slots if the slots exists.
    //
    // Parameters:
    //   player:
    //     The player owning the slots
    //
    //   invIds:
    //     The inventory IDs
    //
    //   slotIds:
    //     The slot ids
    //
    // Returns:
    //     The slots obtained.
    public virtual ItemSlot[] GetSlotsIfExists(IPlayer player, string[] invIds, int[] slotIds)
    {
        ItemSlot[] array = new ItemSlot[2];
        InventoryBase inventoryBase = (InventoryBase)player.InventoryManager.GetInventory(invIds[0]);
        InventoryBase inventoryBase2 = (InventoryBase)player.InventoryManager.GetInventory(invIds[1]);
        if (inventoryBase == null || inventoryBase2 == null)
        {
            return array;
        }

        if (!inventoryBase.CanPlayerModify(player, player.Entity.Pos) || !inventoryBase2.CanPlayerModify(player, player.Entity.Pos))
        {
            return array;
        }

        array[0] = inventoryBase[slotIds[0]];
        array[1] = inventoryBase2[slotIds[1]];
        return array;
    }

    //
    // Summary:
    //     Creates a collection of slots from a tree.
    //
    // Parameters:
    //   tree:
    //     The tree to build slots from
    //
    //   slots:
    //     pre-existing slots. (default: null)
    //
    //   modifiedSlots:
    //     Pre-modified slots. (default: null)
    public virtual ItemSlot[] SlotsFromTreeAttributes(ITreeAttribute tree, ItemSlot[] slots = null, List<ItemSlot> modifiedSlots = null)
    {
        if (tree == null)
        {
            return slots;
        }

        if (slots == null)
        {
            slots = new ItemSlot[tree.GetInt("qslots")];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = NewSlot(i);
            }
        }

        for (int j = 0; j < slots.Length; j++)
        {
            ItemStack itemStack = tree.GetTreeAttribute("slots")?.GetItemstack(j.ToString() ?? "");
            slots[j].Itemstack = itemStack;
            if (Api?.World == null)
            {
                continue;
            }

            itemStack?.ResolveBlockOrItem(Api.World);
            if (modifiedSlots != null)
            {
                ItemStack itemstack = slots[j].Itemstack;
                bool num = itemStack != null && !itemStack.Equals(Api.World, itemstack);
                bool flag = itemstack != null && !itemstack.Equals(Api.World, itemStack);
                if (num || flag)
                {
                    modifiedSlots.Add(slots[j]);
                }
            }
        }

        return slots;
    }

    //
    // Summary:
    //     Sets the tree attribute using the slots.
    //
    // Parameters:
    //   slots:
    //
    //   tree:
    public void SlotsToTreeAttributes(ItemSlot[] slots, ITreeAttribute tree)
    {
        tree.SetInt("qslots", slots.Length);
        TreeAttribute treeAttribute = new TreeAttribute();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].Itemstack != null)
            {
                treeAttribute.SetItemstack(i.ToString() ?? "", slots[i].Itemstack.Clone());
            }
        }

        tree["slots"] = treeAttribute;
    }

    //
    // Summary:
    //     Gets a specified number of empty slots.
    //
    // Parameters:
    //   quantity:
    //     the number of empty slots to get.
    //
    // Returns:
    //     The pre-specified slots.
    public ItemSlot[] GenEmptySlots(int quantity)
    {
        ItemSlot[] array = new ItemSlot[quantity];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = NewSlot(i);
        }

        return array;
    }

    //
    // Summary:
    //     A command to build a new empty slot.
    //
    // Parameters:
    //   i:
    //     the index of the slot.
    //
    // Returns:
    //     An empty slot bound to this inventory.
    protected virtual ItemSlot NewSlot(int i)
    {
        return new ItemSlot(this);
    }

    //
    // Summary:
    //     Server Side: Will resent the slot contents to the client and mark them dirty
    //     there as well Client Side: Will refresh stack size, model and stuff if this stack
    //     is currently being rendered
    //
    // Parameters:
    //   slotId:
    public virtual void MarkSlotDirty(int slotId)
    {
        if (slotId < 0)
        {
            throw new Exception("Negative slotid?!");
        }

        dirtySlots.Add(slotId);
    }

    //
    // Summary:
    //     Discards everything in the item slots.
    public virtual void DiscardAll()
    {
        for (int i = 0; i < Count; i++)
        {
            if (this[i].Itemstack != null)
            {
                dirtySlots.Add(i);
            }

            this[i].Itemstack = null;
        }
    }

    public virtual void DropSlotIfHot(ItemSlot slot, IPlayer player = null)
    {
        if (Api.Side != EnumAppSide.Client && !slot.Empty && (player == null || player.WorldData.CurrentGameMode != EnumGameMode.Creative))
        {
            JsonObject attributes = slot.Itemstack.Collectible.Attributes;
            if ((attributes == null || !attributes.IsTrue("allowHotCrafting")) && slot.Itemstack.Collectible.GetTemperature(Api.World, slot.Itemstack) > 300f && !hasHeatResistantHandGear(player))
            {
                (Api as ICoreServerAPI).SendIngameError(player as IServerPlayer, "requiretongs", Lang.Get("Requires tongs to hold"));
                player.Entity.ReceiveDamage(new DamageSource
                {
                    DamageTier = 0,
                    Source = EnumDamageSource.Player,
                    SourceEntity = player.Entity,
                    Type = EnumDamageType.Fire
                }, 0.25f);
                player.InventoryManager.DropItem(slot, fullStack: true);
            }
        }
    }

    private bool hasHeatResistantHandGear(IPlayer player)
    {
        if (player == null)
        {
            return false;
        }

        ItemSlot leftHandItemSlot = player.Entity.LeftHandItemSlot;
        if (leftHandItemSlot == null)
        {
            return false;
        }

        return (leftHandItemSlot.Itemstack?.Collectible.Attributes?.IsTrue("heatResistant")).GetValueOrDefault();
    }

    //
    // Summary:
    //     Drops the contents of the specified slots in the world.
    //
    // Parameters:
    //   pos:
    //     The position of the inventory attached to the slots.
    //
    //   slotsIds:
    //     The slots to have their inventory drop.
    public virtual void DropSlots(Vec3d pos, params int[] slotsIds)
    {
        foreach (int num in slotsIds)
        {
            if (num < 0)
            {
                throw new Exception("Negative slotid?!");
            }

            ItemSlot itemSlot = this[num];
            if (itemSlot.Itemstack != null)
            {
                Api.World.SpawnItemEntity(itemSlot.Itemstack, pos);
                itemSlot.Itemstack = null;
                itemSlot.MarkDirty();
            }
        }
    }

    //
    // Summary:
    //     Drops the contents of all the slots into the world.
    //
    // Parameters:
    //   pos:
    //     Where to drop all this stuff.
    //
    //   maxStackSize:
    //     If non-zero, will split up the stacks into stacks of give max stack size
    public virtual void DropAll(Vec3d pos, int maxStackSize = 0)
    {
        using IEnumerator<ItemSlot> enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            ItemSlot current = enumerator.Current;
            if (current.Itemstack == null)
            {
                continue;
            }

            if (maxStackSize > 0)
            {
                while (current.StackSize > 0)
                {
                    ItemStack itemstack = current.TakeOut(GameMath.Clamp(current.StackSize, 1, maxStackSize));
                    Api.World.SpawnItemEntity(itemstack, pos);
                }
            }
            else
            {
                Api.World.SpawnItemEntity(current.Itemstack, pos);
            }

            current.Itemstack = null;
            current.MarkDirty();
        }
    }

    //
    // Summary:
    //     Deletes the contents of all the slots
    public void Clear()
    {
        using IEnumerator<ItemSlot> enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            enumerator.Current.Itemstack = null;
        }
    }

    public virtual void OnOwningEntityDeath(Vec3d pos)
    {
        DropAll(pos);
    }

    //
    // Summary:
    //     Does this inventory speed up or slow down a transition for given itemstack? (Default:
    //     1 for perish and 0 otherwise)
    //
    // Parameters:
    //   transType:
    //
    //   stack:
    public virtual float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
    {
        float defaultTransitionSpeedMul = GetDefaultTransitionSpeedMul(transType);
        return InvokeTransitionSpeedDelegates(transType, stack, defaultTransitionSpeedMul);
    }

    public float InvokeTransitionSpeedDelegates(EnumTransitionType transType, ItemStack stack, float mul)
    {
        if (this.OnAcquireTransitionSpeed != null)
        {
            Delegate[] invocationList = this.OnAcquireTransitionSpeed.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++)
            {
                CustomGetTransitionSpeedMulDelegate customGetTransitionSpeedMulDelegate = (CustomGetTransitionSpeedMulDelegate)invocationList[i];
                mul *= customGetTransitionSpeedMulDelegate(transType, stack, mul);
            }
        }

        return mul;
    }

    protected virtual float GetDefaultTransitionSpeedMul(EnumTransitionType transitionType)
    {
        return transitionType switch
        {
            EnumTransitionType.Perish => 1,
            EnumTransitionType.Dry => 1,
            EnumTransitionType.Cure => 1,
            EnumTransitionType.Ripen => 1,
            EnumTransitionType.Melt => 1,
            EnumTransitionType.Harden => 1,
            _ => 0,
        };
    }

    //
    // Summary:
    //     Marks the inventory available for interaction for this player. Returns a open
    //     inventory packet that can be sent to the server for synchronization.
    //
    // Parameters:
    //   player:
    public virtual object Open(IPlayer player)
    {
        object result = InvNetworkUtil.DidOpen(player);
        openedByPlayerGUIds.Add(player.PlayerUID);
        this.OnInventoryOpened?.Invoke(player);
        if (AuditLogAccess)
        {
            Api.World.Logger.Audit("{0} opened inventory {1}", player.PlayerName, InventoryID);
        }

        return result;
    }

    //
    // Summary:
    //     Removes ability to interact with this inventory for this player. Returns a close
    //     inventory packet that can be sent to the server for synchronization.
    //
    // Parameters:
    //   player:
    public virtual object Close(IPlayer player)
    {
        object result = InvNetworkUtil.DidClose(player);
        openedByPlayerGUIds.Remove(player.PlayerUID);
        this.OnInventoryClosed?.Invoke(player);
        if (AuditLogAccess)
        {
            Api.World.Logger.Audit("{0} closed inventory {1}", player.PlayerName, InventoryID);
        }

        return result;
    }

    //
    // Summary:
    //     Checks if given player has this inventory currently opened
    //
    // Parameters:
    //   player:
    public virtual bool HasOpened(IPlayer player)
    {
        return openedByPlayerGUIds.Contains(player.PlayerUID);
    }

    //
    // Summary:
    //     Gets the enumerator for the inventory.
    [IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__104))]
    public IEnumerator<ItemSlot> GetEnumerator()
    {
        //yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
        return new _003CGetEnumerator_003Ed__104(0)
        {
            _003C_003E4__this = this
        };
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    //
    // Summary:
    //     Return the slot where a chute may push items into. Return null if it shouldn't
    //     move items into this inventory.
    //
    // Parameters:
    //   atBlockFace:
    //
    //   fromSlot:
    public virtual ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
    {
        return GetBestSuitedSlot(fromSlot).slot;
    }

    //
    // Summary:
    //     Return the slot where a chute may pull items from. Return null if it is now allowed
    //     to pull any items from this inventory
    //
    // Parameters:
    //   atBlockFace:
    public virtual ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
    {
        return null;
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
