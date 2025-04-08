namespace Vintagestory.API.Client;

public static class GuiElementGrayBackgroundHelpber
{
	/// <summary>
	/// Adds a gray background to the current GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the backgrounds.</param>
	public static GuiComposer AddGrayBG(this GuiComposer composer, ElementBounds bounds)
	{
		if (!composer.Composed)
		{
			composer.AddStaticElement(new GuiElementGrayBackground(composer.Api, bounds));
		}
		return composer;
	}
}
