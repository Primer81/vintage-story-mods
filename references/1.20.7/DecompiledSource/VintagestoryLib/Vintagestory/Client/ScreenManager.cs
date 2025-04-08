using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cairo;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.Gui;
using Vintagestory.Client.MaxObf;
using Vintagestory.Client.NoObf;
using Vintagestory.ClientNative;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class ScreenManager : KeyEventHandler, MouseEventHandler, NewFrameHandler
{
	public static int MainThreadId;

	public static GuiComposerManager GuiComposers;

	public static ClientPlatformAbstract Platform;

	public static FrameProfilerUtil FrameProfiler;

	public static KeyModifiers KeyboardModifiers = new KeyModifiers();

	private static int keysMax = 512;

	public static bool[] KeyboardKeyState = new bool[keysMax];

	public static bool[] MouseButtonState = new bool[(int)Enum.GetValues(typeof(EnumMouseButton)).Cast<EnumMouseButton>().Max()];

	public static Dictionary<AssetLocation, AudioData> soundAudioData = new Dictionary<AssetLocation, AudioData>();

	public static Dictionary<AssetLocation, AudioData> soundAudioDataAsyncLoadTemp = new Dictionary<AssetLocation, AudioData>();

	public static Queue<Action> MainThreadTasks = new Queue<Action>();

	public static bool debugDrawCallNextFrame = false;

	public SessionManager sessionManager;

	public static HotkeyManager hotkeyManager;

	private Dictionary<GuiScreen, Type> CachedScreens = new Dictionary<GuiScreen, Type>();

	internal GuiScreen CurrentScreen;

	internal GuiScreenMainRight mainScreen;

	private long lastSaveCheck;

	public string loadingText;

	public bool ClientIsOffline = true;

	private bool cursorsLoaded;

	protected EnumAuthServerResponse? validationResponse;

	private bool awaitValidation = true;

	private int mouseX;

	private int mouseY;

	internal static ILoadedSound IntroMusic;

	internal static bool introMusicShouldStop = false;

	internal GuiComposer mainMenuComposer;

	internal GuiCompositeMainMenuLeft guiMainmenuLeft;

	public MainMenuAPI api;

	internal ModLoader modloader;

	internal List<ModContainer> allMods = new List<ModContainer>();

	internal List<ModContainer> verifiedMods = new List<ModContainer>();

	public static ClientProgramArgs ParsedArgs;

	public static string[] RawCmdLineArgs;

	internal LoadedTexture versionNumberTexture;

	internal string newestVersion;

	private bool withMainMenu = true;

	internal Dictionary<string, int> textures;

	public ClientPlatformAbstract GamePlatform => Platform;

	public List<IInventory> OpenedInventories => null;

	public IWorldAccessor World => null;

	public static bool AsyncSoundLoadComplete { get; set; }

	public IPlayerInventoryManager PlayerInventoryManager => null;

	public IPlayer Player
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public ScreenManager(ClientPlatformAbstract platform)
	{
		Platform = platform;
		textures = new Dictionary<string, int>();
		sessionManager = new SessionManager();
		hotkeyManager = new HotkeyManager();
		FrameProfiler = new FrameProfilerUtil(delegate(string text)
		{
			platform.Logger.Notification(text);
		});
	}

	public void Start(ClientProgramArgs args, string[] rawArgs)
	{
		api = new MainMenuAPI(this);
		GuiComposers = new GuiComposerManager(api);
		ParsedArgs = args;
		RawCmdLineArgs = rawArgs;
		MainThreadId = Environment.CurrentManagedThreadId;
		RuntimeEnv.MainThreadId = MainThreadId;
		Platform.SetTitle("Vintage Story");
		mainScreen = new GuiScreenMainRight(this, null);
		Platform.SetWindowClosedHandler(onWindowClosed);
		Platform.SetFrameHandler(this);
		Platform.RegisterKeyboardEvent(this);
		Platform.RegisterMouseEvent(this);
		Platform.RegisterOnFocusChange(onFocusChanged);
		Platform.SetFileDropHandler(onFileDrop);
		LoadAndCacheScreen(typeof(GuiScreenLoadingGame));
		versionNumberTexture = api.Gui.TextTexture.GenUnscaledTextTexture(GameVersion.LongGameVersion, CairoFont.WhiteDetailText());
		Thread thread = new Thread(DoGameInitStage1);
		thread.Start();
		thread.IsBackground = true;
		registerSettingsWatchers();
		Platform.GlDebugMode = ClientSettings.GlDebugMode;
		Platform.GlErrorChecking = ClientSettings.GlErrorChecking;
		Platform.GlToggleBlend(on: true);
		TyronThreadPool.QueueLongDurationTask(delegate
		{
			while (true)
			{
				string environmentVariable = Environment.GetEnvironmentVariable("TEXTURE_DEBUG_DISPOSE");
				string environmentVariable2 = Environment.GetEnvironmentVariable("CAIRO_DEBUG_DISPOSE");
				string environmentVariable3 = Environment.GetEnvironmentVariable("VAO_DEBUG_DISPOSE");
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					RuntimeEnv.DebugTextureDispose = environmentVariable == "1";
				}
				if (!string.IsNullOrEmpty(environmentVariable2))
				{
					CairoDebug.Enabled = environmentVariable2 == "1";
				}
				if (!string.IsNullOrEmpty(environmentVariable3))
				{
					RuntimeEnv.DebugVAODispose = environmentVariable3 == "1";
				}
				Thread.Sleep(1000);
			}
		});
	}

	public void registerSettingsWatchers()
	{
		ClientSettings.Inst.AddWatcher<int>("masterSoundLevel", OnMasterSoundLevelChanged);
		ClientSettings.Inst.AddWatcher<int>("musicLevel", OnMusicLevelChanged);
		ClientSettings.Inst.AddWatcher<int>("windowBorder", OnWindowBorderChanged);
		ClientSettings.Inst.AddWatcher<int>("gameWindowMode", OnWindowModeChanged);
		ClientSettings.Inst.AddWatcher("glDebugMode", delegate(bool val)
		{
			Platform.GlDebugMode = val;
		});
		ClientSettings.Inst.AddWatcher("glErrorChecking", delegate(bool val)
		{
			Platform.GlErrorChecking = val;
		});
		ClientSettings.Inst.AddWatcher("guiScale", delegate(float val)
		{
			RuntimeEnv.GUIScale = val;
			GuiComposers.MarkAllDialogsForRecompose();
			loadCursors();
		});
		Platform.AddAudioSettingsWatchers();
		Platform.MasterSoundLevel = (float)ClientSettings.MasterSoundLevel / 100f;
	}

	private void OnWindowModeChanged(int newvalue)
	{
		CurrentScreen.ElementComposer?.GetDropDown("windowModeSwitch")?.SetSelectedIndex(GuiCompositeSettings.GetWindowModeIndex());
	}

	private void OnWindowBorderChanged(int newValue)
	{
		Platform.WindowBorder = (EnumWindowBorder)newValue;
		CurrentScreen.ElementComposer?.GetDropDown("windowBorder")?.SetSelectedIndex(ClientSettings.WindowBorder);
	}

	public void DoGameInitStage1()
	{
		sessionManager.GetNewestVersion(OnReceivedNewestVersion);
		loadingText = Lang.Get("Loading assets");
		Platform.LoadAssets();
		loadingText = Lang.Get("Loading sounds");
		Platform.Logger.Notification("Loading sounds");
		LoadSoundsInitial();
		Platform.Logger.Notification("Sounds loaded");
		loadingText = null;
	}

	public void DoGameInitStage2()
	{
		ShaderRegistry.Load();
		if (!cursorsLoaded)
		{
			loadCursors();
			cursorsLoaded = true;
		}
		TyronThreadPool.QueueTask(delegate
		{
			AssetLocation location = new AssetLocation("music/roots.ogg");
			AudioData audioData = LoadMusicTrack(Platform.AssetManager.TryGet_BaseAssets(location));
			if (audioData != null)
			{
				Random random = new Random();
				IntroMusic = Platform.CreateAudio(new SoundParams
				{
					SoundType = EnumSoundType.Music,
					Location = location
				}, audioData);
				while (!Platform.IsShuttingDown && !introMusicShouldStop)
				{
					IntroMusic.Start();
					while (!IntroMusic.HasStopped)
					{
						Thread.Sleep(100);
						if (Platform.IsShuttingDown)
						{
							break;
						}
					}
					if (!Platform.IsShuttingDown)
					{
						Thread.Sleep((300 + random.Next(600)) * 1000);
					}
				}
			}
		});
		hotkeyManager.RegisterDefaultHotKeys();
		setupHotkeyHandlers();
		if (!sessionManager.IsCachedSessionKeyValid())
		{
			Platform.Logger.Notification("Cached session key is invalid, require login");
			Platform.ToggleOffscreenBuffer(enable: true);
			LoadAndCacheScreen(typeof(GuiScreenLogin));
			return;
		}
		Platform.Logger.Notification("Cached session key is valid, validating with server");
		loadingText = "Validating session with server";
		sessionManager.ValidateSessionKeyWithServer(OnValidationDone);
		TyronThreadPool.QueueTask(delegate
		{
			int num = 1;
			Thread.Sleep(1000);
			while (awaitValidation)
			{
				loadingText = "Validating session with server\n" + num;
				num++;
				Thread.Sleep(1000);
			}
		});
	}

	private void setupHotkeyHandlers()
	{
		hotkeyManager.SetHotKeyHandler("recomposeallguis", delegate
		{
			GuiComposers.RecomposeAllDialogs();
			return true;
		}, isIngameHotkey: false);
		hotkeyManager.SetHotKeyHandler("reloadworld", delegate
		{
			CurrentScreen.ReloadWorld("Reload world hotkey triggered");
			return true;
		}, isIngameHotkey: false);
		hotkeyManager.SetHotKeyHandler("cycledialogoutlines", delegate
		{
			GuiComposer.Outlines = (GuiComposer.Outlines + 1) % 3;
			return true;
		}, isIngameHotkey: false);
		hotkeyManager.SetHotKeyHandler("togglefullscreen", delegate
		{
			GuiCompositeSettings.SetWindowMode((Platform.GetWindowState() != WindowState.Fullscreen) ? 1 : 0);
			return true;
		}, isIngameHotkey: false);
	}

	private void loadCursors()
	{
		LoadCursor("textselect");
		LoadCursor("linkselect");
		LoadCursor("move");
		LoadCursor("busy");
		LoadCursor("normal");
		Platform.UseMouseCursor("normal", forceUpdate: true);
	}

	private void LoadCursor(string code)
	{
		if (RuntimeEnv.OS != OS.Mac)
		{
			Dictionary<string, Vec2i> coords = Platform.AssetManager.Get("textures/gui/cursors/coords.json").ToObject<Dictionary<string, Vec2i>>();
			BitmapRef bmp = Platform.AssetManager.TryGet_BaseAssets("textures/gui/cursors/" + code + ".png")?.ToBitmap(api);
			if (bmp != null)
			{
				Platform.LoadMouseCursor(code, coords[code].X, coords[code].Y, bmp);
			}
		}
	}

	private void OnReceivedNewestVersion(string newestversion)
	{
		newestVersion = newestversion;
	}

	private void OnValidationDone(EnumAuthServerResponse response)
	{
		validationResponse = response;
		awaitValidation = false;
	}

	private void DoGameInitStage3()
	{
		Platform.ToggleOffscreenBuffer(enable: true);
		ILogger logger = Platform.Logger;
		EnumAuthServerResponse? enumAuthServerResponse = validationResponse;
		logger.Notification("Server validation response: " + enumAuthServerResponse.ToString());
		ClientIsOffline = false;
		if (validationResponse == EnumAuthServerResponse.Good)
		{
			DoGameInitStage4();
		}
		else if (validationResponse.GetValueOrDefault() == EnumAuthServerResponse.Bad)
		{
			LoadAndCacheScreen(typeof(GuiScreenLogin));
			validationResponse = null;
		}
		else
		{
			ClientIsOffline = true;
			DoGameInitStage4();
		}
	}

	internal void DoGameInitStage4()
	{
		validationResponse = null;
		loadingText = "Loading mod meta infos";
		TyronThreadPool.QueueTask(delegate
		{
			loadMods();
			EnqueueMainThreadTask(DoGameInitStage5);
		});
	}

	private void DoGameInitStage5()
	{
		StartMainMenu();
		HandleArgs();
	}

	public static void CatalogSounds(Action onCompleted)
	{
		AsyncSoundLoadComplete = false;
		soundAudioData.Clear();
		new LoadSoundsThread(Platform.Logger, null, onCompleted).Process();
	}

	public static void LoadSoundsSlow_Async(ClientMain game)
	{
		Thread thread = new Thread(new LoadSoundsThread(Platform.Logger, game, null).ProcessSlow);
		thread.IsBackground = true;
		thread.Priority = ThreadPriority.BelowNormal;
		thread.Name = "LoadSounds async";
		thread.Start();
	}

	public static void LoadSoundsInitial()
	{
		List<IAsset> soundAssets = Platform.AssetManager.GetMany(AssetCategory.sounds, loadAsset: false);
		Platform.Logger.VerboseDebug("Loadsounds, found " + soundAssets.Count + " sounds");
		List<AudioData> menuSounds = new List<AudioData>();
		foreach (IAsset soundAsset in soundAssets)
		{
			LoadSound(soundAsset);
			if (soundAsset.Location.PathStartsWith("sounds/menubutton"))
			{
				menuSounds.Add(new AudioMetaData(soundAsset)
				{
					Loaded = 0
				});
			}
		}
		foreach (AudioData item in menuSounds)
		{
			item.Load();
			Thread.Sleep(1);
		}
	}

	public static void LoadSoundsSlow(ClientMain game)
	{
		bool debug = ClientSettings.ExtendedDebugInfo;
		foreach (KeyValuePair<AssetLocation, AudioData> soundEntry2 in soundAudioDataAsyncLoadTemp)
		{
			if (game.disposed)
			{
				return;
			}
			if (soundEntry2.Key.BeginsWith("game", "sounds/weather/") || soundEntry2.Key.BeginsWith("game", "sounds/environment") || soundEntry2.Key.BeginsWith("game", "sounds/effect/tempstab") || soundEntry2.Key.BeginsWith("game", "sounds/effect/rift"))
			{
				if (debug)
				{
					Platform.Logger.VerboseDebug("Load sound asset " + soundEntry2.Key.ToShortString());
				}
				soundEntry2.Value.Load();
				Thread.Sleep(15);
				if (game.disposed)
				{
					return;
				}
			}
		}
		AsyncSoundLoadComplete = true;
		Platform.Logger.VerboseDebug("Loaded highest priority sound assets");
		if (ClientSettings.OptimizeRamMode < 2)
		{
			if (game.disposed)
			{
				return;
			}
			foreach (KeyValuePair<AssetLocation, AudioData> soundEntry in soundAudioDataAsyncLoadTemp)
			{
				if (debug && soundEntry.Value.Loaded == 0)
				{
					Platform.Logger.VerboseDebug("Load sound asset " + soundEntry.Key);
				}
				soundEntry.Value.Load();
				Thread.Sleep(20);
				if (game.disposed)
				{
					return;
				}
			}
		}
		soundAudioDataAsyncLoadTemp.Clear();
	}

	public static AudioData LoadMusicTrack(IAsset asset)
	{
		if (asset != null && !asset.IsLoaded())
		{
			asset.Origin.LoadAsset(asset);
		}
		if (asset == null || asset.Data == null)
		{
			return null;
		}
		if (!soundAudioData.TryGetValue(asset.Location, out var track))
		{
			soundAudioData[asset.Location] = Platform.CreateAudioData(asset);
			asset.Data = null;
		}
		else
		{
			track.Load();
		}
		return soundAudioData[asset.Location];
	}

	public static AudioData LoadSound(IAsset asset)
	{
		if (asset == null)
		{
			return null;
		}
		return soundAudioData[asset.Location] = new AudioMetaData(asset)
		{
			Loaded = 0
		};
	}

	public void loadMods()
	{
		List<string> modSearchPaths = new List<string>(ClientSettings.ModPaths);
		if (ParsedArgs.AddModPath != null)
		{
			modSearchPaths.AddRange(ParsedArgs.AddModPath);
		}
		modloader = new ModLoader(GamePlatform.Logger, EnumAppSide.Client, modSearchPaths, ParsedArgs.TraceLog);
		allMods.Clear();
		allMods.AddRange(modloader.LoadModInfos());
		verifiedMods = modloader.DisableAndVerify(new List<ModContainer>(allMods), ClientSettings.DisabledMods);
		CrashReporter.LoadedMods = verifiedMods.Where((ModContainer mod) => mod.Enabled).ToList();
	}

	private void HandleArgs()
	{
		if (ParsedArgs.AddOrigin != null)
		{
			foreach (string item in ParsedArgs.AddOrigin)
			{
				string[] domainPaths = Directory.GetDirectories(item);
				for (int i = 0; i < domainPaths.Length; i++)
				{
					string domain = new DirectoryInfo(domainPaths[i]).Name;
					Platform.AssetManager.CustomAppOrigins.Add(new PathOrigin(domain, domainPaths[i], "textures"));
				}
			}
		}
		if (ParsedArgs.ConnectServerAddress != null)
		{
			ConnectToMultiplayer(ParsedArgs.ConnectServerAddress, ParsedArgs.Password);
		}
		else if (ParsedArgs.InstallModId != null)
		{
			EnqueueMainThreadTask(delegate
			{
				GamePlatform.XPlatInterface.FocusWindow();
				InstallMod(ParsedArgs.InstallModId);
			});
		}
		else if (ParsedArgs.OpenWorldName != null || ParsedArgs.CreateRndWorld)
		{
			EnqueueMainThreadTask(openWorldFromArgs);
		}
	}

	private void openWorldFromArgs()
	{
		string playstyle = ParsedArgs.PlayStyle;
		string worldname = ParsedArgs.OpenWorldName;
		if (worldname == null)
		{
			int i = 0;
			while (File.Exists(GamePaths.Saves + System.IO.Path.DirectorySeparatorChar + "world" + i + ".vcdbs"))
			{
				i++;
			}
			worldname = "world" + i + ".vcdbs";
		}
		foreach (ModContainer mod in verifiedMods)
		{
			if (mod.WorldConfig?.PlayStyles == null)
			{
				continue;
			}
			PlayStyle[] playStyles = mod.WorldConfig.PlayStyles;
			foreach (PlayStyle modplaystyle in playStyles)
			{
				if (modplaystyle.LangCode == playstyle)
				{
					StartServerArgs startServerArgs = new StartServerArgs();
					ReadOnlySpan<char> readOnlySpan = GamePaths.Saves;
					char reference = System.IO.Path.DirectorySeparatorChar;
					startServerArgs.SaveFileLocation = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference), worldname, ".vcdbs");
					startServerArgs.WorldName = worldname;
					startServerArgs.PlayStyle = modplaystyle.Code;
					startServerArgs.PlayStyleLangCode = modplaystyle.LangCode;
					startServerArgs.WorldType = modplaystyle.WorldType;
					startServerArgs.WorldConfiguration = modplaystyle.WorldConfig;
					startServerArgs.AllowCreativeMode = true;
					startServerArgs.DisabledMods = ClientSettings.DisabledMods;
					startServerArgs.Language = ClientSettings.Language;
					ConnectToSingleplayer(startServerArgs);
					break;
				}
			}
		}
	}

	public void OnNewFrame(float dt)
	{
		if (debugDrawCallNextFrame)
		{
			Platform.DebugDrawCalls = true;
		}
		if (validationResponse.HasValue)
		{
			DoGameInitStage3();
		}
		if (CurrentScreen is GuiScreenRunningGame)
		{
			Platform.MaxFps = ClientSettings.MaxFPS;
		}
		else
		{
			Platform.MaxFps = 60f;
		}
		if (Environment.TickCount - lastSaveCheck > 2000)
		{
			ClientSettings.Inst.Save();
			lastSaveCheck = Environment.TickCount;
			if (guiMainmenuLeft != null && newestVersion != null)
			{
				if (GameVersion.IsNewerVersionThan(newestVersion, "1.20.7"))
				{
					guiMainmenuLeft.SetHasNewVersion(newestVersion);
				}
				newestVersion = null;
			}
		}
		int quantity = MainThreadTasks.Count;
		while (quantity-- > 0)
		{
			lock (MainThreadTasks)
			{
				MainThreadTasks.Dequeue()();
			}
		}
		Render(dt);
		FrameProfiler.Mark("rendered");
		if (debugDrawCallNextFrame)
		{
			Platform.DebugDrawCalls = false;
			debugDrawCallNextFrame = false;
		}
	}

	public static void EnqueueMainThreadTask(Action a)
	{
		lock (MainThreadTasks)
		{
			MainThreadTasks.Enqueue(a);
		}
	}

	public static void EnqueueCallBack(Action a, int msdelay)
	{
		TyronThreadPool.QueueTask(delegate
		{
			Thread.Sleep(msdelay);
			EnqueueMainThreadTask(a);
		});
	}

	internal void Render(float dt)
	{
		int width = Platform.WindowSize.Width;
		int height = Platform.WindowSize.Height;
		Platform.GlViewport(0, 0, width, height);
		Platform.ClearFrameBuffer(EnumFrameBuffer.Default);
		Platform.ClearFrameBuffer(EnumFrameBuffer.Primary);
		Platform.GlDisableDepthTest();
		Platform.GlDisableCullFace();
		Platform.CheckGlError();
		Platform.DoPostProcessingEffects = false;
		Platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
		CurrentScreen.RenderToPrimary(dt);
		FrameProfiler.Mark("doneRend");
		float[] projMat = null;
		if (CurrentScreen is GuiScreenRunningGame cs)
		{
			projMat = cs.runningGame.CurrentProjectionMatrix;
		}
		Platform.RenderPostprocessingEffects(projMat);
		CurrentScreen.RenderAfterPostProcessing(dt);
		Platform.RenderFinalComposition();
		CurrentScreen.RenderAfterFinalComposition(dt);
		Platform.BlitPrimaryToDefault();
		Platform.CheckGlError();
		FrameProfiler.Mark("doneRender2Default");
		Mat4f.Identity(api.renderapi.pMatrix);
		Mat4f.Ortho(api.renderapi.pMatrix, 0f, Platform.WindowSize.Width, Platform.WindowSize.Height, 0f, 0f, 20001f);
		Platform.GlDepthFunc(EnumDepthFunction.Lequal);
		float clearval = 20000f;
		GL.ClearBuffer(ClearBuffer.Depth, 0, ref clearval);
		GL.DepthRange(0f, 20000f);
		Platform.GlEnableDepthTest();
		Platform.GlDisableCullFace();
		Platform.GlToggleBlend(on: true);
		CurrentScreen.RenderAfterBlit(dt);
		if (CurrentScreen.RenderBg && !Platform.IsShuttingDown)
		{
			guiMainmenuLeft?.RenderBg(dt, withMainMenu);
			withMainMenu = false;
		}
		CurrentScreen.RenderToDefaultFramebuffer(dt);
		Platform.GlDepthFunc(EnumDepthFunction.Less);
		FrameProfiler.Mark("doneAfterRender");
		Platform.CheckGlError();
	}

	internal void RenderMainMenuParts(float dt, ElementBounds bounds, bool withMainMenu, bool darkenEdges = true)
	{
		this.withMainMenu = withMainMenu;
		if (withMainMenu)
		{
			Platform.GlToggleBlend(on: true);
			mainMenuComposer.Render(dt);
			mainMenuComposer.PostRender(dt);
		}
		float windowSizeX = Platform.WindowSize.Width;
		float windowSizeY = Platform.WindowSize.Height;
		api.Render.Render2DTexturePremultipliedAlpha(versionNumberTexture.TextureId, windowSizeX - (float)versionNumberTexture.Width - 10f, (double)(windowSizeY - (float)versionNumberTexture.Height) - GuiElement.scaled(5.0), versionNumberTexture.Width, versionNumberTexture.Height);
	}

	public IInventory GetOwnInventory(string className)
	{
		throw new NotImplementedException();
	}

	public void SendPacketClient(Packet_Client packetClient)
	{
		throw new NotImplementedException();
	}

	public void TriggerOnMouseEnterSlot(ItemSlot slot)
	{
		throw new NotImplementedException();
	}

	public void TriggerOnMouseLeaveSlot(ItemSlot itemSlot)
	{
		throw new NotImplementedException();
	}

	public void TriggerOnMouseClickSlot(ItemSlot itemSlot)
	{
		throw new NotImplementedException();
	}

	public void RenderItemstack(ItemStack itemstack, double posX, double posY, double posZ, float size, int color, bool rotate = false, bool showStackSize = true)
	{
		throw new NotImplementedException();
	}

	public void BeginClipArea(ElementBounds bounds)
	{
		Platform.GlScissor((int)bounds.renderX, (int)((double)Platform.WindowSize.Height - bounds.renderY - bounds.InnerHeight), (int)bounds.InnerWidth, (int)bounds.InnerHeight);
		Platform.GlScissorFlag(enable: true);
	}

	public void EndClipArea()
	{
		Platform.GlScissorFlag(enable: false);
	}

	private void OnMasterSoundLevelChanged(int newValue)
	{
		Platform.MasterSoundLevel = (float)newValue / 100f;
	}

	private void OnMusicLevelChanged(int newValue)
	{
		if (IntroMusic != null && !IntroMusic.HasStopped)
		{
			IntroMusic.SetVolume();
		}
	}

	private void onWindowClosed()
	{
		CurrentScreen.OnWindowClosed();
	}

	public void OnKeyDown(KeyEvent e)
	{
		KeyboardKeyState[e.KeyCode] = true;
		KeyboardModifiers.AltPressed = e.AltPressed;
		KeyboardModifiers.CtrlPressed = e.CtrlPressed;
		KeyboardModifiers.ShiftPressed = e.ShiftPressed;
		if (CurrentScreen.GetType() != typeof(GuiScreenRunningGame))
		{
			bool handled = hotkeyManager.TriggerGlobalHotKey(e, null, null, keyUp: false);
			e.Handled = handled;
		}
		CurrentScreen.OnKeyDown(e);
	}

	public void OnKeyUp(KeyEvent e)
	{
		KeyboardKeyState[e.KeyCode] = false;
		KeyboardModifiers.AltPressed = e.AltPressed;
		KeyboardModifiers.CtrlPressed = e.CtrlPressed;
		KeyboardModifiers.ShiftPressed = e.ShiftPressed;
		CurrentScreen.OnKeyUp(e);
	}

	public void OnKeyPress(KeyEvent e)
	{
		if (e.KeyCode == 50)
		{
			CurrentScreen.OnBackPressed();
		}
		CurrentScreen.OnKeyPress(e);
	}

	public void OnMouseDown(MouseEvent e)
	{
		MouseButtonState[(int)e.Button] = true;
		mouseX = e.X;
		mouseY = e.Y;
		CurrentScreen.OnMouseDown(e);
	}

	public void OnMouseUp(MouseEvent e)
	{
		MouseButtonState[(int)e.Button] = false;
		CurrentScreen.OnMouseUp(e);
	}

	public void OnMouseMove(MouseEvent e)
	{
		mouseX = e.X;
		mouseY = e.Y;
		CurrentScreen.OnMouseMove(e);
	}

	public void OnMouseWheel(Vintagestory.API.Client.MouseWheelEventArgs e)
	{
		CurrentScreen.OnMouseWheel(e);
	}

	public void LoadAndCacheScreen(Type screenType)
	{
		if (CachedScreens.ContainsValue(screenType))
		{
			if (CurrentScreen != null && !CachedScreens.ContainsKey(CurrentScreen) && CurrentScreen != mainScreen)
			{
				CurrentScreen.Dispose();
			}
			CurrentScreen = CachedScreens.FirstOrDefault((KeyValuePair<GuiScreen, Type> x) => x.Value == screenType).Key;
			if (CurrentScreen != null)
			{
				CurrentScreen.OnScreenLoaded();
			}
		}
		else
		{
			CurrentScreen = (GuiScreen)Activator.CreateInstance(screenType, this, mainScreen);
			CachedScreens[CurrentScreen] = screenType;
			if (CurrentScreen != null)
			{
				CurrentScreen.OnScreenLoaded();
			}
		}
	}

	public void LoadScreen(GuiScreen screen)
	{
		if (CurrentScreen != null && !CachedScreens.ContainsKey(CurrentScreen) && screen != CurrentScreen && screen != null && screen.ShouldDisposePreviousScreen)
		{
			CurrentScreen.Dispose();
		}
		CurrentScreen = screen;
		if (CurrentScreen != null)
		{
			CurrentScreen.OnScreenLoaded();
		}
	}

	public void LoadScreenNoLoadCall(GuiScreen screen)
	{
		if (CurrentScreen != null && !CachedScreens.ContainsKey(CurrentScreen) && screen != CurrentScreen && screen != null && screen.ShouldDisposePreviousScreen)
		{
			CurrentScreen.Dispose();
		}
		CurrentScreen = screen;
	}

	internal void StartMainMenu()
	{
		initMainMenu();
		CurrentScreen = mainScreen;
		CurrentScreen.OnScreenLoaded();
		Platform.MouseGrabbed = false;
	}

	private void initMainMenu()
	{
		GuiComposers.ClearCache();
		foreach (KeyValuePair<GuiScreen, Type> cachedScreen in CachedScreens)
		{
			cachedScreen.Key.Dispose();
		}
		CachedScreens.Clear();
		CurrentScreen?.Dispose();
		mainScreen?.Dispose();
		guiMainmenuLeft?.Dispose();
		guiMainmenuLeft = new GuiCompositeMainMenuLeft(this);
		mainScreen.Compose();
		mainScreen.Refresh();
	}

	public void StartGame(bool singleplayer, StartServerArgs serverargs, ServerConnectData connectData)
	{
		GuiScreenRunningGame screenGame = new GuiScreenRunningGame(this, mainScreen);
		screenGame.Start(singleplayer, serverargs, connectData);
		CurrentScreen = new GuiScreenConnectingToServer(singleplayer, this, screenGame);
	}

	public void InstallMod(string modid)
	{
		if (CurrentScreen is GuiScreenRunningGame screenGame)
		{
			screenGame.ExitOrRedirect(isDisconnect: false, "modinstall request");
			TyronThreadPool.QueueTask(delegate
			{
				int num = 0;
				while (num++ < 1000 && !(CurrentScreen is GuiScreenMainRight))
				{
					Thread.Sleep(100);
				}
				EnqueueMainThreadTask(delegate
				{
					InstallMod(modid);
				});
			}, "mod install");
		}
		else
		{
			modid = modid.Replace("vintagestorymodinstall://", "").TrimEnd('/');
			LoadScreen(new GuiScreenDownloadMods(null, GamePaths.DataPathMods, new List<string> { modid }, this, mainScreen));
		}
	}

	public void ConnectToMultiplayer(string host, string password)
	{
		if (host.Contains("vintagestoryjoin://"))
		{
			GuiScreen curScreen = CurrentScreen;
			LoadScreen(new GuiScreenConfirmAction(Lang.Get("confirm-joinserver", host.Replace("vintagestoryjoin://", "").TrimEnd('/')), delegate(bool ok)
			{
				if (ok)
				{
					ConnectToMultiplayer(host.Replace("vintagestoryjoin://", ""), null);
				}
				else
				{
					LoadScreen(curScreen);
				}
			}, this, CurrentScreen));
			return;
		}
		try
		{
			ServerConnectData connectData = ServerConnectData.FromHost(host);
			connectData.ServerPassword = password;
			StartGame(singleplayer: false, null, connectData);
		}
		catch (Exception e)
		{
			LoadScreen(new GuiScreenDisconnected(Lang.Get("multiplayer-disconnected", e.Message), this, mainScreen));
			Platform.Logger.Warning("Could not initiate connection:");
			Platform.Logger.Warning(e);
		}
	}

	public void ConnectToSingleplayer(StartServerArgs serverargs)
	{
		StartGame(singleplayer: true, serverargs, null);
	}

	internal int GetGuiTexture(string name)
	{
		if (!textures.ContainsKey(name))
		{
			BitmapRef bmp = Platform.AssetManager.Get("textures/gui/" + name).ToBitmap(api);
			int textureid = Platform.LoadTexture(bmp);
			textures[name] = textureid;
			bmp.Dispose();
		}
		return textures[name];
	}

	public int GetMouseCurrentX()
	{
		return mouseX;
	}

	public int GetMouseCurrentY()
	{
		return mouseY;
	}

	public static void PlaySound(string name)
	{
		PlaySound(new AssetLocation("sounds/" + name).WithPathAppendixOnce(".ogg"));
	}

	public static void PlaySound(AssetLocation location)
	{
		AudioData data = null;
		location = location.Clone().WithPathAppendixOnce(".ogg");
		soundAudioData.TryGetValue(location, out data);
		if (data != null)
		{
			if (!data.Load())
			{
				return;
			}
			ILoadedSound sound = Platform.CreateAudio(new SoundParams(location), data);
			sound.Start();
			TyronThreadPool.QueueLongDurationTask(delegate
			{
				while (!sound.HasStopped)
				{
					Thread.Sleep(100);
				}
				sound.Dispose();
			});
		}
		else
		{
			Platform.Logger.Error("Could not play {0}, sound file not found", location);
		}
	}

	public ClientPlatformAbstract getGamePlatform()
	{
		return Platform;
	}

	private void onFocusChanged(bool focus)
	{
		CurrentScreen.OnFocusChanged(focus);
	}

	private void onFileDrop(string filename)
	{
		CurrentScreen.OnFileDrop(filename);
	}

	internal void TryRedirect(MultiplayerServerEntry entry)
	{
		Platform.Logger.Notification("Redirecting to new server");
		ConnectToMultiplayer(entry.host, entry.password);
	}
}
