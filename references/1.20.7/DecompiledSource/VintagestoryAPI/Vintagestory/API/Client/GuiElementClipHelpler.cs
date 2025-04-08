namespace Vintagestory.API.Client;

public static class GuiElementClipHelpler
{
	/// <summary>
	/// Add a clip area. Thhis select an area to be rendered, where anything outside will be invisible. Useful for scrollable content. Can be called multiple times, to reduce the render area further, but needs an equal amount of calls to EndClip()
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the object.</param>
	public static GuiComposer BeginClip(this GuiComposer composer, ElementBounds bounds)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementClip(composer.Api, clip: true, bounds));
			composer.InsideClipBounds = bounds;
			composer.BeginChildElements();
		}
		return composer;
	}

	/// <summary>
	/// Remove a previously added clip area.
	/// </summary>
	public static GuiComposer EndClip(this GuiComposer composer)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementClip(composer.Api, clip: false, ElementBounds.Empty));
			composer.InsideClipBounds = null;
			composer.EndChildElements();
		}
		return composer;
	}
}
