using System;
using Cairo;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// A title bar for your GUI.  
/// </summary>
public class GuiElementDialogTitleBar : GuiElementTextBase
{
	private GuiElementListMenu listMenu;

	private Action OnClose;

	internal GuiComposer baseComposer;

	/// <summary>
	/// The size of the close icon in the top right corner of the GUI.
	/// </summary>
	public static int unscaledCloseIconSize = 15;

	private LoadedTexture closeIconHoverTexture;

	private LoadedTexture menuIconHoverTexture;

	private Rectangled closeIconRect;

	private Rectangled menuIconRect;

	private bool didInit;

	private bool firstFrameRendered;

	public bool drawBg;

	private bool movable;

	private bool moving;

	private Vec2i movingStartPos = new Vec2i();

	private ElementBounds parentBoundsBefore;

	public bool Movable => movable;

	/// <summary>
	/// Creates a new title bar.  
	/// </summary>
	/// <param name="capi">The Client API.</param>
	/// <param name="text">The text on the title bar.</param>
	/// <param name="composer">The GuiComposer for the title bar.</param>
	/// <param name="OnClose">The event fired when the title bar is closed.</param>
	/// <param name="font">The font of the title bar.</param>
	/// <param name="bounds">The bounds of the title bar.</param>
	public GuiElementDialogTitleBar(ICoreClientAPI capi, string text, GuiComposer composer, Action OnClose = null, CairoFont font = null, ElementBounds bounds = null)
		: base(capi, text, font, bounds)
	{
		closeIconHoverTexture = new LoadedTexture(capi);
		menuIconHoverTexture = new LoadedTexture(capi);
		if (bounds == null)
		{
			Bounds = ElementStdBounds.TitleBar();
		}
		if (font == null)
		{
			Font = CairoFont.WhiteSmallText();
		}
		this.OnClose = OnClose;
		ElementBounds dropDownBounds = ElementBounds.Fixed(0.0, 0.0, 100.0, 25.0);
		Bounds.WithChild(dropDownBounds);
		listMenu = new GuiElementListMenu(capi, new string[2] { "auto", "manual" }, new string[2]
		{
			Lang.Get("Fixed"),
			Lang.Get("Movable")
		}, 0, onSelectionChanged, dropDownBounds, CairoFont.WhiteSmallText(), multiSelect: false)
		{
			HoveredIndex = 0
		};
		baseComposer = composer;
	}

	private void onSelectionChanged(string val, bool on)
	{
		SetUpMovableState(val);
	}

	private void SetUpMovableState(string val)
	{
		if (val == null)
		{
			Vec2i pos = api.Gui.GetDialogPosition(baseComposer.DialogName);
			if (pos != null)
			{
				movable = true;
				parentBoundsBefore = Bounds.ParentBounds.FlatCopy();
				Bounds.ParentBounds.Alignment = EnumDialogArea.None;
				Bounds.ParentBounds.fixedX = pos.X;
				Bounds.ParentBounds.fixedY = Math.Max(0.0 - Bounds.ParentBounds.fixedOffsetY, pos.Y);
				Bounds.ParentBounds.absMarginX = 0.0;
				Bounds.ParentBounds.absMarginY = 0.0;
				Bounds.ParentBounds.MarkDirtyRecursive();
				Bounds.ParentBounds.CalcWorldBounds();
			}
		}
		else if (val == "auto")
		{
			if (parentBoundsBefore != null)
			{
				Bounds.ParentBounds.fixedX = parentBoundsBefore.fixedX;
				Bounds.ParentBounds.fixedY = parentBoundsBefore.fixedY;
				Bounds.ParentBounds.fixedOffsetX = parentBoundsBefore.fixedOffsetX;
				Bounds.ParentBounds.fixedOffsetY = parentBoundsBefore.fixedOffsetY;
				Bounds.ParentBounds.Alignment = parentBoundsBefore.Alignment;
				Bounds.ParentBounds.absMarginX = parentBoundsBefore.absMarginX;
				Bounds.ParentBounds.absMarginY = parentBoundsBefore.absMarginY;
				Bounds.ParentBounds.MarkDirtyRecursive();
				Bounds.ParentBounds.CalcWorldBounds();
			}
			movable = false;
			api.Gui.SetDialogPosition(baseComposer.DialogName, null);
		}
		else
		{
			movable = true;
			parentBoundsBefore = Bounds.ParentBounds.FlatCopy();
			Bounds.ParentBounds.Alignment = EnumDialogArea.None;
			Bounds.ParentBounds.fixedOffsetX = 0.0;
			Bounds.ParentBounds.fixedOffsetY = 0.0;
			Bounds.ParentBounds.fixedX = Bounds.ParentBounds.absX / (double)RuntimeEnv.GUIScale;
			Bounds.ParentBounds.fixedY = Bounds.ParentBounds.absY / (double)RuntimeEnv.GUIScale;
			Bounds.ParentBounds.absMarginX = 0.0;
			Bounds.ParentBounds.absMarginY = 0.0;
			Bounds.ParentBounds.MarkDirtyRecursive();
			Bounds.ParentBounds.CalcWorldBounds();
		}
	}

