using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogBlockEntityCommand : GuiDialogBlockEntity
{
	public override string ToggleKeyCombinationCode => null;

	public GuiDialogBlockEntityCommand(BlockPos BlockEntityPosition, string command, bool silent, ICoreClientAPI capi, string title)
		: base("Command block", BlockEntityPosition, capi)
	{
		_ = GuiElementItemSlotGridBase.unscaledSlotPadding;
		int spacing = 5;
		_ = GuiElementPassiveItemSlot.unscaledSlotSize;
		_ = GuiElementItemSlotGridBase.unscaledSlotPadding;
		double innerWidth = 700.0;
		_ = innerWidth / 2.0;
		ElementBounds commmandsBounds = ElementBounds.Fixed(0.0, 30.0, innerWidth, 30.0);
		ElementBounds textAreaBounds = ElementBounds.Fixed(0.0, 0.0, innerWidth - 20.0, 200.0);
		ElementBounds clippingBounds = ElementBounds.Fixed(0.0, 0.0, innerWidth - 20.0 - 1.0, 199.0).FixedUnder(commmandsBounds, spacing - 10);
		ElementBounds scrollbarBounds = clippingBounds.CopyOffsetedSibling(clippingBounds.fixedWidth + 6.0, -1.0).WithFixedWidth(20.0).FixedGrow(0.0, 2.0);
		ElementBounds toClipboardBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clippingBounds, 2 + 2 * spacing).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds cancelBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(toClipboardBounds, 44 + 2 * spacing).WithAlignment(EnumDialogArea.LeftFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds saveBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(toClipboardBounds, 44 + 2 * spacing).WithAlignment(EnumDialogArea.RightFixed)
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
			.AddStaticText("Commands", CairoFont.WhiteSmallText(), commmandsBounds)
			.BeginClip(clippingBounds)
			.AddTextArea(textAreaBounds, OnCommandCodeChanged, CairoFont.TextInput().WithFontSize(16f), "commands")
			.EndClip()
			.AddVerticalScrollbar(OnNewCmdScrollbarvalue, scrollbarBounds, "scrollbar")
			.AddSwitch(null, textAreaBounds.BelowCopy(0.0, 10.0).WithFixedSize(30.0, 30.0), "silentSwitch", 25.0, 3.0)
			.AddStaticText(Lang.Get("Execute silently"), CairoFont.WhiteSmallText(), textAreaBounds.BelowCopy(30.0, 13.0).WithFixedSize(150.0, 30.0))
			.AddSmallButton(Lang.Get("Copy to clipboard"), OnCopy, toClipboardBounds)
			.AddSmallButton(Lang.Get("Cancel"), OnCancel, cancelBounds)
			.AddSmallButton(Lang.Get("Save"), OnSave, saveBounds)
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetTextArea("commands").SetValue(command);
		base.SingleComposer.GetTextArea("commands").OnCursorMoved = OnTextAreaCursorMoved;
		base.SingleComposer.GetSwitch("silentSwitch").On = silent;
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
		bool silent = base.SingleComposer.GetSwitch("silentSwitch").On;
		capi.Network.SendBlockEntityPacket(base.BlockEntityPosition, 12, SerializerUtil.Serialize(new BlockEntityCommandPacket
		{
			Commands = commands,
			Silent = silent
		}));
		TryClose();
		return true;
	}

	private void OnCommandCodeChanged(string t1)
	{
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
