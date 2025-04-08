using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.Client.Util;

namespace Vintagestory.Client;

public class GuiScreenServerResetWorld : GuiScreen
{
	private ServerCtrlBackendInterface backend;

	public GuiScreenServerResetWorld(ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		backend = new ServerCtrlBackendInterface();
		ShowMainMenu = true;
		InitGui();
	}

	private void InitGui()
	{
		_ = CairoFont.ButtonText().GetTextExtents(Lang.Get("general-save")).Width;
		ElementBounds rowLeft = ElementBounds.Fixed(0.0, 0.0, 300.0, 35.0);
		ElementBounds rowRight = ElementBounds.Fixed(330.0, 0.0, 300.0, 25.0);
		string[] playstyleValues = new string[1] { "test" };
		string[] playstyleNames = new string[1] { "test" };
		ElementComposer = ScreenManager.GuiComposers.Create("mainmenu-servercontrol-dashboard", ElementStdBounds.MainScreenRightPart()).AddImageBG(ElementBounds.Fill, GuiElement.dirtTextureName, 1f, 1f, 0.125f).BeginChildElements(ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 110.0, 550.0, 600.0))
			.AddStaticText(Lang.Get("serverctrl-dashboard"), CairoFont.WhiteSmallText(), rowLeft)
			.AddStaticText(Lang.Get("serverctrl-serverstatus"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 30.0))
			.AddRichtext(Lang.Get("Loading..."), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 30.0))
			.AddStaticText(Lang.Get("serverctrl-servername"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 30.0))
			.AddStaticText(Lang.Get("serverctrl-serverdescription"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy())
			.AddStaticText(Lang.Get("serverctrl-whitelisted"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy())
			.AddStaticText(Lang.Get("serverctrl-serverpassword"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy())
			.AddStaticText(Lang.Get("serverctrl-motd"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy())
			.AddStaticText(Lang.Get("serverctrl-advertise"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy())
			.AddStaticText(Lang.Get("serverctrl-seed"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy())
			.AddStaticText(Lang.Get("serverctrl-playstyle"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy())
			.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 40.0), null, null, "servername")
			.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 10.0), null, null, "serverdescription")
			.AddSwitch(onToggleWhiteListed, rowRight = rowRight.BelowCopy(0.0, 10.0), "whiteListedSwitch", 25.0)
			.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 10.0).WithFixedWidth(300.0), null, null, "serverpassword")
			.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 10.0), null, null, "motd")
			.AddSwitch(onToggleAdvertise, rowRight = rowRight.BelowCopy(0.0, 10.0), "advertiseSwith", 25.0)
			.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 10.0).WithFixedWidth(300.0), null, null, "seed")
			.AddDropDown(playstyleValues, playstyleNames, 0, onPlayStyleChanged, rowRight = rowRight.BelowCopy(0.0, 10.0))
			.AddButton(Lang.Get("general-cancel"), OnCancel, ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0))
			.AddButton(Lang.Get("general-save"), OnSave, ElementStdBounds.Rowed(5.2f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
			.EndChildElements()
			.Compose();
	}

	private void onPlayStyleChanged(string code, bool selected)
	{
		throw new NotImplementedException();
	}

	private void onToggleAdvertise(bool t1)
	{
		throw new NotImplementedException();
	}

	private void onToggleWhiteListed(bool t1)
	{
		throw new NotImplementedException();
	}

	private bool OnSave()
	{
		return true;
	}

	private bool OnCancel()
	{
		ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
		return true;
	}

	public override void OnScreenLoaded()
	{
		InitGui();
	}
}
