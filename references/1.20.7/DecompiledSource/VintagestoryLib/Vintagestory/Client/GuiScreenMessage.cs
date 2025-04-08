using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client;

public class GuiScreenMessage : GuiScreen
{
	private Action DidPressOnBack;

	public GuiScreenMessage(string title, string text, Action OnPressBack, ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		DidPressOnBack = OnPressBack;
		ShowMainMenu = true;
		ElementComposer = dialogBase("mainmenu-message").AddStaticText(title, CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(0f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(300.0)).AddStaticText(text, CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(1f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(500.0)).AddButton(Lang.Get("Back"), OnBack, ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
			.EndChildElements()
			.Compose();
	}

	private bool OnBack()
	{
		DidPressOnBack();
		return true;
	}
}
