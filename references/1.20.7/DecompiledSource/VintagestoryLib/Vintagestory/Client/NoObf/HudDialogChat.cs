using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class HudDialogChat : HudElement
{
	private static int historyMax = 30;

	private LimitedList<string> typedMessagesHistory = new LimitedList<string>(100);

	private int typedMessagesHistoryPos = -1;

	private long lastActivityMs = -100000L;

	private int lastMessageInGroupId = -99999;

	private TreeAttribute eventAttr;

	internal bool MultiCommandPasteMode;

	private ClientMain game;

	private int chatWindowInnerWidth = ClientSettings.ChatWindowWidth;

	private int chatWindowInnerHeight = ClientSettings.ChatWindowHeight;

	private int tabsHeight = 23;

	private int chatInputHeight = 25;

	private int horPadding = 6;

	private int verPadding = 3;

	private int bottomOffset = 100;

	private int scrollbarPadding = 1;

	private int scrollbarWidth = 10;

	private GuiTab[] tabs;

	private double lastAlpha = 1.0;

	private bool isLinkChatTyped;

	public override string ToggleKeyCombinationCode => "beginchat";

	public override double InputOrder => 1.1;

	public override double DrawOrder => 0.0;

	public override EnumDialogType DialogType
	{
		get
		{
			if (IsOpened() && focused)
			{
				return EnumDialogType.Dialog;
			}
			return EnumDialogType.HUD;
		}
	}

	public HudDialogChat(ICoreClientAPI capi)
		: base(capi)
	{
		eventAttr = new TreeAttribute();
		eventAttr["key"] = new IntAttribute();
		eventAttr["text"] = new StringAttribute();
		eventAttr["scrolltoEnd"] = new BoolAttribute();
		game = capi.World as ClientMain;
		game.eventManager?.OnNewServerToClientChatLine.Add(OnNewServerToClientChatLine);
		game.eventManager?.OnNewClientToServerChatLine.Add(OnNewClientToServerChatLine);
		game.eventManager?.OnNewClientOnlyChatLine.Add(OnNewClientOnlyChatLine);
		game.ChatHistoryByPlayerGroup[GlobalConstants.GeneralChatGroup] = new LimitedList<string>(historyMax);
		ComposeChatGuis();
		Composers["chat"].UnfocusOwnElements();
		UpdateText();
		CommandArgumentParsers parsers = game.api.ChatCommands.Parsers;
		game.api.ChatCommands.GetOrCreate("debug").BeginSubCommand("recomposechat").RequiresPrivilege(Privilege.chat)
			.WithDescription("Recompose chat dialogs")
			.HandleWith(CmdChatC)
			.EndSubCommand();
		game.api.ChatCommands.Create("clearchat").WithDescription("Clear all chat history").HandleWith(CmdClearChat);
		game.api.ChatCommands.Create("chatsize").WithDescription("Set the chat dialog width and height (default 400x160)").WithArgs(parsers.OptionalInt("width", 700), parsers.OptionalInt("height", 200))
			.HandleWith(CmdChatSize);
		game.api.ChatCommands.Create("pastemode").WithDescription("Set the chats paste mode. If set to multi pasting multiple lines will produce multiple chat lines.").WithArgs(parsers.WordRange("mode", "single", "multi"))
			.HandleWith(CmdPasteMode);
		game.PacketHandlers[50] = HandlePlayerGroupPacket;
		game.PacketHandlers[49] = HandlePlayerGroupsPacket;
		game.PacketHandlers[57] = HandleGotoGroupPacket;
		ScreenManager.hotkeyManager.SetHotKeyHandler("chatdialog", OnKeyCombinationTab);
		ScreenManager.hotkeyManager.SetHotKeyHandler("beginclientcommand", delegate(KeyCombination kc)
		{
			OnKeyCombinationTab(kc);
			OnKeyCombinationToggle(kc);
			Composers["chat"].GetChatInput("chatinput").SetValue(".");
			return true;
		});
		ScreenManager.hotkeyManager.SetHotKeyHandler("beginservercommand", delegate(KeyCombination kc)
		{
			OnKeyCombinationTab(kc);
			OnKeyCombinationToggle(kc);
			Composers["chat"].GetChatInput("chatinput").SetValue("/");
			return true;
		});
		game.api.RegisterLinkProtocol("screenshot", onLinkClicked);
		game.api.RegisterLinkProtocol("chattype", onChatType);
		game.api.RegisterLinkProtocol("datafolder", onDataFolderLinkClicked);
	}

	private void onDataFolderLinkClicked(LinkTextComponent comp)
	{
		string[] comps = comp.Href.Split(new string[1] { "://" }, StringSplitOptions.RemoveEmptyEntries);
		if (comps.Length == 2 && comps[1] == "worldedit")
		{
			NetUtil.OpenUrlInBrowser(Path.Combine(GamePaths.DataPath, "worldedit"));
		}
	}

	private void onLinkClicked(LinkTextComponent comp)
	{
		string[] comps = comp.Href.Split(new string[1] { "://" }, StringSplitOptions.RemoveEmptyEntries);
		if (comps.Length == 2 && Regex.IsMatch(comps[1], "[\\d\\w\\-]+\\.png"))
		{
			string path = Path.Combine(GamePaths.Screenshots, comps[1]);
			if (File.Exists(path))
			{
				NetUtil.OpenUrlInBrowser(path);
			}
		}
	}

	public override void OnOwnPlayerDataReceived()
	{
		if (ClientSettings.ChatDialogVisible)
		{
			TryOpen();
			UnFocus();
		}
	}

	private TextCommandResult CmdPasteMode(TextCommandCallingArgs args)
	{
		MultiCommandPasteMode = args[0] as string == "multi";
		return TextCommandResult.Success("Pastemode " + args[0]?.ToString() + " set.");
	}

	private TextCommandResult CmdClearChat(TextCommandCallingArgs textCommandCallingArgs)
	{
		foreach (KeyValuePair<int, LimitedList<string>> item in game.ChatHistoryByPlayerGroup)
		{
			item.Value.Clear();
		}
		UpdateText();
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdChatC(TextCommandCallingArgs textCommandCallingArgs)
	{
		ComposeChatGuis();
		UpdateText();
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdChatSize(TextCommandCallingArgs args)
	{
		ClientSettings.ChatWindowWidth = (chatWindowInnerWidth = (int)args[0]);
		ClientSettings.ChatWindowHeight = (chatWindowInnerHeight = (int)args[1]);
		ComposeChatGuis();
		UpdateText();
		return TextCommandResult.Success();
	}

	private void ComposeChatGuis()
	{
		ClearComposers();
		int outerWidth = horPadding + chatWindowInnerWidth + scrollbarWidth + horPadding;
		int outerHeight = tabsHeight + verPadding + chatWindowInnerHeight + verPadding + chatInputHeight + verPadding;
		int chatInputYPos = tabsHeight + verPadding + chatWindowInnerHeight + verPadding;
		int chatTextBottomOffset = bottomOffset + chatInputHeight + 3 * verPadding;
		int scrollbarHeight = chatWindowInnerHeight - 2 * scrollbarPadding + 2 * verPadding - 1;
		ElementBounds dialogBounds = ElementBounds.Fixed(EnumDialogArea.LeftBottom, 0.0, 0.0, outerWidth, outerHeight).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, -bottomOffset);
		ElementBounds chatTextDialogBg = ElementBounds.Fixed(EnumDialogArea.LeftBottom, horPadding, verPadding, chatWindowInnerWidth, chatWindowInnerHeight).WithFixedAlignmentOffset(GuiStyle.DialogToScreenPadding, -chatTextBottomOffset);
		ElementBounds clipBounds = ElementBounds.Fixed(0.0, 0.0, chatWindowInnerWidth, chatWindowInnerHeight);
		ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, chatWindowInnerWidth, chatWindowInnerHeight);
		tabs = new GuiTab[game.OwnPlayerGroupsById.Count];
		int i = 0;
		foreach (KeyValuePair<int, PlayerGroup> val in game.OwnPlayerGroupsById)
		{
			tabs[i++] = new GuiTab
			{
				DataInt = val.Key,
				Name = val.Value.Name
			};
		}
		CairoFont font = CairoFont.WhiteDetailText().WithFontSize(17f);
		Composers["chat"] = capi.Gui.CreateCompo("chatdialog", dialogBounds).AddGameOverlay(ElementBounds.Fixed(0.0, tabsHeight, outerWidth, outerHeight), GuiStyle.DialogLightBgColor).AddChatInput(ElementBounds.Fixed(0.0, chatInputYPos, outerWidth, chatInputHeight), OnTextChanged, "chatinput")
			.AddCompactVerticalScrollbar(OnNewScrollbarValue, ElementBounds.Fixed(outerWidth - scrollbarWidth, tabsHeight + scrollbarPadding, scrollbarWidth, scrollbarHeight), "scrollbar")
			.AddHorizontalTabs(tabs, ElementBounds.Fixed(0.0, 0.0, outerWidth, tabsHeight), OnTabClicked, font, font.Clone().WithColor(GuiStyle.ActiveButtonTextColor), "tabs")
			.Compose();
		CairoFont alarmFont = font.Clone().WithColor(GuiStyle.DialogDefaultTextColor);
		Composers["chat"].GetHorizontalTabs("tabs").WithAlarmTabs(alarmFont);
		Composers["chat-group-" + GlobalConstants.GeneralChatGroup] = capi.Gui.CreateCompo("chat-group-" + GlobalConstants.GeneralChatGroup, chatTextDialogBg.FlatCopy()).BeginClip(clipBounds.FlatCopy()).AddRichtext("", font, textBounds.FlatCopy(), null, "chathistory")
			.EndClip()
			.Compose();
		Composers["chat-group-" + GlobalConstants.DamageLogChatGroup] = capi.Gui.CreateCompo("chat-group-damagelog", chatTextDialogBg.FlatCopy()).BeginClip(clipBounds.FlatCopy()).AddRichtext("", font, textBounds.FlatCopy(), null, "chathistory")
			.EndClip()
			.Compose();
		Composers["chat-group-" + GlobalConstants.InfoLogChatGroup] = capi.Gui.CreateCompo("chat-group-infolog", chatTextDialogBg.FlatCopy()).BeginClip(clipBounds.FlatCopy()).AddRichtext("", font, textBounds.FlatCopy(), null, "chathistory")
			.EndClip()
			.Compose();
		Composers["chat-group-" + GlobalConstants.ServerInfoChatGroup] = capi.Gui.CreateCompo("chat-group-" + GlobalConstants.ServerInfoChatGroup, chatTextDialogBg.FlatCopy()).BeginClip(clipBounds.FlatCopy()).AddRichtext("", font, textBounds.FlatCopy(), null, "chathistory")
			.EndClip()
			.Compose();
		foreach (PlayerGroup group in game.OwnPlayerGroupsById.Values)
		{
			Composers["chat-group-" + group.Uid]?.Dispose();
			Composers["chat-group-" + group.Uid] = capi.Gui.CreateCompo("chat-group-" + group.Uid, chatTextDialogBg.FlatCopy()).BeginClip(clipBounds.FlatCopy()).AddRichtext("", font, textBounds.FlatCopy(), null, "chathistory")
				.EndClip()
				.Compose();
		}
		Composers["chat"].GetCompactScrollbar("scrollbar").SetHeights(scrollbarHeight, scrollbarHeight);
		Composers["chat"].UnfocusOwnElements();
	}

	private void OnTabClicked(int groupId)
	{
		game.currentGroupid = groupId;
		int tabIndex = tabIndexByGroupId(groupId);
		Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[tabIndex] = false;
		if (!game.ChatHistoryByPlayerGroup.ContainsKey(game.currentGroupid))
		{
			game.ChatHistoryByPlayerGroup[game.currentGroupid] = new LimitedList<string>(historyMax);
		}
		UpdateText();
	}

	private void OnNewScrollbarValue(float value)
	{
		GuiElementRichtext richtext = Composers["chat-group-" + game.currentGroupid].GetRichtext("chathistory");
		richtext.Bounds.fixedY = 0f - value;
		richtext.Bounds.CalcWorldBounds();
		lastActivityMs = game.Platform.EllapsedMs;
	}

	private void UpdateText()
	{
		GuiElementRichtext textElem = Composers["chat-group-" + game.currentGroupid].GetRichtext("chathistory");
		LimitedList<string> limitedList = game.ChatHistoryByPlayerGroup[game.currentGroupid];
		StringBuilder fullchattext = new StringBuilder();
		int i = 0;
		foreach (string line in limitedList)
		{
			if (i++ > 0)
			{
				fullchattext.Append("\r\n");
			}
			fullchattext.Append(line);
		}
		textElem.SetNewText(fullchattext.ToString(), CairoFont.WhiteDetailText().WithFontSize(17f));
		GuiElementScrollbar scrollbarElem = Composers["chat"].GetCompactScrollbar("scrollbar");
		scrollbarElem.SetNewTotalHeight((float)textElem.Bounds.fixedHeight + 5f);
		if (!scrollbarElem.mouseDownOnScrollbarHandle)
		{
			scrollbarElem.ScrollToBottom();
		}
	}

	private void OnTextChanged(string text)
	{
	}

	public override void OnRenderGUI(float deltaTime)
	{
		double alpha = (focused ? 1.0 : Math.Max(0.5, lastAlpha - (double)(deltaTime / 6f)));
		lastAlpha = alpha;
		foreach (KeyValuePair<string, GuiComposer> val in (IEnumerable<KeyValuePair<string, GuiComposer>>)Composers)
		{
			if (val.Key == "chat")
			{
				if (val.Value.Color == null)
				{
					val.Value.Color = new Vec4f(1f, 1f, 1f, 1f);
				}
				val.Value.Color.W = (float)lastAlpha;
				val.Value.Render(deltaTime);
			}
			else if (val.Key == "chat-group-" + game.currentGroupid)
			{
				val.Value.Render(deltaTime);
			}
		}
	}

	public override void OnFinalizeFrame(float dt)
	{
		foreach (KeyValuePair<string, GuiComposer> item in (IEnumerable<KeyValuePair<string, GuiComposer>>)Composers)
		{
			item.Value.PostRender(dt);
		}
		if (Focused)
		{
			lastActivityMs = game.Platform.EllapsedMs;
		}
		if (!ClientSettings.AutoChat)
		{
			return;
		}
		if (IsOpened() && game.Platform.EllapsedMs - lastActivityMs > 15000)
		{
			DoClose();
		}
		if (IsOpened() || game.Platform.EllapsedMs - lastActivityMs >= 50 || lastMessageInGroupId <= -99)
		{
			return;
		}
		int groupId = lastMessageInGroupId;
		if (groupId == GlobalConstants.CurrentChatGroup)
		{
			groupId = game.currentGroupid;
		}
		if (ClientSettings.AutoChatOpenSelected)
		{
			if (groupId == game.currentGroupid)
			{
				TryOpen();
				int tabIndex = tabIndexByGroupId(groupId);
				if (tabIndex >= 0)
				{
					Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[tabIndex] = false;
				}
				UpdateText();
			}
		}
		else
		{
			TryOpen();
			int tabIndex2 = tabIndexByGroupId(groupId);
			if (tabIndex2 >= 0)
			{
				Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[tabIndex2] = false;
				Composers["chat"].GetHorizontalTabs("tabs").SetValue(tabIndex2, callhandler: false);
			}
			game.currentGroupid = groupId;
			UpdateText();
		}
	}

	public override bool OnEscapePressed()
	{
		return TryClose();
	}

	public override bool IsOpened(string dialogComposerName)
	{
		if (IsOpened())
		{
			return dialogComposerName == "chat-group-" + game.currentGroupid;
		}
		return false;
	}

	public override void UnFocus()
	{
		Composers["chat"].UnfocusOwnElements();
		base.UnFocus();
	}

	public override void OnGuiOpened()
	{
		base.OnGuiOpened();
	}

	public override void OnGuiClosed()
	{
		base.OnGuiClosed();
		Composers["chat"].UnfocusOwnElements();
		typedMessagesHistoryPos = -1;
	}

	public override bool TryClose()
	{
		UnFocus();
		return false;
	}

	public void DoClose()
	{
		lastActivityMs = -100000L;
		lastMessageInGroupId = -9999;
		base.TryClose();
	}

	private bool OnKeyCombinationTab(KeyCombination viaKeyComb)
	{
		if (!IsOpened())
		{
			ClientSettings.ChatDialogVisible = true;
			opened = true;
			OnGuiOpened();
			game.eventManager?.TriggerDialogOpened(this);
			lastActivityMs = game.Platform.EllapsedMs;
			lastMessageInGroupId = -9999;
		}
		else
		{
			ClientSettings.ChatDialogVisible = false;
			UnFocus();
			DoClose();
		}
		return true;
	}

	private void onChatType(LinkTextComponent link)
	{
		if (!IsOpened())
		{
			ClientSettings.ChatDialogVisible = true;
			TryOpen();
		}
		Focus();
		capi.Gui.RequestFocus(this);
		Composers["chat"].FocusElement(0);
		GuiElementChatInput chatInput = Composers["chat"].GetChatInput("chatinput");
		string text = chatInput.GetText();
		chatInput.SetValue((isLinkChatTyped ? "" : text) + link.Href.Substring("chattype://".Length).Replace("&lt;", "<").Replace("&gt;", ">"));
		isLinkChatTyped = true;
	}

	internal override bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
	{
		if (!IsOpened())
		{
			ClientSettings.ChatDialogVisible = true;
			TryOpen();
		}
		Focus();
		capi.Gui.RequestFocus(this);
		Composers["chat"].FocusElement(0);
		string keyName = GlKeyNames.GetPrintableChar(viaKeyComb.KeyCode);
		if (!viaKeyComb.Alt && !viaKeyComb.Ctrl && !viaKeyComb.Shift && !string.IsNullOrWhiteSpace(keyName))
		{
			ignoreNextKeyPress = true;
		}
		return true;
	}

	public override void OnKeyPress(KeyEvent args)
	{
		if (IsOpened())
		{
			base.OnKeyPress(args);
		}
	}

	public override void OnKeyDown(KeyEvent args)
	{
		if (!IsOpened())
		{
			return;
		}
		GuiElementChatInput elem = Composers["chat"].GetChatInput("chatinput");
		if (args.KeyCode == 50)
		{
			UnFocus();
			args.Handled = true;
			return;
		}
		string text = elem.GetText();
		if (args.KeyCode == 49 || args.KeyCode == 82)
		{
			if (text.Length != 0)
			{
				EnumHandling handling = EnumHandling.PassThrough;
				game.api.eventapi.TriggerSendChatMessage(game.currentGroupid, ref text, ref handling);
				if (handling == EnumHandling.PassThrough)
				{
					game.eventManager?.TriggerNewClientChatLine(game.currentGroupid, text, EnumChatType.OwnMessage, null);
				}
				if (typedMessagesHistoryPos != 0 || elem.GetText() != GetHistoricalMessage(typedMessagesHistoryPos))
				{
					typedMessagesHistory.Add(elem.GetText());
				}
				elem.SetValue("");
			}
			UnFocus();
			typedMessagesHistoryPos = -1;
			args.Handled = true;
			isLinkChatTyped = false;
			return;
		}
		if (args.KeyCode == 45 && typedMessagesHistoryPos < typedMessagesHistory.Count - 1)
		{
			typedMessagesHistoryPos++;
			elem.SetValue(GetHistoricalMessage(typedMessagesHistoryPos));
			elem.SetCaretPos(elem.GetText().Length);
			args.Handled = true;
			return;
		}
		if (args.KeyCode == 46 && typedMessagesHistoryPos >= 0 && typedMessagesHistory.Count > 0)
		{
			typedMessagesHistoryPos--;
			if (typedMessagesHistoryPos < 0)
			{
				elem.SetValue("");
			}
			else
			{
				elem.SetValue(GetHistoricalMessage(typedMessagesHistoryPos));
			}
			elem.SetCaretPos(elem.GetText().Length);
			args.Handled = true;
			return;
		}
		if (args.KeyCode == 104 && args.CtrlPressed)
		{
			string insert = capi.Forms.GetClipboardText();
			insert = insert.Replace("\ufeff", "");
			if (MultiCommandPasteMode || insert.StartsWithOrdinal(".pastemode multi"))
			{
				string[] lines = Regex.Split(insert, "(\r\n|\n|\r)");
				for (int i = 0; i < lines.Length; i++)
				{
					game.eventManager?.TriggerNewClientChatLine(game.currentGroupid, lines[i], EnumChatType.OwnMessage, null);
				}
				args.Handled = true;
				return;
			}
		}
		IntAttribute obj = eventAttr["key"] as IntAttribute;
		StringAttribute textAttr = eventAttr["text"] as StringAttribute;
		eventAttr.SetInt("deltacaretpos", 0);
		obj.value = args.KeyCode;
		textAttr.value = text;
		game.api.eventapi.PushEvent("chatkeydownpre", eventAttr);
		if (text != textAttr.value)
		{
			elem.SetValue(textAttr.value);
		}
		base.OnKeyDown(args);
		textAttr.value = elem.GetText();
		game.api.eventapi.PushEvent("chatkeydownpost", eventAttr);
		if (textAttr.value != elem.GetText())
		{
			elem.SetValue(textAttr.value, setCaretPosToEnd: false);
			text = textAttr.value;
			if (eventAttr.GetInt("deltacaretpos") != 0)
			{
				elem.SetCaretPos(elem.CaretPosInLine + eventAttr.GetInt("deltacaretpos"));
			}
		}
		if (text.Length == 0)
		{
			isLinkChatTyped = false;
		}
		if (ScreenManager.hotkeyManager.GetHotKeyByCode("chatdialog").DidPress(args, game, game.player, allowCharacterControls: true))
		{
			DoClose();
			UnFocus();
			args.Handled = true;
		}
		else if (focused)
		{
			args.Handled = true;
		}
	}

	public override void OnMouseUp(MouseEvent args)
	{
		base.OnMouseUp(args);
		if (IsOpened())
		{
			GuiElement elem = Composers["chat"].GetChatInput("chatinput");
			if (elem.IsPositionInside(args.X, args.Y))
			{
				Composers["chat"].FocusElement(elem.TabIndex);
			}
		}
	}

	public string GetHistoricalMessage(int typedMessagesHistoryPos)
	{
		int pos = typedMessagesHistory.Count - 1 - typedMessagesHistoryPos;
		if (pos < 0 || pos >= typedMessagesHistory.Count)
		{
			return null;
		}
		return typedMessagesHistory[pos];
	}

	private int tabIndexByGroupId(int groupId)
	{
		for (int i = 0; i < tabs.Length; i++)
		{
			if (tabs[i].DataInt == groupId)
			{
				return i;
			}
		}
		return -1;
	}

	private void OnNewServerToClientChatLine(int groupId, string message, EnumChatType chattype, string data)
	{
		if (groupId != game.currentGroupid)
		{
			int tabIndex2 = tabIndexByGroupId(groupId);
			if (tabIndex2 >= 0)
			{
				Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[tabIndex2] = true;
			}
		}
		if (message.Contains("</hk>", StringComparison.InvariantCulture))
		{
			int i = message.IndexOfOrdinal("<hk>");
			int j = message.IndexOfOrdinal("</hk>");
			if (j > i)
			{
				string hotkeycode = message.Substring(i + 4, j - i - 4);
				if (capi.Input.HotKeys.TryGetValue(hotkeycode.ToLowerInvariant(), out var hotkey))
				{
					message = message.Substring(0, i) + hotkey.CurrentMapping.ToString() + message.Substring(j + 5);
				}
			}
		}
		if ((chattype == EnumChatType.Notification || chattype == EnumChatType.CommandSuccess) && groupId != GlobalConstants.InfoLogChatGroup)
		{
			message = "<font color=\"#CCe0cfbb\">" + message + "</font>";
		}
		if (chattype != EnumChatType.OthersMessage && chattype != EnumChatType.JoinLeave && ClientSettings.AutoChat && ClientSettings.AutoChatOpenSelected && groupId != GlobalConstants.DamageLogChatGroup && groupId != GlobalConstants.AllChatGroups && groupId != GlobalConstants.ServerInfoChatGroup)
		{
			int tabIndex = tabIndexByGroupId(groupId);
			if (tabIndex >= 0)
			{
				Composers["chat"].GetHorizontalTabs("tabs").TabHasAlarm[tabIndex] = false;
				Composers["chat"].GetHorizontalTabs("tabs").SetValue(tabIndex);
			}
			if (groupId != GlobalConstants.CurrentChatGroup)
			{
				game.currentGroupid = groupId;
			}
		}
		if (groupId == GlobalConstants.AllChatGroups)
		{
			foreach (int memberGroupId in game.ChatHistoryByPlayerGroup.Keys)
			{
				game.ChatHistoryByPlayerGroup[memberGroupId].Add(message);
			}
			UpdateText();
			lastActivityMs = game.Platform.EllapsedMs;
			lastMessageInGroupId = game.currentGroupid;
			return;
		}
		if (groupId == GlobalConstants.CurrentChatGroup)
		{
			groupId = game.currentGroupid;
		}
		if (!game.ChatHistoryByPlayerGroup.ContainsKey(groupId))
		{
			game.ChatHistoryByPlayerGroup[groupId] = new LimitedList<string>(historyMax);
		}
		game.ChatHistoryByPlayerGroup[groupId].Add(message);
		if (game.currentGroupid == groupId)
		{
			UpdateText();
		}
		if (groupId != GlobalConstants.ServerInfoChatGroup && groupId != GlobalConstants.DamageLogChatGroup)
		{
			lastActivityMs = game.Platform.EllapsedMs;
			lastMessageInGroupId = groupId;
		}
	}

	private void OnNewClientToServerChatLine(int groupId, string message, EnumChatType chattype, string data)
	{
		HandleClientMessage(groupId, message);
		if (!message.StartsWithOrdinal(ChatCommandApi.ServerCommandPrefix) && !message.StartsWithOrdinal(ChatCommandApi.ClientCommandPrefix) && groupId != GlobalConstants.ServerInfoChatGroup && groupId != GlobalConstants.DamageLogChatGroup)
		{
			lastActivityMs = game.Platform.EllapsedMs;
			lastMessageInGroupId = groupId;
		}
	}

	private void OnNewClientOnlyChatLine(int groupId, string message, EnumChatType chattype, string data)
	{
		if (!(message == "") && message != null)
		{
			if (message.StartsWithOrdinal(ChatCommandApi.ClientCommandPrefix))
			{
				HandleClientCommand(message, groupId);
			}
			else
			{
				game.ShowChatMessage(message);
			}
		}
	}

	public void HandleClientCommand(string message, int groupid)
	{
		message = message.Substring(1);
		int argsStart = message.IndexOf(' ');
		string args;
		string command;
		if (argsStart > 0)
		{
			args = message.Substring(argsStart + 1);
			command = message.Substring(0, argsStart);
		}
		else
		{
			args = "";
			command = message;
		}
		game.api.chatcommandapi.Execute(command, game.player, groupid, args);
	}

	public void HandleClientMessage(int groupid, string message)
	{
		if (!(message == "") && message != null)
		{
			if (message.StartsWithOrdinal(ChatCommandApi.ClientCommandPrefix))
			{
				HandleClientCommand(message, groupid);
				return;
			}
			message = message.Substring(0, Math.Min(1024, message.Length));
			game.SendPacketClient(ClientPackets.Chat(groupid, message));
		}
	}

	private void HandleGotoGroupPacket(Packet_Server packet)
	{
		int groupId = packet.GotoGroup.GroupId;
		if (!game.OwnPlayerGroupsById.ContainsKey(groupId))
		{
			return;
		}
		game.currentGroupid = groupId;
		if (!game.ChatHistoryByPlayerGroup.ContainsKey(game.currentGroupid))
		{
			game.ChatHistoryByPlayerGroup[game.currentGroupid] = new LimitedList<string>(historyMax);
		}
		UpdateText();
		GuiTab[] tabs = Composers["chat"].GetHorizontalTabs("tabs").tabs;
		for (int i = 0; i < tabs.Length; i++)
		{
			if (tabs[i].DataInt == game.currentGroupid)
			{
				Composers["chat"].GetHorizontalTabs("tabs").activeElement = i;
				break;
			}
		}
	}

	private void HandlePlayerGroupsPacket(Packet_Server packet)
	{
		game.OwnPlayerGroupsById.Clear();
		game.OwnPlayerGroupsById[GlobalConstants.GeneralChatGroup] = new PlayerGroup
		{
			Name = Lang.Get("chattab-general")
		};
		game.OwnPlayerGroupsById[GlobalConstants.DamageLogChatGroup] = new PlayerGroup
		{
			Name = Lang.Get("chattab-damagelog")
		};
		game.OwnPlayerGroupsById[GlobalConstants.InfoLogChatGroup] = new PlayerGroup
		{
			Name = Lang.Get("chattab-infolog")
		};
		game.OwnPlayerGroupMemembershipsById.Clear();
		if (game.Player?.Privileges != null && game.player.Privileges.Contains("controlserver") && !game.IsSingleplayer)
		{
			game.OwnPlayerGroupsById[GlobalConstants.ServerInfoChatGroup] = new PlayerGroup
			{
				Name = Lang.Get("chattab-serverinfo")
			};
		}
		for (int i = 0; i < packet.PlayerGroups.GroupsCount; i++)
		{
			PlayerGroup plrGroup = PlayerGroupFromPacket(packet.PlayerGroups.Groups[i]);
			game.OwnPlayerGroupsById[plrGroup.Uid] = plrGroup;
			game.OwnPlayerGroupMemembershipsById[plrGroup.Uid] = new PlayerGroupMembership
			{
				GroupName = plrGroup.Name,
				GroupUid = plrGroup.Uid,
				Level = (EnumPlayerGroupMemberShip)packet.PlayerGroups.Groups[i].Membership
			};
		}
		List<int> deletedGroups = new List<int>();
		foreach (int groupId2 in game.ChatHistoryByPlayerGroup.Keys)
		{
			if (!game.OwnPlayerGroupsById.ContainsKey(groupId2))
			{
				deletedGroups.Add(groupId2);
			}
		}
		foreach (int groupId in deletedGroups)
		{
			game.ChatHistoryByPlayerGroup.Remove(groupId);
		}
		if (!game.OwnPlayerGroupsById.ContainsKey(game.currentGroupid))
		{
			game.currentGroupid = GlobalConstants.GeneralChatGroup;
		}
		ComposeChatGuis();
	}

	private void HandlePlayerGroupPacket(Packet_Server packet)
	{
		PlayerGroup plrGroup = PlayerGroupFromPacket(packet.PlayerGroup);
		game.OwnPlayerGroupsById[plrGroup.Uid] = plrGroup;
		game.OwnPlayerGroupMemembershipsById[plrGroup.Uid] = new PlayerGroupMembership
		{
			GroupName = plrGroup.Name,
			GroupUid = plrGroup.Uid,
			Level = (EnumPlayerGroupMemberShip)packet.PlayerGroup.Membership
		};
		ComposeChatGuis();
	}

	private PlayerGroup PlayerGroupFromPacket(Packet_PlayerGroup packet)
	{
		PlayerGroup plrGroup = new PlayerGroup
		{
			Name = packet.Name,
			OwnerUID = packet.Owneruid,
			Uid = packet.Uid
		};
		for (int i = 0; i < packet.ChathistoryCount; i++)
		{
			plrGroup.ChatHistory.Add(new Vintagestory.API.Common.ChatLine
			{
				ChatType = (EnumChatType)packet.Chathistory[i].ChatType,
				Message = packet.Chathistory[i].Message
			});
		}
		if (plrGroup.ChatHistory.Count > historyMax)
		{
			plrGroup.ChatHistory.RemoveAt(0);
		}
		return plrGroup;
	}
}
