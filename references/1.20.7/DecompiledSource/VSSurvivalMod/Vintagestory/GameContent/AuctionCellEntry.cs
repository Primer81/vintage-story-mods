using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class AuctionCellEntry : GuiElement, IGuiElementCell, IDisposable
{
	public DummySlot dummySlot;

	private ElementBounds scissorBounds;

	public Auction auction;

	public LoadedTexture hoverTexture;

	private float unscaledIconSize = 35f;

	private float iconSize;

	private double unScaledCellHeight = 35.0;

	private GuiElementRichtext stackNameTextElem;

	private GuiElementRichtext priceTextElem;

	private GuiElementRichtext expireTextElem;

	private GuiElementRichtext sellerTextElem;

	private bool composed;

	public bool Selected;

	private Action<int> onClick;

	private float accum1Sec;

	private string prevExpireText;

	public bool Visible => true;

	ElementBounds IGuiElementCell.Bounds => Bounds;

	public AuctionCellEntry(ICoreClientAPI capi, InventoryBase inventoryAuction, ElementBounds bounds, Auction auction, Action<int> onClick)
		: base(capi, bounds)
	{
		iconSize = (float)GuiElement.scaled(unscaledIconSize);
		dummySlot = new DummySlot(auction.ItemStack, inventoryAuction);
		this.onClick = onClick;
		this.auction = auction;
		CairoFont font = CairoFont.WhiteDetailText();
		double offY = (unScaledCellHeight - font.UnscaledFontsize) / 2.0;
		scissorBounds = ElementBounds.FixedSize(unscaledIconSize, unscaledIconSize).WithParent(Bounds);
		ElementBounds stackNameTextBounds = ElementBounds.Fixed(0.0, offY, 270.0, 25.0).WithParent(Bounds).FixedRightOf(scissorBounds, 10.0);
		ElementBounds priceTextBounds = ElementBounds.Fixed(0.0, offY, 75.0, 25.0).WithParent(Bounds).FixedRightOf(stackNameTextBounds, 10.0);
		ElementBounds expireTextBounds = ElementBounds.Fixed(0.0, 0.0, 160.0, 25.0).WithParent(Bounds).FixedRightOf(priceTextBounds, 10.0);
		ElementBounds sellerTextBounds = ElementBounds.Fixed(0.0, offY, 110.0, 25.0).WithParent(Bounds).FixedRightOf(expireTextBounds, 10.0);
		stackNameTextElem = new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, dummySlot.Itemstack.GetName(), font), stackNameTextBounds);
		double fl = font.UnscaledFontsize;
		ItemStack gearStack = capi.ModLoader.GetModSystem<ModSystemAuction>().SingleCurrencyStack;
		RichTextComponentBase[] comps = new RichTextComponentBase[2]
		{
			new RichTextComponent(capi, auction.Price.ToString() ?? "", font)
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
		priceTextElem = new GuiElementRichtext(capi, comps, priceTextBounds);
		expireTextElem = new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, prevExpireText = auction.GetExpireText(capi), font.Clone().WithFontSize(14f)), expireTextBounds);
		expireTextElem.BeforeCalcBounds();
		expireTextBounds.fixedY = 5.0 + (25.0 - expireTextElem.TotalHeight / (double)RuntimeEnv.GUIScale) / 2.0;
		sellerTextElem = new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, auction.SellerName, font.Clone().WithOrientation(EnumTextOrientation.Right)), sellerTextBounds);
		hoverTexture = new LoadedTexture(capi);
	}

	public void Recompose()
	{
		composed = true;
		stackNameTextElem.Compose();
		priceTextElem.Compose();
		expireTextElem.Compose();
		sellerTextElem.Compose();
		ImageSurface surface = new ImageSurface(Format.Argb32, 2, 2);
		Context context = genContext(surface);
		context.NewPath();
		context.LineTo(0.0, 0.0);
		context.LineTo(2.0, 0.0);
		context.LineTo(2.0, 2.0);
		context.LineTo(0.0, 2.0);
		context.ClosePath();
		context.SetSourceRGBA(0.0, 0.0, 0.0, 0.15);
		context.Fill();
		generateTexture(surface, ref hoverTexture);
		context.Dispose();
		surface.Dispose();
	}

	public void OnRenderInteractiveElements(ICoreClientAPI api, float deltaTime)
	{
		if (!composed)
		{
			Recompose();
		}
		accum1Sec += deltaTime;
		if (accum1Sec > 1f)
		{
			string expireText = auction.GetExpireText(api);
			if (expireText != prevExpireText)
			{
				expireTextElem.Components = VtmlUtil.Richtextify(api, expireText, CairoFont.WhiteDetailText().WithFontSize(14f));
				expireTextElem.RecomposeText();
				prevExpireText = expireText;
			}
		}
		if (scissorBounds.InnerWidth <= 0.0 || scissorBounds.InnerHeight <= 0.0)
		{
			return;
		}
		api.Render.PushScissor(scissorBounds, stacking: true);
		api.Render.RenderItemstackToGui(dummySlot, scissorBounds.renderX + (double)(iconSize / 2f), scissorBounds.renderY + (double)(iconSize / 2f), 100.0, iconSize * 0.55f, -1);
		api.Render.PopScissor();
		api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, scissorBounds.renderX, scissorBounds.renderY, scissorBounds.OuterWidth, scissorBounds.OuterHeight);
		stackNameTextElem.RenderInteractiveElements(deltaTime);
		priceTextElem.RenderInteractiveElements(deltaTime);
		expireTextElem.RenderInteractiveElements(deltaTime);
		MouseOverCursor = expireTextElem.MouseOverCursor;
		sellerTextElem.RenderInteractiveElements(deltaTime);
		int dx = api.Input.MouseX;
		int dy = api.Input.MouseY;
		Vec2d pos = Bounds.PositionInside(dx, dy);
		if (Selected || (pos != null && IsPositionInside(api.Input.MouseX, api.Input.MouseY)))
		{
			api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			if (Selected)
			{
				api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			}
		}
	}

	public void OnMouseMoveOnElement(MouseEvent args, int elementIndex)
	{
		int x = api.Input.MouseX;
		int y = api.Input.MouseY;
		if (scissorBounds.PositionInside(x, y) != null)
		{
			api.Input.TriggerOnMouseEnterSlot(dummySlot);
		}
		else
		{
			api.Input.TriggerOnMouseLeaveSlot(dummySlot);
		}
		args.Handled = true;
	}

	public void OnMouseDownOnElement(MouseEvent args, int elementIndex)
	{
		int x = api.Input.MouseX;
		int y = api.Input.MouseY;
		if (expireTextElem.Bounds.PointInside(x, y))
		{
			expireTextElem.OnMouseDownOnElement(api, args);
		}
	}

	public void UpdateCellHeight()
	{
		Bounds.CalcWorldBounds();
		scissorBounds.CalcWorldBounds();
		stackNameTextElem.BeforeCalcBounds();
		priceTextElem.BeforeCalcBounds();
		expireTextElem.BeforeCalcBounds();
		sellerTextElem.BeforeCalcBounds();
		Bounds.fixedHeight = unScaledCellHeight;
	}

	public void OnMouseUpOnElement(MouseEvent args, int elementIndex)
	{
		int x = api.Input.MouseX;
		int y = api.Input.MouseY;
		if (expireTextElem.Bounds.PointInside(x, y))
		{
			expireTextElem.OnMouseUp(api, args);
		}
		if (!args.Handled)
		{
			onClick?.Invoke(elementIndex);
		}
	}

	public override void Dispose()
	{
		stackNameTextElem.Dispose();
		priceTextElem.Dispose();
		expireTextElem.Dispose();
	}
}
