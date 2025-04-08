using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.Client.NoObf;

internal class GuiDialogEscapeMenu : GuiDialog, IGameSettingsHandler, IGuiCompositeHandler
{
	private GuiCompositeSettings gameSettingsMenu;

	private string internethosting = "";

	private string internethostingrevealed;

	private string internethostingtooltip = "";

	private bool internethostingWarn;

	private string advertiseStatus;

	private ClientMain game;

	private string localip = RuntimeEnv.GetLocalIpAddress();

	private CairoFont inetHostingFont;

	private int confirmType;

	private bool wasInGraphics;

	public bool IsIngame => true;

	public override double InputOrder => 0.0;

	public override double DrawOrder => 0.89;

	public GuiComposerManager GuiComposers => (capi.World as ClientMain).GuiComposers;

	public GuiComposer GuiComposerForRender
	{
		get
		{
			return base.SingleComposer;
		}
		set
		{
			base.SingleComposer = value;
		}
	}

	public int? MaxViewDistanceAlarmValue
	{
		get
		{
			if (game.IsSingleplayer)
			{
				return null;
			}
			return (capi.World as ClientMain).WorldMap.MaxViewDistance;
		}
	}

	public override string ToggleKeyCombinationCode => "escapemenudialog";

	public override bool DisableMouseGrab => true;

	public ICoreClientAPI Api => capi;

	public void LoadComposer(GuiComposer composer)
	{
		base.SingleComposer = composer;
	}

	public GuiDialogEscapeMenu(ICoreClientAPI capi)
		: base(capi)
	{
		gameSettingsMenu = new GuiCompositeSettings(this, onMainScreen: false);
		game = capi.World as ClientMain;
		game.eventManager.OnGameWindowFocus.Add(OnWindowFocusChanged);
		game.eventManager.OnNewServerToClientChatLine.Add(OnServerChatLine);
		EscapeMenuHome();
	}

	private void OnServerChatLine(int groupId, string message, EnumChatType chattype, string data)
	{
		if (groupId != GlobalConstants.ServerInfoChatGroup || !game.IsSingleplayer)
		{
			return;
		}
		bool num = data?.StartsWithOrdinal("foundnatdevice:") ?? false;
		bool foundNatPrivate = data?.StartsWithOrdinal("foundnatdeviceprivip:") ?? false;
		bool notFoundNat = data?.StartsWithOrdinal("nonatdevice") ?? false;
		if (num || foundNatPrivate)
		{
			string ip = data.Split(new string[1] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1];
			internethosting = Lang.Get("singleplayer-hosting-inet", ip, foundNatPrivate ? "...?" : "");
			internethostingrevealed = Lang.Get("singleplayer-hosting-inet-iprevealed", ip, foundNatPrivate ? "...?" : "");
			if (foundNatPrivate)
			{
				internethostingWarn = true;
				internethostingtooltip = Lang.Get("opentolan-foundnatprivate");
			}
			EscapeMenuHome();
		}
		if (notFoundNat)
		{
			internethostingWarn = true;
			internethosting = Lang.Get("Internet hosting failed");
			internethostingtooltip = Lang.Get("No UPnP or NAT-PMP device found. Please make sure your router has UPnP enabled.");
			EscapeMenuHome();
		}
		if (data == "masterserverstatus:ok")
		{
			advertiseStatus = "ok";
			EscapeMenuHome();
		}
		if (data == "masterserverstatus:fail")
		{
			advertiseStatus = "fail";
			EscapeMenuHome();
		}
	}

	private void OnWindowFocusChanged(bool focus)
	{
		if (ClientSettings.PauseGameOnLostFocus && !focus)
		{
			TryOpen();
		}
	}

	public override void OnGuiOpened()
	{
		gameSettingsMenu.IsInCreativeMode = game.player.worlddata.CurrentGameMode == EnumGameMode.Creative;
		EscapeMenuHome();
		game.ShouldRender2DOverlays = true;
		if (!game.OpenedToLan)
		{
			game.PauseGame(paused: true);
		}
	}

