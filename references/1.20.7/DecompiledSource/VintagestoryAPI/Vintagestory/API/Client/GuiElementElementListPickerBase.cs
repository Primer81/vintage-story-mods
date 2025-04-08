using System;
using Cairo;

namespace Vintagestory.API.Client;

/// <summary>
/// Creates a toggle button for the GUI.
/// </summary>
public abstract class GuiElementElementListPickerBase<T> : GuiElementControl
{
	public Action<bool> handler;

	/// <summary>
	/// Is this button on?
	/// </summary>
	public bool On;

	private LoadedTexture activeTexture;

	private T elem;

	public bool ShowToolTip;

	private GuiElementHoverText hoverText;

	public string TooltipText
	{
		set
		{
			hoverText.SetNewText(value);
		}
	}

	/// <summary>
	/// Is this element capable of being in the focus?
	/// </summary>
	public override bool Focusable => true;

	/// <summary>
	/// Constructor for the button
	/// </summary>
	/// <param name="capi">The core client API.</param>
	/// <param name="elem"></param>
	/// <param name="bounds">The bounding box of the button.</param>
	public GuiElementElementListPickerBase(ICoreClientAPI capi, T elem, ElementBounds bounds)
		: base(capi, bounds)
	{
		activeTexture = new LoadedTexture(capi);
		this.elem = elem;
		hoverText = new GuiElementHoverText(capi, "", CairoFont.WhiteSmallText(), 200, Bounds.CopyOnlySize());
		hoverText.Bounds.ParentBounds = bounds;
		hoverText.SetAutoWidth(on: true);
		bounds.ChildBounds.Add(hoverText.Bounds);
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
		DrawElement(elem, ctx, surface);
		ComposeActiveButton();
		hoverText.ComposeElements(ctx, surface);
	}

	public abstract void DrawElement(T elem, Context ctx, ImageSurface surface);

	private void ComposeActiveButton()
	{
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)Bounds.InnerWidth + 6, (int)Bounds.InnerHeight + 6);
		Context context = genContext(surface);
		context.SetSourceRGBA(0.0, 0.0, 0.0, 0.0);
		context.Paint();
		context.SetSourceRGBA(1.0, 1.0, 1.0, 0.65);
		GuiElement.RoundRectangle(context, 3.0, 3.0, Bounds.InnerWidth + 1.0, Bounds.InnerHeight + 1.0, 1.0);
		context.LineWidth = 2.0;
		context.Stroke();
		generateTexture(surface, ref activeTexture);
		context.Dispose();
		surface.Dispose();
	}

	/// <summary>
	/// Renders the button.
	/// </summary>
	/// <param name="deltaTime">The time elapsed.</param>
	public override void RenderInteractiveElements(float deltaTime)
	{
		if (On)
		{
			api.Render.Render2DTexturePremultipliedAlpha(activeTexture.TextureId, Bounds.renderX - 3.0, Bounds.renderY - 3.0, activeTexture.Width, activeTexture.Height);
		}
		if (ShowToolTip)
		{
			hoverText.RenderInteractiveElements(deltaTime);
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
	}

	/// <summary>
	/// Handles the event fired when the mouse is released.
	/// </summary>
	/// <param name="api">The client API</param>
	/// <param name="args">Mouse event arguments</param>
	public override void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseUp(api, args);
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
		activeTexture.Dispose();
		hoverText?.Dispose();
	}
}
