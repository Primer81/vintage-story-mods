using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class GuiHandbookGroupedItemstackPage : GuiHandbookItemStackPage
{
	public List<ItemStack> Stacks = new List<ItemStack>();

	public string Name;

	public override string PageCode => Name;

	public GuiHandbookGroupedItemstackPage(ICoreClientAPI capi, ItemStack stack)
		: base(capi, null)
	{
	}

	public override void RenderListEntryTo(ICoreClientAPI capi, float dt, double x, double y, double cellWidth, double cellHeight)
	{
		float size = (float)GuiElement.scaled(25.0);
		float pad = (float)GuiElement.scaled(10.0);
		int index = (int)(capi.ElapsedMilliseconds / 1000 % Stacks.Count);
		dummySlot.Itemstack = Stacks[index];
		capi.Render.RenderItemstackToGui(dummySlot, x + (double)pad + (double)(size / 2f), y + (double)(size / 2f), 100.0, size, -1, shading: true, rotate: false, showStackSize: false);
		if (Texture == null)
		{
			Texture = new TextTextureUtil(capi).GenTextTexture(Name, CairoFont.WhiteSmallText());
		}
		capi.Render.Render2DTexturePremultipliedAlpha(Texture.TextureId, x + (double)size + GuiElement.scaled(25.0), y + (double)(size / 4f) - 3.0, Texture.Width, Texture.Height);
	}

	protected override RichTextComponentBase[] GetPageText(ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor)
	{
		dummySlot.Itemstack = Stacks[0];
		return Stacks[0].Collectible.GetBehavior<CollectibleBehaviorHandbookTextAndExtraInfo>().GetHandbookInfo(dummySlot, capi, allStacks, openDetailPageFor);
	}
}