	public override void ComposeTextElements(Context ctx, ImageSurface surface)
	{
		if (!didInit)
		{
			SetUpMovableState(null);
			didInit = true;
		}
		Bounds.CalcWorldBounds();
		double strokeWidth = 5.0;
		GuiElement.RoundRectangle(ctx, Bounds.bgDrawX, Bounds.bgDrawY, Bounds.OuterWidth, Bounds.OuterHeight, 0.0);
		ctx.SetSourceRGBA(GuiStyle.DialogStrongBgColor[0] * 1.2, GuiStyle.DialogStrongBgColor[1] * 1.2, GuiStyle.DialogStrongBgColor[2] * 1.2, GuiStyle.DialogStrongBgColor[3]);
		ctx.FillPreserve();
		GuiElement.RoundRectangle(ctx, Bounds.bgDrawX + strokeWidth, Bounds.bgDrawY + strokeWidth, Bounds.OuterWidth - 2.0 * strokeWidth, Bounds.OuterHeight - 2.0 * strokeWidth, 0.0);
		ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor[0] * 1.6, GuiStyle.DialogStrongBgColor[1] * 1.6, GuiStyle.DialogStrongBgColor[2] * 1.6, 1.0);
		ctx.LineWidth = strokeWidth * 1.75;
		ctx.StrokePreserve();
		double r = GuiElement.scaled(8.0);
		surface.BlurPartial(r, (int)(2.0 * r + 1.0), (int)Bounds.bgDrawX, (int)(Bounds.bgDrawY + 0.0), (int)Bounds.OuterWidth, (int)Bounds.InnerHeight);
		double radius = 0.0;
		ctx.NewPath();
		ctx.MoveTo(Bounds.drawX, Bounds.drawY + Bounds.InnerHeight);
		ctx.LineTo(Bounds.drawX, Bounds.drawY + radius);
		ctx.Arc(Bounds.drawX + radius, Bounds.drawY + radius, radius, 3.1415927410125732, 4.71238899230957);
		ctx.Arc(Bounds.drawX + Bounds.OuterWidth - radius, Bounds.drawY + radius, radius, -1.5707963705062866, 0.0);
		ctx.LineTo(Bounds.drawX + Bounds.OuterWidth, Bounds.drawY + Bounds.InnerHeight);
		ctx.SetSourceRGBA(new double[4]
		{
			0.17647058823529413,
			7.0 / 51.0,
			11.0 / 85.0,
			1.0
		});
		ctx.LineWidth = strokeWidth;
		ctx.Stroke();
		Font.SetupContext(ctx);
		DrawTextLineAt(ctx, text, GuiElement.scaled(GuiStyle.ElementToDialogPadding), (Bounds.InnerHeight - Font.GetFontExtents().Height) / 2.0 + GuiElement.scaled(1.0));
		double crossSize = GuiElement.scaled(unscaledCloseIconSize);
		double menuSize = GuiElement.scaled(unscaledCloseIconSize + 2);
		double crossX = Bounds.drawX + Bounds.OuterWidth - crossSize - GuiElement.scaled(12.0);
		double iconY = Bounds.drawY + GuiElement.scaled(7.0);
		double crossWidth = GuiElement.scaled(2.0);
		double menuX = Bounds.drawX + Bounds.OuterWidth - crossSize - menuSize - GuiElement.scaled(20.0);
		menuIconRect = new Rectangled(Bounds.OuterWidth - crossSize - menuSize - GuiElement.scaled(20.0), GuiElement.scaled(6.0), crossSize, crossSize);
		closeIconRect = new Rectangled(Bounds.OuterWidth - crossSize - GuiElement.scaled(12.0), GuiElement.scaled(5.0), menuSize, menuSize);
		ctx.Operator = Operator.Over;
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.3);
		api.Gui.Icons.DrawCross(ctx, crossX + 2.0, iconY + 2.0, crossWidth, crossSize);
		ctx.Operator = Operator.Source;
		ctx.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
		api.Gui.Icons.DrawCross(ctx, crossX, iconY, crossWidth, crossSize);
		ctx.Operator = Operator.Over;
		api.Gui.Icons.Drawmenuicon_svg(ctx, (int)menuX + 2, (int)iconY + 2, (int)menuSize, (int)menuSize, new double[4] { 0.0, 0.0, 0.0, 0.3 });
		ctx.Operator = Operator.Source;
		api.Gui.Icons.Drawmenuicon_svg(ctx, (int)menuX, (int)iconY + 1, (int)menuSize, (int)menuSize, GuiStyle.DialogDefaultTextColor);
		ctx.Operator = Operator.Over;
		ComposeHoverIcons();
		listMenu.Bounds.fixedX = (Bounds.absX + menuIconRect.X - Bounds.absX) / (double)RuntimeEnv.GUIScale;
		listMenu.ComposeDynamicElements();
	}

	private void ComposeHoverIcons()
	{
		double crossSize = GuiElement.scaled(unscaledCloseIconSize);
		double menuSize = GuiElement.scaled(unscaledCloseIconSize + 2);
		int crossWidth = (int)Math.Round(GuiElement.scaled(1.9));
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)crossSize + 4, (int)crossSize + 4);
		Context ctx = genContext(surface);
		ctx.Operator = Operator.Source;
		ctx.SetSourceRGBA(0.8, 0.0, 0.0, 1.0);
		api.Gui.Icons.DrawCross(ctx, 0.5, 1.5, crossWidth, crossSize);
		ctx.SetSourceRGBA(0.8, 0.2, 0.2, 1.0);
		api.Gui.Icons.DrawCross(ctx, 1.0, 2.0, crossWidth, crossSize);
		generateTexture(surface, ref closeIconHoverTexture);
		surface.Dispose();
		ctx.Dispose();
		surface = new ImageSurface(Format.Argb32, (int)menuSize, (int)menuSize);
		ctx = genContext(surface);
		ctx.Operator = Operator.Source;
		api.Gui.Icons.Drawmenuicon_svg(ctx, 0.0, GuiElement.scaled(1.0), (int)menuSize, (int)menuSize, new double[4] { 0.0, 0.8, 0.0, 0.6 });
		generateTexture(surface, ref menuIconHoverTexture);
		surface.Dispose();
		ctx.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (!firstFrameRendered && movable)
		{
			float scale = RuntimeEnv.GUIScale;
			double maxx = (float)api.Render.FrameWidth - 60f * scale;
			double maxy = (float)api.Render.FrameHeight - 60f * scale;
			double x = GameMath.Clamp((double)(int)Bounds.ParentBounds.fixedX + Bounds.ParentBounds.fixedOffsetX, 0.0, maxx / (double)scale) - Bounds.ParentBounds.fixedOffsetX;
			double y = GameMath.Clamp((double)(int)Bounds.ParentBounds.fixedY + Bounds.ParentBounds.fixedOffsetY, 0.0, maxy / (double)scale) - Bounds.ParentBounds.fixedOffsetY;
			api.Gui.SetDialogPosition(baseComposer.DialogName, new Vec2i((int)x, (int)y));
			Bounds.ParentBounds.fixedX = x;
			Bounds.ParentBounds.fixedY = y;
			Bounds.ParentBounds.CalcWorldBounds();
			firstFrameRendered = true;
		}
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		if (closeIconRect.PointInside((double)mouseX - Bounds.absX, (double)mouseY - Bounds.absY))
		{
			api.Render.Render2DTexturePremultipliedAlpha(closeIconHoverTexture.TextureId, Bounds.absX + closeIconRect.X - GuiElement.scaled(1.0), Bounds.absY + closeIconRect.Y, closeIconRect.Width + 4.0, closeIconRect.Height + 4.0, 200f);
		}
		if (menuIconRect.PointInside((double)mouseX - Bounds.absX, (double)mouseY - Bounds.absY) || listMenu.IsOpened)
		{
			api.Render.Render2DTexturePremultipliedAlpha(menuIconHoverTexture.TextureId, Bounds.absX + menuIconRect.X, Bounds.absY + menuIconRect.Y, menuIconRect.Width + 4.0, menuIconRect.Height + 4.0, 200f);
		}
		listMenu.RenderInteractiveElements(deltaTime);
	}

	public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
	{
		int mouseX = api.Input.MouseX;
		int mouseY = api.Input.MouseY;
		if (closeIconRect.PointInside((double)mouseX - Bounds.absX, (double)mouseY - Bounds.absY))
		{
			args.Handled = true;
			OnClose?.Invoke();
		}
		else if (menuIconRect.PointInside((double)mouseX - Bounds.absX, (double)mouseY - Bounds.absY))
		{
			listMenu.Open();
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		listMenu.OnKeyDown(api, args);
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		listMenu.OnMouseUp(api, args);
		base.OnMouseUp(api, args);
		if (moving)
		{
			api.Gui.SetDialogPosition(baseComposer.DialogName, new Vec2i((int)Bounds.ParentBounds.fixedX, (int)Bounds.ParentBounds.fixedY));
		}
		moving = false;
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		listMenu.OnMouseMove(api, args);
		if (moving)
		{
			Bounds.ParentBounds.fixedX += (float)(args.X - movingStartPos.X) / RuntimeEnv.GUIScale;
			Bounds.ParentBounds.fixedY += (float)(args.Y - movingStartPos.Y) / RuntimeEnv.GUIScale;
			movingStartPos.Set(args.X, args.Y);
			Bounds.ParentBounds.CalcWorldBounds();
		}
	}

	public override void OnMouseDown(ICoreClientAPI api, MouseEvent args)
	{
		listMenu.OnMouseDown(api, args);
		if (movable && !args.Handled && IsPositionInside(args.X, args.Y))
		{
			moving = true;
			movingStartPos.Set(args.X, args.Y);
		}
		if (!args.Handled && !listMenu.IsPositionInside(args.X, args.Y))
		{
			listMenu.Close();
		}
	}

	public override void OnFocusLost()
	{
		base.OnFocusLost();
		listMenu.OnFocusLost();
	}

	internal void SetSelectedIndex(int selectedIndex)
	{
		listMenu.SetSelectedIndex(selectedIndex);
	}

	public override void Dispose()
	{
		base.Dispose();
		closeIconHoverTexture.Dispose();
		menuIconHoverTexture.Dispose();
		listMenu?.Dispose();
	}
}
