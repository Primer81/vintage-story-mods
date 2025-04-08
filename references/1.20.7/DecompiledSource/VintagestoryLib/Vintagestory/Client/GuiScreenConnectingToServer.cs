using System;
using System.Collections.Generic;
using System.IO;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Server;

namespace Vintagestory.Client;

internal class GuiScreenConnectingToServer : GuiScreen
{
	protected ClientMain runningGame;

	protected long lastLogfileCheck;

	protected long lastTextUpdate;

	private long lastDotsUpdate;

	private int dotsCount;

	private bool singleplayer;

	private string prevText;

	private long ellapseMs;

	private List<string> _lines;

	protected bool loggerAdded;

	private readonly EnumLogType _logToWatch = EnumLogType.Event;

	private static ILogger Logger => ScreenManager.Platform.Logger;

	public GuiScreenConnectingToServer(bool singleplayer, ScreenManager ScreenManager, GuiScreen parent)
		: base(ScreenManager, parent)
	{
		this.singleplayer = singleplayer;
		_lines = new List<string>();
		if (parent != null)
		{
			runningGame = ((GuiScreenRunningGame)parent).runningGame;
			if (singleplayer)
			{
				if (ClientSettings.DeveloperMode)
				{
					ComposeDeveloperLogDialog("startingspserver", Lang.Get("Launching singleplayer server..."), Lang.Get("Starting server..."));
				}
				else
				{
					_logToWatch = EnumLogType.StoryEvent;
					ComposePlayerLogDialog("startingspserver", Lang.Get("It begins..."));
				}
			}
			else
			{
				ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 900.0, 200.0);
				ElementBounds insetBounds = textBounds.ForkBoundingParent(10.0, 10.0, 10.0, 10.0);
				ElementBounds dialogBounds = insetBounds.ForkBoundingParent(0.0, 50.0, 0.0, 100.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 280.0);
				ElementComposer?.Dispose();
				ElementComposer = ScreenManager.GuiComposers.Create("connectingtoserver", dialogBounds).BeginChildElements(insetBounds).AddStaticCustomDraw(ElementBounds.Fill, delegate(Context ctx, ImageSurface surface, ElementBounds bounds)
				{
					GuiElement.RoundRectangle(ctx, bounds.bgDrawX, bounds.bgDrawY, bounds.OuterWidth, bounds.OuterHeight, 1.0);
					ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor);
					ctx.Fill();
				})
					.AddDynamicText(Lang.Get("Connecting to multiplayer server..."), CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center), textBounds, "centertext")
					.EndChildElements()
					.AddButton(Lang.Get("Cancel"), onCancel, ElementStdBounds.MenuButton(4f).WithFixedPadding(10.0, 4.0), EnumButtonStyle.Normal, "cancelButton")
					.Compose();
				ElementComposer.GetButton("cancelButton").Enabled = true;
			}
		}
		Logger.Debug("GuiScreenConnectingToServer constructed");
	}

	protected void LogAdded(EnumLogType type, string message, object[] args)
	{
		if (type == _logToWatch || type == EnumLogType.Error || type == EnumLogType.Fatal)
		{
			try
			{
				string msg = string.Format(message, args);
				string line = $"{DateTime.Now:d.M.yyyy HH:mm:ss} [{type}] {msg}";
				_lines.Add(line);
			}
			catch (FormatException)
			{
				_lines.Add("Couldn't write to log file, failed formatting " + message + " (FormatException)");
			}
		}
	}

	protected void ComposePlayerLogDialog(string dialogCode, string firstLine)
	{
		ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 900.0, 300.0);
		ElementBounds insetBounds = textBounds.ForkBoundingParent(10.0, 7.0, 10.0, 10.0);
		ElementBounds clipBounds = textBounds.FlatCopy().WithParent(insetBounds);
		clipBounds.fixedHeight -= 3.0;
		ElementBounds dialogBounds = insetBounds.ForkBoundingParent(0.0, 50.0, 26.0, 80.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 280.0);
		ElementBounds titleBounds = ElementBounds.Fixed(0.0, -30.0, dialogBounds.fixedWidth, 28.0);
		ElementBounds buttonBounds = ElementBounds.FixedPos(EnumDialogArea.CenterBottom, 0.0, 0.0).WithFixedPadding(10.0, 2.0);
		ElementBounds scrollbarBounds = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 1.0, 10.0, insetBounds.fixedHeight - 2.0);
		ElementComposer?.Dispose();
		CairoFont loadingFont = CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center);
		loadingFont.Color[3] = 0.65;
		ElementComposer = ScreenManager.GuiComposers.Create(dialogCode, dialogBounds).BeginChildElements(insetBounds).AddStaticCustomDraw(ElementBounds.Fill, delegate(Context ctx, ImageSurface surface, ElementBounds bounds)
		{
			GuiElement.RoundRectangle(ctx, bounds.bgDrawX, bounds.bgDrawY, bounds.OuterWidth, bounds.OuterHeight, 1.0);
			ctx.SetSourceRGBA(GuiStyle.DialogLightBgColor);
			ctx.Fill();
		})
			.BeginClip(clipBounds)
			.AddDynamicText(firstLine, CairoFont.WhiteSmallishText(), textBounds, "centertext")
			.EndClip()
			.AddCompactVerticalScrollbar(OnNewScrollbarBalue, scrollbarBounds, "scrollbar")
			.AddDynamicText(Lang.Get("Loading..."), loadingFont, titleBounds)
			.EndChildElements()
			.AddSmallButton(Lang.Get("Open Logs folder"), onOpenLogs, buttonBounds.BelowCopy(0.0, 50.0), EnumButtonStyle.Normal, "logsButton")
			.AddButton((dialogCode == "startingspserver") ? Lang.Get("Cancel") : Lang.Get("Force quit"), onCancel, buttonBounds, EnumButtonStyle.Normal, "cancelButton")
			.Compose();
		ElementComposer.GetButton("cancelButton").Enabled = true;
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clipBounds.fixedHeight, (float)textBounds.fixedHeight);
	}

	internal void ComposeDeveloperLogDialog(string dialogCode, string titleText, string firstLine)
	{
		ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 900.0, 300.0);
		ElementBounds insetBounds = textBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
		ElementBounds clipBounds = textBounds.FlatCopy().WithParent(insetBounds);
		clipBounds.fixedHeight -= 3.0;
		ElementBounds dialogBounds = insetBounds.ForkBoundingParent(0.0, 50.0, 26.0, 80.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 280.0);
		ElementBounds titleBounds = ElementBounds.Fixed(0.0, 0.0, dialogBounds.fixedWidth, 20.0);
		ElementBounds buttonBounds = ElementBounds.FixedPos(EnumDialogArea.CenterBottom, 0.0, 0.0).WithFixedPadding(10.0, 2.0);
		ElementBounds scrollbarBounds = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 50.0, 20.0, insetBounds.fixedHeight);
		ElementComposer?.Dispose();
		ElementComposer = ScreenManager.GuiComposers.Create(dialogCode, ElementBounds.Fill).AddShadedDialogBG(ElementBounds.Fill, withTitleBar: false).BeginChildElements(dialogBounds)
			.AddStaticText(titleText, CairoFont.WhiteSmallishText(), titleBounds)
			.AddInset(insetBounds, 3, 0.8f)
			.BeginClip(clipBounds)
			.AddDynamicText(firstLine, CairoFont.WhiteSmallishText(), textBounds, "centertext")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarBalue, scrollbarBounds, "scrollbar")
			.AddSmallButton(Lang.Get("Open Logs folder"), onOpenLogs, buttonBounds.BelowCopy(0.0, 50.0), EnumButtonStyle.Normal, "logsButton")
			.AddButton((dialogCode == "startingspserver") ? Lang.Get("Cancel") : Lang.Get("Force quit"), onCancel, buttonBounds, EnumButtonStyle.Normal, "cancelButton")
			.EndChildElements()
			.Compose();
		ElementComposer.GetButton("cancelButton").Enabled = true;
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clipBounds.fixedHeight, (float)textBounds.fixedHeight);
	}

	private void OnNewScrollbarBalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetDynamicText("centertext").Bounds;
		bounds.fixedY = 5f - value;
		bounds.CalcWorldBounds();
	}

	private bool onOpenLogs()
	{
		NetUtil.OpenUrlInBrowser(GamePaths.Logs);
		return true;
	}

	private bool onCancel()
	{
		if (runningGame != null)
		{
			runningGame.DestroyGameSession(gotDisconnected: false);
			runningGame = null;
			ScreenManager.GamePlatform.ExitSinglePlayerServer();
		}
		if (singleplayer)
		{
			ScreenManager.LoadAndCacheScreen(typeof(GuiScreenExitingServer));
		}
		else
		{
			ScreenManager.LoadAndCacheScreen(typeof(GuiScreenMultiplayer));
		}
		ElementComposer.GetButton("cancelButton").Enabled = false;
		return true;
	}

	protected void updateLogText()
	{
		string log = string.Join("\n", _lines);
		GuiElementDynamicText textElem = ElementComposer.GetDynamicText("centertext");
		GuiElementScrollbar scrollElem = ElementComposer.GetScrollbar("scrollbar");
		if (textElem == null || log.Length > 10000)
		{
			return;
		}
		textElem.SetNewText(log, autoHeight: true);
		if (scrollElem != null)
		{
			scrollElem.SetNewTotalHeight((float)(textElem.Bounds.OuterHeight / (double)ClientSettings.GUIScale));
			if (!scrollElem.mouseDownOnScrollbarHandle && prevText != log)
			{
				scrollElem.ScrollToBottom();
			}
		}
		prevText = log;
	}

	public override void RenderToDefaultFramebuffer(float dt)
	{
		if (!singleplayer || !ClientSettings.DeveloperMode)
		{
			if (!runningGame.BlocksReceivedAndLoaded)
			{
				ellapseMs = ScreenManager.GamePlatform.EllapsedMs;
			}
			ScreenManager.mainScreen.Render(dt, ellapseMs, onlyBackground: true);
		}
		if (ServerMain.Logger != null && !loggerAdded)
		{
			loggerAdded = true;
			ServerMain.Logger.EntryAdded += LogAdded;
		}
		if (singleplayer && ScreenManager.Platform.EllapsedMs - lastLogfileCheck > 400)
		{
			updateLogText();
			lastLogfileCheck = ScreenManager.Platform.EllapsedMs;
		}
		if (runningGame == null)
		{
			ScreenManager.StartMainMenu();
			return;
		}
		updateScreenUI();
		ElementComposer.Render(dt);
		ElementComposer.PostRender(dt);
		LoadedTexture versionNumberTexture = ScreenManager.versionNumberTexture;
		float windowSizeX = ScreenManager.GamePlatform.WindowSize.Width;
		float windowSizeY = ScreenManager.GamePlatform.WindowSize.Height;
		ScreenManager.api.renderapi.Render2DTexturePremultipliedAlpha(versionNumberTexture.TextureId, windowSizeX - (float)versionNumberTexture.Width - 10f, windowSizeY - (float)versionNumberTexture.Height - 10f, versionNumberTexture.Width, versionNumberTexture.Height);
		runningGame.ExecuteMainThreadTasks(dt);
		if ((!runningGame.IsSingleplayer || (runningGame.IsSingleplayer && runningGame.AssetsReceived && !runningGame.AssetLoadingOffThread && ScreenManager.Platform.IsLoadedSinglePlayerServer())) && !runningGame.StartedConnecting)
		{
			connectToGameServer();
		}
		if (runningGame.exitToDisconnectScreen)
		{
			exitToDisconnectScreen();
		}
		if (runningGame.exitToMainMenu)
		{
			exitToMainMenu();
		}
	}

	private void updateScreenUI()
	{
		long ellapsedMS = ScreenManager.Platform.EllapsedMs;
		if (runningGame.AssetsReceived && runningGame.ServerReady)
		{
			if (!singleplayer)
			{
				ElementComposer.GetDynamicText("centertext")?.SetNewText(Lang.Get("Data received, launching client instance..."));
			}
			else
			{
				if (ellapsedMS - lastDotsUpdate > 500)
				{
					lastDotsUpdate = ellapsedMS;
					dotsCount = dotsCount % 3 + 1;
				}
				GuiElementDynamicText center = ElementComposer.GetDynamicText("centertext");
				if (center != null)
				{
					string msg = ((!ClientSettings.DeveloperMode) ? ("\n" + Lang.Get("...")) : ("\n" + Lang.Get("Data received, launching single player instance...")));
					int dotsCountLocal = dotsCount;
					while (--dotsCountLocal > 0)
					{
						msg = msg + " " + Lang.Get("...");
					}
					center.SetNewText(prevText + msg);
				}
			}
			GuiElementDynamicText textElem = ElementComposer.GetDynamicText("centertext");
			GuiElementScrollbar scrollElem = ElementComposer.GetScrollbar("scrollbar");
			if (textElem != null && scrollElem != null)
			{
				scrollElem.SetNewTotalHeight((float)(textElem.Bounds.OuterHeight / (double)ClientSettings.GUIScale));
				if (!scrollElem.mouseDownOnScrollbarHandle)
				{
					scrollElem.ScrollToBottom();
				}
			}
		}
		else if (runningGame.Connectdata.ErrorMessage == null)
		{
			if (ellapsedMS - lastTextUpdate <= 150)
			{
				return;
			}
			lastTextUpdate = ellapsedMS;
			if (runningGame.Connectdata.Connected)
			{
				int kbytes = runningGame.networkProc.TotalBytesReceivedAndReceiving / 1024;
				string text;
				if (runningGame.Connectdata.PositionInQueue > 0)
				{
					text = Lang.Get("connect-inqueue", runningGame.Connectdata.PositionInQueue);
				}
				else
				{
					text = Lang.Get("Connected to server, downloading data...");
					text = text + "\n" + Lang.Get("{0} kilobyte received", kbytes);
				}
				if (text != ElementComposer.GetDynamicText("centertext").GetText())
				{
					Logger.Notification(text);
				}
				ElementComposer.GetDynamicText("centertext").SetNewText(text);
			}
		}
		else
		{
			string text2 = Lang.Get("error-connecting", runningGame.Connectdata.ErrorMessage);
			if (text2 != ElementComposer.GetDynamicText("centertext").GetText())
			{
				Logger.Notification(Lang.Get("error-connecting-host", runningGame.Connectdata.Host, runningGame.Connectdata.ErrorMessage));
			}
			ElementComposer.GetDynamicText("centertext").SetNewText(text2);
		}
	}

	private void exitToMainMenu()
	{
		runningGame.Dispose();
		if (runningGame.IsSingleplayer && ScreenManager.Platform.IsServerRunning)
		{
			ScreenManager.LoadAndCacheScreen(typeof(GuiScreenExitingServer));
		}
		else
		{
			ScreenManager.StartMainMenu();
		}
	}

	private void exitToDisconnectScreen()
	{
		runningGame?.Dispose();
		Logger.Notification("Exiting current game");
		if (runningGame?.disconnectAction == "trydownloadmods")
		{
			ServerConnectData cdata = runningGame.Connectdata;
			string installPath = ((cdata.Host == null) ? GamePaths.DataPathMods : System.IO.Path.Combine(GamePaths.DataPathServerMods, GamePaths.ReplaceInvalidChars(cdata.Host + "-" + cdata.Port)));
			GuiScreenDownloadMods modScreen = new GuiScreenDownloadMods(cdata, installPath, runningGame.disconnectMissingMods, ScreenManager, ScreenManager.mainScreen);
			modScreen.serverargs = (ParentScreen as GuiScreenRunningGame).serverargs;
			ScreenManager.LoadScreen(modScreen);
		}
		else
		{
			string disconnectReason = runningGame.disconnectReason ?? "unknown";
			ScreenManager.LoadScreen(new GuiScreenDisconnected(disconnectReason, ScreenManager, ScreenManager.mainScreen));
		}
	}

	private void connectToGameServer()
	{
		Logger.Debug("Opening socket to server...");
		runningGame.StartedConnecting = true;
		try
		{
			runningGame.Connect();
		}
		catch (Exception e)
		{
			Logger.Notification("Exiting current game");
			string msg = Lang.Get("Could not initiate connection: {0}\n\n<font color=\"#bbb\">Full Trace:\n{1}</font>", e.Message, LoggerBase.CleanStackTrace(e.ToString()));
			Logger.Warning(msg.Replace("\n\n", "\n"));
			runningGame.Dispose();
			ScreenManager.LoadScreen(new GuiScreenDisconnected(msg, ScreenManager, ScreenManager.mainScreen, "server-unableconnect"));
		}
	}

	public override void Dispose()
	{
		base.Dispose();
		if (ServerMain.Logger != null)
		{
			ServerMain.Logger.EntryAdded -= LogAdded;
		}
		_lines = null;
	}
}
