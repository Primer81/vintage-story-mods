using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

internal class GuiScreenLogin : GuiScreen
{
	private long lastStatusUpdateMS;

	private bool logincomplete;

	private bool connecting;

	private bool requireTOTPCode;

	private EnumAuthServerResponse response;

	private string failreason;

	private string failreasondata;

	private string prelogintoken;

	public GuiScreenLogin(ScreenManager screenManager, GuiScreen parentScreen)
		: base(screenManager, parentScreen)
	{
		compose();
	}

	private void compose()
	{
		float dy = 0f;
		ElementComposer = ScreenManager.GuiComposers.Create("mainmenu-login", ElementBounds.Fill).AddShadedDialogBG(ElementBounds.Fill, withTitleBar: false).BeginChildElements(ElementBounds.Fixed(EnumDialogArea.CenterFixed, 0.0, 160.0, 400.0, 500.0))
			.AddStaticText(Lang.Get("Please enter your game account credentials"), CairoFont.WhiteSmallishText(), EnumTextOrientation.Center, ElementStdBounds.Rowed(0f, 0.0).WithFixedWidth(400.0))
			.AddIf(!requireTOTPCode)
			.AddStaticText(Lang.Get("E-Mail"), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(1.2f, 0.0).WithFixedWidth(400.0))
			.AddTextInput(ElementStdBounds.Rowed(1.6f, 0.0).WithFixedSize(400.0, 30.0), null, null, "email")
			.AddStaticText(Lang.Get("Password"), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(2.3f, 0.0).WithFixedWidth(400.0))
			.AddTextInput(ElementStdBounds.Rowed(2.7f, 0.0).WithFixedSize(400.0, 30.0), null, null, "password")
			.EndIf()
			.AddIf(requireTOTPCode)
			.Execute(delegate
			{
				dy += 0.9f;
			})
			.AddStaticText(Lang.Get("Access Code"), CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(3.2f, 0.0).WithFixedWidth(400.0))
			.AddTextInput(ElementStdBounds.Rowed(3.6f, 0.0).WithFixedSize(100.0, 30.0), null, null, "totpcode")
			.EndIf()
			.AddIf(!requireTOTPCode)
			.AddSmallButton(Lang.Get("Forgot Password?"), OnForgotPwd, ElementStdBounds.Rowed(3.2f + dy, 0.0).WithFixedPadding(10.0, 2.0))
			.EndIf()
			.AddButton(Lang.Get("Login"), OnLogin, ElementStdBounds.Rowed(4f + dy, 0.0).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(10.0, 2.0), EnumButtonStyle.Normal, "login")
			.AddButton(Lang.Get("Quit"), OnQuit, ElementStdBounds.Rowed(4f + dy, 0.0).WithFixedPadding(10.0, 2.0))
			.AddRichtext(string.Empty, CairoFont.WhiteSmallishText(), ElementStdBounds.Rowed(5f + dy, 0.0).WithFixedSize(400.0, 100.0), null, "status")
			.EndChildElements()
			.Compose();
		ElementComposer.GetTextInput("password")?.HideCharacters();
		ElementComposer.GetTextInput("email")?.SetValue(ClientSettings.UserEmail);
	}

	private bool OnForgotPwd()
	{
		NetUtil.OpenUrlInBrowser("https://account.vintagestory.at/requestresetpwd");
		return true;
	}

	private bool OnQuit()
	{
		ScreenManager.Platform.WindowExit("Login quit button was pressed");
		return true;
	}

	private bool OnLogin()
	{
		connecting = true;
		string email = ElementComposer.GetTextInput("email")?.GetText() ?? string.Empty;
		string password = ElementComposer.GetTextInput("password")?.GetText() ?? string.Empty;
		string totpCode = ElementComposer.GetTextInput("totpcode")?.GetText() ?? string.Empty;
		ElementComposer.GetButton("login").Enabled = false;
		ElementComposer.GetRichtext("status").SetNewText(Lang.Get("Connecting..."), CairoFont.WhiteSmallishText());
		lastStatusUpdateMS = ScreenManager.Platform.EllapsedMs;
		ScreenManager.sessionManager.DoLogin(email, password, totpCode, prelogintoken, OnLoginComplete);
		return true;
	}

	private void OnLoginComplete(EnumAuthServerResponse response, string failreason, string failreasondata, string prelogintoken)
	{
		logincomplete = true;
		this.response = response;
		this.failreason = failreason;
		this.failreasondata = failreasondata;
		if (this.response == EnumAuthServerResponse.Good)
		{
			this.prelogintoken = null;
		}
		else if (this.prelogintoken == null)
		{
			this.prelogintoken = prelogintoken;
		}
		connecting = false;
	}

	public override void OnKeyDown(KeyEvent e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == 49 || e.KeyCode == 82)
		{
			OnLogin();
			e.Handled = true;
		}
	}

	public override void RenderToDefaultFramebuffer(float dt)
	{
		ElementComposer.Render(dt);
		ElementComposer.PostRender(dt);
		if (connecting && ScreenManager.Platform.EllapsedMs - lastStatusUpdateMS > 1000)
		{
			lastStatusUpdateMS = ScreenManager.Platform.EllapsedMs;
			GuiElementRichtext richtext = ElementComposer.GetRichtext("status");
			string text = (((int)(lastStatusUpdateMS / 1000 % 2) == 0) ? Lang.Get("Connecting...") : Lang.Get("Connecting.."));
			richtext.SetNewText(text, CairoFont.WhiteSmallishText());
		}
		if (!logincomplete)
		{
			return;
		}
		ElementComposer.GetButton("login").Enabled = true;
		ScreenManager.ClientIsOffline = true;
		if (response == EnumAuthServerResponse.Good)
		{
			ScreenManager.ClientIsOffline = false;
			ScreenManager.DoGameInitStage4();
			requireTOTPCode = false;
		}
		else
		{
			if (failreason == "requiretotpcode" || failreason == "wrongtotpcode")
			{
				requireTOTPCode = true;
				compose();
			}
			ElementComposer.GetRichtext("status").SetNewText(Lang.Get("game:loginfailure-" + failreason, failreasondata), CairoFont.WhiteSmallishText());
		}
		logincomplete = false;
	}

	public override void OnScreenLoaded()
	{
	}
}
