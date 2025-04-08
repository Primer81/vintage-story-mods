using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.Network;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.Server;

namespace Vintagestory.Client;

public class GuiScreenRunningGame : GuiScreen
{
	internal ClientMain runningGame;

	private ServerConnectData connectData;

	private bool singleplayer;

	public StartServerArgs serverargs;

	private ClientPlatformAbstract platform;

	private static object warningEntriesLock = new object();

	private static List<LogEntry> warningEntries = new List<LogEntry>();

	private static bool captureWarnings = true;

	public GuiScreenRunningGame(ScreenManager screenManager, GuiScreen parent)
		: base(screenManager, parent)
	{
		platform = ScreenManager.Platform;
		runningGame = new ClientMain(this, ScreenManager.Platform);
		base.RenderBg = false;
	}

	private static void Logger_EntryAddedClient(EnumLogType logType, string message, object[] args)
	{
		Logger_EntryAdded(EnumAppSide.Client, logType, message, args);
	}

	private static void Logger_EntryAddedServer(EnumLogType logType, string message, object[] args)
	{
		Logger_EntryAdded(EnumAppSide.Server, logType, message, args);
	}

	private static void Logger_EntryAdded(EnumAppSide side, EnumLogType logType, string message, params object[] args)
	{
		lock (warningEntriesLock)
		{
			if (captureWarnings && (logType == EnumLogType.Error || logType == EnumLogType.Warning || logType == EnumLogType.Fatal))
			{
				warningEntries.Add(new LogEntry
				{
					Logtype = logType,
					Message = message,
					args = args,
					Side = side
				});
			}
		}
	}

	private void handOverRenderingToRunningGame()
	{
		runningGame.MouseGrabbed = platform.IsFocused;
		ScreenManager.LoadScreen(this);
		ScreenManager.introMusicShouldStop = true;
		if (ScreenManager.IntroMusic != null && !ScreenManager.IntroMusic.HasStopped)
		{
			float volume = ScreenManager.IntroMusic.Params.Volume;
			TyronThreadPool.QueueLongDurationTask(delegate
			{
				while ((double)ScreenManager.IntroMusic.Params.Volume > 0.01)
				{
					Thread.Sleep(40);
					ScreenManager.IntroMusic.SetVolume(volume *= 0.98f);
				}
				ScreenManager.IntroMusic.Stop();
			});
		}
		ScreenManager.EnqueueMainThreadTask(printWarningsAndEndCapture);
	}

	public override bool OnEvent(string eventCode, object arg)
	{
		if (eventCode == "maploaded")
		{
			handOverRenderingToRunningGame();
			return true;
		}
		return false;
	}

	public void Start(bool singleplayer, StartServerArgs serverargs, ServerConnectData connectData)
	{
		this.singleplayer = singleplayer;
		this.serverargs = serverargs;
		this.connectData = connectData;
		runningGame.IsSingleplayer = singleplayer;
		runningGame.Start();
		if (this.connectData?.ErrorMessage == null)
		{
			Connect();
		}
	}

	private void Connect()
	{
		warningEntries.Clear();
		captureWarnings = true;
		ScreenManager.Platform.Logger.EntryAdded -= Logger_EntryAddedClient;
		ScreenManager.Platform.Logger.EntryAdded += Logger_EntryAddedClient;
		if (singleplayer)
		{
			platform.StartSinglePlayerServer(serverargs);
			TyronThreadPool.QueueTask(delegate
			{
				while (ServerMain.Logger == null)
				{
					Thread.Sleep(1);
				}
				ServerMain.Logger.EntryAdded -= Logger_EntryAddedServer;
				ServerMain.Logger.EntryAdded += Logger_EntryAddedServer;
			});
			connectData = new ServerConnectData();
			runningGame.Connectdata = connectData;
			DummyTcpNetClient netClient = new DummyTcpNetClient();
			DummyNetwork[] dummyNetworks = platform.GetSinglePlayerServerNetwork();
			netClient.SetNetwork(dummyNetworks[0]);
			runningGame.MainNetClient = netClient;
			DummyUdpNetClient udpNetClient = new DummyUdpNetClient();
			runningGame.UdpNetClient = udpNetClient;
			udpNetClient.SetNetwork(dummyNetworks[1]);
		}
		else
		{
			runningGame.Connectdata = connectData;
			TcpNetClient client = new TcpNetClient();
			UdpNetClient udpclient = new UdpNetClient();
			runningGame.MainNetClient = client;
			runningGame.UdpNetClient = udpclient;
		}
		ScreenManager.Platform.Logger.Notification("Initialized Server Connection");
	}

	public override void RenderToPrimary(float dt)
	{
		platform.DoPostProcessingEffects = true;
		float ssaaLevel = ClientSettings.SSAA;
		int width = platform.WindowSize.Width;
		int height = platform.WindowSize.Height;
		int fullWidth = (int)((float)width * ssaaLevel);
		int fullHeight = (int)((float)height * ssaaLevel);
		platform.GlViewport(0, 0, fullWidth, fullHeight);
		runningGame.MainGameLoop(dt);
		if (runningGame.doReconnect)
		{
			Reconnect();
		}
		else if (runningGame.exitToDisconnectScreen)
		{
			ExitOrRedirect(isDisconnect: true);
		}
		else
		{
			if (!runningGame.exitToMainMenu)
			{
				return;
			}
			bool deleteWorld = runningGame.deleteWorld;
			ExitOrRedirect();
			if (!deleteWorld)
			{
				return;
			}
			TyronThreadPool.QueueTask(delegate
			{
				Thread.Sleep(150);
				try
				{
					ScreenManager.GamePlatform.XPlatInterface.MoveFileToRecyclebin(serverargs.SaveFileLocation);
				}
				catch
				{
				}
			});
		}
	}

