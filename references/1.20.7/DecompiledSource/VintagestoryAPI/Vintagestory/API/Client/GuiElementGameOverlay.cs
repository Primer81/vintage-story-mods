using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementGameOverlay : GuiElement
{
	private double[] bgcolor;

	/// <summary>
	/// Creates a new overlay element.
	/// </summary>
	/// <param name="capi">The client API.</param>
	/// <param name="bounds">The bounds of the element.</param>
	/// <param name="bgcolor">The background color of the element.</param>
	public GuiElementGameOverlay(ICoreClientAPI capi, ElementBounds bounds, double[] bgcolor)
		: base(capi, bounds)
	{
		this.bgcolor = bgcolor;
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		ctx.SetSourceRGBA(bgcolor);
		ctx.Rectangle(Bounds.bgDrawX, Bounds.bgDrawY, Bounds.OuterWidth, Bounds.OuterHeight);
		ctx.FillPreserve();
		ShadePath(ctx);
	}
}
