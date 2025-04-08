using System.Collections.Generic;
using System.Text;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.ModDb;

namespace Vintagestory.Client;

public class GuiScreenDownloadMods : GuiScreen
{
	private ServerConnectData connectdata;

	private List<string> modsToDownload;

	private string installPath;

	private StringBuilder logText = new StringBuilder();

	internal StartServerArgs serverargs;

	private EnumDownloadModType dlType;

	private int modsToDownloadTotal;

	private int modsLeftToDownload;

	private int errorCount;

	private ModDbUtil modUtil;

	private int waitcounter;

	public GuiScreenDownloadMods(ServerConnectData connectdata, string installPath, List<string> modsToDownload, ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		this.connectdata = connectdata;
		this.modsToDownload = modsToDownload;
		this.installPath = installPath;
		dlType = EnumDownloadModType.ServerRequiredMods;
		if (connectdata == null)
		{
			dlType = EnumDownloadModType.SelectiveInstall;
		}
		else if (connectdata.Host == null)
		{
			dlType = EnumDownloadModType.ResolveDependencies;
		}
		ScreenManager.GuiComposers.ClearCache();
		ElementBounds titleBounds = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 650.0, 30.0);
		ElementBounds textBounds = ElementBounds.Fixed(EnumDialogArea.LeftFixed, 0.0, 0.0, 650.0, 360.0).FixedUnder(titleBounds);
		ElementBounds dlgBounds = textBounds.ForkBoundingParent(15.0, 15.0, 15.0, 15.0).WithAlignment(EnumDialogArea.CenterMiddle);
		string text = "";
		string title = "";
		switch (dlType)
		{
		case EnumDownloadModType.ResolveDependencies:
			title = Lang.Get("downloadmods-title-dependencyinstall");
			text = Lang.Get("downloadmods-dependencyinstall", string.Join(", ", modsToDownload[0]));
			break;
		case EnumDownloadModType.SelectiveInstall:
			title = Lang.Get("downloadmods-title-selectinstall");
			text = Lang.Get("downloadmods-selectinstall", modsToDownload[0]);
			break;
		case EnumDownloadModType.ServerRequiredMods:
			title = Lang.Get("downloadmods-title-serverinstall");
			text = Lang.Get("downloadmods-serverinstall", modsToDownload.Count);
			break;
		}
		ElementComposer = ScreenManager.GuiComposers.Create("mainmenu-downloadmods", dlgBounds).AddShadedDialogBG(ElementBounds.Fill, withTitleBar: false, 5.0, 1f).BeginChildElements(textBounds)
			.AddRichtext(title, CairoFont.WhiteSmallishText().WithWeight(FontWeight.Bold), titleBounds, didClickLink, "titleText")
			.AddRichtext(text + "\r\n\r\n" + Lang.Get("downloadmods-disclaimer"), CairoFont.WhiteSmallishText(), textBounds.ForkChild().WithFixedPosition(0.0, 25.0), didClickLink, "centertext")
			.AddButton(Lang.Get("Cancel"), OnCancel, ElementStdBounds.Rowed(4.5f, 0.0, EnumDialogArea.LeftFixed).WithFixedPadding(10.0, 2.0))
			.AddButton(Lang.Get("Download mods"), OnConfirm, ElementStdBounds.Rowed(4.5f, 0.0, EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, "confirmButton")
			.EndChildElements()
			.Compose();
	}

	private bool OnCancel()
	{
		ScreenManager.StartMainMenu();
		ScreenManager.guiMainmenuLeft.OnMultiplayer();
		return true;
	}

