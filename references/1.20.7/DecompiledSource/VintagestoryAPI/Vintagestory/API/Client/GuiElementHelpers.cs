using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public static class GuiElementHelpers
{
	public static GuiComposer AddImage(this GuiComposer composer, ElementBounds bounds, AssetLocation imageAsset)
	{
		if (!composer.Composed)
		{
			composer.AddStaticElement(new GuiElementImage(composer.Api, bounds, imageAsset));
		}
		return composer;
	}
}
