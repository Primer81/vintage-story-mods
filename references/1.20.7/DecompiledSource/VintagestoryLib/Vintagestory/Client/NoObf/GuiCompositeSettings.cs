using System;
using System.Collections.Generic;
using System.Linq;
using Cairo;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.Gui;

namespace Vintagestory.Client.NoObf;

public class GuiCompositeSettings : GuiComposite
{
	public class LanguageConfig
	{
		public string Code;

		public string Englishname;

		public string Name;
	}

	private IGameSettingsHandler handler;

	private bool onMainscreen;

	private GuiComposer composer;

	private string startupLanguage = ClientSettings.Language;

	public bool IsInCreativeMode;

	private ElementBounds gButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

	private ElementBounds mButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

	private ElementBounds aButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

	private ElementBounds cButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

	private ElementBounds sButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

	private ElementBounds iButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

	private ElementBounds dButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

	private ElementBounds backButtonBounds = ElementBounds.Fixed(0.0, 0.0, 0.0, 40.0).WithFixedPadding(0.0, 3.0);

	private List<ConfigItem> mousecontrolItems = new List<ConfigItem>();

	private bool mousecontrolsTabActive;

	private List<ConfigItem> keycontrolItems = new List<ConfigItem>();

	private HotKey keyCombClone;

	private int? clickedItemIndex;

	private HotkeyCapturer hotkeyCapturer = new HotkeyCapturer();

	public string currentSearchText;

	private Dictionary<HotkeyType, int> sortOrder = new Dictionary<HotkeyType, int>
	{
		{
			HotkeyType.MovementControls,
			0
		},
		{
			HotkeyType.MouseModifiers,
			1
		},
		{
			HotkeyType.CharacterControls,
			2
		},
		{
			HotkeyType.HelpAndOverlays,
			3
		},
		{
			HotkeyType.GUIOrOtherControls,
			4
		},
		{
			HotkeyType.InventoryHotkeys,
			5
		},
		{
			HotkeyType.CreativeOrSpectatorTool,
			6
		},
		{
			HotkeyType.CreativeTool,
			7
		},
		{
			HotkeyType.DevTool,
			8
		},
		{
			HotkeyType.MouseControls,
			9
		}
	};

	private string[] titles = new string[9]
	{
		Lang.Get("Movement controls"),
		Lang.Get("Mouse click modifiers"),
		Lang.Get("Actions"),
		Lang.Get("In-game Help and Overlays"),
		Lang.Get("User interface & More"),
		Lang.Get("Inventory hotkeys"),
		Lang.Get("Creative mode"),
		Lang.Get("Creative mode"),
		Lang.Get("Debug and Macros")
	};

	public bool IsCapturingHotKey => hotkeyCapturer.IsCapturing();

	public GuiCompositeSettings(IGameSettingsHandler handler, bool onMainScreen)
	{
		this.handler = handler;
		onMainscreen = onMainScreen;
	}

