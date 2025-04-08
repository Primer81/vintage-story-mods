using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public static class GuiElementImageBackgroundHelper
{
	/// <summary>
	/// Adds a background to the current GUI
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="bounds">The bounds of the background</param>
	/// <param name="textureLoc">The name of the background texture.</param>
	/// <param name="brightness">The brightness of the texture (default: 1f)</param>
	/// <param name="alpha"></param>
	/// <param name="scale"></param>
	public static GuiComposer AddImageBG(this GuiComposer composer, ElementBounds bounds, AssetLocation textureLoc, float brightness = 1f, float alpha = 1f, float scale = 1f)
	{
		if (!composer.Composed)
		{
			composer.AddStaticElement(new GuiElementImageBackground(composer.Api, bounds, textureLoc, brightness, alpha, scale));
		}
		return composer;
	}
}
