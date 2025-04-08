using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemAuction : ModSystem
{
	protected ICoreAPI api;

	protected ICoreServerAPI sapi;

	protected ICoreClientAPI capi;

	protected AuctionsData auctionsData = new AuctionsData();

	public Dictionary<string, InventoryGeneric> createAuctionSlotByPlayer = new Dictionary<string, InventoryGeneric>();

	public Action OnCellUpdateClient;

	public EntityTrader curTraderClient;

	public float debtClient;

	public float DeliveryPriceMul = 1f;

	public int DurationWeeksMul = 6;

	public float SalesCutRate = 0.1f;

	public ItemStack SingleCurrencyStack;

	public List<Auction> activeAuctions = new List<Auction>();

	public List<Auction> ownAuctions = new List<Auction>();

	protected OrderedDictionary<long, Auction> auctions => auctionsData.auctions;

	protected IServerNetworkChannel serverCh => sapi.Network.GetChannel("auctionHouse");

	protected IClientNetworkChannel clientCh => capi.Network.GetChannel("auctionHouse");

	private bool auctionHouseEnabled => sapi.World.Config.GetBool("auctionHouse", defaultValue: true);

	public int DeliveryCostsByDistance(Vec3d src, Vec3d dst)
	{
		return DeliveryCostsByDistance(src.DistanceTo(dst));
	}

	public int DeliveryCostsByDistance(double distance)
	{
		return (int)Math.Ceiling(3.5 * Math.Log((distance - 200.0) / 10000.0 + 1.0) * (double)DeliveryPriceMul);
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		this.api = api;
		api.Network.RegisterChannel("auctionHouse").RegisterMessageType<AuctionActionPacket>().RegisterMessageType<AuctionlistPacket>()
			.RegisterMessageType<AuctionActionResponsePacket>()
			.RegisterMessageType<DebtPacket>();
	}

	public void loadPricingConfig()
	{
		DeliveryPriceMul = api.World.Config.GetFloat("auctionHouseDeliveryPriceMul", 1f);
		DurationWeeksMul = api.World.Config.GetInt("auctionHouseDurationWeeksMul", 3);
		SalesCutRate = api.World.Config.GetFloat("auctionHouseSalesCutRate", 0.1f);
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		capi = api;
		clientCh.SetMessageHandler<AuctionlistPacket>(onAuctionList).SetMessageHandler<AuctionActionResponsePacket>(onAuctionActionResponse).SetMessageHandler<DebtPacket>(onDebtPkt);
		api.Event.BlockTexturesLoaded += Event_BlockTexturesLoaded;
	}

	private void onDebtPkt(DebtPacket pkt)
	{
		debtClient = pkt.TraderDebt;
	}

	private void Event_BlockTexturesLoaded()
	{
		Item item = capi.World.GetItem(new AssetLocation("gear-rusty"));
		if (item != null)
		{
			SingleCurrencyStack = new ItemStack(item);
		}
		loadPricingConfig();
	}

	private void onAuctionActionResponse(AuctionActionResponsePacket pkt)
	{
		if (pkt.ErrorCode != null)
		{
			capi.TriggerIngameError(this, pkt.ErrorCode, Lang.Get("auctionerror-" + pkt.ErrorCode));
			curTraderClient?.talkUtil.Talk(EnumTalkType.Complain);
			return;
		}
		if (pkt.Action == EnumAuctionAction.PurchaseAuction || (pkt.Action == EnumAuctionAction.RetrieveAuction && pkt.MoneyReceived))
		{
			capi.Gui.PlaySound(new AssetLocation("sounds/effect/cashregister"), randomizePitch: false, 0.25f);
		}
		curTraderClient?.talkUtil.Talk(EnumTalkType.Purchase);
	}

	private void onAuctionList(AuctionlistPacket pkt)
	{
		debtClient = pkt.TraderDebt;
		if (pkt.IsFullUpdate)
		{
			activeAuctions.Clear();
			ownAuctions.Clear();
			auctions.Clear();
		}
		if (pkt.NewAuctions != null)
		{
			Auction[] newAuctions = pkt.NewAuctions;
			foreach (Auction auction in newAuctions)
			{
				auctions[auction.AuctionId] = auction;
				auction.ItemStack.ResolveBlockOrItem(capi.World);
				if (auction.State == EnumAuctionState.Active || (auction.State == EnumAuctionState.Sold && auction.RetrievableTotalHours - capi.World.Calendar.TotalHours > 0.0))
				{
					insertOrUpdate(activeAuctions, auction);
				}
				else
				{
					remove(activeAuctions, auction);
				}
				if (auction.SellerUid == capi.World.Player.PlayerUID || (auction.State == EnumAuctionState.Sold && auction.BuyerUid == capi.World.Player.PlayerUID))
				{
					insertOrUpdate(ownAuctions, auction);
				}
				else
				{
					remove(ownAuctions, auction);
				}
			}
		}
		if (pkt.RemovedAuctions != null)
		{
			long[] removedAuctions = pkt.RemovedAuctions;
			foreach (long auctionId in removedAuctions)
			{
				auctions.Remove(auctionId);
				RemoveFromList(auctionId, activeAuctions);
				RemoveFromList(auctionId, ownAuctions);
			}
		}
		activeAuctions.Sort();
		ownAuctions.Sort();
		OnCellUpdateClient?.Invoke();
	}

	private void remove(List<Auction> auctions, Auction auction)
	{
		for (int i = 0; i < auctions.Count; i++)
		{
			if (auctions[i].AuctionId == auction.AuctionId)
			{
				auctions.RemoveAt(i);
				break;
			}
		}
	}

	private void insertOrUpdate(List<Auction> auctions, Auction auction)
	{
		bool found = false;
		int i = 0;
		while (!found && i < auctions.Count)
		{
			if (auctions[i].AuctionId == auction.AuctionId)
			{
				auctions[i] = auction;
				return;
			}
			i++;
		}
		auctions.Add(auction);
	}

	private void RemoveFromList(long auctionId, List<Auction> auctions)
	{
		for (int i = 0; i < auctions.Count; i++)
		{
			if (auctions[i].AuctionId == auctionId)
			{
				auctions.RemoveAt(i);
				i--;
			}
		}
	}

	public void DidEnterAuctionHouse()
	{
		clientCh.SendPacket(new AuctionActionPacket
		{
			Action = EnumAuctionAction.EnterAuctionHouse
		});
	}

	public void DidLeaveAuctionHouse()
	{
		clientCh.SendPacket(new AuctionActionPacket
		{
			Action = EnumAuctionAction.LeaveAuctionHouse
		});
	}

	public void PlaceAuctionClient(Entity traderEntity, int price, int durationWeeks = 1)
	{
		clientCh.SendPacket(new AuctionActionPacket
		{
			Action = EnumAuctionAction.PlaceAuction,
			AtAuctioneerEntityId = traderEntity.EntityId,
			Price = price,
			DurationWeeks = durationWeeks
		});
	}

	public void BuyAuctionClient(Entity traderEntity, long auctionId, bool withDelivery)
	{
		clientCh.SendPacket(new AuctionActionPacket
		{
			Action = EnumAuctionAction.PurchaseAuction,
			AtAuctioneerEntityId = traderEntity.EntityId,
			AuctionId = auctionId,
			WithDelivery = withDelivery
		});
	}

	public void RetrieveAuctionClient(Entity traderEntity, long auctionId)
	{
		clientCh.SendPacket(new AuctionActionPacket
		{
			Action = EnumAuctionAction.RetrieveAuction,
			AtAuctioneerEntityId = traderEntity.EntityId,
			AuctionId = auctionId
		});
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		api.Network.GetChannel("auctionHouse").SetMessageHandler<AuctionActionPacket>(onAuctionAction);
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.PlayerDisconnect += Event_PlayerDisconnect;
		api.Event.PlayerJoin += Event_PlayerJoin;
		api.Event.RegisterGameTickListener(TickAuctions, 5000);
	}

	private void Event_PlayerJoin(IServerPlayer byPlayer)
	{
		int expiredAuctions = 0;
		int activeAuctions = 0;
		int readyPurchasedAuctions = 0;
		int enroutePurchasedAuctions = 0;
		int soldAuctions = 0;
		foreach (Auction auction in auctionsData.auctions.Values)
		{
			if (auction.BuyerUid == byPlayer.PlayerUID)
			{
				if (auction.RetrievableTotalHours <= sapi.World.Calendar.TotalHours)
				{
					readyPurchasedAuctions++;
				}
				else
				{
					enroutePurchasedAuctions++;
				}
			}
			if (auction.SellerUid == byPlayer.PlayerUID)
			{
				if (auction.State == EnumAuctionState.Sold || auction.State == EnumAuctionState.SoldRetrieved)
				{
					soldAuctions++;
				}
				if (auction.State == EnumAuctionState.Expired)
				{
					expiredAuctions++;
				}
				if (auction.State == EnumAuctionState.Active)
				{
					activeAuctions++;
				}
			}
		}
		if (expiredAuctions + activeAuctions + readyPurchasedAuctions + enroutePurchasedAuctions + soldAuctions > 0)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(Lang.Get("Auction House: You have") + " ");
			if (activeAuctions > 0)
			{
				sb.AppendLine(Lang.Get("{0} active auctions", activeAuctions));
			}
			if (soldAuctions > 0)
			{
				sb.AppendLine(Lang.Get("{0} sold auctions", soldAuctions));
			}
			if (expiredAuctions > 0)
			{
				sb.AppendLine(Lang.Get("{0} expired auctions", expiredAuctions));
			}
			if (enroutePurchasedAuctions > 0)
			{
				sb.AppendLine(Lang.Get("{0} purchased auctions en-route", readyPurchasedAuctions));
			}
			if (readyPurchasedAuctions > 0)
			{
				sb.AppendLine(Lang.Get("{0} purchased auctions ready for pick-up", readyPurchasedAuctions));
			}
			byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, sb.ToString(), EnumChatType.Notification);
		}
	}

	private void Event_PlayerDisconnect(IServerPlayer byPlayer)
	{
		if (createAuctionSlotByPlayer.TryGetValue(byPlayer.PlayerUID, out var _))
		{
			byPlayer.InventoryManager.CloseInventory(createAuctionSlotByPlayer[byPlayer.PlayerUID]);
			createAuctionSlotByPlayer.Remove(byPlayer.PlayerUID);
		}
	}

	private void onAuctionAction(IServerPlayer fromPlayer, AuctionActionPacket pkt)
	{
		if (!auctionHouseEnabled)
		{
			return;
		}
		switch (pkt.Action)
		{
		case EnumAuctionAction.EnterAuctionHouse:
			if (!createAuctionSlotByPlayer.ContainsKey(fromPlayer.PlayerUID))
			{
				InventoryGeneric inventoryGeneric2 = (createAuctionSlotByPlayer[fromPlayer.PlayerUID] = new InventoryGeneric(1, "auctionslot-" + fromPlayer.PlayerUID, sapi));
				InventoryGeneric ainv = inventoryGeneric2;
				ainv.OnGetSuitability = (ItemSlot s, ItemSlot t, bool isMerge) => -1f;
				ainv.OnInventoryClosed += delegate(IPlayer plr)
				{
					ainv.DropAll(plr.Entity.Pos.XYZ);
				};
			}
			fromPlayer.InventoryManager.OpenInventory(createAuctionSlotByPlayer[fromPlayer.PlayerUID]);
			sendAuctions(auctions.Values, null, isFullUpdate: true, fromPlayer);
			break;
		case EnumAuctionAction.LeaveAuctionHouse:
			Event_PlayerDisconnect(fromPlayer);
			break;
		case EnumAuctionAction.PurchaseAuction:
		{
			Entity auctioneerEntity2 = sapi.World.GetEntityById(pkt.AtAuctioneerEntityId);
			PurchaseAuction(pkt.AuctionId, fromPlayer.Entity, auctioneerEntity2, pkt.WithDelivery, out var failureCode3);
			serverCh.SendPacket(new AuctionActionResponsePacket
			{
				Action = pkt.Action,
				AuctionId = pkt.AuctionId,
				ErrorCode = failureCode3
			}, fromPlayer);
			break;
		}
		case EnumAuctionAction.RetrieveAuction:
		{
			string failureCode2;
			ItemStack stack = RetrieveAuction(pkt.AuctionId, pkt.AtAuctioneerEntityId, fromPlayer.Entity, out failureCode2);
			if (stack != null)
			{
				if (!fromPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
				{
					sapi.World.SpawnItemEntity(stack, fromPlayer.Entity.Pos.XYZ);
				}
				sapi.World.Logger.Audit("{0} Got 1x{1} from Auction at {2}.", fromPlayer.PlayerName, stack.Collectible.Code, fromPlayer.Entity.Pos);
			}
			serverCh.SendPacket(new AuctionActionResponsePacket
			{
				Action = pkt.Action,
				AuctionId = pkt.AuctionId,
				ErrorCode = failureCode2,
				MoneyReceived = (stack != null && (stack.Collectible.Attributes?["currency"].Exists).GetValueOrDefault())
			}, fromPlayer);
			break;
		}
		case EnumAuctionAction.PlaceAuction:
		{
			if (!createAuctionSlotByPlayer.TryGetValue(fromPlayer.PlayerUID, out var inv))
			{
				break;
			}
			if (inv.Empty)
			{
				serverCh.SendPacket(new AuctionActionResponsePacket
				{
					Action = pkt.Action,
					AuctionId = pkt.AuctionId,
					ErrorCode = "emptyauctionslot"
				}, fromPlayer);
				break;
			}
			pkt.DurationWeeks = Math.Max(1, pkt.DurationWeeks);
			Entity auctioneerEntity = sapi.World.GetEntityById(pkt.AtAuctioneerEntityId);
			PlaceAuction(inv[0], inv[0].StackSize, pkt.Price, pkt.DurationWeeks * 7 * 24, pkt.DurationWeeks / DurationWeeksMul, fromPlayer.Entity, auctioneerEntity, out var failureCode);
			if (failureCode != null)
			{
				inv.DropAll(fromPlayer.Entity.Pos.XYZ);
			}
			float debt = 0f;
			auctionsData.DebtToTraderByPlayer.TryGetValue(fromPlayer.PlayerUID, out debt);
			serverCh.SendPacket(new AuctionActionResponsePacket
			{
				Action = pkt.Action,
				AuctionId = pkt.AuctionId,
				ErrorCode = failureCode
			}, fromPlayer);
			serverCh.SendPacket(new DebtPacket
			{
				TraderDebt = debt
			}, fromPlayer);
			break;
		}
		}
	}

	public List<Auction> GetActiveAuctions()
	{
		return auctions.Values.Where((Auction ac) => ac.State == EnumAuctionState.Active || ac.State == EnumAuctionState.Sold).ToList();
	}

	public List<Auction> GetAuctionsFrom(IPlayer player)
	{
		List<Auction> auctions = new List<Auction>();
		foreach (Auction auction in this.auctions.Values)
		{
			if (auction.SellerName == player.PlayerUID)
			{
				auctions.Add(auction);
			}
		}
		return auctions;
	}

	private void TickAuctions(float dt)
	{
		double totalhours = sapi.World.Calendar.TotalHours;
		Auction[] array = auctions.Values.ToArray();
		List<Auction> toSendAuctions = new List<Auction>();
		Auction[] array2 = array;
		foreach (Auction auction in array2)
		{
			if (auction.State == EnumAuctionState.Active && auction.ExpireTotalHours < totalhours)
			{
				auction.State = EnumAuctionState.Expired;
				toSendAuctions.Add(auction);
			}
		}
		sendAuctions(toSendAuctions, null);
	}

	public virtual int GetDepositCost(ItemSlot forItem)
	{
		return 1;
	}

	public void PlaceAuction(ItemSlot slot, int quantity, int price, double durationHours, int depositCost, EntityAgent sellerEntity, Entity auctioneerEntity, out string failureCode)
	{
		if (slot.StackSize < quantity)
		{
			failureCode = "notenoughitems";
			return;
		}
		if (GetAuctionsFrom((sellerEntity as EntityPlayer).Player).Count > 30)
		{
			failureCode = "toomanyauctions";
			return;
		}
		if (InventoryTrader.GetPlayerAssets(sellerEntity) < GetDepositCost(slot) * depositCost)
		{
			failureCode = "notenoughgears";
			return;
		}
		if (price < 1)
		{
			failureCode = "atleast1gear";
			return;
		}
		failureCode = null;
		InventoryTrader.DeductFromEntity(sapi, sellerEntity, depositCost);
		(auctioneerEntity as EntityTrader).Inventory?.GiveToTrader(depositCost);
		long id = ++auctionsData.nextAuctionId;
		string sellerName = sellerEntity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName;
		if (sellerName == null)
		{
			sellerName = sellerEntity.Properties.Code.ToShortString();
		}
		string uid = (sellerEntity as EntityPlayer)?.PlayerUID ?? "";
		auctionsData.DebtToTraderByPlayer.TryGetValue(uid, out var debt);
		float traderCutGears = (float)price * SalesCutRate + debt;
		auctionsData.DebtToTraderByPlayer[uid] = traderCutGears - (float)(int)traderCutGears;
		Auction auction = new Auction
		{
			AuctionId = id,
			ExpireTotalHours = sapi.World.Calendar.TotalHours + durationHours,
			ItemStack = slot.TakeOut(quantity),
			PostedTotalHours = sapi.World.Calendar.TotalHours,
			Price = price,
			TraderCut = (int)traderCutGears,
			SellerName = sellerName,
			SellerUid = (sellerEntity as EntityPlayer)?.PlayerUID,
			SellerEntityId = sellerEntity.EntityId,
			SrcAuctioneerEntityPos = auctioneerEntity.Pos.XYZ,
			SrcAuctioneerEntityId = auctioneerEntity.EntityId
		};
		auctions.Add(id, auction);
		slot.MarkDirty();
		sendAuctions(new Auction[1] { auction }, null);
	}

	public void PurchaseAuction(long auctionId, EntityAgent buyerEntity, Entity auctioneerEntity, bool withDelivery, out string failureCode)
	{
		if (auctions.TryGetValue(auctionId, out var auction))
		{
			if ((buyerEntity as EntityPlayer)?.PlayerUID == auction.SellerUid)
			{
				failureCode = "ownauction";
				return;
			}
			if (auction.BuyerName != null)
			{
				failureCode = "alreadypurchased";
				return;
			}
			int playerAssets = InventoryTrader.GetPlayerAssets(buyerEntity);
			int deliveryCosts = (withDelivery ? DeliveryCostsByDistance(auctioneerEntity.Pos.XYZ, auction.SrcAuctioneerEntityPos) : 0);
			int totalcost = auction.Price + deliveryCosts;
			if (playerAssets < totalcost)
			{
				failureCode = "notenoughgears";
				return;
			}
			InventoryTrader.DeductFromEntity(sapi, buyerEntity, totalcost);
			(auctioneerEntity as EntityTrader).Inventory?.GiveToTrader((int)((float)auction.Price * SalesCutRate + (float)deliveryCosts));
			string buyerName = buyerEntity.GetBehavior<EntityBehaviorNameTag>()?.DisplayName;
			if (buyerName == null)
			{
				buyerName = buyerEntity.Properties.Code.ToShortString();
			}
			auction.BuyerName = buyerName;
			auction.WithDelivery = withDelivery;
			auction.BuyerUid = (buyerEntity as EntityPlayer)?.PlayerUID;
			auction.RetrievableTotalHours = sapi.World.Calendar.TotalHours + 1.0 + (double)(3 * deliveryCosts);
			auction.DstAuctioneerEntityId = (withDelivery ? auctioneerEntity.EntityId : auction.SrcAuctioneerEntityId);
			auction.DstAuctioneerEntityPos = (withDelivery ? auctioneerEntity.Pos.XYZ : auction.SrcAuctioneerEntityPos);
			auction.State = EnumAuctionState.Sold;
			sendAuctions(new Auction[1] { auction }, null);
			failureCode = null;
		}
		else
		{
			failureCode = "nosuchauction";
		}
	}

	public void DeleteActiveAuction(long auctionId)
	{
		auctions.Remove(auctionId);
		sendAuctions(null, new long[1] { auctionId });
	}

	public ItemStack RetrieveAuction(long auctionId, long atAuctioneerEntityId, EntityPlayer reqEntity, out string failureCode)
	{
		if (!auctions.TryGetValue(auctionId, out var auction))
		{
			failureCode = "nosuchauction";
			return null;
		}
		if (reqEntity.PlayerUID == auction.BuyerUid)
		{
			if (auction.RetrievableTotalHours > sapi.World.Calendar.TotalHours)
			{
				failureCode = "notyetretrievable";
				return null;
			}
			if (auction.State == EnumAuctionState.SoldRetrieved)
			{
				failureCode = "alreadyretrieved";
				return null;
			}
			if (auction.State == EnumAuctionState.Expired || auction.State == EnumAuctionState.Active)
			{
				sapi.Logger.Notification("Auction was bought by {0}, but is in state {1}? O.o Setting it to sold state.");
				auction.State = EnumAuctionState.Sold;
				auction.RetrievableTotalHours = sapi.World.Calendar.TotalHours + 6.0;
				failureCode = null;
				sendAuctions(new Auction[1] { auction }, null);
				return null;
			}
			if (!auction.WithDelivery && auction.SrcAuctioneerEntityId != atAuctioneerEntityId && auction.SrcAuctioneerEntityPos.DistanceTo(reqEntity.Pos.XYZ) > 100f)
			{
				failureCode = "wrongtrader";
				return null;
			}
			auction.State = EnumAuctionState.SoldRetrieved;
			if (auction.MoneyCollected)
			{
				auctions.Remove(auctionId);
				sendAuctions(null, new long[1] { auctionId });
			}
			else
			{
				sendAuctions(new Auction[1] { auction }, null);
			}
			failureCode = null;
			return auction.ItemStack.Clone();
		}
		if (reqEntity.PlayerUID == auction.SellerUid)
		{
			if (auction.State == EnumAuctionState.Active)
			{
				auction.State = EnumAuctionState.Expired;
				auction.RetrievableTotalHours = sapi.World.Calendar.TotalHours + 6.0;
				failureCode = null;
				sendAuctions(new Auction[1] { auction }, null);
				return null;
			}
			if (auction.RetrievableTotalHours > sapi.World.Calendar.TotalHours)
			{
				failureCode = "notyetretrievable";
				return null;
			}
			if (auction.State == EnumAuctionState.Expired)
			{
				auctions.Remove(auctionId);
				sendAuctions(null, new long[1] { auctionId });
				failureCode = null;
				return auction.ItemStack;
			}
			if (auction.State == EnumAuctionState.Sold || auction.State == EnumAuctionState.SoldRetrieved)
			{
				if (auction.MoneyCollected)
				{
					failureCode = "moneyalreadycollected";
					return null;
				}
				if (auction.State == EnumAuctionState.SoldRetrieved)
				{
					auctions.Remove(auctionId);
					sendAuctions(null, new long[1] { auctionId });
				}
				else
				{
					sendAuctions(new Auction[1] { auction }, null);
				}
				failureCode = null;
				auction.MoneyCollected = true;
				ItemStack itemStack = SingleCurrencyStack.Clone();
				itemStack.StackSize = auction.Price - auction.TraderCut;
				return itemStack;
			}
			failureCode = "codingerror";
			return null;
		}
		failureCode = "notyouritem";
		return null;
	}

	private void sendAuctions(IEnumerable<Auction> newauctions, long[] removedauctions, bool isFullUpdate = false, IServerPlayer toPlayer = null)
	{
		Auction[] newauctionsa = newauctions?.ToArray();
		if ((newauctionsa == null || newauctionsa.Length == 0) && (removedauctions == null || removedauctions.Length == 0) && !isFullUpdate)
		{
			return;
		}
		float debt = 0f;
		if (toPlayer != null)
		{
			auctionsData.DebtToTraderByPlayer.TryGetValue(toPlayer.PlayerUID, out debt);
		}
		AuctionlistPacket pkt = new AuctionlistPacket
		{
			NewAuctions = newauctionsa,
			RemovedAuctions = removedauctions,
			IsFullUpdate = isFullUpdate,
			TraderDebt = debt
		};
		if (toPlayer != null)
		{
			sapi.Network.GetChannel("auctionHouse").SendPacket(pkt, toPlayer);
			return;
		}
		foreach (string playeruid in createAuctionSlotByPlayer.Keys)
		{
			if (sapi.World.PlayerByUid(playeruid) is IServerPlayer plr)
			{
				sapi.Network.GetChannel("auctionHouse").SendPacket(pkt, plr);
			}
		}
	}

	private void Event_GameWorldSave()
	{
		sapi.WorldManager.SaveGame.StoreData("auctionsData", SerializerUtil.Serialize(auctionsData));
	}

	private void Event_SaveGameLoaded()
	{
		Item item = sapi.World.GetItem(new AssetLocation("gear-rusty"));
		if (item == null)
		{
			return;
		}
		SingleCurrencyStack = new ItemStack(item);
		byte[] data = sapi.WorldManager.SaveGame.GetData("auctionsData");
		if (data != null)
		{
			auctionsData = SerializerUtil.Deserialize<AuctionsData>(data);
			foreach (Auction value in auctionsData.auctions.Values)
			{
				value.ItemStack?.ResolveBlockOrItem(sapi.World);
			}
		}
		loadPricingConfig();
	}
}
