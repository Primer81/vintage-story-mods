using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Common;

public abstract class PlayerInventoryManager : IPlayerInventoryManager
{
	public static string[] defaultInventories = new string[7] { "hotbar", "creative", "backpack", "ground", "mouse", "craftinggrid", "character" };

	public IPlayer player;

	public OrderedDictionary<string, InventoryBase> Inventories;

	public IEnumerable<InventoryBase> InventoriesOrdered => Inventories.ValuesOrdered;

	public EnumTool? ActiveTool => ActiveHotbarSlot.Itemstack?.Collectible.Tool;

	public virtual int ActiveHotbarSlotNumber { get; set; }

	public ItemSlot ActiveHotbarSlot
	{
		get
		{
			string invId = "hotbar-" + player.PlayerUID;
			GetInventory(invId, out var hotbarInv);
			int skoffset = ((hotbarInv != null && !hotbarInv[10].Empty) ? 1 : 0);
			if (ActiveHotbarSlotNumber >= 10 + skoffset)
			{
				invId = "backpack-" + player.PlayerUID;
				if (GetInventory(invId, out var backpackInv))
				{
					return backpackInv[ActiveHotbarSlotNumber - 10 - skoffset];
				}
				return null;
			}
			return hotbarInv?[ActiveHotbarSlotNumber];
		}
	}

	public ItemSlot MouseItemSlot
	{
		get
		{
			string invId = "mouse-" + player.PlayerUID;
			if (Inventories.ContainsKey(invId))
			{
				return Inventories[invId][0];
			}
			return null;
		}
	}

	Dictionary<string, IInventory> IPlayerInventoryManager.Inventories
	{
		get
		{
			Dictionary<string, IInventory> inv = new Dictionary<string, IInventory>();
			foreach (KeyValuePair<string, InventoryBase> val in Inventories)
			{
				inv[val.Key] = val.Value;
			}
			return inv;
		}
	}

	public List<IInventory> OpenedInventories => ((IEnumerable<IInventory>)InventoriesOrdered.Where((InventoryBase inv) => inv.HasOpened(player))).ToList();

	public abstract ItemSlot CurrentHoveredSlot { get; set; }

	public abstract void BroadcastHotbarSlot();

	public PlayerInventoryManager(OrderedDictionary<string, InventoryBase> AllInventories, IPlayer player)
	{
		Inventories = AllInventories;
		this.player = player;
	}

	public bool IsVisibleHandSlot(string invid, int slotNumber)
	{
		if (Inventories.ContainsKey(invid) && Inventories[invid] is InventoryPlayerHotbar)
		{
			if (ActiveHotbarSlotNumber != slotNumber)
			{
				return slotNumber == 10;
			}
			return true;
		}
		return false;
	}

	public string GetInventoryName(string inventoryClassName)
	{
		return inventoryClassName + "-" + player.PlayerUID;
	}

	public IInventory GetOwnInventory(string inventoryClassName)
	{
		if (Inventories.ContainsKey(GetInventoryName(inventoryClassName)))
		{
			return Inventories[GetInventoryName(inventoryClassName)];
		}
		return null;
	}

	public IInventory GetInventory(string inventoryClassName)
	{
		if (Inventories.ContainsKey(inventoryClassName))
		{
			return Inventories[inventoryClassName];
		}
		return null;
	}

	public ItemStack GetHotbarItemstack(int slotId)
	{
		string invId = "hotbar-" + player.PlayerUID;
		if (Inventories.ContainsKey(invId))
		{
			return Inventories[invId][slotId].Itemstack;
		}
		return null;
	}

	public IInventory GetHotbarInventory()
	{
		string invId = "hotbar-" + player.PlayerUID;
		if (Inventories.ContainsKey(invId))
		{
			return Inventories[invId];
		}
		return null;
	}

	public bool GetInventory(string invID, out InventoryBase invFound)
	{
		return Inventories.TryGetValue(invID, out invFound);
	}

	[Obsolete("Use GetBestSuitedSlot(ItemSlot sourceSlot, bool onlyPlayerInventory, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null) instead")]
	public ItemSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
	{
		return GetBestSuitedSlot(sourceSlot, onlyPlayerInventory: true, op, skipSlots);
	}

	public ItemSlot GetBestSuitedSlot(ItemSlot sourceSlot, bool onlyPlayerInventory, ItemStackMoveOperation op = null, List<ItemSlot> skipSlots = null)
	{
		WeightedSlot bestFreeslot = new WeightedSlot();
		foreach (InventoryBase inv in InventoriesOrdered.Reverse())
		{
			if ((!onlyPlayerInventory || inv is InventoryBasePlayer) && inv.HasOpened(player) && inv.CanPlayerAccess(player, new EntityPos()))
			{
				WeightedSlot freeSlot = inv.GetBestSuitedSlot(sourceSlot, op, skipSlots);
				if (freeSlot.weight > bestFreeslot.weight)
				{
					bestFreeslot = freeSlot;
				}
			}
		}
		return bestFreeslot.slot;
	}

