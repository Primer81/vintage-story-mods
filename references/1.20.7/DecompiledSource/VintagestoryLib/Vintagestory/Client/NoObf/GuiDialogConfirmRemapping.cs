using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class GuiDialogConfirmRemapping : GuiDialog
{
	private bool genBackup;

	private bool applynow;

	private bool reloadnow;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogConfirmRemapping(ICoreClientAPI capi)
		: base(capi)
	{
		capi.Event.ChatMessage += Event_ChatMessage;
	}

	private void Compose()
	{
		ElementBounds textBounds = ElementStdBounds.Rowed(0.4f, 0.0, EnumDialogArea.LeftFixed).WithFixedWidth(500.0);
		TextDrawUtil textDrawUtil = new TextDrawUtil();
		CairoFont font = CairoFont.WhiteSmallText();
		string text = Lang.Get("requireremapping-text");
		float y = (float)textDrawUtil.GetMultilineTextHeight(font, text, textBounds.fixedWidth) / RuntimeEnv.GUIScale;
		ElementBounds switchBounds = ElementBounds.Fixed(0.0, y + 45f, 25.0, 25.0);
		ElementBounds buttonBounds = ElementStdBounds.MenuButton((y + 150f) / 100f).WithFixedPadding(6.0);
		ElementBounds bgBounds = ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding);
		base.SingleComposer = capi.Gui.CreateCompo("confirmremapping", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(bgBounds).AddDialogTitleBar(Lang.Get("Upgrade required"), OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddRichtext(text, font, textBounds)
			.AddSwitch(onToggleBackup, switchBounds, "switch", 25.0)
			.AddStaticText(Lang.Get("remapper-backup"), font, switchBounds.RightCopy(10.0, 3.0).WithFixedWidth(500.0))
			.AddSmallButton(Lang.Get("Remind me later"), onRemindMeLater, buttonBounds.FlatCopy().WithAlignment(EnumDialogArea.LeftFixed))
			.AddSmallButton(Lang.Get("No, Ignore"), onIgnore, buttonBounds.FlatCopy().WithAlignment(EnumDialogArea.RightFixed).WithFixedAlignmentOffset(-180.0, 0.0))
			.AddSmallButton(Lang.Get("Ok, Apply now"), onApplyNow, buttonBounds.FlatCopy().WithAlignment(EnumDialogArea.RightFixed))
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetSwitch("switch").On = genBackup;
	}

	private void onToggleBackup(bool on)
	{
		genBackup = on;
	}

	private void Event_ChatMessage(int groupId, string message, EnumChatType chattype, string data)
	{
		if (chattype == EnumChatType.CommandSuccess && data == "backupdone" && genBackup)
		{
			genBackup = false;
			capi.ShowChatMessage(Lang.Get("remappingwarning-wait"));
			capi.SendChatMessage("/fixmapping applyall");
		}
		if (chattype == EnumChatType.CommandSuccess && data == "fixmappingdone" && applynow)
		{
			applynow = false;
			if ((capi.World as ClientMain).IsSingleplayer)
			{
				reloadnow = true;
			}
		}
	}

	public override void OnFinalizeFrame(float dt)
	{
		if (reloadnow)
		{
			capi.Input.HotKeys["reloadworld"].Handler?.Invoke(null);
		}
	}

	private bool onApplyNow()
	{
		applynow = true;
		if (genBackup)
		{
			capi.SendChatMessage("/genbackup");
		}
		else
		{
			capi.ShowChatMessage(Lang.Get("remappingwarning-wait"));
			capi.SendChatMessage("/fixmapping applyall");
		}
		TryClose();
		return true;
	}

	private bool onIgnore()
	{
		capi.SendChatMessage("/fixmapping ignoreall");
		TryClose();
		return true;
	}

	private bool onRemindMeLater()
	{
		TryClose();
		return true;
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}

	public override void OnGuiOpened()
	{
		Compose();
		base.OnGuiOpened();
	}

	public override void OnLevelFinalize()
	{
		capi.Logger.VerboseDebug("Handling LevelFinalize packet; requires remapping is " + (capi.World as ClientMain).ServerInfo.RequiresRemappings);
		if ((capi.World as ClientMain).ServerInfo.RequiresRemappings)
		{
			Compose();
			TryOpen();
		}
	}
}
