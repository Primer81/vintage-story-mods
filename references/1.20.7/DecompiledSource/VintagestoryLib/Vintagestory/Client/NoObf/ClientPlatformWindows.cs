using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Cairo;
using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SkiaSharp;
using VSPlatform;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.ClientNative;
using Vintagestory.Common;
using Vintagestory.Common.Convert;

namespace Vintagestory.Client.NoObf;

public sealed class ClientPlatformWindows : ClientPlatformAbstract
{
	public class GLBuffer
	{
		public int BufferId;
	}

	private AudioOpenAl audio;

	public GameExit gameexit;

	public bool SupportsThickLines;

	private int cpuCoreCount = 2;

	private AssetManager assetManager;

	private Stopwatch frameStopWatch;

	internal Stopwatch uptimeStopWatch = new Stopwatch();

	private Logger logger;

	private int doResize;

	private static DebugProc _debugProcCallback;

	private static GCHandle _debugProcCallbackHandle;

	public GameWindowNative window;

	private Screenshot screenshot = new Screenshot();

	public Action<StartServerArgs> OnStartSinglePlayerServer;

	public GameExit ServerExit = new GameExit();

	public bool singlePlayerServerLoaded;

	public DummyNetwork[] singlePlayerServerDummyNetwork;

	private Size2i windowsize = new Size2i();

	private Size2i screensize = new Size2i();

	private List<OnFocusChanged> focusChangedDelegates = new List<OnFocusChanged>();

	private Action windowClosedHandler;

	private NewFrameHandler frameHandler;

	public CrashReporter crashreporter;

	private OnCrashHandler onCrashHandler;

	public List<KeyEventHandler> keyEventHandlers = new List<KeyEventHandler>();

	public List<MouseEventHandler> mouseEventHandlers = new List<MouseEventHandler>();

	public Action<string> fileDropEventHandler;

	private bool debugDrawCalls;

	private List<string> drawCallStacks = new List<string>();

	private List<FrameBufferRef> frameBuffers;

	private MeshRef screenQuad;

	private bool serverRunning;

	private bool gamepause;

	private bool OffscreenBuffer = true;

	private bool RenderBloom;

	private bool RenderGodRays;

	private bool RenderFXAA;

	private bool RenderSSAO;

	private bool SetupSSAO;

	private int ShadowMapQuality;

	private float ssaaLevel;

	public GLBuffer[] PixelPackBuffer;

	public int sampleCount = 32;

	public int CurrentPixelPackBufferNum;

	private Random rand = new Random();

	private float[] ssaoKernel = new float[192];

	private FrameBufferRef curFb;

	private float[] clearColor = new float[4] { 0f, 0f, 0f, 1f };

	private bool glDebugMode;

	private bool supportsGlDebugMode;

	private bool supportsPersistentMapping;

	public bool ENABLE_MIPMAPS = true;

	public bool ENABLE_ANISOTROPICFILTERING;

	public bool ENABLE_TRANSPARENCY = true;

	private Vector2 previousMousePosition;

	private CursorState previousCursorState;

	private float mouseX;

	private float mouseY;

	private Dictionary<string, MouseCursor> preLoadedCursors = new Dictionary<string, MouseCursor>();

	private bool ignoreMouseMoveEvent;

	private float prevWheelValue;

	private long lastKeyUpMs;

	private int lastKeyUpKey;

	private ShaderProgramMinimalGui minimalGuiShaderProgram;

	public override IList<string> AvailableAudioDevices => audio.Devices;

	public override string CurrentAudioDevice
	{
		get
		{
			return audio.CurrentDevice;
		}
		set
		{
			LoadedSoundNative.ChangeOutputDevice(delegate
			{
				audio.SetDevice(logger, value);
			});
		}
	}

	public override float MasterSoundLevel
	{
		get
		{
			return audio.MasterSoundLevel;
		}
		set
		{
			audio.MasterSoundLevel = value;
		}
	}

	public override AssetManager AssetManager => assetManager;

	public override ILogger Logger => logger;

	public override long EllapsedMs => uptimeStopWatch.ElapsedMilliseconds;

	public override Size2i WindowSize => windowsize;

	public override Size2i ScreenSize => screensize;

	public override EnumWindowBorder WindowBorder
	{
		get
		{
			return (EnumWindowBorder)window.WindowBorder;
		}
		set
		{
			window.WindowBorder = (WindowBorder)value;
		}
	}

	public override int CpuCoreCount => cpuCoreCount;

	public override bool IsFocused => window.IsFocused;

	public override bool DebugDrawCalls
	{
		get
		{
			return debugDrawCalls;
		}
		set
		{
			debugDrawCalls = value;
			if (!value)
			{
				logger.Notification("Call stacks:");
				int i = 0;
				foreach (string val in drawCallStacks)
				{
					logger.Notification("{0}: {1}", i++, val.Substring(0, 600));
				}
			}
			drawCallStacks.Clear();
		}
	}

	public override List<FrameBufferRef> FrameBuffers => frameBuffers;

	public override bool IsServerRunning
	{
		get
		{
			return serverRunning;
		}
		set
		{
			serverRunning = value;
		}
	}

	public override bool IsGamePaused => gamepause;

