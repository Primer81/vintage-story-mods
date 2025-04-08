using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

public class GuiDialogLogViewer : GuiDialogGeneric
{
	public GuiDialogLogViewer(string text, ICoreClientAPI capi)
		: base("Log Viewer", capi)
	{
		ElementBounds topTextBounds = ElementBounds.Fixed(GuiStyle.ElementToDialogPadding, 40.0, 900.0, 30.0);
		ElementBounds logtextBounds = ElementBounds.Fixed(0.0, 0.0, 900.0, 300.0).FixedUnder(topTextBounds, 5.0);
		ElementBounds clippingBounds = logtextBounds.ForkBoundingParent();
		ElementBounds insetBounds = logtextBounds.FlatCopy().FixedGrow(6.0).WithFixedOffset(-3.0, -3.0);
		ElementBounds scrollbarBounds = insetBounds.CopyOffsetedSibling(logtextBounds.fixedWidth + 7.0).WithFixedWidth(20.0);
		ElementBounds closeButtonBounds = ElementBounds.FixedSize(0.0, 0.0).FixedUnder(clippingBounds, 10.0).WithAlignment(EnumDialogArea.RightFixed)
			.WithFixedPadding(20.0, 4.0);
		ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		bgBounds.BothSizing = ElementSizing.FitToChildren;
		bgBounds.WithChildren(insetBounds, clippingBounds, scrollbarBounds, closeButtonBounds);
		ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
		base.SingleComposer = capi.Gui.CreateCompo("dialogviewer", dialogBounds).AddShadedDialogBG(bgBounds).AddDialogTitleBar(DialogTitle, OnTitleBarClose)
			.AddStaticText("The following warnings and errors were reported during startup:", CairoFont.WhiteDetailText(), topTextBounds)
			.BeginChildElements(bgBounds)
			.BeginClip(clippingBounds)
			.AddInset(insetBounds, 3)
			.AddDynamicText("", CairoFont.WhiteDetailText(), logtextBounds, "text")
			.EndClip()
			.AddVerticalScrollbar(OnNewScrollbarvalue, scrollbarBounds, "scrollbar")
			.AddSmallButton("Close", OnButtonClose, closeButtonBounds)
			.EndChildElements()
			.Compose();
		GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("text");
		dynamicText.AutoHeight();
		dynamicText.SetNewText(text);
		base.SingleComposer.GetScrollbar("scrollbar").SetHeights(300f, (float)logtextBounds.fixedHeight);
	}

	private void OnNewScrollbarvalue(float value)
	{
		GuiElementDynamicText dynamicText = base.SingleComposer.GetDynamicText("text");
		dynamicText.Bounds.fixedY = 3f - value;
		dynamicText.Bounds.CalcWorldBounds();
	}

	private void OnTitleBarClose()
	{
		OnButtonClose();
	}

	private bool OnButtonClose()
	{
		TryClose();
		return true;
	}
}
