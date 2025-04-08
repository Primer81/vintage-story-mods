using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Vintagestory.API.Client;

namespace Vintagestory.API.Common;

/// <summary>
/// The contents of one or more bags
/// </summary>
public class BagInventory : IReadOnlyCollection<ItemSlot>, IEnumerable<ItemSlot>, IEnumerable
{
	[CompilerGenerated]
	private sealed class _003CGetEnumerator_003Ed__15 : IEnumerator<ItemSlot>, IEnumerator, IDisposable
	{
		private int _003C_003E1__state;

		private ItemSlot _003C_003E2__current;

		public BagInventory _003C_003E4__this;

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
		public _003CGetEnumerator_003Ed__15(int _003C_003E1__state)
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
			BagInventory bagInventory = _003C_003E4__this;
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
			if (_003Ci_003E5__2 < bagInventory.Count)
			{
				_003C_003E2__current = bagInventory[_003Ci_003E5__2];
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

	protected ICoreAPI Api;

	protected List<ItemSlot> bagContents = new List<ItemSlot>();

	private ItemSlot[] bagSlots;

	public int Count => bagContents.Count;

	public ItemSlot[] BagSlots
	{
		get
		{
			return bagSlots;
		}
		set
		{
			bagSlots = value;
		}
	}

	public ItemSlot this[int slotId]
	{
		get
		{
			return bagContents[slotId];
		}
		set
		{
			bagContents[slotId] = value;
		}
	}

	public BagInventory(ICoreAPI api, ItemSlot[] bagSlots)
	{
		BagSlots = bagSlots;
		Api = api;
	}

	public void SaveSlotIntoBag(ItemSlotBagContent slot)
	{
		ItemStack backPackStack = BagSlots[slot.BagIndex].Itemstack;
		backPackStack?.Collectible.GetCollectibleInterface<IHeldBag>().Store(backPackStack, slot);
	}

	public void SaveSlotsIntoBags()
	{
		if (BagSlots == null)
		{
			return;
		}
		foreach (ItemSlot slot in bagContents)
		{
			SaveSlotIntoBag((ItemSlotBagContent)slot);
		}
	}

	public void ReloadBagInventory(InventoryBase parentinv, ItemSlot[] bagSlots)
	{
		BagSlots = bagSlots;
		if (BagSlots == null || BagSlots.Length == 0)
		{
			bagContents.Clear();
			return;
		}
		bagContents.Clear();
		for (int bagIndex = 0; bagIndex < BagSlots.Length; bagIndex++)
		{
			ItemStack bagstack = BagSlots[bagIndex].Itemstack;
			if (bagstack != null && bagstack.ItemAttributes != null)
			{
				bagstack.ResolveBlockOrItem(Api.World);
				IHeldBag bag = bagstack.Collectible.GetCollectibleInterface<IHeldBag>();
				if (bag != null)
				{
					List<ItemSlotBagContent> slots = bag.GetOrCreateSlots(bagstack, parentinv, bagIndex, Api.World);
					bagContents.AddRange(slots);
				}
			}
		}
		if (!(Api is ICoreClientAPI capi))
		{
			return;
		}
		ItemSlotBagContent currentHoveredSlot = capi.World.Player?.InventoryManager.CurrentHoveredSlot as ItemSlotBagContent;
		if (currentHoveredSlot?.Inventory == parentinv)
		{
			ItemSlot hslot = bagContents.FirstOrDefault((ItemSlot slot) => (slot as ItemSlotBagContent).SlotIndex == currentHoveredSlot.SlotIndex && (slot as ItemSlotBagContent).BagIndex == currentHoveredSlot.BagIndex);
			if (hslot != null)
			{
				capi.World.Player.InventoryManager.CurrentHoveredSlot = hslot;
			}
		}
	}

	/// <summary>
	/// Gets the enumerator for the inventory.
	/// </summary>
	[IteratorStateMachine(typeof(_003CGetEnumerator_003Ed__15))]
	public IEnumerator<ItemSlot> GetEnumerator()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new _003CGetEnumerator_003Ed__15(0)
		{
			_003C_003E4__this = this
		};
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
