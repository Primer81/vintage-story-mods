using System;
using Cairo;

namespace Vintagestory.API.Client;

/// <summary>
/// Creates a toggle button for the GUI.
/// </summary>
public class GuiElementToggleButton : GuiElementTextBase
{
	private Action<bool> handler;

	/// <summary>
	/// Is this button toggleable?
	/// </summary>
	public bool Toggleable;

	/// <summary>
	/// Is this button on?
	/// </summary>
	public bool On;

	private LoadedTexture releasedTexture;

	private LoadedTexture pressedTexture;

	private LoadedTexture hoverTexture;

	private int unscaledDepth = 4;

	private string icon;

	private double pressedYOffset;

	private double nonPressedYOffset;

	/// <summary>
	/// Is this element capable of being in the focus?
	/// </summary>
	public override bool Focusable => true;

	/// <summary>
	/// Constructor for the button
	/// </summary>
	/// <param name="capi">The core client API.</param>
	/// <param name="icon">The icon name</param>
	/// <param name="text">The text for the button.</param>
	/// <param name="font">The font of the text.</param>
	/// <param name="OnToggled">The action that happens when the button is toggled.</param>
	/// <param name="bounds">The bounding box of the button.</param>
	/// <param name="toggleable">Can the button be toggled on or off?</param>
	public GuiElementToggleButton(ICoreClientAPI capi, string icon, string text, CairoFont font, Action<bool> OnToggled, ElementBounds bounds, bool toggleable = false)
		: base(capi, text, font, bounds)
	{
		releasedTexture = new LoadedTexture(capi);
		pressedTexture = new LoadedTexture(capi);
		hoverTexture = new LoadedTexture(capi);
		handler = OnToggled;
		Toggleable = toggleable;
		this.icon = icon;
	}

