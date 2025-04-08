using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class GuiDialogEditableBook : GuiDialogReadonlyBook
{
	public bool DidSave;

	public bool DidSign;

	private int maxPageCount;

	private bool ignoreTextChange;

	public GuiDialogEditableBook(ItemStack bookStack, ICoreClientAPI capi, int maxPageCount)
		: base(bookStack, capi)
	{
		this.maxPageCount = maxPageCount;
		KeyboardNavigation = false;
	}

	protected override void Compose()
	{
		double lineHeight = font.GetFontExtents().Height * font.LineHeightMultiplier / (double)RuntimeEnv.GUIScale;
		ElementBounds titleBounds = ElementBounds.Fixed(0.0, 30.0, maxWidth, 24.0);
		ElementBounds textAreaBounds = ElementBounds.Fixed(0.0, 0.0, 400.0, (double)maxLines * lineHeight + 1.0).FixedUnder(titleBounds, 5.0);
		ElementBounds prevButtonBounds = ElementBounds.FixedSize(60.0, 30.0).FixedUnder(textAreaBounds, 5.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds pageLabelBounds = ElementBounds.FixedSize(80.0, 30.0).FixedUnder(textAreaBounds, 17.0).WithAlignment(EnumDialogArea.CenterFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds nextButtonBounds = ElementBounds.FixedSize(60.0, 30.0).FixedUnder(textAreaBounds, 5.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds cancelButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(prevButtonBounds, 25.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds signButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(prevButtonBounds, 25.0).WithAlignment(EnumDialogArea.CenterFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds saveButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(nextButtonBounds, 25.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		bgBounds.WithChildren(cancelButtonBounds, saveButtonBounds);
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		base.SingleComposer = capi.Gui.CreateCompo("blockentitytexteditordialog", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(Lang.Get("Edit book"), OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddTextInput(titleBounds, null, CairoFont.TextInput().WithFontSize(18f), "title")
			.AddTextArea(textAreaBounds, onTextChanged, font, "text")
			.AddSmallButton(Lang.Get("<"), OnPreviousPage, prevButtonBounds)
			.AddDynamicText("1/1", CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), pageLabelBounds, "pageNum")
			.AddSmallButton(Lang.Get(">"), OnNextPage, nextButtonBounds)
			.AddSmallButton(Lang.Get("Cancel"), OnButtonCancel, cancelButtonBounds)
			.AddSmallButton(Lang.Get("editablebook-sign"), OnButtonSign, signButtonBounds)
			.AddSmallButton(Lang.Get("editablebook-save"), OnButtonSave, saveButtonBounds)
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetTextInput("title").SetPlaceHolderText(Lang.Get("Book title"));
		base.SingleComposer.GetTextInput("title").SetValue(Title);
		base.SingleComposer.GetTextArea("text").OnCaretPositionChanged = onCaretPositionChanged;
		base.SingleComposer.GetTextArea("text").Autoheight = false;
		updatePage(setCaretPosToEnd: false);
		base.SingleComposer.GetTextArea("text").OnTryTextChangeText = onTryTextChange;
	}

	private bool onTryTextChange(List<string> lines)
	{
		int totalLineCount = 0;
		bool hasMoreLinesNow = lines.Count > Pages[curPage].LineCount;
		for (int i = 0; i < Pages.Count; i++)
		{
			totalLineCount = ((i == curPage) ? lines.Count : Pages[i].LineCount);
		}
		if (totalLineCount > maxPageCount * maxLines && hasMoreLinesNow)
		{
			return false;
		}
		return true;
	}

	private bool OnButtonSign()
	{
		new GuiDialogConfirm(capi, Lang.Get("Save and sign book now? It can not be edited afterwards."), onConfirmSign).TryOpen();
		return true;
	}

	private void onConfirmSign(bool ok)
	{
		if (ok)
		{
			StoreCurrentPage();
			Title = base.SingleComposer.GetTextInput("title").GetText();
			DidSign = true;
			DidSave = true;
			TryClose();
		}
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		base.SingleComposer.FocusElement(base.SingleComposer.GetTextArea("text").TabIndex);
	}

	private void OnTitleBarClose()
	{
		OnButtonCancel();
	}

	private void onTextChanged(string text)
	{
		if (!ignoreTextChange)
		{
			ignoreTextChange = true;
			GuiElementTextArea textArea = base.SingleComposer.GetTextArea("text");
			int posLine = textArea.CaretPosLine;
			int posInLine = textArea.CaretPosInLine;
			StoreCurrentPage();
			updatePage(setCaretPosToEnd: false);
			textArea.SetCaretPos(posInLine, posLine);
			ignoreTextChange = false;
		}
	}

	private void onCaretPositionChanged(int posLine, int posInLine)
	{
		if (!ignoreTextChange)
		{
			ignoreTextChange = true;
			if (posLine >= maxLines && curPage + 1 < maxPageCount && Pages.Count - 1 > curPage + 1)
			{
				GuiElementTextArea textArea = base.SingleComposer.GetTextArea("text");
				StoreCurrentPage();
				nextPage();
				textArea.SetCaretPos(posInLine, posLine - maxLines);
			}
			ignoreTextChange = false;
		}
	}

	private bool OnButtonSave()
	{
		StoreCurrentPage();
		Title = base.SingleComposer.GetTextInput("title").GetText();
		DidSave = true;
		TryClose();
		return true;
	}

	private bool OnButtonCancel()
	{
		DidSave = false;
		TryClose();
		return true;
	}

	protected bool OnNextPage()
	{
		if (curPage >= maxPageCount)
		{
			return true;
		}
		if (curPage + 1 >= Pages.Count)
		{
			return false;
		}
		if (Pages.Count <= curPage + 1)
		{
			PagePosition lastPage = Pages[0];
			Pages.Add(new PagePosition
			{
				Start = lastPage.Length + 1,
				Length = 1
			});
			AllPagesText += "___NEWPAGE___";
		}
		ignoreTextChange = true;
		StoreCurrentPage();
		curPage = Math.Min(curPage + 1, Pages.Count);
		updatePage();
		ignoreTextChange = false;
		return true;
	}

	private bool StoreCurrentPage()
	{
		PagePosition curPagePos = Pages[curPage];
		string pageText = base.SingleComposer.GetTextArea("text").GetText();
		AllPagesText = AllPagesText.Substring(0, curPagePos.Start) + pageText + AllPagesText.Substring(Math.Min(AllPagesText.Length, curPagePos.Start + curPagePos.Length)).Replace("\r", "");
		Pages = Pageize(AllPagesText, font, base.textAreaWidth, maxLines);
		if (curPage >= Pages.Count)
		{
			curPage = Pages.Count - 1;
		}
		return true;
	}

	protected bool OnPreviousPage()
	{
		ignoreTextChange = true;
		StoreCurrentPage();
		curPage = Math.Max(0, curPage - 1);
		updatePage();
		ignoreTextChange = false;
		return true;
	}

	public override void OnKeyDown(KeyEvent args)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("text");
		if (args.KeyCode == 53 && textArea.CaretPosInLine == 0 && textArea.CaretPosLine == 0 && curPage > 0)
		{
			StoreCurrentPage();
			curPage--;
			ignoreTextChange = true;
			updatePage();
			ignoreTextChange = false;
		}
		if (args.KeyCode == 47 && textArea.CaretPosInLine == 0 && textArea.CaretPosLine == 0 && curPage > 0)
		{
			StoreCurrentPage();
			curPage--;
			ignoreTextChange = true;
			updatePage();
			ignoreTextChange = false;
		}
		else if (args.KeyCode == 48 && curPage < Pages.Count - 1 && textArea.CaretPosWithoutLineBreaks == textArea.GetText().Length)
		{
			StoreCurrentPage();
			curPage++;
			ignoreTextChange = true;
			updatePage(setCaretPosToEnd: false);
			textArea.SetCaretPos(0);
			ignoreTextChange = false;
		}
		else if (args.KeyCode == 46 && textArea.CaretPosLine + 1 >= maxLines && curPage < Pages.Count - 1)
		{
			int pos = textArea.CaretPosInLine;
			StoreCurrentPage();
			curPage++;
			ignoreTextChange = true;
			updatePage(setCaretPosToEnd: false);
			textArea.SetCaretPos(pos);
			ignoreTextChange = false;
		}
		else if (args.KeyCode == 45 && curPage > 0 && textArea.CaretPosLine == 0)
		{
			StoreCurrentPage();
			curPage--;
			ignoreTextChange = true;
			updatePage();
			args.Handled = true;
			ignoreTextChange = false;
		}
		else
		{
			base.OnKeyDown(args);
		}
	}
}
