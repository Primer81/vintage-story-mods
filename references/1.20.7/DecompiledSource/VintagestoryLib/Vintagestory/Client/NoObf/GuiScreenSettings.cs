using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class GuiScreenSettings : GuiScreen, IGameSettingsHandler, IGuiCompositeHandler
{
	private GuiCompositeSettings gameSettingsMenu;

	public bool IsIngame => false;

	public int? MaxViewDistanceAlarmValue => null;

	public GuiComposerManager GuiComposers => ScreenManager.GuiComposers;

	public ICoreClientAPI Api => ScreenManager.api;

	public GuiScreenSettings(ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		ShowMainMenu = true;
		gameSettingsMenu = new GuiCompositeSettings(this, onMainScreen: true);
		gameSettingsMenu.OpenSettingsMenu();
	}

	public override bool OnBackPressed()
	{
		return true;
	}

	public void LoadComposer(GuiComposer composer)
	{
		ElementComposer = composer;
	}

	public bool LeaveSettingsMenu()
	{
		ScreenManager.StartMainMenu();
		return true;
	}

	public override void OnKeyDown(KeyEvent e)
	{
		gameSettingsMenu.OnKeyDown(e);
		base.OnKeyDown(e);
	}

	public override void OnKeyUp(KeyEvent e)
	{
		gameSettingsMenu.OnKeyUp(e);
		base.OnKeyUp(e);
	}

	public override void OnMouseDown(MouseEvent e)
	{
		gameSettingsMenu.OnMouseDown(e);
		base.OnMouseDown(e);
	}

	public override void OnMouseUp(MouseEvent e)
	{
		gameSettingsMenu.OnMouseUp(e);
		base.OnMouseUp(e);
	}

	public void ReloadShaders()
	{
		ShaderRegistry.ReloadShaders();
	}

	public override void OnScreenLoaded()
	{
		gameSettingsMenu.OpenSettingsMenu();
		base.OnScreenLoaded();
	}

	GuiComposer IGameSettingsHandler.dialogBase(string name, double width, double height)
	{
		return dialogBase(name, width, height);
	}

	public void OnMacroEditor()
	{
		throw new NotImplementedException();
	}
}
