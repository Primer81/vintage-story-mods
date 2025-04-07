#region Assembly VintagestoryLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

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
        bool flag = data?.StartsWithOrdinal("foundnatdeviceprivip:") ?? false;
        bool flag2 = data?.StartsWithOrdinal("nonatdevice") ?? false;
        if (num || flag)
        {
            string text = data.Split(new string[1] { ":" }, StringSplitOptions.RemoveEmptyEntries)[1];
            internethosting = Lang.Get("singleplayer-hosting-inet", text, flag ? "...?" : "");
            internethostingrevealed = Lang.Get("singleplayer-hosting-inet-iprevealed", text, flag ? "...?" : "");
            if (flag)
            {
                internethostingWarn = true;
                internethostingtooltip = Lang.Get("opentolan-foundnatprivate");
            }

            EscapeMenuHome();
        }

        if (flag2)
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

        double width = 330.0;
        float bposy = 1.5f;
        ClearComposers();
        GuiComposer guiComposer = game.GuiComposers.Create("escapemenu", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding), withTitleBar: false).BeginChildElements()
            .AddStaticText((game.IsSingleplayer && !game.OpenedToLan) ? Lang.Get("game-ispaused") : Lang.Get("game-isrunning"), CairoFont.WhiteSmallishText().WithFontSize(25f), EnumTextOrientation.Center, ElementStdBounds.MenuButton(0f).WithFixedWidth(width))
            .AddIf(game.OpenedToLan)
            .AddRichtext(Lang.Get("singleplayer-hosting-local"), CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), ElementStdBounds.MenuButton(0.37f).WithFixedWidth(width), revealLocalIp, "hosttext")
            .AddHoverText(Lang.Get("game-spidergame"), CairoFont.WhiteDetailText(), 350, ElementStdBounds.MenuButton(0.37f).WithFixedSize(width, 14.0))
            .AddIf(game.OpenedToInternet)
            .AddRichtext(internethosting, inetHostingFont.WithOrientation(EnumTextOrientation.Center), ElementStdBounds.MenuButton(0.61f).WithFixedWidth(width), revealInetIp, "internethosttext")
            .AddIf(internethostingtooltip != null && internethostingtooltip.Length > 0)
            .AddHoverText(internethostingtooltip, CairoFont.WhiteDetailText(), 350, ElementStdBounds.MenuButton(0.61f).WithFixedSize(width, 14.0))
            .EndIf()
            .EndIf()
            .EndIf()
            .AddButton(Lang.Get("pause-back2game"), OnBackToGame, ElementStdBounds.MenuButton(1f).WithFixedWidth(width))
            .AddButton(Lang.Get("mainmenu-settings"), gameSettingsMenu.OpenSettingsMenu, ElementStdBounds.MenuButton(bposy).WithFixedWidth(width))
            .Execute(delegate
            {
                bposy += 0.55f;
            })
            .AddIf(game.IsSingleplayer && !game.OpenedToLan)
            .Execute(delegate
            {
                bposy += 0.3f;
            })
            .AddButton(Lang.Get("pause-open2lan"), onOpenToLan, ElementStdBounds.MenuButton(bposy).WithFixedWidth(width), CairoFont.ButtonText().WithFontSize(18f))
            .EndIf()
            .AddIf(game.IsSingleplayer && game.OpenedToLan && !game.OpenedToInternet)
            .Execute(delegate
            {
                bposy += 0.35f;
            })
            .AddButton(Lang.Get("pause-open2internet"), onOpenToInternet, ElementStdBounds.MenuButton(bposy).WithFixedWidth(width), CairoFont.ButtonText().WithFontSize(18f))
            .EndIf();
        ClientPlayer player = game.player;
        GuiComposer guiComposer2 = guiComposer.AddIf(player != null && (player.WorldData?.CurrentGameMode).GetValueOrDefault() == EnumGameMode.Survival).Execute(delegate
        {
            bposy += 0.45f;
        }).AddButton(Lang.Get("pause-survivalguide"), openSurvivalGuide, ElementStdBounds.MenuButton(bposy).WithFixedWidth(width), CairoFont.ButtonText().WithFontSize(18f))
            .EndIf();
        ClientPlayer player2 = game.player;
        base.SingleComposer = guiComposer2.AddIf(player2 != null && (player2.WorldData?.CurrentGameMode).GetValueOrDefault() == EnumGameMode.Creative).Execute(delegate
        {
            bposy += 0.45f;
        }).AddButton(Lang.Get("pause-commandhandbook"), openCommandHandbook, ElementStdBounds.MenuButton(bposy).WithFixedWidth(width), CairoFont.ButtonText().WithFontSize(18f))
            .EndIf()
            .AddButton(game.IsSingleplayer ? Lang.Get("pause-savequit") : Lang.Get("pause-disconnect"), OnLeaveWorld, ElementStdBounds.MenuButton(bposy + 1f).WithFixedWidth(width))
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
        float num = (checkbox ? 1 : 0);
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
            .AddButton(Lang.Get("Cancel"), OnCancel, ElementStdBounds.Rowed(4.3f + num, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0).WithFixedAlignmentOffset(-10.0, 0.0))
            .AddButton(Lang.Get("Confirm"), OnConfirm, ElementStdBounds.Rowed(4.3f + num, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0))
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
            bool on = base.SingleComposer.GetSwitch("switch").On;
            game.OpenedToInternet = true;
            EscapeMenuHome();
            game.SendPacketClient(ClientPackets.Chat(GlobalConstants.GeneralChatGroup, "/upnp 1"));
            game.SendPacketClient(ClientPackets.Chat(GlobalConstants.GeneralChatGroup, "/serverconfig advertise " + (on ? "1" : "0")));
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
        bool flag = base.SingleComposer?.DialogName.StartsWithOrdinal("gamesettings-graphics") ?? false;
        if (wasInGraphics != flag)
        {
            if (!flag)
            {
                game.api.eventapi.PushEvent("leftGraphicsDlg");
            }
            else
            {
                game.api.eventapi.PushEvent("enteredGraphicsDlg");
            }

            wasInGraphics = flag;
        }

        base.OnRenderGUI(deltaTime);
    }

    public override void OnFinalizeFrame(float dt)
    {
        base.OnFinalizeFrame(dt);
    }

    public override void Dispose()
    {
        if ((capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-graphicsingame", out var value))
        {
            value.Dispose();
        }

        if ((capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-controlsingame", out value))
        {
            value.Dispose();
        }

        if ((capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-interfaceoptionsingame", out value))
        {
            value.Dispose();
        }

        if ((capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-soundoptionsingame", out value))
        {
            value.Dispose();
        }

        if ((capi.World as ClientMain).GuiComposers.Composers.TryGetValue("gamesettings-developeroptionsingame", out value))
        {
            value.Dispose();
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
#if false // Decompilation log
'168' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Could not find by name: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'CommandLine, Version=2.9.1.0, Culture=neutral, PublicKeyToken=5a870481e358d379'
Could not find by name: 'CommandLine, Version=2.9.1.0, Culture=neutral, PublicKeyToken=5a870481e358d379'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Could not find by name: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Http.dll'
------------------
Resolve: 'Open.Nat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f22a6a4582336c76'
Could not find by name: 'Open.Nat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f22a6a4582336c76'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'Mono.Nat, Version=3.0.0.0, Culture=neutral, PublicKeyToken=6c9468a3c21bc6d1'
Could not find by name: 'Mono.Nat, Version=3.0.0.0, Culture=neutral, PublicKeyToken=6c9468a3c21bc6d1'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Net.Sockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Sockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Sockets.dll'
------------------
Resolve: 'Mono.Cecil, Version=0.11.5.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e'
Could not find by name: 'Mono.Cecil, Version=0.11.5.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e'
------------------
Resolve: 'Microsoft.CodeAnalysis, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'Microsoft.CodeAnalysis, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'Microsoft.CodeAnalysis.CSharp, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'Microsoft.CodeAnalysis.CSharp, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\gwlar\.nuget\packages\system.collections.immutable\8.0.0\lib\net7.0\System.Collections.Immutable.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'ICSharpCode.SharpZipLib, Version=1.4.2.13, Culture=neutral, PublicKeyToken=1b03e6acf1164f73'
Could not find by name: 'ICSharpCode.SharpZipLib, Version=1.4.2.13, Culture=neutral, PublicKeyToken=1b03e6acf1164f73'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.IO.Compression.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Could not find by name: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\0Harmony.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Audio.OpenAL, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Audio.OpenAL, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Mathematics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Mathematics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Windowing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'DnsClient, Version=1.7.0.0, Culture=neutral, PublicKeyToken=4574bb5573c51424'
Could not find by name: 'DnsClient, Version=1.7.0.0, Culture=neutral, PublicKeyToken=4574bb5573c51424'
------------------
Resolve: 'System.IO.Pipes, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.Pipes, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.IO.Pipes.dll'
------------------
Resolve: 'OpenTK.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'OpenTK.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Diagnostics.FileVersionInfo, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.FileVersionInfo, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.FileVersionInfo.dll'
------------------
Resolve: 'System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.dll'
------------------
Resolve: 'csvorbis, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=eaa89a1626beb708'
Could not find by name: 'csvorbis, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=eaa89a1626beb708'
------------------
Resolve: 'csogg, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=cbfcc0aaeece6bdb'
Could not find by name: 'csogg, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=cbfcc0aaeece6bdb'
------------------
Resolve: 'System.Diagnostics.TraceSource, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.TraceSource, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.TraceSource.dll'
------------------
Resolve: 'xplatforminterface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'xplatforminterface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'Microsoft.Win32.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.Win32.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\Microsoft.Win32.Primitives.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
