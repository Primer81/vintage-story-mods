using System;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public class GuiCompositeMainMenuLeft : GuiComposite
{
	private ElementBounds sidebarBounds;

	private ScreenManager screenManager;

	private LoadedTexture bgtex;

	private LoadedTexture logoTexture;

	private ParticleRenderer2D particleSystem;

	private long renderStartMs;

	private float curdx;

	private float curdy;

	private SimpleParticleProperties prop;

	private Vec3d minPos = new Vec3d();

	private Vec3d addPos = new Vec3d();

	private Random rand = new Random();

	public double Width => sidebarBounds.OuterWidth;

	public GuiCompositeMainMenuLeft(ScreenManager screenManager)
	{
		this.screenManager = screenManager;
		particleSystem = new ParticleRenderer2D(screenManager, screenManager.api);
		Compose();
	}

	internal void SetHasNewVersion(string versionnumber)
	{
		((GuiElementNewVersionText)screenManager.mainMenuComposer.GetElement("newversion")).Activate(versionnumber);
	}

	public void Compose()
	{
		particleSystem.Compose("textures/particle/white-spec.png");
		sidebarBounds = new ElementBounds();
		sidebarBounds.horizontalSizing = ElementSizing.Fixed;
		sidebarBounds.verticalSizing = ElementSizing.Percentual;
		sidebarBounds.percentHeight = 1.0;
		sidebarBounds.fixedWidth = ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding + ElementStdBounds.mainMenuUnscaledWoodPlankWidth;
		ElementBounds logoBounds = ElementBounds.Fixed(0.0, 25.0, ElementStdBounds.mainMenuUnscaledLogoSize, ElementStdBounds.mainMenuUnscaledLogoSize).WithFixedPadding(ElementStdBounds.mainMenuUnscaledLogoHorPadding, ElementStdBounds.mainMenuUnscaledLogoVerPadding);
		ElementBounds button = ElementBounds.Fixed(0.0, ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoVerPadding + 25 + 50, (double)(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding) - 2.0 * GuiElementTextButton.Padding + 2.0, 33.0);
		CairoFont buttonFont = CairoFont.ButtonText().WithFontSize(22f).WithWeight(FontWeight.Normal);
		button.fixedHeight = buttonFont.GetFontExtents().Height / (double)ClientSettings.GUIScale + 2.0 * GuiElementTextButton.Padding + 5.0;
		CairoFont loginFont = CairoFont.WhiteSmallText();
		loginFont.Color = GuiStyle.ButtonTextColor;
		ElementBounds leftBottomText = ElementBounds.Fixed(EnumDialogArea.LeftBottom, 0.0, 0.0, 300.0, 30.0).WithFixedAlignmentOffset(13.0, -8.0);
		ElementBounds newVersionBounds = ElementBounds.Fixed(0.0, 0.0, ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding - 11, 60.0).WithFixedPadding(5.0);
		string loginText = Lang.Get("mainmenu-loggedin", ClientSettings.PlayerName);
		if (screenManager.ClientIsOffline)
		{
			loginText = loginText + "<br>" + Lang.Get("mainmenu-offline");
		}
		screenManager.mainMenuComposer?.Dispose();
		screenManager.mainMenuComposer = ScreenManager.GuiComposers.Create("compositemainmenu", ElementBounds.Fill).AddShadedDialogBG(sidebarBounds, withTitleBar: false).BeginChildElements()
			.AddStaticCustomDraw(logoBounds, OnDrawTree)
			.AddButton(Lang.Get("mainmenu-sp"), OnSingleplayer, button, buttonFont, EnumButtonStyle.MainMenu, "singleplayer")
			.AddButton(Lang.Get("mainmenu-mp"), OnMultiplayer, button = button.BelowCopy(), buttonFont, EnumButtonStyle.MainMenu, "multiplayer")
			.AddIf(ClientSettings.HasGameServer)
			.AddButton(Lang.Get("mainmenu-gameserver"), OnGameServer, button = button.BelowCopy(), buttonFont, EnumButtonStyle.MainMenu, "gameserver")
			.EndIf()
			.AddStaticCustomDraw(ElementBounds.Fill, OnDrawSidebar)
			.AddButton(Lang.Get("mainmenu-settings"), OnSettings, button = button.BelowCopy(0.0, 25.0), buttonFont, EnumButtonStyle.MainMenu, "settings")
			.AddButton(Lang.Get("mainmenu-mods"), OnMods, button = button.BelowCopy(), buttonFont, EnumButtonStyle.MainMenu, "mods")
			.AddButton(Lang.Get("mainmenu-credits"), OnCredits, button = button.BelowCopy(), buttonFont, EnumButtonStyle.MainMenu, "credits")
			.AddButton(Lang.Get("mainmenu-quit"), OnQuit, button = button.BelowCopy(0.0, 45.0), buttonFont, EnumButtonStyle.MainMenu, "quit")
			.AddRichtext(loginText, loginFont, leftBottomText, didClickLink, "logintext")
			.AddInteractiveElement(new GuiElementNewVersionText(screenManager.api, CairoFont.WhiteDetailText().WithWeight(FontWeight.Bold).WithColor(GuiStyle.DarkBrownColor), newVersionBounds), "newversion")
			.EndChildElements()
			.Compose();
		(screenManager.mainMenuComposer.GetElement("newversion") as GuiElementNewVersionText).OnClicked = onUpdateGame;
		GuiElementRichtext.DebugLogging = false;
		screenManager.GamePlatform.Logger.VerboseDebug("Left bottom main menu text is at {0}/{1} w/h {2},{3}", leftBottomText.absX, leftBottomText.absY, leftBottomText.OuterWidth, leftBottomText.OuterHeight);
		int numScreenshots = 6;
		string filename = "textures/gui/backgrounds/mainmenu" + (1 + (int)(UnixTimeNow() / 604800) % numScreenshots) + ".png";
		int day = DateTime.Now.Day;
		bool xmas = DateTime.Now.Month == 12 && day >= 20 && day <= 30;
		bool num = (DateTime.Now.Month == 10 && day > 18) || (DateTime.Now.Month == 11 && day < 12);
		if (xmas)
		{
			filename = "textures/gui/backgrounds/mainmenu-xmas.png";
		}
		if (num)
		{
			filename = "textures/gui/backgrounds/mainmenu-halloween.png";
		}
		BitmapRef bmp = screenManager.GamePlatform.AssetManager.TryGet_BaseAssets(new AssetLocation(filename))?.ToBitmap(screenManager.api);
		if (bmp != null)
		{
			bgtex = new LoadedTexture(screenManager.api, screenManager.GamePlatform.LoadTexture(bmp, linearMag: true), bmp.Width, bmp.Height);
			bmp.Dispose();
		}
		else
		{
			bgtex = new LoadedTexture(screenManager.api, 0, 1, 1);
		}
		ClientSettings.Inst.AddWatcher<float>("guiScale", OnGuiScaleChanged);
		byte[] pngdata = ScreenManager.Platform.AssetManager.Get("textures/gui/logo.png").Data;
		BitmapExternal bitmap = (BitmapExternal)ScreenManager.Platform.CreateBitmapFromPng(pngdata, pngdata.Length);
		ImageSurface logosurface = new ImageSurface(Format.Argb32, bitmap.Width, bitmap.Height);
		logosurface.Image(bitmap, 0, 0, bitmap.Width, bitmap.Height);
		bitmap.Dispose();
		logoTexture?.Dispose();
		logoTexture = new LoadedTexture(screenManager.api);
		screenManager.api.Gui.LoadOrUpdateCairoTexture(logosurface, linearMag: true, ref logoTexture);
		logosurface.Dispose();
	}

	private void onUpdateGame(string versionnumber)
	{
		if (RuntimeEnv.OS == OS.Windows)
		{
			screenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("mainmenu-confirm-updategame", versionnumber), delegate(bool ok)
			{
				OnConfirmUpdateGame(ok, versionnumber);
			}, screenManager, screenManager.mainScreen));
		}
		else
		{
			NetUtil.OpenUrlInBrowser("https://account.vintagestory.at");
		}
	}

	private void OnConfirmUpdateGame(bool ok, string versionnumber)
	{
		if (!ok)
		{
			screenManager.StartMainMenu();
		}
		else
		{
			screenManager.LoadScreen(new GuiScreenGetUpdate(versionnumber, screenManager, screenManager.mainScreen));
		}
	}

	public long UnixTimeNow()
	{
		return (long)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
	}

	private void OnGuiScaleChanged(float newValue)
	{
		screenManager.versionNumberTexture?.Dispose();
		screenManager.versionNumberTexture = screenManager.api.Gui.TextTexture.GenUnscaledTextTexture(GameVersion.LongGameVersion, CairoFont.WhiteDetailText());
	}

	public void updateButtonActiveFlag(string key)
	{
		screenManager.mainMenuComposer.GetButton("singleplayer").SetActive(key == "singleplayer");
		screenManager.mainMenuComposer.GetButton("multiplayer").SetActive(key == "multiplayer");
		screenManager.mainMenuComposer.GetButton("gameserver")?.SetActive(key == "gameserver");
		screenManager.mainMenuComposer.GetButton("settings").SetActive(key == "settings");
		screenManager.mainMenuComposer.GetButton("credits").SetActive(key == "credits");
		screenManager.mainMenuComposer.GetButton("mods").SetActive(key == "mods");
		screenManager.mainMenuComposer.GetButton("quit").SetActive(key == "quit");
	}

	private void OnDrawTree(Context ctx, ImageSurface surface, ElementBounds currentBounds)
	{
		ctx.Antialias = Antialias.Best;
		double paddedWidth = GuiElement.scaled(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding);
		double paddedHeight = GuiElement.scaled(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoVerPadding);
		double height = GuiElement.scaled(100.0);
		LinearGradient gradient = new LinearGradient(0.0, 0.0, 0.0, paddedHeight + height);
		gradient.AddColorStop(0.0, new Color(56.0 / 255.0, 0.17647058823529413, 32.0 / 255.0, 0.5));
		gradient.AddColorStop(0.5, new Color(73.0 / 255.0, 58.0 / 255.0, 41.0 / 255.0, 0.5));
		gradient.AddColorStop(1.0, new Color(73.0 / 255.0, 58.0 / 255.0, 41.0 / 255.0, 0.0));
		GuiElement.Rectangle(ctx, 0.0, 0.0, paddedWidth, paddedHeight + height);
		ctx.SetSource(gradient);
		ctx.Fill();
		gradient.Dispose();
		byte[] pngdata = ScreenManager.Platform.AssetManager.Get("textures/gui/tree.png").Data;
		BitmapExternal bitmap = (BitmapExternal)ScreenManager.Platform.CreateBitmapFromPng(pngdata, pngdata.Length);
		surface.Image(bitmap, (int)currentBounds.drawX, (int)currentBounds.drawY, (int)currentBounds.InnerWidth, (int)currentBounds.InnerHeight);
		bitmap.Dispose();
	}

	private void OnDrawSidebar(Context ctx, ImageSurface surface, ElementBounds currentBounds)
	{
		double woodPlankWidth = GuiElement.scaled(ElementStdBounds.mainMenuUnscaledWoodPlankWidth);
		double paddedWidth = GuiElement.scaled(ElementStdBounds.mainMenuUnscaledLogoSize + 2 * ElementStdBounds.mainMenuUnscaledLogoHorPadding) + woodPlankWidth;
		SurfacePattern pattern = GuiElement.getPattern(screenManager.api, new AssetLocation("gui/backgrounds/oak.png"), doCache: true, 255, 0.125f);
		GuiElement.Rectangle(ctx, paddedWidth - woodPlankWidth, 0.0, woodPlankWidth, currentBounds.OuterHeight);
		ctx.SetSource(pattern);
		ctx.Fill();
		LinearGradient gradient = new LinearGradient(paddedWidth - 5.0 - woodPlankWidth, 0.0, paddedWidth - woodPlankWidth, 0.0);
		gradient.AddColorStop(0.0, new Color(0.0, 0.0, 0.0, 0.0));
		gradient.AddColorStop(0.6, new Color(0.0, 0.0, 0.0, 0.38));
		gradient.AddColorStop(1.0, new Color(0.0, 0.0, 0.0, 0.38));
		ctx.Operator = Operator.Multiply;
		GuiElement.Rectangle(ctx, paddedWidth - 5.0 - woodPlankWidth, 0.0, 5.0, currentBounds.OuterHeight);
		ctx.SetSource(gradient);
		ctx.Fill();
		gradient.Dispose();
		ctx.Operator = Operator.Over;
	}

	private void didClickLink(LinkTextComponent comp)
	{
		string href = comp.Href;
		if (href.StartsWithOrdinal("https://"))
		{
			NetUtil.OpenUrlInBrowser(href);
		}
		if (href.Contains("logout"))
		{
			OnLogout();
		}
	}

	private void OnLogout()
	{
		screenManager.sessionManager.DoLogout();
		ClientSettings.UserEmail = "";
		ClientSettings.PlayerName = "";
		ClientSettings.Sessionkey = "";
		ClientSettings.SessionSignature = "";
		ClientSettings.MpToken = "";
		ClientSettings.Inst.Save(force: true);
		screenManager.LoadAndCacheScreen(typeof(GuiScreenLogin));
	}

	private bool OnCredits()
	{
		updateButtonActiveFlag("credits");
		screenManager.LoadAndCacheScreen(typeof(GuiScreenCredits));
		return true;
	}

	private bool OnMods()
	{
		updateButtonActiveFlag("mods");
		screenManager.LoadAndCacheScreen(typeof(GuiScreenMods));
		return true;
	}

	private bool OnSettings()
	{
		updateButtonActiveFlag("settings");
		screenManager.LoadAndCacheScreen(typeof(GuiScreenSettings));
		return true;
	}

	public bool OnSingleplayer()
	{
		updateButtonActiveFlag("singleplayer");
		screenManager.LoadAndCacheScreen(typeof(GuiScreenSingleplayer));
		return true;
	}

	public bool OnMultiplayer()
	{
		updateButtonActiveFlag("multiplayer");
		screenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
		return true;
	}

	private bool OnGameServer()
	{
		updateButtonActiveFlag("gameserver");
		screenManager.LoadAndCacheScreen(typeof(GuiScreenServerDashboard));
		return true;
	}

	public bool OnQuit()
	{
		ClientSettings.Inst.Save(force: true);
		updateButtonActiveFlag("quit");
		particleSystem?.Dispose();
		screenManager.GamePlatform.WindowExit("Main screen quit button was pressed");
		return true;
	}

	public void OnMouseDown(MouseEvent e)
	{
		if ((double)e.X < GuiElement.scaled(ElementStdBounds.mainMenuUnscaledLogoSize + ElementStdBounds.mainMenuUnscaledLogoHorPadding) && (double)e.Y < GuiElement.scaled(ElementStdBounds.mainMenuUnscaledLogoSize) && e.Y > 50)
		{
			screenManager.LoadScreen(screenManager.mainScreen);
			e.Handled = true;
		}
		else
		{
			screenManager.mainMenuComposer.OnMouseDown(e);
		}
	}

	public void OnMouseUp(MouseEvent e)
	{
		screenManager.mainMenuComposer.OnMouseUp(e);
	}

	internal void OnMouseMove(MouseEvent e)
	{
		screenManager.mainMenuComposer.OnMouseMove(e);
	}

	public void RenderBg(float dt, bool mainMenuVisible)
	{
		Render(dt, screenManager.GamePlatform.EllapsedMs, mainMenuVisible);
	}

	protected void Render(float dt, long ellapsedMs, bool mainMenuVisible, bool onlyBackground = false)
	{
		if (renderStartMs == 0L)
		{
			renderStartMs = ellapsedMs;
		}
		float baseIter = (float)((double)ellapsedMs / 1500.0);
		float easein = GameMath.Clamp((float)(ellapsedMs - renderStartMs) / 60000f, 0f, 1f);
		float dx = (GameMath.Sin(baseIter / 2.4f) * 12f + GameMath.Sin(baseIter / 2f) * 8f + GameMath.Sin(baseIter / 1.2f) * 4f) / 2f;
		float num = (GameMath.Sin(baseIter / 2.3f) * 9f + GameMath.Sin(baseIter / 1.5f) * 11f + GameMath.Sin(baseIter / 1.4f) * 4f) / 2f;
		float winWidth = Math.Max(10, screenManager.GamePlatform.WindowSize.Width);
		float winHeight = Math.Max(10, screenManager.GamePlatform.WindowSize.Height);
		float mouseRelx = (float)screenManager.api.inputapi.MouseX / winWidth;
		float mouseRely = (float)screenManager.api.inputapi.MouseY / winHeight;
		float dtCapped = Math.Min(1f / 30f, dt);
		curdx += (-30f * mouseRelx + 15f - curdx) * 5f * dtCapped;
		curdy += (-30f * mouseRely + 15f - curdy) * 1.5f * dtCapped;
		dx += curdx;
		float num2 = num + curdy;
		float zoom = Math.Max(1f, (winWidth + 80f) / winWidth + (1f + GameMath.Sin(baseIter / 5f) + GameMath.Sin(baseIter / 6f)) / 5f) + 0.05f;
		dx *= 1f + zoom - (winWidth + 40f) / winWidth;
		float val = num2 * (1f + zoom - (winWidth + 40f) / winWidth);
		dx = GameMath.Clamp(dx, -100f, 100f);
		float num3 = GameMath.Clamp(val, -100f, 100f);
		double ratioW = winWidth / (float)bgtex.Width;
		double ratioH = winHeight / (float)bgtex.Height;
		double ratio = ((ratioW > ratioH) ? ratioW : ratioH);
		float renderWidth = (float)((double)bgtex.Width * ratio);
		float renderHeight = (float)((double)bgtex.Height * ratio);
		dx *= easein;
		float num4 = num3 * easein;
		zoom = 1f + (zoom - 1f) * easein;
		float x = dx + (1f - zoom) * renderWidth / 2f;
		float y = num4 + (1f - zoom) * renderHeight / 2f;
		screenManager.api.Render.Render2DTexture(bgtex.TextureId, x, y, renderWidth * zoom, renderHeight * zoom, 10f);
		ShaderPrograms.Gui.Stop();
		screenManager.GamePlatform.GlDepthMask(flag: false);
		spawnParticles(dtCapped);
		float[] pmat = screenManager.api.renderapi.pMatrix;
		particleSystem.pMatrix = pmat;
		particleSystem.mvMatrix = Mat4f.Identity(new float[16]);
		Mat4f.Translate(particleSystem.mvMatrix, particleSystem.mvMatrix, 2f * curdx, 2f * curdy, 0f);
		Mat4f.Translate(particleSystem.mvMatrix, particleSystem.mvMatrix, renderWidth / 2f, renderHeight / 2f, 0f);
		Mat4f.Scale(particleSystem.mvMatrix, particleSystem.mvMatrix, zoom, zoom, zoom);
		Mat4f.Translate(particleSystem.mvMatrix, particleSystem.mvMatrix, (0f - renderWidth) / 2f, (0f - renderHeight) / 2f, 0f);
		particleSystem.Render(dt);
		screenManager.GamePlatform.GlDepthMask(flag: true);
		float windowSizeX = screenManager.GamePlatform.WindowSize.Width;
		double lx = screenManager.guiMainmenuLeft.Width + GuiElement.scaled(15.0);
		float lwidth = (windowSizeX - (float)lx) * 0.8f;
		lx += (double)((windowSizeX - lwidth) / 4f);
		if (!mainMenuVisible)
		{
			lx = windowSizeX * 0.15f;
			lwidth = windowSizeX * 0.7f;
		}
		float lheight = (float)logoTexture.Height * (lwidth / (float)logoTexture.Width);
		screenManager.api.Render.Render2DTexture(logoTexture.TextureId, (float)lx + (float)Math.Sin((double)ellapsedMs / 2000.0) * 10f, (float)GuiElement.scaled(25.0) + (float)Math.Sin(20.0 + (double)ellapsedMs / 2220.0) * 10f, lwidth, lheight, 20f);
	}

	private void spawnParticles(float dt)
	{
		ClientPlatformAbstract plt = screenManager.GamePlatform;
		minPos.X = 0.0;
		minPos.Y = (float)plt.WindowSize.Height * 0.5f;
		minPos.Z = -50.0;
		addPos.X = plt.WindowSize.Width;
		addPos.Y = (float)plt.WindowSize.Height * 0.75f;
		if (prop == null)
		{
			prop = new SimpleParticleProperties(0.025f, 0.125f, ColorUtil.ToRgba(40, 255, 255, 255), new Vec3d(0.0, 0.0, 0.0), new Vec3d(), new Vec3f(), new Vec3f(), 5f, 0f, 0f, 0.4f);
			prop.MinPos = minPos;
			prop.AddPos = addPos;
			prop.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.CLAMPEDPOSITIVESINUS, (float)Math.PI);
			minPos.X = 0.0;
			minPos.Y = (float)plt.WindowSize.Height * 0f;
			addPos.X = plt.WindowSize.Width;
			addPos.Y = (float)plt.WindowSize.Height * 1.25f;
			for (int i = 0; i < 1000; i++)
			{
				float nearness = 3f * (2.5f + (float)rand.NextDouble() * 4f);
				prop.MinVelocity.Set(-12f * (0.5f + nearness / 13f), -3f - nearness * 2f, 0f);
				prop.AddVelocity.Set(24f - 12f * (1f - nearness / 13f), 3f, 0f);
				prop.MinSize = Math.Max(1f, (float)Math.Pow(nearness, 1.6) / 17f);
				prop.MaxSize = prop.MinSize * 1.1f;
				prop.MinQuantity = 0.025f * dt * 33f;
				prop.AddQuantity = 0.1f * dt * 33f;
				PrepareParticleProps(dt);
				particleSystem.Spawn(prop);
			}
			for (ParticleBase particle = particleSystem.Pool.ParticlesPool.FirstAlive; particle != null; particle = particle.Next)
			{
				particle.SecondsAlive = (float)rand.NextDouble() * particle.LifeLength;
			}
		}
		PrepareParticleProps(dt);
		particleSystem.Spawn(prop);
	}

	private void PrepareParticleProps(float dt)
	{
		float nearness = 2.2f * (2.5f + (float)rand.NextDouble() * 4f);
		prop.MinVelocity.Set(-12f * (0.5f + nearness / 13f), -3f - nearness * 2f, 0f);
		prop.AddVelocity.Set(24f - 12f * (1f - nearness / 13f), 3f, 0f);
		prop.MinSize = Math.Max(1f, (float)Math.Pow(nearness, 1.6) / 17f);
		prop.MaxSize = prop.MinSize * 1.1f;
		prop.MinQuantity = 0.025f * dt * 33f;
		prop.AddQuantity = 0.1f * dt * 33f;
	}

	public void Dispose()
	{
		bgtex?.Dispose();
		bgtex = null;
		logoTexture?.Dispose();
		particleSystem?.Dispose();
	}
}