	private bool OnConfirm()
	{
		ElementBounds dialogBounds = ElementBounds.Fixed(0.0, 50.0, 800.0, 390.0).WithAlignment(EnumDialogArea.CenterFixed).WithFixedPosition(0.0, 150.0);
		ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 790.0, 300.0);
		ElementBounds insetBounds = textBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0);
		ElementBounds clipBounds = textBounds.FlatCopy().WithParent(insetBounds);
		clipBounds.fixedHeight -= 3.0;
		ElementBounds scrollbarBounds = ElementBounds.Fixed(EnumDialogArea.RightFixed, 0.0, 1.0, 10.0, insetBounds.fixedHeight - 2.0);
		ElementBounds titleBounds = ElementBounds.Fixed(0.0, -30.0, dialogBounds.fixedWidth, 28.0);
		CairoFont loadingFont = CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center);
		loadingFont.Color[3] = 0.65;
		modsLeftToDownload = modsToDownload.Count;
		modsToDownloadTotal = modsToDownload.Count;
		ElementBounds cancelBounds = ElementBounds.Fixed(0, 30).FixedUnder(insetBounds).WithFixedPadding(10.0, 2.0)
			.WithAlignment(EnumDialogArea.LeftFixed);
		ElementBounds joinBounds = ElementBounds.Fixed(0, 30).FixedUnder(insetBounds).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedAlignmentOffset(-10.0, 0.0)
			.WithFixedPadding(10.0, 2.0);
		string btnText = Lang.Get((connectdata?.Host == null) ? "Continue" : "Join Server");
		ElementComposer = ScreenManager.GuiComposers.Create("downloadmods", ElementBounds.Fill).AddShadedDialogBG(ElementBounds.Fill, withTitleBar: false, 5.0, 1f).BeginChildElements(dialogBounds)
			.AddDynamicText(Lang.Get("Attempting to download {0} mods...", modsLeftToDownload), loadingFont, titleBounds, "titleText")
			.AddInset(insetBounds, 3, 0.8f)
			.BeginClip(clipBounds)
			.AddRichtext("", CairoFont.WhiteSmallText(), textBounds, "logText")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarBalue, scrollbarBounds, "scrollbar")
			.AddButton(Lang.Get("Cancel"), OnCancel, cancelBounds)
			.AddButton(btnText, OnJoin, joinBounds, EnumButtonStyle.Normal, "joinBtn")
			.EndChildElements()
			.Compose();
		modUtil = new ModDbUtil(ScreenManager.api, ClientSettings.ModDbUrl, installPath);
		ElementComposer.GetButton("joinBtn").Enabled = false;
		textBounds.CalcWorldBounds();
		clipBounds.CalcWorldBounds();
		ElementComposer.GetScrollbar("scrollbar").SetHeights((float)clipBounds.fixedHeight, (float)textBounds.fixedHeight);
		return true;
	}

	public override void RenderToPrimary(float dt)
	{
		base.RenderToPrimary(dt);
		if (modUtil != null && modsToDownload.Count > 0 && !modUtil.IsLoading)
		{
			waitcounter++;
			if (waitcounter > 2)
			{
				string mod = modsToDownload[0];
				modsToDownload.RemoveAt(0);
				modUtil.SearchAndInstall(mod, "1.20.7", onProgressUpdate, deletedOutdated: false);
				waitcounter = 0;
			}
		}
	}

	private bool OnJoin()
	{
		ScreenManager.loadMods();
		if (connectdata == null)
		{
			ScreenManager.StartMainMenu();
		}
		else if (connectdata.Host == null)
		{
			ScreenManager.ConnectToSingleplayer(serverargs);
		}
		else
		{
			ScreenManager.ConnectToMultiplayer(connectdata.HostRaw, connectdata.ServerPassword);
		}
		return true;
	}

	private void OnNewScrollbarBalue(float value)
	{
		ElementBounds bounds = ElementComposer.GetRichtext("logText").Bounds;
		bounds.fixedY = 5f - value;
		bounds.CalcWorldBounds();
	}

	private void onProgressUpdate(string message, EnumModInstallState state)
	{
		if (state == EnumModInstallState.InProgress || state == EnumModInstallState.Offline)
		{
			logText.Append(message);
		}
		else
		{
			logText.AppendLine(message);
		}
		if (state != 0)
		{
			modsLeftToDownload--;
			ElementComposer.GetDynamicText("titleText").SetNewText(Lang.Get("Attempting to download {0}/{1} mods...", modsToDownloadTotal - modsLeftToDownload, modsToDownloadTotal));
			if (state != EnumModInstallState.InstalledOrReady)
			{
				errorCount++;
			}
		}
		if (modsLeftToDownload == 0 && errorCount > 0)
		{
			logText.AppendLine("\r\n" + Lang.Get("Unable to download some mods from the mod database. You'll have to manually install {0} mods. Sorry!", errorCount));
		}
		if (modsLeftToDownload == 0 && errorCount == 0)
		{
			ElementComposer.GetButton("joinBtn").Enabled = true;
			logText.AppendLine("\r\n" + ((connectdata?.Host == null) ? Lang.Get("All mods downloaded, ready to continue!") : Lang.Get("All mods downloaded, ready to join this server!")));
		}
		ElementComposer.GetRichtext("logText").SetNewText(logText.ToString(), CairoFont.WhiteSmallText());
		ScreenManager.GamePlatform.Logger.Notification(logText.ToString());
		GuiElementScrollbar scrollElem = ElementComposer.GetScrollbar("scrollbar");
		GuiElementRichtext textElem = ElementComposer.GetRichtext("logText");
		scrollElem.SetNewTotalHeight((float)(textElem.Bounds.OuterHeight / (double)ClientSettings.GUIScale));
		if (!scrollElem.mouseDownOnScrollbarHandle)
		{
			scrollElem.ScrollToBottom();
		}
	}

	private void didClickLink(LinkTextComponent link)
	{
		ScreenManager.StartMainMenu();
		ScreenManager.EnqueueMainThreadTask(delegate
		{
			ScreenManager.api.Gui.OpenLink(link.Href);
		});
	}

	public override void RenderToDefaultFramebuffer(float dt)
	{
		if (ScreenManager.KeyboardKeyState[50])
		{
			ScreenManager.StartMainMenu();
			return;
		}
		ElementComposer.Render(dt);
		ScreenManager.RenderMainMenuParts(dt, ElementComposer.Bounds, withMainMenu: false);
		if (ScreenManager.mainMenuComposer.MouseOverCursor != null)
		{
			FocusedMouseCursor = ScreenManager.mainMenuComposer.MouseOverCursor;
		}
		ElementComposer.PostRender(dt);
	}
}
