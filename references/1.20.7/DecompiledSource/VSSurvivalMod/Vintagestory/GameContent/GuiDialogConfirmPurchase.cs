using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogConfirmPurchase : GuiDialog
{
	private ModSystemAuction auctionSys;

	private EntityAgent buyerEntity;

	private EntityAgent traderEntity;

	private Auction auction;

	private ElementBounds dialogBounds;

	public override double InputOrder => 0.1;

	public override double DrawOrder => 1.0;

	public override bool UnregisterOnClose => true;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogConfirmPurchase(ICoreClientAPI capi, EntityAgent buyerEntity, EntityAgent auctioneerEntity, Auction auction)
		: base(capi)
	{
		this.buyerEntity = buyerEntity;
		traderEntity = auctioneerEntity;
		this.auction = auction;
		auctionSys = capi.ModLoader.GetModSystem<ModSystemAuction>();
		Init();
	}

	public void Init()
	{
		ElementBounds descBounds = ElementBounds.Fixed(0.0, 30.0, 400.0, 80.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		bgBounds.verticalSizing = ElementSizing.FitToChildren;
		bgBounds.horizontalSizing = ElementSizing.Fixed;
		bgBounds.fixedWidth = 300.0;
		int deliveryCosts = auctionSys.DeliveryCostsByDistance(traderEntity.Pos.XYZ, auction.SrcAuctioneerEntityPos);
		RichTextComponentBase[] stackComps = new RichTextComponentBase[2]
		{
			new ItemstackTextComponent(capi, auction.ItemStack, 60.0, 10.0),
			new RichTextComponent(capi, auction.ItemStack.GetName() + "\r\n", CairoFont.WhiteSmallText())
		};
		stackComps = stackComps.Append(VtmlUtil.Richtextify(capi, auction.ItemStack.GetDescription(capi.World, new DummySlot(auction.ItemStack)), CairoFont.WhiteDetailText()));
		CairoFont font = CairoFont.WhiteDetailText();
		double fl = font.UnscaledFontsize;
		ItemStack gearStack = auctionSys.SingleCurrencyStack;
		RichTextComponentBase[] deliveryCostComps = new RichTextComponentBase[2]
		{
			new RichTextComponent(capi, Lang.Get("Delivery: {0}", deliveryCosts), font)
			{
				PaddingRight = 10.0,
				VerticalAlign = EnumVerticalAlign.Top
			},
			new ItemstackTextComponent(capi, gearStack, fl * 2.5, 0.0, EnumFloat.Inline)
			{
				VerticalAlign = EnumVerticalAlign.Top,
				offX = 0.0 - GuiElement.scaled(fl * 0.5),
				offY = 0.0 - GuiElement.scaled(fl * 0.75)
			}
		};
		RichTextComponentBase[] totalCostComps = new RichTextComponentBase[2]
		{
			new RichTextComponent(capi, Lang.Get("Total Cost: {0}", auction.Price + deliveryCosts), font)
			{
				PaddingRight = 10.0,
				VerticalAlign = EnumVerticalAlign.Top
			},
			new ItemstackTextComponent(capi, gearStack, fl * 2.5, 0.0, EnumFloat.Inline)
			{
				VerticalAlign = EnumVerticalAlign.Top,
				offX = 0.0 - GuiElement.scaled(fl * 0.5),
				offY = 0.0 - GuiElement.scaled(fl * 0.75)
			}
		};
		Composers["confirmauctionpurchase"] = capi.Gui.CreateCompo("tradercreateauction-" + buyerEntity.EntityId, dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(Lang.Get("Purchase this item?"), OnCreateAuctionClose)
			.BeginChildElements(bgBounds)
			.AddRichtext(stackComps, descBounds, "itemstack");
		Composers["confirmauctionpurchase"].GetRichtext("itemstack").BeforeCalcBounds();
		double y = Math.Max(110.0, descBounds.fixedHeight + 20.0);
		ElementBounds deliverySwitchBounds = ElementBounds.Fixed(0.0, y, 35.0, 25.0);
		ElementBounds deliveryTextBounds = ElementBounds.Fixed(0.0, y + 3.0, 250.0, 25.0).FixedRightOf(deliverySwitchBounds);
		ElementBounds deliveryCostBounds = ElementBounds.Fixed(0.0, 0.0, 200.0, 30.0).FixedUnder(deliveryTextBounds, 20.0);
		ElementBounds totalCostBounds = ElementBounds.Fixed(0.0, 0.0, 150.0, 30.0).FixedUnder(deliveryCostBounds);
		ElementBounds leftButton = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0).FixedUnder(totalCostBounds, 15.0);
		ElementBounds rightButton = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 0.0, 0.0, 0.0).WithFixedPadding(8.0, 5.0).FixedUnder(totalCostBounds, 15.0);
		Composers["confirmauctionpurchase"].AddSwitch(onDeliveryModeChanged, deliverySwitchBounds, "delivery", 25.0).AddStaticText(Lang.Get("Deliver to current trader"), CairoFont.WhiteSmallText(), deliveryTextBounds).AddRichtext(deliveryCostComps, deliveryCostBounds, "deliveryCost")
			.AddRichtext(totalCostComps, totalCostBounds, "totalCost")
			.AddSmallButton(Lang.Get("Cancel"), OnCancel, leftButton)
			.AddSmallButton(Lang.Get("Purchase"), OnPurchase, rightButton, EnumButtonStyle.Normal, "buysellButton")
			.EndChildElements()
			.Compose();
		Composers["confirmauctionpurchase"].GetSwitch("delivery").On = true;
	}

	private void onDeliveryModeChanged(bool on)
	{
		int deliveryCosts = auctionSys.DeliveryCostsByDistance(traderEntity.Pos.XYZ, auction.SrcAuctioneerEntityPos);
		GuiElementRichtext rtele = Composers["confirmauctionpurchase"].GetRichtext("totalCost");
		(rtele.Components[0] as RichTextComponent).DisplayText = Lang.Get("Total Cost: {0}", auction.Price + (on ? deliveryCosts : 0));
		rtele.RecomposeText();
	}

	private bool OnCancel()
	{
		TryClose();
		return true;
	}

	private bool OnPurchase()
	{
		auctionSys.BuyAuctionClient(traderEntity, auction.AuctionId, Composers["confirmauctionpurchase"].GetSwitch("delivery").On);
		TryClose();
		return true;
	}

	public override bool CaptureAllInputs()
	{
		return IsOpened();
	}

	private void OnCreateAuctionClose()
	{
		TryClose();
	}

	public override void OnMouseMove(MouseEvent args)
	{
		base.OnMouseMove(args);
		args.Handled = dialogBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY);
	}

	public override void OnKeyDown(KeyEvent args)
	{
		base.OnKeyDown(args);
		if (focused && args.KeyCode == 50)
		{
			TryClose();
		}
		args.Handled = dialogBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY);
	}

	public override void OnKeyUp(KeyEvent args)
	{
		base.OnKeyUp(args);
		args.Handled = dialogBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY);
	}

	public override void OnMouseDown(MouseEvent args)
	{
		base.OnMouseDown(args);
		args.Handled = dialogBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY);
	}

	public override void OnMouseUp(MouseEvent args)
	{
		base.OnMouseUp(args);
		args.Handled = dialogBounds.PointInside(capi.Input.MouseX, capi.Input.MouseY);
	}
}
