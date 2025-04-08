using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

public static class GuiComposerHelpers
{
	public static GuiComposer AddFlatList(this GuiComposer composer, ElementBounds bounds, Action<int> onleftClick = null, List<IFlatListItem> stacks = null, string key = null)
	{
		if (!composer.Composed)
		{
			composer.AddInteractiveElement(new GuiElementFlatList(composer.Api, bounds, onleftClick, stacks), key);
		}
		return composer;
	}

	public static GuiElementFlatList GetFlatList(this GuiComposer composer, string key)
	{
		return (GuiElementFlatList)composer.GetElement(key);
	}
}
