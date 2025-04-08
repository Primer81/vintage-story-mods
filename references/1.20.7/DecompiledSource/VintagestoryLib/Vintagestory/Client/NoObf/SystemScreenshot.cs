using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemScreenshot : ClientSystem
{
	private bool takeScreenshot;

	private int doTakeMegaScreenshot;

	private long nextScreenshotdelay;

	private bool scaleScreenshotbefore;

	private float ssaabefore;

	private bool usePrimaryFramebuffer;

	public override string Name => "scr";

	public SystemScreenshot(ClientMain game)
		: base(game)
	{
		takeScreenshot = false;
		ScreenManager.hotkeyManager.SetHotKeyHandler("megascreenshot", takeMegaScreenshot);
		ScreenManager.hotkeyManager.SetHotKeyHandler("screenshot", delegate
		{
			usePrimaryFramebuffer = ClientSettings.ScaleScreenshot;
			takeScreenshot = true;
			return true;
		});
		game.eventManager.RegisterRenderer(AfterFinalCompo, EnumRenderStage.AfterFinalComposition, Name, 2.0);
		game.eventManager.RegisterRenderer(OnFrameDone, EnumRenderStage.Done, Name, 2.0);
	}

	private bool takeMegaScreenshot(KeyCombination t1)
	{
		if (doTakeMegaScreenshot > 0)
		{
			return true;
		}
		usePrimaryFramebuffer = true;
		doTakeMegaScreenshot = 2;
		scaleScreenshotbefore = ClientSettings.ScaleScreenshot;
		ClientSettings.ScaleScreenshot = false;
		ssaabefore = ClientSettings.SSAA;
		ClientSettings.SSAA = ClientSettings.MegaScreenshotSizeMul;
		ScreenManager.Platform.RebuildFrameBuffers();
		return true;
	}

	private void AfterFinalCompo(float dt)
	{
		if (usePrimaryFramebuffer && takeScreenshot && game.Platform.EllapsedMs - nextScreenshotdelay > 1000)
		{
			DoScreenshot(null);
			takeScreenshot = false;
			nextScreenshotdelay = game.Platform.EllapsedMs;
			game.PlaySound(new AssetLocation("sounds/camerasnap"));
		}
		else if (game.timelapse > 0f)
		{
			string timelapsePath = Path.Combine(GamePaths.Screenshots, "timelapse");
			Directory.CreateDirectory(timelapsePath);
			DoScreenshot(timelapsePath);
		}
	}

	private void DoScreenshot(string path)
	{
		game.Platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
		try
		{
			string filename = game.Platform.SaveScreenshot(path, null, withAlpha: false, ClientSettings.FlipScreenshot, GetMetaData());
			if (path == null)
			{
				string text = ((doTakeMegaScreenshot > 0) ? Lang.Get("screenshottaken-mega", filename) : Lang.Get("screenshottaken-normal", filename));
				game.ShowChatMessage(text);
			}
		}
		catch (Exception e)
		{
			game.Logger.Error("Screenshot failed:");
			game.Logger.Error(e);
			game.ShowChatMessage(Lang.Get("Unable to take screenshot. Check client-main.log file for error."));
		}
		game.Platform.UnloadFrameBuffer(EnumFrameBuffer.Default);
	}

	public void OnFrameDone(float dt)
	{
		if (!usePrimaryFramebuffer && takeScreenshot && game.Platform.EllapsedMs - nextScreenshotdelay > 1000)
		{
			game.Platform.LoadFrameBuffer(EnumFrameBuffer.Default);
			takeScreenshot = false;
			nextScreenshotdelay = game.Platform.EllapsedMs;
			game.PlaySound(new AssetLocation("sounds/camerasnap"));
			try
			{
				string filename = game.Platform.SaveScreenshot(null, null, withAlpha: false, ClientSettings.FlipScreenshot, GetMetaData());
				string text = ((doTakeMegaScreenshot > 0) ? Lang.Get("screenshottaken-mega", filename) : Lang.Get("screenshottaken-normal", filename));
				game.ShowChatMessage(text);
			}
			catch (Exception e)
			{
				game.Logger.Error("Screenshot failed:");
				game.Logger.Error(e);
				game.ShowChatMessage(Lang.Get("Unable to take screenshot. Check client-main.log log file for error."));
			}
			game.Platform.UnloadFrameBuffer(EnumFrameBuffer.Default);
		}
		if (doTakeMegaScreenshot > 0)
		{
			doTakeMegaScreenshot--;
			if (doTakeMegaScreenshot == 0)
			{
				ClientSettings.ScaleScreenshot = scaleScreenshotbefore;
				ClientSettings.SSAA = ssaabefore;
				ScreenManager.Platform.RebuildFrameBuffers();
			}
			else
			{
				takeScreenshot = true;
			}
		}
	}

	public string GetMetaData()
	{
		if (ClientSettings.ScreenshotExifDataMode > 0)
		{
			return JsonUtil.ToString(new ScreenshotLocationMetaData
			{
				Pos = game.player.Entity.Pos.XYZ,
				RollYawPitch = new Vec3f(game.player.Entity.Pos.Roll, game.player.Entity.Pos.Yaw, game.player.Entity.Pos.Pitch),
				WorldSeed = game.Seed,
				WorldConfig = game.Config.ToJsonToken()
			});
		}
		return "";
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
