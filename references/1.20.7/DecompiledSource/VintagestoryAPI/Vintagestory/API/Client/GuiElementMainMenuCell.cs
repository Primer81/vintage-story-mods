using System;
using Cairo;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class GuiElementMainMenuCell : GuiElementTextBase, IGuiElementCell, IDisposable
{
	public static double unscaledRightBoxWidth = 40.0;

	private static int unscaledDepth = 4;

	/// <summary>
	/// The table cell information.
	/// </summary>
	public SavegameCellEntry cellEntry;

	private double titleTextheight;

	public bool ShowModifyIcons = true;

	private LoadedTexture releasedButtonTexture;

	private LoadedTexture pressedButtonTexture;

	private LoadedTexture leftHighlightTexture;

	private LoadedTexture rightHighlightTexture;

	private double pressedYOffset;

	public double MainTextWidthSub;

	public Action<int> OnMouseDownOnCellLeft;

	public Action<int> OnMouseDownOnCellRight;

	public double? FixedHeight;

	ElementBounds IGuiElementCell.Bounds => Bounds;

	/// <summary>
	/// Creates a new Element Cell.  A container for TableCells.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="cell">The base cell</param>
	/// <param name="bounds">The bounds of the TableCell</param>
	public GuiElementMainMenuCell(ICoreClientAPI capi, SavegameCellEntry cell, ElementBounds bounds)
		: base(capi, "", null, bounds)
	{
		cellEntry = cell;
		leftHighlightTexture = new LoadedTexture(capi);
		rightHighlightTexture = new LoadedTexture(capi);
		releasedButtonTexture = new LoadedTexture(capi);
		pressedButtonTexture = new LoadedTexture(capi);
		if (cell.TitleFont == null)
		{
			cell.TitleFont = CairoFont.WhiteSmallishText();
		}
		if (cell.DetailTextFont == null)
		{
			cell.DetailTextFont = CairoFont.WhiteSmallText();
			cell.DetailTextFont.Color[3] *= 0.8;
			cell.DetailTextFont.LineHeightMultiplier = 1.1;
		}
	}

	public void Compose()
	{
		Bounds.CalcWorldBounds();
		ImageSurface surface = new ImageSurface(Format.Argb32, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		Context ctx = new Context(surface);
		ComposeButton(ctx, surface, pressed: false);
		generateTexture(surface, ref releasedButtonTexture);
		ctx.Operator = Operator.Clear;
		ctx.Paint();
		ctx.Operator = Operator.Over;
		ComposeButton(ctx, surface, pressed: true);
		generateTexture(surface, ref pressedButtonTexture);
		ctx.Dispose();
		surface.Dispose();
		ComposeHover(left: true, ref leftHighlightTexture);
		if (ShowModifyIcons)
		{
			ComposeHover(left: false, ref rightHighlightTexture);
		}
	}

	private void ComposeButton(Context ctx, ImageSurface surface, bool pressed)
	{
		double rightBoxWidth = (ShowModifyIcons ? GuiElement.scaled(unscaledRightBoxWidth) : 0.0);
		pressedYOffset = 0.0;
		if (cellEntry.DrawAsButton)
		{
			GuiElement.RoundRectangle(ctx, 0.0, 0.0, Bounds.OuterWidthInt, Bounds.OuterHeightInt, 1.0);
			ctx.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
			ctx.Fill();
			if (pressed)
			{
				pressedYOffset = GuiElement.scaled(unscaledDepth) / 2.0;
			}
			EmbossRoundRectangleElement(ctx, 0.0, 0.0, Bounds.OuterWidthInt, Bounds.OuterHeightInt, pressed, (int)GuiElement.scaled(unscaledDepth));
		}
		Font = cellEntry.TitleFont;
		titleTextheight = textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, cellEntry.Title, Bounds.absPaddingX, Bounds.absPaddingY + Bounds.absPaddingY + GuiElement.scaled(cellEntry.LeftOffY) + pressedYOffset, Bounds.InnerWidth - rightBoxWidth - MainTextWidthSub);
		Font = cellEntry.DetailTextFont;
		textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, cellEntry.DetailText, Bounds.absPaddingX, Bounds.absPaddingY + cellEntry.DetailTextOffY + titleTextheight + 2.0 + Bounds.absPaddingY + GuiElement.scaled(cellEntry.LeftOffY) + pressedYOffset, Bounds.InnerWidth - rightBoxWidth - MainTextWidthSub);
		if (cellEntry.RightTopText != null)
		{
			TextExtents extents = Font.GetTextExtents(cellEntry.RightTopText);
			textUtil.AutobreakAndDrawMultilineTextAt(ctx, Font, cellEntry.RightTopText, Bounds.absPaddingX + Bounds.InnerWidth - extents.Width - rightBoxWidth - GuiElement.scaled(10.0), Bounds.absPaddingY + Bounds.absPaddingY + GuiElement.scaled(cellEntry.RightTopOffY) + pressedYOffset, extents.Width + 1.0, EnumTextOrientation.Right);
		}
		if (ShowModifyIcons)
		{
			ctx.LineWidth = GuiElement.scaled(1.0);
			double crossSize = GuiElement.scaled(20.0);
			double crossWidth = GuiElement.scaled(5.0);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
			ctx.NewPath();
			ctx.MoveTo(Bounds.InnerWidth - rightBoxWidth, GuiElement.scaled(1.0));
			ctx.LineTo(Bounds.InnerWidth - rightBoxWidth, Bounds.OuterHeight - GuiElement.scaled(2.0));
			ctx.ClosePath();
			ctx.Stroke();
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.3);
			ctx.NewPath();
			ctx.MoveTo(Bounds.InnerWidth - rightBoxWidth + GuiElement.scaled(1.0), GuiElement.scaled(1.0));
			ctx.LineTo(Bounds.InnerWidth - rightBoxWidth + GuiElement.scaled(1.0), Bounds.OuterHeight - GuiElement.scaled(2.0));
			ctx.ClosePath();
			ctx.Stroke();
			double crossX = Bounds.absPaddingX + Bounds.InnerWidth - rightBoxWidth + GuiElement.scaled(5.0);
			double crossY = Bounds.absPaddingY;
			ctx.Operator = Operator.Source;
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.8);
			api.Gui.Icons.DrawPen(ctx, crossX - 1.0, crossY - 1.0 + GuiElement.scaled(5.0), crossWidth, crossSize);
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.5);
			api.Gui.Icons.DrawPen(ctx, crossX + 1.0, crossY + 1.0 + GuiElement.scaled(5.0), crossWidth, crossSize);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
			api.Gui.Icons.DrawPen(ctx, crossX, crossY + GuiElement.scaled(5.0), crossWidth, crossSize);
			ctx.Operator = Operator.Over;
		}
		if (cellEntry.DrawAsButton && pressed)
		{
			GuiElement.RoundRectangle(ctx, 0.0, 0.0, Bounds.OuterWidthInt, Bounds.OuterHeightInt, 1.0);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.15);
			ctx.Fill();
		}
	}

	private void ComposeHover(bool left, ref LoadedTexture texture)
	{
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context ctx = genContext(surface);
		double rightBoxWidth = GuiElement.scaled(unscaledRightBoxWidth);
		if (!ShowModifyIcons)
		{
			rightBoxWidth = 0.0 - Bounds.OuterWidth + Bounds.InnerWidth;
		}
		if (left)
		{
			ctx.NewPath();
			ctx.LineTo(0.0, 0.0);
			ctx.LineTo(Bounds.InnerWidth - rightBoxWidth, 0.0);
			ctx.LineTo(Bounds.InnerWidth - rightBoxWidth, Bounds.OuterHeight);
			ctx.LineTo(0.0, Bounds.OuterHeight);
			ctx.ClosePath();
		}
		else
		{
			ctx.NewPath();
			ctx.LineTo(Bounds.InnerWidth - rightBoxWidth, 0.0);
			ctx.LineTo(Bounds.OuterWidth, 0.0);
			ctx.LineTo(Bounds.OuterWidth, Bounds.OuterHeight);
			ctx.LineTo(Bounds.InnerWidth - rightBoxWidth, Bounds.OuterHeight);
			ctx.ClosePath();
		}
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.15);
		ctx.Fill();
		generateTexture(surface, ref texture);
		ctx.Dispose();
		surface.Dispose();
	}

	/// <summary>
	/// Updates the height of the cell based off the contents.
	/// </summary>
	public void UpdateCellHeight()
	{
		Bounds.CalcWorldBounds();
		if (FixedHeight.HasValue)
		{
			Bounds.fixedHeight = FixedHeight.Value;
			return;
		}
		double unscaledPadding = Bounds.absPaddingY / (double)RuntimeEnv.GUIScale;
		double boxwidth = Bounds.InnerWidth;
		Font = cellEntry.TitleFont;
		text = cellEntry.Title;
		titleTextheight = textUtil.GetMultilineTextHeight(Font, cellEntry.Title, boxwidth - MainTextWidthSub) / (double)RuntimeEnv.GUIScale;
		Font = cellEntry.DetailTextFont;
		text = cellEntry.DetailText;
		double detailTextHeight = textUtil.GetMultilineTextHeight(Font, cellEntry.DetailText, boxwidth - MainTextWidthSub) / (double)RuntimeEnv.GUIScale;
		Bounds.fixedHeight = unscaledPadding + titleTextheight + unscaledPadding + detailTextHeight + unscaledPadding;
		if (ShowModifyIcons && Bounds.fixedHeight < 73.0)
		{
			Bounds.fixedHeight = 73.0;
		}
	}

	/// <summary>
	/// Renders the main menu cell
	/// </summary>
	/// <param name="api"></param>
	/// <param name="deltaTime"></param>
	public void OnRenderInteractiveElements(ICoreClientAPI api, float deltaTime)
	{
		if (pressedButtonTexture.TextureId == 0)
		{
			Compose();
		}
		if (cellEntry.Selected)
		{
			api.Render.Render2DTexturePremultipliedAlpha(pressedButtonTexture.TextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		}
		else
		{
			api.Render.Render2DTexturePremultipliedAlpha(releasedButtonTexture.TextureId, (int)Bounds.absX, (int)Bounds.absY, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		}
		int dx = api.Input.MouseX;
		int dy = api.Input.MouseY;
		Vec2d pos = Bounds.PositionInside(dx, dy);
		if (!(pos == null) && IsPositionInside(api.Input.MouseX, api.Input.MouseY))
		{
			if (ShowModifyIcons && pos.X > Bounds.InnerWidth - GuiElement.scaled(unscaledRightBoxWidth))
			{
				api.Render.Render2DTexturePremultipliedAlpha(rightHighlightTexture.TextureId, Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			}
			else
			{
				api.Render.Render2DTexturePremultipliedAlpha(leftHighlightTexture.TextureId, Bounds.absX, Bounds.absY, Bounds.OuterWidth, Bounds.OuterHeight);
			}
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		leftHighlightTexture.Dispose();
		rightHighlightTexture.Dispose();
		releasedButtonTexture.Dispose();
		pressedButtonTexture.Dispose();
	}

	public void OnMouseUpOnElement(MouseEvent args, int elementIndex)
	{
		int mousex = api.Input.MouseX;
		int mousey = api.Input.MouseY;
		Vec2d vec2d = Bounds.PositionInside(mousex, mousey);
		api.Gui.PlaySound("toggleswitch");
		if (vec2d.X > Bounds.InnerWidth - GuiElement.scaled(unscaledRightBoxWidth))
		{
			OnMouseDownOnCellRight?.Invoke(elementIndex);
			args.Handled = true;
		}
		else
		{
			OnMouseDownOnCellLeft?.Invoke(elementIndex);
			args.Handled = true;
		}
	}

	public void OnMouseMoveOnElement(MouseEvent args, int elementIndex)
	{
	}

	public void OnMouseDownOnElement(MouseEvent args, int elementIndex)
	{
	}
}
