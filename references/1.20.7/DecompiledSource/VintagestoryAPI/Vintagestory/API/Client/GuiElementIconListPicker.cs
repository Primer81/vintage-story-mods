using Cairo;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

/// <summary>
/// Creates a toggle button for the GUI.
/// </summary>
public class GuiElementIconListPicker : GuiElementElementListPickerBase<string>
{
	public GuiElementIconListPicker(ICoreClientAPI capi, string elem, ElementBounds bounds)
		: base(capi, elem, bounds)
	{
	}

	public override void DrawElement(string icon, Context ctx, ImageSurface surface)
	{
		ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.2);
		GuiElement.RoundRectangle(ctx, Bounds.drawX, Bounds.drawY, Bounds.InnerWidth, Bounds.InnerHeight, 1.0);
		ctx.Fill();
		api.Gui.Icons.DrawIcon(ctx, "wp" + icon.UcFirst(), Bounds.drawX + 2.0, Bounds.drawY + 2.0, Bounds.InnerWidth - 4.0, Bounds.InnerHeight - 4.0, new double[4] { 1.0, 1.0, 1.0, 1.0 });
	}
}
