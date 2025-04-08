using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class InventoryTrader : InventoryBase
{
	private EntityTradingHumanoid traderEntity;

	private ItemSlot[] slots;

	private string[] ignoredAttrs = GlobalConstants.IgnoredStackAttributes.Append("backpack", "condition");

	public ItemSlotTrade[] SellingSlots
	{
		get
		{
			ItemSlotTrade[] sellslots = new ItemSlotTrade[16];
			for (int i = 0; i < 16; i++)
			{
				sellslots[i] = slots[i] as ItemSlotTrade;
			}
			return sellslots;
		}
	}

	public ItemSlotTrade[] BuyingSlots
	{
		get
		{
			ItemSlotTrade[] buyslots = new ItemSlotTrade[16];
			for (int i = 0; i < 16; i++)
			{
				buyslots[i] = slots[20 + i] as ItemSlotTrade;
			}
			return buyslots;
		}
	}

	public int BuyingCartTotalCost => 0;

	public ItemSlot MoneySlot => slots[40];

	public override int Count => 41;

	public override ItemSlot this[int slotId]
	{
		get
		{
			if (slotId < 0 || slotId >= Count)
			{
				return null;
			}
			return slots[slotId];
		}
		set
		{
			if (slotId < 0 || slotId >= Count)
			{
				throw new ArgumentOutOfRangeException("slotId");
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			slots[slotId] = value;
		}
	}

	public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
	{
		return 0f;
	}

	public InventoryTrader(string inventoryID, ICoreAPI api)
		: base(inventoryID, api)
	{
		slots = GenEmptySlots(Count);
	}

	public InventoryTrader(string className, string instanceID, ICoreAPI api)
		: base(className, instanceID, api)
	{
		slots = GenEmptySlots(Count);
	}

	internal void LateInitialize(string id, ICoreAPI api, EntityTradingHumanoid traderEntity)
	{
		base.LateInitialize(id, api);
		this.traderEntity = traderEntity;
	}

	public override object ActivateSlot(int slotId, ItemSlot mouseSlot, ref ItemStackMoveOperation op)
	{
		if (slotId <= 15)
		{
			AddToBuyingCart(slots[slotId] as ItemSlotTrade);
			return InvNetworkUtil.GetActivateSlotPacket(slotId, op);
		}
		if (slotId <= 19)
		{
			ItemSlotTrade cartSlot = slots[slotId] as ItemSlotTrade;
			if (op.MouseButton == EnumMouseButton.Right)
			{
				if (cartSlot.TradeItem?.Stack != null)
				{
					cartSlot.TakeOut(cartSlot.TradeItem.Stack.StackSize);
					cartSlot.MarkDirty();
				}
			}
			else
			{
				cartSlot.Itemstack = null;
				cartSlot.MarkDirty();
			}
			return InvNetworkUtil.GetActivateSlotPacket(slotId, op);
		}
		if (slotId <= 34)
		{
			return InvNetworkUtil.GetActivateSlotPacket(slotId, op);
		}
		if (slotId <= 39)
		{
			return base.ActivateSlot(slotId, mouseSlot, ref op);
		}
		return InvNetworkUtil.GetActivateSlotPacket(slotId, op);
	}

	private void AddToBuyingCart(ItemSlotTrade sellingSlot)
	{
		if (sellingSlot.Empty)
		{
			return;
		}
		for (int j = 0; j < 4; j++)
		{
			ItemSlotTrade slot2 = slots[16 + j] as ItemSlotTrade;
			if (!slot2.Empty && slot2.Itemstack.Equals(Api.World, sellingSlot.Itemstack) && slot2.Itemstack.StackSize + sellingSlot.TradeItem.Stack.StackSize <= slot2.Itemstack.Collectible.MaxStackSize)
			{
				slot2.Itemstack.StackSize += sellingSlot.TradeItem.Stack.StackSize;
				slot2.MarkDirty();
				return;
			}
		}
		for (int i = 0; i < 4; i++)
		{
			ItemSlotTrade slot = slots[16 + i] as ItemSlotTrade;
			if (slot.Empty)
			{
				slot.Itemstack = sellingSlot.TradeItem.Stack.Clone();
				slot.Itemstack.ResolveBlockOrItem(Api.World);
				slot.TradeItem = sellingSlot.TradeItem;
				slot.MarkDirty();
				break;
			}
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree)
	{
		slots = SlotsFromTreeAttributes(tree, slots);
		ITreeAttribute tradeItems = tree.GetTreeAttribute("tradeItems");
		if (tradeItems == null)
		{
			return;
		}
		for (int slotId = 0; slotId < slots.Length; slotId++)
		{
			if (slots[slotId] is ItemSlotTrade && !slots[slotId].Empty)
			{
				(slots[slotId] as ItemSlotTrade).TradeItem = new ResolvedTradeItem(tradeItems.GetTreeAttribute(slotId.ToString() ?? ""));
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		SlotsToTreeAttributes(slots, tree);
		TreeAttribute tradeItemTree = new TreeAttribute();
		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i].Itemstack != null && slots[i] is ItemSlotTrade)
			{
				TreeAttribute subtree = new TreeAttribute();
				(slots[i] as ItemSlotTrade).TradeItem?.ToTreeAttributes(subtree);
				tradeItemTree[i.ToString() ?? ""] = subtree;
			}
		}
		tree["tradeItems"] = tradeItemTree;
	}

	internal EnumTransactionResult TryBuySell(IPlayer buyingPlayer)
	{
		if (!HasPlayerEnoughAssets(buyingPlayer))
		{
			return EnumTransactionResult.PlayerNotEnoughAssets;
		}
		if (!HasTraderEnoughAssets())
		{
			return EnumTransactionResult.TraderNotEnoughAssets;
		}
		if (!HasTraderEnoughStock(buyingPlayer))
		{
			return EnumTransactionResult.TraderNotEnoughSupplyOrDemand;
		}
		if (!HasTraderEnoughDemand(buyingPlayer))
		{
			return EnumTransactionResult.TraderNotEnoughSupplyOrDemand;
		}
		if (Api.Side == EnumAppSide.Client)
		{
			for (int i = 0; i < 4; i++)
			{
				GetBuyingCartSlot(i).Itemstack = null;
			}
			return EnumTransactionResult.Success;
		}
		for (int m = 0; m < 4; m++)
		{
			ItemSlotTrade slot4 = GetBuyingCartSlot(m);
			if (slot4.Itemstack?.Collectible is ITradeableCollectible itc3)
			{
				EnumTransactionResult result2 = itc3.OnTryTrade(traderEntity, slot4, EnumTradeDirection.Buy);
				if (result2 != EnumTransactionResult.Success)
				{
					return result2;
				}
			}
		}
		for (int l = 0; l < 4; l++)
		{
			ItemSlot slot3 = GetSellingCartSlot(l);
			if (slot3.Itemstack?.Collectible is ITradeableCollectible itc2)
			{
				EnumTransactionResult result = itc2.OnTryTrade(traderEntity, slot3, EnumTradeDirection.Sell);
				if (result != EnumTransactionResult.Success)
				{
					return result;
				}
			}
		}
		if (!HandleMoneyTransaction(buyingPlayer))
		{
			return EnumTransactionResult.Failure;
		}
		for (int k = 0; k < 4; k++)
		{
			ItemSlotTrade slot2 = GetBuyingCartSlot(k);
			if (slot2.Itemstack != null)
			{
				GiveOrDrop(buyingPlayer.Entity, slot2.Itemstack);
				slot2.TradeItem.Stock -= slot2.Itemstack.StackSize / slot2.TradeItem.Stack.StackSize;
				slot2.Itemstack = null;
				slot2.MarkDirty();
			}
		}
		for (int j = 0; j < 4; j++)
		{
			ItemSlot slot = GetSellingCartSlot(j);
			if (slot.Itemstack == null)
			{
				continue;
			}
			ResolvedTradeItem tradeItem = GetBuyingConditionsSlot(slot.Itemstack).TradeItem;
			if (tradeItem != null)
			{
				int q = slot.Itemstack.StackSize / tradeItem.Stack.StackSize;
				tradeItem.Stock -= q;
				ItemStack stack = slot.TakeOut(q * tradeItem.Stack.StackSize);
				if (stack.Collectible is ITradeableCollectible itc)
				{
					itc.OnDidTrade(traderEntity, stack, EnumTradeDirection.Buy);
				}
				slot.MarkDirty();
			}
		}
		return EnumTransactionResult.Success;
	}

	public bool HasTraderEnoughStock(IPlayer player)
	{
		Dictionary<int, int> Stocks = new Dictionary<int, int>();
		for (int i = 0; i < 4; i++)
		{
			ItemSlotTrade slot = GetBuyingCartSlot(i);
			if (slot.Itemstack != null)
			{
				ItemSlotTrade tradeSlot = GetSellingConditionsSlot(slot.Itemstack);
				int tradeslotid = GetSlotId(tradeSlot);
				if (!Stocks.TryGetValue(tradeslotid, out var stock))
				{
					stock = slot.TradeItem.Stock;
				}
				Stocks[tradeslotid] = stock - slot.Itemstack.StackSize / slot.TradeItem.Stack.StackSize;
				if (Stocks[tradeslotid] < 0)
				{
					player.InventoryManager.NotifySlot(player, slot);
					player.InventoryManager.NotifySlot(player, tradeSlot);
					return false;
				}
			}
		}
		return true;
	}

	public bool HasTraderEnoughDemand(IPlayer player)
	{
		Dictionary<int, int> Stocks = new Dictionary<int, int>();
		for (int i = 0; i < 4; i++)
		{
			ItemSlot slot = GetSellingCartSlot(i);
			if (slot.Itemstack != null)
			{
				ItemSlotTrade tradeSlot = GetBuyingConditionsSlot(slot.Itemstack);
				ResolvedTradeItem tradeItem = tradeSlot?.TradeItem;
				if (tradeItem == null)
				{
					player.InventoryManager.NotifySlot(player, slot);
					return false;
				}
				int tradeslotid = GetSlotId(tradeSlot);
				if (!Stocks.TryGetValue(tradeslotid, out var stock))
				{
					stock = tradeItem.Stock;
				}
				Stocks[tradeslotid] = stock - slot.Itemstack.StackSize / tradeItem.Stack.StackSize;
				if (Stocks[tradeslotid] < 0)
				{
					player.InventoryManager.NotifySlot(player, tradeSlot);
					player.InventoryManager.NotifySlot(player, slot);
					return false;
				}
			}
		}
		return true;
	}

	public bool IsTraderInterestedIn(ItemStack stack)
	{
		ItemSlotTrade tradeSlot = GetBuyingConditionsSlot(stack);
		ResolvedTradeItem tradeItem = tradeSlot?.TradeItem;
		if (tradeItem == null)
		{
			return false;
		}
		if (tradeItem.Stock == 0)
		{
			PerformNotifySlot(GetSlotId(tradeSlot));
		}
		return tradeItem.Stock > 0;
	}

	public bool HasPlayerEnoughAssets(IPlayer buyingPlayer)
	{
		int playerAssets = GetPlayerAssets(buyingPlayer.Entity);
		int totalCost = GetTotalCost();
		int totalGain = GetTotalGain();
		if (playerAssets - totalCost + totalGain < 0)
		{
			return false;
		}
		return true;
	}

	public bool HasTraderEnoughAssets()
	{
		int traderAssets = GetTraderAssets();
		int totalCost = GetTotalCost();
		int totalGain = GetTotalGain();
		if (traderAssets + totalCost - totalGain < 0)
		{
			return false;
		}
		return true;
	}

	private bool HandleMoneyTransaction(IPlayer buyingPlayer)
	{
		int playerAssets = GetPlayerAssets(buyingPlayer.Entity);
		int traderAssets = GetTraderAssets();
		int totalCost = GetTotalCost();
		int totalGain = GetTotalGain();
		if (playerAssets - totalCost + totalGain < 0)
		{
			return false;
		}
		if (traderAssets + totalCost - totalGain < 0)
		{
			return false;
		}
		int deduct = totalCost - totalGain;
		if (deduct > 0)
		{
			DeductFromEntity(Api, buyingPlayer.Entity, deduct);
			GiveToTrader(deduct);
		}
		else
		{
			GiveOrDrop(buyingPlayer.Entity, new ItemStack(Api.World.GetItem(new AssetLocation("gear-rusty"))), -deduct, null);
			DeductFromTrader(-deduct);
		}
		return true;
	}

	public void GiveToTrader(int units)
	{
		if (MoneySlot.Empty)
		{
			MoneySlot.Itemstack = new ItemStack(Api.World.GetItem(new AssetLocation("gear-rusty")), units);
		}
		else
		{
			MoneySlot.Itemstack.StackSize += units;
		}
		MoneySlot.MarkDirty();
	}

	public void DeductFromTrader(int units)
	{
		MoneySlot.Itemstack.StackSize -= units;
		if (MoneySlot.StackSize <= 0)
		{
			MoneySlot.Itemstack = null;
		}
		MoneySlot.MarkDirty();
	}

	public static void DeductFromEntity(ICoreAPI api, EntityAgent eagent, int totalUnitsToDeduct)
	{
		SortedDictionary<int, List<ItemSlot>> moneys = new SortedDictionary<int, List<ItemSlot>>();
		eagent.WalkInventory(delegate(ItemSlot invslot)
		{
			if (invslot is ItemSlotCreative)
			{
				return true;
			}
			if (invslot.Itemstack == null || invslot.Itemstack.Collectible.Attributes == null)
			{
				return true;
			}
			int num = CurrencyValuePerItem(invslot);
			if (num != 0)
			{
				List<ItemSlot> value = null;
				if (!moneys.TryGetValue(num, out value))
				{
					value = new List<ItemSlot>();
				}
				value.Add(invslot);
				moneys[num] = value;
			}
			return true;
		});
		foreach (KeyValuePair<int, List<ItemSlot>> val2 in moneys.Reverse())
		{
			int pieceValue2 = val2.Key;
			foreach (ItemSlot slot2 in val2.Value)
			{
				int removeUnits2 = Math.Min(pieceValue2 * slot2.StackSize, totalUnitsToDeduct);
				removeUnits2 = removeUnits2 / pieceValue2 * pieceValue2;
				slot2.Itemstack.StackSize -= removeUnits2 / pieceValue2;
				if (slot2.StackSize <= 0)
				{
					slot2.Itemstack = null;
				}
				slot2.MarkDirty();
				totalUnitsToDeduct -= removeUnits2;
			}
			if (totalUnitsToDeduct <= 0)
			{
				break;
			}
		}
		if (totalUnitsToDeduct > 0)
		{
			foreach (KeyValuePair<int, List<ItemSlot>> val in moneys)
			{
				int pieceValue = val.Key;
				foreach (ItemSlot slot in val.Value)
				{
					int removeUnits = Math.Max(pieceValue, Math.Min(pieceValue * slot.StackSize, totalUnitsToDeduct));
					removeUnits = removeUnits / pieceValue * pieceValue;
					slot.Itemstack.StackSize -= removeUnits / pieceValue;
					if (slot.StackSize <= 0)
					{
						slot.Itemstack = null;
					}
					slot.MarkDirty();
					totalUnitsToDeduct -= removeUnits;
				}
				if (totalUnitsToDeduct <= 0)
				{
					break;
				}
			}
		}
		if (totalUnitsToDeduct < 0)
		{
			GiveOrDrop(eagent, new ItemStack(api.World.GetItem(new AssetLocation("gear-rusty"))), -totalUnitsToDeduct, null);
		}
	}

	public void GiveOrDrop(EntityAgent eagent, ItemStack stack)
	{
		if (stack != null)
		{
			GiveOrDrop(eagent, stack, stack.StackSize, traderEntity);
		}
	}

	public static void GiveOrDrop(EntityAgent eagent, ItemStack stack, int quantity, EntityTradingHumanoid entityTrader)
	{
		if (stack == null)
		{
			return;
		}
		while (quantity > 0)
		{
			int stacksize = Math.Min(quantity, stack.Collectible.MaxStackSize);
			if (stacksize <= 0)
			{
				break;
			}
			ItemStack stackPart = stack.Clone();
			stackPart.StackSize = stacksize;
			if (entityTrader != null && stackPart.Collectible is ITradeableCollectible itc)
			{
				itc.OnDidTrade(entityTrader, stackPart, EnumTradeDirection.Sell);
			}
			if (!eagent.TryGiveItemStack(stackPart))
			{
				eagent.World.SpawnItemEntity(stackPart, eagent.Pos.XYZ);
			}
			quantity -= stacksize;
		}
	}

	public static int GetPlayerAssets(EntityAgent eagent)
	{
		int totalAssets = 0;
		eagent.WalkInventory(delegate(ItemSlot invslot)
		{
			if (invslot is ItemSlotCreative || !(invslot.Inventory is InventoryBasePlayer))
			{
				return true;
			}
			totalAssets += CurrencyValuePerItem(invslot) * invslot.StackSize;
			return true;
		});
		return totalAssets;
	}

	public int GetTraderAssets()
	{
		int totalAssets = 0;
		if (MoneySlot.Empty)
		{
			return 0;
		}
		return totalAssets + CurrencyValuePerItem(MoneySlot) * MoneySlot.StackSize;
	}

	private static int CurrencyValuePerItem(ItemSlot slot)
	{
		JsonObject obj = slot.Itemstack?.Collectible?.Attributes?["currency"];
		if (obj != null && obj.Exists)
		{
			JsonObject v = obj["value"];
			if (!v.Exists)
			{
				return 0;
			}
			return v.AsInt();
		}
		return 0;
	}

	public int GetTotalCost()
	{
		int totalCost = 0;
		for (int i = 0; i < 4; i++)
		{
			ItemSlotTrade buySlot = GetBuyingCartSlot(i);
			ResolvedTradeItem tradeitem = buySlot.TradeItem;
			if (tradeitem != null)
			{
				int cnt = buySlot.StackSize / tradeitem.Stack.StackSize;
				totalCost += tradeitem.Price * cnt;
			}
		}
		return totalCost;
	}

	public int GetTotalGain()
	{
		int totalGain = 0;
		for (int i = 0; i < 4; i++)
		{
			ItemSlotSurvival sellSlot = GetSellingCartSlot(i);
			if (sellSlot.Itemstack != null)
			{
				ResolvedTradeItem tradeitem = GetBuyingConditionsSlot(sellSlot.Itemstack)?.TradeItem;
				if (tradeitem != null)
				{
					int cnt = sellSlot.StackSize / tradeitem.Stack.StackSize;
					totalGain += tradeitem.Price * cnt;
				}
			}
		}
		return totalGain;
	}

	protected override ItemSlot NewSlot(int slotId)
	{
		if (slotId < 36)
		{
			return new ItemSlotTrade(this, slotId > 19 && slotId <= 35);
		}
		return new ItemSlotBuying(this);
	}

	public ItemSlotTrade GetSellingSlot(int index)
	{
		return slots[index] as ItemSlotTrade;
	}

	public ItemSlotTrade GetBuyingSlot(int index)
	{
		return slots[20 + index] as ItemSlotTrade;
	}

	public ItemSlotTrade GetBuyingCartSlot(int index)
	{
		return slots[16 + index] as ItemSlotTrade;
	}

	public ItemSlotSurvival GetSellingCartSlot(int index)
	{
		return slots[36 + index] as ItemSlotSurvival;
	}

	public ItemSlotTrade GetBuyingConditionsSlot(ItemStack forStack)
	{
		for (int i = 0; i < 16; i++)
		{
			ItemSlotTrade slot = GetBuyingSlot(i);
			if (slot.Itemstack != null)
			{
				string[] ignoredAttributes = ((slot.TradeItem.AttributesToIgnore == null) ? ignoredAttrs : ignoredAttrs.Append<string>(slot.TradeItem.AttributesToIgnore.Split(',')));
				if ((slot.Itemstack.Equals(Api.World, forStack, ignoredAttributes) || slot.Itemstack.Satisfies(forStack)) && forStack.Collectible.IsReasonablyFresh(traderEntity.World, forStack))
				{
					return slot;
				}
			}
		}
		return null;
	}

	public ItemSlotTrade GetSellingConditionsSlot(ItemStack forStack)
	{
		for (int i = 0; i < 16; i++)
		{
			ItemSlotTrade slot = GetSellingSlot(i);
			if (slot.Itemstack != null && slot.Itemstack.Equals(Api.World, forStack, GlobalConstants.IgnoredStackAttributes))
			{
				return slot;
			}
		}
		return null;
	}

	public override object Close(IPlayer player)
	{
		object p = base.Close(player);
		for (int i = 0; i < 4; i++)
		{
			slots[i + 16].Itemstack = null;
			Api.World.SpawnItemEntity(slots[i + 36].Itemstack, traderEntity.ServerPos.XYZ);
			slots[i + 36].Itemstack = null;
		}
		traderEntity.tradingWithPlayer = null;
		return p;
	}

	public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op, List<ItemSlot> skipSlots = null)
	{
		WeightedSlot bestWSlot = new WeightedSlot();
		if (PutLocked || sourceSlot.Inventory == this)
		{
			return bestWSlot;
		}
		if (!IsTraderInterestedIn(sourceSlot.Itemstack))
		{
			return bestWSlot;
		}
		if (CurrencyValuePerItem(sourceSlot) != 0)
		{
			return bestWSlot;
		}
		for (int i = 0; i < 4; i++)
		{
			ItemSlot slot = GetSellingCartSlot(i);
			if ((skipSlots == null || !skipSlots.Contains(slot)) && slot.CanTakeFrom(sourceSlot))
			{
				float curWeight = GetSuitability(sourceSlot, slot, slot.Itemstack != null);
				if (bestWSlot.slot == null || bestWSlot.weight < curWeight)
				{
					bestWSlot.slot = slot;
					bestWSlot.weight = curWeight;
				}
			}
		}
		return bestWSlot;
	}
}
