using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class GuiDialogJournal : GuiDialogGeneric
{
	private List<JournalEntry> journalitems = new List<JournalEntry>();

	private string[] pages;

	private int currentLoreItemIndex;

	private int page;

	private ElementBounds containerBounds;

	public override string ToggleKeyCombinationCode => null;

	public override bool PrefersUngrabbedMouse => false;

	public GuiDialogJournal(List<JournalEntry> journalitems, ICoreClientAPI capi)
		: base(Lang.Get("Journal"), capi)
	{
		this.journalitems = journalitems;
	}

	private void ComposeDialog()
	{
		_ = GuiStyle.ElementToDialogPadding;
		ElementBounds button = ElementBounds.Fixed(3.0, 3.0, 283.0, 25.0).WithFixedPadding(10.0, 2.0);
		ElementBounds lorelistBounds = ElementBounds.Fixed(0.0, 32.0, 285.0, 500.0);
		ElementBounds clippingBounds = lorelistBounds.ForkBoundingParent();
		ElementBounds insetBounds = lorelistBounds.FlatCopy().FixedGrow(6.0).WithFixedOffset(-3.0, -3.0);
		ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(lorelistBounds.fixedWidth + 7.0).WithFixedWidth(20.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(6.0);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		bgBounds.WithChildren(insetBounds, clippingBounds, scrollbarBounds);
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.LeftMiddle).WithFixedAlignmentOffset(5.0, 0.0);
		ClearComposers();
		Composers["loreList"] = capi.Gui.CreateCompo("loreList", dialogBounds).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(Lang.Get("Journal Inventory"), CloseIconPressed)
			.BeginChildElements(bgBounds)
			.AddInset(insetBounds, 3)
			.BeginClip(clippingBounds)
			.AddContainer(containerBounds = clippingBounds.ForkContainingChild(0.0, 0.0, 0.0, -3.0), "journallist");
		GuiElementContainer container = Composers["loreList"].GetContainer("journallist");
		CairoFont hoverFont = CairoFont.WhiteSmallText().Clone().WithColor(GuiStyle.ActiveButtonTextColor);
		for (int i = 0; i < journalitems.Count; i++)
		{
			int page = i;
			GuiElementTextButton elem = new GuiElementTextButton(capi, Lang.Get(journalitems[i].Title), CairoFont.WhiteSmallText(), hoverFont, () => onClickItem(page), button, EnumButtonStyle.Small);
			elem.SetOrientation(EnumTextOrientation.Left);
			container.Add(elem);
			button = button.BelowCopy();
		}
		if (journalitems.Count == 0)
		{
			string vtmlCode = "<i>" + Lang.Get("No lore found. Collect lore in the world to fill this list!.") + "</i>";
			container.Add(new GuiElementRichtext(capi, VtmlUtil.Richtextify(capi, vtmlCode, CairoFont.WhiteSmallText()), button));
		}
		Composers["loreList"].EndClip().AddVerticalScrollbar(OnNewScrollbarvalue, scrollbarBounds, "scrollbar").EndChildElements()
			.Compose();
		containerBounds.CalcWorldBounds();
		clippingBounds.CalcWorldBounds();
		Composers["loreList"].GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)containerBounds.fixedHeight);
	}

	private bool onClickItem(int page)
	{
		currentLoreItemIndex = page;
		this.page = 0;
		CairoFont font = CairoFont.WhiteDetailText().WithFontSize(17f).WithLineHeightMultiplier(1.149999976158142);
		new TextDrawUtil();
		StringBuilder fulltext = new StringBuilder();
		JournalEntry entry = journalitems[currentLoreItemIndex];
		for (int p = 0; p < entry.Chapters.Count; p++)
		{
			if (p > 0)
			{
				fulltext.AppendLine();
			}
			fulltext.Append(Lang.Get(entry.Chapters[p].Text));
		}
		pages = Paginate(fulltext.ToString(), font, GuiElement.scaled(629.0), GuiElement.scaled(450.0));
		double elemToDlgPad = GuiStyle.ElementToDialogPadding;
		ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 630.0, 450.0);
		ElementBounds dialogBounds = textBounds.ForkBoundingParent(elemToDlgPad, elemToDlgPad + 20.0, elemToDlgPad, elemToDlgPad + 30.0).WithAlignment(EnumDialogArea.LeftMiddle);
		dialogBounds.fixedX = 350.0;
		Composers["loreItem"] = capi.Gui.CreateCompo("loreItem", dialogBounds).AddShadedDialogBG(ElementBounds.Fill).AddDialogTitleBar(Lang.Get(journalitems[page].Title), CloseIconPressedLoreItem)
			.AddRichtext(pages[0], font, textBounds, "page")
			.AddDynamicText("1 / " + pages.Length, CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center), ElementBounds.Fixed(250.0, 500.0, 100.0, 30.0), "currentpage")
			.AddButton(Lang.Get("Previous Page"), OnPrevPage, ElementBounds.Fixed(17.0, 500.0, 100.0, 23.0).WithFixedPadding(10.0, 4.0), CairoFont.WhiteSmallishText())
			.AddButton(Lang.Get("Next Page"), OnNextPage, ElementBounds.Fixed(520.0, 500.0, 100.0, 23.0).WithFixedPadding(10.0, 4.0), CairoFont.WhiteSmallishText())
			.Compose();
		return true;
	}

	private string[] Paginate(string fullText, CairoFont font, double pageWidth, double pageHeight)
	{
		TextDrawUtil textUtil = new TextDrawUtil();
		Stack<string> lines = new Stack<string>();
		foreach (TextLine val in textUtil.Lineize(font, fullText, pageWidth).Reverse())
		{
			lines.Push(val.Text);
		}
		double lineheight = textUtil.GetLineHeight(font);
		int maxlinesPerPage = (int)(pageHeight / lineheight);
		List<string> pagesTemp = new List<string>();
		StringBuilder pageBuilder = new StringBuilder();
		while (lines.Count > 0)
		{
			int currentPageLines = 0;
			while (currentPageLines < maxlinesPerPage && lines.Count > 0)
			{
				string line = lines.Pop();
				string[] parts = line.Split(new string[1] { "___NEWPAGE___" }, 2, StringSplitOptions.None);
				if (parts.Length > 1)
				{
					pageBuilder.AppendLine(parts[0]);
					if (parts[1].Length > 0)
					{
						lines.Push(parts[1]);
					}
					break;
				}
				currentPageLines++;
				pageBuilder.AppendLine(line);
			}
			string pageText = pageBuilder.ToString().TrimEnd();
			if (pageText.Length > 0)
			{
				pagesTemp.Add(pageText);
			}
			pageBuilder.Clear();
		}
		return pagesTemp.ToArray();
	}

	private bool OnNextPage()
	{
		CairoFont font = CairoFont.WhiteDetailText().WithFontSize(17f).WithLineHeightMultiplier(1.149999976158142);
		page = Math.Min(pages.Length - 1, page + 1);
		Composers["loreItem"].GetRichtext("page").SetNewText(pages[page], font);
		Composers["loreItem"].GetDynamicText("currentpage").SetNewText(page + 1 + " / " + pages.Length);
		return true;
	}

	private bool OnPrevPage()
	{
		CairoFont font = CairoFont.WhiteDetailText().WithFontSize(17f).WithLineHeightMultiplier(1.149999976158142);
		page = Math.Max(0, page - 1);
		Composers["loreItem"].GetRichtext("page").SetNewText(pages[page], font);
		Composers["loreItem"].GetDynamicText("currentpage").SetNewText(page + 1 + " / " + pages.Length);
		return true;
	}

	public override void OnGuiOpened()
	{
		ComposeDialog();
	}

	private void CloseIconPressed()
	{
		TryClose();
	}

	private void CloseIconPressedLoreItem()
	{
		Composers.Remove("loreItem");
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = Composers["loreList"].GetContainer("journallist").Bounds;
		bounds.fixedY = 0f - value;
		bounds.CalcWorldBounds();
	}

	public void ReloadValues()
	{
	}
}
