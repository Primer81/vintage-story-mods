using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiHandbookItemStackPage : GuiHandbookPage
{
	public ItemStack Stack;

	public LoadedTexture Texture;

	public string TextCacheTitle;

	public string TextCacheAll;

	public float searchWeightOffset;

	public InventoryBase unspoilableInventory;

	public DummySlot dummySlot;

	private ElementBounds scissorBounds;

	private bool isDuplicate;

	public override string PageCode => PageCodeForStack(Stack);

	public override string CategoryCode => "stack";

	public override bool IsDuplicate => isDuplicate;

	public GuiHandbookItemStackPage(ICoreClientAPI capi, ItemStack stack)
	{
		Stack = stack;
		unspoilableInventory = new CreativeInventoryTab(1, "not-used", null);
		dummySlot = new DummySlot(stack, unspoilableInventory);
		TextCacheTitle = stack.GetName().ToSearchFriendly();
		TextCacheAll = stack.GetName() + " " + stack.GetDescription(capi.World, dummySlot);
		JsonObject attributes = stack.Collectible.Attributes;
		isDuplicate = attributes != null && (attributes["handbook"]?["isDuplicate"].AsBool()).GetValueOrDefault();
		searchWeightOffset = (stack.Collectible.Attributes?["handbook"]?["searchWeightOffset"].AsFloat()).GetValueOrDefault();
	}

	public static string PageCodeForStack(ItemStack stack)
	{
		if (stack.Attributes != null && stack.Attributes.Count > 0)
		{
			ITreeAttribute tree = stack.Attributes.Clone();
			string[] ignoredStackAttributes = GlobalConstants.IgnoredStackAttributes;
			foreach (string val in ignoredStackAttributes)
			{
				tree.RemoveAttribute(val);
			}
			tree.RemoveAttribute("durability");
			OrderedDictionary<string, IAttribute> sortedtree = tree.SortedCopy(recursive: true);
			if (tree.Count != 0)
			{
				string treeStr = TreeAttribute.ToJsonToken(sortedtree);
				return stack.Class.Name() + "-" + stack.Collectible.Code.ToShortString() + "-" + treeStr;
			}
		}
		return stack.Class.Name() + "-" + stack.Collectible.Code.ToShortString();
	}

	public void Recompose(ICoreClientAPI capi)
	{
		Texture?.Dispose();
		Texture = new TextTextureUtil(capi).GenTextTexture(Stack.GetName(), CairoFont.WhiteSmallText());
		scissorBounds = ElementBounds.FixedSize(50.0, 50.0);
		scissorBounds.ParentBounds = capi.Gui.WindowBounds;
	}

	public override void RenderListEntryTo(ICoreClientAPI capi, float dt, double x, double y, double cellWidth, double cellHeight)
	{
		float size = (float)GuiElement.scaled(25.0);
		float pad = (float)GuiElement.scaled(10.0);
		if (Texture == null)
		{
			Recompose(capi);
		}
		scissorBounds.fixedX = ((double)pad + x - (double)(size / 2f)) / (double)RuntimeEnv.GUIScale;
		scissorBounds.fixedY = (y - (double)(size / 2f)) / (double)RuntimeEnv.GUIScale;
		scissorBounds.CalcWorldBounds();
		if (!(scissorBounds.InnerWidth <= 0.0) && !(scissorBounds.InnerHeight <= 0.0))
		{
			capi.Render.PushScissor(scissorBounds, stacking: true);
			capi.Render.RenderItemstackToGui(dummySlot, x + (double)pad + (double)(size / 2f), y + (double)(size / 2f), 100.0, size, -1, shading: true, rotate: false, showStackSize: false);
			capi.Render.PopScissor();
			capi.Render.Render2DTexturePremultipliedAlpha(Texture.TextureId, x + (double)size + GuiElement.scaled(25.0), y + (double)(size / 4f) - GuiElement.scaled(3.0), Texture.Width, Texture.Height);
		}
	}

	public override void Dispose()
	{
		Texture?.Dispose();
		Texture = null;
	}

	public override void ComposePage(GuiComposer detailViewGui, ElementBounds textBounds, ItemStack[] allstacks, ActionConsumable<string> openDetailPageFor)
	{
		RichTextComponentBase[] cmps = GetPageText(detailViewGui.Api, allstacks, openDetailPageFor);
		detailViewGui.AddRichtext(cmps, textBounds, "richtext");
	}

	protected virtual RichTextComponentBase[] GetPageText(ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor)
	{
		return Stack.Collectible.GetBehavior<CollectibleBehaviorHandbookTextAndExtraInfo>()?.GetHandbookInfo(dummySlot, capi, allStacks, openDetailPageFor) ?? new RichTextComponentBase[0];
	}

	public override float GetTextMatchWeight(string searchText)
	{
		string title = TextCacheTitle;
		if (title.Equals(searchText, StringComparison.InvariantCultureIgnoreCase))
		{
			return searchWeightOffset + 3f;
		}
		if (title.StartsWith(searchText + " ", StringComparison.InvariantCultureIgnoreCase))
		{
			return searchWeightOffset + 2.75f + (float)Math.Max(0, 15 - title.Length) / 100f;
		}
		if (title.StartsWith(searchText, StringComparison.InvariantCultureIgnoreCase))
		{
			return searchWeightOffset + 2.5f + (float)Math.Max(0, 15 - title.Length) / 100f;
		}
		if (title.CaseInsensitiveContains(searchText))
		{
			return searchWeightOffset + 2f;
		}
		if (TextCacheAll.CaseInsensitiveContains(searchText))
		{
			return searchWeightOffset + 1f;
		}
		return 0f;
	}
}
