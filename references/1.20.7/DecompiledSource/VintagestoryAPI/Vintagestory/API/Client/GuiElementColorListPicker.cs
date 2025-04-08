using Cairo;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

/// <summary>
/// Creates a toggle button for the GUI.
/// </summary>
public class GuiElementColorListPicker : GuiElementElementListPickerBase<int>
{
	public GuiElementColorListPicker(ICoreClientAPI capi, int elem, ElementBounds bounds)
		: base(capi, elem, bounds)
	{
	}

	public override void DrawElement(int color, Context ctx, ImageSurface surface)
	{
		double[] dcolor = ColorUtil.ToRGBADoubles(color);
		ctx.SetSourceRGBA(dcolor[0], dcolor[1], dcolor[2], 1.0);
		GuiElement.RoundRectangle(ctx, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, 1.0);
		ctx.Fill();
	}
}
