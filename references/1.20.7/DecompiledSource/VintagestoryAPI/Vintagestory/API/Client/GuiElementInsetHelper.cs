namespace Vintagestory.API.Client;

public static class GuiElementInsetHelper
{
	/// <summary>
	/// Adds an inset to the current GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the inset.</param>
	/// <param name="depth">The depth of the inset.</param>
	/// <param name="brightness">The brightness of the inset.</param>
	public static GuiComposer AddInset(this GuiComposer composer, ElementBounds bounds, int depth = 4, float brightness = 0.85f)
	{
		if (!composer.Composed)
		{
			composer.AddStaticElement(new GuiElementInset(composer.Api, bounds, depth, brightness));
		}
		return composer;
	}
}
