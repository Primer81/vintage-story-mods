namespace Vintagestory.API.Client;

public static class GuiElementGameOverlyHelper
{
	/// <summary>
	/// Adds an overlay to the current GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the overlay.</param>
	/// <param name="backgroundColor">The background color of the overlay.</param>
	public static GuiComposer AddGameOverlay(this GuiComposer composer, ElementBounds bounds, double[] backgroundColor = null)
	{
		if (!composer.Composed)
		{
			if (backgroundColor == null)
			{
				backgroundColor = GuiStyle.DialogDefaultBgColor;
			}
			composer.AddStaticElement(new GuiElementGameOverlay(composer.Api, bounds, backgroundColor));
		}
		return composer;
	}
}
