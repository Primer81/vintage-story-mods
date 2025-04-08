using Vintagestory.API.Client;

namespace Vintagestory.GameContent;

public class HudTutorial : HudElement
{
	public HudTutorial(ICoreClientAPI capi)
		: base(capi)
	{
	}

	public void loadHud(string pagecode)
	{
		ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 300.0, 200.0);
		ElementBounds bgBounds = new ElementBounds().WithSizing(ElementSizing.FitToChildren).WithFixedPadding(GuiStyle.ElementToDialogPadding / 2.0);
		bgBounds.WithChildren(textBounds);
		ElementBounds dialogBounds = bgBounds.ForkBoundingParent().WithAlignment(EnumDialogArea.None).WithAlignment(EnumDialogArea.RightMiddle)
			.WithFixedPosition(0.0, -225.0);
		RichTextComponentBase[] cmps = capi.ModLoader.GetModSystem<ModSystemTutorial>().GetPageText(pagecode, skipOld: true);
		base.SingleComposer?.Dispose();
		base.SingleComposer = capi.Gui.CreateCompo("tutorialhud", dialogBounds).AddGameOverlay(bgBounds, GuiStyle.DialogLightBgColor).AddRichtext(cmps, textBounds, "richtext")
			.Compose();
	}

	public override void Dispose()
	{
		base.Dispose();
	}
}
