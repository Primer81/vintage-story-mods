using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class InventoryPlayerCreative : InventoryBasePlayer, ITabbedInventory, IInventory, IReadOnlyCollection<ItemSlot>, IEnumerable<ItemSlot>, IEnumerable
{
	public CreativeTabs tabs = new CreativeTabs();

	private CreativeTab currentTab;

	private ItemSlotBlackHole blackholeSlot;

	public bool Accessible
	{
		get
		{
			EnumGameMode? mode = Api.World.PlayerByUid(playerUID)?.WorldData.CurrentGameMode;
			if (playerUID != null)
			{
				return mode.GetValueOrDefault() == EnumGameMode.Creative;
			}
			return false;
		}
	}

	public CreativeTab CurrentTab => currentTab;

	public int CurrentTabIndex => (currentTab.Inventory as CreativeInventoryTab).TabIndex;

	public override int Count => currentTab.Inventory.Count;

	public CreativeTabs CreativeTabs => tabs;

	public override ItemSlot this[int slotId]
	{
		get
		{
			if (slotId == 99999)
			{
				return blackholeSlot;
			}
			if (slotId < 0 || slotId >= Count)
			{
				return new ItemSlotCreative(this);
			}
			return currentTab.Inventory[slotId];
		}
		set
		{
			throw new NotSupportedException("InventoryPlayerCreative doesn't support replacing slots");
		}
	}

	public InventoryPlayerCreative(string className, string playerUID, ICoreAPI api)
		: base(className, playerUID, api)
	{
		blackholeSlot = new ItemSlotBlackHole(this);
		InvNetworkUtil = new CreativeNetworkUtil(this, api);
	}

	public InventoryPlayerCreative(string inventoryId, ICoreAPI api)
		: base(inventoryId, api)
	{
		blackholeSlot = new ItemSlotBlackHole(this);
		InvNetworkUtil = new CreativeNetworkUtil(this, api);
	}

	public override void LateInitialize(string inventoryID, ICoreAPI api)
	{
		base.LateInitialize(inventoryID, api);
		(InvNetworkUtil as CreativeNetworkUtil).Api = api;
	}

	public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
	{
		return 0f;
	}

	public override bool CanPlayerAccess(IPlayer player, EntityPos position)
	{
		if (base.CanPlayerAccess(player, position))
		{
			return player.WorldData.CurrentGameMode == EnumGameMode.Creative;
		}
		return false;
	}

	public override object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
	{
		if (!Accessible)
		{
			return null;
		}
		if (op.ShiftDown)
		{
			return base.ActivateSlot(slotId, sourceSlot, ref op);
		}
		Packet_Client obj = (Packet_Client)base.ActivateSlot(slotId, sourceSlot, ref op);
		obj.ActivateInventorySlot.TabIndex = currentTab.Index;
		return obj;
	}

	internal void UpdateFromWorld(IWorldAccessor world)
	{
		if (tabs.TabsByCode.Count != 0)
		{
			return;
		}
		IList<Block> blocks = world.Blocks;
		IList<Item> items = world.Items;
		blocks = blocks.OrderBy((Block elem) => elem?.BlockMaterial).ToList();
		items = items.OrderBy((Item elem) => elem?.Tool).ToList();
		CollectibleObject[] collectibles = new CollectibleObject[blocks.Count + items.Count];
		Array.Copy(blocks.ToArray(), collectibles, blocks.Count);
		Array.Copy(items.ToArray(), 0, collectibles, blocks.Count, items.Count);
		Dictionary<string, List<ItemStack>> dictionary = GatherTabStacks(collectibles);
		tabs = new CreativeTabs();
		foreach (KeyValuePair<string, List<ItemStack>> val in dictionary)
		{
			tabs.Add(CreateTab(val.Key, val.Value));
		}
		SetTab(0);
	}

	private CreativeTab CreateTab(string tabCode, List<ItemStack> tabStacks)
	{
		CreativeTab tab = new CreativeTab(tabCode, new CreativeInventoryTab(tabStacks.Count, base.InventoryID, Api));
		int i = 0;
		foreach (ItemStack stack in tabStacks)
		{
			tab.Inventory[i++].Itemstack = stack;
		}
		return tab;
	}

	private Dictionary<string, List<ItemStack>> GatherTabStacks(CollectibleObject[] collectibles)
	{
		Dictionary<string, List<ItemStack>> itemstacksByTab = new Dictionary<string, List<ItemStack>>();
		foreach (CollectibleObject collectible in collectibles)
		{
			if (collectible?.CreativeInventoryTabs != null)
			{
				string[] creativeInventoryTabs = collectible.CreativeInventoryTabs;
				foreach (string tab in creativeInventoryTabs)
				{
					List<ItemStack> stackList2 = null;
					if (!itemstacksByTab.TryGetValue(tab, out stackList2))
					{
						stackList2 = (itemstacksByTab[tab] = new List<ItemStack>());
					}
					stackList2.Add(new ItemStack(collectible));
				}
			}
			if (collectible?.CreativeInventoryStacks == null)
			{
				continue;
			}
			for (int j = 0; j < collectible.CreativeInventoryStacks.Length; j++)
			{
				CreativeTabAndStackList ctasl = collectible.CreativeInventoryStacks[j];
				for (int k = 0; k < ctasl.Tabs.Length; k++)
				{
					if (!itemstacksByTab.TryGetValue(ctasl.Tabs[k], out var stackList))
					{
						stackList = new List<ItemStack>();
						itemstacksByTab[ctasl.Tabs[k]] = stackList;
					}
					for (int l = 0; l < ctasl.Stacks.Length; l++)
					{
						ItemStack stack = ctasl.Stacks[l].ResolvedItemstack.Clone();
						stack.ResolveBlockOrItem(Api.World);
						stackList.Add(stack);
					}
				}
			}
		}
		return itemstacksByTab;
	}

	public void SetTab(int tabIndex)
	{
		currentTab = tabs.Tabs.FirstOrDefault((CreativeTab tab) => tab.Index == tabIndex);
		(currentTab.Inventory as CreativeInventoryTab).TabIndex = tabIndex;
	}

	public override void AfterBlocksLoaded(IWorldAccessor world)
	{
	}

	public override object Open(IPlayer player)
	{
		UpdateFromWorld(player.Entity.World);
		return currentTab?.Inventory.Open(player);
	}

	public override object Close(IPlayer player)
	{
		return currentTab?.Inventory.Close(player);
	}

	public override void ResolveBlocksOrItems()
	{
	}

	public override int GetSlotId(ItemSlot slot)
	{
		if (slot is ItemSlotBlackHole)
		{
			return 99999;
		}
		if (slot.Itemstack == null)
		{
			return -1;
		}
		for (int i = 0; i < Count; i++)
		{
			if (slot.Itemstack.Equals(this[i].Itemstack))
			{
				return i;
			}
		}
		return -1;
	}

	public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
	{
		if (skipSlots.Contains(blackholeSlot))
		{
			return new WeightedSlot
			{
				weight = -1f
			};
		}
		if (!blackholeSlot.CanTakeFrom(sourceSlot))
		{
			return new WeightedSlot
			{
				slot = null,
				weight = 0f
			};
		}
		return new WeightedSlot
		{
			slot = blackholeSlot,
			weight = 0.01f
		};
	}

	public override void FromTreeAttributes(ITreeAttribute tree)
	{
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
	}

	public override void MarkSlotDirty(int slotId)
	{
	}

	public override void DiscardAll()
	{
	}

	public override void DropAll(Vec3d pos, int maxStackSize = 0)
	{
	}

	public override bool HasOpened(IPlayer player)
	{
		return player.WorldData.CurrentGameMode == EnumGameMode.Creative;
	}

	public CreativeTab GetSelectedTab()
	{
		return currentTab;
	}
}
