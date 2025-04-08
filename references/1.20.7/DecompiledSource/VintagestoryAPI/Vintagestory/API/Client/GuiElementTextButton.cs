using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class GuiElementTextButton : GuiElementControl
{
	private GuiElementStaticText normalText;

	private GuiElementStaticText pressedText;

	private LoadedTexture normalTexture;

	private LoadedTexture activeTexture;

	private LoadedTexture hoverTexture;

	private LoadedTexture disabledTexture;

	private ActionConsumable onClick;

	private bool isOver;

	private EnumButtonStyle buttonStyle;

	private bool active;

	private bool currentlyMouseDownOnElement;

	public bool PlaySound = true;

	public static double Padding = 2.0;

	private double textOffsetY;

	public bool Visible = true;

	public override bool Focusable => true;

	public string Text
	{
		get
		{
			return normalText.GetText();
		}
		set
		{
			normalText.Text = value;
			pressedText.Text = value;
		}
	}

	/// <summary>
	/// Creates a button with text.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="text">The text of the button.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="hoverFont">The font of the text when the player is hovering over the button.</param>
	/// <param name="onClick">The event fired when the button is clicked.</param>
	/// <param name="bounds">The bounds of the button.</param>
	/// <param name="style">The style of the button.</param>
	public GuiElementTextButton(ICoreClientAPI capi, string text, CairoFont font, CairoFont hoverFont, ActionConsumable onClick, ElementBounds bounds, EnumButtonStyle style = EnumButtonStyle.Normal)
		: base(capi, bounds)
	{
		hoverTexture = new LoadedTexture(capi);
		activeTexture = new LoadedTexture(capi);
		normalTexture = new LoadedTexture(capi);
		disabledTexture = new LoadedTexture(capi);
		buttonStyle = style;
		normalText = new GuiElementStaticText(capi, text, EnumTextOrientation.Center, bounds.CopyOnlySize(), font);
		normalText.AutoBoxSize(onlyGrow: true);
		pressedText = new GuiElementStaticText(capi, text, EnumTextOrientation.Center, bounds.CopyOnlySize(), hoverFont);
		this.onClick = onClick;
	}

	/// <summary>
	/// Sets the orientation of the text both when clicked and when idle.
	/// </summary>
	/// <param name="orientation">The orientation of the text.</param>
	public void SetOrientation(EnumTextOrientation orientation)
	{
		normalText.orientation = orientation;
		pressedText.orientation = orientation;
	}

	public override void BeforeCalcBounds()
	{
		normalText.AutoBoxSize(onlyGrow: true);
		Bounds.fixedWidth = normalText.Bounds.fixedWidth;
		Bounds.fixedHeight = normalText.Bounds.fixedHeight;
		pressedText.Bounds = normalText.Bounds.CopyOnlySize();
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		Bounds.CalcWorldBounds();
		normalText.Bounds.CalcWorldBounds();
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context ctx = genContext(surface);
		ComposeButton(ctx, surface);
		generateTexture(surface, ref normalTexture);
		ctx.Clear();
		if (buttonStyle != 0)
		{
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
			ctx.Rectangle(0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
			ctx.Fill();
		}
		pressedText.Bounds.fixedY += textOffsetY;
		pressedText.ComposeElements(ctx, surface);
		pressedText.Bounds.fixedY -= textOffsetY;
		generateTexture(surface, ref activeTexture);
		ctx.Clear();
		if (buttonStyle != 0)
		{
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
			ctx.Rectangle(0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
			ctx.Fill();
		}
		generateTexture(surface, ref hoverTexture);
		ctx.Dispose();
		surface.Dispose();
		surface = new ImageSurface(Format.Argb32, 2, 2);
		ctx = genContext(surface);
		if (buttonStyle != 0)
		{
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.4);
			ctx.Rectangle(0.0, 0.0, 2.0, 2.0);
			ctx.Fill();
		}
		generateTexture(surface, ref disabledTexture);
		ctx.Dispose();
		surface.Dispose();
	}

	private void ComposeButton(Context ctx, ImageSurface surface)
	{
		double embossHeight = GuiElement.scaled(2.5);
		if (buttonStyle == EnumButtonStyle.Normal || buttonStyle == EnumButtonStyle.Small)
		{
			embossHeight = GuiElement.scaled(1.5);
		}
		if (buttonStyle != 0)
		{
			GuiElement.Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight);
			ctx.SetSourceRGBA(23.0 / 85.0, 52.0 / 255.0, 12.0 / 85.0, 0.8);
			ctx.Fill();
		}
		if (buttonStyle == EnumButtonStyle.MainMenu)
		{
			GuiElement.Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth, embossHeight);
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
			ctx.Fill();
		}
		if (buttonStyle == EnumButtonStyle.Normal || buttonStyle == EnumButtonStyle.Small)
		{
			GuiElement.Rectangle(ctx, 0.0, 0.0, Bounds.OuterWidth - embossHeight, embossHeight);
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
			ctx.Fill();
			GuiElement.Rectangle(ctx, 0.0, 0.0 + embossHeight, embossHeight, Bounds.OuterHeight - embossHeight);
			ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.15);
			ctx.Fill();
		}
		surface.BlurPartial(2.0, 5);
		FontExtents fontex = normalText.Font.GetFontExtents();
		TextExtents textex = normalText.Font.GetTextExtents(normalText.GetText());
		double resetY = 0.0 - fontex.Ascent - textex.YBearing;
		textOffsetY = (resetY + (normalText.Bounds.InnerHeight + textex.YBearing) / 2.0) / (double)RuntimeEnv.GUIScale;
		normalText.Bounds.fixedY += textOffsetY;
		normalText.ComposeElements(ctx, surface);
		normalText.Bounds.fixedY -= textOffsetY;
		Bounds.CalcWorldBounds();
		if (buttonStyle == EnumButtonStyle.MainMenu)
		{
			GuiElement.Rectangle(ctx, 0.0, 0.0 + Bounds.OuterHeight - embossHeight, Bounds.OuterWidth, embossHeight);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
			ctx.Fill();
		}
		if (buttonStyle == EnumButtonStyle.Normal || buttonStyle == EnumButtonStyle.Small)
		{
			GuiElement.Rectangle(ctx, 0.0 + embossHeight, 0.0 + Bounds.OuterHeight - embossHeight, Bounds.OuterWidth - 2.0 * embossHeight, embossHeight);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
			ctx.Fill();
			GuiElement.Rectangle(ctx, 0.0 + Bounds.OuterWidth - embossHeight, 0.0, embossHeight, Bounds.OuterHeight);
			ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
			ctx.Fill();
		}
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (Visible)
		{
			api.Render.Render2DTexturePremultipliedAlpha(normalTexture.TextureId, Bounds);
			if (!enabled)
			{
				api.Render.Render2DTexturePremultipliedAlpha(disabledTexture.TextureId, Bounds);
			}
			else if (active || currentlyMouseDownOnElement)
			{
				api.Render.Render2DTexturePremultipliedAlpha(activeTexture.TextureId, Bounds);
			}
			else if (isOver)
			{
				api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds);
			}
		}
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (!Visible || !base.HasFocus || args.KeyCode != 49)
		{
			return;
		}
		args.Handled = true;
		if (enabled)
		{
			if (PlaySound)
			{
				api.Gui.PlaySound("menubutton_press");
			}
			args.Handled = onClick();
		}
	}

	public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		bool num = isOver;
		setIsOver();
		if (!num && isOver && PlaySound)
		{
			api.Gui.PlaySound("menubutton");
		}
	}

	protected void setIsOver()
	{
		isOver = Visible && enabled && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY);
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (Visible && enabled)
		{
			base.OnMouseDownOnElement(api, args);
			currentlyMouseDownOnElement = true;
			if (PlaySound)
			{
				api.Gui.PlaySound("menubutton_down");
			}
			setIsOver();
		}
	}

	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		if (Visible)
		{
			if (currentlyMouseDownOnElement && !Bounds.PointInside(args.X, args.Y) && !active && PlaySound)
			{
				api.Gui.PlaySound("menubutton_up");
			}
			base.OnMouseUp(api, args);
			currentlyMouseDownOnElement = false;
		}
	}

	public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (enabled && currentlyMouseDownOnElement && Bounds.PointInside(args.X, args.Y) && (args.Button == EnumMouseButton.Left || args.Button == EnumMouseButton.Right))
		{
			args.Handled = onClick();
		}
		currentlyMouseDownOnElement = false;
	}

	/// <summary>
	/// Sets the button as active or inactive.
	/// </summary>
	/// <param name="active">Active == clickable</param>
	public void SetActive(bool active)
	{
		this.active = active;
	}

	public override void Dispose()
	{
		base.Dispose();
		hoverTexture?.Dispose();
		activeTexture?.Dispose();
		pressedText?.Dispose();
		disabledTexture?.Dispose();
		normalTexture?.Dispose();
	}
}