	/// <summary>
	/// Composes the element in both the pressed, and released states.
	/// </summary>
	/// <param name="ctx">The context of the element.</param>
	/// <param name="surface">The surface of the element.</param>
	/// <remarks>Neither the context, nor the surface is used in this function.</remarks>
	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		ComposeReleasedButton();
		ComposePressedButton();
	}

	private void ComposeReleasedButton()
	{
		double depth = GuiElement.scaled(unscaledDepth);
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context ctx = genContext(surface);
		ctx.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
		GuiElement.RoundRectangle(ctx, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, GuiStyle.ElementBGRadius);
		ctx.FillPreserve();
		ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
		ctx.Fill();
		EmbossRoundRectangleElement(ctx, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, inverse: false, (int)depth);
		double height = GetMultilineTextHeight();
		nonPressedYOffset = (Bounds.InnerHeight - height) / 2.0 - 1.0;
		DrawMultilineTextAt(ctx, Bounds.absPaddingX, Bounds.absPaddingY + nonPressedYOffset, EnumTextOrientation.Center);
		if (icon != null && icon.Length > 0)
		{
			api.Gui.Icons.DrawIcon(ctx, icon, Bounds.absPaddingX + GuiElement.scaled(4.0), Bounds.absPaddingY + GuiElement.scaled(4.0), Bounds.InnerWidth - GuiElement.scaled(9.0), Bounds.InnerHeight - GuiElement.scaled(9.0), Font.Color);
		}
		generateTexture(surface, ref releasedTexture);
		ctx.Dispose();
		surface.Dispose();
	}

	private void ComposePressedButton()
	{
		double depth = GuiElement.scaled(unscaledDepth);
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		Context ctx = genContext(surface);
		ctx.SetSourceRGB(GuiStyle.DialogDefaultBgColor[0], GuiStyle.DialogDefaultBgColor[1], GuiStyle.DialogDefaultBgColor[2]);
		GuiElement.RoundRectangle(ctx, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, GuiStyle.ElementBGRadius);
		ctx.FillPreserve();
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.1);
		ctx.Fill();
		EmbossRoundRectangleElement(ctx, 0.0, 0.0, Bounds.OuterWidth, Bounds.OuterHeight, inverse: true, (int)depth);
		double height = GetMultilineTextHeight();
		pressedYOffset = (Bounds.InnerHeight - height) / 2.0 + depth / 2.0 - 1.0;
		DrawMultilineTextAt(ctx, Bounds.absPaddingX, Bounds.absPaddingY + pressedYOffset, EnumTextOrientation.Center);
		if (icon != null && icon.Length > 0)
		{
			ctx.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
			api.Gui.Icons.DrawIcon(ctx, icon, Bounds.absPaddingX + GuiElement.scaled(4.0), Bounds.absPaddingY + GuiElement.scaled(4.0), Bounds.InnerWidth - GuiElement.scaled(8.0), Bounds.InnerHeight - GuiElement.scaled(8.0), GuiStyle.DialogDefaultTextColor);
		}
		generateTexture(surface, ref pressedTexture);
		ctx.Dispose();
		surface.Dispose();
		surface = new ImageSurface(Format.Argb32, (int)Bounds.OuterWidth, (int)Bounds.OuterHeight);
		ctx = genContext(surface);
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		ctx.Fill();
		double[] prevcolor = Font.Color;
		Font.Color = GuiStyle.ActiveButtonTextColor;
		DrawMultilineTextAt(ctx, Bounds.absPaddingX, 0.0, EnumTextOrientation.Center);
		if (icon != null && icon.Length > 0)
		{
			ctx.SetSourceRGBA(GuiStyle.DialogDefaultTextColor);
			api.Gui.Icons.DrawIcon(ctx, icon, Bounds.absPaddingX + GuiElement.scaled(4.0), Bounds.absPaddingY + GuiElement.scaled(4.0), Bounds.InnerWidth - GuiElement.scaled(8.0), Bounds.InnerHeight - GuiElement.scaled(8.0), GuiStyle.DialogDefaultTextColor);
		}
		Font.Color = prevcolor;
		generateTexture(surface, ref hoverTexture);
		ctx.Dispose();
		surface.Dispose();
	}

	/// <summary>
	/// Renders the button.
	/// </summary>
	/// <param name="deltaTime">The time elapsed.</param>
	public override void RenderInteractiveElements(float deltaTime)
	{
		api.Render.Render2DTexturePremultipliedAlpha(On ? pressedTexture.TextureId : releasedTexture.TextureId, Bounds);
		if (icon == null && Bounds.PointInside(api.Input.MouseX, api.Input.MouseY))
		{
			api.Render.Render2DTexturePremultipliedAlpha(hoverTexture.TextureId, Bounds.renderX, Bounds.renderY + (On ? pressedYOffset : nonPressedYOffset), Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		}
	}

	/// <summary>
	/// Handles the mouse button press while the mouse is on this button.
	/// </summary>
	/// <param name="api">The client API</param>
	/// <param name="args">The mouse event arguments.</param>
	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		On = !On;
		handler?.Invoke(On);
		api.Gui.PlaySound("toggleswitch");
	}

	/// <summary>
	/// Handles the mouse button release while the mouse is on this button.
	/// </summary>
	/// <param name="api">The client API</param>
	/// <param name="args">The mouse event arguments</param>
	public override void OnMouseUpOnElement(ICoreClientAPI api, MouseEvent args)
	{
		if (!Toggleable)
		{
			On = false;
		}
	}

	/// <summary>
	/// Handles the event fired when the mouse is released.
	/// </summary>
	/// <param name="api">The client API</param>
	/// <param name="args">Mouse event arguments</param>
	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		if (!Toggleable)
		{
			On = false;
		}
		base.OnMouseUp(api, args);
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (base.HasFocus && args.KeyCode == 49)
		{
			args.Handled = true;
			On = !On;
			handler?.Invoke(On);
			api.Gui.PlaySound("toggleswitch");
		}
	}

	/// <summary>
	/// Sets the value of the button.
	/// </summary>
	/// <param name="on">Am I on or off?</param>
	public void SetValue(bool on)
	{
		On = on;
	}

	/// <summary>
	/// Disposes of the button.
	/// </summary>
	public override void Dispose()
	{
		base.Dispose();
		releasedTexture.Dispose();
		pressedTexture.Dispose();
		hoverTexture.Dispose();
	}
}
