using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogBlockEntityConditional : GuiDialogBlockEntity
{
	public override string ToggleKeyCombinationCode => null;

	public GuiDialogBlockEntityConditional(BlockPos BlockEntityPosition, string command, bool latching, ICoreClientAPI capi, string title)
		: base("Conditional block", BlockEntityPosition, capi)
	{
		_ = GuiElementItemSlotGridBase.unscaledSlotPadding;
		int spacing = 5;
		_ = GuiElementPassiveItemSlot.unscaledSlotSize;
		_ = GuiElementItemSlotGridBase.unscaledSlotPadding;
		double innerWidth = 600.0;
		_ = innerWidth / 2.0;
		ElementBounds commmandsBounds = ElementBounds.Fixed(0.0, 30.0, innerWidth, 30.0);
		ElementBounds textAreaBounds = ElementBounds.Fixed(0.0, 0.0, innerWidth - 20.0, 80.0);
		ElementBounds clippingBounds = ElementBounds.Fixed(0.0, 0.0, innerWidth - 20.0 - 1.0, 79.0).FixedUnder(commmandsBounds, spacing - 10);
		ElementBounds scrollbarBounds = clippingBounds.CopyOffsetedSibling(clippingBounds.fixedWidth + 6.0, -1.0).WithFixedWidth(20.0).FixedGrow(0.0, 2.0);
		ElementBounds labelBounds = ElementBounds.Fixed(0.0, 0.0, innerWidth - 40.0, 20.0).FixedUnder(clippingBounds, 2 + 2 * spacing);
		ElementBounds resultBounds = ElementBounds.Fixed(0.0, 0.0, innerWidth - 20.0, 25.0).FixedUnder(labelBounds, 2.0);
		ElementBounds toClipboardBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(resultBounds, 4 + 2 * spacing).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds cancelBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(toClipboardBounds, 34 + 2 * spacing).WithFixedPadding(10.0, 2.0);
		ElementBounds saveBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(toClipboardBounds, 34 + 2 * spacing).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		if (base.SingleComposer != null)
		{
			base.SingleComposer.Dispose();
		}
		base.SingleComposer = capi.Gui.CreateCompo("commandeditordialog", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(title, OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddStaticText(Lang.Get("Condition (e.g. e[type=gazelle,range=10])"), CairoFont.WhiteSmallText(), commmandsBounds)
			.BeginClip(clippingBounds)
			.AddTextArea(textAreaBounds, OnCommandCodeChanged, CairoFont.TextInput().WithFontSize(16f), "commands")
			.EndClip()
			.AddVerticalScrollbar(OnNewCmdScrollbarvalue, scrollbarBounds, "scrollbar")
			.AddStaticText(Lang.Get("Condition syntax status"), CairoFont.WhiteSmallText(), labelBounds)
			.AddInset(resultBounds)
			.AddDynamicText("", CairoFont.WhiteSmallText(), resultBounds.ForkContainingChild(2.0, 2.0, 2.0, 2.0), "result")
			.AddSmallButton(Lang.Get("Cancel"), OnCancel, cancelBounds)
			.AddSwitch(null, resultBounds.BelowCopy(0.0, 10.0).WithFixedSize(30.0, 30.0), "latchingSwitch", 25.0, 3.0)
			.AddStaticText(Lang.Get("Latching"), CairoFont.WhiteSmallText(), resultBounds.BelowCopy(30.0, 13.0).WithFixedSize(150.0, 30.0))
			.AddHoverText(Lang.Get("If latching is enabled, a repeatedly ticked Conditional Block only activates neibouring Command Block once, each time the condition changes"), CairoFont.WhiteSmallText(), 250, resultBounds.BelowCopy(25.0, 10.0).WithFixedSize(82.0, 25.0))
			.AddSmallButton(Lang.Get("Copy to clipboard"), OnCopy, toClipboardBounds)
			.AddSmallButton(Lang.Get("Save"), OnSave, saveBounds)
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetTextArea("commands").SetValue(command);
		base.SingleComposer.GetTextArea("commands").OnCursorMoved = OnTextAreaCursorMoved;
		base.SingleComposer.GetSwitch("latchingSwitch").On = latching;
		base.SingleComposer.GetScrollbar("scrollbar").SetHeights((float)textAreaBounds.fixedHeight - 1f, (float)textAreaBounds.fixedHeight);
		base.SingleComposer.UnfocusOwnElements();
	}

	private bool OnCopy()
	{
		capi.Input.ClipboardText = base.SingleComposer.GetTextArea("commands").GetText();
		return true;
	}

	private void OnNewCmdScrollbarvalue(float value)
	{
		GuiElementTextArea textArea = base.SingleComposer.GetTextArea("commands");
		textArea.Bounds.fixedY = 1f - value;
		textArea.Bounds.CalcWorldBounds();
	}

	private bool OnCancel()
	{
		TryClose();
		return true;
	}

	private bool OnSave()
	{
		string commands = base.SingleComposer.GetTextArea("commands").GetText();
		bool latching = base.SingleComposer.GetSwitch("latchingSwitch").On;
		capi.Network.SendBlockEntityPacket(base.BlockEntityPosition, 12, SerializerUtil.Serialize(new BlockEntityCommandPacket
		{
			Commands = commands,
			Silent = latching
		}));
		TryClose();
		return true;
	}

	private void OnCommandCodeChanged(string t1)
	{
		string s = t1.Trim();
		if (s.Length == 0)
		{
			base.SingleComposer.GetDynamicText("result").SetNewText("");
			return;
		}
		string display = "Ok";
		ICommandArgumentParser test = new EntitiesArgParser("test", capi, isMandatoryArg: true);
		TextCommandCallingArgs textCommandCallingArgs = new TextCommandCallingArgs();
		textCommandCallingArgs.Caller = new Caller
		{
			Type = EnumCallerType.Console,
			CallerRole = "admin",
			CallerPrivileges = new string[1] { "*" },
			FromChatGroupId = GlobalConstants.ConsoleGroup,
			Pos = new Vec3d(0.5, 0.5, 0.5)
		};
		textCommandCallingArgs.RawArgs = new CmdArgs(s);
		TextCommandCallingArgs packedArgs = textCommandCallingArgs;
		if (test.TryProcess(packedArgs) != 0)
		{
			display = test.LastErrorMessage;
		}
		base.SingleComposer.GetDynamicText("result").SetNewText(display);
	}

	private void OnTextAreaCursorMoved(double posX, double posY)
	{
		double lineHeight = base.SingleComposer.GetTextArea("commands").Font.GetFontExtents().Height;
		base.SingleComposer.GetScrollbar("scrollbar").EnsureVisible(posX, posY);
		base.SingleComposer.GetScrollbar("scrollbar").EnsureVisible(posX, posY + lineHeight + 5.0);
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}
}
