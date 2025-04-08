using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class GuiDialogBlockEntityTicker : GuiDialogBlockEntity
{
	public override string ToggleKeyCombinationCode => null;

	public GuiDialogBlockEntityTicker(BlockPos pos, int tickIntervalMs, bool active, ICoreClientAPI capi)
		: base("Command block", pos, capi)
	{
		_ = GuiElementItemSlotGridBase.unscaledSlotPadding;
		int spacing = 5;
		_ = GuiElementPassiveItemSlot.unscaledSlotSize;
		_ = GuiElementItemSlotGridBase.unscaledSlotPadding;
		double innerWidth = 400.0;
		_ = innerWidth / 2.0;
		ElementBounds commmandsBounds = ElementBounds.Fixed(0.0, 34.0, innerWidth, 30.0);
		ElementBounds autoNumberBounds = ElementBounds.Fixed(110.0, 26.0, 80.0, 30.0);
		ElementBounds cancelBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(commmandsBounds, 20 + 2 * spacing).WithFixedPadding(10.0, 2.0);
		ElementBounds saveBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(commmandsBounds, 20 + 2 * spacing).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(10.0, 2.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0);
		if (base.SingleComposer != null)
		{
			base.SingleComposer.Dispose();
		}
		base.SingleComposer = capi.Gui.CreateCompo("commandeditordialog", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar("Ticker Block", OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddStaticText("Timer (ms)", CairoFont.WhiteSmallText(), commmandsBounds)
			.AddNumberInput(autoNumberBounds, null, CairoFont.WhiteDetailText(), "automs")
			.AddSwitch(null, autoNumberBounds.RightCopy(90.0, 3.0).WithFixedPadding(0.0, 0.0), "onSwitch", 25.0, 3.0)
			.AddStaticText("Active", CairoFont.WhiteSmallText(), autoNumberBounds.RightCopy(120.0, 6.0))
			.AddSmallButton("Cancel", OnCancel, cancelBounds)
			.AddSmallButton("Save", OnSave, saveBounds)
			.EndChildElements()
			.Compose();
		base.SingleComposer.GetNumberInput("automs").SetValue(tickIntervalMs);
		base.SingleComposer.GetSwitch("onSwitch").On = active;
		base.SingleComposer.UnfocusOwnElements();
	}

	private bool OnCancel()
	{
		TryClose();
		return true;
	}

	private bool OnSave()
	{
		EditTickerPacket packet = new EditTickerPacket
		{
			Interval = (base.SingleComposer.GetNumberInput("automs").GetValue().ToString() ?? ""),
			Active = base.SingleComposer.GetSwitch("onSwitch").On
		};
		capi.Network.SendBlockEntityPacket(base.BlockEntityPosition, 12, SerializerUtil.Serialize(packet));
		TryClose();
		return true;
	}

	private void OnTitleBarClose()
	{
		TryClose();
	}
}
