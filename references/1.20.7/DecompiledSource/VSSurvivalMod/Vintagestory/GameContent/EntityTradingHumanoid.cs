using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityTradingHumanoid : EntityDressedHumanoid
{
	public InventoryTrader Inventory;

	public TradeProperties TradeProps;

	public EntityPlayer tradingWithPlayer;

	protected GuiDialog dlg;

	protected int tickCount;

	protected double doubleRefreshIntervalDays = 7.0;

	private bool wasImported;

	private EntityBehaviorConversable ConversableBh => GetBehavior<EntityBehaviorConversable>();

	public virtual EntityTalkUtil TalkUtil { get; }

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		EntityBehaviorConversable bh = GetBehavior<EntityBehaviorConversable>();
		if (bh != null)
		{
			bh.OnControllerCreated = (Action<DialogueController>)Delegate.Combine(bh.OnControllerCreated, (Action<DialogueController>)delegate(DialogueController controller)
			{
				controller.DialogTriggers += Dialog_DialogTriggers;
			});
		}
		if (Inventory == null)
		{
			Inventory = new InventoryTrader("traderInv", EntityId.ToString() ?? "", api);
		}
		if (api.Side == EnumAppSide.Server)
		{
			string stringpath = base.Properties.Attributes?["tradePropsFile"].AsString();
			AssetLocation filepath = null;
			try
			{
				if (stringpath != null)
				{
					filepath = ((stringpath == null) ? null : AssetLocation.Create(stringpath, Code.Domain));
					TradeProps = api.Assets.Get(filepath.WithPathAppendixOnce(".json")).ToObject<TradeProperties>();
				}
				else
				{
					TradeProps = base.Properties.Attributes["tradeProps"]?.AsObject<TradeProperties>();
				}
			}
			catch (Exception e2)
			{
				api.World.Logger.Error("Failed deserializing TradeProperties for trader {0}, exception logged to verbose debug", properties.Code);
				api.World.Logger.Error(e2);
				api.World.Logger.VerboseDebug("Failed deserializing TradeProperties:");
				api.World.Logger.VerboseDebug("=================");
				api.World.Logger.VerboseDebug("Tradeprops json:");
				if (filepath != null)
				{
					api.World.Logger.VerboseDebug("File path {0}:", filepath);
				}
				api.World.Logger.VerboseDebug("{0}", base.Properties.Server.Attributes["tradeProps"].ToJsonToken());
			}
		}
		try
		{
			Inventory.LateInitialize("traderInv-" + EntityId, api, this);
		}
		catch (Exception e)
		{
			api.World.Logger.Error("Failed initializing trader inventory. Will recreate. Exception logged to verbose debug");
			api.World.Logger.Error(e);
			api.World.Logger.VerboseDebug("Failed initializing trader inventory. Will recreate.");
			WatchedAttributes.RemoveAttribute("traderInventory");
			Inventory = new InventoryTrader("traderInv", EntityId.ToString() ?? "", api);
			Inventory.LateInitialize("traderInv-" + EntityId, api, this);
			RefreshBuyingSellingInventory();
		}
	}

	public override void OnEntitySpawn()
	{
		base.OnEntitySpawn();
		if (World.Api.Side == EnumAppSide.Server)
		{
			setupTaskBlocker();
			reloadTradingList();
		}
	}

	private void reloadTradingList()
	{
		if (TradeProps != null)
		{
			RefreshBuyingSellingInventory();
			WatchedAttributes.SetDouble("lastRefreshTotalDays", World.Calendar.TotalDays - World.Rand.NextDouble() * 6.0);
			Inventory.MoneySlot.Itemstack = null;
			Inventory.GiveToTrader((int)TradeProps.Money.nextFloat(1f, World.Rand));
		}
	}

	public override void DidImportOrExport(BlockPos startPos)
	{
		base.DidImportOrExport(startPos);
		wasImported = true;
	}

	public override void OnEntityLoaded()
	{
		base.OnEntityLoaded();
		if (Api.Side == EnumAppSide.Server)
		{
			setupTaskBlocker();
			if (wasImported)
			{
				reloadTradingList();
			}
		}
	}

	protected void setupTaskBlocker()
	{
		EntityBehaviorTaskAI taskAi = GetBehavior<EntityBehaviorTaskAI>();
		if (taskAi != null)
		{
			taskAi.TaskManager.OnShouldExecuteTask += (IAiTask task) => tradingWithPlayer == null;
		}
		EntityBehaviorActivityDriven actAi = GetBehavior<EntityBehaviorActivityDriven>();
		if (actAi != null)
		{
			actAi.OnShouldRunActivitySystem += () => tradingWithPlayer == null;
		}
	}

	protected void RefreshBuyingSellingInventory(float refreshChance = 1.1f)
	{
		if (TradeProps == null)
		{
			return;
		}
		TradeProps.Buying.List.Shuffle(World.Rand);
		int buyingQuantity = Math.Min(TradeProps.Buying.List.Length, TradeProps.Buying.MaxItems);
		TradeProps.Selling.List.Shuffle(World.Rand);
		int sellingQuantity = Math.Min(TradeProps.Selling.List.Length, TradeProps.Selling.MaxItems);
		Stack<TradeItem> newBuyItems = new Stack<TradeItem>();
		Stack<TradeItem> newsellItems = new Stack<TradeItem>();
		ItemSlotTrade[] sellingSlots = Inventory.SellingSlots;
		ItemSlotTrade[] buyingSlots = Inventory.BuyingSlots;
		string[] ignoredAttributes = GlobalConstants.IgnoredStackAttributes.Append("condition");
		for (int j = 0; j < TradeProps.Selling.List.Length; j++)
		{
			if (newsellItems.Count >= sellingQuantity)
			{
				break;
			}
			TradeItem item2 = TradeProps.Selling.List[j];
			if (item2.Resolve(World, "tradeItem resolver") && !sellingSlots.Any((ItemSlotTrade slot) => slot?.Itemstack != null && slot.TradeItem.Stock > 0 && (item2.ResolvedItemstack?.Equals(World, slot.Itemstack, ignoredAttributes) ?? false)))
			{
				newsellItems.Push(item2);
			}
		}
		for (int i = 0; i < TradeProps.Buying.List.Length; i++)
		{
			if (newBuyItems.Count >= buyingQuantity)
			{
				break;
			}
			TradeItem item = TradeProps.Buying.List[i];
			if (item.Resolve(World, "tradeItem resolver") && !buyingSlots.Any((ItemSlotTrade slot) => slot?.Itemstack != null && slot.TradeItem.Stock > 0 && (item.ResolvedItemstack?.Equals(World, slot.Itemstack, ignoredAttributes) ?? false)))
			{
				newBuyItems.Push(item);
			}
		}
		replaceTradeItems(newBuyItems, buyingSlots, buyingQuantity, refreshChance, EnumTradeDirection.Buy);
		replaceTradeItems(newsellItems, sellingSlots, sellingQuantity, refreshChance, EnumTradeDirection.Sell);
		ITreeAttribute tree = GetOrCreateTradeStore();
		Inventory.ToTreeAttributes(tree);
		WatchedAttributes.MarkAllDirty();
	}

	protected void replaceTradeItems(Stack<TradeItem> newItems, ItemSlotTrade[] slots, int quantity, float refreshChance, EnumTradeDirection tradeDir)
	{
		HashSet<int> refreshedSlots = new HashSet<int>();
		for (int i = 0; i < quantity; i++)
		{
			if (World.Rand.NextDouble() > (double)refreshChance)
			{
				continue;
			}
			if (newItems.Count == 0)
			{
				break;
			}
			TradeItem newTradeItem = newItems.Pop();
			if (newTradeItem.ResolvedItemstack.Collectible is ITradeableCollectible itc && !itc.ShouldTrade(this, newTradeItem, tradeDir))
			{
				i--;
				continue;
			}
			int duplSlotIndex = slots.IndexOf((ItemSlotTrade bslot) => bslot.Itemstack != null && bslot.TradeItem.Stock == 0 && (newTradeItem?.ResolvedItemstack.Equals(World, bslot.Itemstack, GlobalConstants.IgnoredStackAttributes) ?? false));
			ItemSlotTrade intoSlot;
			if (duplSlotIndex != -1)
			{
				intoSlot = slots[duplSlotIndex];
				refreshedSlots.Add(duplSlotIndex);
			}
			else
			{
				for (; refreshedSlots.Contains(i); i++)
				{
				}
				if (i >= slots.Length)
				{
					break;
				}
				intoSlot = slots[i];
				refreshedSlots.Add(i);
			}
			ResolvedTradeItem titem = newTradeItem.Resolve(World);
			if (titem.Stock > 0)
			{
				intoSlot.SetTradeItem(titem);
				intoSlot.MarkDirty();
			}
		}
	}

	protected int Dialog_DialogTriggers(EntityAgent triggeringEntity, string value, JsonObject data)
	{
		if (value == "opentrade")
		{
			ConversableBh.Dialog?.TryClose();
			TryOpenTradeDialog(triggeringEntity);
			tradingWithPlayer = triggeringEntity as EntityPlayer;
			return 0;
		}
		return -1;
	}

	private void TryOpenTradeDialog(EntityAgent forEntity)
	{
		if (!Alive || World.Side != EnumAppSide.Client)
		{
			return;
		}
		EntityPlayer entityplr = forEntity as EntityPlayer;
		IPlayer player = World.PlayerByUid(entityplr.PlayerUID);
		ICoreClientAPI capi = (ICoreClientAPI)Api;
		if (forEntity.Pos.SquareDistanceTo(Pos) <= 5f)
		{
			GuiDialog guiDialog = dlg;
			if (guiDialog == null || !guiDialog.IsOpened())
			{
				if (capi.Gui.OpenedGuis.FirstOrDefault((GuiDialog dlg) => dlg is GuiDialogTrader && dlg.IsOpened()) == null)
				{
					capi.Network.SendEntityPacket(EntityId, 1001);
					player.InventoryManager.OpenInventory(Inventory);
					dlg = new GuiDialogTrader(Inventory, this, World.Api as ICoreClientAPI);
					dlg.TryOpen();
				}
				else
				{
					capi.TriggerIngameError(this, "onlyonedialog", Lang.Get("Can only trade with one trader at a time"));
				}
				return;
			}
		}
		capi.Network.SendPacketClient(capi.World.Player.InventoryManager.CloseInventory(Inventory));
	}

	public override void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data)
	{
		base.OnReceivedClientPacket(player, packetid, data);
		if (packetid < 1000)
		{
			Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
			return;
		}
		if (packetid == 1000 && Inventory.TryBuySell(player) == EnumTransactionResult.Success)
		{
			(Api as ICoreServerAPI).WorldManager.GetChunk(ServerPos.AsBlockPos)?.MarkModified();
			AnimManager.StopAnimation("idle");
			AnimManager.StartAnimation(new AnimationMetaData
			{
				Animation = "nod",
				Code = "nod",
				Weight = 10f,
				EaseOutSpeed = 10000f,
				EaseInSpeed = 10000f
			});
			TreeAttribute tree = new TreeAttribute();
			Inventory.ToTreeAttributes(tree);
			(Api as ICoreServerAPI).Network.BroadcastEntityPacket(EntityId, 1234, tree.ToBytes());
		}
		if (packetid == 1001)
		{
			player.InventoryManager.OpenInventory(Inventory);
		}
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		base.OnReceivedServerPacket(packetid, data);
		if (packetid == 1234)
		{
			TreeAttribute tree = new TreeAttribute();
			tree.FromBytes(data);
			Inventory.FromTreeAttributes(tree);
		}
	}

	public double NextRefreshTotalDays()
	{
		double lastRefreshTotalDays = WatchedAttributes.GetDouble("lastRefreshTotalDays", World.Calendar.TotalDays - 10.0);
		return doubleRefreshIntervalDays - (World.Calendar.TotalDays - lastRefreshTotalDays);
	}

	public override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (World.Side == EnumAppSide.Server && TradeProps != null && tickCount++ > 200)
		{
			double lastRefreshTotalDays = WatchedAttributes.GetDouble("lastRefreshTotalDays", World.Calendar.TotalDays - 10.0);
			int maxRefreshes = 10;
			while (World.Calendar.TotalDays - lastRefreshTotalDays > doubleRefreshIntervalDays && tradingWithPlayer == null && maxRefreshes-- > 0)
			{
				int traderAssets = Inventory.GetTraderAssets();
				double giveRel = 0.07 + World.Rand.NextDouble() * 0.21;
				float nowWealth = TradeProps.Money.nextFloat(1f, World.Rand);
				int toGive = (int)Math.Max(-3.0, Math.Min(nowWealth, (double)traderAssets + giveRel * (double)(int)nowWealth) - (double)traderAssets);
				Inventory.GiveToTrader(toGive);
				RefreshBuyingSellingInventory(0.5f);
				lastRefreshTotalDays += doubleRefreshIntervalDays;
				WatchedAttributes.SetDouble("lastRefreshTotalDays", lastRefreshTotalDays);
				tickCount = 1;
			}
			if (maxRefreshes <= 0)
			{
				WatchedAttributes.SetDouble("lastRefreshTotalDays", World.Calendar.TotalDays + 1.0 + World.Rand.NextDouble() * 5.0);
			}
		}
		if (tradingWithPlayer != null && (tradingWithPlayer.Pos.SquareDistanceTo(Pos) > 5f || Inventory.openedByPlayerGUIds.Count == 0 || !Alive))
		{
			dlg?.TryClose();
			IPlayer tradingPlayer = tradingWithPlayer?.Player;
			if (tradingPlayer != null)
			{
				Inventory.Close(tradingPlayer);
			}
		}
	}

	public override void FromBytes(BinaryReader reader, bool forClient)
	{
		base.FromBytes(reader, forClient);
		if (Inventory == null)
		{
			Inventory = new InventoryTrader("traderInv", EntityId.ToString() ?? "", null);
		}
		Inventory.FromTreeAttributes(GetOrCreateTradeStore());
	}

	public override void ToBytes(BinaryWriter writer, bool forClient)
	{
		Inventory.ToTreeAttributes(GetOrCreateTradeStore());
		base.ToBytes(writer, forClient);
	}

	private ITreeAttribute GetOrCreateTradeStore()
	{
		if (!WatchedAttributes.HasAttribute("traderInventory"))
		{
			ITreeAttribute tree = new TreeAttribute();
			Inventory.ToTreeAttributes(tree);
			WatchedAttributes["traderInventory"] = tree;
		}
		return WatchedAttributes["traderInventory"] as ITreeAttribute;
	}
}
