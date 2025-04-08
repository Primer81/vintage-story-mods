using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ActivityCellEntry : GuiElement, IGuiElementCell, IDisposable
{
	public LoadedTexture hoverTexture;

	private double unScaledCellHeight = 35.0;

	private GuiElementRichtext nameTextElem;

	private GuiElementRichtext detailTextElem;

	private bool composed;

	public bool Selected;

	private Action<int> onClick;

	private float accum1Sec;

	private string prevExpireText;

	public bool Visible => true;

	ElementBounds IGuiElementCell.Bounds => Bounds;

	public ActivityCellEntry(ICoreClientAPI capi, ElementBounds bounds, string name, string detail, Action<int> onClick, float leftColWidth = 200f, float rightColWidth = 300f)
		: base(capi, bounds)
	{
		this.onClick = onClick;
		CairoFont font = CairoFont.WhiteDetailText();
		double offY = (unScaledCellHeight - font.UnscaledFontsize) / 2.0;
		ElementBounds nameTextBounds = ElementBounds.Fixed(0.0, offY, leftColWidth, 25.0).WithParent(Bounds);
		ElementBounds activitiesBounds = ElementBounds.Fixed(0.0, offY, rightColWidth, 25.0).WithParent(Bounds).FixedRightOf(nameTextBounds, 10.0);
		nameTextElem = new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, name, font), nameTextBounds);
		detailTextElem = new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, detail, font), activitiesBounds);
		hoverTexture = new LoadedTexture(capi);
	}

	public void Recompose()
	{
		composed = true;
		nameTextElem.Compose();
		detailTextElem.Compose();
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
		nameTextElem.RenderInteractiveElements(deltaTime);
		detailTextElem.RenderInteractiveElements(deltaTime);
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

	public void UpdateCellHeight()
	{
		Bounds.CalcWorldBounds();
		nameTextElem.BeforeCalcBounds();
		detailTextElem.BeforeCalcBounds();
		Bounds.fixedHeight = unScaledCellHeight;
	}

	public void OnMouseUpOnElement(MouseEvent args, int elementIndex)
	{
		_ = api.Input.MouseX;
		_ = api.Input.MouseY;
		if (!args.Handled)
		{
			onClick?.Invoke(elementIndex);
		}
	}

	public override void Dispose()
	{
		nameTextElem.Dispose();
		detailTextElem.Dispose();
		hoverTexture?.Dispose();
	}

	public void OnMouseDownOnElement(MouseEvent args, int elementIndex)
	{
	}

	public void OnMouseMoveOnElement(MouseEvent args, int elementIndex)
	{
	}
}
