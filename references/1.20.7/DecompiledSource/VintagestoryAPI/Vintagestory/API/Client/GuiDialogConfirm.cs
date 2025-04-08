using System;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

public class GuiDialogConfirm : GuiDialog
{
	private string text;

	private Action<bool> DidPressButton;

	public override double DrawOrder => 2.0;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogConfirm(ICoreClientAPI capi, string text, Action<bool> DidPressButton)
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
		base.SingleComposer = capi.Gui.CreateCompo("confirmdialog", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(bgBounds).AddDialogTitleBar(Lang.Get("Please Confirm"), OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddStaticText(text, font, textBounds)
			.AddSmallButton(Lang.Get("Cancel"), delegate
			{
				DidPressButton(obj: false);
				TryClose();
				return true;
			}, ElementStdBounds.MenuButton((y + 80f) / 80f).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(6.0))
			.AddSmallButton(Lang.Get("Confirm"), delegate
			{
				DidPressButton(obj: true);
				TryClose();
				return true;
			}, ElementStdBounds.MenuButton((y + 80f) / 80f).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(6.0))
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
