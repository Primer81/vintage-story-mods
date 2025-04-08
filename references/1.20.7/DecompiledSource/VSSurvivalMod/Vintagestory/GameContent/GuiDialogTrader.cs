using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogTrader : GuiDialog
{
	private InventoryTrader traderInventory;

	private EntityAgent owningEntity;

	private double prevPlrAbsFixedX;

	private double prevPlrAbsFixedY;

	private double prevTdrAbsFixedX;

	private double prevTdrAbsFixedY;

	private double notifyPlayerMoneyTextSeconds;

	private double notifyTraderMoneyTextSeconds;

	private int rows = 4;

	private int cols = 4;

	private int curTab;

	private ModSystemAuction auctionSys;

	private InventoryGeneric auctionSlotInv;

	private GuiElementCellList<Auction> listElem;

	private List<Auction> auctions;

	private ElementBounds clipBounds;

	private AuctionCellEntry selectedElem;

	public override string ToggleKeyCombinationCode => null;

	private bool auctionHouseEnabled => capi.World.Config.GetBool("auctionHouse", defaultValue: true);

	public override bool PrefersUngrabbedMouse => false;

	public override float ZSize => 300f;

	public GuiDialogTrader(InventoryTrader traderInventory, EntityAgent owningEntity, ICoreClientAPI capi, int rows = 4, int cols = 4)
		: base(capi)
	{
		auctionSys = capi.ModLoader.GetModSystem<ModSystemAuction>();
		auctionSys.OnCellUpdateClient = delegate
		{
			listElem?.ReloadCells(auctions);
			updateScrollbarBounds();
		};
		auctionSys.curTraderClient = owningEntity as EntityTrader;
		this.traderInventory = traderInventory;
		this.owningEntity = owningEntity;
		this.rows = rows;
		this.cols = cols;
		if (!auctionSys.createAuctionSlotByPlayer.TryGetValue(capi.World.Player.PlayerUID, out auctionSlotInv))
		{
			auctionSys.createAuctionSlotByPlayer[capi.World.Player.PlayerUID] = (auctionSlotInv = new InventoryGeneric(1, "auctionslot-" + capi.World.Player.PlayerUID, capi));
			auctionSlotInv.OnGetSuitability = (ItemSlot s, ItemSlot t, bool isMerge) => -1f;
		}
		capi.Network.SendPacketClient(capi.World.Player.InventoryManager.OpenInventory(auctionSlotInv));
		Compose();
	}

	public void Compose()
	{
		GuiTab[] tabs = new GuiTab[3]
		{
			new GuiTab
			{
				Name = Lang.Get("Local goods"),
				DataInt = 0
			},
			new GuiTab
			{
				Name = Lang.Get("Auction house"),
				DataInt = 1
			},
			new GuiTab
			{
				Name = Lang.Get("Your Auctions"),
				DataInt = 2
			}
		};
		ElementBounds tabBounds = ElementBounds.Fixed(0.0, -24.0, 500.0, 25.0);
		CairoFont tabFont = CairoFont.WhiteDetailText();
		if (!auctionHouseEnabled)
		{
			tabs = new GuiTab[1]
			{
				new GuiTab
				{
					Name = Lang.Get("Local goods"),
					DataInt = 0
				}
			};
		}
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		ElementBounds leftButton = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		ElementBounds rightButton = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
		string traderName = owningEntity.GetBehavior<EntityBehaviorNameTag>().DisplayName;
		string dlgTitle = Lang.Get("tradingwindow-" + owningEntity.Code.Path, traderName);
		if (curTab > 0)
		{
			dlgTitle = Lang.Get("tradertabtitle-" + curTab);
		}
		base.SingleComposer = capi.Gui.CreateCompo("traderdialog-" + owningEntity.EntityId, dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(dlgTitle, OnTitleBarClose)
			.AddHorizontalTabs(tabs, tabBounds, OnTabClicked, tabFont, tabFont.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
			.BeginChildElements(bgBounds);
		base.SingleComposer.GetHorizontalTabs("tabs").activeElement = curTab;
		if (curTab == 0)
		{
			double pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
			ElementBounds leftTopSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, 70.0 + pad, cols, rows).FixedGrow(2.0 * pad, 2.0 * pad);
			ElementBounds rightTopSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad + leftTopSlotBounds.fixedWidth + 20.0, 70.0 + pad, cols, rows).FixedGrow(2.0 * pad, 2.0 * pad);
			ElementBounds rightBotSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad + leftTopSlotBounds.fixedWidth + 20.0, 15.0 + pad, cols, 1).FixedGrow(2.0 * pad, 2.0 * pad).FixedUnder(rightTopSlotBounds, 5.0);
			ElementBounds leftBotSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, 15.0 + pad, cols, 1).FixedGrow(2.0 * pad, 2.0 * pad).FixedUnder(leftTopSlotBounds, 5.0);
			ElementBounds costTextBounds = ElementBounds.Fixed(pad, 85.0 + 2.0 * pad + leftTopSlotBounds.fixedHeight + leftBotSlotBounds.fixedHeight, 200.0, 25.0);
			ElementBounds offerTextBounds = ElementBounds.Fixed(leftTopSlotBounds.fixedWidth + pad + 20.0, 85.0 + 2.0 * pad + leftTopSlotBounds.fixedHeight + leftBotSlotBounds.fixedHeight, 200.0, 25.0);
			ElementBounds traderMoneyBounds = offerTextBounds.FlatCopy().WithFixedOffset(0.0, offerTextBounds.fixedHeight);
			ElementBounds playerMoneyBounds = costTextBounds.FlatCopy().WithFixedOffset(0.0, costTextBounds.fixedHeight);
			double daysLeft = (owningEntity as EntityTradingHumanoid).NextRefreshTotalDays();
			string daysLeftString = ((daysLeft < 1.0) ? Lang.Get("Delievery of new goods in less than 1 day") : Lang.Get("Delievery of new goods in {0} days", (int)daysLeft));
			CairoFont deliveryTextFont = CairoFont.WhiteDetailText();
			deliveryTextFont.Color[3] *= 0.7;
			base.SingleComposer.AddStaticText(daysLeftString, deliveryTextFont, ElementBounds.Fixed(pad, 20.0 + pad, 430.0, 25.0)).AddStaticText(Lang.Get("You can Buy"), CairoFont.WhiteDetailText(), ElementBounds.Fixed(pad, 50.0 + pad, 200.0, 25.0)).AddStaticText(Lang.Get("You can Sell"), CairoFont.WhiteDetailText(), ElementBounds.Fixed(leftTopSlotBounds.fixedWidth + pad + 20.0, 50.0 + pad, 200.0, 25.0))
				.AddItemSlotGrid(traderInventory, DoSendPacket, cols, new int[rows * cols].Fill((int i) => i), leftTopSlotBounds, "traderSellingSlots")
				.AddItemSlotGrid(traderInventory, DoSendPacket, cols, new int[cols].Fill((int i) => rows * cols + i), leftBotSlotBounds, "playerBuyingSlots")
				.AddItemSlotGrid(traderInventory, DoSendPacket, cols, new int[rows * cols].Fill((int i) => rows * cols + cols + i), rightTopSlotBounds, "traderBuyingSlots")
				.AddItemSlotGrid(traderInventory, DoSendPacket, cols, new int[cols].Fill((int i) => rows * cols + cols + rows * cols + i), rightBotSlotBounds, "playerSellingSlots")
				.AddStaticText(Lang.Get("trader-yourselection"), CairoFont.WhiteDetailText(), ElementBounds.Fixed(pad, 70.0 + 2.0 * pad + leftTopSlotBounds.fixedHeight, 150.0, 25.0))
				.AddStaticText(Lang.Get("trader-youroffer"), CairoFont.WhiteDetailText(), ElementBounds.Fixed(leftTopSlotBounds.fixedWidth + pad + 20.0, 70.0 + 2.0 * pad + leftTopSlotBounds.fixedHeight, 150.0, 25.0))
				.AddDynamicText("", CairoFont.WhiteDetailText(), costTextBounds, "costText")
				.AddDynamicText("", CairoFont.WhiteDetailText(), playerMoneyBounds, "playerMoneyText")
				.AddDynamicText("", CairoFont.WhiteDetailText(), offerTextBounds, "gainText")
				.AddDynamicText("", CairoFont.WhiteDetailText(), traderMoneyBounds, "traderMoneyText")
				.AddSmallButton(Lang.Get("Goodbye!"), OnByeClicked, leftButton.FixedUnder(playerMoneyBounds, 20.0))
				.AddSmallButton(Lang.Get("Buy / Sell"), OnBuySellClicked, rightButton.FixedUnder(traderMoneyBounds, 20.0), EnumButtonStyle.Normal, "buysellButton")
				.EndChildElements()
				.Compose();
			base.SingleComposer.GetButton("buysellButton").Enabled = false;
			CalcAndUpdateAssetsDisplay();
			return;
		}
		double listHeight = 377.0;
		ElementBounds stackListBounds = ElementBounds.Fixed(0.0, 25.0, 700.0, listHeight);
		clipBounds = stackListBounds.ForkBoundingParent();
		ElementBounds insetBounds = stackListBounds.FlatCopy().FixedGrow(3.0).WithFixedOffset(0.0, 0.0);
		ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(3.0 + stackListBounds.fixedWidth + 7.0).WithFixedWidth(20.0);
		if (curTab == 1)
		{
			auctions = auctionSys.activeAuctions;
			base.SingleComposer.BeginClip(clipBounds).AddInset(insetBounds, 3).AddCellList(stackListBounds, createCell, auctionSys.activeAuctions, "stacklist")
				.EndClip()
				.AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
				.AddSmallButton(Lang.Get("Goodbye!"), OnByeClicked, leftButton.FixedUnder(clipBounds, 20.0))
				.AddSmallButton(Lang.Get("Buy"), OnBuyAuctionClicked, rightButton.FixedUnder(clipBounds, 20.0), EnumButtonStyle.Normal, "buyauction");
		}
		if (curTab == 2)
		{
			auctions = auctionSys.ownAuctions;
			ElementBounds button = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0);
			string placeStr = Lang.Get("Place Auction");
			string cancelStr = Lang.Get("Cancel Auction");
			double placelen = CairoFont.ButtonText().GetTextExtents(placeStr).Width / (double)RuntimeEnv.GUIScale;
			_ = CairoFont.ButtonText().GetTextExtents(cancelStr).Width / (double)RuntimeEnv.GUIScale;
			base.SingleComposer.BeginClip(clipBounds).AddInset(insetBounds, 3).AddCellList(stackListBounds, createCell, auctionSys.ownAuctions, "stacklist")
				.EndClip()
				.AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds, "scrollbar")
				.AddSmallButton(Lang.Get("Goodbye!"), OnByeClicked, leftButton.FixedUnder(clipBounds, 20.0))
				.AddSmallButton(Lang.Get("Place Auction"), OnCreateAuction, rightButton.FixedUnder(clipBounds, 20.0), EnumButtonStyle.Normal, "placeAuction")
				.AddSmallButton(cancelStr, OnCancelAuction, button.FlatCopy().FixedUnder(clipBounds, 20.0).WithFixedAlignmentOffset(0.0 - placelen, 0.0), EnumButtonStyle.Normal, "cancelAuction")
				.AddSmallButton(Lang.Get("Collect Funds"), OnCollectFunds, button.FlatCopy().FixedUnder(clipBounds, 20.0).WithFixedAlignmentOffset(0.0 - placelen, 0.0), EnumButtonStyle.Normal, "collectFunds")
				.AddSmallButton(Lang.Get("Retrieve Items"), OnRetrieveItems, button.FixedUnder(clipBounds, 20.0).WithFixedAlignmentOffset(0.0 - placelen, 0.0), EnumButtonStyle.Normal, "retrieveItems");
		}
		if (curTab == 1 || curTab == 2)
		{
			selectedElem = null;
			listElem = base.SingleComposer.GetCellList<Auction>("stacklist");
			listElem.BeforeCalcBounds();
			listElem.UnscaledCellVerPadding = 0;
			listElem.unscaledCellSpacing = 5;
			base.SingleComposer.EndChildElements().Compose();
			updateScrollbarBounds();
			didClickAuctionElem(-1);
		}
	}

	private void updateScrollbarBounds()
	{
		if (listElem != null)
		{
			base.SingleComposer.GetScrollbar("scrollbar")?.Bounds.CalcWorldBounds();
			base.SingleComposer.GetScrollbar("scrollbar")?.SetHeights((float)clipBounds.fixedHeight, (float)listElem.Bounds.fixedHeight);
		}
	}

	private void OnNewScrollbarValue(float value)
	{
		listElem = base.SingleComposer.GetCellList<Auction>("stacklist");
		listElem.Bounds.fixedY = 0f - value;
		listElem.Bounds.CalcWorldBounds();
	}

	private bool OnCancelAuction()
	{
		if (selectedElem?.auction == null)
		{
			return false;
		}
		auctionSys.RetrieveAuctionClient(owningEntity, selectedElem.auction.AuctionId);
		return true;
	}

	private bool OnBuyAuctionClicked()
	{
		if (selectedElem?.auction == null)
		{
			return false;
		}
		object odlg = capi.OpenedGuis.FirstOrDefault((object d) => d is GuiDialogConfirmPurchase);
		if (odlg != null)
		{
			(odlg as GuiDialog).Focus();
			return true;
		}
		new GuiDialogConfirmPurchase(capi, capi.World.Player.Entity, owningEntity, selectedElem.auction).TryOpen();
		return true;
	}

	private bool OnCollectFunds()
	{
		if (selectedElem?.auction == null)
		{
			return false;
		}
		auctionSys.RetrieveAuctionClient(owningEntity, selectedElem.auction.AuctionId);
		return true;
	}

	private bool OnRetrieveItems()
	{
		if (selectedElem?.auction == null)
		{
			return false;
		}
		auctionSys.RetrieveAuctionClient(owningEntity, selectedElem.auction.AuctionId);
		return true;
	}

	private IGuiElementCell createCell(Auction auction, ElementBounds bounds)
	{
		bounds.fixedPaddingY = 0.0;
		return new AuctionCellEntry(capi, auctionSlotInv, bounds, auction, didClickAuctionElem);
	}

	private void didClickAuctionElem(int index)
	{
		if (selectedElem != null)
		{
			selectedElem.Selected = false;
		}
		if (index >= 0)
		{
			selectedElem = base.SingleComposer.GetCellList<Auction>("stacklist").elementCells[index] as AuctionCellEntry;
			selectedElem.Selected = true;
		}
		if (curTab == 2)
		{
			Auction auction = selectedElem?.auction;
			bool sold = (auction != null && auction.State == EnumAuctionState.Sold) || (auction != null && auction.State == EnumAuctionState.SoldRetrieved);
			base.SingleComposer.GetButton("cancelAuction").Visible = auction != null && auction.State == EnumAuctionState.Active;
			base.SingleComposer.GetButton("retrieveItems").Visible = (auction != null && auction.State == EnumAuctionState.Expired) || (sold && auction.SellerUid != capi.World.Player.PlayerUID);
			base.SingleComposer.GetButton("collectFunds").Visible = sold && auction.SellerUid == capi.World.Player.PlayerUID;
		}
	}

	private bool OnCreateAuction()
	{
		object odlg = capi.OpenedGuis.FirstOrDefault((object d) => d is GuiDialogCreateAuction);
		if (odlg != null)
		{
			(odlg as GuiDialog).Focus();
			return true;
		}
		new GuiDialogCreateAuction(capi, owningEntity, auctionSlotInv).TryOpen();
		return true;
	}

	private void OnTabClicked(int tab)
	{
		curTab = tab;
		Compose();
	}

	private void CalcAndUpdateAssetsDisplay()
	{
		int playerAssets = InventoryTrader.GetPlayerAssets(capi.World.Player.Entity);
		base.SingleComposer.GetDynamicText("playerMoneyText")?.SetNewText(Lang.Get("You have {0} Gears", playerAssets));
		int traderAssets = traderInventory.GetTraderAssets();
		base.SingleComposer.GetDynamicText("traderMoneyText")?.SetNewText(Lang.Get("{0} has {1} Gears", owningEntity.GetBehavior<EntityBehaviorNameTag>().DisplayName, traderAssets));
	}

	private void TraderInventory_SlotModified(int slotid)
	{
		int totalCost = traderInventory.GetTotalCost();
		int totalGain = traderInventory.GetTotalGain();
		base.SingleComposer.GetDynamicText("costText")?.SetNewText((totalCost > 0) ? Lang.Get("Total Cost: {0} Gears", totalCost) : "");
		base.SingleComposer.GetDynamicText("gainText")?.SetNewText((totalGain > 0) ? Lang.Get("Total Gain: {0} Gears", totalGain) : "");
		if (base.SingleComposer.GetButton("buysellButton") != null)
		{
			base.SingleComposer.GetButton("buysellButton").Enabled = totalCost > 0 || totalGain > 0;
			CalcAndUpdateAssetsDisplay();
		}
	}

	private bool OnBuySellClicked()
	{
		EnumTransactionResult num = traderInventory.TryBuySell(capi.World.Player);
		if (num == EnumTransactionResult.Success)
		{
			capi.Gui.PlaySound(new AssetLocation("sounds/effect/cashregister"), randomizePitch: false, 0.25f);
			(owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Purchase);
		}
		if (num == EnumTransactionResult.PlayerNotEnoughAssets)
		{
			(owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Complain);
			if (notifyPlayerMoneyTextSeconds <= 0.0)
			{
				prevPlrAbsFixedX = base.SingleComposer.GetDynamicText("playerMoneyText").Bounds.absFixedX;
				prevPlrAbsFixedY = base.SingleComposer.GetDynamicText("playerMoneyText").Bounds.absFixedY;
			}
			notifyPlayerMoneyTextSeconds = 1.5;
		}
		if (num == EnumTransactionResult.TraderNotEnoughAssets)
		{
			(owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Complain);
			if (notifyTraderMoneyTextSeconds <= 0.0)
			{
				prevTdrAbsFixedX = base.SingleComposer.GetDynamicText("traderMoneyText").Bounds.absFixedX;
				prevTdrAbsFixedY = base.SingleComposer.GetDynamicText("traderMoneyText").Bounds.absFixedY;
			}
			notifyTraderMoneyTextSeconds = 1.5;
		}
		if (num == EnumTransactionResult.TraderNotEnoughSupplyOrDemand)
		{
			(owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Complain);
		}
		capi.Network.SendEntityPacket(owningEntity.EntityId, 1000);
		TraderInventory_SlotModified(0);
		CalcAndUpdateAssetsDisplay();
		return true;
	}

	private bool OnByeClicked()
	{
		TryClose();
		return true;
	}

	private void DoSendPacket(object p)
	{
		capi.Network.SendEntityPacket(owningEntity.EntityId, p);
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		traderInventory.SlotModified += TraderInventory_SlotModified;
		auctionSys.DidEnterAuctionHouse();
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		traderInventory.SlotModified -= TraderInventory_SlotModified;
		(owningEntity as EntityTradingHumanoid).TalkUtil?.Talk(EnumTalkType.Goodbye);
		capi.Network.SendPacketClient(capi.World.Player.InventoryManager.CloseInventory(traderInventory));
		base.SingleComposer.GetSlotGrid("traderSellingSlots")?.OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("playerBuyingSlots")?.OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("traderBuyingSlots")?.OnGuiClosed(capi);
		base.SingleComposer.GetSlotGrid("playerSellingSlots")?.OnGuiClosed(capi);
		auctionSlotInv[0].Itemstack = null;
		capi.World.Player.InventoryManager.CloseInventory(auctionSlotInv);
		auctionSys.DidLeaveAuctionHouse();
	}

	public override void OnBeforeRenderFrame3D(float deltaTime)
	{
		base.OnBeforeRenderFrame3D(deltaTime);
		if (notifyPlayerMoneyTextSeconds > 0.0)
		{
			notifyPlayerMoneyTextSeconds -= deltaTime;
			GuiElementDynamicText elem2 = base.SingleComposer.GetDynamicText("playerMoneyText");
			if (elem2 != null)
			{
				if (notifyPlayerMoneyTextSeconds <= 0.0)
				{
					elem2.Bounds.absFixedX = prevPlrAbsFixedX;
					elem2.Bounds.absFixedY = prevPlrAbsFixedY;
				}
				else
				{
					elem2.Bounds.absFixedX = prevPlrAbsFixedX + notifyPlayerMoneyTextSeconds * (capi.World.Rand.NextDouble() * 4.0 - 2.0);
					elem2.Bounds.absFixedY = prevPlrAbsFixedY + notifyPlayerMoneyTextSeconds * (capi.World.Rand.NextDouble() * 4.0 - 2.0);
				}
			}
		}
		if (!(notifyTraderMoneyTextSeconds > 0.0))
		{
			return;
		}
		notifyTraderMoneyTextSeconds -= deltaTime;
		GuiElementDynamicText elem = base.SingleComposer.GetDynamicText("traderMoneyText");
		if (elem != null)
		{
			if (notifyTraderMoneyTextSeconds <= 0.0)
			{
				elem.Bounds.absFixedX = prevPlrAbsFixedX;
				elem.Bounds.absFixedY = prevPlrAbsFixedY;
			}
			else
			{
				elem.Bounds.absFixedX = prevTdrAbsFixedX + notifyTraderMoneyTextSeconds * (capi.World.Rand.NextDouble() * 4.0 - 2.0);
				elem.Bounds.absFixedY = prevTdrAbsFixedY + notifyTraderMoneyTextSeconds * (capi.World.Rand.NextDouble() * 4.0 - 2.0);
			}
		}
	}
}
