using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementCustomDraw : GuiElement
{
	private DrawDelegateWithBounds OnDraw;

	private bool interactive;

	private int texId;

	/// <summary>
	/// Adds a custom drawing element to the GUI
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="bounds">The bounds of the Element</param>
	/// <param name="OnDraw">The event fired when the object is drawn.</param>
	/// <param name="interactive">Whether or not the element is able to be interacted with (Default: false)</param>
	public GuiElementCustomDraw(ICoreClientAPI capi, ElementBounds bounds, DrawDelegateWithBounds OnDraw, bool interactive = false)
		: base(capi, bounds)
	{
		this.OnDraw = OnDraw;
		this.interactive = interactive;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		Bounds.CalcWorldBounds();
		if (!interactive)
		{
			OnDraw(ctxStatic, surfaceStatic, Bounds);
		}
		else
		{
			Redraw();
		}
	}

	/// <summary>
	/// Redraws the element.
	/// </summary>
	public void Redraw()
	{
		ImageSurface surface = new ImageSurface(Format.Argb32, Bounds.OuterWidthInt, Bounds.OuterHeightInt);
		Context ctx = new Context(surface);
		OnDraw(ctx, surface, Bounds);
		generateTexture(surface, ref texId);
		ctx.Dispose();
		surface.Dispose();
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		if (interactive)
		{
			api.Render.Render2DTexture(texId, Bounds);
		}
	}
}