	internal void EscapeMenuHome()
	{
		if (internethosting == null || internethosting == "")
		{
			internethosting = Lang.Get("Searching for UPnP devices...");
		}
		inetHostingFont = CairoFont.WhiteSmallText();
		if (internethostingWarn || advertiseStatus == "fail")
		{
			inetHostingFont = inetHostingFont.WithColor(new double[4]
			{
				241.0 / 255.0,
				191.0 / 255.0,
				7.0 / 15.0,
				1.0
			});
		}
		if (advertiseStatus == "ok")
		{
			inetHostingFont = inetHostingFont.WithColor(new double[4]
			{
				134.0 / 255.0,
				71.0 / 85.0,
				28.0 / 51.0,
				1.0
			});
			internethostingtooltip = internethostingtooltip + ((internethostingtooltip.Length > 0) ? "\n\n" : "") + Lang.Get("Registration at the master server successfull and external connections are working!");
		}
		if (advertiseStatus == "fail")
		{
			internethostingtooltip = internethostingtooltip + ((internethostingtooltip.Length > 0) ? "\n\n" : "") + Lang.Get("Registration at the master server was not successfull, external connections probably blocked by firewall");
		}
		double buttonWidth = 330.0;
		float bposy = 1.5f;
		ClearComposers();
		GuiComposer guiComposer = game.GuiComposers.Create("escapemenu", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding), withTitleBar: false).BeginChildElements()
			.AddStaticText((game.IsSingleplayer && !game.OpenedToLan) ? Lang.Get("game-ispaused") : Lang.Get("game-isrunning"), CairoFont.WhiteSmallishText().WithFontSize(25f), EnumTextOrientation.Center, ElementStdBounds.MenuButton(0f).WithFixedWidth(buttonWidth))
			.AddIf(game.OpenedToLan)
			.AddRichtext(Lang.Get("singleplayer-hosting-local"), CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), ElementStdBounds.MenuButton(0.37f).WithFixedWidth(buttonWidth), revealLocalIp, "hosttext")
			.AddHoverText(Lang.Get("game-spidergame"), CairoFont.WhiteDetailText(), 350, ElementStdBounds.MenuButton(0.37f).WithFixedSize(buttonWidth, 14.0))
			.AddIf(game.OpenedToInternet)
			.AddRichtext(internethosting, inetHostingFont.WithOrientation(EnumTextOrientation.Center), ElementStdBounds.MenuButton(0.61f).WithFixedWidth(buttonWidth), revealInetIp, "internethosttext")
			.AddIf(internethostingtooltip != null && internethostingtooltip.Length > 0)
			.AddHoverText(internethostingtooltip, CairoFont.WhiteDetailText(), 350, ElementStdBounds.MenuButton(0.61f).WithFixedSize(buttonWidth, 14.0))
			.EndIf()
			.EndIf()
			.EndIf()
			.AddButton(Lang.Get("pause-back2game"), OnBackToGame, ElementStdBounds.MenuButton(1f).WithFixedWidth(buttonWidth))
			.AddButton(Lang.Get("mainmenu-settings"), gameSettingsMenu.OpenSettingsMenu, ElementStdBounds.MenuButton(bposy).WithFixedWidth(buttonWidth))
			.Execute(delegate
			{
				bposy += 0.55f;
			})
			.AddIf(game.IsSingleplayer && !game.OpenedToLan)
			.Execute(delegate
			{
				bposy += 0.3f;
			})
			.AddButton(Lang.Get("pause-open2lan"), onOpenToLan, ElementStdBounds.MenuButton(bposy).WithFixedWidth(buttonWidth), CairoFont.ButtonText().WithFontSize(18f))
			.EndIf()
			.AddIf(game.IsSingleplayer && game.OpenedToLan && !game.OpenedToInternet)
			.Execute(delegate
			{
				bposy += 0.35f;
			})
			.AddButton(Lang.Get("pause-open2internet"), onOpenToInternet, ElementStdBounds.MenuButton(bposy).WithFixedWidth(buttonWidth), CairoFont.ButtonText().WithFontSize(18f))
			.EndIf();
		ClientPlayer player = game.player;
		GuiComposer guiComposer2 = guiComposer.AddIf(player != null && (player.WorldData?.CurrentGameMode).GetValueOrDefault() == EnumGameMode.Survival).Execute(delegate
		{
			bposy += 0.45f;
		}).AddButton(Lang.Get("pause-survivalguide"), openSurvivalGuide, ElementStdBounds.MenuButton(bposy).WithFixedWidth(buttonWidth), CairoFont.ButtonText().WithFontSize(18f))
			.EndIf();
		ClientPlayer player2 = game.player;
		base.SingleComposer = guiComposer2.AddIf(player2 != null && (player2.WorldData?.CurrentGameMode).GetValueOrDefault() == EnumGameMode.Creative).Execute(delegate
		{
			bposy += 0.45f;
		}).AddButton(Lang.Get("pause-commandhandbook"), openCommandHandbook, ElementStdBounds.MenuButton(bposy).WithFixedWidth(buttonWidth), CairoFont.ButtonText().WithFontSize(18f))
			.EndIf()
			.AddButton(game.IsSingleplayer ? Lang.Get("pause-savequit") : Lang.Get("pause-disconnect"), OnLeaveWorld, ElementStdBounds.MenuButton(bposy + 1f).WithFixedWidth(buttonWidth))
			.EndChildElements()
			.Compose(focusFirstElement: false);
	}

	private void revealLocalIp(LinkTextComponent component)
	{
		base.SingleComposer.GetRichtext("hosttext")?.SetNewText(Lang.Get("Hosting local game at {0}", localip), CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center));
	}

	private void revealInetIp(LinkTextComponent component)
	{
		base.SingleComposer.GetRichtext("internethosttext")?.SetNewText(internethostingrevealed, inetHostingFont);
	}

	private bool openCommandHandbook()
	{
		new LinkTextComponent(Api, "none", CairoFont.SmallTextInput(), null).SetHref("commandhandbook://").Trigger();
		TryClose();
		return true;
	}

	private bool openSurvivalGuide()
	{
		new LinkTextComponent(Api, "none", CairoFont.SmallTextInput(), null).SetHref("handbook://craftinginfo-starterguide").Trigger();
		TryClose();
		return true;
	}

	private bool onOpenToLan()
	{
		RequireConfirm(0, Lang.Get("confirm-opentolan"));
		return true;
	}

	private bool onOpenToInternet()
	{
		RequireConfirm(1, Lang.Get("confirm-opentointernet"), checkbox: true);
		return true;
	}

	private bool RequireConfirm(int type, string text, bool checkbox = false)
	{
		confirmType = type;
		float offY = (checkbox ? 1 : 0);
		ClearComposers();
		base.SingleComposer = game.GuiComposers.Create("escapemenu-confirm", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding), withTitleBar: false).BeginChildElements()
			.AddStaticText(Lang.Get("Please Confirm"), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(0.1f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(370.0))
			.AddStaticText(text, CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(1f, 0.0, EnumDialogArea.LeftFixed).WithFixedSize(500.0, 200.0))
			.AddIf(checkbox)
			.AddSwitch(delegate
			{
			}, ElementStdBounds.Rowed(4f, 0.0), "switch")
			.AddStaticText(Lang.Get("Publicly advertise the server for everyone to join"), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(4f, 4.0).WithFixedOffset(40.0, 0.0).WithFixedWidth(450.0))
			.EndIf()
			.AddButton(Lang.Get("Cancel"), OnCancel, ElementStdBounds.Rowed(4.3f + offY, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0).WithFixedAlignmentOffset(-10.0, 0.0))
			.AddButton(Lang.Get("Confirm"), OnConfirm, ElementStdBounds.Rowed(4.3f + offY, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
			.EndChildElements()
			.Compose(focusFirstElement: false);
		if (checkbox)
		{
			base.SingleComposer.GetSwitch("switch").On = true;
		}
		return true;
	}

	private bool OnConfirm()
	{
		if (confirmType == 0)
		{
			game.OpenedToLan = true;
			game.PauseGame(paused: false);
			EscapeMenuHome();
			game.SendPacketClient(ClientPackets.Chat(GlobalConstants.GeneralChatGroup, "/allowlan 1"));
		}
		if (confirmType == 1)
		{
			bool advertise = base.SingleComposer.GetSwitch("switch").On;
			game.OpenedToInternet = true;
			EscapeMenuHome();
			game.SendPacketClient(ClientPackets.Chat(GlobalConstants.GeneralChatGroup, "/upnp 1"));
			game.SendPacketClient(ClientPackets.Chat(GlobalConstants.GeneralChatGroup, "/serverconfig advertise " + (advertise ? "1" : "0")));
		}
		return true;
	}

	private bool OnCancel()
	{
		EscapeMenuHome();
		return true;
	}

	public bool OnBackPressed()
	{
		EscapeMenuHome();
		return true;
	}

	internal bool OnBackToGame()
	{
		game.Logger.VerboseDebug("Back to game clicked");
		TryClose();
		game.Logger.VerboseDebug("Escape menu closed");
		return true;
	}

	public override void OnGuiClosed()
	{
		game.api.eventapi.PushEvent("leftGraphicsDlg");
		base.OnGuiClosed();
		game.PauseGame(paused: false);
	}

	internal bool OnLeaveWorld()
	{
		game.SendLeave(0);
		game.exitReason = "leave world button pressed";
		game.DestroyGameSession(gotDisconnected: false);
		return true;
	}

	public override bool OnEscapePressed()
	{
		if (!gameSettingsMenu.IsCapturingHotKey)
		{
			return base.OnEscapePressed();
		}
		return false;
	}

	internal override bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
	{
		if (IsOpened() || game.DialogsOpened == 0)
		{
			return base.OnKeyCombinationToggle(viaKeyComb);
		}
		return false;
	}

	public override bool CaptureAllInputs()
	{
		return IsOpened();
	}

	public override bool CaptureRawMouse()
	{
		return IsOpened();
	}

	bool IGameSettingsHandler.LeaveSettingsMenu()
	{
		EscapeMenuHome();
		return true;
	}

	public override void OnKeyDown(KeyEvent args)
	{
		gameSettingsMenu.OnKeyDown(args);
		base.OnKeyDown(args);
		args.Handled = true;
	}

	public override void OnKeyUp(KeyEvent args)
	{
		gameSettingsMenu.OnKeyUp(args);
		base.OnKeyUp(args);
		args.Handled = true;
	}

	public override void OnMouseDown(MouseEvent args)
	{
		gameSettingsMenu.OnMouseDown(args);
		base.OnMouseDown(args);
		args.Handled = true;
	}

	public override void OnMouseUp(MouseEvent args)
	{
		gameSettingsMenu.OnMouseUp(args);
		base.OnMouseUp(args);
		args.Handled = true;
	}

	public void ReloadShaders()
	{
		ShaderRegistry.ReloadShaders();
		game.eventManager?.TriggerReloadShaders();
	}

	public override void OnRenderGUI(float deltaTime)
	{
		bool nowInGraphics = base.SingleComposer?.DialogName.StartsWithOrdinal("gamesettings-graphics") ?? false;
		if (wasInGraphics != nowInGraphics)
		{
			if (!nowInGraphics)
			{
				game.api.eventapi.PushEvent("leftGraphicsDlg");
			}
			else
			{
				game.api.eventapi.PushEvent("enteredGraphicsDlg");
			}
			wasInGraphics = nowInGraphics;
		}
		base.OnRenderGUI(deltaTime);
	}

	public override void OnFinalizeFrame(float dt)
	{
		base.OnFinalizeFrame(dt);
	}

	public override void Dispose()
	{
		if ((capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-graphicsingame", out var cp))
		{
			cp.Dispose();
		}
		if ((capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-controlsingame", out cp))
		{
			cp.Dispose();
		}
		if ((capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-interfaceoptionsingame", out cp))
		{
			cp.Dispose();
		}
		if ((capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-soundoptionsingame", out cp))
		{
			cp.Dispose();
		}
		if ((capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-developeroptionsingame", out cp))
		{
			cp.Dispose();
		}
		base.Dispose();
	}

	public GuiComposer dialogBase(string name, double width = -1.0, double height = -1.0)
	{
		throw new NotImplementedException();
	}

	public void OnMacroEditor()
	{
		TryClose();
		game.LoadedGuis.FirstOrDefault((GuiDialog dlg) => dlg is GuiDialogMacroEditor)?.TryOpen();
	}
}
