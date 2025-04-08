using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace Vintagestory.Client.NoObf;

public class GuiDialogStoryGenFailed : GuiDialog
{
	public StoryGenFailed storyGenFailed;

	public bool isInitilized;

	public override string ToggleKeyCombinationCode => null;

	public GuiDialogStoryGenFailed(ICoreClientAPI capi)
		: base(capi)
	{
	}

	private void Compose()
	{
		CairoFont font = CairoFont.WhiteSmallText();
		ElementBounds bgBounds = ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding);
		ElementBounds textBounds = ElementBounds.Fixed(0.0, 0.0, 600.0, 500.0);
		ElementBounds titleBounds = ElementBounds.Fixed(EnumDialogArea.LeftTop, 0.0, 0.0, 690.0, 30.0);
		ElementBounds insetBounds = textBounds.ForkBoundingParent(5.0, 5.0, 5.0, 5.0).FixedUnder(titleBounds);
		ElementBounds clippingBounds = textBounds.CopyOffsetedSibling();
		ElementBounds scrollbarBounds = ElementStdBounds.VerticalScrollbar(insetBounds);
		string text = Lang.Get("storygenfailed-text");
		string message = ((storyGenFailed?.MissingStructures != null) ? string.Join(",", storyGenFailed.MissingStructures) : "");
		text = text + "\n" + message + "<br><br>";
		base.SingleComposer = capi.Gui.CreateCompo("storygenfailed", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(bgBounds).AddDialogTitleBar(Lang.Get("Automatic Story Location Generation Failed"), OnTitleBarClose)
			.BeginChildElements(bgBounds)
			.AddInset(insetBounds)
			.AddVerticalScrollbar(OnNewScrollbarvalue, scrollbarBounds, "scrollbar")
			.BeginClip(clippingBounds)
			.AddRichtext(text, font, textBounds, null, "storygenfailed")
			.EndClip()
			.EndChildElements()
			.Compose();
		clippingBounds.CalcWorldBounds();
		base.SingleComposer.GetScrollbar("scrollbar").SetHeights((float)clippingBounds.fixedHeight, (float)textBounds.fixedHeight);
	}

	private void OnNewScrollbarvalue(float value)
	{
		ElementBounds bounds = base.SingleComposer.GetRichtext("storygenfailed").Bounds;
		bounds.fixedY = 10f - value;
		bounds.CalcWorldBounds();
	}

	private bool OnOk()
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
		isInitilized = true;
		if (storyGenFailed != null)
		{
			Compose();
			TryOpen();
		}
	}
}
