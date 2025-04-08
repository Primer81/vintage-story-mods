using System;

namespace Vintagestory.API.Client;

public static class GuiElementDynamicTextHelper
{
	/// <summary>
	/// Adds dynamic text to the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="text"></param>
	/// <param name="font"></param>
	/// <param name="bounds"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public static GuiComposer AddDynamicText(this GuiComposer composer, string text, CairoFont font, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			GuiElementDynamicText elem = new GuiElementDynamicText(composer.Api, text, font, bounds);
			composer.AddInteractiveElement(elem, key);
		}
		return composer;
	}

	[Obsolete("Use AddDymiacText without orientation attribute, that can be configured through the font")]
	public static GuiComposer AddDynamicText(this GuiComposer composer, string text, CairoFont font, EnumTextOrientation orientation, ElementBounds bounds, string key = null)
	{
		if (!composer.Composed)
		{
			font = font.WithOrientation(orientation);
			GuiElementDynamicText elem = new GuiElementDynamicText(composer.Api, text, font, bounds);
			composer.AddInteractiveElement(elem, key);
		}
		return composer;
	}

	/// <summary>
	/// Gets the Dynamic Text by name from the GUI.
	/// </summary>
	/// <param name="composer"></param>
	/// <param name="key">The name of the element.</param>
	public static GuiElementDynamicText GetDynamicText(this GuiComposer composer, string key)
	{
		return (GuiElementDynamicText)composer.GetElement(key);
	}
}
