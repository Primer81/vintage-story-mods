using System;
using System.Collections.Generic;
using System.Runtime;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;

public class GuiScreenMainRight : GuiScreen
{
	private LoadedTexture grayBg;

	private LoadedTexture quoteTexture;

	private float gcCollectAccum = -2f;

	private int gcCollectAttempts;

	private long renderStartMs;

	private string quote;

	public GuiScreenMainRight(ScreenManager screenManager, GuiScreen parent)
		: base(screenManager, parent)
	{
		ShowMainMenu = true;
		quote = getQuote();
	}

	public override void OnScreenLoaded()
	{
		ScreenManager.guiMainmenuLeft.updateButtonActiveFlag("home");
		gcCollectAttempts = 0;
		gcCollectAccum = -5f;
	}

	public void Compose()
	{
		ElementBounds dlgBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightBottom).WithFixedAlignmentOffset(-50.0, -50.0);
		ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 450.0, 170.0);
		CairoFont font = CairoFont.WhiteDetailText().WithFontSize(15f).WithLineHeightMultiplier(1.100000023841858);
		ElementComposer = ScreenManager.GuiComposers.Create("welcomedialog", dlgBounds).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding), withTitleBar: false, 5.0, 0.8f).BeginChildElements()
			.AddRichtext(Lang.Get("mainmenu-greeting"), font, textBounds.FlatCopy())
			.EndChildElements()
			.Compose();
		quoteTexture = ScreenManager.api.Gui.TextTexture.GenUnscaledTextTexture("„" + quote + "‟", CairoFont.WhiteDetailText().WithSlant(FontSlant.Italic));
		grayBg = new LoadedTexture(ScreenManager.api);
		ImageSurface surface = new ImageSurface(Format.Argb32, 1, 1);
		Context context = new Context(surface);
		context.SetSourceRGBA(0.0, 0.0, 0.0, 0.25);
		context.Paint();
		ScreenManager.api.Gui.LoadOrUpdateCairoTexture(surface, linearMag: true, ref grayBg);
		context.Dispose();
		surface.Dispose();
	}

	public override void RenderToDefaultFramebuffer(float dt)
	{
		Render(dt, ScreenManager.GamePlatform.EllapsedMs);
	}

	public void Render(float dt, long ellapsedMs, bool onlyBackground = false)
	{
		if (renderStartMs == 0L)
		{
			renderStartMs = ellapsedMs;
		}
		ensureLOHCompacted(dt);
		float windowSizeX = ScreenManager.GamePlatform.WindowSize.Width;
		float windowSizeY = ScreenManager.GamePlatform.WindowSize.Height;
		double x = ScreenManager.guiMainmenuLeft.Width + GuiElement.scaled(15.0);
		if (!onlyBackground)
		{
			ElementComposer.Render(dt);
			if (ElementComposer.MouseOverCursor != null)
			{
				FocusedMouseCursor = ElementComposer.MouseOverCursor;
			}
			ScreenManager.api.Render.Render2DTexturePremultipliedAlpha(grayBg.TextureId, ScreenManager.guiMainmenuLeft.Width, (double)(windowSizeY - (float)quoteTexture.Height) - GuiElement.scaled(10.0), windowSizeX, (double)quoteTexture.Height + GuiElement.scaled(10.0));
			ScreenManager.RenderMainMenuParts(dt, ElementComposer.Bounds, ShowMainMenu, darkenEdges: false);
			if (ScreenManager.mainMenuComposer.MouseOverCursor != null)
			{
				FocusedMouseCursor = ScreenManager.mainMenuComposer.MouseOverCursor;
			}
			ScreenManager.api.Render.Render2DTexturePremultipliedAlpha(quoteTexture.TextureId, x, (double)(windowSizeY - (float)quoteTexture.Height) - GuiElement.scaled(5.0), quoteTexture.Width, quoteTexture.Height);
			ElementComposer.PostRender(dt);
			ScreenManager.GamePlatform.UseMouseCursor((FocusedMouseCursor == null) ? "normal" : FocusedMouseCursor);
		}
	}

	private void ensureLOHCompacted(float dt)
	{
		if (ScreenManager.CurrentScreen is GuiScreenConnectingToServer || ScreenManager.CurrentScreen is GuiScreenExitingServer || gcCollectAttempts > 6)
		{
			return;
		}
		int thresholdMb = 300;
		long memMegaBytes = GC.GetTotalMemory(forceFullCollection: false) / 1024 / 1024;
		gcCollectAccum += dt;
		if (gcCollectAccum > 1f && memMegaBytes > thresholdMb)
		{
			if (ClientSettings.OptimizeRamMode > 0)
			{
				GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
				GC.Collect();
			}
			gcCollectAccum = 0f;
			gcCollectAttempts++;
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		quoteTexture?.Dispose();
		quoteTexture = null;
		grayBg?.Dispose();
		grayBg = null;
	}

	private string getQuote()
	{
		List<string> quotes = new List<string>();
		int i = 1;
		while (Lang.HasTranslation("mainscreen-quote" + i, findWildcarded: false))
		{
			quotes.Add(Lang.Get("mainscreen-quote" + i));
			i++;
		}
		Random rand = new Random();
		if (quotes.Count == 0)
		{
			return "";
		}
		return quotes[rand.Next(quotes.Count)];
	}
}
