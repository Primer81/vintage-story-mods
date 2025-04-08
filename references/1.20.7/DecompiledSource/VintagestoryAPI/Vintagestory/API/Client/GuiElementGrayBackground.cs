using Cairo;

namespace Vintagestory.API.Client;

public class GuiElementGrayBackground : GuiElement
{
	/// <summary>
	/// Creates a new gray background.
	/// </summary>
	/// <param name="capi">The client API</param>
	/// <param name="bounds">The bouds of the GUI Element.</param>
	public GuiElementGrayBackground(ICoreClientAPI capi, ElementBounds bounds)
		: base(capi, bounds)
	{
	}

	public override void ComposeElements(Context ctx, ImageSurface surface)
	{
		Bounds.CalcWorldBounds();
		ctx.SetSourceRGBA(0.0, 0.0, 0.0, 0.35);
		ctx.Fill();
		ctx.Paint();
	}
}
