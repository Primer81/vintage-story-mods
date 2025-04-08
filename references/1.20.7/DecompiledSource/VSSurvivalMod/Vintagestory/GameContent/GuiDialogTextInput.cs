using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public abstract class GuiDialogTextInput : GuiDialogGeneric
{
	private double textareaFixedY;

	public Action<string> OnTextChanged;

	public Action OnCloseCancel;

	public float FontSize;

	private bool didSave;

	private TextAreaConfig signConfig;

	public GuiDialogTextInput(string DialogTitle, string text, ICoreClientAPI capi, TextAreaConfig signConfig)
		: base(DialogTitle, capi)
	{
		if (signConfig == null)
		{
			signConfig = new TextAreaConfig();
		}
		this.signConfig = signConfig;
		FontSize = signConfig.FontSize;
		ElementBounds textAreaBounds = ElementBounds.Fixed(0.0, 0.0, signConfig.MaxWidth + 4, signConfig.MaxHeight - 2);
		textareaFixedY = textAreaBounds.fixedY;
		ElementBounds clippingBounds = textAreaBounds.ForkBoundingParent().WithFixedPosition(0.0, 30.0);
		ElementBounds scrollbarBounds = clippingBounds.CopyOffsetedSibling(textAreaBounds.fixedWidth + 3.0).WithFixedWidth(20.0);
		ElementBounds cancelButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clippingBounds, 10.0).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(8.0, 2.0)
			.WithFixedAlignmentOffset(-1.0, 0.0);
		ElementBounds fontSizeBounds = ElementBounds.FixedSize(45.0, 22.0).FixedUnder(clippingBounds, 10.0).WithAlignment(EnumDialogArea.CenterFixed)
			.WithFixedAlignmentOffset(3.0, 0.0);
		ElementBounds saveButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clippingBounds, 10.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(8.0, 2.0);
		ElementBounds bgBounds = ElementBounds.FixedSize(signConfig.MaxWidth + 32, 220.0).WithFixedPadding(GuiStyle.ElementToDialogPadding);
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		CairoFont font = CairoFont.TextInput().WithFontSize(signConfig.FontSize).WithFont(signConfig.FontName);
		if (signConfig.BoldFont)
		{
			font.WithWeight(FontWeight.Bold);
		}
		font.LineHeightMultiplier = 0.9;
		string[] sizes = new string[8] { "14", "18", "20", "24", "28", "32", "36", "40" };
		base.SingleComposer = capi.Gui.CreateCompo("blockentitytexteditordialog", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.BeginClip(clippingBounds)
			.AddTextArea(textAreaBounds, OnTextAreaChanged, font, "text")
			.EndClip()
			.AddIf(signConfig.WithScrollbar)
			.AddVerticalScrollbar(OnNewScrollbarvalue, scrollbarBounds, "scrollbar")
			.EndIf()
			.AddSmallButton(Lang.Get("Cancel"), OnButtonCancel, cancelButtonBounds)
			.AddDropDown(sizes, sizes, sizes.IndexOf<string>(signConfig.FontSize.ToString() ?? ""), onfontsizechanged, fontSizeBounds)
			.AddSmallButton(Lang.Get("Save"), OnButtonSave, saveButtonBounds)
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetTextArea("text").SetMaxHeight(signConfig.MaxHeight);
		if (signConfig.WithScrollbar)
		{
			base.SingleComposer.GetScrollbar("scrollbar").SetHeights((float)textAreaBounds.fixedHeight, (float)textAreaBounds.fixedHeight);
		}
		if (text.Length > 0)
		{
			base.SingleComposer.GetTextArea("text").SetValue(text);
		}
	}

	private void onfontsizechanged(string code, bool selected)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("text");
		string text = textArea.GetText();
		textArea.SetFont(textArea.Font.Clone().WithFontSize(FontSize = code.ToInt()));
		textArea.Font.WithFontSize(FontSize = code.ToInt());
		textArea.SetMaxHeight(signConfig.MaxHeight);
		textArea.SetValue(text);
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
		base.SingleComposer.FocusElement(base.SingleComposer.GetTextArea("text").TabIndex);
	}

	private void OnTextAreaChanged(string value)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("text");
		if (signConfig.WithScrollbar)
		{
			base.SingleComposer.GetScrollbar("scrollbar").SetNewTotalHeight((float)textArea.Bounds.fixedHeight);
		}
		OnTextChanged?.Invoke(textArea.GetText());
	}

	private void OnNewScrollbarvalue(float value)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("text");
		textArea.Bounds.fixedY = 3.0 + textareaFixedY - (double)value;
		textArea.Bounds.CalcWorldBounds();
	}

	private void OnTitleBarClose()
	{
		OnButtonCancel();
	}

	private bool OnButtonSave()
	{
		string text = base.SingleComposer.GetTextArea("text").GetText();
		OnSave(text);
		didSave = true;
		TryClose();
		return true;
	}

	private bool OnButtonCancel()
	{
		TryClose();
		return true;
	}

	public override void OnGuiClosed()
	{
		if (!didSave)
		{
			OnCloseCancel?.Invoke();
		}
		base.OnGuiClosed();
	}

	public abstract void OnSave(string text);
}
