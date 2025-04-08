using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.Server;

namespace Vintagestory.Client;

internal class GuiScreenExitingServer : GuiScreenConnectingToServer
{
	public GuiScreenExitingServer(ScreenManager screenManager, GuiScreen parent)
		: base(singleplayer: true, screenManager, null)
	{
		if (ClientSettings.DeveloperMode)
		{
			ComposeDeveloperLogDialog("exitingspserver", Lang.Get("Shutting down singleplayer server..."), "");
		}
		else
		{
			ComposePlayerLogDialog("exitingspserver", Lang.Get("It pauses."));
		}
		if (ServerMain.Logger != null)
		{
			ServerMain.Logger.EntryAdded += base.LogAdded;
		}
	}

	public override void RenderToDefaultFramebuffer(float dt)
	{
		if (!ClientSettings.DeveloperMode)
		{
			ScreenManager.mainScreen.Render(dt, 0L, onlyBackground: true);
		}
		if (ScreenManager.Platform.EllapsedMs - lastLogfileCheck > 400)
		{
			updateLogText();
			lastLogfileCheck = ScreenManager.Platform.EllapsedMs;
		}
		ElementComposer.Render(dt);
		ElementComposer.PostRender(dt);
		if (!ScreenManager.Platform.IsServerRunning)
		{
			ScreenManager.StartMainMenu();
		}
	}
}
