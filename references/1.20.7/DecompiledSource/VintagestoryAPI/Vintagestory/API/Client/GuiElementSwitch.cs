using System;
using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementSwitch : GuiElementControl
{
	private Action<bool> handler;

	private LoadedTexture onTexture;

	/// <summary>
	/// Wether the switch has been toggled to On
	/// </summary>
	public bool On;

	internal double unscaledPadding;

	internal double unscaledSize;

	public override bool Focusable => true;

	/// <summary>
	/// Creates a switch which can be toggled.
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="OnToggled">The event that happens when the switch is flipped.</param>
	/// <param name="bounds">The bounds of the element.</param>
	/// <param name="size">The size of the switch. (Default: 30)</param>
	/// <param name="padding">The padding on the outside of the switch (Default: 5)</param>
	public GuiElementSwitch(ICoreClientAPI capi, Action<bool> OnToggled, ElementBounds bounds, double size = 30.0, double padding = 4.0)
		: base(capi, bounds)
	{
		onTexture = new LoadedTexture(capi);
		bounds.fixedWidth = size;
		bounds.fixedHeight = size;
		unscaledPadding = padding;
		unscaledSize = size;
		handler = OnToggled;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		ctxStatic.SetSourceRGBA(0.0, 0.0, 0.0, 0.2);
		GuiElement.RoundRectangle(ctxStatic, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, 1.0);
		ctxStatic.Fill();
		EmbossRoundRectangleElement(ctxStatic, Bounds, inverse: true, 1, 1);
		genOnTexture();
	}

	private void genOnTexture()
	{
		double size = GuiElement.scaled(unscaledSize - 2.0 * unscaledPadding);
		ImageSurface surface = new ImageSurface(Format.Argb32, (int)size, (int)size);
		Context ctx = genContext(surface);
		GuiElement.RoundRectangle(ctx, 0.0, 0.0, size, size, 1.0);
		GuiElement.fillWithPattern(api, ctx, GuiElement.waterTextureName, nearestScalingFiler: false, preserve: true, 255, 0.5f);
		generateTexture(surface, ref onTexture);
		ctx.Dispose();
		surface.Dispose();
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (On)
		{
			double padding = GuiElement.scaled(unscaledPadding);
			api.Render.Render2DLoadedTexture(onTexture, (int)(Bounds.renderX + padding), (int)(Bounds.renderY + padding));
		}
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
		base.OnMouseDownOnElement(api, args);
		On = !On;
		handler?.Invoke(On);
		api.Gui.PlaySound("toggleswitch");
	}

	public override void OnKeyDown(ICoreClientAPI api, KeyEvent args)
	{
		if (base.HasFocus && (args.KeyCode == 49 || args.KeyCode == 51))
		{
			args.Handled = true;
			On = !On;
			handler?.Invoke(On);
			api.Gui.PlaySound("toggleswitch");
		}
	}

	/// <summary>
	/// Sets the value of the switch on or off.
	/// </summary>
	/// <param name="on">on == true.</param>
	public void SetValue(bool on)
	{
		On = on;
	}

	public override void Dispose()
	{
		base.Dispose();
		onTexture.Dispose();
	}
}
