using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogHandbook : GuiDialog
{
	protected Dictionary<string, int> pageNumberByPageCode = new Dictionary<string, int>();

	internal List<GuiHandbookPage> allHandbookPages = new List<GuiHandbookPage>();

	protected List<IFlatListItem> shownHandbookPages = new List<IFlatListItem>();

	protected List<string> categoryCodes = new List<string>();

	protected Stack<BrowseHistoryElement> browseHistory = new Stack<BrowseHistoryElement>();

	protected string currentSearchText;

	protected GuiComposer overviewGui;

	protected GuiComposer detailViewGui;

	protected bool loadingPagesAsync;

	protected double listHeight = 500.0;

	protected GuiTab[] tabs;

	public string currentCatgoryCode;

	private OnCreatePagesDelegate createPageHandlerAsync;

	private OnComposePageDelegate composePageHandler;

	public override double DrawOrder => 0.2;

	public override string ToggleKeyCombinationCode => "handbook";

	public virtual string DialogTitle => "";

	public override bool PrefersUngrabbedMouse => true;

	public GuiDialogHandbook(ICoreClientAPI capi, OnCreatePagesDelegate createPageHandlerAsync, OnComposePageDelegate composePageHandler)
		: base(capi)
	{
		this.createPageHandlerAsync = createPageHandlerAsync;
		this.composePageHandler = composePageHandler;
		capi.Settings.AddWatcher<float>("guiScale", delegate
		{
			initOverviewGui();
			FilterItems();
			foreach (GuiHandbookPage shownHandbookPage in shownHandbookPages)
			{
				shownHandbookPage.Dispose();
			}
		});
		loadEntries();
	}

	protected virtual void loadEntries()
	{
		capi.Logger.VerboseDebug("Starting initialising handbook");
		pageNumberByPageCode.Clear();
		shownHandbookPages.Clear();
		allHandbookPages.Clear();
		HashSet<string> codes = initCustomPages();
		codes.Add("stack");
		categoryCodes = codes.ToList();
		loadingPagesAsync = true;
		TyronThreadPool.QueueTask(LoadPages_Async);
		initOverviewGui();
		capi.Logger.VerboseDebug("Done creating handbook index GUI");
	}

	public void initOverviewGui()
	{
		ElementBounds searchFieldBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding - 2.0, 45.0, 300.0, 30.0);
		ElementBounds stackListBounds = ElementBounds.Fixed(0.0, 0.0, 500.0, listHeight).FixedUnder(searchFieldBounds, 5.0);
		ElementBounds clipBounds = stackListBounds.ForkBoundingParent();
		ElementBounds insetBounds = stackListBounds.FlatCopy().FixedGrow(6.0).WithFixedOffset(-3.0, -3.0);
		ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(3.0 + stackListBounds.fixedWidth + 7.0).WithFixedWidth(20.0);
		ElementBounds closeButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clipBounds, 18.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(20.0, 4.0)
			.WithFixedAlignmentOffset(2.0, 0.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		bgBounds.WithChildren(insetBounds, stackListBounds, scrollbarBounds, closeButtonBounds);
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.None).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 70.0);
		ElementBounds tabBounds = ElementBounds.Fixed(-200.0, 35.0, 200.0, 545.0);
		ElementBounds backButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clipBounds, 15.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(20.0, 4.0)
			.WithFixedAlignmentOffset(-6.0, 3.0);
		tabs = genTabs(out var curTab);
		overviewGui = capi.Gui.CreateCompo("handbook-overview", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
			.AddIf(tabs.Length != 0)
			.AddVerticalTabs(tabs, tabBounds, OnTabClicked, "verticalTabs")
			.EndIf()
			.AddTextInput(searchFieldBounds, FilterItemsBySearchText, CairoFont.WhiteSmallishText(), "searchField")
			.BeginChildElements(bgBounds)
			.BeginClip(clipBounds)
			.AddInset(insetBounds, 3)
			.AddFlatList(stackListBounds, onLeftClickListElement, shownHandbookPages, "stacklist")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarvalueOverviewPage, scrollbarBounds, "scrollbar")
			.AddIf(capi.IsSinglePlayer && !capi.OpenedToLan)
			.AddToggleButton(Lang.Get("Pause game"), CairoFont.WhiteDetailText(), onTogglePause, ElementBounds.Fixed(360.0, -15.0, 100.0, 22.0), "pausegame")
			.EndIf()
			.AddSmallButton(Lang.Get("general-back"), OnButtonBack, backButtonBounds, EnumButtonStyle.Normal, "backButton")
			.AddSmallButton(Lang.Get("Close Handbook"), OnButtonClose, closeButtonBounds)
			.EndChildElements()
			.Compose();
		overviewGui.GetScrollbar("scrollbar").SetHeights((float)listHeight, (float)overviewGui.GetFlatList("stacklist").insideBounds.fixedHeight);
		overviewGui.GetTextInput("searchField").SetPlaceHolderText(Lang.Get("Search..."));
		if (tabs.Length != 0)
		{
			overviewGui.GetVerticalTab("verticalTabs").SetValue(curTab, triggerHandler: false);
			currentCatgoryCode = (tabs[curTab] as HandbookTab).CategoryCode;
		}
		overviewGui.GetToggleButton("pausegame")?.SetValue(!capi.Settings.Bool["noHandbookPause"]);
		overviewGui.FocusElement(overviewGui.GetTextInput("searchField").TabIndex);
	}

	protected virtual void onTogglePause(bool on)
	{
		capi.PauseGame(on);
		capi.Settings.Bool["noHandbookPause"] = !on;
	}

	protected virtual GuiTab[] genTabs(out int curTab)
	{
		curTab = 0;
		return new GuiTab[0];
	}

	protected virtual void OnTabClicked(int index, GuiTab tab)
	{
		selectTab((tab as HandbookTab).CategoryCode);
	}

	public void selectTab(string code)
	{
		currentCatgoryCode = code;
		FilterItems();
		capi.Settings.String["currentHandbookCategoryCode"] = currentCatgoryCode;
	}

	public void ReloadPage()
	{
		if (browseHistory.Count > 0)
		{
			initDetailGui();
		}
		else
		{
			initOverviewGui();
		}
	}

	protected virtual void initDetailGui()
	{
		ElementBounds textBounds = ElementBounds.Fixed(9.0, 45.0, 500.0, 30.0 + listHeight + 17.0);
		ElementBounds clipBounds = textBounds.ForkBoundingParent();
		ElementBounds insetBounds = textBounds.FlatCopy().FixedGrow(6.0).WithFixedOffset(-3.0, -3.0);
		ElementBounds scrollbarBounds = clipBounds.CopyOffsetedSibling(textBounds.fixedWidth + 7.0, -6.0, 0.0, 6.0).WithFixedWidth(20.0);
		ElementBounds closeButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clipBounds, 15.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(20.0, 4.0)
			.WithFixedAlignmentOffset(-11.0, 1.0);
		ElementBounds backButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clipBounds, 15.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(20.0, 4.0)
			.WithFixedAlignmentOffset(4.0, 1.0);
		ElementBounds overviewButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clipBounds, 15.0).WithAlignment(EnumDialogArea.CenterFixed)
			.WithFixedPadding(20.0, 4.0)
			.WithFixedAlignmentOffset(0.0, 1.0);
		ElementBounds bgBounds = insetBounds.ForkBoundingParent(5.0, 40.0, 36.0, 52.0).WithFixedPadding(GuiStyle.ElementToDialogPadding / 2.0);
		bgBounds.WithChildren(insetBounds, textBounds, scrollbarBounds, backButtonBounds, closeButtonBounds);
		ElementBounds dialogBounds = bgBounds.ForkBoundingParent().WithAlignment(EnumDialogArea.None).WithAlignment(EnumDialogArea.CenterFixed)
			.WithFixedPosition(0.0, 70.0);
		ElementBounds tabBounds = ElementBounds.Fixed(-200.0, 35.0, 200.0, 545.0);
		BrowseHistoryElement curPage = browseHistory.Peek();
		float posY = curPage.PosY;
		detailViewGui?.Dispose();
		detailViewGui = capi.Gui.CreateCompo("handbook-detail", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
			.AddVerticalTabs(genTabs(out var curTab), tabBounds, OnDetailViewTabClicked, "verticalTabs")
			.BeginChildElements(bgBounds)
			.BeginClip(clipBounds)
			.AddInset(insetBounds, 3);
		composePageHandler(curPage.Page, detailViewGui, textBounds, OpenDetailPageFor);
		GuiElement lastAddedElement = detailViewGui.LastAddedElement;
		detailViewGui.EndClip().AddVerticalScrollbar(OnNewScrollbarvalueDetailPage, scrollbarBounds, "scrollbar").AddIf(capi.IsSinglePlayer && !capi.OpenedToLan)
			.AddToggleButton(Lang.Get("Pause game"), CairoFont.WhiteDetailText(), onTogglePause, ElementBounds.Fixed(370.0, -5.0, 100.0, 22.0), "pausegame")
			.EndIf()
			.AddSmallButton(Lang.Get("general-back"), OnButtonBack, backButtonBounds)
			.AddSmallButton(Lang.Get("handbook-overview"), OnButtonOverview, overviewButtonBounds)
			.AddSmallButton(Lang.Get("general-close"), OnButtonClose, closeButtonBounds)
			.EndChildElements()
			.Compose();
		detailViewGui.GetScrollbar("scrollbar").SetHeights((float)listHeight, (float)lastAddedElement.Bounds.fixedHeight);
		detailViewGui.GetScrollbar("scrollbar").CurrentYPosition = posY;
		OnNewScrollbarvalueDetailPage(posY);
		detailViewGui.GetVerticalTab("verticalTabs").SetValue(curTab, triggerHandler: false);
		detailViewGui.GetToggleButton("pausegame")?.SetValue(!capi.Settings.Bool["noHandbookPause"]);
	}

	protected virtual void OnDetailViewTabClicked(int index, GuiTab tab)
	{
		browseHistory.Clear();
		OnTabClicked(index, tab);
	}

	protected bool OnButtonOverview()
	{
		browseHistory.Clear();
		return true;
	}

	public virtual bool OpenDetailPageFor(string pageCode)
	{
		capi.Gui.PlaySound("menubutton_press");
		if (pageNumberByPageCode.TryGetValue(pageCode, out var num))
		{
			GuiHandbookPage elem = allHandbookPages[num];
			if (browseHistory.Count > 0 && elem == browseHistory.Peek().Page)
			{
				return true;
			}
			browseHistory.Push(new BrowseHistoryElement
			{
				Page = elem,
				PosY = 0f
			});
			initDetailGui();
			return true;
		}
		return false;
	}

	protected bool OnButtonBack()
	{
		if (browseHistory.Count == 0)
		{
			return true;
		}
		browseHistory.Pop();
		if (browseHistory.Count > 0)
		{
			if (browseHistory.Peek().SearchText != null)
			{
				Search(browseHistory.Peek().SearchText);
			}
			else
			{
				initDetailGui();
			}
		}
		return true;
	}

	protected void onLeftClickListElement(int index)
	{
		browseHistory.Push(new BrowseHistoryElement
		{
			Page = (shownHandbookPages[index] as GuiHandbookPage)
		});
		initDetailGui();
	}

	protected void OnNewScrollbarvalueOverviewPage(float value)
	{
		GuiElementFlatList flatList = overviewGui.GetFlatList("stacklist");
		flatList.insideBounds.fixedY = 3f - value;
		flatList.insideBounds.CalcWorldBounds();
	}

	protected void OnNewScrollbarvalueDetailPage(float value)
	{
		GuiElementRichtext richtext = detailViewGui.GetRichtext("richtext");
		richtext.Bounds.fixedY = 3f - value;
		richtext.Bounds.CalcWorldBounds();
		browseHistory.Peek().PosY = detailViewGui.GetScrollbar("scrollbar").CurrentYPosition;
	}

	protected void OnTitleBarClose()
	{
		TryClose();
	}

	protected bool OnButtonClose()
	{
		TryClose();
		return true;
	}

	public override void OnGuiOpened()
	{
		initOverviewGui();
		FilterItems();
		base.OnGuiOpened();
		if (capi.IsSinglePlayer && !capi.OpenedToLan && !capi.Settings.Bool["noHandbookPause"])
		{
			capi.PauseGame(paused: true);
		}
	}

	public override void OnGuiClosed()
	{
		browseHistory.Clear();
		overviewGui.GetTextInput("searchField").SetValue("");
		if (capi.IsSinglePlayer && !capi.OpenedToLan && !capi.Settings.Bool["noHandbookPause"] && capi.OpenedGuis.FirstOrDefault((object dlg) => dlg is GuiDialogCreateCharacter) == null)
		{
			capi.PauseGame(paused: false);
		}
		base.OnGuiClosed();
	}

	protected virtual HashSet<string> initCustomPages()
	{
		return new HashSet<string>();
	}

	protected void LoadPages_Async()
	{
		allHandbookPages.AddRange(createPageHandlerAsync());
		for (int i = 0; i < allHandbookPages.Count; i++)
		{
			GuiHandbookPage page = allHandbookPages[i];
			pageNumberByPageCode[page.PageCode] = (page.PageNumber = i);
		}
		loadingPagesAsync = false;
	}

	public void Search(string text)
	{
		currentCatgoryCode = null;
		base.SingleComposer = overviewGui;
		overviewGui.GetTextInput("searchField").SetValue(text);
		if (browseHistory.Count <= 0 || !(browseHistory.Peek().SearchText == text))
		{
			capi.Gui.PlaySound("menubutton_press");
			browseHistory.Push(new BrowseHistoryElement
			{
				Page = null,
				SearchText = text,
				PosY = 0f
			});
		}
	}

	protected void FilterItemsBySearchText(string text)
	{
		if (!(currentSearchText == text))
		{
			currentSearchText = text;
			FilterItems();
		}
	}

	public void FilterItems()
	{
		string text = currentSearchText?.ToLowerInvariant();
		bool logicalAnd = false;
		string[] texts;
		if (text == null)
		{
			texts = new string[0];
		}
		else
		{
			if (text.Contains(" or ", StringComparison.Ordinal))
			{
				texts = (from str in text.Split(new string[1] { " or " }, StringSplitOptions.RemoveEmptyEntries)
					orderby str.Length
					select str).ToArray();
			}
			else if (text.Contains(" and ", StringComparison.Ordinal))
			{
				texts = (from str in text.Split(new string[1] { " and " }, StringSplitOptions.RemoveEmptyEntries)
					orderby str.Length
					select str).ToArray();
				logicalAnd = texts.Length > 1;
			}
			else
			{
				texts = new string[1] { text };
			}
			int countEmpty = 0;
			for (int k = 0; k < texts.Length; k++)
			{
				texts[k] = texts[k].ToSearchFriendly().Trim();
				if (texts[k].Length == 0)
				{
					countEmpty++;
				}
			}
			if (countEmpty > 0)
			{
				string[] newTexts = new string[texts.Length - countEmpty];
				int m = 0;
				for (int j = 0; j < texts.Length; j++)
				{
					if (texts[j].Length != 0)
					{
						newTexts[m++] = texts[j];
					}
				}
				texts = newTexts;
				logicalAnd = logicalAnd && texts.Length > 1;
			}
		}
		List<WeightedHandbookPage> foundPages = new List<WeightedHandbookPage>();
		shownHandbookPages.Clear();
		if (!loadingPagesAsync)
		{
			for (int i = 0; i < allHandbookPages.Count; i++)
			{
				GuiHandbookPage page = allHandbookPages[i];
				if ((currentCatgoryCode != null && page.CategoryCode != currentCatgoryCode) || page.IsDuplicate)
				{
					continue;
				}
				float weight = 1f;
				bool matched = logicalAnd;
				for (int l = 0; l < texts.Length; l++)
				{
					weight = page.GetTextMatchWeight(texts[l]);
					if (weight > 0f)
					{
						if (!logicalAnd)
						{
							matched = true;
							break;
						}
					}
					else if (logicalAnd)
					{
						matched = false;
						break;
					}
				}
				if (matched || texts.Length == 0)
				{
					foundPages.Add(new WeightedHandbookPage
					{
						Page = page,
						Weight = weight
					});
				}
			}
			foreach (WeightedHandbookPage val in foundPages.OrderByDescending((WeightedHandbookPage wpage) => wpage.Weight))
			{
				shownHandbookPages.Add(val.Page);
			}
		}
		GuiElementFlatList stacklist = overviewGui.GetFlatList("stacklist");
		stacklist.CalcTotalHeight();
		overviewGui.GetScrollbar("scrollbar").SetHeights((float)listHeight, (float)stacklist.insideBounds.fixedHeight);
	}

	public override void OnRenderGUI(float deltaTime)
	{
		if (browseHistory.Count == 0 || browseHistory.Peek().SearchText != null)
		{
			base.SingleComposer = overviewGui;
		}
		else
		{
			base.SingleComposer = detailViewGui;
		}
		if (base.SingleComposer == overviewGui)
		{
			overviewGui.GetButton("backButton").Enabled = browseHistory.Count > 0;
		}
		base.OnRenderGUI(deltaTime);
	}

	public override bool CaptureAllInputs()
	{
		return false;
	}

	public override void Dispose()
	{
		if (allHandbookPages != null)
		{
			foreach (GuiHandbookPage allHandbookPage in allHandbookPages)
			{
				allHandbookPage?.Dispose();
			}
		}
		overviewGui?.Dispose();
		detailViewGui?.Dispose();
	}
}
