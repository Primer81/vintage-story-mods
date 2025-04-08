using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class GuiDialogReadonlyBook : GuiDialogGeneric
{
	public string AllPagesText;

	public string Title;

	protected int curPage;

	protected int maxLines = 20;

	protected int maxWidth = 400;

	public List<PagePosition> Pages = new List<PagePosition>();

	protected CairoFont font = CairoFont.TextInput().WithFontSize(18f);

	private TranscribePressedDelegate onTranscribedPressed;

	protected bool KeyboardNavigation = true;

	public double textAreaWidth => GuiElement.scaled(maxWidth);

	public string CurPageText
	{
		get
		{
			if (curPage >= Pages.Count)
			{
				return "";
			}
			if (Pages[curPage].Start < AllPagesText.Length)
			{
				return AllPagesText.Substring(Pages[curPage].Start, Math.Min(AllPagesText.Length - Pages[curPage].Start, Pages[curPage].Length)).TrimStart(' ');
			}
			return "";
		}
	}

	public GuiDialogReadonlyBook(ItemStack bookStack, ICoreClientAPI capi, TranscribePressedDelegate onTranscribedPressed = null)
		: base("", capi)
	{
		this.onTranscribedPressed = onTranscribedPressed;
		if (bookStack.Attributes.HasAttribute("textCodes"))
		{
			AllPagesText = string.Join("\n", (bookStack.Attributes["textCodes"] as StringArrayAttribute).value.Select((string code) => Lang.Get(code))).Replace("\r", "").Replace("___NEWPAGE___", "");
			Title = Lang.Get(bookStack.Attributes.GetString("titleCode", ""));
		}
		else
		{
			AllPagesText = bookStack.Attributes.GetString("text", "").Replace("\r", "");
			Title = bookStack.Attributes.GetString("title", "");
		}
		Pages = Pageize(AllPagesText, font, textAreaWidth, maxLines);
		Compose();
	}

	protected List<PagePosition> Pageize(string fullText, CairoFont font, double pageWidth, int maxLinesPerPage)
	{
		TextDrawUtil textDrawUtil = new TextDrawUtil();
		Stack<string> lines = new Stack<string>();
		foreach (TextLine val in textDrawUtil.Lineize(font, fullText, pageWidth, EnumLinebreakBehavior.Default, keepLinebreakChar: true).Reverse())
		{
			lines.Push(val.Text);
		}
		List<PagePosition> pages = new List<PagePosition>();
		int start = 0;
		int curLen = 0;
		while (lines.Count > 0)
		{
			int currentPageLines = 0;
			while (currentPageLines < maxLinesPerPage && lines.Count > 0)
			{
				string line = lines.Pop();
				currentPageLines++;
				curLen += line.Length;
			}
			if (currentPageLines > 0)
			{
				pages.Add(new PagePosition
				{
					Start = start,
					Length = curLen,
					LineCount = currentPageLines
				});
				start += curLen;
			}
			curLen = 0;
		}
		if (pages.Count == 0)
		{
			pages.Add(new PagePosition
			{
				Start = 0,
				Length = 0
			});
		}
		return pages;
	}

	protected virtual void Compose()
	{
		double lineHeight = font.GetFontExtents().Height * font.LineHeightMultiplier / (double)RuntimeEnv.GUIScale;
		ElementBounds textAreaBounds = ElementBounds.Fixed(0.0, 30.0, maxWidth, (double)(maxLines + ((Pages.Count > 1) ? 2 : 0)) * lineHeight + 1.0);
		ElementBounds prevButtonBounds = ElementBounds.FixedSize(60.0, 30.0).FixedUnder(textAreaBounds, 23.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds pageLabelBounds = ElementBounds.FixedSize(80.0, 30.0).FixedUnder(textAreaBounds, 33.0).WithAlignment(EnumDialogArea.CenterFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds nextButtonBounds = ElementBounds.FixedSize(60.0, 30.0).FixedUnder(textAreaBounds, 23.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds closeButton = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(prevButtonBounds, 25.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds saveButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(nextButtonBounds, 25.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		bgBounds.WithChildren(closeButton);
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		base.SingleComposer = capi.Gui.CreateCompo("blockentitytexteditordialog", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(Title, delegate
		{
			TryClose();
		})
			.BeginChildElements(bgBounds)
			.AddRichtext("", font, textAreaBounds, "text")
			.AddIf(Pages.Count > 1)
			.AddSmallButton(Lang.Get("<"), prevPage, prevButtonBounds)
			.EndIf()
			.AddDynamicText("1/1", CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), pageLabelBounds, "pageNum")
			.AddIf(Pages.Count > 1)
			.AddSmallButton(Lang.Get(">"), nextPage, nextButtonBounds)
			.EndIf()
			.AddSmallButton(Lang.Get("Close"), () => TryClose(), closeButton)
			.AddIf(onTranscribedPressed != null)
			.AddSmallButton(Lang.Get("Transcribe"), onButtonTranscribe, saveButtonBounds)
			.EndIf()
			.EndChildElements()
			.Compose();
		updatePage();
	}

	private bool onButtonTranscribe()
	{
		onTranscribedPressed(CurPageText, Title, curPage);
		return true;
	}

	protected bool nextPage()
	{
		curPage = Math.Min(curPage + 1, Pages.Count - 1);
		updatePage();
		return true;
	}

	private bool prevPage()
	{
		curPage = Math.Max(curPage - 1, 0);
		updatePage();
		return true;
	}

	protected void updatePage(bool setCaretPosToEnd = true)
	{
		string text = CurPageText;
		base.SingleComposer.GetDynamicText("pageNum").SetNewText(curPage + 1 + "/" + Pages.Count);
		GuiElement elem = base.SingleComposer.GetElement("text");
		if (elem is GuiElementTextArea textArea)
		{
			textArea.SetValue(text, setCaretPosToEnd);
		}
		else
		{
			(elem as GuiElementRichtext).SetNewText(text, font);
		}
	}

	public override void OnKeyDown(KeyEvent args)
	{
		base.OnKeyDown(args);
		if (KeyboardNavigation)
		{
			if (args.KeyCode == 47 || args.KeyCode == 56)
			{
				prevPage();
			}
			if (args.KeyCode == 48 || args.KeyCode == 57)
			{
				nextPage();
			}
		}
	}
}