	public bool TryGiveItemstack(ItemStack itemstack, bool slotNotifyEffect = false)
	{
		if (itemstack == null || itemstack.StackSize == 0)
		{
			return false;
		}
		ItemSlot dummySlot = new DummySlot(null);
		dummySlot.Itemstack = itemstack;
		ItemStackMoveOperation op = new ItemStackMoveOperation(player.Entity.World, EnumMouseButton.Left, (EnumModifierKey)0, EnumMergePriority.AutoMerge, itemstack.StackSize);
		object[] array = TryTransferAway(dummySlot, ref op, onlyPlayerInventory: true, null, slotNotifyEffect);
		if (dummySlot.Itemstack == null)
		{
			itemstack.StackSize = 0;
		}
		return array != null;
	}

	public object[] TryTransferAway(ItemSlot sourceSlot, ref ItemStackMoveOperation op, bool onlyPlayerInventory, bool slotNotifyEffect = false)
	{
		return TryTransferAway(sourceSlot, ref op, onlyPlayerInventory, null, slotNotifyEffect);
	}

	public object[] TryTransferAway(ItemSlot sourceSlot, ref ItemStackMoveOperation op, bool onlyPlayerInventory, StringBuilder shiftClickDebugText, bool slotNotifyEffect = false)
	{
		if (sourceSlot.Itemstack == null || !sourceSlot.CanTake())
		{
			return null;
		}
		List<object> packets = new List<object>();
		List<ItemSlot> skipSlots = new List<ItemSlot>();
		op.RequestedQuantity = sourceSlot.StackSize;
		int i = 0;
		while (i++ < 5000 && sourceSlot.StackSize > 0)
		{
			ItemSlot sinkSlot = GetBestSuitedSlot(sourceSlot, onlyPlayerInventory, op, skipSlots);
			if (sinkSlot == null)
			{
				break;
			}
			skipSlots.Add(sinkSlot);
			int beforeQuantity = sinkSlot.StackSize;
			sourceSlot.TryPutInto(sinkSlot, ref op);
			if (shiftClickDebugText != null)
			{
				if (beforeQuantity != sinkSlot.StackSize)
				{
					if (shiftClickDebugText.Length > 0)
					{
						shiftClickDebugText.Append(", ");
					}
					shiftClickDebugText.Append($"{sinkSlot.StackSize - beforeQuantity}x into {sinkSlot.Inventory?.InventoryID}");
				}
				else if (sinkSlot is ItemSlotBlackHole)
				{
					if (shiftClickDebugText.Length > 0)
					{
						shiftClickDebugText.Append(", ");
					}
					shiftClickDebugText.Append($"{op.RequestedQuantity}x into black hole slot");
				}
			}
			int quantityUnMerged = op.NotMovedQuantity;
			if (beforeQuantity != sinkSlot.StackSize && !sinkSlot.Empty && sinkSlot.Inventory is InventoryBasePlayer)
			{
				TreeAttribute tree = new TreeAttribute();
				tree["itemstack"] = new ItemstackAttribute(sinkSlot.Itemstack.Clone());
				tree["byentityid"] = new LongAttribute((player?.Entity?.EntityId).GetValueOrDefault());
				player.Entity.Api.Event.PushEvent("onitemgrabbed", tree);
			}
			if (beforeQuantity != sinkSlot.StackSize && slotNotifyEffect)
			{
				sinkSlot.MarkDirty();
				sourceSlot.MarkDirty();
				NotifySlot(player, sinkSlot);
				if (sinkSlot == ActiveHotbarSlot)
				{
					BroadcastHotbarSlot();
				}
			}
			if (sourceSlot.Inventory == null || sourceSlot is ItemSlotCreative)
			{
				if (sinkSlot.Itemstack != null && sinkSlot.Itemstack.StackSize != beforeQuantity)
				{
					packets.Add(new Packet_Client
					{
						CreateItemstack = new Packet_CreateItemstack
						{
							Itemstack = StackConverter.ToPacket(sinkSlot.Itemstack),
							TargetInventoryId = sinkSlot.Inventory.InventoryID,
							TargetLastChanged = sinkSlot.Inventory.LastChanged,
							TargetSlot = sinkSlot.Inventory.GetSlotId(sinkSlot)
						},
						Id = 10
					});
				}
				if (quantityUnMerged == 0)
				{
					break;
				}
				continue;
			}
			packets.Add(new Packet_Client
			{
				MoveItemstack = new Packet_MoveItemstack
				{
					SourceInventoryId = sourceSlot.Inventory.InventoryID,
					TargetInventoryId = sinkSlot.Inventory.InventoryID,
					SourceSlot = sourceSlot.Inventory.GetSlotId(sourceSlot),
					TargetSlot = sinkSlot.Inventory.GetSlotId(sinkSlot),
					SourceLastChanged = sourceSlot.Inventory.LastChanged,
					TargetLastChanged = sinkSlot.Inventory.LastChanged,
					Quantity = op.RequestedQuantity,
					Modifiers = (int)op.Modifiers,
					MouseButton = (int)op.MouseButton,
					Priority = (int)op.CurrentPriority
				},
				Id = 8
			});
			if (quantityUnMerged == 0 || sourceSlot.Empty)
			{
				break;
			}
		}
		if (packets.Count <= 0)
		{
			return null;
		}
		return packets.ToArray();
	}