	public FrameBufferRef CurrentFrameBuffer
	{
		get
		{
			return curFb;
		}
		set
		{
			curFb = value;
			if (value == null)
			{
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			}
			else
			{
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, value.FboId);
			}
		}
	}

	public override bool GlErrorChecking { get; set; }

	public override bool GlDebugMode
	{
		get
		{
			return glDebugMode;
		}
		set
		{
			if (value)
			{
				if (!supportsGlDebugMode)
				{
					throw new NotSupportedException("Your graphics card does not seem to support gl debug mode (neither GL_ARB_debug_output nor GL_KHR_debug was found)");
				}
				_debugProcCallback = DebugCallback;
				_debugProcCallbackHandle = GCHandle.Alloc(_debugProcCallback);
				GL.DebugMessageCallback(_debugProcCallback, IntPtr.Zero);
				GL.Enable(EnableCap.DebugOutput);
				GL.Enable(EnableCap.DebugOutputSynchronous);
			}
			else
			{
				GL.Disable(EnableCap.DebugOutput);
				GL.Disable(EnableCap.DebugOutputSynchronous);
			}
			glDebugMode = value;
		}
	}

	public override bool GlScissorFlagEnabled => GL.IsEnabled(EnableCap.ScissorTest);

	public override string CurrentMouseCursor { get; protected set; }

	public override bool MouseGrabbed
	{
		get
		{
			return window.CursorState == CursorState.Grabbed;
		}
		set
		{
			CursorState newState = (value ? CursorState.Grabbed : CursorState.Normal);
			if (newState != window.CursorState)
			{
				Vector2 newPos = new Vector2((float)window.ClientSize.X / 2f, (float)window.ClientSize.Y / 2f);
				SetMousePosition(newPos.X, newPos.Y);
				window.MousePosition = newPos;
			}
			window.CursorState = newState;
		}
	}

	public override DefaultShaderUniforms ShaderUniforms { get; set; } = new DefaultShaderUniforms();


	public override ShaderProgramMinimalGui MinimalGuiShader => minimalGuiShaderProgram;

	public void StartAudio()
	{
		if (audio == null)
		{
			audio = new AudioOpenAl(logger);
			audio.d_GameExit = gameexit;
		}
	}

	public override void AddAudioSettingsWatchers()
	{
		ClientSettings.Inst.AddWatcher("audioDevice", delegate(string newDevice)
		{
			CurrentAudioDevice = newDevice;
		});
		ClientSettings.Inst.AddWatcher<bool>("useHRTFaudio", delegate
		{
			LoadedSoundNative.ChangeOutputDevice(delegate
			{
				audio.RecreateContext(logger);
			});
		});
	}

	public void StopAudio()
	{
		ScreenManager.IntroMusic?.Dispose();
		if (audio != null)
		{
			audio.Dispose();
			audio = null;
		}
	}

	public override AudioData CreateAudioData(IAsset asset)
	{
		StartAudio();
		AudioMetaData sampleFromArray = audio.GetSampleFromArray(asset);
		sampleFromArray.Loaded = 2;
		return sampleFromArray;
	}

	public override ILoadedSound CreateAudio(SoundParams sound, AudioData data)
	{
		if ((data as AudioMetaData).Asset == null)
		{
			return null;
		}
		if (data.Loaded < 2)
		{
			if (data.Loaded == 0)
			{
				logger.VerboseDebug("Loading sound file, game may stutter " + (data as AudioMetaData)?.Asset.Location);
				data.Load();
			}
			else
			{
				logger.VerboseDebug("Attempt to use still-loading sound file, sound may error or not play " + (data as AudioMetaData)?.Asset.Location);
			}
		}
		return new LoadedSoundNative(sound, (AudioMetaData)data);
	}

	public override ILoadedSound CreateAudio(SoundParams sound, AudioData data, ClientMain game)
	{
		if ((data as AudioMetaData)?.Asset == null)
		{
			return null;
		}
		if (data.Loaded == 0)
		{
			logger.VerboseDebug("Loading sound file, game may stutter " + (data as AudioMetaData)?.Asset.Location);
			data.Load();
		}
		return new LoadedSoundNative(sound, (AudioMetaData)data, game);
	}

	public override void UpdateAudioListener(float posX, float posY, float posZ, float orientX, float orientY, float orientZ)
	{
		StartAudio();
		audio.UpdateListener(new Vector3(posX, posY, posZ), new Vector3(orientX, orientY, orientZ));
	}

	public ClientPlatformWindows(Logger logger)
	{
		if (logger == null)
		{
			this.logger = new NullLogger();
		}
		else
		{
			base.XPlatInterface = XPlatformInterfaces.GetInterface();
			screensize = base.XPlatInterface.GetScreenSize();
			if (RuntimeEnv.OS == OS.Mac && screensize.Width > 2500)
			{
				screensize.Width = screensize.Width * 5 / 8;
				screensize.Height = screensize.Height * 5 / 8;
			}
			this.logger = logger;
		}
		TyronThreadPool.Inst.Logger = this.logger;
		uptimeStopWatch.Start();
		frameStopWatch = new Stopwatch();
		frameStopWatch.Start();
	}

	private void window_RenderFrame(FrameEventArgs e)
	{
		if (doResize != 0 && Environment.TickCount >= doResize)
		{
			Window_Resize();
		}
		ScreenManager.FrameProfiler.Begin(null);
		if (ClientSettings.VsyncMode != 1 && base.MaxFps > 10f && base.MaxFps < 241f)
		{
			int freeTime = (int)(1000f / base.MaxFps - 1000f * (float)frameStopWatch.ElapsedTicks / (float)Stopwatch.Frequency);
			if (freeTime > 0)
			{
				Thread.Sleep(freeTime);
			}
		}
		float dt = (float)frameStopWatch.ElapsedTicks / (float)Stopwatch.Frequency;
		frameStopWatch.Restart();
		ScreenManager.FrameProfiler.Mark("sleep");
		UpdateMousePosition();
		RenderBloom = ClientSettings.Bloom && base.DoPostProcessingEffects;
		RenderGodRays = ClientSettings.GodRayQuality > 0 && base.DoPostProcessingEffects;
		RenderFXAA = ClientSettings.FXAA && base.DoPostProcessingEffects;
		RenderSSAO = ClientSettings.SSAOQuality > 0 && base.DoPostProcessingEffects;
		SetupSSAO = ClientSettings.SSAOQuality > 0;
		ShadowMapQuality = ClientSettings.ShadowMapQuality;
		ShaderProgramBase.shadowmapQuality = ShadowMapQuality;
		frameHandler.OnNewFrame(dt);
		window.SwapBuffers();
		ScreenManager.FrameProfiler.End();
	}

	public string GetGraphicsCardRenderer()
	{
		return GL.GetString(StringName.Renderer);
	}

	public void LogAndTestHardwareInfosStage1()
	{
		logger.Notification("Process path: {0}", Environment.ProcessPath);
		logger.Notification("Operating System: " + RuntimeEnv.GetOsString());
		logger.Notification("CPU Cores: {0}", Environment.ProcessorCount);
		logger.Notification("Available RAM: {0} MB", base.XPlatInterface.GetRamCapacity() / 1024);
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			LogFrameworkVersions();
		}
	}

	private void LogFrameworkVersions()
	{
		logger.Notification("C# Framework: " + GetFrameworkInfos());
		logger.Notification("Cairo Graphics Version: " + CairoAPI.VersionString);
	}

	public void LogAndTestHardwareInfosStage2()
	{
		logger.Notification("Graphics Card Vendor: " + GL.GetString(StringName.Vendor));
		logger.Notification("Graphics Card Version: " + GL.GetString(StringName.Version));
		logger.Notification("Graphics Card Renderer: " + GL.GetString(StringName.Renderer));
		logger.Notification("Graphics Card ShadingLanguageVersion: " + GL.GetString(StringName.ShadingLanguageVersion));
		logger.Notification("GL.MaxVertexUniformComponents: " + GL.GetInteger(GetPName.MaxVertexUniformComponents));
		logger.Notification("GL.MaxUniformBlockSize: " + GL.GetInteger(GetPName.MaxUniformBlockSize));
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			LogFrameworkVersions();
		}
		logger.Notification("OpenAL Version: " + AL.Get(ALGetString.Version));
		string path = System.IO.Path.Combine(GamePaths.Binaries, "Lib/OpenTK.dll");
		if (File.Exists(path))
		{
			FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(path);
			logger.Notification("OpenTK Version: " + fvi.FileVersion + " (" + fvi.Comments + ")");
		}
		logger.Notification("Zstd Version: " + ZstdNative.Version);
		CheckGlError("loghwinfo");
		if (RuntimeEnv.OS != OS.Mac && ClientSettings.TestGlExtensions)
		{
			HashSet<string> required = new HashSet<string>(new string[4] { "GL_ARB_framebuffer_object", "GL_ARB_vertex_array_object", "GL_ARB_draw_instanced", "GL_ARB_explicit_attrib_location" });
			int count = GL.GetInteger(GetPName.NumExtensions);
			for (int i = 0; i < count; i++)
			{
				string extension = GL.GetString(StringNameIndexed.Extensions, i);
				supportsGlDebugMode |= extension == "GL_ARB_debug_output" || extension == "GL_KHR_debug";
				supportsPersistentMapping |= extension == "GL_ARB_buffer_storage";
				if (required.Contains(extension))
				{
					required.Remove(extension);
				}
			}
			if (required.Count > 0)
			{
				throw new NotSupportedException("Your graphics card does not support the extensions " + string.Join(", ", required) + " which is required to start the game");
			}
		}
		CheckGlError("testhwinfo");
	}

	public override string GetGraphicCardInfos()
	{
		return "GC Vendor: " + GL.GetString(StringName.Vendor) + "\nGC Version: " + GL.GetString(StringName.Version) + "\nGC Renderer: " + GL.GetString(StringName.Renderer) + "\nGC ShaderVersion: " + GL.GetString(StringName.ShadingLanguageVersion);
	}

	public override string GetFrameworkInfos()
	{
		return ".net " + Environment.Version;
	}

	public override bool IsExitAvailable()
	{
		return true;
	}

	public override void SetWindowSize(int width, int height)
	{
		window.ClientSize = new Vector2i(width, height);
		Window_Resize();
	}

	public override BitmapRef CreateBitmap(int width, int height)
	{
		return new BitmapExternal(width, height);
	}

	public override void SetBitmapPixelsArgb(BitmapRef bmp, int[] pixels)
	{
		BitmapExternal bitmapExternal = (BitmapExternal)bmp;
		int width = bitmapExternal.bmp.Width;
		int height = bitmapExternal.bmp.Height;
		FastBitmap fastBitmap = new FastBitmap();
		fastBitmap.bmp = bitmapExternal.bmp;
		for (int i = 0; i < width; i++)
		{
			for (int j = 0; j < height; j++)
			{
				fastBitmap.SetPixel(i, j, pixels[i + j * width]);
			}
		}
	}

	public override BitmapRef CreateBitmapFromPng(IAsset asset)
	{
		using MemoryStream stream = new MemoryStream(asset.Data);
		return new BitmapExternal(stream, Logger, asset.Location);
	}

	public override BitmapRef CreateBitmapFromPng(byte[] data)
	{
		return CreateBitmapFromPng(data, data.Length);
	}

	public override BitmapRef CreateBitmapFromPng(byte[] data, int dataLength)
	{
		return new BitmapExternal(data, dataLength, Logger);
	}

	public override BitmapRef CreateBitmapFromPixels(int[] pixels, int width, int height)
	{
		return new BitmapExternal
		{
			bmp = SKBitmap.FromImage(SKImage.FromPixels(pixels: GCHandle.Alloc(pixels, GCHandleType.Pinned).AddrOfPinnedObject(), info: new SKImageInfo(width, height)))
		};
	}

	public override IAviWriter CreateAviWriter(float framerate, string codec)
	{
		return base.XPlatInterface.GetAviWriter(ClientSettings.RecordingBufferSize, framerate, codec);
	}

	public override AvailableCodec[] GetAvailableCodecs()
	{
		return base.XPlatInterface.AvailableCodecs();
	}

	public override string GetGameVersion()
	{
		return "1.20.7";
	}

	public void SetServerExitInterface(GameExit exit)
	{
		gameexit = exit;
	}

	public override void ThreadSpinWait(int iterations)
	{
		Thread.SpinWait(iterations);
	}

	public override void LoadAssets()
	{
		if (assetManager == null)
		{
			assetManager = new AssetManager(GamePaths.AssetsPath, EnumAppSide.Client);
		}
		logger.Notification("Start discovering assets");
		int count = assetManager.InitAndLoadBaseAssets(logger, "textures");
		logger.Notification("Found {0} base assets in total", count);
	}

	public void Start()
	{
		window.FocusedChanged += window_FocusChanged;
		window.KeyDown += game_KeyDown;
		window.KeyUp += game_KeyUp;
		window.TextInput += game_KeyPress;
		window.MouseDown += Mouse_ButtonDown;
		window.MouseUp += Mouse_ButtonUp;
		window.MouseMove += Mouse_Move;
		window.MouseWheel += Mouse_WheelChanged;
		window.RenderFrame += window_RenderFrame;
		window.Closing += window_Closing;
		window.Resize += OnWindowResize;
		window.Title = "Vintage Story";
		window.FileDrop += Window_FileDrop;
		frameBuffers = SetupDefaultFrameBuffers();
		minimalGuiShaderProgram = new ShaderProgramMinimalGui();
		minimalGuiShaderProgram.Compile();
		windowsize.Width = window.ClientSize.X;
		windowsize.Height = window.ClientSize.Y;
		GL.LineWidth(1.5f);
		OpenTK.Graphics.OpenGL.ErrorCode code = GL.GetError();
		SupportsThickLines = code != OpenTK.Graphics.OpenGL.ErrorCode.InvalidValue;
		cpuCoreCount = Environment.ProcessorCount;
	}

	public override void RebuildFrameBuffers()
	{
		List<FrameBufferRef> oldFrameBuffers = frameBuffers;
		List<FrameBufferRef> newFrameBuffers = SetupDefaultFrameBuffers();
		frameBuffers = newFrameBuffers;
		DisposeFrameBuffers(oldFrameBuffers);
	}

	private void Window_FileDrop(FileDropEventArgs e)
	{
		fileDropEventHandler?.Invoke(e.FileNames[0]);
	}

	private void OnWindowResize(ResizeEventArgs e)
	{
		doResize = Environment.TickCount + 40;
	}

	private void Window_Resize()
	{
		doResize = 0;
		if (window.WindowState != WindowState.Minimized)
		{
			Vector2i windowSize = window.ClientSize;
			if (window.ClientSize.X < 600)
			{
				windowSize.X = 600;
			}
			if (window.ClientSize.Y < 400)
			{
				windowSize.Y = 400;
			}
			if (window.ClientSize.Y < 400 || window.ClientSize.X < 600)
			{
				window.ClientSize = windowSize;
			}
		}
		if (window.ClientSize.X == 0 || window.ClientSize.Y == 0)
		{
			logger.Notification("Window was resized to {0} {1}? Window probably got minimized. Will not rebuild frame buffers", window.ClientSize.X, window.ClientSize.Y);
		}
		else if (window.ClientSize.X != windowsize.Width || window.ClientSize.Y != windowsize.Height)
		{
			logger.Notification("Window was resized to {0} {1}, rebuilding framebuffers...", window.ClientSize.X, window.ClientSize.Y);
			RebuildFrameBuffers();
			windowsize.Width = window.ClientSize.X;
			windowsize.Height = window.ClientSize.Y;
			if (window.WindowState == WindowState.Normal)
			{
				ClientSettings.ScreenWidth = window.Size.X;
				ClientSettings.ScreenHeight = window.Size.Y;
			}
			int windowState = window.WindowState switch
			{
				WindowState.Maximized => 2, 
				WindowState.Fullscreen => (ClientSettings.GameWindowMode != 3) ? 1 : 3, 
				_ => 0, 
			};
			if (ClientSettings.GameWindowMode != windowState)
			{
				ClientSettings.GameWindowMode = windowState;
			}
			TriggerWindowResized(window.ClientSize.X, window.ClientSize.Y);
		}
	}

	private void window_Closing(CancelEventArgs e)
	{
		gameexit.exit = true;
		try
		{
			windowClosedHandler();
		}
		catch (Exception)
		{
		}
	}

	public override void SetVSync(bool enabled)
	{
		window.VSync = (enabled ? VSyncMode.On : VSyncMode.Off);
	}

	public unsafe override void SetDirectMouseMode(bool enabled)
	{
		GLFW.SetInputMode(window.WindowPtr, RawMouseMotionAttribute.RawMouseMotion, enabled);
	}

	public override string SaveScreenshot(string path = null, string filename = null, bool withAlpha = false, bool flip = false, string metaDataStr = null)
	{
		screenshot.d_GameWindow = window;
		FrameBufferRef currentFrameBuffer = CurrentFrameBuffer;
		Size2i size = ((currentFrameBuffer == null) ? new Size2i(window.ClientSize.X, window.ClientSize.Y) : new Size2i(currentFrameBuffer.Width, currentFrameBuffer.Height));
		return screenshot.SaveScreenshot(this, size, path, filename, withAlpha, flip, metaDataStr);
	}

	public override BitmapRef GrabScreenshot(bool withAlpha = false, bool scale = false)
	{
		screenshot.d_GameWindow = window;
		FrameBufferRef currentFrameBuffer = CurrentFrameBuffer;
		Size2i size = ((currentFrameBuffer == null) ? new Size2i(window.ClientSize.X, window.ClientSize.Y) : new Size2i(currentFrameBuffer.Width, currentFrameBuffer.Height));
		SKBitmap bmp = screenshot.GrabScreenshot(size, scale, flip: false, withAlpha);
		return new BitmapExternal
		{
			bmp = bmp
		};
	}

	public override BitmapRef GrabScreenshot(int width, int height, bool scaleScreenshot, bool flip, bool withAlpha = false)
	{
		screenshot.d_GameWindow = window;
		SKBitmap bmp = screenshot.GrabScreenshot(new Size2i(width, height), scaleScreenshot, flip, withAlpha);
		return new BitmapExternal
		{
			bmp = bmp
		};
	}

	public override void WindowExit(string reason)
	{
		logger.Notification("Exiting game now. Server running=" + serverRunning + ". Exit reason: {0}", reason);
		base.IsShuttingDown = true;
		if (gameexit != null)
		{
			gameexit.exit = true;
		}
		try
		{
			UriHandler.Instance.Dispose();
			window?.Close();
		}
		catch (Exception)
		{
			Environment.Exit(0);
		}
	}

	public override void SetTitle(string applicationname)
	{
		window.Title = applicationname;
	}

	public override WindowState GetWindowState()
	{
		return window.WindowState;
	}

	public override void SetWindowState(WindowState value)
	{
		MonitorInfo currentMonitor = Monitors.GetMonitorFromWindow(window);
		if (!((IntPtr)currentMonitor.Handle.Pointer).Equals(window.CurrentMonitor.Pointer))
		{
			window.CurrentMonitor = currentMonitor.Handle;
		}
		window.WindowState = value;
		if (window.Location.Y < 0)
		{
			window.Location = new Vector2i(window.Location.X, 0);
		}
	}

	public unsafe override void SetWindowAttribute(WindowAttribute attribute, bool value)
	{
		GLFW.SetWindowAttrib(window.WindowPtr, attribute, value);
	}

	public override void StartSinglePlayerServer(StartServerArgs serverargs)
	{
		ServerExit = new GameExit();
		OnStartSinglePlayerServer(serverargs);
	}

	public override void ExitSinglePlayerServer()
	{
		ServerExit.SetExit(p: true);
	}

	public override bool IsLoadedSinglePlayerServer()
	{
		return singlePlayerServerLoaded;
	}

	public override DummyNetwork[] GetSinglePlayerServerNetwork()
	{
		return singlePlayerServerDummyNetwork;
	}

	public override void SetFileDropHandler(Action<string> handler)
	{
		fileDropEventHandler = handler;
	}

	public override void RegisterOnFocusChange(OnFocusChanged handler)
	{
		focusChangedDelegates.Add(handler);
	}

	private void window_FocusChanged(FocusedChangedEventArgs e)
	{
		foreach (OnFocusChanged focusChangedDelegate in focusChangedDelegates)
		{
			focusChangedDelegate(window.IsFocused);
		}
	}

	public override void SetWindowClosedHandler(Action handler)
	{
		windowClosedHandler = handler;
	}

	public override void SetFrameHandler(NewFrameHandler handler)
	{
		frameHandler = handler;
	}

	public override void RegisterKeyboardEvent(KeyEventHandler handler)
	{
		keyEventHandlers.Add(handler);
	}

	public override void RegisterMouseEvent(MouseEventHandler handler)
	{
		mouseEventHandlers.Add(handler);
	}

	public override void AddOnCrash(OnCrashHandler handler)
	{
		crashreporter.OnCrash = OnCrash;
		onCrashHandler = handler;
	}

	public override void ClearOnCrash()
	{
		onCrashHandler = null;
		crashreporter.OnCrash = null;
	}

	private void OnCrash()
	{
		if (onCrashHandler != null)
		{
			onCrashHandler.OnCrash();
		}
	}

	public override void RenderMesh(MeshRef modelRef)
	{
		RuntimeStats.drawCallsCount++;
		if (debugDrawCalls)
		{
			drawCallStacks.Add(Environment.StackTrace);
		}
		VAO vao = (VAO)modelRef;
		if (vao.VaoId == 0 || vao.Disposed)
		{
			if (vao.VaoId == 0)
			{
				throw new ArgumentException("Fatal: Trying to render an uninitialized mesh");
			}
			throw new ArgumentException("Fatal: Trying to render a disposed mesh");
		}
		GL.BindVertexArray(vao.VaoId);
		for (int j = 0; j < vao.vaoSlotNumber; j++)
		{
			GL.EnableVertexAttribArray(j);
		}
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, vao.vboIdIndex);
		GL.DrawElements(vao.drawMode, vao.IndicesCount, DrawElementsType.UnsignedInt, 0);
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		for (int i = 0; i < vao.vaoSlotNumber; i++)
		{
			GL.DisableVertexAttribArray(i);
		}
		GL.BindVertexArray(0);
	}

	public void RenderFullscreenTriangle(MeshRef modelRef)
	{
		RuntimeStats.drawCallsCount++;
		GL.BindVertexArray(((VAO)modelRef).VaoId);
		GL.DrawArrays(PrimitiveType.Triangles, 0, 3);
		GL.BindVertexArray(0);
	}

	public override void RenderMesh(MeshRef modelRef, int[] indices, int[] indicesSizes, int groupCount)
	{
		RuntimeStats.drawCallsCount++;
		VAO vao = (VAO)modelRef;
		GL.BindVertexArray(vao.VaoId);
		for (int j = 0; j < vao.vaoSlotNumber; j++)
		{
			GL.EnableVertexAttribArray(j);
		}
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, vao.vboIdIndex);
		GL.MultiDrawElements(vao.drawMode, indicesSizes, DrawElementsType.UnsignedInt, indices, groupCount);
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		for (int i = 0; i < vao.vaoSlotNumber; i++)
		{
			GL.DisableVertexAttribArray(i);
		}
		GL.BindVertexArray(0);
	}

	public override void RenderMeshInstanced(MeshRef modelRef, int quantity = 1)
	{
		RuntimeStats.drawCallsCount++;
		VAO vao = (VAO)modelRef;
		GL.BindVertexArray(vao.VaoId);
		for (int j = 0; j < vao.vaoSlotNumber; j++)
		{
			GL.EnableVertexAttribArray(j);
		}
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, vao.vboIdIndex);
		GL.DrawElementsInstanced(vao.drawMode, vao.IndicesCount, DrawElementsType.UnsignedInt, IntPtr.Zero, quantity);
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		for (int i = 0; i < vao.vaoSlotNumber; i++)
		{
			GL.DisableVertexAttribArray(i);
		}
		GL.BindVertexArray(0);
	}

	public override void SetGamePausedState(bool paused)
	{
		gamepause = paused;
	}

	public override void ResetGamePauseAndUptimeState()
	{
		uptimeStopWatch.Start();
	}

	public override void ToggleOffscreenBuffer(bool enable)
	{
		OffscreenBuffer = enable;
	}

	public override void DisposeFrameBuffer(FrameBufferRef frameBuffer, bool disposeTextures = true)
	{
		if (frameBuffer == null)
		{
			return;
		}
		if (disposeTextures)
		{
			for (int i = 0; i < frameBuffer.ColorTextureIds.Length; i++)
			{
				GLDeleteTexture(frameBuffer.ColorTextureIds[i]);
			}
			if (frameBuffer.DepthTextureId > 0)
			{
				GLDeleteTexture(frameBuffer.DepthTextureId);
			}
		}
		GL.DeleteFramebuffer(frameBuffer.FboId);
	}

	public override FrameBufferRef CreateFramebuffer(FramebufferAttrs fbAttrs)
	{
		FrameBufferRef framebuffer = (CurrentFrameBuffer = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = fbAttrs.Width,
			Height = fbAttrs.Height
		});
		List<DrawBuffersEnum> drawBuffers = new List<DrawBuffersEnum>();
		List<int> colorTextureIds = new List<int>();
		FramebufferAttrsAttachment[] attachments = fbAttrs.Attachments;
		foreach (FramebufferAttrsAttachment fbAtt in attachments)
		{
			RawTexture tex = fbAtt.Texture;
			int textureId = ((tex.TextureId == 0) ? GL.GenTexture() : tex.TextureId);
			if (tex.TextureId == 0)
			{
				GL.BindTexture(TextureTarget.Texture2D, textureId);
				GL.TexImage2D(TextureTarget.Texture2D, 0, (PixelInternalFormat)tex.PixelInternalFormat, tex.Width, tex.Height, 0, (PixelFormat)tex.PixelFormat, PixelType.Float, IntPtr.Zero);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)tex.MinFilter);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)tex.MagFilter);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)tex.WrapS);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)tex.WrapT);
			}
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, (FramebufferAttachment)fbAtt.AttachmentType, TextureTarget.Texture2D, textureId, 0);
			if (fbAtt.AttachmentType == EnumFramebufferAttachment.DepthAttachment)
			{
				GL.DepthFunc(DepthFunction.Less);
				framebuffer.DepthTextureId = textureId;
			}
			else
			{
				drawBuffers.Add((DrawBuffersEnum)fbAtt.AttachmentType);
				colorTextureIds.Add(tex.TextureId = textureId);
			}
		}
		framebuffer.ColorTextureIds = colorTextureIds.ToArray();
		GL.DrawBuffers(drawBuffers.Count, drawBuffers.ToArray());
		CheckFboStatus(FramebufferTarget.Framebuffer, fbAttrs.Name);
		CurrentFrameBuffer = null;
		GL.BindTexture(TextureTarget.Texture2D, 0);
		return framebuffer;
	}

	public List<FrameBufferRef> SetupDefaultFrameBuffers()
	{
		SetupSSAO = ClientSettings.SSAOQuality > 0;
		if (ClientSettings.IsNewSettingsFile && window.ClientSize.X > 1920)
		{
			ClientSettings.SSAA = 0.5f;
		}
		List<FrameBufferRef> framebuffers = new List<FrameBufferRef>(31);
		for (int l = 0; l <= 24; l++)
		{
			framebuffers.Add(null);
		}
		ShadowMapQuality = ClientSettings.ShadowMapQuality;
		ssaaLevel = ClientSettings.SSAA;
		int fullWidth = (int)((float)window.ClientSize.X * ssaaLevel);
		int fullHeight = (int)((float)window.ClientSize.Y * ssaaLevel);
		if (fullWidth == 0 || fullHeight == 0)
		{
			return framebuffers;
		}
		PixelFormat rgbaFormat = PixelFormat.Rgba;
		CheckGlError("sdfb-begin");
		FrameBufferRef frameBufferRef2 = (framebuffers[0] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = fullWidth,
			Height = fullHeight
		});
		FrameBufferRef frameBuffer = frameBufferRef2;
		frameBuffer.DepthTextureId = GL.GenTexture();
		if (frameBuffer.FboId == 0)
		{
			base.XPlatInterface.ShowMessageBox("Fatal error", "Unable to generate a new framebuffer. This shouldn't happen, ever. Maybe a restart resolves the problem?");
		}
		CurrentFrameBuffer = frameBuffer;
		GL.BindTexture(TextureTarget.Texture2D, frameBuffer.DepthTextureId);
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, fullWidth, fullHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9728);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9728);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33071);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33071);
		GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, frameBuffer.DepthTextureId, 0);
		GL.DepthFunc(DepthFunction.Less);
		frameBuffer.ColorTextureIds = ArrayUtil.CreateFilled(SetupSSAO ? 4 : 2, (int n) => GL.GenTexture());
		GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[0]);
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, fullWidth, fullHeight, 0, rgbaFormat, PixelType.UnsignedShort, IntPtr.Zero);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (ssaaLevel <= 1f) ? 9728 : 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (ssaaLevel <= 1f) ? 9728 : 9729);
		GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[0], 0);
		GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[1]);
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, fullWidth, fullHeight, 0, rgbaFormat, PixelType.UnsignedByte, IntPtr.Zero);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (ssaaLevel <= 1f) ? 9728 : 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (ssaaLevel <= 1f) ? 9728 : 9729);
		GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[1], 0);
		if (SetupSSAO)
		{
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[2]);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, fullWidth, fullHeight, 0, rgbaFormat, PixelType.Float, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[4] { 1f, 1f, 1f, 1f });
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33069);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33069);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[2], 0);
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[3]);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, fullWidth, fullHeight, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[4] { 1f, 1f, 1f, 1f });
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33069);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33069);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment3, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[3], 0);
			DrawBuffersEnum[] bufs2 = new DrawBuffersEnum[4]
			{
				DrawBuffersEnum.ColorAttachment0,
				DrawBuffersEnum.ColorAttachment1,
				DrawBuffersEnum.ColorAttachment2,
				DrawBuffersEnum.ColorAttachment3
			};
			GL.DrawBuffers(4, bufs2);
		}
		else
		{
			DrawBuffersEnum[] bufs = new DrawBuffersEnum[2]
			{
				DrawBuffersEnum.ColorAttachment0,
				DrawBuffersEnum.ColorAttachment1
			};
			GL.DrawBuffers(2, bufs);
		}
		CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.Primary);
		frameBufferRef2 = (framebuffers[1] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = fullWidth,
			Height = fullHeight
		});
		frameBuffer = frameBufferRef2;
		frameBuffer.ColorTextureIds = new int[3]
		{
			GL.GenTexture(),
			GL.GenTexture(),
			GL.GenTexture()
		};
		CurrentFrameBuffer = frameBuffer;
		GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[0]);
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, fullWidth, fullHeight, 0, rgbaFormat, PixelType.UnsignedShort, IntPtr.Zero);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
		GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[0], 0);
		GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[1]);
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16f, fullWidth, fullHeight, 0, PixelFormat.Red, PixelType.UnsignedShort, IntPtr.Zero);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
		GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[1], 0);
		GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[2]);
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, fullWidth, fullHeight, 0, rgbaFormat, PixelType.UnsignedByte, IntPtr.Zero);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
		GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[2], 0);
		GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, framebuffers[0].DepthTextureId, 0);
		DrawBuffersEnum[] bufs3 = new DrawBuffersEnum[3]
		{
			DrawBuffersEnum.ColorAttachment0,
			DrawBuffersEnum.ColorAttachment1,
			DrawBuffersEnum.ColorAttachment2
		};
		GL.DrawBuffers(3, bufs3);
		ClearFrameBuffer(EnumFrameBuffer.Transparent);
		CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.Transparent);
		if (SetupSSAO)
		{
			_ = ClientSettings.SSAOQuality;
			float ssaoSizeFq = 0.5f;
			FrameBufferRef obj = new FrameBufferRef
			{
				FboId = GL.GenFramebuffer(),
				Width = (int)((float)fullWidth * ssaoSizeFq),
				Height = (int)((float)fullHeight * ssaoSizeFq)
			};
			frameBufferRef2 = obj;
			framebuffers[13] = obj;
			frameBuffer = (CurrentFrameBuffer = frameBufferRef2);
			frameBuffer.ColorTextureIds = new int[2]
			{
				GL.GenTexture(),
				GL.GenTexture()
			};
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[0]);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, frameBuffer.Width, frameBuffer.Height, 0, PixelFormat.Rgb, PixelType.Float, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9728);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9728);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[0], 0);
			Random rand = new Random(5);
			_ = frameBuffer.ColorTextureIds[1];
			int size2 = 16;
			float[] vecs = new float[size2 * size2 * 3];
			Vec3f tmpvec = new Vec3f();
			for (int j = 0; j < size2 * size2; j++)
			{
				tmpvec.Set((float)rand.NextDouble() * 2f - 1f, (float)rand.NextDouble() * 2f - 1f, 0f).Normalize();
				vecs[j * 3] = tmpvec.X;
				vecs[j * 3 + 1] = tmpvec.Y;
				vecs[j * 3 + 2] = tmpvec.Z;
			}
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[1]);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, size2, size2, 0, PixelFormat.Rgb, PixelType.Float, vecs);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9728);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9728);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 10497);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 10497);
			for (int k = 0; k < 64; k++)
			{
				Vec3f vec = new Vec3f((float)rand.NextDouble() * 2f - 1f, (float)rand.NextDouble() * 2f - 1f, (float)rand.NextDouble());
				vec.Normalize();
				vec *= (float)rand.NextDouble();
				float scale = (float)k / 64f;
				scale = GameMath.Lerp(0.1f, 1f, scale * scale);
				vec *= scale;
				ssaoKernel[k * 3] = vec.X;
				ssaoKernel[k * 3 + 1] = vec.Y;
				ssaoKernel[k * 3 + 2] = vec.Z;
			}
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
			CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.SSAO);
			EnumFrameBuffer[] array = new EnumFrameBuffer[2]
			{
				EnumFrameBuffer.SSAOBlurVertical,
				EnumFrameBuffer.SSAOBlurHorizontal
			};
			foreach (EnumFrameBuffer val in array)
			{
				frameBufferRef2 = (framebuffers[(int)val] = new FrameBufferRef
				{
					FboId = GL.GenFramebuffer(),
					Width = (int)((float)fullWidth * ssaoSizeFq),
					Height = (int)((float)fullHeight * ssaoSizeFq)
				});
				frameBuffer = (CurrentFrameBuffer = frameBufferRef2);
				frameBuffer.ColorTextureIds = new int[1] { GL.GenTexture() };
				setupAttachment(frameBuffer, frameBuffer.Width, frameBuffer.Height, 0, rgbaFormat, PixelInternalFormat.Rgba8);
				GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
				CheckFboStatus(FramebufferTarget.Framebuffer, val);
			}
		}
		frameBufferRef2 = (framebuffers[2] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = fullWidth / 2,
			Height = fullHeight / 2
		});
		frameBuffer = (CurrentFrameBuffer = frameBufferRef2);
		frameBuffer.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBuffer, fullWidth / 2, fullHeight / 2, 0, rgbaFormat, PixelInternalFormat.Rgba8);
		GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
		CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.BlurHorizontalMedRes);
		frameBufferRef2 = (framebuffers[3] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = fullWidth / 2,
			Height = fullHeight / 2
		});
		frameBuffer = (CurrentFrameBuffer = frameBufferRef2);
		frameBuffer.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBuffer, fullWidth / 2, fullHeight / 2, 0, rgbaFormat, PixelInternalFormat.Rgba8);
		GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
		CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.BlurVerticalMedRes);
		frameBufferRef2 = (framebuffers[9] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = fullWidth / 4,
			Height = fullHeight / 4
		});
		frameBuffer = (CurrentFrameBuffer = frameBufferRef2);
		frameBuffer.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBuffer, fullWidth / 4, fullHeight / 4, 0, rgbaFormat, PixelInternalFormat.Rgba8);
		GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
		CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.BlurHorizontalLowRes);
		frameBufferRef2 = (framebuffers[8] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer()
		});
		frameBuffer = (CurrentFrameBuffer = frameBufferRef2);
		frameBuffer.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBuffer, fullWidth / 4, fullHeight / 4, 0, rgbaFormat, PixelInternalFormat.Rgba8);
		GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
		CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.BlurVerticalLowRes);
		frameBufferRef2 = (framebuffers[4] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = fullWidth,
			Height = fullHeight
		});
		frameBuffer = (CurrentFrameBuffer = frameBufferRef2);
		frameBuffer.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBuffer, fullWidth, fullHeight, 0, rgbaFormat, PixelInternalFormat.Rgba16f);
		GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
		CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.FindBright);
		frameBufferRef2 = (framebuffers[7] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = fullWidth / 2,
			Height = fullHeight / 2
		});
		frameBuffer = (CurrentFrameBuffer = frameBufferRef2);
		frameBuffer.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBuffer, fullWidth / 2, fullHeight / 2, 0, rgbaFormat, PixelInternalFormat.Rgba16f);
		GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
		CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.GodRays);
		frameBufferRef2 = (framebuffers[10] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = fullWidth,
			Height = fullHeight
		});
		frameBuffer = (CurrentFrameBuffer = frameBufferRef2);
		frameBuffer.ColorTextureIds = new int[1] { GL.GenTexture() };
		setupAttachment(frameBuffer, fullWidth, fullHeight, 0, rgbaFormat, PixelInternalFormat.Rgba16f);
		GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
		CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.Luma);
		frameBufferRef2 = (framebuffers[5] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = fullWidth / 4,
			Height = fullHeight / 4
		});
		frameBuffer = frameBufferRef2;
		frameBuffer.ColorTextureIds = new int[0];
		CheckGlError("sdfb-lide");
		CurrentFrameBuffer = frameBuffer;
		frameBuffer.DepthTextureId = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, frameBuffer.DepthTextureId);
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, frameBuffer.Width, frameBuffer.Height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 0);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33071);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33071);
		GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, frameBuffer.DepthTextureId, 0);
		GL.DepthFunc(DepthFunction.Less);
		GL.DrawBuffer(DrawBufferMode.None);
		GL.ReadBuffer(ReadBufferMode.None);
		CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.LiquidDepth);
		int shadowMapWidth = Math.Max(4, ShadowMapQuality + 2) * 1024;
		int shadowMapHeight = Math.Max(4, ShadowMapQuality + 2) * 1024;
		frameBufferRef2 = (framebuffers[11] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = shadowMapWidth,
			Height = shadowMapHeight
		});
		frameBuffer = frameBufferRef2;
		frameBuffer.ColorTextureIds = new int[0];
		CheckGlError("sdfb-fsm");
		if (ShadowMapQuality > 0)
		{
			CurrentFrameBuffer = frameBuffer;
			frameBuffer.DepthTextureId = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.DepthTextureId);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, shadowMapWidth, shadowMapHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 34894);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, 515);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[4] { 1f, 1f, 1f, 1f });
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33069);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33069);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, frameBuffer.DepthTextureId, 0);
			GL.DepthFunc(DepthFunction.Less);
			GL.DrawBuffer(DrawBufferMode.None);
			GL.ReadBuffer(ReadBufferMode.None);
			CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.ShadowmapFar);
		}
		frameBufferRef2 = (framebuffers[12] = new FrameBufferRef
		{
			FboId = GL.GenFramebuffer(),
			Width = shadowMapWidth,
			Height = shadowMapHeight
		});
		frameBuffer = frameBufferRef2;
		frameBuffer.ColorTextureIds = new int[0];
		CheckGlError("sdfb-nsm-before");
		if (ShadowMapQuality > 1)
		{
			CurrentFrameBuffer = frameBuffer;
			frameBuffer.DepthTextureId = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, frameBuffer.DepthTextureId);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, shadowMapWidth, shadowMapHeight, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, 34894);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, 515);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, new float[4] { 1f, 1f, 1f, 1f });
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33069);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33069);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, frameBuffer.DepthTextureId, 0);
			GL.DepthFunc(DepthFunction.Less);
			GL.DrawBuffer(DrawBufferMode.None);
			GL.ReadBuffer(ReadBufferMode.None);
			CheckFboStatus(FramebufferTarget.Framebuffer, EnumFrameBuffer.ShadowmapNear);
		}
		CheckGlError("sdfb-nsm-after");
		PixelPackBuffer = new GLBuffer[3];
		for (int i = 0; i < PixelPackBuffer.Length; i++)
		{
			PixelPackBuffer[i] = new GLBuffer
			{
				BufferId = GL.GenBuffer()
			};
			GL.BindBuffer(BufferTarget.PixelPackBuffer, PixelPackBuffer[i].BufferId);
			int size = 4 * sampleCount;
			GL.BufferData(BufferTarget.PixelPackBuffer, size, IntPtr.Zero, BufferUsageHint.StreamRead);
		}
		GL.BindBuffer(BufferTarget.PixelPackBuffer, 0);
		MeshData quad = QuadMeshUtil.GetCustomQuadModelData(-1f, -1f, 0f, 2f, 2f);
		quad.Normals = null;
		quad.Rgba = null;
		quad.Uv = null;
		if (screenQuad != null)
		{
			screenQuad.Dispose();
		}
		screenQuad = UploadMesh(quad);
		if (OffscreenBuffer)
		{
			CurrentFrameBuffer = framebuffers[0];
		}
		else
		{
			CurrentFrameBuffer = null;
			GL.DrawBuffer(DrawBufferMode.Back);
		}
		logger.Notification("(Re-)loaded frame buffers");
		return framebuffers;
	}

	private void setupAttachment(FrameBufferRef frameBuffer, int width, int height, int index, PixelFormat rgbaFormat, PixelInternalFormat dataFormat)
	{
		GL.BindTexture(TextureTarget.Texture2D, frameBuffer.ColorTextureIds[index]);
		GL.TexImage2D(TextureTarget.Texture2D, 0, dataFormat, width, height, 0, rgbaFormat, (dataFormat == PixelInternalFormat.Rgba16f) ? PixelType.UnsignedShort : PixelType.UnsignedByte, IntPtr.Zero);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
		GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, frameBuffer.ColorTextureIds[index], 0);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, Convert.ToInt32(TextureWrapMode.ClampToEdge));
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, Convert.ToInt32(TextureWrapMode.ClampToEdge));
	}

	public void DisposeFrameBuffers(List<FrameBufferRef> buffers)
	{
		for (int i = 0; i < buffers.Count; i++)
		{
			if (buffers[i] != null)
			{
				GL.DeleteFramebuffer(buffers[i].FboId);
				GL.DeleteTexture(buffers[i].DepthTextureId);
				for (int j = 0; j < buffers[i].ColorTextureIds.Length; j++)
				{
					GL.DeleteTexture(buffers[i].ColorTextureIds[j]);
				}
			}
		}
	}

	public override void ClearFrameBuffer(FrameBufferRef framebuffer, bool clearDepth = true)
	{
		ClearFrameBuffer(framebuffer, clearColor, clearDepth);
	}

	public override void ClearFrameBuffer(FrameBufferRef framebuffer, float[] clearColor, bool clearDepthBuffer = true, bool clearColorBuffers = true)
	{
		CurrentFrameBuffer = framebuffer;
		if (clearColorBuffers)
		{
			for (int i = 0; i < framebuffer.ColorTextureIds.Length; i++)
			{
				GL.ClearBuffer(ClearBuffer.Color, i, clearColor);
			}
		}
		if (clearDepthBuffer)
		{
			float clearval = 1f;
			GL.ClearBuffer(ClearBuffer.Depth, 0, ref clearval);
		}
	}

	public override void LoadFrameBuffer(FrameBufferRef frameBuffer, int textureId)
	{
		CurrentFrameBuffer = frameBuffer;
		GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, textureId, 0);
		GL.Viewport(0, 0, frameBuffer.Width, frameBuffer.Height);
	}

	public override void LoadFrameBuffer(FrameBufferRef frameBuffer)
	{
		CurrentFrameBuffer = frameBuffer;
		GL.Viewport(0, 0, frameBuffer.Width, frameBuffer.Height);
	}

	public override void UnloadFrameBuffer(FrameBufferRef frameBuffer)
	{
		LoadFrameBuffer(EnumFrameBuffer.Primary);
	}

	public override void ClearFrameBuffer(EnumFrameBuffer framebuffer)
	{
		switch (framebuffer)
		{
		case EnumFrameBuffer.Default:
			CurrentFrameBuffer = null;
			GL.DrawBuffer(DrawBufferMode.Back);
			GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
			CurrentFrameBuffer = frameBuffers[0];
			break;
		case EnumFrameBuffer.Primary:
		{
			GL.ClearBuffer(ClearBuffer.Color, 0, new float[4] { 0f, 0f, 0f, 1f });
			GL.ClearBuffer(ClearBuffer.Color, 1, new float[4] { 0f, 0f, 0f, 1f });
			if (RenderSSAO)
			{
				GL.ClearBuffer(ClearBuffer.Color, 2, new float[4] { 0f, 0f, 0f, 1f });
				GL.ClearBuffer(ClearBuffer.Color, 3, new float[4] { 0f, 0f, 0f, 1f });
			}
			float clearval2 = 1f;
			GL.ClearBuffer(ClearBuffer.Depth, 0, ref clearval2);
			break;
		}
		case EnumFrameBuffer.LiquidDepth:
		case EnumFrameBuffer.ShadowmapFar:
		case EnumFrameBuffer.ShadowmapNear:
		{
			FrameBufferRef fb = FrameBuffers[(int)framebuffer];
			float clearval = 1f;
			GL.Viewport(0, 0, fb.Width, fb.Height);
			GL.ClearBuffer(ClearBuffer.Depth, 0, ref clearval);
			break;
		}
		case EnumFrameBuffer.Transparent:
			GL.ClearBuffer(ClearBuffer.Color, 0, new float[4]);
			GL.ClearBuffer(ClearBuffer.Color, 1, new float[4] { 1f, 0f, 0f, 0f });
			GL.ClearBuffer(ClearBuffer.Color, 2, new float[4]);
			break;
		}
	}

	public override void LoadFrameBuffer(EnumFrameBuffer framebuffer)
	{
		switch (framebuffer)
		{
		case EnumFrameBuffer.Transparent:
		{
			CurrentFrameBuffer = frameBuffers[1];
			ScreenManager.FrameProfiler.Mark("rendTransp-fbbound");
			GlDisableCullFace();
			GlDepthMask(flag: false);
			GlEnableDepthTest();
			ScreenManager.FrameProfiler.Mark("rendTransp-dbset");
			DrawBuffersEnum[] bufs = new DrawBuffersEnum[3]
			{
				DrawBuffersEnum.ColorAttachment0,
				DrawBuffersEnum.ColorAttachment1,
				DrawBuffersEnum.ColorAttachment2
			};
			GL.DrawBuffers(3, bufs);
			GL.Enable(EnableCap.Blend);
			GL.BlendEquation(0, BlendEquationMode.FuncAdd);
			GL.BlendFunc(0, BlendingFactorSrc.One, BlendingFactorDest.One);
			GL.BlendEquation(1, BlendEquationMode.FuncAdd);
			GL.BlendFunc(1, BlendingFactorSrc.Zero, BlendingFactorDest.OneMinusSrcColor);
			GL.BlendEquation(2, BlendEquationMode.FuncAdd);
			GL.BlendFunc(2, BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			break;
		}
		case EnumFrameBuffer.Default:
			CurrentFrameBuffer = null;
			GL.Viewport(0, 0, window.ClientSize.X, window.ClientSize.Y);
			GL.DrawBuffer(DrawBufferMode.Back);
			break;
		case EnumFrameBuffer.BlurHorizontalMedRes:
		case EnumFrameBuffer.BlurVerticalMedRes:
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)window.ClientSize.X / 2f), (int)(ssaaLevel * (float)window.ClientSize.Y / 2f));
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.SSAOBlurVertical:
		case EnumFrameBuffer.SSAOBlurHorizontal:
		case EnumFrameBuffer.SSAOBlurVerticalHalfRes:
		case EnumFrameBuffer.SSAOBlurHorizontalHalfRes:
		{
			FrameBufferRef fb = frameBuffers[(int)framebuffer];
			GL.Viewport(0, 0, fb.Width, fb.Height);
			CurrentFrameBuffer = fb;
			break;
		}
		case EnumFrameBuffer.BlurVerticalLowRes:
		case EnumFrameBuffer.BlurHorizontalLowRes:
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)window.ClientSize.X / 4f), (int)(ssaaLevel * (float)window.ClientSize.Y / 4f));
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.FindBright:
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.GodRays:
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)window.ClientSize.X / 2f), (int)(ssaaLevel * (float)window.ClientSize.Y / 2f));
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.Luma:
			GL.Disable(EnableCap.Blend);
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.SSAO:
		{
			FrameBufferRef fb2 = frameBuffers[(int)framebuffer];
			GL.Viewport(0, 0, fb2.Width, fb2.Height);
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		}
		case EnumFrameBuffer.ShadowmapFar:
		case EnumFrameBuffer.ShadowmapNear:
			GlDepthMask(flag: true);
			GlEnableDepthTest();
			GlToggleBlend(on: true);
			GlEnableCullFace();
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.LiquidDepth:
			GlDepthMask(flag: true);
			GlEnableDepthTest();
			GlToggleBlend(on: true);
			GlEnableCullFace();
			CurrentFrameBuffer = frameBuffers[(int)framebuffer];
			break;
		case EnumFrameBuffer.Primary:
			if (OffscreenBuffer)
			{
				CurrentFrameBuffer = frameBuffers[0];
				GL.Viewport(0, 0, (int)(ssaaLevel * (float)window.ClientSize.X), (int)(ssaaLevel * (float)window.ClientSize.Y));
			}
			else
			{
				CurrentFrameBuffer = null;
				GL.DrawBuffer(DrawBufferMode.Back);
			}
			break;
		case (EnumFrameBuffer)6:
			break;
		}
	}

	public override void UnloadFrameBuffer(EnumFrameBuffer framebuffer)
	{
		if (framebuffer == EnumFrameBuffer.Transparent)
		{
			GlDepthMask(flag: true);
		}
		GL.Viewport(0, 0, (int)((float)window.ClientSize.X * ssaaLevel), (int)((float)window.ClientSize.Y * ssaaLevel));
		if (OffscreenBuffer)
		{
			CurrentFrameBuffer = frameBuffers[0];
			return;
		}
		CurrentFrameBuffer = null;
		GL.DrawBuffer(DrawBufferMode.Back);
	}

	public override void MergeTransparentRenderPass()
	{
		if (OffscreenBuffer)
		{
			CurrentFrameBuffer = frameBuffers[0];
		}
		else
		{
			CurrentFrameBuffer = null;
			GL.DrawBuffer(DrawBufferMode.Back);
		}
		GL.Disable(EnableCap.DepthTest);
		GL.Enable(EnableCap.Blend);
		GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
		ShaderProgramTransparentcompose transparentcompose = ShaderPrograms.Transparentcompose;
		transparentcompose.Use();
		transparentcompose.Revealage2D = frameBuffers[1].ColorTextureIds[1];
		transparentcompose.Accumulation2D = frameBuffers[1].ColorTextureIds[0];
		transparentcompose.InGlow2D = frameBuffers[1].ColorTextureIds[2];
		RenderFullscreenTriangle(screenQuad);
		transparentcompose.Stop();
	}

	public override void RenderPostprocessingEffects(float[] projectMatrix)
	{
		if (!OffscreenBuffer)
		{
			return;
		}
		int width = window.ClientSize.X;
		int height = window.ClientSize.Y;
		if (RenderBloom)
		{
			GlToggleBlend(on: false);
			LoadFrameBuffer(EnumFrameBuffer.FindBright);
			ShaderProgramFindbright findbright = ShaderPrograms.Findbright;
			findbright.Use();
			findbright.ColorTex2D = frameBuffers[0].ColorTextureIds[0];
			findbright.GlowTex2D = frameBuffers[0].ColorTextureIds[1];
			findbright.AmbientBloomLevel = ClientSettings.AmbientBloomLevel / 100f + ShaderUniforms.AmbientBloomLevelAdd[0] + ShaderUniforms.AmbientBloomLevelAdd[1] + ShaderUniforms.AmbientBloomLevelAdd[2] + ShaderUniforms.AmbientBloomLevelAdd[3];
			findbright.ExtraBloom = ShaderUniforms.ExtraBloom;
			RenderFullscreenTriangle(screenQuad);
			findbright.Stop();
			ShaderProgramBlur blur = ShaderPrograms.Blur;
			blur.Use();
			blur.FrameSize = new Vec2f((float)width * ssaaLevel, (float)height * ssaaLevel);
			LoadFrameBuffer(EnumFrameBuffer.BlurHorizontalMedRes);
			blur.IsVertical = 0;
			blur.InputTexture2D = frameBuffers[4].ColorTextureIds[0];
			RenderFullscreenTriangle(screenQuad);
			LoadFrameBuffer(EnumFrameBuffer.BlurVerticalMedRes);
			blur.IsVertical = 1;
			blur.InputTexture2D = frameBuffers[2].ColorTextureIds[0];
			RenderFullscreenTriangle(screenQuad);
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)window.ClientSize.X / 4f), (int)(ssaaLevel * (float)window.ClientSize.Y / 4f));
			LoadFrameBuffer(EnumFrameBuffer.BlurHorizontalLowRes);
			blur.IsVertical = 0;
			blur.InputTexture2D = frameBuffers[3].ColorTextureIds[0];
			RenderFullscreenTriangle(screenQuad);
			LoadFrameBuffer(EnumFrameBuffer.BlurVerticalLowRes);
			blur.IsVertical = 1;
			blur.InputTexture2D = frameBuffers[9].ColorTextureIds[0];
			RenderFullscreenTriangle(screenQuad);
			blur.Stop();
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)window.ClientSize.X), (int)(ssaaLevel * (float)window.ClientSize.Y));
			GlToggleBlend(on: true);
		}
		if (RenderGodRays)
		{
			LoadFrameBuffer(EnumFrameBuffer.GodRays);
			ShaderProgramGodrays godrays = ShaderPrograms.Godrays;
			godrays.Use();
			godrays.InvFrameSizeIn = new Vec2f(1f / ((float)width * ssaaLevel), 1f / ((float)height * ssaaLevel));
			godrays.SunPosScreenIn = ShaderUniforms.SunPositionScreen;
			godrays.SunPos3dIn = ShaderUniforms.LightPosition3D;
			godrays.PlayerViewVector = ShaderUniforms.PlayerViewVector;
			godrays.Dusk = ShaderUniforms.Dusk;
			godrays.IGlobalTimeIn = (float)EllapsedMs / 1000f;
			godrays.InputTexture2D = frameBuffers[0].ColorTextureIds[0];
			godrays.GlowParts2D = frameBuffers[0].ColorTextureIds[1];
			RenderFullscreenTriangle(screenQuad);
			godrays.Stop();
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)window.ClientSize.X), (int)(ssaaLevel * (float)window.ClientSize.Y));
		}
		if (RenderSSAO && projectMatrix != null)
		{
			GlToggleBlend(on: false);
			LoadFrameBuffer(EnumFrameBuffer.SSAO);
			GL.ClearBuffer(ClearBuffer.Color, 0, new float[4] { 1f, 1f, 1f, 1f });
			ShaderProgramSsao ssao = ShaderPrograms.Ssao;
			ssao.Use();
			ssao.GNormal2D = frameBuffers[0].ColorTextureIds[2];
			ssao.GPosition2D = frameBuffers[0].ColorTextureIds[3];
			ssao.TexNoise2D = frameBuffers[13].ColorTextureIds[1];
			float ssaoSizeFq = ((ssaaLevel == 1f) ? 0.5f : 1f);
			ssao.ScreenSize = new Vec2f(ssaaLevel * (float)width * ssaoSizeFq, ssaaLevel * (float)height * ssaoSizeFq);
			ssao.Revealage2D = frameBuffers[1].ColorTextureIds[1];
			ssao.Projection = projectMatrix;
			ssao.SamplesArray(64, ssaoKernel);
			RenderFullscreenTriangle(screenQuad);
			ssao.Stop();
			ShaderProgramBilateralblur progblur = ShaderPrograms.Bilateralblur;
			progblur.Use();
			int q = ((ClientSettings.SSAOQuality == 1) ? 1 : 3);
			for (int i = 0; i < q; i++)
			{
				FrameBufferRef fb = frameBuffers[15];
				LoadFrameBuffer(EnumFrameBuffer.SSAOBlurHorizontal);
				progblur.FrameSize = new Vec2f(fb.Width, fb.Height);
				progblur.IsVertical = 0;
				progblur.InputTexture2D = frameBuffers[(i == 0) ? 13 : 14].ColorTextureIds[0];
				progblur.DepthTexture2D = frameBuffers[0].DepthTextureId;
				RenderFullscreenTriangle(screenQuad);
				LoadFrameBuffer(EnumFrameBuffer.SSAOBlurVertical);
				progblur.IsVertical = 1;
				progblur.FrameSize = new Vec2f(fb.Width, fb.Height);
				progblur.InputTexture2D = frameBuffers[15].ColorTextureIds[0];
				RenderFullscreenTriangle(screenQuad);
			}
			progblur.Stop();
			GlToggleBlend(on: true);
			GL.Viewport(0, 0, (int)(ssaaLevel * (float)window.ClientSize.X), (int)(ssaaLevel * (float)window.ClientSize.Y));
		}
		if (RenderFXAA)
		{
			LoadFrameBuffer(EnumFrameBuffer.Luma);
			ShaderProgramLuma luma = ShaderPrograms.Luma;
			luma.Use();
			luma.Scene2D = frameBuffers[0].ColorTextureIds[0];
			RenderFullscreenTriangle(screenQuad);
			luma.Stop();
		}
		else
		{
			LoadFrameBuffer(EnumFrameBuffer.Luma);
			ShaderProgramBlit blit = ShaderPrograms.Blit;
			blit.Use();
			blit.Scene2D = frameBuffers[0].ColorTextureIds[0];
			RenderFullscreenTriangle(screenQuad);
			blit.Stop();
		}
		GL.Enable(EnableCap.Blend);
		LoadFrameBuffer(EnumFrameBuffer.Primary);
		ScreenManager.Platform.CheckGlError();
	}

	public override void RenderFinalComposition()
	{
		if (OffscreenBuffer)
		{
			int bloomPartsTexId = frameBuffers[8].ColorTextureIds[0];
			int godrayPartsTexId = frameBuffers[7].ColorTextureIds[0];
			int primarySceneTexId = frameBuffers[10].ColorTextureIds[0];
			if (RenderBloom)
			{
				_ = frameBuffers[8].ColorTextureIds[0];
			}
			DrawBuffersEnum[] bufs = new DrawBuffersEnum[1] { DrawBuffersEnum.ColorAttachment0 };
			GL.DrawBuffers(1, bufs);
			GL.Disable(EnableCap.DepthTest);
			GlToggleBlend(on: true);
			ShaderProgramFinal progf = ShaderPrograms.Final;
			progf.Use();
			progf.PrimaryScene2D = primarySceneTexId;
			progf.BloomParts2D = bloomPartsTexId;
			progf.GlowParts2D = frameBuffers[0].ColorTextureIds[1];
			progf.GodrayParts2D = godrayPartsTexId;
			progf.AmbientBloomLevel = ClientSettings.AmbientBloomLevel / 100f + ShaderUniforms.AmbientBloomLevelAdd[0] + ShaderUniforms.AmbientBloomLevelAdd[1] + ShaderUniforms.AmbientBloomLevelAdd[2] + ShaderUniforms.AmbientBloomLevelAdd[3];
			if (RenderSSAO)
			{
				progf.SsaoScene2D = frameBuffers[14].ColorTextureIds[0];
			}
			progf.InvFrameSizeIn = new Vec2f(1f / ((float)window.ClientSize.X * ssaaLevel), 1f / ((float)window.ClientSize.Y * ssaaLevel));
			progf.GammaLevel = ClientSettings.GammaLevel;
			progf.ExtraGamma = ClientSettings.ExtraGammaLevel;
			progf.ContrastLevel = ClientSettings.ExtraContrastLevel;
			progf.BrightnessLevel = ClientSettings.BrightnessLevel + Math.Max(0f, ShaderUniforms.DropShadowIntensity * 2f - 1.66f) / 3f;
			progf.SepiaLevel = ClientSettings.SepiaLevel + ShaderUniforms.ExtraSepia;
			progf.WindWaveCounter = ShaderUniforms.WindWaveCounter;
			progf.GlitchEffectStrength = ShaderUniforms.GlitchStrength;
			if (RenderGodRays)
			{
				progf.SunPosScreenIn = ShaderUniforms.SunPositionScreen;
				progf.SunPos3dIn = ShaderUniforms.SunPosition3D;
				progf.PlayerViewVector = ShaderUniforms.PlayerViewVector;
			}
			progf.DamageVignetting = ShaderUniforms.DamageVignetting;
			progf.DamageVignettingSide = ShaderUniforms.DamageVignettingSide;
			progf.FrostVignetting = ShaderUniforms.FrostVignetting;
			RenderFullscreenTriangle(screenQuad);
			progf.Stop();
			if (RenderSSAO)
			{
				bufs = new DrawBuffersEnum[4]
				{
					DrawBuffersEnum.ColorAttachment0,
					DrawBuffersEnum.ColorAttachment1,
					DrawBuffersEnum.ColorAttachment2,
					DrawBuffersEnum.ColorAttachment3
				};
				GL.DrawBuffers(4, bufs);
			}
			else
			{
				bufs = new DrawBuffersEnum[2]
				{
					DrawBuffersEnum.ColorAttachment0,
					DrawBuffersEnum.ColorAttachment1
				};
				GL.DrawBuffers(2, bufs);
			}
		}
	}

	private void DebugCallback(DebugSource source, DebugType type, int id, DebugSeverity severity, int length, nint message, nint userParam)
	{
		if (type != DebugType.DebugTypeOther)
		{
			string messageString = Marshal.PtrToStringAnsi(message, length);
			Logger.Notification("{0} {1} | {2}", severity, type, messageString);
			if (type == DebugType.DebugTypeError)
			{
				throw new Exception(messageString);
			}
		}
	}

	public override void BlitPrimaryToDefault()
	{
		if (OffscreenBuffer)
		{
			int textureId = frameBuffers[0].ColorTextureIds[0];
			LoadFrameBuffer(EnumFrameBuffer.Default);
			GL.Viewport(0, 0, window.ClientSize.X, window.ClientSize.Y);
			ShaderProgramBlit blit = ShaderPrograms.Blit;
			blit.Use();
			blit.Scene2D = textureId;
			RenderFullscreenTriangle(screenQuad);
			blit.Stop();
		}
	}

	private void CheckFboStatus(FramebufferTarget target, EnumFrameBuffer fbtype)
	{
		CheckFboStatus(target, fbtype.ToString() ?? "");
	}

	private void CheckFboStatus(FramebufferTarget target, string fbtype)
	{
		FramebufferErrorCode err = GL.Ext.CheckFramebufferStatus(target);
		switch (err)
		{
		case FramebufferErrorCode.FramebufferComplete:
			return;
		case FramebufferErrorCode.FramebufferIncompleteAttachment:
			throw new Exception("FBO " + fbtype + ": One or more attachment points are not framebuffer attachment complete. This could mean there’s no texture attached or the format isn’t renderable. For color textures this means the base format must be RGB or RGBA and for depth textures it must be a DEPTH_COMPONENT format. Other causes of this error are that the width or height is zero or the z-offset is out of range in case of render to volume.");
		case FramebufferErrorCode.FramebufferIncompleteMissingAttachment:
			throw new Exception("FBO " + fbtype + ": There are no attachments.");
		case FramebufferErrorCode.FramebufferIncompleteDimensionsExt:
			throw new Exception("FBO " + fbtype + ": Attachments are of different size. All attachments must have the same width and height.");
		case FramebufferErrorCode.FramebufferIncompleteFormatsExt:
			throw new Exception("FBO " + fbtype + ": The color attachments have different format. All color attachments must have the same format.");
		case FramebufferErrorCode.FramebufferIncompleteDrawBuffer:
			throw new Exception("FBO " + fbtype + ": An attachment point referenced by GL.DrawBuffers() doesn’t have an attachment.");
		case FramebufferErrorCode.FramebufferIncompleteReadBuffer:
			throw new Exception("FBO " + fbtype + ": The attachment point referenced by GL.ReadBuffers() doesn’t have an attachment.");
		case FramebufferErrorCode.FramebufferUnsupported:
			throw new Exception("FBO " + fbtype + ": This particular FBO configuration is not supported by the implementation.");
		}
		throw new Exception("FBO " + fbtype + ": Framebuffer unknown error (" + err.ToString() + ")");
	}

	public override void CheckGlError(string errmsg = null)
	{
		if (GlErrorChecking)
		{
			OpenTK.Graphics.OpenGL.ErrorCode err = GL.GetError();
			if (err != 0)
			{
				throw new Exception(string.Format("{0} - OpenGL threw an error: {1}", (errmsg == null) ? "" : (errmsg + " "), err));
			}
		}
	}

	public override void CheckGlErrorAlways(string errmsg = null)
	{
		OpenTK.Graphics.OpenGL.ErrorCode err = GL.GetError();
		if (err != 0)
		{
			string note = (ClientSettings.GlDebugMode ? "" : ". Enable Gl Debug Mode in the settings or clientsettings.json to track this error");
			string logmsg = string.Format("{0} - OpenGL threw an error: {1}{2}", (errmsg == null) ? "" : (errmsg + " "), err, note);
			Logger.Error(logmsg);
		}
		if (err == OpenTK.Graphics.OpenGL.ErrorCode.OutOfMemory)
		{
			throw new OutOfMemoryException("Either the graphics card or the OS ran out of memory! Please close other programs and reduce your view distance to prevent the game from crashing.");
		}
	}

	public override string GlGetError()
	{
		OpenTK.Graphics.OpenGL.ErrorCode err = GL.GetError();
		if (err != 0)
		{
			return err.ToString();
		}
		return null;
	}

	public override string GetGLShaderVersionString()
	{
		return GL.GetString(StringName.ShadingLanguageVersion);
	}

	public override int GenSampler(bool linear)
	{
		int num = GL.GenSampler();
		GL.SamplerParameter(num, SamplerParameterName.TextureMagFilter, linear ? 9729 : 9728);
		GL.SamplerParameter(num, SamplerParameterName.TextureMinFilter, 9986);
		return num;
	}

	public override void GLWireframes(bool toggle)
	{
		GL.PolygonMode(MaterialFace.FrontAndBack, toggle ? PolygonMode.Line : PolygonMode.Fill);
	}

	public override void GlViewport(int x, int y, int width, int height)
	{
		GL.Viewport(x, y, width, height);
	}

	public override void GlScissor(int x, int y, int width, int height)
	{
		GL.Scissor(x, y, width, height);
	}

	public override void GlScissorFlag(bool enable)
	{
		if (enable)
		{
			GL.Enable(EnableCap.ScissorTest);
		}
		else
		{
			GL.Disable(EnableCap.ScissorTest);
		}
	}

	public override void GlEnableDepthTest()
	{
		GL.Enable(EnableCap.DepthTest);
	}

	public override void GlDisableDepthTest()
	{
		GL.Disable(EnableCap.DepthTest);
	}

	public override void BindTexture2d(int texture)
	{
		GL.ActiveTexture(TextureUnit.Texture0);
		GL.BindTexture(TextureTarget.Texture2D, texture);
	}

	public override void BindTextureCubeMap(int texture)
	{
		GL.BindTexture(TextureTarget.TextureCubeMap, texture);
	}

	public override void UnBindTextureCubeMap()
	{
		GL.BindTexture(TextureTarget.TextureCubeMap, 0);
	}

	public override void GlToggleBlend(bool on, EnumBlendMode blendMode = EnumBlendMode.Standard)
	{
		if (on)
		{
			GL.Enable(EnableCap.Blend);
			switch (blendMode)
			{
			case EnumBlendMode.Brighten:
				GL.BlendFunc(BlendingFactor.DstColor, BlendingFactor.One);
				return;
			case EnumBlendMode.Multiply:
				GL.BlendFuncSeparate(BlendingFactorSrc.Zero, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
				return;
			case EnumBlendMode.PremultipliedAlpha:
				GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
				return;
			case EnumBlendMode.Glow:
				GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.One, BlendingFactorSrc.One, BlendingFactorDest.Zero);
				return;
			case EnumBlendMode.Overlay:
				GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.One);
				return;
			}
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			if (RenderSSAO)
			{
				GL.BlendEquation(2, BlendEquationMode.FuncAdd);
				GL.BlendFunc(2, BlendingFactorSrc.One, BlendingFactorDest.Zero);
				GL.BlendEquation(3, BlendEquationMode.FuncAdd);
				GL.BlendFunc(3, BlendingFactorSrc.One, BlendingFactorDest.Zero);
			}
		}
		else
		{
			GL.Disable(EnableCap.Blend);
		}
	}

	public override void GlDisableCullFace()
	{
		GL.Disable(EnableCap.CullFace);
	}

	public override void GlEnableCullFace()
	{
		GL.Enable(EnableCap.CullFace);
	}

	public override void GlClearColorRgbaf(float r, float g, float b, float a)
	{
		GL.ClearColor(r, g, b, a);
	}

	public override void GLLineWidth(float width)
	{
		if (RuntimeEnv.OS != OS.Mac)
		{
			GL.LineWidth(width);
		}
	}

	public override void SmoothLines(bool on)
	{
		if (RuntimeEnv.OS != OS.Mac)
		{
			if (on)
			{
				GL.Enable(EnableCap.LineSmooth);
				GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
			}
			else
			{
				GL.Disable(EnableCap.LineSmooth);
				GL.Hint(HintTarget.LineSmoothHint, HintMode.DontCare);
			}
		}
	}

	public override void GlDepthMask(bool flag)
	{
		GL.DepthMask(flag);
	}

	public override void GlDepthFunc(EnumDepthFunction depthFunc)
	{
		GL.DepthFunc((DepthFunction)depthFunc);
	}

	public override void GlCullFaceBack()
	{
		GL.CullFace(CullFaceMode.Back);
	}

	public override void GlGenerateTex2DMipmaps()
	{
		GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
	}

	public override int LoadCairoTexture(ImageSurface surface, bool linearMag)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Texture uploads must happen in the main thread. We only have one OpenGL context.");
		}
		int id = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, id);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, linearMag ? 9729 : 9728);
		GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, surface.Width, surface.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, surface.DataPtr);
		return id;
	}

	public override void LoadOrUpdateCairoTexture(ImageSurface surface, bool linearMag, ref LoadedTexture intoTexture)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Texture uploads must happen in the main thread. We only have one OpenGL context.");
		}
		if (intoTexture.TextureId == 0 || intoTexture.Width != surface.Width || intoTexture.Height != surface.Height)
		{
			if (intoTexture.TextureId != 0)
			{
				GL.DeleteTexture(intoTexture.TextureId);
			}
			intoTexture.TextureId = GL.GenTexture();
			intoTexture.Width = surface.Width;
			intoTexture.Height = surface.Height;
			GL.BindTexture(TextureTarget.Texture2D, intoTexture.TextureId);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, linearMag ? 9729 : 9728);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, surface.Width, surface.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, surface.DataPtr);
		}
		else
		{
			GL.BindTexture(TextureTarget.Texture2D, intoTexture.TextureId);
			GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, surface.Width, surface.Height, PixelFormat.Bgra, PixelType.UnsignedByte, surface.DataPtr);
		}
		CheckGlError("LoadOrUpdateCairoTexture");
	}

	public override int LoadTexture(SKBitmap bmp, bool linearMag = false, int clampMode = 0, bool generateMipmaps = false)
	{
		return LoadTexture(new BitmapExternal
		{
			bmp = bmp
		}, linearMag, clampMode, generateMipmaps);
	}

	public override void LoadIntoTexture(IBitmap srcBmp, int targetTextureId, int destX, int destY, bool generateMipmaps = false)
	{
		GL.BindTexture(TextureTarget.Texture2D, targetTextureId);
		if (srcBmp is BitmapExternal bmpExt)
		{
			GL.TexSubImage2D(TextureTarget.Texture2D, 0, destX, destY, srcBmp.Width, srcBmp.Height, PixelFormat.Bgra, PixelType.UnsignedByte, bmpExt.PixelsPtrAndLock);
		}
		else
		{
			GL.TexSubImage2D(TextureTarget.Texture2D, 0, destX, destY, srcBmp.Width, srcBmp.Height, PixelFormat.Bgra, PixelType.UnsignedByte, srcBmp.Pixels);
		}
		if (ENABLE_MIPMAPS && generateMipmaps)
		{
			BuildMipMaps(targetTextureId);
		}
	}

	public override int LoadTexture(IBitmap bmp, bool linearMag = false, int clampMode = 0, bool generateMipmaps = false)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Texture uploads must happen in the main thread. We only have one OpenGL context.");
		}
		int id = GL.GenTexture();
		GL.BindTexture(TextureTarget.Texture2D, id);
		if (ENABLE_ANISOTROPICFILTERING)
		{
			float maxAniso = GL.GetFloat(GetPName.MaxTextureMaxAnisotropy);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxAnisotropy, maxAniso);
		}
		switch (clampMode)
		{
		case 1:
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33071);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33071);
			break;
		case 2:
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 10497);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 10497);
			break;
		}
		if (bmp is BitmapExternal bitmapExternal)
		{
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bitmapExternal.PixelsPtrAndLock);
		}
		else
		{
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmp.Pixels);
		}
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
		GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, linearMag ? 9729 : 9728);
		if (ENABLE_MIPMAPS && generateMipmaps)
		{
			BuildMipMaps(id);
		}
		return id;
	}

	public override void LoadOrUpdateTextureFromBgra_DeferMipMap(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture)
	{
		PixelFormat format = PixelFormat.Bgra;
		LoadOrUpdateTextureFromPixels(rgbaPixels, linearMag, clampMode, ref intoTexture, format, makeMipMap: false);
	}

	public override void LoadOrUpdateTextureFromBgra(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture)
	{
		PixelFormat format = PixelFormat.Bgra;
		LoadOrUpdateTextureFromPixels(rgbaPixels, linearMag, clampMode, ref intoTexture, format, makeMipMap: true);
	}

	public override void LoadOrUpdateTextureFromRgba(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture)
	{
		PixelFormat format = PixelFormat.Rgba;
		LoadOrUpdateTextureFromPixels(rgbaPixels, linearMag, clampMode, ref intoTexture, format, makeMipMap: true);
	}

	private void LoadOrUpdateTextureFromPixels(int[] rgbaPixels, bool linearMag, int clampMode, ref LoadedTexture intoTexture, PixelFormat format, bool makeMipMap)
	{
		if (Environment.CurrentManagedThreadId != RuntimeEnv.MainThreadId)
		{
			throw new InvalidOperationException("Texture uploads must happen in the main thread. We only have one OpenGL context.");
		}
		if (intoTexture.TextureId == 0 || intoTexture.Width * intoTexture.Height != rgbaPixels.Length)
		{
			if (intoTexture.TextureId != 0)
			{
				GL.DeleteTexture(intoTexture.TextureId);
			}
			intoTexture.TextureId = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, intoTexture.TextureId);
			if (ENABLE_ANISOTROPICFILTERING)
			{
				float maxAniso = GL.GetFloat(GetPName.MaxTextureMaxAnisotropy);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxAnisotropy, maxAniso);
			}
			if (clampMode == 1)
			{
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, 33071);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, 33071);
			}
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, intoTexture.Width, intoTexture.Height, 0, format, PixelType.UnsignedByte, rgbaPixels);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, linearMag ? 9729 : 9728);
			if (makeMipMap)
			{
				BuildMipMaps(intoTexture.TextureId);
			}
		}
		else
		{
			GL.BindTexture(TextureTarget.Texture2D, intoTexture.TextureId);
			GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, intoTexture.Width, intoTexture.Height, format, PixelType.UnsignedByte, rgbaPixels);
		}
	}

	public override void BuildMipMaps(int textureId)
	{
		if (ENABLE_MIPMAPS)
		{
			GL.BindTexture(TextureTarget.Texture2D, textureId);
			GL.GetTexParameter(TextureTarget.Texture2D, GetTextureParameter.TextureMaxLevel, out int MipMapCount);
			if (MipMapCount > 0)
			{
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9986);
				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureLodBias, 0f);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, ClientSettings.MipMapLevel);
			}
		}
	}

	public override int Load3DTextureCube(BitmapRef[] bmps)
	{
		GL.ActiveTexture(TextureUnit.Texture0);
		int textureId = GL.GenTexture();
		GL.BindTexture(TextureTarget.TextureCubeMap, textureId);
		for (int i = 0; i < 6; i++)
		{
			Load3DTextureSide((BitmapExternal)bmps[i], (TextureTarget)(34069 + i));
		}
		GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, 9729);
		GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, 9729);
		GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapR, Convert.ToInt32(TextureWrapMode.ClampToEdge));
		GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapS, Convert.ToInt32(TextureWrapMode.ClampToEdge));
		GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureWrapT, Convert.ToInt32(TextureWrapMode.ClampToEdge));
		return textureId;
	}

	private void Load3DTextureSide(BitmapExternal bmp, TextureTarget target)
	{
		GL.TexImage2D(target, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, bmp.PixelsPtrAndLock);
	}

	public override void GLDeleteTexture(int id)
	{
		GL.DeleteTexture(id);
	}

	public override int GlGetMaxTextureSize()
	{
		int size = 1024;
		try
		{
			GL.GetInteger(GetPName.MaxTextureSize, out size);
		}
		catch
		{
		}
		return size;
	}

	public override UBORef CreateUBO(int shaderProgramId, int bindingPoint, string blockName, int size)
	{
		int handle = GL.GenBuffer();
		GL.BindBuffer(BufferTarget.UniformBuffer, handle);
		GL.BufferData(BufferTarget.UniformBuffer, size, IntPtr.Zero, BufferUsageHint.DynamicDraw);
		ScreenManager.Platform.CheckGlError();
		int uboIndex = GL.GetUniformBlockIndex(shaderProgramId, blockName);
		ScreenManager.Platform.CheckGlError();
		GL.UniformBlockBinding(shaderProgramId, uboIndex, bindingPoint);
		GL.BindBufferBase(BufferRangeTarget.UniformBuffer, bindingPoint, handle);
		ScreenManager.Platform.CheckGlError();
		UBO uBO = new UBO();
		uBO.Handle = handle;
		uBO.Size = size;
		uBO.Unbind();
		ScreenManager.Platform.CheckGlError();
		return uBO;
	}

	public unsafe override void UpdateMesh(MeshRef modelRef, MeshData data)
	{
		VAO vao = (VAO)modelRef;
		BufferAccessMask flags = BufferAccessMask.MapWriteBit | BufferAccessMask.MapUnsynchronizedBit;
		if (vao.Persistent)
		{
			flags |= BufferAccessMask.MapFlushExplicitBit | BufferAccessMask.MapPersistentBit;
		}
		bool pers = vao.Persistent;
		if (data.xyz != null)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vao.xyzVboId);
			if (pers)
			{
				float* ptr9 = (float*)vao.xyzPtr;
				ptr9 += data.XyzOffset / 4;
				int cnt9 = data.XyzCount;
				for (int i5 = 0; i5 < cnt9; i5++)
				{
					*(ptr9++) = data.xyz[i5];
				}
			}
			else
			{
				GL.BufferSubData(BufferTarget.ArrayBuffer, data.XyzOffset, 4 * data.XyzCount, data.xyz);
			}
		}
		if (data.Normals != null && data.VerticesCount > 0)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vao.normalsVboId);
			if (pers)
			{
				int* ptr10 = (int*)vao.normalsPtr;
				ptr10 += data.NormalsOffset / 4;
				int cnt10 = data.VerticesCount;
				for (int i4 = 0; i4 < cnt10; i4++)
				{
					*(ptr10++) = data.Normals[i4];
				}
			}
			else
			{
				GL.BufferSubData(BufferTarget.ArrayBuffer, data.NormalsOffset, 4 * data.VerticesCount, data.Normals);
			}
		}
		if (data.Uv != null && data.UvCount > 0)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vao.uvVboId);
			if (pers)
			{
				float* ptr8 = (float*)vao.uvPtr;
				ptr8 += data.UvOffset / 4;
				int cnt8 = data.UvCount;
				for (int i3 = 0; i3 < cnt8; i3++)
				{
					*(ptr8++) = data.Uv[i3];
				}
			}
			else
			{
				GL.BufferSubData(BufferTarget.ArrayBuffer, data.UvOffset, 4 * data.UvCount, data.Uv);
			}
		}
		if (data.Rgba != null && data.RgbaCount > 0)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vao.rgbaVboId);
			if (pers)
			{
				byte* ptr7 = (byte*)vao.rgbaPtr;
				ptr7 += data.RgbaOffset / 1;
				int cnt7 = data.RgbaCount;
				for (int i2 = 0; i2 < cnt7; i2++)
				{
					*(ptr7++) = data.Rgba[i2];
				}
			}
			else
			{
				GL.BufferSubData(BufferTarget.ArrayBuffer, data.RgbaOffset, data.RgbaCount, data.Rgba);
			}
		}
		if (data.Flags != null && data.FlagsCount > 0)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vao.flagsVboId);
			if (pers)
			{
				int* ptr6 = (int*)vao.flagsPtr;
				ptr6 += data.FlagsOffset / 4;
				int cnt6 = data.FlagsCount;
				for (int n = 0; n < cnt6; n++)
				{
					*(ptr6++) = data.Flags[n];
				}
			}
			else
			{
				GL.BufferSubData(BufferTarget.ArrayBuffer, data.FlagsOffset, 4 * data.FlagsCount, data.Flags);
			}
		}
		if (data.CustomFloats != null && data.CustomFloats.Count > 0)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vao.customDataFloatVboId);
			if (pers)
			{
				float* ptr5 = (float*)vao.customDataFloatPtr;
				ptr5 += data.CustomFloats.BaseOffset / 4;
				int cnt5 = data.CustomFloats.Count;
				for (int m = 0; m < cnt5; m++)
				{
					*(ptr5++) = data.CustomFloats.Values[m];
				}
			}
			else
			{
				GL.BufferSubData(BufferTarget.ArrayBuffer, data.CustomFloats.BaseOffset, 4 * data.CustomFloats.Count, data.CustomFloats.Values);
			}
		}
		if (data.CustomShorts != null && data.CustomShorts.Count > 0)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vao.customDataShortVboId);
			if (pers)
			{
				short* ptr4 = (short*)vao.customDataShortPtr;
				ptr4 += data.CustomShorts.BaseOffset / 2;
				int cnt4 = data.CustomShorts.Count;
				for (int l = 0; l < cnt4; l++)
				{
					*(ptr4++) = data.CustomShorts.Values[l];
				}
			}
			else
			{
				GL.BufferSubData(BufferTarget.ArrayBuffer, data.CustomShorts.BaseOffset, 2 * data.CustomShorts.Count, data.CustomShorts.Values);
			}
		}
		if (data.CustomInts != null && data.CustomInts.Count > 0)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vao.customDataIntVboId);
			if (pers)
			{
				int* ptr3 = (int*)vao.customDataIntPtr;
				ptr3 += data.CustomInts.BaseOffset / 4;
				int cnt3 = data.CustomInts.Count;
				for (int k = 0; k < cnt3; k++)
				{
					*(ptr3++) = data.CustomInts.Values[k];
				}
			}
			else
			{
				GL.BufferSubData(BufferTarget.ArrayBuffer, data.CustomInts.BaseOffset, 4 * data.CustomInts.Count, data.CustomInts.Values);
			}
		}
		if (data.CustomBytes != null && data.CustomBytes.Count > 0)
		{
			GL.BindBuffer(BufferTarget.ArrayBuffer, vao.customDataByteVboId);
			if (pers)
			{
				byte* ptr2 = (byte*)vao.customDataBytePtr;
				ptr2 += data.CustomBytes.BaseOffset / 1;
				int cnt2 = data.CustomBytes.Count;
				for (int j = 0; j < cnt2; j++)
				{
					*(ptr2++) = data.CustomBytes.Values[j];
				}
			}
			else
			{
				GL.BufferSubData(BufferTarget.ArrayBuffer, data.CustomBytes.BaseOffset, data.CustomBytes.Count, data.CustomBytes.Values);
			}
		}
		GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		if (data.Indices != null)
		{
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, vao.vboIdIndex);
			if (pers)
			{
				int* ptr = (int*)vao.indicesPtr;
				ptr += data.IndicesOffset / 4;
				int cnt = data.IndicesCount;
				for (int i = 0; i < cnt; i++)
				{
					*(ptr++) = data.Indices[i];
				}
			}
			else
			{
				GL.BufferSubData(BufferTarget.ElementArrayBuffer, data.IndicesOffset, 4 * data.IndicesCount, data.Indices);
			}
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			vao.IndicesCount = data.IndicesCount;
		}
		if (GlErrorChecking && GlDebugMode)
		{
			CheckGlError($"Error when trying to update vao indices, modeldata xyz/rgba/uv/indices sizes: {data.XyzCount}/{data.RgbaCount}/{data.UvCount}/{data.IndicesCount}");
		}
	}

	public override MeshRef AllocateEmptyMesh(int xyzSize, int normalsSize, int uvSize, int rgbaSize, int flagsSize, int indicesSize, CustomMeshDataPartFloat customFloats, CustomMeshDataPartShort customShorts, CustomMeshDataPartByte customBytes, CustomMeshDataPartInt customInts, EnumDrawMode drawMode = EnumDrawMode.Triangles, bool staticDraw = true)
	{
		VAO vao = new VAO();
		int vaoId = GL.GenVertexArray();
		int vaoSlotNumber = 0;
		GL.BindVertexArray(vaoId);
		int xyzVboId = 0;
		int normalsVboId = 0;
		int uvVboId = 0;
		int rgbaVboId = 0;
		int customDataFloatsVboId = 0;
		int customDataBytesVboId = 0;
		int customDataIntsVboId = 0;
		int customDataShortsVboId = 0;
		int flagsVboId = 0;
		BufferUsageHint usageHint = (staticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
		bool doPStorage = supportsPersistentMapping && !staticDraw;
		doPStorage = false;
		BufferStorageFlags flags = (BufferStorageFlags)450;
		BufferAccessMask mapflags = BufferAccessMask.MapWriteBit | BufferAccessMask.MapPersistentBit | BufferAccessMask.MapCoherentBit;
		if (xyzSize > 0)
		{
			xyzVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, xyzVboId);
			if (doPStorage)
			{
				GL.BufferStorage(BufferTarget.ArrayBuffer, xyzSize, IntPtr.Zero, flags);
				vao.xyzPtr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, xyzSize, mapflags);
				CheckGlError("Failed loading model");
			}
			else
			{
				GL.BufferData(BufferTarget.ArrayBuffer, xyzSize, IntPtr.Zero, usageHint);
			}
			GL.VertexAttribPointer(vaoSlotNumber, 3, VertexAttribPointerType.Float, normalized: false, 0, 0);
			vaoSlotNumber++;
		}
		CheckGlError("Failed loading model");
		if (normalsSize > 0)
		{
			normalsVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, normalsVboId);
			if (doPStorage)
			{
				GL.BufferStorage(BufferTarget.ArrayBuffer, normalsSize, IntPtr.Zero, flags);
				vao.normalsPtr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, normalsSize, mapflags);
			}
			else
			{
				GL.BufferData(BufferTarget.ArrayBuffer, normalsSize, IntPtr.Zero, usageHint);
			}
			GL.VertexAttribPointer(vaoSlotNumber, 4, VertexAttribPointerType.Int2101010Rev, normalized: true, 0, 0);
			vaoSlotNumber++;
		}
		if (uvSize > 0)
		{
			uvVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, uvVboId);
			if (doPStorage)
			{
				GL.BufferStorage(BufferTarget.ArrayBuffer, uvSize, IntPtr.Zero, flags);
				vao.uvPtr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, uvSize, mapflags);
			}
			else
			{
				GL.BufferData(BufferTarget.ArrayBuffer, uvSize, IntPtr.Zero, usageHint);
			}
			GL.VertexAttribPointer(vaoSlotNumber, 2, VertexAttribPointerType.Float, normalized: false, 0, 0);
			vaoSlotNumber++;
		}
		if (rgbaSize > 0)
		{
			rgbaVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, rgbaVboId);
			if (doPStorage)
			{
				GL.BufferStorage(BufferTarget.ArrayBuffer, rgbaSize, IntPtr.Zero, flags);
				vao.rgbaPtr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, rgbaSize, mapflags);
			}
			else
			{
				GL.BufferData(BufferTarget.ArrayBuffer, rgbaSize, IntPtr.Zero, usageHint);
			}
			GL.VertexAttribPointer(vaoSlotNumber, 4, VertexAttribPointerType.UnsignedByte, normalized: true, 0, 0);
			vaoSlotNumber++;
		}
		if (flagsSize > 0)
		{
			flagsVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, flagsVboId);
			if (doPStorage)
			{
				GL.BufferStorage(BufferTarget.ArrayBuffer, flagsSize, IntPtr.Zero, flags);
				vao.flagsPtr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, flagsSize, mapflags);
			}
			else
			{
				GL.BufferData(BufferTarget.ArrayBuffer, flagsSize, IntPtr.Zero, usageHint);
			}
			GL.VertexAttribIPointer(vaoSlotNumber, 1, VertexAttribIntegerType.UnsignedInt, 0, IntPtr.Zero);
			vaoSlotNumber++;
		}
		if (customFloats != null)
		{
			customDataFloatsVboId = GL.GenBuffer();
			BufferUsageHint hint4 = (customFloats.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, customDataFloatsVboId);
			if (doPStorage)
			{
				GL.BufferStorage(BufferTarget.ArrayBuffer, 4 * customFloats.AllocationSize, IntPtr.Zero, flags);
				vao.customDataFloatPtr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, 4 * customFloats.AllocationSize, mapflags);
			}
			else
			{
				GL.BufferData(BufferTarget.ArrayBuffer, 4 * customFloats.AllocationSize, IntPtr.Zero, hint4);
			}
			for (int l = 0; l < customFloats.InterleaveSizes.Length; l++)
			{
				GL.VertexAttribPointer(vaoSlotNumber, customFloats.InterleaveSizes[l], VertexAttribPointerType.Float, normalized: false, customFloats.InterleaveStride, customFloats.InterleaveOffsets[l]);
				if (customFloats.Instanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
		}
		if (customShorts != null)
		{
			customDataShortsVboId = GL.GenBuffer();
			BufferUsageHint hint3 = (customShorts.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, customDataShortsVboId);
			if (doPStorage)
			{
				GL.BufferStorage(BufferTarget.ArrayBuffer, 2 * customShorts.AllocationSize, IntPtr.Zero, flags);
				vao.customDataShortPtr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, 2 * customShorts.AllocationSize, mapflags);
			}
			else
			{
				GL.BufferData(BufferTarget.ArrayBuffer, 2 * customShorts.AllocationSize, IntPtr.Zero, hint3);
			}
			for (int k = 0; k < customShorts.InterleaveSizes.Length; k++)
			{
				if (customShorts.Conversion == DataConversion.Integer)
				{
					GL.VertexAttribIPointer(vaoSlotNumber, customShorts.InterleaveSizes[k], VertexAttribIntegerType.Short, customShorts.InterleaveStride, customShorts.InterleaveOffsets[k]);
				}
				else
				{
					GL.VertexAttribPointer(vaoSlotNumber, customShorts.InterleaveSizes[k], VertexAttribPointerType.Short, customShorts.Conversion == DataConversion.NormalizedFloat, customShorts.InterleaveStride, customShorts.InterleaveOffsets[k]);
				}
				if (customShorts.Instanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
		}
		if (customInts != null)
		{
			customDataIntsVboId = GL.GenBuffer();
			BufferUsageHint hint2 = (customInts.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, customDataIntsVboId);
			if (doPStorage)
			{
				GL.BufferStorage(BufferTarget.ArrayBuffer, 4 * customInts.AllocationSize, IntPtr.Zero, flags);
				vao.customDataIntPtr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, 4 * customInts.AllocationSize, mapflags);
			}
			else
			{
				GL.BufferData(BufferTarget.ArrayBuffer, 4 * customInts.AllocationSize, IntPtr.Zero, hint2);
			}
			for (int j = 0; j < customInts.InterleaveSizes.Length; j++)
			{
				if (customInts.Conversion == DataConversion.Integer)
				{
					GL.VertexAttribIPointer(vaoSlotNumber, customInts.InterleaveSizes[j], VertexAttribIntegerType.UnsignedInt, customInts.InterleaveStride, customInts.InterleaveOffsets[j]);
				}
				else
				{
					GL.VertexAttribPointer(vaoSlotNumber, customInts.InterleaveSizes[j], VertexAttribPointerType.UnsignedInt, customInts.Conversion == DataConversion.NormalizedFloat, customInts.InterleaveStride, customBytes.InterleaveOffsets[j]);
				}
				if (customInts.Instanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
		}
		if (customBytes != null)
		{
			customDataBytesVboId = GL.GenBuffer();
			BufferUsageHint hint = (customBytes.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, customDataBytesVboId);
			if (doPStorage)
			{
				GL.BufferStorage(BufferTarget.ArrayBuffer, customBytes.AllocationSize, IntPtr.Zero, flags);
				vao.customDataBytePtr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, customBytes.AllocationSize, mapflags);
			}
			else
			{
				GL.BufferData(BufferTarget.ArrayBuffer, customBytes.AllocationSize, IntPtr.Zero, hint);
			}
			for (int i = 0; i < customBytes.InterleaveSizes.Length; i++)
			{
				if (customBytes.Conversion == DataConversion.Integer)
				{
					GL.VertexAttribIPointer(vaoSlotNumber, customBytes.InterleaveSizes[i], VertexAttribIntegerType.UnsignedByte, customBytes.InterleaveStride, customBytes.InterleaveOffsets[i]);
				}
				else
				{
					GL.VertexAttribPointer(vaoSlotNumber, customBytes.InterleaveSizes[i], VertexAttribPointerType.UnsignedByte, customBytes.Conversion == DataConversion.NormalizedFloat, customBytes.InterleaveStride, customBytes.InterleaveOffsets[i]);
				}
				if (customBytes.Instanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
		}
		GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		int vboIdIndex = GL.GenBuffer();
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboIdIndex);
		if (doPStorage)
		{
			GL.BufferStorage(BufferTarget.ElementArrayBuffer, indicesSize, IntPtr.Zero, flags);
			vao.indicesPtr = GL.MapBufferRange(BufferTarget.ElementArrayBuffer, IntPtr.Zero, indicesSize, mapflags);
		}
		else
		{
			GL.BufferData(BufferTarget.ElementArrayBuffer, indicesSize, IntPtr.Zero, BufferUsageHint.StaticDraw);
		}
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		GL.BindVertexArray(0);
		CheckGlError("Failed loading model");
		vao.Persistent = doPStorage;
		vao.VaoId = vaoId;
		vao.IndicesCount = indicesSize;
		vao.vaoSlotNumber = vaoSlotNumber;
		vao.vboIdIndex = vboIdIndex;
		vao.normalsVboId = normalsVboId;
		vao.xyzVboId = xyzVboId;
		vao.uvVboId = uvVboId;
		vao.rgbaVboId = rgbaVboId;
		vao.customDataFloatVboId = customDataFloatsVboId;
		vao.customDataByteVboId = customDataBytesVboId;
		vao.customDataIntVboId = customDataIntsVboId;
		vao.customDataShortVboId = customDataShortsVboId;
		vao.flagsVboId = flagsVboId;
		vao.drawMode = DrawModeToPrimiteType(drawMode);
		return vao;
	}

	public override MeshRef UploadMesh(MeshData data)
	{
		int vaoId = GL.GenVertexArray();
		int vaoSlotNumber = 0;
		GL.BindVertexArray(vaoId);
		int xyzVboId = 0;
		int normalsVboId = 0;
		int uvVboId = 0;
		int rgbaVboId = 0;
		int customDataFloatVboId = 0;
		int customDataShortVboId = 0;
		int customDataIntVboId = 0;
		int customDataByteVboId = 0;
		int flagsVboId = 0;
		if (data.xyz != null)
		{
			BufferUsageHint hint = (data.XyzStatic ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			xyzVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, xyzVboId);
			GL.BufferData(BufferTarget.ArrayBuffer, 4 * data.XyzCount, data.xyz, hint);
			GL.VertexAttribPointer(vaoSlotNumber, 3, VertexAttribPointerType.Float, normalized: false, 0, 0);
			if (data.XyzInstanced)
			{
				GL.VertexAttribDivisor(vaoSlotNumber, 1);
			}
			vaoSlotNumber++;
		}
		if (data.Normals != null)
		{
			BufferUsageHint hint = (data.XyzStatic ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			normalsVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, normalsVboId);
			GL.BufferData(BufferTarget.ArrayBuffer, 4 * data.VerticesCount, data.Normals, hint);
			GL.VertexAttribPointer(vaoSlotNumber, 4, VertexAttribPointerType.Int2101010Rev, normalized: true, 0, 0);
			if (data.XyzInstanced)
			{
				GL.VertexAttribDivisor(vaoSlotNumber, 1);
			}
			vaoSlotNumber++;
		}
		if (data.Uv != null)
		{
			BufferUsageHint hint = (data.UvStatic ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			uvVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, uvVboId);
			GL.BufferData(BufferTarget.ArrayBuffer, 4 * data.UvCount, data.Uv, hint);
			GL.VertexAttribPointer(vaoSlotNumber, 2, VertexAttribPointerType.Float, normalized: false, 0, 0);
			if (data.UvInstanced)
			{
				GL.VertexAttribDivisor(vaoSlotNumber, 1);
			}
			vaoSlotNumber++;
		}
		if (data.Rgba != null)
		{
			BufferUsageHint hint = (data.RgbaStatic ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			rgbaVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, rgbaVboId);
			GL.BufferData(BufferTarget.ArrayBuffer, data.RgbaCount, data.Rgba, hint);
			GL.VertexAttribPointer(vaoSlotNumber, 4, VertexAttribPointerType.UnsignedByte, normalized: true, 0, 0);
			if (data.RgbaInstanced)
			{
				GL.VertexAttribDivisor(vaoSlotNumber, 1);
			}
			vaoSlotNumber++;
		}
		if (data.Flags != null)
		{
			BufferUsageHint hint = (data.FlagsStatic ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			flagsVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, flagsVboId);
			GL.BufferData(BufferTarget.ArrayBuffer, 4 * data.Flags.Length, data.Flags, hint);
			GL.VertexAttribIPointer(vaoSlotNumber, 1, VertexAttribIntegerType.UnsignedInt, 0, IntPtr.Zero);
			if (data.FlagsInstanced)
			{
				GL.VertexAttribDivisor(vaoSlotNumber, 1);
			}
			vaoSlotNumber++;
		}
		if (data.CustomFloats != null)
		{
			BufferUsageHint hint = (data.CustomFloats.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			customDataFloatVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, customDataFloatVboId);
			GL.BufferData(BufferTarget.ArrayBuffer, 4 * data.CustomFloats.AllocationSize, data.CustomFloats.Values, hint);
			for (int l = 0; l < data.CustomFloats.InterleaveSizes.Length; l++)
			{
				GL.VertexAttribPointer(vaoSlotNumber, data.CustomFloats.InterleaveSizes[l], VertexAttribPointerType.Float, normalized: false, data.CustomFloats.InterleaveStride, data.CustomFloats.InterleaveOffsets[l]);
				if (data.CustomFloats.Instanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
		}
		if (data.CustomShorts != null)
		{
			BufferUsageHint hint = (data.CustomShorts.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			customDataShortVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, customDataShortVboId);
			GL.BufferData(BufferTarget.ArrayBuffer, 2 * data.CustomShorts.AllocationSize, data.CustomShorts.Values, hint);
			for (int k = 0; k < data.CustomShorts.InterleaveSizes.Length; k++)
			{
				if (data.CustomShorts.Conversion == DataConversion.Integer)
				{
					GL.VertexAttribIPointer(vaoSlotNumber, data.CustomShorts.InterleaveSizes[k], VertexAttribIntegerType.Short, data.CustomShorts.InterleaveStride, IntPtr.Zero);
				}
				else
				{
					GL.VertexAttribPointer(vaoSlotNumber, data.CustomShorts.InterleaveSizes[k], VertexAttribPointerType.Short, data.CustomShorts.Conversion == DataConversion.NormalizedFloat, data.CustomShorts.InterleaveStride, data.CustomShorts.InterleaveOffsets[k]);
				}
				if (data.CustomShorts.Instanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
		}
		if (data.CustomInts != null)
		{
			BufferUsageHint hint = (data.CustomInts.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			customDataIntVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, customDataIntVboId);
			GL.BufferData(BufferTarget.ArrayBuffer, 4 * data.CustomInts.AllocationSize, data.CustomInts.Values, hint);
			for (int j = 0; j < data.CustomInts.InterleaveSizes.Length; j++)
			{
				GL.VertexAttribIPointer(vaoSlotNumber, data.CustomInts.InterleaveSizes[j], VertexAttribIntegerType.Int, data.CustomInts.InterleaveStride, IntPtr.Zero);
				if (data.CustomInts.Instanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
		}
		if (data.CustomBytes != null)
		{
			BufferUsageHint hint = (data.CustomBytes.StaticDraw ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
			customDataByteVboId = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, customDataByteVboId);
			GL.BufferData(BufferTarget.ArrayBuffer, data.CustomBytes.AllocationSize, data.CustomBytes.Values, hint);
			for (int i = 0; i < data.CustomBytes.InterleaveSizes.Length; i++)
			{
				if (data.CustomBytes.Conversion == DataConversion.Integer)
				{
					GL.VertexAttribIPointer(vaoSlotNumber, data.CustomBytes.InterleaveSizes[i], VertexAttribIntegerType.UnsignedByte, data.CustomBytes.InterleaveStride, IntPtr.Zero);
				}
				else
				{
					GL.VertexAttribPointer(vaoSlotNumber, data.CustomBytes.InterleaveSizes[i], VertexAttribPointerType.UnsignedByte, data.CustomBytes.Conversion == DataConversion.NormalizedFloat, data.CustomBytes.InterleaveStride, data.CustomBytes.InterleaveOffsets[i]);
				}
				if (data.CustomBytes.Instanced)
				{
					GL.VertexAttribDivisor(vaoSlotNumber, 1);
				}
				vaoSlotNumber++;
			}
		}
		GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		int vboIdIndex = GL.GenBuffer();
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboIdIndex);
		GL.BufferData(BufferTarget.ElementArrayBuffer, 4 * data.IndicesCount, data.Indices, data.IndicesStatic ? BufferUsageHint.StaticDraw : BufferUsageHint.DynamicDraw);
		GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		GL.BindVertexArray(0);
		CheckGlError("Something failed during mesh upload");
		return new VAO
		{
			VaoId = vaoId,
			IndicesCount = data.IndicesCount,
			vaoSlotNumber = vaoSlotNumber,
			vboIdIndex = vboIdIndex,
			normalsVboId = normalsVboId,
			xyzVboId = xyzVboId,
			uvVboId = uvVboId,
			rgbaVboId = rgbaVboId,
			customDataFloatVboId = customDataFloatVboId,
			customDataIntVboId = customDataIntVboId,
			customDataByteVboId = customDataByteVboId,
			customDataShortVboId = customDataShortVboId,
			flagsVboId = flagsVboId,
			drawMode = DrawModeToPrimiteType(data.mode)
		};
	}

	public override void DeleteMesh(MeshRef modelref)
	{
		if (modelref != null)
		{
			((VAO)modelref).Dispose();
		}
	}

	private PrimitiveType DrawModeToPrimiteType(EnumDrawMode drawmode)
	{
		return drawmode switch
		{
			EnumDrawMode.Lines => PrimitiveType.Lines, 
			EnumDrawMode.LineStrip => PrimitiveType.LineStrip, 
			_ => PrimitiveType.Triangles, 
		};
	}

	public override bool LoadMouseCursor(string cursorCoode, int hotx, int hoty, BitmapRef bmpRef)
	{
		try
		{
			SKBitmap bmp = ((BitmapExternal)bmpRef).bmp;
			if (bmp.Width > 32 || bmp.Height > 32)
			{
				return false;
			}
			float gUIScale = ClientSettings.GUIScale;
			if (gUIScale != 1f)
			{
				bmp = bmp.Resize(new SKImageInfo((int)((float)bmp.Width * gUIScale), (int)((float)bmp.Height * gUIScale)), SKFilterQuality.High);
			}
			int i = 0;
			byte[] data = new byte[bmp.BytesPerPixel * bmp.Width * bmp.Height];
			for (int y = 0; y < bmp.Height; y++)
			{
				for (int x = 0; x < bmp.Width; x++)
				{
					SKColor color = bmp.GetPixel(x, y);
					data[i] = color.Red;
					data[i + 1] = color.Green;
					data[i + 2] = color.Blue;
					data[i + 3] = color.Alpha;
					i += 4;
				}
			}
			preLoadedCursors[cursorCoode] = new MouseCursor(hotx, hoty, bmp.Width, bmp.Height, data);
			bmp.Dispose();
		}
		catch (Exception ex)
		{
			Logger.Error("Failed loading mouse cursor {0}:", cursorCoode);
			Logger.Error(ex);
			RestoreWindowCursor();
			return false;
		}
		return true;
	}

	public override void UseMouseCursor(string cursorCode, bool forceUpdate = false)
	{
		if ((cursorCode == null || cursorCode == CurrentMouseCursor) && !forceUpdate)
		{
			return;
		}
		try
		{
			window.Cursor = preLoadedCursors[cursorCode];
			CurrentMouseCursor = cursorCode;
		}
		catch
		{
			RestoreWindowCursor();
		}
	}

	public override void RestoreWindowCursor()
	{
		window.Cursor = MouseCursor.Default;
	}

	private void UpdateMousePosition()
	{
		if (!window.IsFocused || window.MouseState.Position == previousMousePosition)
		{
			return;
		}
		float xdelta;
		float ydelta;
		if (previousCursorState != window.CursorState)
		{
			xdelta = (ydelta = 0f);
		}
		else
		{
			xdelta = window.MouseState.Position.X - previousMousePosition.X;
			ydelta = window.MouseState.Position.Y - previousMousePosition.Y;
		}
		foreach (MouseEventHandler mouseEventHandler in mouseEventHandlers)
		{
			MouseEvent args = new MouseEvent((int)mouseX, (int)mouseY, (int)xdelta, (int)ydelta);
			mouseEventHandler.OnMouseMove(args);
		}
		if (window.CursorState == CursorState.Grabbed)
		{
			ignoreMouseMoveEvent = true;
			SetMousePosition((float)window.ClientSize.X / 2f, (float)window.ClientSize.Y / 2f);
		}
		else if (ignoreMouseMoveEvent)
		{
			ignoreMouseMoveEvent = false;
		}
		previousMousePosition = window.MouseState.Position;
		previousCursorState = window.CursorState;
	}

	private void Mouse_Move(MouseMoveEventArgs e)
	{
		if (!ignoreMouseMoveEvent)
		{
			SetMousePosition(e.X, e.Y);
		}
	}

	private void SetMousePosition(float x, float y)
	{
		mouseX = x;
		mouseY = y;
		if (RuntimeEnv.OS == OS.Mac)
		{
			mouseY += ClientSettings.WeirdMacOSMouseYOffset;
		}
	}

	private void Mouse_WheelChanged(OpenTK.Windowing.Common.MouseWheelEventArgs e)
	{
		foreach (MouseEventHandler mouseEventHandler in mouseEventHandlers)
		{
			float delta = e.OffsetY * ClientSettings.MouseWheelSensivity;
			if (RuntimeEnv.OS == OS.Mac)
			{
				delta = GameMath.Clamp(delta, -1f, 1f);
			}
			prevWheelValue += delta;
			Vintagestory.API.Client.MouseWheelEventArgs e2 = new Vintagestory.API.Client.MouseWheelEventArgs
			{
				delta = (int)delta,
				deltaPrecise = (int)delta,
				value = (int)prevWheelValue,
				valuePrecise = prevWheelValue
			};
			mouseEventHandler.OnMouseWheel(e2);
		}
	}

	private void Mouse_ButtonDown(MouseButtonEventArgs e)
	{
		EnumMouseButton enumMouseButton = MouseButtonConverter.ToEnumMouseButton(e.Button);
		foreach (MouseEventHandler mouseEventHandler in mouseEventHandlers)
		{
			MouseEvent args = new MouseEvent((int)mouseX, (int)mouseY, enumMouseButton, (int)e.Modifiers);
			mouseEventHandler.OnMouseDown(args);
		}
	}

	private void Mouse_ButtonUp(MouseButtonEventArgs e)
	{
		EnumMouseButton enumMouseButton = MouseButtonConverter.ToEnumMouseButton(e.Button);
		foreach (MouseEventHandler mouseEventHandler in mouseEventHandlers)
		{
			MouseEvent e2 = new MouseEvent((int)mouseX, (int)mouseY, enumMouseButton, (int)e.Modifiers);
			mouseEventHandler.OnMouseUp(e2);
		}
	}

	private void game_KeyPress(TextInputEventArgs e)
	{
		foreach (KeyEventHandler keyEventHandler in keyEventHandlers)
		{
			keyEventHandler.OnKeyPress(new KeyEvent
			{
				KeyCode = e.Unicode,
				KeyChar = (char)e.Unicode
			});
		}
	}

	private void game_KeyDown(KeyboardKeyEventArgs e)
	{
		if (e.Key == Keys.Unknown)
		{
			return;
		}
		int key = KeyConverter.NewKeysToGlKeys[(int)e.Key];
		foreach (KeyEventHandler keyEventHandler in keyEventHandlers)
		{
			KeyEvent args = new KeyEvent
			{
				KeyCode = key
			};
			if (EllapsedMs - lastKeyUpMs <= 200)
			{
				args.KeyCode2 = lastKeyUpKey;
			}
			args.CommandPressed = e.Command;
			args.CtrlPressed = e.Control;
			args.ShiftPressed = e.Shift;
			args.AltPressed = e.Alt;
			keyEventHandler.OnKeyDown(args);
		}
	}

	private void game_KeyUp(KeyboardKeyEventArgs e)
	{
		if (e.Key == Keys.Unknown)
		{
			return;
		}
		int key = KeyConverter.NewKeysToGlKeys[(int)e.Key];
		lastKeyUpMs = EllapsedMs;
		lastKeyUpKey = key;
		foreach (KeyEventHandler keyEventHandler in keyEventHandlers)
		{
			KeyEvent args = new KeyEvent
			{
				KeyCode = key
			};
			args.CommandPressed = e.Command;
			args.CtrlPressed = e.Control;
			args.ShiftPressed = e.Shift;
			args.AltPressed = e.Alt;
			keyEventHandler.OnKeyUp(args);
		}
	}

	public override MouseEvent CreateMouseEvent(EnumMouseButton button)
	{
		return new MouseEvent((int)mouseX, (int)mouseY, button, 0);
	}

	public override int GetUniformLocation(ShaderProgram program, string name)
	{
		return GL.GetUniformLocation(program.ProgramId, name);
	}

	public override bool CompileShader(Shader shader)
	{
		int shaderId = (shader.ShaderId = GL.CreateShader((ShaderType)shader.shaderType));
		string shaderCode = shader.Code;
		if (shaderCode != null)
		{
			if (shaderCode.IndexOfOrdinal("#version") == -1)
			{
				logger.Warning("Shader {0}: Is not defining a shader version via #version", shader.Filename);
			}
			if (RuntimeEnv.OS == OS.Mac)
			{
				shaderCode = Regex.Replace(shaderCode, "#version \\d+", "#version 330");
			}
			int startIndex = shaderCode.IndexOf('\n', Math.Max(0, shaderCode.IndexOfOrdinal("#version"))) + 1;
			shaderCode = shaderCode.Insert(startIndex, shader.PrefixCode);
		}
		GL.ShaderSource(shaderId, shaderCode);
		GL.CompileShader(shaderId);
		GL.GetShader(shaderId, ShaderParameter.CompileStatus, out var outval);
		if (outval != 1)
		{
			string logText = GL.GetShaderInfoLog(shaderId);
			logger.Error("Shader compile error in {0} {1}", shader.Filename, logText.TrimEnd());
			logger.VerboseDebug("{0}", shaderCode);
			return false;
		}
		return true;
	}

	public override bool CreateShaderProgram(ShaderProgram program)
	{
		bool ok = true;
		int programId = (program.ProgramId = GL.CreateProgram());
		GL.AttachShader(programId, program.VertexShader.ShaderId);
		GL.AttachShader(programId, program.FragmentShader.ShaderId);
		if (program.GeometryShader != null)
		{
			GL.AttachShader(programId, program.GeometryShader.ShaderId);
		}
		foreach (KeyValuePair<int, string> val in program.attributes)
		{
			GL.BindAttribLocation(program.ProgramId, val.Key, val.Value);
		}
		GL.LinkProgram(programId);
		GL.GetProgram(programId, GetProgramParameterName.LinkStatus, out var outval);
		string logText = GL.GetProgramInfoLog(programId);
		if (outval != 1)
		{
			logger.Error("Link error in shader program for pass {0}: {1}", program.PassName, logText.TrimEnd());
			ok = false;
		}
		else
		{
			logger.Notification("Loaded Shaderprogramm for render pass {0}.", program.PassName);
		}
		return ok;
	}
}