	public override void RenderAfterPostProcessing(float dt)
	{
		if (!runningGame.doReconnect && !runningGame.exitToMainMenu)
		{
			runningGame.RenderAfterPostProcessing(dt);
		}
	}

	public override void RenderAfterFinalComposition(float dt)
	{
		if (!runningGame.doReconnect && !runningGame.exitToMainMenu)
		{
			runningGame.RenderAfterFinalComposition(dt);
		}
	}

	public override void RenderAfterBlit(float dt)
	{
		if (!runningGame.doReconnect && !runningGame.exitToMainMenu)
		{
			runningGame.RenderAfterBlit(dt);
		}
	}

	public override void RenderToDefaultFramebuffer(float dt)
	{
		int width = platform.WindowSize.Width;
		int height = platform.WindowSize.Height;
		platform.GlViewport(0, 0, width, height);
		if (!runningGame.exitToMainMenu)
		{
			runningGame.RenderToDefaultFramebuffer(dt);
		}
	}

	public override void OnWindowClosed()
	{
		captureWarnings = false;
		ExitOrRedirect(isDisconnect: false, "window close event");
	}

	private void Reconnect()
	{
		captureWarnings = true;
		ExitOrRedirect();
		ScreenManager.StartGame(singleplayer, serverargs, connectData);
	}

	public override void ReloadWorld(string reason)
	{
		captureWarnings = false;
		ExitOrRedirect(isDisconnect: false, reason);
		while (ScreenManager.Platform.IsServerRunning)
		{
			Thread.Sleep(5);
		}
		ScreenManager.StartGame(singleplayer, serverargs, connectData);
	}

	public void ExitOrRedirect(bool isDisconnect = false, string reason = null)
	{
		captureWarnings = false;
		runningGame.MouseGrabbed = false;
		runningGame.DestroyGameSession(isDisconnect);
		if (reason == null)
		{
			reason = runningGame.exitReason;
		}
		if (reason == null)
		{
			reason = "unknown";
		}
		if (isDisconnect)
		{
			string disconnectReason = runningGame.disconnectReason ?? "unknown";
			ScreenManager.Platform.Logger.Notification("Exiting current game to disconnected screen, reason: {0}", disconnectReason);
			ScreenManager.LoadScreen(new GuiScreenDisconnected(disconnectReason, ScreenManager, ScreenManager.mainScreen));
		}
		else
		{
			ScreenManager.Platform.Logger.Notification("Exiting current game to main menu, reason: {0}", reason);
			if (runningGame.IsSingleplayer && ScreenManager.Platform.IsServerRunning)
			{
				ScreenManager.LoadAndCacheScreen(typeof(GuiScreenExitingServer));
			}
			else
			{
				ScreenManager.StartMainMenu();
			}
		}
		if (runningGame.GetRedirect() != null)
		{
			ScreenManager.TryRedirect(runningGame.GetRedirect());
		}
		runningGame = null;
		ScreenManager.GamePlatform.ResetGamePauseAndUptimeState();
	}

	public override void OnKeyDown(KeyEvent args)
	{
		runningGame.OnKeyDown(args);
	}

	public override void OnKeyUp(KeyEvent args)
	{
		runningGame.OnKeyUp(args);
	}

	public override void OnKeyPress(KeyEvent args)
	{
		runningGame.OnKeyPress(args);
	}

	public override void OnMouseDown(MouseEvent args)
	{
		if (runningGame.Platform.IsFocused)
		{
			runningGame.OnMouseDownRaw(args);
		}
	}

	public override void OnMouseMove(MouseEvent args)
	{
		if (runningGame.Platform.IsFocused && !runningGame.disposed)
		{
			runningGame.OnMouseMove(args);
		}
	}

	public override void OnMouseUp(MouseEvent args)
	{
		if (runningGame.Platform.IsFocused)
		{
			runningGame.OnMouseUpRaw(args);
		}
	}

	public override void OnMouseWheel(MouseWheelEventArgs args)
	{
		runningGame.OnMouseWheel(args);
	}

	public override bool OnFileDrop(string filename)
	{
		return runningGame.OnFileDrop(filename);
	}

	public override void OnFocusChanged(bool focus)
	{
		runningGame.OnFocusChanged(focus);
	}

	private void printWarningsAndEndCapture()
	{
		lock (warningEntriesLock)
		{
			captureWarnings = false;
			if (warningEntries.Count > 0)
			{
				ScreenManager.Platform.Logger.Warning("===============================================================");
				ScreenManager.Platform.Logger.Warning("(x_x) Captured {0} issues during startup:", warningEntries.Count);
				foreach (LogEntry line in warningEntries)
				{
					ILogger logger;
					if (line.Side != EnumAppSide.Server)
					{
						logger = ScreenManager.Platform.Logger;
					}
					else
					{
						ILogger logger2 = ServerMain.Logger;
						logger = logger2;
					}
					logger?.Log(line.Logtype, line.Message, line.args);
				}
				ScreenManager.Platform.Logger.Warning("===============================================================");
			}
			else
			{
				ScreenManager.Platform.Logger.Notification("===============================================================");
				ScreenManager.Platform.Logger.Notification("(^_^) No issues captured during startup");
				ScreenManager.Platform.Logger.Notification("===============================================================");
			}
		}
	}
}