	private GuiComposer ComposerHeader(string dialogName, string currentTab)
	{
		CairoFont fnt = CairoFont.ButtonText();
		updateButtonBounds();
		GuiComposer composerHeader;
		if (onMainscreen)
		{
			_ = ScreenManager.Platform.WindowSize.Width;
			_ = ScreenManager.Platform.WindowSize.Height;
			ElementBounds containerBounds = ElementBounds.Fixed(0.0, 0.0, 950.0, 740.0);
			aButtonBounds.ParentBounds = containerBounds;
			gButtonBounds.ParentBounds = containerBounds;
			mButtonBounds.ParentBounds = containerBounds;
			cButtonBounds.ParentBounds = containerBounds;
			sButtonBounds.ParentBounds = containerBounds;
			iButtonBounds.ParentBounds = containerBounds;
			dButtonBounds.ParentBounds = containerBounds;
			composerHeader = handler.dialogBase(dialogName + "main", containerBounds.fixedWidth, containerBounds.fixedHeight).BeginChildElements(containerBounds).AddToggleButton(Lang.Get("setting-graphics-header"), fnt, OnGraphicsOptions, gButtonBounds, "graphics")
				.AddToggleButton(Lang.Get("setting-mouse-header"), fnt, OnMouseOptions, mButtonBounds, "mouse")
				.AddToggleButton(Lang.Get("setting-controls-header"), fnt, OnControlOptions, cButtonBounds, "controls")
				.AddToggleButton(Lang.Get("setting-accessibility-header"), fnt, OnAccessibilityOptions, aButtonBounds, "accessibility")
				.AddToggleButton(Lang.Get("setting-sound-header"), fnt, OnSoundOptions, sButtonBounds, "sounds")
				.AddToggleButton(Lang.Get("setting-interface-header"), fnt, OnInterfaceOptions, iButtonBounds, "interface")
				.AddIf(ClientSettings.DeveloperMode)
				.AddToggleButton(Lang.Get("setting-dev-header"), fnt, OnDeveloperOptions, dButtonBounds, "developer")
				.EndIf();
		}
		else
		{
			ElementBounds dlgBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 75.0);
			double width = backButtonBounds.fixedX + backButtonBounds.fixedWidth + 35.0;
			dlgBounds.horizontalSizing = ElementSizing.Fixed;
			dlgBounds.fixedWidth = width;
			ElementBounds bgBounds = new ElementBounds().WithSizing(ElementSizing.FitToChildren).WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.horizontalSizing = ElementSizing.Fixed;
			bgBounds.fixedWidth = width - 2.0 * GuiStyle.ElementToDialogPadding;
			gButtonBounds.ParentBounds = bgBounds;
			aButtonBounds.ParentBounds = bgBounds;
			mButtonBounds.ParentBounds = bgBounds;
			cButtonBounds.ParentBounds = bgBounds;
			sButtonBounds.ParentBounds = bgBounds;
			iButtonBounds.ParentBounds = bgBounds;
			dButtonBounds.ParentBounds = bgBounds;
			backButtonBounds.ParentBounds = bgBounds;
			composerHeader = handler.GuiComposers.Create(dialogName + "ingame", dlgBounds).AddShadedDialogBG(bgBounds, withTitleBar: false).AddStaticCustomDraw(bgBounds, delegate(Context ctx, ImageSurface surface, ElementBounds bounds)
			{
				ctx.SetSourceRGBA(1.0, 1.0, 1.0, 0.1);
				GuiElement.RoundRectangle(ctx, GuiElement.scaled(5.0) + bounds.bgDrawX, GuiElement.scaled(5.0) + bounds.bgDrawY, bounds.OuterWidth - GuiElement.scaled(10.0), GuiElement.scaled(75.0), 1.0);
				ctx.Fill();
			})
				.BeginChildElements()
				.AddToggleButton(Lang.Get("setting-graphics-header"), fnt, OnGraphicsOptions, gButtonBounds, "graphics")
				.AddToggleButton(Lang.Get("setting-mouse-header"), fnt, OnMouseOptions, mButtonBounds, "mouse")
				.AddToggleButton(Lang.Get("setting-controls-header"), fnt, OnControlOptions, cButtonBounds, "controls")
				.AddToggleButton(Lang.Get("setting-accessibility-header"), fnt, OnAccessibilityOptions, aButtonBounds, "accessibility")
				.AddToggleButton(Lang.Get("setting-sound-header"), fnt, OnSoundOptions, sButtonBounds, "sounds")
				.AddToggleButton(Lang.Get("setting-interface-header"), fnt, OnInterfaceOptions, iButtonBounds, "interface")
				.AddIf(ClientSettings.DeveloperMode)
				.AddToggleButton(Lang.Get("setting-dev-header"), fnt, OnDeveloperOptions, dButtonBounds, "developer")
				.EndIf()
				.AddButton(Lang.Get("general-back"), delegate
				{
					clickedItemIndex = null;
					hotkeyCapturer?.EndCapture(wasCancelled: true);
					return handler.OnBackPressed();
				}, backButtonBounds);
		}
		composerHeader.GetToggleButton("graphics").SetValue(currentTab == "graphics");
		composerHeader.GetToggleButton("mouse").SetValue(currentTab == "mouse");
		composerHeader.GetToggleButton("controls").SetValue(currentTab == "controls");
		composerHeader.GetToggleButton("accessibility").SetValue(currentTab == "accessibility");
		composerHeader.GetToggleButton("sounds").SetValue(currentTab == "sounds");
		composerHeader.GetToggleButton("interface").SetValue(currentTab == "interface");
		composerHeader.GetToggleButton("developer")?.SetValue(currentTab == "developer");
		return composerHeader;
	}

	private void updateButtonBounds()
	{
		CairoFont cairoFont = CairoFont.ButtonText();
		double gWidth = cairoFont.GetTextExtents(Lang.Get("setting-graphics-header")).Width / (double)ClientSettings.GUIScale + 15.0;
		double mWidth = cairoFont.GetTextExtents(Lang.Get("setting-mouse-header")).Width / (double)ClientSettings.GUIScale + 15.0;
		double cWidth = cairoFont.GetTextExtents(Lang.Get("setting-controls-header")).Width / (double)ClientSettings.GUIScale + 15.0;
		double aWidth = cairoFont.GetTextExtents(Lang.Get("setting-accessibility-header")).Width / (double)ClientSettings.GUIScale + 15.0;
		double sWidth = cairoFont.GetTextExtents(Lang.Get("setting-sound-header")).Width / (double)ClientSettings.GUIScale + 15.0;
		double iWidth = cairoFont.GetTextExtents(Lang.Get("setting-interface-header")).Width / (double)ClientSettings.GUIScale + 15.0;
		double dWidth = cairoFont.GetTextExtents(Lang.Get("setting-dev-header")).Width / (double)ClientSettings.GUIScale + 15.0;
		double backWidth = cairoFont.GetTextExtents(Lang.Get("general-back")).Width / (double)ClientSettings.GUIScale + 15.0;
		gButtonBounds.WithFixedWidth(gWidth);
		mButtonBounds.WithFixedWidth(mWidth).FixedRightOf(gButtonBounds, 15.0);
		cButtonBounds.WithFixedWidth(cWidth).FixedRightOf(mButtonBounds, 15.0);
		aButtonBounds.WithFixedWidth(aWidth).FixedRightOf(cButtonBounds, 15.0);
		sButtonBounds.WithFixedWidth(sWidth).FixedRightOf(aButtonBounds, 15.0);
		iButtonBounds.WithFixedWidth(iWidth).FixedRightOf(sButtonBounds, 15.0);
		dButtonBounds.WithFixedWidth(dWidth).FixedRightOf(iButtonBounds, 15.0);
		backButtonBounds.WithFixedWidth(backWidth).FixedRightOf(ClientSettings.DeveloperMode ? dButtonBounds : iButtonBounds, 25.0);
	}

	internal bool OpenSettingsMenu()
	{
		OnGraphicsOptions(on: true);
		return true;
	}

	internal void OnGraphicsOptions(bool on)
	{
		int sliderWidth = 160;
		ElementBounds leftColumnLeftText = ElementBounds.Fixed(0.0, 82.0, 225.0, 42.0);
		ElementBounds leftColumnRightSlider = ElementBounds.Fixed(235.0, 85.0, sliderWidth, 20.0);
		ElementBounds rightColumnLeftText = ElementBounds.Fixed(470.0, 90.0, 225.0, 42.0);
		ElementBounds rightColumnRightSlider = ElementBounds.Fixed(705.0, 119.0, sliderWidth, 20.0);
		ElementBounds.Fixed(0.0, 0.0, 30.0, 30.0).WithFixedPadding(10.0, 2.0);
		string[] hoverTexts = new string[26]
		{
			(!handler.MaxViewDistanceAlarmValue.HasValue) ? Lang.Get("setting-hover-viewdist-singleplayer") : Lang.Get("setting-hover-viewdist"),
			Lang.Get("setting-hover-gamma"),
			Lang.Get("setting-hover-sepia"),
			Lang.Get("setting-hover-fov"),
			Lang.Get("setting-hover-guiscale"),
			Lang.Get("setting-hover-maxfps"),
			Lang.Get("setting-hover-resolution"),
			Lang.Get("setting-hover-smoothshadows"),
			Lang.Get("setting-hover-vsync"),
			Lang.Get("setting-hover-fxaa"),
			Lang.Get("setting-hover-bloom"),
			Lang.Get("setting-hover-contrast"),
			Lang.Get("setting-hover-godrays"),
			Lang.Get("setting-hover-particles"),
			Lang.Get("setting-hover-grasswaves"),
			Lang.Get("setting-hover-dynalight"),
			Lang.Get("setting-hover-dynashade"),
			"",
			Lang.Get("setting-hover-hqanimation"),
			Lang.Get("setting-hover-optimizeram"),
			Lang.Get("setting-hover-occlusionculling"),
			Lang.Get("setting-hover-foamandshinyeffect"),
			Lang.Get("setting-hover-ssao"),
			"setting-hover-radeonhdfix",
			Lang.Get("setting-hover-instancedgrass"),
			Lang.Get("setting-hover-chunkuploadratelimiter")
		};
		string[] presetIds = GraphicsPreset.Presets.Select((GraphicsPreset p) => p.PresetId.ToString() ?? "").ToArray();
		string[] presetNames = GraphicsPreset.Presets.Select((GraphicsPreset p) => Lang.Get(p.Langcode) ?? "").ToArray();
		string linktext = (ClientSettings.ShowMoreGfxOptions ? Lang.Get("general-lessoptions") : Lang.Get("general-moreoptions"));
		CairoFont titleFont = CairoFont.WhiteSmallishText().Clone().WithWeight(FontWeight.Bold);
		titleFont.Color[3] = 0.6;
		GuiComposer guiComposer = ComposerHeader("gamesettings-graphics", "graphics");
		RichTextComponentBase[] components = new RichTextComponent[1]
		{
			new LinkTextComponent(handler.Api, linktext, CairoFont.WhiteDetailText(), OnMoreOptions)
		};
		GuiComposer guiComposer2 = guiComposer.AddRichtext(components, rightColumnLeftText = rightColumnLeftText.FlatCopy()).AddStaticText(Lang.Get("setting-column-appear"), titleFont, rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, 10.0).WithFixedWidth(250.0)).AddStaticText(Lang.Get("setting-name-gamma"), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, -4.0).WithFixedWidth(200.0))
			.AddSlider(onGammaChanged, rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 45.0), "gammaSlider")
			.AddHoverText(hoverTexts[1], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0))
			.AddStaticText(Lang.Get("setting-name-dynamiccolorgrading"), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy())
			.AddHoverText(Lang.Get("setting-hover-dynamiccolorgrading"), CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onDynamicGradingToggled, rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 15.0), "dynamicColorGradingSwitch")
			.AddStaticText(Lang.Get("setting-name-contrast"), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy())
			.AddSlider(onContrastChanged, rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 21.0).WithFixedSize(sliderWidth, 20.0), "contrastSlider")
			.AddHoverText(hoverTexts[11], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0))
			.AddStaticText(Lang.Get("setting-name-sepia"), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy())
			.AddSlider(onSepiaLevelChanged, rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 21.0), "sepiaSlider")
			.AddHoverText(hoverTexts[2], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0))
			.AddStaticText(Lang.Get("setting-name-abloom"), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy())
			.AddSlider(onAmbientBloomChanged, rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 21.0).WithFixedSize(sliderWidth, 20.0), "ambientBloomSlider")
			.AddHoverText(hoverTexts[11], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0))
			.AddStaticText(Lang.Get("setting-name-fov"), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy());
		ActionConsumable<int> onNewSliderValue = onVowChanged;
		ElementBounds elementBounds = rightColumnRightSlider.BelowCopy(0.0, 21.0);
		composer = GuiComposerHelpers.AddDropDown(bounds: rightColumnRightSlider = elementBounds.BelowCopy(0.0, 18.0).WithFixedSize(sliderWidth, 26.0), composer: guiComposer2.AddSlider(onNewSliderValue, elementBounds, "fovSlider").AddHoverText(hoverTexts[3], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0)).AddStaticText(Lang.Get("setting-name-windowmode"), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, -2.0)), values: new string[4] { "0", "1", "2", "3" }, names: new string[4]
		{
			Lang.Get("windowmode-normal"),
			Lang.Get("windowmode-fullscreen"),
			Lang.Get("windowmode-maxborderless"),
			Lang.Get("windowmode-fullscreen-ontop")
		}, selectedIndex: GetWindowModeIndex(), onSelectionChanged: OnWindowModeChanged, key: "windowModeSwitch");
		if (ClientSettings.ShowMoreGfxOptions)
		{
			GuiComposer guiComposer3 = composer.AddStaticText(Lang.Get("setting-name-maxfps"), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, -3.0).WithFixedHeight(40.0)).AddSlider(onMaxFpsChanged, rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 15.0).WithFixedSize(sliderWidth, 20.0), "maxFpsSlider").AddHoverText(hoverTexts[5], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0))
				.AddStaticText(Lang.Get("setting-name-vsync"), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, 4.0))
				.AddHoverText(hoverTexts[8], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0));
			string[] values = new string[3] { "0", "1", "2" };
			string[] names = new string[3]
			{
				Lang.Get("Off"),
				Lang.Get("On"),
				Lang.Get("On + Sleep")
			};
			int vsyncMode = ClientSettings.VsyncMode;
			SelectionChangedDelegate onSelectionChanged = onVsyncChanged;
			ElementBounds elementBounds2 = rightColumnRightSlider.BelowCopy(0.0, 21.0).WithFixedSize(sliderWidth, 26.0);
			GuiComposer guiComposer4 = GuiComposerHelpers.AddDropDown(bounds: rightColumnRightSlider = elementBounds2.BelowCopy(0.0, 18.0).WithFixedSize(sliderWidth, 26.0), composer: guiComposer3.AddDropDown(values, names, vsyncMode, onSelectionChanged, elementBounds2, "vsyncMode").AddStaticText(Lang.Get("setting-name-optimizeram"), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, 5.0)).AddHoverText(hoverTexts[19], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0)), values: new string[2] { "1", "2" }, names: new string[2]
			{
				Lang.Get("Optimize somewhat"),
				Lang.Get("Aggressively optimize ram")
			}, selectedIndex: ClientSettings.OptimizeRamMode - 1, onSelectionChanged: onOptimizeRamChanged, key: "optimizeRamMode").AddStaticText(Lang.Get("setting-name-occlusionculling"), CairoFont.WhiteSmallishText(), rightColumnLeftText = rightColumnLeftText.BelowCopy(0.0, 3.0)).AddHoverText(hoverTexts[20], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0))
				.AddSwitch(onOcclusionCullingChanged, rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 17.0), "occlusionCullingSwitch");
			string text = Lang.Get("setting-name-windowborder");
			CairoFont font = CairoFont.WhiteSmallishText();
			ElementBounds elementBounds3 = rightColumnLeftText.BelowCopy(0.0, 4.0);
			GuiComposerHelpers.AddStaticText(bounds: rightColumnLeftText = elementBounds3.BelowCopy(0.0, 3.0).WithFixedHeight(40.0), composer: guiComposer4.AddStaticText(text, font, elementBounds3).AddDropDown(new string[3] { "0", "1", "2" }, new string[3]
			{
				Lang.Get("windowborder-resizable"),
				Lang.Get("windowborder-fixed"),
				Lang.Get("windowborder-hidden")
			}, (int)ScreenManager.Platform.WindowBorder, OnWindowBorderChanged, rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 18.0).WithFixedSize(sliderWidth, 26.0), "windowBorder"), text: Lang.Get("setting-name-chunkuploadratelimiter"), font: CairoFont.WhiteSmallishText()).AddHoverText(hoverTexts[25], CairoFont.WhiteSmallText(), 250, rightColumnLeftText.FlatCopy().WithFixedHeight(25.0)).AddSlider(onLagspikeReductionChanged, rightColumnRightSlider = rightColumnRightSlider.BelowCopy(0.0, 18.0).WithFixedSize(sliderWidth, 20.0), "uploadRateLimiterSlider");
		}
		composer.AddStaticText(Lang.Get("setting-name-preset"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.FlatCopy().WithFixedOffset(0.0, 5.0)).AddDropDown(presetIds, presetNames, ClientSettings.GraphicsPresetId, onPresetChanged, leftColumnRightSlider = leftColumnRightSlider.FlatCopy().WithFixedSize(sliderWidth, 30.0), "graphicsPreset").AddStaticText(Lang.Get("setting-column-graphics"), titleFont, leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 15.0))
			.AddStaticText(Lang.Get("setting-name-viewdist"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, -6.0))
			.AddSlider(onViewdistanceChanged, leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 68.0).WithFixedSize(sliderWidth, 20.0), "viewDistanceSlider")
			.AddHoverText(hoverTexts[0], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0));
		if (ClientSettings.ShowMoreGfxOptions)
		{
			composer.AddStaticText(Lang.Get("setting-name-smoothshadows"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy()).AddHoverText(hoverTexts[7], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0)).AddSwitch(onSmoothShadowsToggled, leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 15.0), "smoothShadowsLever")
				.AddStaticText(Lang.Get("setting-name-fxaa"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy())
				.AddHoverText(hoverTexts[9], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0))
				.AddSwitch(onFxaaChanged, leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 12.0), "FxaaSwitch")
				.AddStaticText(Lang.Get("setting-name-grasswaves"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy())
				.AddHoverText(hoverTexts[14], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0))
				.AddSwitch(onWavingFoliageChanged, leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 12.0), "wavingFoliageSwitch")
				.AddStaticText(Lang.Get("setting-name-foamandshinyeffect"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy())
				.AddHoverText(hoverTexts[21], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0))
				.AddSwitch(onFoamAndShinyEffectChanged, leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 12.0), "liquidFoamEffectSwitch")
				.AddStaticText(Lang.Get("setting-name-bloom"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy())
				.AddHoverText(hoverTexts[10], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0))
				.AddSwitch(onBloomChanged, leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 12.0), "BloomSwitch")
				.AddStaticText(Lang.Get("setting-name-godrays"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy().WithFixedHeight(39.0))
				.AddSwitch(onGodRaysToggled, leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 15.0).WithFixedSize(sliderWidth, 20.0), "godraySwitch")
				.AddHoverText(hoverTexts[12], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0))
				.AddStaticText(Lang.Get("setting-name-ssao"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 6.0).WithFixedHeight(36.0))
				.AddHoverText(hoverTexts[22], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0))
				.AddSlider(onSsaoChanged, leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 17.0).WithFixedSize(sliderWidth, 20.0), "ssaoSlider")
				.AddStaticText(Lang.Get("setting-name-shadows"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 3.0))
				.AddSlider(onShadowsChanged, leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 21.0).WithFixedSize(sliderWidth, 20.0), "shadowsSlider")
				.AddHoverText(hoverTexts[16], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0))
				.AddStaticText(Lang.Get("setting-name-particles"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 4.0))
				.AddSlider(onParticleLevelChanged, leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 21.0).WithFixedSize(sliderWidth, 20.0), "particleSlider")
				.AddHoverText(hoverTexts[13], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0))
				.AddStaticText(Lang.Get("setting-name-dynalight"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 5.0))
				.AddSlider(onDynamicLightsChanged, leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 21.0).WithFixedSize(sliderWidth, 20.0), "dynamicLightsSlider")
				.AddHoverText(hoverTexts[15], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0))
				.AddStaticText(Lang.Get("setting-name-resolution"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 7.0))
				.AddHoverText(hoverTexts[6], CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0))
				.AddSlider(onResolutionChanged, leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 21.0), "resolutionSlider")
				.AddStaticText(Lang.Get("setting-name-lodbiasfar"), CairoFont.WhiteSmallishText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 7.0))
				.AddHoverText(Lang.Get("setting-hover-lodbiasfar"), CairoFont.WhiteSmallText(), 250, leftColumnLeftText.FlatCopy().WithFixedHeight(25.0))
				.AddSlider(onLodbiasFarChanged, leftColumnRightSlider = leftColumnRightSlider.BelowCopy(0.0, 21.0), "lodbiasfarSlider")
				.AddRichtext(Lang.Get("help-framerateissues"), CairoFont.WhiteDetailText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 5.0))
				.EndChildElements();
		}
		else
		{
			composer.AddRichtext(Lang.Get("help-moresettingsavailable", linktext), CairoFont.WhiteDetailText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 225.0, 440.0)).AddRichtext(Lang.Get("help-framerateissues"), CairoFont.WhiteDetailText(), leftColumnLeftText = leftColumnLeftText.BelowCopy(0.0, 125.0));
		}
		composer.GetDropDown("graphicsPreset").listMenu.MaxHeight = 330;
		composer.Compose();
		handler.LoadComposer(composer);
		SetGfxValues();
	}

	private void onDynamicGradingToggled(bool on)
	{
		ClientSettings.DynamicColorGrading = on;
		composer.GetSlider("sepiaSlider").Enabled = !ClientSettings.DynamicColorGrading;
		composer.GetSlider("contrastSlider").Enabled = !ClientSettings.DynamicColorGrading;
	}

	private void onVsyncChanged(string newvalue, bool selected)
	{
		ClientSettings.VsyncMode = newvalue.ToInt();
	}

	private void OnMoreOptions(LinkTextComponent comp)
	{
		ClientSettings.ShowMoreGfxOptions = !ClientSettings.ShowMoreGfxOptions;
		OnGraphicsOptions(on: true);
	}

	private void SetGfxValues()
	{
		composer.GetSlider("viewDistanceSlider").SetValues(ClientSettings.ViewDistance, 32, 1536, 32, " blocks");
		composer.GetSlider("viewDistanceSlider").OnSliderTooltip = delegate(int value)
		{
			string text = Lang.Get("createworld-worldheight", value);
			return (value <= 512) ? text : (text + "\n" + Lang.Get("vram-warning"));
		};
		composer.GetSlider("viewDistanceSlider").TriggerOnlyOnMouseUp();
		if (handler.MaxViewDistanceAlarmValue.HasValue)
		{
			composer.GetSlider("viewDistanceSlider").SetAlarmValue(handler.MaxViewDistanceAlarmValue.Value);
		}
		if (ClientSettings.ShowMoreGfxOptions)
		{
			composer.GetSwitch("smoothShadowsLever").On = ClientSettings.SmoothShadows;
			composer.GetSwitch("FxaaSwitch").On = ClientSettings.FXAA;
			composer.GetDropDown("optimizeRamMode").SetSelectedIndex(ClientSettings.OptimizeRamMode - 1);
			composer.GetSwitch("occlusionCullingSwitch").On = ClientSettings.Occlusionculling;
			composer.GetSwitch("wavingFoliageSwitch").On = ClientSettings.WavingFoliage;
			composer.GetSwitch("liquidFoamEffectSwitch").On = ClientSettings.LiquidFoamAndShinyEffect;
			composer.GetSwitch("BloomSwitch").On = ClientSettings.Bloom;
			composer.GetSwitch("godraySwitch").On = ClientSettings.GodRayQuality > 0;
			composer.GetSlider("ambientBloomSlider").SetValues((int)ClientSettings.AmbientBloomLevel, 0, 100, 10, "%");
			composer.GetSlider("ambientBloomSlider").TriggerOnlyOnMouseUp();
			composer.GetSlider("ssaoSlider").SetValues(ClientSettings.SSAOQuality, 0, 2, 1);
			string[] qualityssao = new string[3]
			{
				Lang.Get("Off"),
				Lang.Get("Medium quality"),
				Lang.Get("High quality")
			};
			composer.GetSlider("ssaoSlider").OnSliderTooltip = (int value) => qualityssao[value];
			composer.GetSlider("ssaoSlider").ComposeHoverTextElement();
			composer.GetSlider("ssaoSlider").TriggerOnlyOnMouseUp();
			composer.GetSlider("uploadRateLimiterSlider").SetValues(ClientSettings.ChunkVerticesUploadRateLimiter, 0, 8, 1);
			composer.GetSlider("uploadRateLimiterSlider").TriggerOnlyOnMouseUp();
			composer.GetSlider("shadowsSlider").SetValues(ClientSettings.ShadowMapQuality, 0, 4, 1);
			string[] quality2 = new string[5]
			{
				Lang.Get("Off"),
				Lang.Get("Low quality"),
				Lang.Get("Medium quality"),
				Lang.Get("High quality"),
				Lang.Get("Very high quality")
			};
			composer.GetSlider("shadowsSlider").OnSliderTooltip = (int value) => quality2[value];
			composer.GetSlider("shadowsSlider").ComposeHoverTextElement();
			composer.GetSlider("shadowsSlider").TriggerOnlyOnMouseUp();
			composer.GetSlider("particleSlider").SetValues(ClientSettings.ParticleLevel, 0, 100, 2, " %");
			composer.GetSlider("dynamicLightsSlider").SetValues(ClientSettings.MaxDynamicLights, 0, 100, 1, " " + Lang.Get("units-lightsources"));
			composer.GetSlider("dynamicLightsSlider").OnSliderTooltip = (int value) => (value != 0) ? (value + " " + Lang.Get("units-lightsources")) : Lang.Get("disabled");
			composer.GetSlider("dynamicLightsSlider").TriggerOnlyOnMouseUp();
			composer.GetSlider("resolutionSlider").SetValues((int)(ClientSettings.SSAA * 100f), 25, 100, 25, " %");
			composer.GetSlider("resolutionSlider").OnSliderTooltip = delegate(int value)
			{
				float num2 = (float)value / 100f;
				return num2 + "x (" + (int)(num2 * num2 * 100f) + "%)";
			};
			composer.GetSlider("resolutionSlider").TriggerOnlyOnMouseUp();
			composer.GetSlider("lodbiasfarSlider").SetValues((int)(ClientSettings.LodBiasFar * 100f), 35, 100, 1, " %");
			composer.GetSlider("lodbiasfarSlider").OnSliderTooltip = delegate(int value)
			{
				float num = (float)value / 100f;
				return num + "x (" + (int)(num * num * 100f) + "%)";
			};
			composer.GetSlider("lodbiasfarSlider").TriggerOnlyOnMouseUp();
		}
		composer.GetSlider("gammaSlider").Enabled = true;
		composer.GetSlider("gammaSlider").OnSliderTooltip = null;
		composer.GetSlider("gammaSlider").ComposeHoverTextElement();
		composer.GetSlider("gammaSlider").SetValues((int)Math.Round(ClientSettings.GammaLevel * 100f), 30, 300, 5);
		composer.GetSwitch("dynamicColorGradingSwitch").On = ClientSettings.DynamicColorGrading;
		composer.GetSlider("sepiaSlider").SetValues((int)(ClientSettings.SepiaLevel * 100f), 0, 100, 5);
		composer.GetSlider("sepiaSlider").Enabled = !ClientSettings.DynamicColorGrading;
		composer.GetSlider("contrastSlider").SetValues((int)(ClientSettings.ExtraContrastLevel * 100f) + 100, 100, 200, 10, "%");
		composer.GetSlider("contrastSlider").Enabled = !ClientSettings.DynamicColorGrading;
		composer.GetSlider("fovSlider").SetValues(ClientSettings.FieldOfView, 20, 150, 1, "°");
		composer.GetDropDown("windowModeSwitch").SetSelectedIndex(GetWindowModeIndex());
		if (ClientSettings.ShowMoreGfxOptions)
		{
			composer.GetSlider("maxFpsSlider").SetValues(GameMath.Clamp(ClientSettings.MaxFPS, 15, 241), 15, 241, 1);
			composer.GetSlider("maxFpsSlider").OnSliderTooltip = (int value) => (value != 241) ? value.ToString() : Lang.Get("unlimited");
			composer.GetSlider("maxFpsSlider").ComposeHoverTextElement();
			composer.GetDropDown("vsyncMode").SetSelectedIndex(ClientSettings.VsyncMode);
		}
	}

	internal static int GetWindowModeIndex()
	{
		int windowMode = ClientSettings.GameWindowMode;
		if (ClientSettings.GameWindowMode == 2 && ScreenManager.Platform.WindowBorder != EnumWindowBorder.Hidden)
		{
			windowMode = 0;
		}
		return windowMode;
	}

	private void onPresetChanged(string id, bool on)
	{
		GraphicsPreset preset = GraphicsPreset.Presets[int.Parse(id)];
		if (!(preset.Langcode == "preset-custom"))
		{
			ClientSettings.GraphicsPresetId = preset.PresetId;
			ClientSettings.ViewDistance = preset.ViewDistance;
			ClientSettings.SmoothShadows = preset.SmoothLight;
			ClientSettings.FXAA = preset.FXAA;
			ClientSettings.SSAOQuality = preset.SSAO;
			ClientSettings.WavingFoliage = preset.WavingFoliage;
			ClientSettings.LiquidFoamAndShinyEffect = preset.LiquidFoamEffect;
			ClientSettings.Bloom = preset.Bloom;
			ClientSettings.GodRayQuality = (preset.GodRays ? 1 : 0);
			ClientSettings.ShadowMapQuality = preset.ShadowMapQuality;
			ClientSettings.ParticleLevel = preset.ParticleLevel;
			ClientSettings.MaxDynamicLights = preset.DynamicLights;
			ClientSettings.SSAA = preset.Resolution;
			ClientSettings.MaxFPS = preset.MaxFps;
			ClientSettings.LodBiasFar = preset.LodBiasFar;
			SetGfxValues();
			ScreenManager.Platform.RebuildFrameBuffers();
			handler.ReloadShaders();
		}
	}

	private void SetCustomPreset()
	{
		GraphicsPreset preset = GraphicsPreset.Presets.Where((GraphicsPreset p) => p.Langcode == "preset-custom").FirstOrDefault();
		ClientSettings.GraphicsPresetId = preset.PresetId;
		composer.GetDropDown("graphicsPreset").SetSelectedIndex(preset.PresetId);
	}

	private void OnWindowModeChanged(string code, bool selected)
	{
		SetWindowMode(code.ToInt());
	}

	internal static void SetWindowMode(int mode)
	{
		switch (mode)
		{
		case 3:
			ScreenManager.Platform.SetWindowAttribute(WindowAttribute.AutoIconify, value: false);
			ScreenManager.Platform.SetWindowState(WindowState.Fullscreen);
			ClientSettings.GameWindowMode = 3;
			break;
		case 2:
			ClientSettings.WindowBorder = 2;
			ScreenManager.Platform.WindowBorder = EnumWindowBorder.Hidden;
			if (ScreenManager.Platform.GetWindowState() == WindowState.Maximized)
			{
				ScreenManager.Platform.SetWindowState(WindowState.Normal);
			}
			ScreenManager.Platform.SetWindowState(WindowState.Maximized);
			ClientSettings.GameWindowMode = 2;
			break;
		case 1:
			ScreenManager.Platform.SetWindowAttribute(WindowAttribute.AutoIconify, value: true);
			ScreenManager.Platform.SetWindowState(WindowState.Fullscreen);
			ClientSettings.GameWindowMode = 1;
			break;
		default:
			ScreenManager.Platform.SetWindowState(WindowState.Normal);
			if (ScreenManager.Platform.WindowBorder != 0)
			{
				ScreenManager.Platform.WindowBorder = EnumWindowBorder.Resizable;
				ClientSettings.WindowBorder = 0;
			}
			ClientSettings.GameWindowMode = 0;
			break;
		}
	}

	private void OnWindowBorderChanged(string newval, bool on)
	{
		int.TryParse(newval, out var val);
		ClientSettings.WindowBorder = val;
		if (ClientSettings.GameWindowMode == 2 && val != 2)
		{
			ClientSettings.GameWindowMode = 0;
		}
	}

	private void onOptimizeRamChanged(string code, bool selected)
	{
		ClientSettings.OptimizeRamMode = code.ToInt();
	}

	private void onOcclusionCullingChanged(bool on)
	{
		ClientSettings.Occlusionculling = on;
	}

	private bool onResolutionChanged(int newval)
	{
		ClientSettings.SSAA = (float)newval / 100f;
		ScreenManager.Platform.RebuildFrameBuffers();
		SetCustomPreset();
		return true;
	}

	private bool onLodbiasFarChanged(int newval)
	{
		ClientSettings.LodBiasFar = (float)newval / 100f;
		SetCustomPreset();
		return true;
	}

	private bool onDynamicLightsChanged(int value)
	{
		ClientSettings.MaxDynamicLights = value;
		handler.ReloadShaders();
		SetCustomPreset();
		return true;
	}

	private void onWavingFoliageChanged(bool on)
	{
		ClientSettings.WavingFoliage = on;
		handler.ReloadShaders();
		SetCustomPreset();
	}

	private void onFoamAndShinyEffectChanged(bool on)
	{
		ClientSettings.LiquidFoamAndShinyEffect = on;
		handler.ReloadShaders();
		SetCustomPreset();
	}

	private bool onParticleLevelChanged(int level)
	{
		ClientSettings.ParticleLevel = level;
		SetCustomPreset();
		return true;
	}

	private bool onMaxFpsChanged(int fps)
	{
		ClientSettings.MaxFPS = fps;
		return true;
	}

	private bool onSepiaLevelChanged(int value)
	{
		ClientSettings.SepiaLevel = (float)value / 100f;
		return true;
	}

	private bool onGammaChanged(int value)
	{
		ClientSettings.GammaLevel = (float)value / 100f;
		return true;
	}

	private void onGodRaysToggled(bool on)
	{
		ClientSettings.GodRayQuality = (on ? 1 : 0);
		handler.ReloadShaders();
		SetCustomPreset();
	}

	private bool onShadowsChanged(int newvalue)
	{
		ClientSettings.ShadowMapQuality = newvalue;
		ScreenManager.Platform.RebuildFrameBuffers();
		handler.ReloadShaders();
		SetCustomPreset();
		return true;
	}

	private bool onLagspikeReductionChanged(int newvalue)
	{
		ClientSettings.ChunkVerticesUploadRateLimiter = newvalue;
		return true;
	}

	private bool onAmbientBloomChanged(int newvalue)
	{
		ClientSettings.AmbientBloomLevel = newvalue;
		handler.ReloadShaders();
		SetCustomPreset();
		return true;
	}

	private bool onContrastChanged(int newvalue)
	{
		ClientSettings.ExtraContrastLevel = (float)(newvalue - 100) / 100f;
		SetCustomPreset();
		return true;
	}

	private void onBloomChanged(bool on)
	{
		ClientSettings.Bloom = on;
		handler.ReloadShaders();
		SetCustomPreset();
	}

	private bool onVowChanged(int newvalue)
	{
		ClientSettings.FieldOfView = newvalue;
		return true;
	}

	private bool onGuiScaleChanged(int newsize)
	{
		ClientSettings.GUIScale = (float)newsize / 8f;
		updateButtonBounds();
		return true;
	}

	private void onFxaaChanged(bool fxaa)
	{
		ClientSettings.FXAA = fxaa;
		handler.ReloadShaders();
		SetCustomPreset();
	}

	private bool onSsaoChanged(int ssao)
	{
		ClientSettings.SSAOQuality = ssao;
		if (!handler.IsIngame)
		{
			ScreenManager.Platform.RebuildFrameBuffers();
			handler.ReloadShaders();
		}
		SetCustomPreset();
		return true;
	}

	internal void onSmoothShadowsToggled(bool newstate)
	{
		ClientSettings.SmoothShadows = newstate;
		SetCustomPreset();
	}

	internal bool onViewdistanceChanged(int newvalue)
	{
		ClientSettings.ViewDistance = newvalue;
		SetCustomPreset();
		return true;
	}

	private void OnMouseOptions(bool on)
	{
		mousecontrolsTabActive = true;
		LoadMouseCombinations();
		ElementBounds leftText = ElementBounds.Fixed(0.0, 85.0, 320.0, 42.0);
		ElementBounds rightSlider = ElementBounds.Fixed(340.0, 89.0, 200.0, 20.0);
		ElementBounds configListBounds = ElementBounds.Fixed(0.0, 0.0, 900.0 - 2.0 * GuiStyle.ElementToDialogPadding - 35.0, onMainscreen ? 140 : 114);
		ElementBounds insetBounds = configListBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
		ElementBounds clipBounds = configListBounds.FlatCopy().WithParent(insetBounds);
		GuiComposer guiComposer = ComposerHeader("gamesettings-mouse", "mouse").AddStaticText(Lang.Get("setting-name-mousesensivity"), CairoFont.WhiteSmallishText(), leftText.FlatCopy()).AddSlider(onMouseSensivityChanged, rightSlider = rightSlider.FlatCopy(), "mouseSensivitySlider").AddStaticText(Lang.Get("setting-name-mousesmoothing"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy())
			.AddSlider(onMouseSmoothingChanged, rightSlider = rightSlider.BelowCopy(0.0, 21.0), "mouseSmoothingSlider")
			.AddStaticText(Lang.Get("setting-name-mousewheelsensivity"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy())
			.AddSlider(onMouseWheelSensivityChanged, rightSlider = rightSlider.BelowCopy(0.0, 21.0), "mouseWheelSensivitySlider")
			.AddStaticText(Lang.Get("setting-name-directmousemode"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 3.0))
			.AddSwitch(onMouseModeChanged, rightSlider = rightSlider.BelowCopy(0.0, 21.0), "directMouseModeSwitch")
			.AddHoverText(Lang.Get("setting-hover-directmousemode"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddStaticText(Lang.Get("setting-name-invertyaxis"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 3.0))
			.AddSwitch(onInvertYAxisChanged, rightSlider = rightSlider.BelowCopy(0.0, 21.0), "invertYAxisSwitch");
		string text = Lang.Get("setting-name-itemCollectMode");
		CairoFont font = CairoFont.WhiteSmallishText();
		ElementBounds elementBounds = leftText.BelowCopy(0.0, 2.0);
		composer = GuiComposerHelpers.AddStaticText(bounds: leftText = elementBounds.BelowCopy(0.0, 20.0), composer: guiComposer.AddStaticText(text, font, elementBounds).AddDropDown(new string[2] { "0", "1" }, new string[2]
		{
			Lang.Get("Always collect items"),
			Lang.Get("Only collect items when sneaking")
		}, ClientSettings.ItemCollectMode, onCollectionModeChange, rightSlider = rightSlider.BelowCopy(0.0, 12.0).WithFixedWidth(200.0), "itemCollectionMode"), text: Lang.Get("mousecontrols"), font: CairoFont.WhiteSmallishText()).AddHoverText(Lang.Get("hover-mousecontrols"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(60.0)).AddInset(insetBounds.FixedUnder(leftText, -8.0), 3, 0.8f)
			.BeginClip(clipBounds)
			.AddConfigList(mousecontrolItems, OnMouseControlItemClick, CairoFont.WhiteSmallText().WithFontSize(18f), configListBounds, "configlist")
			.EndClip()
			.AddIf(onMainscreen)
			.AddStaticText(Lang.Get("mousecontrols-mainmenuwarning"), CairoFont.WhiteSmallText(), leftText = leftText.BelowCopy(0.0, 112.0, 500.0))
			.EndIf()
			.EndChildElements()
			.Compose();
		handler.LoadComposer(composer);
		composer.GetSlider("mouseWheelSensivitySlider").SetValues((int)(ClientSettings.MouseWheelSensivity * 10f), 1, 100, 1);
		composer.GetSlider("mouseWheelSensivitySlider").OnSliderTooltip = (int value) => (float)value / 10f + "x";
		composer.GetSlider("mouseWheelSensivitySlider").ComposeHoverTextElement();
		composer.GetSlider("mouseSensivitySlider").SetValues(ClientSettings.MouseSensivity, 1, 200, 5);
		composer.GetSlider("mouseSmoothingSlider").SetValues(100 - ClientSettings.MouseSmoothing, 0, 95, 5);
		composer.GetSwitch("directMouseModeSwitch").SetValue(ClientSettings.DirectMouseMode);
		composer.GetSwitch("invertYAxisSwitch").SetValue(ClientSettings.InvertMouseYAxis);
	}

	private void OnMouseControlItemClick(int index, int indexNoTitle)
	{
		if (!clickedItemIndex.HasValue)
		{
			mousecontrolItems[index].Value = "?";
			clickedItemIndex = index;
			int hotkeyIndex = (int)mousecontrolItems[clickedItemIndex.Value].Data;
			composer.GetConfigList("configlist").Refresh();
			string code = ScreenManager.hotkeyManager.HotKeys.GetKeyAtIndex(hotkeyIndex);
			HotKey keyComb = ScreenManager.hotkeyManager.HotKeys[code];
			keyCombClone = keyComb.Clone();
			hotkeyCapturer.BeginCapture();
			keyCombClone.CurrentMapping = hotkeyCapturer.CapturingKeyComb;
		}
	}

	private void LoadMouseCombinations()
	{
		int hotkeyIndex = -1;
		if (mousecontrolItems.Count >= clickedItemIndex)
		{
			hotkeyIndex = (int)mousecontrolItems[clickedItemIndex.Value].Data;
		}
		mousecontrolItems.Clear();
		int i = 0;
		List<ConfigItem>[] sortedItems = new List<ConfigItem>[sortOrder.Count];
		for (int k = 0; k < sortedItems.Length; k++)
		{
			sortedItems[k] = new List<ConfigItem>();
		}
		mousecontrolItems.Add(new ConfigItem
		{
			Type = EnumItemType.Title,
			Key = Lang.Get("mouseactions")
		});
		foreach (KeyValuePair<string, HotKey> val in ScreenManager.hotkeyManager.HotKeys)
		{
			HotKey kc = val.Value;
			if (clickedItemIndex.HasValue && i == hotkeyIndex)
			{
				kc = keyCombClone;
			}
			string text = "?";
			if (kc.CurrentMapping != null)
			{
				text = kc.CurrentMapping.ToString();
			}
			ConfigItem item = new ConfigItem
			{
				Code = val.Key,
				Key = kc.Name,
				Value = text,
				Data = i
			};
			int index = mousecontrolItems.FindIndex((ConfigItem configitem) => configitem.Value == text);
			if (index != -1)
			{
				item.error = true;
				mousecontrolItems[index].error = true;
			}
			sortedItems[sortOrder[kc.KeyCombinationType]].Add(item);
			i++;
		}
		for (int j = 9; j < sortedItems.Length; j++)
		{
			mousecontrolItems.AddRange(sortedItems[j]);
		}
	}

	private void OnControlOptions(bool on)
	{
		mousecontrolsTabActive = false;
		LoadKeyCombinations();
		ElementBounds configListBounds = ElementBounds.Fixed(0.0, 0.0, 900.0 - 2.0 * GuiStyle.ElementToDialogPadding - 35.0, 400.0);
		ElementBounds insetBounds = configListBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
		ElementBounds clipBounds = configListBounds.FlatCopy().WithParent(insetBounds);
		ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(insetBounds);
		ElementBounds leftText = ElementBounds.Fixed(0.0, 41.0, 360.0, 42.0);
		ElementBounds rightSlider = ElementBounds.Fixed(490.0, 38.0, 200.0, 20.0);
		composer = ComposerHeader("gamesettings-controls", "controls").AddStaticText(Lang.Get("setting-name-noseparatectrlkeys"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 10.0, 120.0)).AddSwitch(onSeparateCtrl, rightSlider = rightSlider.BelowCopy(0.0, 32.0), "separateCtrl").AddHoverText(Lang.Get("setting-hover-noseparatectrlkeys"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddStaticText(Lang.Get("keycontrols"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 5.0, -120.0))
			.AddTextInput(leftText = leftText.BelowCopy(0.0, 5.0), delegate(string text)
			{
				if (!(currentSearchText == text))
				{
					currentSearchText = text;
					ReLoadKeyCombinations();
				}
			}, null, "searchField")
			.AddVerticalScrollbar(OnNewScrollbarValue, scrollbarBounds.FixedUnder(leftText, 10.0), "scrollbar")
			.AddInset(insetBounds.FixedUnder(leftText, 10.0), 3, 0.8f)
			.BeginClip(clipBounds)
			.AddConfigList(keycontrolItems, OnKeyControlItemClick, CairoFont.WhiteSmallText().WithFontSize(18f), configListBounds, "configlist")
			.EndClip()
			.AddButton(Lang.Get("setting-name-setdefault"), OnResetControls, ElementStdBounds.MenuButton(0f, EnumDialogArea.LeftFixed).FixedUnder(insetBounds, 10.0).WithFixedPadding(10.0, 2.0))
			.AddIf(handler.IsIngame)
			.AddButton(Lang.Get("setting-name-macroeditor"), OnMacroEditor, ElementStdBounds.MenuButton(0f, EnumDialogArea.RightFixed).FixedUnder(insetBounds, 10.0).WithFixedPadding(10.0, 2.0))
			.EndIf()
			.EndChildElements()
			.Compose();
		handler.LoadComposer(composer);
		composer.GetSwitch("separateCtrl").SetValue(!ClientSettings.SeparateCtrl);
		composer.GetTextInput("searchField").SetPlaceHolderText(Lang.Get("Search..."));
		composer.GetTextInput("searchField").SetValue("");
		GuiElementConfigList configlist = composer.GetConfigList("configlist");
		configlist.errorFont = configlist.stdFont.Clone();
		configlist.errorFont.Color = GuiStyle.ErrorTextColor;
		configlist.Bounds.CalcWorldBounds();
		clipBounds.CalcWorldBounds();
		ReLoadKeyCombinations();
		composer.GetScrollbar("scrollbar").SetHeights((float)clipBounds.fixedHeight, (float)configlist.innerBounds.fixedHeight);
	}

	private bool OnMacroEditor()
	{
		handler.OnMacroEditor();
		return true;
	}

	private void onCollectionModeChange(string code, bool selected)
	{
		ClientSettings.ItemCollectMode = code.ToInt();
	}

	private void onMouseModeChanged(bool on)
	{
		ClientSettings.DirectMouseMode = on;
		ScreenManager.Platform.SetDirectMouseMode(on);
	}

	private void onInvertYAxisChanged(bool on)
	{
		ClientSettings.InvertMouseYAxis = on;
	}

	private void onSeparateCtrl(bool on)
	{
		ClientSettings.SeparateCtrl = !on;
		if (on)
		{
			HotKey keyComb = ScreenManager.hotkeyManager.HotKeys["shift"];
			keyComb.CurrentMapping = ScreenManager.hotkeyManager.HotKeys["sneak"].CurrentMapping;
			ClientSettings.Inst.SetKeyMapping("shift", keyComb.CurrentMapping);
			keyComb = ScreenManager.hotkeyManager.HotKeys["ctrl"];
			keyComb.CurrentMapping = ScreenManager.hotkeyManager.HotKeys["sprint"].CurrentMapping;
			ClientSettings.Inst.SetKeyMapping("ctrl", keyComb.CurrentMapping);
		}
		else
		{
			HotKey keyComb2 = ScreenManager.hotkeyManager.HotKeys["shift"];
			keyComb2.CurrentMapping = new KeyCombination
			{
				KeyCode = 1
			};
			ClientSettings.Inst.SetKeyMapping("shift", keyComb2.CurrentMapping);
			keyComb2 = ScreenManager.hotkeyManager.HotKeys["ctrl"];
			keyComb2.CurrentMapping = new KeyCombination
			{
				KeyCode = 3
			};
			ClientSettings.Inst.SetKeyMapping("ctrl", keyComb2.CurrentMapping);
		}
		OnControlOptions(on: true);
	}

	private bool onMouseWheelSensivityChanged(int val)
	{
		ClientSettings.MouseWheelSensivity = (float)val / 10f;
		return true;
	}

	private void ReLoadKeyCombinations()
	{
		if (mousecontrolsTabActive)
		{
			LoadMouseCombinations();
		}
		else
		{
			LoadKeyCombinations();
		}
		GuiElementConfigList configlist = composer.GetConfigList("configlist");
		if (configlist != null)
		{
			configlist.Refresh();
			composer.GetScrollbar("scrollbar")?.SetNewTotalHeight((float)configlist.innerBounds.OuterHeight);
			composer.GetScrollbar("scrollbar")?.TriggerChanged();
		}
	}

	private void LoadKeyCombinations()
	{
		int hotkeyIndex = -1;
		if (keycontrolItems.Count >= clickedItemIndex)
		{
			hotkeyIndex = (int)keycontrolItems[clickedItemIndex.Value].Data;
		}
		keycontrolItems.Clear();
		int i = 0;
		List<ConfigItem>[] sortedItems = new List<ConfigItem>[sortOrder.Count];
		for (int k = 0; k < sortedItems.Length; k++)
		{
			sortedItems[k] = new List<ConfigItem>();
		}
		foreach (KeyValuePair<string, HotKey> val in ScreenManager.hotkeyManager.HotKeys)
		{
			HotKey kc = val.Value;
			if (clickedItemIndex.HasValue && i == hotkeyIndex)
			{
				kc = keyCombClone;
			}
			string text = "?";
			if (kc.CurrentMapping != null)
			{
				text = kc.CurrentMapping.ToString();
			}
			ConfigItem item2 = new ConfigItem
			{
				Code = val.Key,
				Key = kc.Name,
				Value = text,
				Data = i
			};
			int index = keycontrolItems.FindIndex((ConfigItem configitem) => configitem.Value == text);
			if (index != -1)
			{
				item2.error = true;
				keycontrolItems[index].error = true;
			}
			sortedItems[sortOrder[kc.KeyCombinationType]].Add(item2);
			i++;
		}
		for (int j = 0; j < sortedItems.Length; j++)
		{
			List<ConfigItem> filteredSortedItems = new List<ConfigItem>();
			string searchText = currentSearchText?.ToSearchFriendly().ToLowerInvariant();
			bool canSearch = !string.IsNullOrEmpty(searchText);
			if ((j == 1 && !ClientSettings.SeparateCtrl) || j == 9)
			{
				continue;
			}
			if (canSearch)
			{
				foreach (ConfigItem item in sortedItems[j])
				{
					if (item.Key.ToSearchFriendly().ToLowerInvariant().Contains(searchText))
					{
						filteredSortedItems.Add(item);
					}
				}
				if (filteredSortedItems != null && !filteredSortedItems.Any())
				{
					continue;
				}
			}
			if (j != 7)
			{
				keycontrolItems.Add(new ConfigItem
				{
					Type = EnumItemType.Title,
					Key = titles[j]
				});
			}
			keycontrolItems.AddRange(canSearch ? filteredSortedItems : sortedItems[j]);
		}
	}

	private void OnKeyControlItemClick(int index, int indexNoTitle)
	{
		if (!clickedItemIndex.HasValue)
		{
			keycontrolItems[index].Value = "?";
			clickedItemIndex = index;
			int hotkeyIndex = (int)keycontrolItems[clickedItemIndex.Value].Data;
			composer.GetConfigList("configlist").Refresh();
			composer.GetScrollbar("scrollbar")?.TriggerChanged();
			string code = ScreenManager.hotkeyManager.HotKeys.GetKeyAtIndex(hotkeyIndex);
			HotKey keyComb = ScreenManager.hotkeyManager.HotKeys[code];
			keyCombClone = keyComb.Clone();
			hotkeyCapturer.BeginCapture();
			keyCombClone.CurrentMapping = hotkeyCapturer.CapturingKeyComb;
		}
	}

	public bool ShouldCaptureAllInputs()
	{
		return hotkeyCapturer.IsCapturing();
	}

	public void OnKeyDown(KeyEvent eventArgs)
	{
		if (hotkeyCapturer.OnKeyDown(eventArgs))
		{
			if (!hotkeyCapturer.IsCapturing())
			{
				clickedItemIndex = null;
				keyCombClone = null;
			}
			ReLoadKeyCombinations();
		}
	}

	public void OnKeyUp(KeyEvent eventArgs)
	{
		hotkeyCapturer.OnKeyUp(eventArgs, CompletedCapture);
	}

	public void OnMouseDown(MouseEvent eventArgs)
	{
		if (hotkeyCapturer.OnMouseDown(eventArgs))
		{
			if (!hotkeyCapturer.IsCapturing())
			{
				clickedItemIndex = null;
				keyCombClone = null;
			}
			ReLoadKeyCombinations();
		}
	}

	public void OnMouseUp(MouseEvent eventArgs)
	{
		hotkeyCapturer.OnMouseUp(eventArgs, CompletedCapture);
	}

	private void CompletedCapture()
	{
		int hotkeyIndex = (mousecontrolsTabActive ? ((int)mousecontrolItems[clickedItemIndex.Value].Data) : ((int)keycontrolItems[clickedItemIndex.Value].Data));
		string code = ScreenManager.hotkeyManager.HotKeys.GetKeyAtIndex(hotkeyIndex);
		if (!hotkeyCapturer.WasCancelled)
		{
			keyCombClone.CurrentMapping = hotkeyCapturer.CapturedKeyComb;
			ScreenManager.hotkeyManager.HotKeys[code] = keyCombClone;
			ClientSettings.Inst.SetKeyMapping(code, keyCombClone.CurrentMapping);
			if (code == "sneak" && !ClientSettings.SeparateCtrl)
			{
				ScreenManager.hotkeyManager.HotKeys["shift"].CurrentMapping = keyCombClone.CurrentMapping;
				ShiftOrCtrlChanged();
			}
			if (code == "sprint" && !ClientSettings.SeparateCtrl)
			{
				ScreenManager.hotkeyManager.HotKeys["ctrl"].CurrentMapping = keyCombClone.CurrentMapping;
				ShiftOrCtrlChanged();
			}
			switch (code)
			{
			case "shift":
			case "ctrl":
			case "primarymouse":
			case "secondarymouse":
			case "toolmodeselect":
				ShiftOrCtrlChanged();
				break;
			}
		}
		clickedItemIndex = null;
		keyCombClone = null;
		ReLoadKeyCombinations();
	}

	private void ShiftOrCtrlChanged()
	{
		(handler.Api as ClientCoreAPI)?.eventapi.TriggerHotkeysChanged();
	}

	private void OnNewScrollbarValue(float value)
	{
		ElementBounds innerBounds = composer.GetConfigList("configlist").innerBounds;
		innerBounds.fixedY = 5f - value;
		innerBounds.CalcWorldBounds();
	}

	private bool onMouseSmoothingChanged(int value)
	{
		ClientSettings.MouseSmoothing = 100 - value;
		return true;
	}

	private bool onMouseSensivityChanged(int value)
	{
		ClientSettings.MouseSensivity = value;
		return true;
	}

	private bool OnResetControls()
	{
		composer = ComposerHeader("gamesettings-confirmreset", "controls").AddStaticText(Lang.Get("Please Confirm"), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(1.5f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(600.0)).AddStaticText(Lang.Get("Really reset key controls to default settings?"), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(2f, 0.0, EnumDialogArea.LeftFixed).WithFixedSize(600.0, 100.0)).AddButton(Lang.Get("Cancel"), OnCancelReset, ElementStdBounds.Rowed(3.7f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0))
			.AddButton(Lang.Get("Confirm"), OnConfirmReset, ElementStdBounds.Rowed(3.7f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
			.EndChildElements()
			.Compose();
		handler.LoadComposer(composer);
		return true;
	}

	private bool OnConfirmReset()
	{
		ClientSettings.KeyMapping.Clear();
		ScreenManager.hotkeyManager.ResetKeyMapping();
		OnControlOptions(on: true);
		return true;
	}

	private bool OnCancelReset()
	{
		OnControlOptions(on: true);
		return true;
	}

	private void OnAccessibilityOptions(bool on)
	{
		ElementBounds leftText = ElementBounds.Fixed(0.0, 85.0, 450.0, 42.0);
		ElementBounds rightSlider = ElementBounds.Fixed(470.0, 138.0, 200.0, 20.0);
		GuiComposer guiComposer = ComposerHeader("gamesettings-accessibility", "accessibility").AddStaticText(Lang.Get("setting-accessibility-notes"), CairoFont.WhiteSmallText(), leftText.FlatCopy().WithFixedWidth(800.0)).AddStaticText(Lang.Get("setting-name-togglesprint"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 12.0).WithFixedWidth(360.0)).AddHoverText(Lang.Get("setting-hover-togglesprint"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onToggleSprint, rightSlider.FlatCopy(), "toggleSprint")
			.AddStaticText(Lang.Get("setting-name-bobblehead"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0))
			.AddHoverText(Lang.Get("setting-hover-bobblehead"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onViewBobbingChanged, rightSlider = rightSlider.BelowCopy(0.0, 20.0), "viewBobbingSwitch")
			.AddStaticText(Lang.Get("setting-name-camerashake"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0))
			.AddSlider(onCameraShakeChanged, rightSlider = rightSlider.BelowCopy(0.0, 18.0).WithFixedSize(200.0, 25.0), "cameraShakeSlider")
			.AddHoverText(Lang.Get("setting-hover-camerashake"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddStaticText(Lang.Get("setting-name-wireframethickness"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0));
		ActionConsumable<int> onNewSliderValue = onWireframeThicknessChanged;
		ElementBounds elementBounds = rightSlider.BelowCopy(0.0, 19.0).WithFixedSize(200.0, 25.0);
		composer = GuiComposerHelpers.AddDropDown(bounds: rightSlider = elementBounds.BelowCopy(0.0, 19.0).WithFixedSize(100.0, 25.0), composer: guiComposer.AddSlider(onNewSliderValue, elementBounds, "wireframethicknessSlider").AddHoverText(Lang.Get("setting-hover-wireframethickness"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0)).AddStaticText(Lang.Get("setting-name-wireframecolors"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0)), values: new string[3] { "Preset1", "Preset2", "Preset3" }, names: new string[3]
		{
			Lang.Get("Preset 1"),
			Lang.Get("Preset 2"),
			Lang.Get("Preset 3")
		}, selectedIndex: ClientSettings.guiColorsPreset - 1, onSelectionChanged: onWireframeColorsChanged, key: "wireframecolorsDropdown").AddHoverText(Lang.Get("setting-hover-wireframecolors"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0)).AddStaticText(Lang.Get("setting-name-instabilityWavingStrength"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0))
			.AddSlider(onInstabilityStrengthChanged, rightSlider = rightSlider.BelowCopy(0.0, 19.0).WithFixedSize(200.0, 25.0), "instabilityWavingStrengthSlider")
			.AddHoverText(Lang.Get("setting-hover-instabilityWavingStrength"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddRichtext(Lang.Get("help-accessibility"), CairoFont.WhiteDetailText(), leftText = leftText.BelowCopy(0.0, 23.0))
			.EndChildElements()
			.Compose();
		composer.GetSwitch("viewBobbingSwitch").On = ClientSettings.ViewBobbing;
		composer.GetSwitch("toggleSprint").SetValue(ClientSettings.ToggleSprint);
		composer.GetSlider("cameraShakeSlider").SetValues((int)(ClientSettings.CameraShakeStrength * 100f), 0, 100, 1, " %");
		composer.GetSlider("wireframethicknessSlider").SetValues((int)(ClientSettings.Wireframethickness * 2f), 1, 16, 1, "x");
		composer.GetSlider("wireframethicknessSlider").OnSliderTooltip = (int value) => (float)value / 2f + "x";
		composer.GetSlider("wireframethicknessSlider").ComposeHoverTextElement();
		composer.GetSlider("instabilityWavingStrengthSlider").SetValues((int)(ClientSettings.InstabilityWavingStrength * 100f), 0, 150, 1, " %");
		handler.LoadComposer(composer);
	}

	private bool onInstabilityStrengthChanged(int value)
	{
		ClientSettings.InstabilityWavingStrength = (float)value / 100f;
		return true;
	}

	private bool onWireframeThicknessChanged(int value)
	{
		ClientSettings.Wireframethickness = (float)value / 2f;
		return true;
	}

	private void onWireframeColorsChanged(string code, bool selected)
	{
		ClientSettings.guiColorsPreset = code[code.Length - 1] - 48;
		handler.Api.ColorPreset?.OnUpdateSetting();
	}

	private bool onCameraShakeChanged(int value)
	{
		ClientSettings.CameraShakeStrength = (float)value / 100f;
		return true;
	}

	private void onViewBobbingChanged(bool val)
	{
		ClientSettings.ViewBobbing = val;
	}

	private void onToggleSprint(bool on)
	{
		ClientSettings.ToggleSprint = on;
	}

	internal void OnSoundOptions(bool on)
	{
		ElementBounds leftText = ElementBounds.Fixed(0.0, 87.0, 320.0, 40.0);
		ElementBounds rightSlider = ElementBounds.Fixed(340.0, 89.0, 330.0, 20.0);
		string[] devices = new string[1].Append(ScreenManager.Platform.AvailableAudioDevices.ToArray());
		string[] devicesnames = new string[1] { "Default" }.Append(ScreenManager.Platform.AvailableAudioDevices.ToArray());
		composer = ComposerHeader("gamesettings-soundoptions", "sounds").AddStaticText(Lang.Get("setting-name-mastersoundlevel"), CairoFont.WhiteSmallishText(), leftText = leftText.FlatCopy()).AddSlider(onMasterSoundLevelChanged, rightSlider = rightSlider.FlatCopy(), "mastersoundLevel").AddStaticText(Lang.Get("setting-name-soundlevel"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 25.0))
			.AddSlider(onSoundLevelChanged, rightSlider = rightSlider.BelowCopy(0.0, 46.0), "soundLevel")
			.AddStaticText(Lang.Get("setting-name-entitysoundlevel"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy())
			.AddSlider(onEntitySoundLevelChanged, rightSlider = rightSlider.BelowCopy(0.0, 21.0), "entitySoundLevel")
			.AddStaticText(Lang.Get("setting-name-ambientsoundlevel"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy())
			.AddSlider(onAmbientSoundLevelChanged, rightSlider = rightSlider.BelowCopy(0.0, 21.0), "ambientSoundLevel")
			.AddStaticText(Lang.Get("setting-name-weathersoundlevel"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy())
			.AddSlider(onWeatherSoundLevelChanged, rightSlider = rightSlider.BelowCopy(0.0, 21.0), "weatherSoundLevel")
			.AddStaticText(Lang.Get("setting-name-musiclevel"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 22.0))
			.AddSlider(onMusicLevelChanged, rightSlider = rightSlider.BelowCopy(0.0, 41.0), "musicLevel")
			.AddStaticText(Lang.Get("setting-name-musicfrequency"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy())
			.AddSlider(onMusicFrequencyChanged, rightSlider = rightSlider.BelowCopy(0.0, 21.0), "musicFrequency")
			.AddStaticText(Lang.Get("setting-name-hrtfmode"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 26.0))
			.AddHoverText(Lang.Get("setting-hover-hrtfmode"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(30.0))
			.AddSwitch(onHRTFMode, rightSlider = rightSlider.BelowCopy(0.0, 34.0), "hrtfmode")
			.AddStaticText(Lang.Get("setting-name-audiooutputdevice"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 5.0))
			.AddDropDown(devices, devicesnames, 0, onAudioDeviceChanged, rightSlider = rightSlider.BelowCopy(0.0, 16.0).WithFixedSize(300.0, 30.0), "audiooutputdevice")
			.EndChildElements()
			.Compose();
		handler.LoadComposer(composer);
		composer.GetSlider("mastersoundLevel").SetValues(ClientSettings.MasterSoundLevel, 0, 100, 1, "%");
		composer.GetSlider("soundLevel").SetValues(ClientSettings.SoundLevel, 0, 100, 1, "%");
		composer.GetSlider("entitySoundLevel").SetValues(ClientSettings.EntitySoundLevel, 0, 100, 1, "%");
		composer.GetSlider("ambientSoundLevel").SetValues(ClientSettings.AmbientSoundLevel, 0, 100, 1, "%");
		composer.GetSlider("weatherSoundLevel").SetValues(ClientSettings.WeatherSoundLevel, 0, 100, 1, "%");
		composer.GetSlider("musicLevel").SetValues(ClientSettings.MusicLevel, 0, 100, 1, "%");
		string[] frequencies = new string[4]
		{
			Lang.Get("setting-musicfrequency-low"),
			Lang.Get("setting-musicfrequency-medium"),
			Lang.Get("setting-musicfrequency-often"),
			Lang.Get("setting-musicfrequency-veryoften")
		};
		composer.GetSlider("musicFrequency").OnSliderTooltip = (int value) => frequencies[value] ?? "";
		composer.GetSlider("musicFrequency").SetValues(ClientSettings.MusicFrequency, 0, 3, 1);
		composer.GetSwitch("hrtfmode").SetValue(ClientSettings.UseHRTFAudio);
		composer.GetDropDown("audiooutputdevice").SetSelectedIndex(Math.Max(0, devices.IndexOf(ClientSettings.AudioDevice)));
	}

	private void onAudioDeviceChanged(string code, bool selected)
	{
		ClientSettings.AudioDevice = code;
	}

	private bool onMusicFrequencyChanged(int val)
	{
		ClientSettings.MusicFrequency = val;
		return true;
	}

	private bool onMasterSoundLevelChanged(int soundLevel)
	{
		ClientSettings.MasterSoundLevel = soundLevel;
		return true;
	}

	private bool onSoundLevelChanged(int soundLevel)
	{
		ClientSettings.SoundLevel = soundLevel;
		return true;
	}

	private bool onEntitySoundLevelChanged(int soundLevel)
	{
		ClientSettings.EntitySoundLevel = soundLevel;
		return true;
	}

	private bool onAmbientSoundLevelChanged(int soundLevel)
	{
		ClientSettings.AmbientSoundLevel = soundLevel;
		return true;
	}

	private bool onWeatherSoundLevelChanged(int soundLevel)
	{
		ClientSettings.WeatherSoundLevel = soundLevel;
		return true;
	}

	private bool onMusicLevelChanged(int musicLevel)
	{
		ClientSettings.MusicLevel = musicLevel;
		return true;
	}

	private void onHRTFMode(bool val)
	{
		ClientSettings.UseHRTFAudio = val;
	}

	public static void getLanguages(out string[] languageCodes, out string[] languageNames)
	{
		LanguageConfig[] configs = ScreenManager.Platform.AssetManager.Get<LanguageConfig[]>(new AssetLocation("lang/languages.json"));
		languageCodes = new string[configs.Length];
		languageNames = new string[configs.Length];
		for (int i = 0; i < configs.Length; i++)
		{
			languageCodes[i] = configs[i].Code;
			languageNames[i] = configs[i].Name + " / " + configs[i].Englishname;
		}
	}

	internal void OnInterfaceOptions(bool on)
	{
		ElementBounds leftText = ElementBounds.Fixed(0.0, 85.0, 475.0, 42.0);
		ElementBounds rightSlider = ElementBounds.Fixed(495.0, 89.0, 200.0, 20.0);
		_ = ScreenManager.Platform.WindowBorder;
		string currentLanguage = ClientSettings.Language;
		getLanguages(out var languageCodes, out var languageNames);
		int langIndex = languageCodes.IndexOf(currentLanguage);
		composer = ComposerHeader("gamesettings-interfaceoptions", "interface").AddStaticText(Lang.Get("setting-name-guiscale"), CairoFont.WhiteSmallishText(), leftText).AddHoverText(Lang.Get("setting-hover-guiscale"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0)).AddSlider(onGuiScaleChanged, rightSlider, "guiScaleSlider")
			.AddStaticText(Lang.Get("setting-name-language"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0))
			.AddHoverText(Lang.Get("setting-hover-language"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddDropDown(languageCodes, languageNames, langIndex, onLanguageChanged, rightSlider = rightSlider.BelowCopy(0.0, 17.0).WithFixedSize(330.0, 30.0))
			.AddStaticText(Lang.Get("setting-name-autochat"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 1.0))
			.AddHoverText(Lang.Get("setting-hover-autochat"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onAutoChatChanged, rightSlider = rightSlider.BelowCopy(0.0, 15.0), "autoChatSwitch")
			.AddStaticText(Lang.Get("setting-name-autochat-selected"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 1.0))
			.AddHoverText(Lang.Get("setting-hover-autochat-selected"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onAutoChatOpenSelectedChanged, rightSlider = rightSlider.BelowCopy(0.0, 15.0), "autoChatOpenSelectedSwitch")
			.AddStaticText(Lang.Get("setting-name-blockinfohud") + HotkeyReminder("blockinfohud"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0))
			.AddHoverText(Lang.Get("setting-hover-blockinfohud"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onBlockInfoHudChanged, rightSlider = rightSlider.BelowCopy(0.0, 14.0), "blockinfohudSwitch")
			.AddStaticText(Lang.Get("setting-name-blockinteractioninfohud") + HotkeyReminder("blockinteractionhelp"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0))
			.AddHoverText(Lang.Get("setting-hover-blockinteractioninfohud"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onBlockInteractionInfoHudChanged, rightSlider = rightSlider.BelowCopy(0.0, 14.0), "blockinteractioninfohudSwitch")
			.AddStaticText(Lang.Get("setting-name-coordinatehud") + HotkeyReminder("coordinateshud"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0))
			.AddHoverText(Lang.Get("setting-hover-coordinatehud"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onCoordinateHudChanged, rightSlider = rightSlider.BelowCopy(0.0, 14.0), "coordinatehudSwitch");
		if (composer.Api is MainMenuAPI || composer.Api.World.Config.GetBool("allowMap", defaultValue: true))
		{
			composer = composer.AddStaticText(Lang.Get("setting-name-minimaphud") + HotkeyReminder("worldmaphud"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 2.0)).AddHoverText(Lang.Get("setting-hover-minimaphud"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0)).AddSwitch(onMinimapHudChanged, rightSlider = rightSlider.BelowCopy(0.0, 14.0), "minimaphudSwitch");
		}
		GuiComposer guiComposer = composer.AddStaticText(Lang.Get("setting-name-immersivemousemode"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0)).AddHoverText(Lang.Get("setting-hover-immersivemousemode"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0)).AddSwitch(onImmersiveMouseModeChanged, rightSlider = rightSlider.BelowCopy(0.0, 17.0), "immersiveMouseModeSwitch")
			.AddStaticText(Lang.Get("setting-name-immersivefpmode"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 5.0))
			.AddHoverText(Lang.Get("setting-hover-immersivefpmode"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onImmersiveFpModeChanged, rightSlider = rightSlider.BelowCopy(0.0, 17.0), "immersiveFpModeSwitch")
			.AddStaticText(Lang.Get("setting-name-fpmodeyoffset"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 5.0))
			.AddHoverText(Lang.Get("setting-hover-fpmodeyoffset"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSlider(onFpModeYOffsetChanged, rightSlider = rightSlider.BelowCopy(0.0, 19.0).WithFixedSize(150.0, 20.0), "fpmodeYOffsetSlider")
			.AddStaticText(Lang.Get("setting-name-fpmodefov"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 5.0))
			.AddHoverText(Lang.Get("setting-hover-fpmodefov"), CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSlider(onFpModeFoVChanged, rightSlider = rightSlider.BelowCopy(0.0, 28.0).WithFixedSize(150.0, 20.0), "fpmodefovSlider");
		string text = Lang.Get("setting-name-developermode");
		CairoFont font = CairoFont.WhiteSmallishText();
		ElementBounds elementBounds = leftText.BelowCopy(0.0, 4.0);
		composer = GuiComposerHelpers.AddRichtext(bounds: elementBounds.BelowCopy().WithFixedMargin(0.0, 25.0).WithFixedSize(880.0, 110.0), composer: guiComposer.AddStaticText(text, font, elementBounds).AddHoverText(Lang.Get("setting-hover-developermode"), CairoFont.WhiteSmallText(), 250, rightSlider = rightSlider.BelowCopy(0.0, 20.0).WithFixedHeight(25.0)).AddSwitch(onDeveloperModeChanged, rightSlider, "developerSwitch"), vtmlCode: (startupLanguage != "en") ? Lang.Get("setting-notice-lang-communitycreated") : "", baseFont: CairoFont.WhiteSmallishText(), key: "restartText").EndChildElements().Compose();
		handler.LoadComposer(composer);
		if (ScreenManager.Platform.ScreenSize.Width > 3000)
		{
			composer.GetSlider("guiScaleSlider").SetValues((int)(8f * ClientSettings.GUIScale), 4, 24, 1);
		}
		else
		{
			composer.GetSlider("guiScaleSlider").SetValues((int)(8f * ClientSettings.GUIScale), 4, 16, 1);
		}
		composer.GetSlider("guiScaleSlider").TriggerOnlyOnMouseUp();
		composer.GetSlider("fpmodeYOffsetSlider").SetValues((int)(ClientSettings.FpHandsYOffset * 100f), -100, 10, 1);
		composer.GetSlider("fpmodefovSlider").SetValues(ClientSettings.FpHandsFoV, 70, 90, 1, "°");
		composer.GetSwitch("immersiveMouseModeSwitch").SetValue(ClientSettings.ImmersiveMouseMode);
		composer.GetSwitch("immersiveFpModeSwitch").SetValue(ClientSettings.ImmersiveFpMode);
		composer.GetSwitch("autoChatSwitch").SetValue(ClientSettings.AutoChat);
		composer.GetSwitch("autoChatOpenSelectedSwitch").SetValue(ClientSettings.AutoChatOpenSelected);
		composer.GetSwitch("blockinfohudSwitch").SetValue(ClientSettings.ShowBlockInfoHud);
		composer.GetSwitch("blockinteractioninfohudSwitch").SetValue(ClientSettings.ShowBlockInteractionHelp);
		composer.GetSwitch("coordinatehudSwitch").SetValue(ClientSettings.ShowCoordinateHud);
		composer.GetSwitch("minimaphudSwitch")?.SetValue(composer.Api.Settings.Bool["showMinimapHud"]);
		composer.GetSwitch("developerSwitch").SetValue(ClientSettings.DeveloperMode);
	}

	private bool onFpModeYOffsetChanged(int pos)
	{
		ClientSettings.FpHandsYOffset = (float)pos / 100f;
		return true;
	}

	private bool onFpModeFoVChanged(int pos)
	{
		ClientSettings.FpHandsFoV = pos;
		return true;
	}

	private string HotkeyReminder(string key)
	{
		if (!ScreenManager.hotkeyManager.HotKeys.TryGetValue(key, out var hotkey))
		{
			return "";
		}
		if (hotkey.CurrentMapping == null)
		{
			return "";
		}
		return " (" + hotkey.CurrentMapping?.ToString() + ")";
	}

	private void onMinimapHudChanged(bool on)
	{
		composer.Api.Settings.Bool["showMinimapHud"] = on;
	}

	private void onCoordinateHudChanged(bool on)
	{
		ClientSettings.ShowCoordinateHud = on;
	}

	private void onBlockInteractionInfoHudChanged(bool on)
	{
		ClientSettings.ShowBlockInteractionHelp = on;
	}

	private void onBlockInfoHudChanged(bool on)
	{
		ClientSettings.ShowBlockInfoHud = on;
	}

	private void onImmersiveMouseModeChanged(bool on)
	{
		ClientSettings.ImmersiveMouseMode = on;
	}

	private void onImmersiveFpModeChanged(bool on)
	{
		ClientSettings.ImmersiveFpMode = on;
	}

	private void onAutoChatChanged(bool on)
	{
		ClientSettings.AutoChat = on;
	}

	private void onAutoChatOpenSelectedChanged(bool on)
	{
		ClientSettings.AutoChatOpenSelected = on;
	}

	private void onLanguageChanged(string lang, bool on)
	{
		bool save = false;
		if (lang != ClientSettings.Language)
		{
			if (lang != "en")
			{
				composer.GetRichtext("restartText").SetNewText(Lang.GetL(lang, "setting-notice-restart") + " " + Lang.GetL(lang, "setting-notice-lang-communitycreated"), CairoFont.WhiteSmallishText());
			}
			else
			{
				composer.GetRichtext("restartText").SetNewText(Lang.GetL(lang, "setting-notice-restart"), CairoFont.WhiteSmallishText());
			}
			save = true;
		}
		if (lang == startupLanguage)
		{
			composer.GetRichtext("restartText").SetNewText((lang != "en") ? Lang.Get("setting-notice-lang-communitycreated") : "", CairoFont.WhiteSmallishText());
		}
		ClientSettings.Language = lang;
		if (!lang.StartsWithOrdinal("zh-"))
		{
			switch (lang)
			{
			case "ar":
			case "ja":
			case "ko":
			case "th":
				break;
			default:
				goto IL_02ac;
			}
		}
		if (RuntimeEnv.OS != 0)
		{
			if (lang != startupLanguage && ClientSettings.DefaultFontName == "sans-serif")
			{
				ClientSettings.DecorativeFontName = "sans-serif";
				composer.GetRichtext("restartText").SetNewText(Lang.GetL(startupLanguage, "setting-notice-restart") + " " + Lang.GetL(startupLanguage, "setting-notice-lang-communitycreated") + "\n" + Lang.GetL(startupLanguage, "setting-notice-lang-nonwindowsfonts"), CairoFont.WhiteSmallishText());
			}
		}
		else
		{
			switch (lang)
			{
			case "ko":
				SetupLocalizedFonts(lang, "Malgun Gothic", "Malgun Gothic");
				save = true;
				break;
			case "th":
				SetupLocalizedFonts(lang, "Leelawadee UI Semilight", "Leelawadee UI");
				save = true;
				break;
			case "ja":
				SetupLocalizedFonts(lang, "meiryo", "meiryo");
				save = true;
				break;
			case "zh-cn":
				SetupLocalizedFonts(lang, "Microsoft YaHei Light", "Microsoft YaHei");
				save = true;
				break;
			case "zh-tw":
				SetupLocalizedFonts(lang, "Microsoft JhengHei UI Light", "Microsoft JhengHei UI");
				save = true;
				break;
			default:
				ClientSettings.DecorativeFontName = "sans-serif";
				save = true;
				break;
			}
		}
		goto IL_032a;
		IL_032a:
		if (save)
		{
			ClientSettings.Inst.Save(force: true);
		}
		return;
		IL_02ac:
		if (ClientSettings.DefaultFontName == "meiryo" || ClientSettings.DefaultFontName == "Malgun Gothic" || ClientSettings.DefaultFontName == "Leelawadee UI Semilight" || ClientSettings.DefaultFontName == "Microsoft YaHei Light" || ClientSettings.DefaultFontName == "Microsoft JhengHei UI Light")
		{
			ClientSettings.DefaultFontName = "sans-serif";
			save = true;
		}
		if (ClientSettings.DefaultFontName == "sans-serif")
		{
			ClientSettings.DecorativeFontName = "Lora";
			save = true;
		}
		goto IL_032a;
	}

	private void SetupLocalizedFonts(string lang, string baseFont, string decorativeFont)
	{
		ClientSettings.DefaultFontName = baseFont;
		ClientSettings.DecorativeFontName = decorativeFont;
		string restartText = ((lang != startupLanguage) ? (Lang.GetL(lang, "setting-notice-restart") + " " + Lang.GetL(lang, "setting-notice-lang-communitycreated")) : Lang.GetL(lang, "setting-notice-lang-communitycreated"));
		if (lang != startupLanguage)
		{
			composer.GetRichtext("restartText").SetNewText(restartText, CairoFont.WhiteSmallishText(baseFont));
		}
		else
		{
			composer.GetRichtext("restartText").SetNewText(restartText, CairoFont.WhiteSmallishText(baseFont));
		}
	}

	private void OnDeveloperOptions(bool on)
	{
		ElementBounds leftText = ElementBounds.Fixed(0.0, 42.0, 425.0, 42.0);
		ElementBounds rightSlider = ElementBounds.Fixed(450.0, 45.0, 200.0, 20.0);
		string[] hoverTexts = new string[8]
		{
			Lang.Get("setting-hover-errorreporter"),
			Lang.Get("setting-hover-extdebuginfo"),
			Lang.Get("setting-hover-opengldebug"),
			Lang.Get("setting-hover-openglerrorchecking"),
			Lang.Get("setting-hover-debugtexturedispose"),
			Lang.Get("setting-hover-debugvaodispose"),
			Lang.Get("setting-hover-debugsounddispose"),
			Lang.Get("setting-hover-fasterstartup")
		};
		composer = ComposerHeader("gamesettings-developeroptions", "developer").AddStaticText(Lang.Get("setting-name-errorreporter"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy()).AddHoverText(hoverTexts[0], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0)).AddSwitch(onErrorReporterChanged, rightSlider = rightSlider.BelowCopy(0.0, 16.0), "errorReporterSwitch")
			.AddStaticText(Lang.Get("setting-name-extdebuginfo"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0))
			.AddHoverText(hoverTexts[1], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onExtDebugInfoChanged, rightSlider = rightSlider.BelowCopy(0.0, 16.0), "extDbgInfoSwitch")
			.AddStaticText(Lang.Get("setting-name-opengldebug"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0))
			.AddHoverText(hoverTexts[2], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onOpenGLDebugChanged, rightSlider = rightSlider.BelowCopy(0.0, 16.0), "openglDebugSwitch")
			.AddStaticText(Lang.Get("setting-name-openglerrorchecking"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0))
			.AddHoverText(hoverTexts[3], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onOpenGLErrorCheckingChanged, rightSlider = rightSlider.BelowCopy(0.0, 16.0), "openglErrorCheckingSwitch")
			.AddStaticText(Lang.Get("setting-name-debugtexturedispose"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0))
			.AddHoverText(hoverTexts[4], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onDebugTextureDisposeChanged, rightSlider = rightSlider.BelowCopy(0.0, 16.0), "debugTextureDisposeSwitch")
			.AddStaticText(Lang.Get("setting-name-debugvaodispose"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0))
			.AddHoverText(hoverTexts[5], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onDebugVaoDisposeChanged, rightSlider = rightSlider.BelowCopy(0.0, 16.0), "debugVaoDisposeSwitch")
			.AddStaticText(Lang.Get("setting-name-debugsounddispose"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0))
			.AddHoverText(hoverTexts[6], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onDebugSoundDisposeChanged, rightSlider = rightSlider.BelowCopy(0.0, 16.0), "debugSoundDisposeSwitch")
			.AddStaticText(Lang.Get("setting-name-fasterstartup"), CairoFont.WhiteSmallishText(), leftText = leftText.BelowCopy(0.0, 4.0))
			.AddHoverText(hoverTexts[7], CairoFont.WhiteSmallText(), 250, leftText.FlatCopy().WithFixedHeight(25.0))
			.AddSwitch(onFasterStartupChanged, rightSlider = rightSlider.BelowCopy(0.0, 16.0), "fasterStartupSwitch")
			.EndChildElements()
			.Compose();
		handler.LoadComposer(composer);
		composer.GetSwitch("errorReporterSwitch").SetValue(ClientSettings.StartupErrorDialog);
		composer.GetSwitch("extDbgInfoSwitch").SetValue(ClientSettings.ExtendedDebugInfo);
		composer.GetSwitch("openglDebugSwitch").SetValue(ClientSettings.GlDebugMode);
		composer.GetSwitch("openglErrorCheckingSwitch").SetValue(ClientSettings.GlErrorChecking);
		composer.GetSwitch("debugTextureDisposeSwitch").SetValue(RuntimeEnv.DebugTextureDispose);
		composer.GetSwitch("debugVaoDisposeSwitch").SetValue(RuntimeEnv.DebugVAODispose);
		composer.GetSwitch("debugSoundDisposeSwitch").SetValue(RuntimeEnv.DebugSoundDispose);
		composer.GetSwitch("fasterStartupSwitch").SetValue(ClientSettings.OffThreadMipMapCreation);
	}

	private void onErrorReporterChanged(bool on)
	{
		ClientSettings.StartupErrorDialog = on;
	}

	private void onDebugSoundDisposeChanged(bool on)
	{
		RuntimeEnv.DebugSoundDispose = on;
	}

	private void onDebugVaoDisposeChanged(bool on)
	{
		RuntimeEnv.DebugVAODispose = on;
	}

	private void onDebugTextureDisposeChanged(bool on)
	{
		RuntimeEnv.DebugTextureDispose = on;
	}

	private void onOpenGLDebugChanged(bool on)
	{
		ClientSettings.GlDebugMode = on;
		ScreenManager.Platform.GlDebugMode = on;
	}

	private void onOpenGLErrorCheckingChanged(bool on)
	{
		ClientSettings.GlErrorChecking = on;
		ScreenManager.Platform.GlErrorChecking = on;
	}

	private void onExtDebugInfoChanged(bool on)
	{
		ClientSettings.ExtendedDebugInfo = on;
	}

	private void onFasterStartupChanged(bool on)
	{
		ClientSettings.OffThreadMipMapCreation = on;
	}

	private void onDeveloperModeChanged(bool on)
	{
		if (!on)
		{
			ClientSettings.DeveloperMode = on;
			ClientSettings.StartupErrorDialog = false;
			ClientSettings.ExtendedDebugInfo = false;
			ClientSettings.GlDebugMode = false;
			ClientSettings.GlErrorChecking = false;
			RuntimeEnv.DebugTextureDispose = false;
			RuntimeEnv.DebugVAODispose = false;
			RuntimeEnv.DebugSoundDispose = false;
			OnInterfaceOptions(on: true);
		}
		else
		{
			composer = ComposerHeader("gamesettings-confirmdevelopermode", "developer").AddStaticText(Lang.Get("Please Confirm"), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(1.5f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(600.0)).AddStaticText(Lang.Get("confirmEnableDevMode"), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(2f, 0.0, EnumDialogArea.LeftFixed).WithFixedSize(600.0, 100.0)).AddButton(Lang.Get("Cancel"), OnCancelDevMode, ElementStdBounds.Rowed(3.7f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0))
				.AddButton(Lang.Get("Confirm"), OnConfirmDevMode, ElementStdBounds.Rowed(3.7f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
				.EndChildElements()
				.Compose();
			handler.LoadComposer(composer);
		}
	}

	private bool OnCancelDevMode()
	{
		OnInterfaceOptions(on: true);
		return true;
	}

	private bool OnConfirmDevMode()
	{
		ClientSettings.DeveloperMode = true;
		OnDeveloperOptions(on: true);
		return true;
	}
}
