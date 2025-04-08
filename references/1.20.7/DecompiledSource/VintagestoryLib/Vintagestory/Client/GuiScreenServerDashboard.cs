using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Client.Util;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class GuiScreenServerDashboard : GuiScreen
{
	private bool CallbackEnqueued;

	private ServerCtrlBackendInterface backend;

	private string connectionString;

	private string identifier;

	private string _password;

	private GameServerStatus gameServerStatus;

	private bool showCancelOnSelectVersion;

	private WorldConfig wcu;

	private string currentScreen;

	private string logText;

	private int serverStatusProbingTries;

	private int dlStatusProbingTries;

	private string desireState;

	private bool proberActive;

	private GuiScreenWorldCustomize customizeScreen;

	private string[] loadtexts = new string[3]
	{
		Lang.Get("Loading."),
		Lang.Get("Loading.."),
		Lang.Get("Loading...")
	};

	private float accum;

	private bool onDownloadWorldNow()
	{
		if (gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready)
		{
			ScreenManager.api.Gui.OpenLink(gameServerStatus.Downloadsavefilename);
		}
		return true;
	}

	private bool onRequestDownload()
	{
		currentScreen = "confirmdownload";
		ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("This action will create a downloadable copy of your savegame and player data. It can be requested once every 24 hours and the downloadable copy will stay online for 24 hours as well. Preparing the copy takes a few minutes, check server dashboard see the current copy status."), OnDidConfirmDownlod, ScreenManager, this));
		return true;
	}

	private void OnDidConfirmDownlod(bool confirm)
	{
		if (!confirm)
		{
			ScreenManager.LoadScreenNoLoadCall(this);
			return;
		}
		backend.RequestDownload(delegate(EnumAuthServerResponse status, GameServerStatus response)
		{
			gameServerStatus = response;
			ScreenManager.LoadScreenNoLoadCall(this);
			if (gameServerStatus.ActiveserverDays <= 0f)
			{
				screenServerExpired(gameServerStatus.ActiveserverDays);
			}
			else
			{
				screenServerStatus(response);
			}
			dlStatusProbingTries = 12;
			runDownloadStatusProber();
		});
	}

	private void runDownloadStatusProber()
	{
		if (gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready && ElementComposer.GetRichtext("dlStatusText") != null)
		{
			ElementComposer.GetRichtext("dlStatusText").SetNewText(Lang.Get("Your world download is ready, please download it within 24 hours."), CairoFont.WhiteDetailText());
		}
		if (gameServerStatus.DownloadState == EnumDownloadSavesStatus.Copying)
		{
			if (ElementComposer.GetRichtext("dlStatusText") != null)
			{
				string[] downloadStateStrs = new string[3]
				{
					Lang.Get("World download requested, copying in progress."),
					Lang.Get("World download requested, copying in progress.."),
					Lang.Get("World download requested, copying in progress...")
				};
				ElementComposer.GetRichtext("dlStatusText").SetNewText(downloadStateStrs[GameMath.Mod(dlStatusProbingTries, 3)], CairoFont.WhiteDetailText());
			}
			if (!CallbackEnqueued)
			{
				CallbackEnqueued = true;
				ScreenManager.EnqueueCallBack(delegate
				{
					backend.GetStatus(delegate(EnumAuthServerResponse rs, GameServerStatus gs)
					{
						if (rs == EnumAuthServerResponse.Good)
						{
							if (currentScreen != "dashboard")
							{
								gameServerStatus = gs;
							}
							else
							{
								onStatusReady(rs, gs);
							}
						}
						CallbackEnqueued = false;
						runDownloadStatusProber();
					});
				}, 5000);
			}
			dlStatusProbingTries--;
		}
		if (ElementComposer.GetButton("worldDownloadButton") != null)
		{
			ElementComposer.GetButton("worldDownloadButton").Enabled = gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready;
		}
	}

	private void screenSelectRegion(GameServerStatus status)
	{
		currentScreen = "selectregion";
		ElementBounds rowLeft = ElementBounds.Fixed(0.0, 50.0, 300.0, 35.0);
		ElementComposer = screenBase(showSupportText: true).AddStaticText(Lang.Get("Please select the server region. This may take up to 40 seconds after confirming."), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 30.0)).AddDropDown(status.Regions, status.Regions, 0, null, rowLeft = rowLeft.BelowCopy(0.0, 30.0), "regionSelect").AddButton(Lang.Get("Confirm"), onConfirmRegion, rowLeft = rowLeft.BelowCopy(0.0, 30.0), EnumButtonStyle.Normal, "saveButton")
			.EndChildElements()
			.Compose();
	}

	private bool onConfirmRegion()
	{
		ElementComposer.GetButton("saveButton").Enabled = false;
		backend.SelectRegion(ElementComposer.GetDropDown("regionSelect").SelectedValue, onRegionSelected);
		return true;
	}

	private void onRegionSelected(EnumAuthServerResponse reqStatus, ServerCtrlResponse response)
	{
		setResponseNotifier(reqStatus, response?.Reason);
		if (reqStatus != 0)
		{
			ElementComposer.GetButton("saveButton").Enabled = true;
		}
		else if (response.StatusCode == "ok" || response.StatusCode == "success")
		{
			backend.GetStatus(onStatusReady);
			screenLoading();
		}
		else
		{
			setResponseNotifier(EnumAuthServerResponse.Bad, response.Code);
		}
	}

	private void screenSelectVersion(string[] versions)
	{
		currentScreen = "selectversion";
		ElementBounds rowLeft = ElementBounds.Fixed(0.0, 50.0, 300.0, 35.0);
		string[] versionnames = versions;
		int index = versions.IndexOf("1.20.7");
		if (index >= 0)
		{
			versionnames = (string[])versions.Clone();
			ref string reference = ref versionnames[index];
			reference = reference + " " + Lang.Get("(recommended)");
		}
		ElementComposer = screenBase(showSupportText: true).AddStaticText(Lang.Get("Version Selector"), CairoFont.WhiteSmallishText(), rowLeft = rowLeft.BelowCopy(0.0, -10.0)).AddStaticText(Lang.Get("Please select the server version you wish to change to."), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 10.0).WithFixedWidth(500.0)).AddDropDown(versions, versionnames, index, null, rowLeft = rowLeft.BelowCopy(0.0, 10.0).WithFixedWidth(300.0), "versionSelect")
			.AddIf(showCancelOnSelectVersion)
			.AddButton(Lang.Get("Cancel"), onCancel, rowLeft = rowLeft.BelowCopy(0.0, 100.0).WithFixedWidth(300.0), EnumButtonStyle.Normal, "cancelButton")
			.AddButton(Lang.Get("Confirm"), onConfirmVersion, rowLeft = rowLeft.RightCopy(10.0), EnumButtonStyle.Normal, "saveButton")
			.EndIf()
			.AddIf(!showCancelOnSelectVersion)
			.AddButton(Lang.Get("Confirm"), onConfirmVersion, rowLeft = rowLeft.BelowCopy(0.0, 100.0), EnumButtonStyle.Normal, "saveButton")
			.EndIf()
			.EndChildElements()
			.Compose();
	}

	private bool onConfirmVersion()
	{
		ElementComposer.GetButton("saveButton").Enabled = false;
		backend.SelectVersion(ElementComposer.GetDropDown("versionSelect").SelectedValue, onRegionSelected);
		return true;
	}

	private bool onConfigureServer()
	{
		dashboardLoading();
		backend.GetConfig(onServerConfigReceived);
		return true;
	}

	private void onServerConfigReceived(EnumAuthServerResponse reqStatus, GameServerConfigResponse response)
	{
		currentScreen = "settings";
		if (reqStatus == EnumAuthServerResponse.Bad)
		{
			setResponseNotifier(reqStatus, null);
			return;
		}
		dashboardReady();
		ElementBounds rowLeft = ElementBounds.Fixed(0.0, 50.0, 200.0, 25.0);
		ElementBounds rowRight = ElementBounds.Fixed(0.0, 50.0, 400.0, 25.0).FixedRightOf(rowLeft);
		ServerConfigPart scfg = response.ServerConfig;
		GuiCompositeSettings.getLanguages(out var langVals, out var langNames);
		int langIndex = langVals.IndexOf(scfg.ServerLanguage);
		ElementComposer = screenBase(showSupportText: false).AddStaticText(Lang.Get("Server configuration"), CairoFont.WhiteSmallishText(), rowLeft = rowLeft.BelowCopy(0.0, -25.0)).AddStaticText(Lang.Get("Server Name"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 10.0)).AddTextInput(rowRight = rowRight.BelowCopy(0.0, 5.0), null, CairoFont.WhiteSmallText(), "serverName")
			.AddStaticText(Lang.Get("Server description"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 10.0))
			.AddTextArea(rowRight = rowRight.BelowCopy(0.0, 15.0).WithFixedHeight(100.0), null, CairoFont.WhiteSmallText(), "serverDescription")
			.AddStaticText(Lang.Get("Welcome message"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 92.0))
			.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 15.0).WithFixedHeight(25.0), null, CairoFont.WhiteSmallText(), "serverMotd")
			.AddStaticText(Lang.Get("Login password"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 15.0))
			.AddTextInput(rowRight = rowRight.BelowCopy(0.0, 15.0).WithFixedHeight(25.0), null, CairoFont.WhiteSmallText(), "serverPassword")
			.AddStaticText(Lang.Get("Language"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 15.0))
			.AddDropDown(langVals, langNames, langIndex, null, rowRight = rowRight.BelowCopy(0.0, 15.0), CairoFont.WhiteSmallText(), "language")
			.AddStaticText(Lang.Get("Allow PvP"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 20.0))
			.AddSwitch(null, rowRight = rowRight.BelowCopy(0.0, 15.0), "serverPvp")
			.AddStaticText(Lang.Get("Allow Fire Spread"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 20.0))
			.AddSwitch(null, rowRight = rowRight.BelowCopy(0.0, 15.0), "serverFireSpread")
			.AddStaticText(Lang.Get("On the public server list"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 20.0))
			.AddSwitch(null, rowRight = rowRight.BelowCopy(0.0, 15.0), "advertise")
			.AddSmallButton(Lang.Get("Cancel"), onCancel, rowLeft = rowLeft.BelowCopy(0.0, 40.0).WithFixedHeight(35.0), EnumButtonStyle.Normal, "saveButton")
			.AddSmallButton(Lang.Get("Save"), onSaveServerConfig, rowLeft.RightCopy(10.0).WithFixedHeight(35.0), EnumButtonStyle.Normal, "cancelButton")
			.AddSmallButton(Lang.Get("Request World Download"), onRequestDownload, rowLeft = rowLeft.BelowCopy(0.0, 40.0), EnumButtonStyle.Small, "serverDownloadWorld")
			.AddSmallButton(Lang.Get("Change Server Version"), onChangeVersion, rowLeft.RightCopy(10.0), EnumButtonStyle.Small, "serverChangeVersion")
			.AddSmallButton(Lang.Get("Delete Savegame"), onDeleteSaves, rowLeft = rowLeft.BelowCopy(0.0, 10.0).WithFixedHeight(25.0), EnumButtonStyle.Small, "deleteButton")
			.AddSmallButton(Lang.Get("Delete everything"), onDeleteEverything, rowLeft = rowLeft.RightCopy(10.0), EnumButtonStyle.Small, "deleteallButton")
			.EndChildElements();
		ElementComposer.Compose();
		ElementComposer.GetTextInput("serverName").SetValue(scfg.ServerName);
		ElementComposer.GetTextArea("serverDescription").SetMaxLines(5);
		ElementComposer.GetTextArea("serverDescription").SetValue(scfg.ServerDescription);
		ElementComposer.GetTextInput("serverPassword").SetValue(scfg.Password);
		ElementComposer.GetTextInput("serverMotd").SetValue(scfg.WelcomeMessage);
		ElementComposer.GetSwitch("serverPvp").SetValue(scfg.AllowPvP);
		ElementComposer.GetSwitch("serverFireSpread").SetValue(scfg.AllowFireSpread);
		ElementComposer.GetSwitch("advertise").SetValue(scfg.AdvertiseServer);
		ElementComposer.GetButton("serverDownloadWorld").Enabled = gameServerStatus.DownloadState == EnumDownloadSavesStatus.Idle;
		if (gameServerStatus.QuantitySavegames == 0)
		{
			ElementComposer.GetButton("deleteButton").Enabled = false;
		}
	}

	private void OnRemoveAllMods(bool b)
	{
		dashboardLoading();
		backend.DeleteAllMods(delegate
		{
			backend.GetConfig(onServerConfigReceived);
		});
	}

	private void OnRemoveSelectedMod(bool b)
	{
		GuiElementDropDown mods = ElementComposer.GetDropDown("mods");
		dashboardLoading();
		backend.DeleteMod(mods.SelectedValue, delegate
		{
			backend.GetConfig(onServerConfigReceived);
		});
	}

	private bool onDeleteEverything()
	{
		currentScreen = "confirmdeleteall";
		ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Really delete your Vintagehosting server and worlds? This action cannot be undone! This action can take up to 30 seconds."), OnDidConfirmDeleteAll, ScreenManager, this));
		return true;
	}

	private bool onDeleteSaves()
	{
		currentScreen = "confirmdelete";
		ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("Really delete the world? This action cannot be undone!"), OnDidConfirmDelete, ScreenManager, this));
		return true;
	}

	private void OnDidConfirmDeleteAll(bool ok)
	{
		if (ok)
		{
			dashboardLoading();
			backend.DeleteAll(delegate(EnumAuthServerResponse resp, GameServerStatus req)
			{
				onStatusReady(resp, req);
				ScreenManager.LoadScreenNoLoadCall(this);
			});
		}
		else
		{
			ScreenManager.LoadScreenNoLoadCall(this);
		}
	}

	private void OnDidConfirmDelete(bool ok)
	{
		if (ok)
		{
			dashboardLoading();
			backend.DeleteSaves(delegate(EnumAuthServerResponse resp, GameServerStatus req)
			{
				onStatusReady(resp, req);
				ScreenManager.LoadScreenNoLoadCall(this);
			});
		}
		else
		{
			ScreenManager.LoadScreenNoLoadCall(this);
		}
	}

	private bool onSaveServerConfig()
	{
		dashboardLoading();
		string scfg = JsonUtil.ToString(new ServerConfigPart
		{
			ServerName = ElementComposer.GetTextInput("serverName").GetText(),
			ServerDescription = ElementComposer.GetTextArea("serverDescription").GetText(),
			WelcomeMessage = ElementComposer.GetTextInput("serverMotd").GetText(),
			ServerLanguage = ElementComposer.GetDropDown("language").SelectedValue,
			AllowPvP = ElementComposer.GetSwitch("serverPvp").On,
			AllowFireSpread = ElementComposer.GetSwitch("serverFireSpread").On,
			AdvertiseServer = ElementComposer.GetSwitch("advertise").On,
			Password = ElementComposer.GetTextInput("serverPassword").GetText()
		});
		backend.SetConfig(onStatusReady, scfg, null);
		return true;
	}

	private bool onChangeVersion()
	{
		showCancelOnSelectVersion = true;
		dashboardLoading();
		backend.GetGameVersions(delegate(EnumAuthServerResponse status, string[] versions)
		{
			screenSelectVersion(versions);
		});
		return true;
	}

	public GuiScreenServerDashboard(ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		backend = new ServerCtrlBackendInterface();
		ShowMainMenu = true;
		wcu = new WorldConfig(screenManager.verifiedMods);
	}

	private void onStatusReady(EnumAuthServerResponse reqStatus, GameServerStatus gameServerStatus)
	{
		this.gameServerStatus = gameServerStatus;
		dashboardReady();
		setResponseNotifier(reqStatus, gameServerStatus?.Reason);
		proberActive = false;
		if (reqStatus != 0)
		{
			return;
		}
		if (gameServerStatus.ActiveserverDays <= 0f)
		{
			screenServerExpired(gameServerStatus.ActiveserverDays);
			return;
		}
		if (gameServerStatus.StatusCode == "selectregion")
		{
			screenSelectRegion(gameServerStatus);
		}
		if (gameServerStatus.StatusCode == "userdoesnotexist")
		{
			showCancelOnSelectVersion = false;
			backend.GetGameVersions(delegate(EnumAuthServerResponse status, string[] versions)
			{
				screenSelectVersion(versions);
			});
		}
		if (gameServerStatus.StatusCode == "ok" || gameServerStatus.StatusCode == "stopped" || gameServerStatus.StatusCode == "running")
		{
			screenServerStatus(gameServerStatus);
		}
	}

	private void screenServerStatus(GameServerStatus gameServerStatus)
	{
		currentScreen = "dashboard";
		int width = 600;
		ElementBounds rowBounds = ElementBounds.Fixed(0.0, 0.0, width, 35.0);
		string statusText = Lang.Get("<font opacity=\"0.6\">Status: </font> {0}", Lang.Get("serverstatus-" + gameServerStatus.StatusCode));
		string downloadStateStr = "";
		if (gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready)
		{
			downloadStateStr = Lang.Get("Your world download is ready, please download it within 24 hours.");
		}
		if (gameServerStatus.DownloadState == EnumDownloadSavesStatus.Copying)
		{
			downloadStateStr = Lang.Get("World download requested, copying in progress...");
		}
		string descText = ((!(gameServerStatus.ActiveserverDays >= 2f)) ? Lang.Get("Your game server (version {0}) is installed and ready to use. You have {1} hours of server time left.", gameServerStatus.Version, (int)(gameServerStatus.ActiveserverDays * 24f)) : Lang.Get("Your game server (version {0}) is installed and ready to use. You have {1} days of server time left.", gameServerStatus.Version, (int)gameServerStatus.ActiveserverDays));
		GuiComposer composer = screenBase(showSupportText: true).BeginChildElements(rowBounds.FlatCopy()).AddStaticText(descText, CairoFont.WhiteSmallText(), rowBounds = rowBounds.BelowCopy(0.0, 80.0));
		CairoFont baseFont = CairoFont.WhiteSmallishText();
		ElementBounds elementBounds = rowBounds.BelowCopy(0.0, 30.0);
		ElementBounds copyBounds;
		ElementBounds stopServerBounds;
		ElementBounds worldSettingsBounds;
		ElementComposer = GuiComposerHelpers.AddRichtext(bounds: rowBounds = elementBounds.BelowCopy(0.0, 5.0), composer: composer.AddRichtext(statusText, baseFont, elementBounds, "serverStatus"), vtmlCode: Lang.Get("<font opacity=\"0.6\">Host:</font> {0}", gameServerStatus.Identifier), baseFont: CairoFont.WhiteSmallishText()).AddAutoSizeHoverText(Lang.Get("With this information other players can connect to your server, be sure to whitelist them however."), CairoFont.WhiteDetailText(), 300, rowBounds.FlatCopy().WithFixedWidth(width - 50)).AddIconButton("copy", OnCopyConnectionString, copyBounds = rowBounds.RightCopy().WithFixedAlignmentOffset(-30.0, 0.0).WithFixedSize(30.0, 30.0))
			.AddAutoSizeHoverText(Lang.Get("Copy host to clipboard"), CairoFont.WhiteSmallText(), 150, copyBounds.FlatCopy())
			.AddRichtext(Lang.Get("<font opacity=\"0.6\">To give players the ability to join your world, join the server and type<br>/player <i>playername</i> whitelist on</font>", gameServerStatus.ConnectionString), CairoFont.WhiteDetailText(), rowBounds = rowBounds.BelowCopy(0.0, 5.0).WithFixedWidth(width).WithAlignment(EnumDialogArea.None))
			.AddSmallButton(Lang.Get("Start Server"), onStartServer, rowBounds = rowBounds.BelowCopy(0.0, 30.0).WithFixedWidth(200.0), EnumButtonStyle.Normal, "startButton")
			.AddSmallButton(Lang.Get("Stop Server"), onStopServer, stopServerBounds = rowBounds.FlatCopy().WithFixedWidth(200.0).RightOf(rowBounds, 10.0), EnumButtonStyle.Normal, "stopButton")
			.AddSmallButton(Lang.Get("Force Stop Server"), onKillServer, rowBounds.FlatCopy().WithFixedWidth(150.0).RightOf(stopServerBounds, 10.0), EnumButtonStyle.Small, "killButton")
			.AddSmallButton(Lang.Get("Server Settings"), onConfigureServer, rowBounds = rowBounds.BelowCopy(0.0, 35.0).WithAlignment(EnumDialogArea.None), EnumButtonStyle.Small, "serverConfigButton")
			.AddIf(gameServerStatus.DownloadState != EnumDownloadSavesStatus.Ready)
			.AddHoverText(Lang.Get("worldsetting-onlywhenstopped"), CairoFont.WhiteDetailText(), 300, rowBounds.FlatCopy())
			.EndIf()
			.AddSmallButton(Lang.Get("World Settings"), onConfigureWorld, worldSettingsBounds = rowBounds.FlatCopy().RightOf(rowBounds, 10.0), EnumButtonStyle.Small, "worldConfigButton")
			.AddIf(gameServerStatus.QuantitySavegames > 0)
			.AddHoverText(Lang.Get("worldsetting-onlyonnewworlds"), CairoFont.WhiteDetailText(), 300, rowBounds.FlatCopy().RightOf(rowBounds, 10.0))
			.EndIf()
			.AddIf(gameServerStatus.DownloadState != EnumDownloadSavesStatus.Idle)
			.AddRichtext(downloadStateStr, CairoFont.WhiteDetailText(), rowBounds = rowBounds.BelowCopy(0.0, 20.0).WithFixedWidth(width).WithAlignment(EnumDialogArea.None), "dlStatusText")
			.AddSmallButton(Lang.Get("Download World"), onDownloadWorldNow, rowBounds.BelowCopy(0.0, -15.0).WithFixedSize(1.0, 1.0).WithAlignment(EnumDialogArea.None)
				.WithFixedPadding(2.0, 2.0), EnumButtonStyle.Small, "worldDownloadButton")
			.EndIf()
			.AddButton(Lang.Get("Join Server"), onJoinServer, rowBounds.BelowCopy(0.0, 50.0).WithFixedWidth(200.0).WithFixedPadding(20.0, 0.0)
				.WithAlignment(EnumDialogArea.CenterFixed), EnumButtonStyle.Normal, "joinButton")
			.EndChildElements()
			.EndChildElements()
			.Compose();
		if (ElementComposer.GetButton("worldDownloadButton") != null)
		{
			ElementComposer.GetButton("worldDownloadButton").Enabled = gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready;
		}
		dashboardReady();
		connectionString = gameServerStatus.ConnectionString;
		identifier = gameServerStatus.Identifier;
		_password = gameServerStatus.Password;
		if (gameServerStatus.DownloadState == EnumDownloadSavesStatus.Copying)
		{
			runDownloadStatusProber();
		}
	}

	private bool onShowLog()
	{
		dashboardLoading();
		backend.GetLog(screenLog);
		return true;
	}

	private void screenLog(EnumAuthServerResponse reqStatus, GameServerLogResponse response)
	{
		currentScreen = "logscreen";
		dashboardReady();
		logText = string.Join("\n", response.Log);
		ElementBounds rowLeft = ElementBounds.Fixed(-25.0, 45.0, 200.0, 25.0);
		ElementBounds logtextBounds = ElementBounds.Fixed(-30.0, 80.0, 610.0, 700.0);
		ElementBounds clippingBounds = logtextBounds.ForkBoundingParent();
		ElementBounds insetBounds = logtextBounds.FlatCopy().FixedGrow(6.0).WithFixedOffset(-3.0, -3.0);
		ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(logtextBounds.fixedWidth + 7.0).WithFixedWidth(20.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(10.0);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		bgBounds.WithChildren(insetBounds, clippingBounds, scrollbarBounds);
		ElementBounds copyBounds;
		ElementComposer = screenBase(showSupportText: true).AddStaticText("Server Logs:", CairoFont.WhiteSmallishText(), rowLeft).AddSmallButton(Lang.Get("Back"), onCancel, rowLeft = rowLeft.BelowCopy(0.0, logtextBounds.fixedHeight + 40.0).WithFixedHeight(35.0), EnumButtonStyle.Normal, "backButton").AddIconButton("copy", OnCopyLog, copyBounds = rowLeft.RightCopy(10.0, 2.0).WithFixedSize(30.0, 30.0))
			.AddAutoSizeHoverText(Lang.Get("Copy Log to clipboard"), CairoFont.WhiteSmallText(), 250, copyBounds.FlatCopy())
			.BeginChildElements(bgBounds)
			.BeginClip(clippingBounds)
			.AddInset(insetBounds, 3)
			.AddDynamicText("", CairoFont.WhiteDetailText(), logtextBounds, "logtext")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarvalue, scrollbarBounds, "scrollbar")
			.EndChildElements()
			.Compose();
		GuiElementDynamicText dynamicText = ElementComposer.GetDynamicText("logtext");
		dynamicText.AutoHeight();
		dynamicText.SetNewText(logText);
		dynamicText.Enabled = false;
		GuiElementScrollbar scrollbar = ElementComposer.GetScrollbar("scrollbar");
		scrollbar.SetHeights(600f, (float)logtextBounds.fixedHeight);
		scrollbar.CurrentYPosition = (float)logtextBounds.fixedHeight - 680f;
		dynamicText.Bounds.fixedY = 0.0 - logtextBounds.fixedHeight + 680.0;
		dynamicText.Bounds.CalcWorldBounds();
	}

	private void OnCopyLog(bool ok)
	{
		if (logText == null)
		{
			logText = string.Empty;
		}
		ScreenManager.Platform.XPlatInterface.SetClipboardText(logText);
	}

	private void OnNewScrollbarvalue(float value)
	{
		GuiElementDynamicText dynamicText = ElementComposer.GetDynamicText("logtext");
		dynamicText.Bounds.fixedY = 3f - value;
		dynamicText.Bounds.CalcWorldBounds();
	}

	private void probeStatus()
	{
		backend.GetStatus(delegate(EnumAuthServerResponse st1, GameServerStatus gameserverStatus)
		{
			if (ScreenManager.CurrentScreen == this)
			{
				onStatusReady(st1, gameserverStatus);
				runServerStatusProber(gameserverStatus.StatusCode);
			}
		});
	}

	private bool onConfigureWorld()
	{
		dashboardLoading();
		backend.GetConfig(onWorldConfigReceived);
		return true;
	}

	private bool onCancel()
	{
		backend.GetStatus(onStatusReady);
		screenLoading();
		return true;
	}

	private void onWorldConfigReceived(EnumAuthServerResponse reqStatus, GameServerConfigResponse response)
	{
		wcu.IsNewWorld = gameServerStatus.QuantitySavegames == 0;
		(wcu.Jworldconfig.Token as JObject).Merge(response.WorldConfig.Token, new JsonMergeSettings
		{
			MergeArrayHandling = MergeArrayHandling.Replace
		});
		currentScreen = "worldconfig";
		customizeScreen = new GuiScreenWorldCustomize(OnReturnFromCustomizer, ScreenManager, this, wcu.Clone(), null);
		ScreenManager.LoadScreen(customizeScreen);
	}

	private void OnReturnFromCustomizer(bool didApply)
	{
		if (didApply)
		{
			wcu = customizeScreen.wcu;
			wcu.Jworldconfig.Token["Seed"] = wcu.Seed;
			wcu.Jworldconfig.Token["MapSizeY"] = wcu.MapsizeY;
			string worldconfig = wcu.Jworldconfig.ToString();
			backend.SetConfig(onStatusReady, null, worldconfig);
		}
		ScreenManager.LoadScreen(this);
	}

	private void OnCopyConnectionString(bool ok)
	{
		ScreenManager.Platform.XPlatInterface.SetClipboardText(identifier);
	}

	private bool onJoinServer()
	{
		ScreenManager.ConnectToMultiplayer(connectionString, _password);
		return true;
	}

	private void dashboardLoading()
	{
		foreach (KeyValuePair<string, GuiElement> interactiveElement in ElementComposer.interactiveElements)
		{
			if (interactiveElement.Value is GuiElementTextButton btn)
			{
				btn.Enabled = false;
			}
		}
	}

	private void dashboardReady()
	{
		if (ScreenManager.CurrentScreen != this)
		{
			return;
		}
		foreach (KeyValuePair<string, GuiElement> interactiveElement in ElementComposer.interactiveElements)
		{
			if (interactiveElement.Value is GuiElementTextButton btn)
			{
				btn.Enabled = true;
			}
		}
		if (ElementComposer.GetButton("joinButton") != null)
		{
			ElementComposer.GetButton("joinButton").Enabled = gameServerStatus.StatusCode == "running";
		}
		if (ElementComposer.GetButton("serverConfigButton") != null)
		{
			ElementComposer.GetButton("serverConfigButton").Enabled = gameServerStatus.StatusCode == "stopped";
			ElementComposer.GetButton("worldConfigButton").Enabled = gameServerStatus.StatusCode == "stopped";
		}
		if (ElementComposer.GetButton("worldConfigButton") != null && gameServerStatus.QuantitySavegames > 0)
		{
			ElementComposer.GetButton("worldConfigButton").Enabled = false;
		}
		if (ElementComposer.GetButton("worldDownloadButton") != null)
		{
			ElementComposer.GetButton("worldDownloadButton").Enabled = gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready;
		}
	}

	private bool onKillServer()
	{
		ScreenManager.LoadScreen(new GuiScreenConfirmAction(Lang.Get("vintagehostingserver-confirmkill"), OnDidConfirmKillServer, ScreenManager, this));
		return true;
	}

	private void OnDidConfirmKillServer(bool ok)
	{
		if (ok)
		{
			dashboardLoading();
			backend.ForceStop(onStopped);
		}
		ScreenManager.LoadScreen(this);
	}

	private bool onStopServer()
	{
		dashboardLoading();
		backend.Stop(onStopped);
		return true;
	}

	private void onStopped(EnumAuthServerResponse reqStatus, ServerCtrlResponse response)
	{
		if (ElementComposer.GetRichtext("serverStatus") != null)
		{
			if (reqStatus == EnumAuthServerResponse.Offline)
			{
				setResponseNotifier(reqStatus, null);
			}
			else
			{
				serverStatusProbingTries = 7;
				desireState = "stopped";
				runServerStatusProber(response.StatusCode);
			}
		}
		dashboardReady();
	}

	private bool onStartServer()
	{
		dashboardLoading();
		backend.Start(onStarted);
		return true;
	}

	private void onStarted(EnumAuthServerResponse reqStatus, ServerCtrlResponse response)
	{
		if (ElementComposer.GetRichtext("serverStatus") != null)
		{
			if (reqStatus == EnumAuthServerResponse.Offline)
			{
				setResponseNotifier(reqStatus, null);
			}
			else
			{
				desireState = "running";
				serverStatusProbingTries = 7;
				runServerStatusProber(response.StatusCode);
			}
		}
		dashboardReady();
	}

	private void runServerStatusProber(string statuscode)
	{
		string statusText = Lang.Get("<font opacity=\"0.6\">Status:</font> {0}", Lang.Get("serverstatus-" + statuscode));
		if (statuscode != desireState && !proberActive)
		{
			statusText = Lang.Get("<font opacity=\"0.6\">Status:</font> Loading...");
			serverStatusProbingTries--;
			if (serverStatusProbingTries > 0)
			{
				proberActive = true;
				ScreenManager.EnqueueCallBack(probeStatus, 3000);
			}
			else
			{
				statusText = Lang.Get("<font opacity=\"0.6\">Status:</font> Timeout");
			}
		}
		ElementComposer.GetRichtext("serverStatus")?.SetNewText(statusText, CairoFont.WhiteSmallishText());
	}

	private void setResponseNotifier(EnumAuthServerResponse reqStatus, string invalidReason)
	{
		GuiElementRichtext elem = ElementComposer.GetRichtext("notificationtext");
		switch (reqStatus)
		{
		case EnumAuthServerResponse.Offline:
		{
			CairoFont font = CairoFont.WhiteDetailText().WithColor(GuiStyle.ErrorTextColor);
			elem.SetNewText("Unable to connect to auth server, server either offline or no internet connection.", font);
			break;
		}
		case EnumAuthServerResponse.Good:
		{
			CairoFont font3 = CairoFont.WhiteDetailText().WithColor(GuiStyle.SuccessTextColor);
			elem.SetNewText("Request succesfull", font3);
			break;
		}
		case EnumAuthServerResponse.Bad:
		{
			CairoFont font2 = CairoFont.WhiteDetailText().WithColor(GuiStyle.ErrorTextColor);
			if (invalidReason == null)
			{
				elem.SetNewText("Bad response from server. Programming error. Please send us a support ticket with your client-main.log log file attached (its in %appdata%/VintageStoryData/Logs).", font2);
				break;
			}
			string text = ((!Lang.HasTranslation("vintagehosting-response-" + invalidReason)) ? Lang.Get("vintagehosting-response-badrequest", invalidReason) : Lang.Get("vintagehosting-response-" + invalidReason));
			elem.SetNewText(text, font2);
			break;
		}
		}
	}

	private void screenLoading()
	{
		currentScreen = "loading";
		ElementBounds rowLeft = ElementBounds.Fixed(0.0, 50.0, 300.0, 35.0).WithAlignment(EnumDialogArea.CenterFixed);
		ElementComposer = screenBase(showSupportText: true).AddRichtext(Lang.Get("Loading..."), CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), rowLeft = rowLeft.BelowCopy(0.0, 30.0), "loadingText").EndChildElements().Compose();
	}

	private GuiComposer screenBase(bool showSupportText)
	{
		ElementBounds rowLeft = ElementBounds.Fixed(0.0, 0.0, 400.0, 35.0).WithAlignment(EnumDialogArea.CenterFixed);
		ElementBounds supportBounds = ElementBounds.Fixed(0.0, 0.0, 595.0, 35.0).WithAlignment(EnumDialogArea.CenterBottom);
		CairoFont font = CairoFont.WhiteDetailText().WithColor(GuiStyle.ErrorTextColor).WithOrientation(EnumTextOrientation.Center);
		return dialogBase("mainmenu-servercontrol-dashboard", 650.0).AddRichtext("", font, ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 0.0, 550.0, 60.0), "notificationtext").AddIf(showSupportText).AddRichtext(Lang.Get("serverctrl-getsupport"), CairoFont.WhiteSmallText(), supportBounds)
			.EndIf()
			.BeginChildElements(ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 30.0, 600.0, 700.0))
			.AddStaticText(Lang.Get("serverctrl-dashboard"), CairoFont.WhiteMediumText().WithOrientation(EnumTextOrientation.Center), rowLeft);
	}

	public override void OnScreenLoaded()
	{
		backend.GetStatus(onStatusReady);
		screenLoading();
		wcu.LoadPlayStyles();
		if (wcu.PlayStyles.Count > 0)
		{
			wcu.selectPlayStyle(0);
		}
	}

	public override void RenderAfterFinalComposition(float dt)
	{
		base.RenderAfterFinalComposition(dt);
		accum += dt;
		if (backend.IsLoading && (double)accum > 0.25)
		{
			accum = 0f;
			int num = (int)(ScreenManager.GamePlatform.EllapsedMs / 500 % 3);
			ElementComposer.GetRichtext("loadingText")?.SetNewText(loadtexts[num], CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center));
		}
	}

	private string prettyServerTimeLeft(float days)
	{
		if (days < 0f)
		{
			if (days > -1f)
			{
				return Lang.Get("Your active server time expired {0:0.#} hours ago.", (0f - days) * 24f);
			}
			int idays = (int)(0f - days);
			float hours = (0f - days - (float)idays) * 24f;
			if (idays == 1)
			{
				return Lang.Get("Your active server time expired 1 day and {0:0.#} hours ago.", hours);
			}
			return Lang.Get("Your active server time expired {0:0.#} days and {1:0.#} hours ago.", idays, hours);
		}
		if (days < 1f)
		{
			return Lang.Get("Your active server time will expire in {0:0.#} hours.", days * 24f);
		}
		int idays2 = (int)days;
		float hours2 = (days - (float)idays2) * 24f;
		if (idays2 == 1)
		{
			return Lang.Get("Your active server time will expire in 1 day and {0:0.#} hours.", hours2);
		}
		return Lang.Get("Your active server time will expire in {0:0.#} days and {1:0.#} hours", idays2, hours2);
	}

	private void screenServerExpired(float daysActive)
	{
		currentScreen = "expired";
		ElementBounds rowLeft = ElementBounds.Fixed(0.0, 50.0, 500.0, 35.0).WithAlignment(EnumDialogArea.CenterFixed);
		ElementComposer = screenBase(showSupportText: true).AddStaticText(prettyServerTimeLeft(daysActive), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 30.0)).AddRichtext(Lang.Get("serverctrl-expireddesc"), CairoFont.WhiteSmallText(), rowLeft = rowLeft.BelowCopy(0.0, 5.0), "richtext");
		ElementComposer.GetRichtext("richtext").BeforeCalcBounds();
		string downloadStateStr = "";
		if (gameServerStatus.DownloadState == EnumDownloadSavesStatus.Ready)
		{
			downloadStateStr = Lang.Get("Your world download is ready, please download it within 24 hours.");
		}
		if (gameServerStatus.DownloadState == EnumDownloadSavesStatus.Copying)
		{
			downloadStateStr = Lang.Get("World download requested, copying in progress...");
		}
		if (gameServerStatus.DownloadState != 0)
		{
			ElementComposer.AddRichtext(downloadStateStr, CairoFont.WhiteDetailText(), rowLeft = rowLeft.BelowCopy(0.0, 30.0), "dlStatusText").AddSmallButton(Lang.Get("Download World"), onDownloadWorldNow, rowLeft = rowLeft.BelowCopy(0.0, -55.0).WithFixedSize(1.0, 1.0).WithFixedPadding(5.0, 3.0), EnumButtonStyle.Small, "worldDownloadButton");
		}
		else
		{
			ElementComposer.AddSmallButton(Lang.Get("Request world download"), onRequestDownload, rowLeft = rowLeft.BelowCopy(0.0, 30.0).WithFixedHeight(40.0), EnumButtonStyle.Normal, "requestDlExpired");
		}
		ElementComposer.AddButton(Lang.Get("Back to main menu"), onMainMenu, rowLeft = rowLeft.BelowCopy(0.0, 50.0).WithFixedHeight(40.0), EnumButtonStyle.Normal, "menuButton").EndChildElements().Compose();
		if (gameServerStatus.DownloadState == EnumDownloadSavesStatus.Copying)
		{
			ElementComposer.GetButton("worldDownloadButton").Enabled = false;
		}
	}

	private bool onMainMenu()
	{
		ScreenManager.StartMainMenu();
		return true;
	}
}
