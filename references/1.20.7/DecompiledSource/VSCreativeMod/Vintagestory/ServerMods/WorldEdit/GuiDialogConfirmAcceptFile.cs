using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.ServerMods.WorldEdit;

public class GuiDialogConfirmAcceptFile : GuiDialog
{
	private string text;

	private Action<string> DidPressButton;

	private static int index;

	public override double DrawOrder => 2.0;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogConfirmAcceptFile(ICoreClientAPI capi, string text, Action<string> DidPressButton)
		: base(capi)
	{
		this.text = text;
		this.DidPressButton = DidPressButton;
		Compose();
	}

	private void Compose()
	{
		ElementBounds textBounds = ElementStdBounds.Rowed(0.4f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(500.0);
		ElementBounds bgBounds = ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding);
		TextDrawUtil textDrawUtil = new TextDrawUtil();
		CairoFont font = CairoFont.WhiteSmallText();
		float y = (float)textDrawUtil.GetMultilineTextHeight(font, text, textBounds.fixedWidth);
		base.SingleComposer = capi.Gui.CreateCompo("confirmdialog-" + index++, ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(bgBounds).AddDialogTitleBar(Lang.Get("Please Confirm"), OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddStaticText(text, font, textBounds)
			.AddSmallButton(Lang.Get("Ignore all files"), delegate
			{
				DidPressButton("ignore");
				TryClose();
				return true;
			}, ElementStdBounds.MenuButton((y + 80f) / 80f).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(6.0))
			.AddSmallButton(Lang.Get("Accept file"), delegate
			{
				DidPressButton("accept");
				TryClose();
				return true;
			}, ElementStdBounds.MenuButton((y + 80f) / 80f).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(6.0))
			.AddSmallButton(Lang.Get("Accept next 10 files"), delegate
			{
				DidPressButton("accept10");
				TryClose();
				return true;
			}, ElementStdBounds.MenuButton((y + 80f) / 80f).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(6.0)
				.WithFixedAlignmentOffset(-100.0, 0.0))
			.EndChildElements()
			.Compose();
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override void OnGuiOpened()
	{
		Compose();
		base.OnGuiOpened();
	}
}
