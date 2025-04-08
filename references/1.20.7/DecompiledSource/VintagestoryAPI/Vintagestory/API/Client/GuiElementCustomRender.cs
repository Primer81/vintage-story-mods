using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementCustomRender : GuiElement
{
	private RenderDelegateWithBounds onRender;

	/// <summary>
	/// Adds a custom drawing element to the GUI
	/// </summary>
	/// <param name="capi">The Client API</param>
	/// <param name="bounds">The bounds of the Element</param>
	/// <param name="onRender">The event fired when the object is drawn.</param>
	public GuiElementCustomRender(ICoreClientAPI capi, ElementBounds bounds, RenderDelegateWithBounds onRender)
		: base(capi, bounds)
	{
		this.onRender = onRender;
	}

	public override void ComposeElements(Context ctxStatic, ImageSurface surfaceStatic)
	{
		Bounds.CalcWorldBounds();
	}

	public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
	{
	}

	public override void RenderInteractiveElements(float deltaTime)
	{
		onRender(deltaTime, Bounds);
	}
}
