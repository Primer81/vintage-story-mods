using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client;

public class GuiScreenLoadingGame : GuiScreen
{
	public GuiElementDynamicText textElem;

	private bool loadingShaders;

	private float accum;

	private bool shadersLoaded;

	public GuiScreenLoadingGame(ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		ElementComposer = ScreenManager.GuiComposers.Create("gameloadingscreen", ElementStdBounds.AutosizedMainDialogAtPos(150.0)).AddGrayBG(ElementBounds.Fill).AddDynamicText(Lang.Get("Loading game"), CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center), ElementBounds.FixedSize(EnumDialogArea.CenterMiddle, 400.0, 400.0), "loadingtext")
			.Compose();
		textElem = ElementComposer.GetDynamicText("loadingtext");
		ScreenManager.Platform.ToggleOffscreenBuffer(enable: false);
	}

	public override void RenderToDefaultFramebuffer(float dt)
	{
		if (ScreenManager.loadingText == null && !loadingShaders)
		{
			textElem.SetNewText("Loading shaders");
			ElementComposer.Render(dt);
			ElementComposer.PostRender(dt);
			loadingShaders = true;
			ScreenManager.Platform.Logger.Notification("Begin loading shaders");
			return;
		}
		ElementComposer.Render(dt);
		ElementComposer.PostRender(dt);
		LoadedTexture tex = ScreenManager.versionNumberTexture;
		float windowSizeX = ScreenManager.GamePlatform.WindowSize.Width;
		float windowSizeY = ScreenManager.GamePlatform.WindowSize.Height;
		ScreenManager.api.Render.Render2DTexturePremultipliedAlpha(tex.TextureId, windowSizeX - (float)tex.Width - 10f, windowSizeY - (float)tex.Height - 10f, tex.Width, tex.Height);
		if (loadingShaders)
		{
			ScreenManager.Platform.Logger.Notification("Load shaders now");
			ScreenManager.DoGameInitStage2();
			loadingShaders = false;
			shadersLoaded = true;
		}
		if (ScreenManager.loadingText != textElem.GetText())
		{
			textElem.SetNewText(ScreenManager.loadingText);
		}
		if (!shadersLoaded)
		{
			accum += dt;
			if (accum > 0.5f)
			{
				ScreenManager.Platform.Logger.Notification("Waiting for async sound loading...");
				accum = 0f;
			}
		}
	}

	public override void OnScreenLoaded()
	{
	}
}