	public void DiscardAll()
	{
		foreach (InventoryBase inv in Inventories.Values)
		{
			if (inv is InventoryBasePlayer)
			{
				inv.DiscardAll();
			}
		}
	}

	public void OnDeath()
	{
		foreach (InventoryBase inv in Inventories.Values)
		{
			if (inv is InventoryBasePlayer)
			{
				inv.OnOwningEntityDeath(player.Entity.SidedPos.XYZ);
			}
		}
	}

	public object OpenInventory(IInventory inventory)
	{
		Inventories[inventory.InventoryID] = (InventoryBase)inventory;
		return inventory.Open(player);
	}

	public object CloseInventory(IInventory inventory)
	{
		if (inventory.RemoveOnClose)
		{
			Inventories.Remove(inventory.InventoryID);
		}
		return inventory.Close(player);
	}

	public bool HasInventory(IInventory inventory)
	{
		return Inventories.ContainsValue((InventoryBase)inventory);
	}

	public abstract void NotifySlot(IPlayer player, ItemSlot slot);

	public bool DropMouseSlotItems(bool fullStack)
	{
		return DropItem(MouseItemSlot, fullStack);
	}

	public bool DropHotbarSlotItems(bool fullStack)
	{
		return DropItem(ActiveHotbarSlot, fullStack);
	}

	public void DropAllInventoryItems(IInventory inventory)
	{
		foreach (ItemSlot slot in inventory)
		{
			DropItem(slot, fullStack: true);
		}
	}

	public abstract bool DropItem(ItemSlot mouseItemSlot, bool fullStack);

	public object TryTransferTo(ItemSlot sourceSlot, ItemSlot targetSlot, ref ItemStackMoveOperation op)
	{
		if (sourceSlot.Itemstack == null || !sourceSlot.CanTake() || targetSlot == null)
		{
			return null;
		}
		int beforeQuantity = targetSlot.StackSize;
		sourceSlot.TryPutInto(targetSlot, ref op);
		if ((sourceSlot.Inventory == null || sourceSlot is ItemSlotCreative) && targetSlot.Itemstack != null && targetSlot.Itemstack.StackSize != beforeQuantity)
		{
			return new Packet_Client
			{
				CreateItemstack = new Packet_CreateItemstack
				{
					Itemstack = StackConverter.ToPacket(targetSlot.Itemstack),
					TargetInventoryId = targetSlot.Inventory.InventoryID,
					TargetLastChanged = targetSlot.Inventory.LastChanged,
					TargetSlot = targetSlot.Inventory.GetSlotId(targetSlot)
				},
				Id = 10
			};
		}
		return new Packet_Client
		{
			MoveItemstack = new Packet_MoveItemstack
			{
				SourceInventoryId = sourceSlot.Inventory.InventoryID,
				TargetInventoryId = targetSlot.Inventory.InventoryID,
				SourceSlot = sourceSlot.Inventory.GetSlotId(sourceSlot),
				TargetSlot = targetSlot.Inventory.GetSlotId(targetSlot),
				SourceLastChanged = sourceSlot.Inventory.LastChanged,
				TargetLastChanged = targetSlot.Inventory.LastChanged,
				Quantity = Math.Max(0, targetSlot.StackSize - beforeQuantity),
				Modifiers = (int)op.Modifiers,
				MouseButton = (int)op.MouseButton,
				Priority = (int)op.CurrentPriority
			},
			Id = 8
		};
	}

	public bool Find(System.Func<ItemSlot, bool> matcher)
	{
		foreach (IInventory openedInventory in OpenedInventories)
		{
			foreach (ItemSlot slot in openedInventory)
			{
				if (matcher(slot))
				{
					return true;
				}
			}
		}
		return false;
	}
}
