using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

public class GuiElementFlatList : GuiElement
{
	public List<IFlatListItem> Elements = new List<IFlatListItem>();

	public int unscaledCellSpacing = 5;

	public int unscaledCellHeight = 40;

	public int unscalledYPad = 8;

	public Action<int> onLeftClick;

	private LoadedTexture hoverOverlayTexture;

	public ElementBounds insideBounds;

	private bool wasMouseDownOnElement;

	public GuiElementFlatList(ICoreClientAPI capi, ElementBounds bounds, Action<int> onLeftClick, List<IFlatListItem> elements = null)
		: base(capi, bounds)
	{
		hoverOverlayTexture = new LoadedTexture(capi);
		insideBounds = new ElementBounds().WithFixedPadding(unscaledCellSpacing).WithEmptyParent();
		insideBounds.CalcWorldBounds();
		this.onLeftClick = onLeftClick;
		if (elements != null)
		{
			Elements = elements;
		}
		CalcTotalHeight();
	}

	public void CalcTotalHeight()
	{
		double height = Elements.Where((IFlatListItem e) => e.Visible).Count() * (unscaledCellHeight + unscaledCellSpacing);
		insideBounds.fixedHeight = height + (double)unscaledCellSpacing;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		insideBounds = new ElementBounds().WithFixedPadding(unscaledCellSpacing).WithEmptyParent();
		insideBounds.CalcWorldBounds();
		CalcTotalHeight();
		Bounds.CalcWorldBounds();
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.InnerWidth, (int)GuiElement.scaled(unscaledCellHeight));
		Context context = new Context(surface);
		context.SetSourceRGBA(1.0, 1.0, 1.0, 0.5);
		context.Paint();
		generateTexture(surface, ref hoverOverlayTexture);
		context.Dispose();
		surface.Dispose();
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			base.OnMouseDownOnElement(api, args);
			wasMouseDownOnElement = true;
		}
	}

	public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y) || !wasMouseDownOnElement)
		{
			return;
		}
		wasMouseDownOnElement = false;
		int i = 0;
		int mx = api.Input.MouseX;
		int my = api.Input.MouseY;
		double posY = insideBounds.absY;
		foreach (IFlatListItem element in Elements)
		{
			if (!element.Visible)
			{
				i++;
				continue;
			}
			float y = (float)(5.0 + Bounds.absY + posY);
			double ypad = GuiElement.scaled(unscalledYPad);
			if ((double)mx > Bounds.absX && (double)mx <= Bounds.absX + Bounds.InnerWidth && (double)my >= (double)y - ypad && (double)my <= (double)y + GuiElement.scaled(unscaledCellHeight) - ypad)
			{
				api.Gui.PlaySound("menubutton_press");
				onLeftClick?.Invoke(i);
				args.Handled = true;
				break;
			}
			posY += GuiElement.scaled(unscaledCellHeight + unscaledCellSpacing);
			i++;
		}
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		EachVisibleElem(delegate(IFlatListItem elem)
		{
			if (elem is IFlatListItemInteractable flatListItemInteractable)
			{
				flatListItemInteractable.OnMouseMove(api, args);
			}
		});
	}

	public override void OnMouseDown(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		EachVisibleElem(delegate(IFlatListItem elem)
		{
			if (elem is IFlatListItemInteractable flatListItemInteractable)
			{
				flatListItemInteractable.OnMouseDown(api, args);
			}
		});
		if (!args.Handled)
		{
			base.OnMouseDown(api, args);
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		if (!Bounds.ParentBounds.PointInside(args.X, args.Y))
		{
			return;
		}
		EachVisibleElem(delegate(IFlatListItem elem)
		{
			if (elem is IFlatListItemInteractable flatListItemInteractable)
			{
				flatListItemInteractable.OnMouseUp(api, args);
			}
		});
		if (!args.Handled)
		{
			base.OnMouseUp(api, args);
		}
	}

	protected void EachVisibleElem(Action<IFlatListItem> onElem)
	{
		foreach (IFlatListItem element in Elements)
		{
			if (element.Visible)
			{
				onElem(element);
			}
		}
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		int mx = api.Input.MouseX;
		int my = api.Input.MouseY;
		bool inbounds = Bounds.ParentBounds.PointInside(mx, my);
		double posY = insideBounds.absY;
		double ypad = GuiElement.scaled(unscalledYPad);
		double height = GuiElement.scaled(unscaledCellHeight);
		foreach (IFlatListItem element in Elements)
		{
			if (element.Visible)
			{
				float y = (float)(5.0 + Bounds.absY + posY);
				if (inbounds && (double)mx > Bounds.absX && (double)mx <= Bounds.absX + Bounds.InnerWidth && (double)my >= (double)y - ypad && (double)my <= (double)y + height - ypad)
				{
					api.Render.Render2DLoadedTexture(hoverOverlayTexture, (float)Bounds.absX, y - (float)ypad);
				}
				if (posY > -50.0 && posY < Bounds.OuterHeight + 50.0)
				{
					element.RenderListEntryTo(api, deltaTime, Bounds.absX, y, Bounds.InnerWidth, height);
				}
				posY += GuiElement.scaled(unscaledCellHeight + unscaledCellSpacing);
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		hoverOverlayTexture.Dispose();
		foreach (IFlatListItem element in Elements)
		{
			element.Dispose();
		}
	}
}
