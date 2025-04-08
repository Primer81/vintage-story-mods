using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class GuiDialogFirstlaunchInfo : GuiDialog
{
	private string playstyle;

	public override string ToggleKeyCombinationCode => "firstlaunchinfo";

	public GuiDialogFirstlaunchInfo(ICoreClientAPI capi)
		: base(capi)
	{
		Compose();
		capi.ChatCommands.Create("firstlaunchinfo").WithDescription("Show the first launch info dialog").HandleWith(OnCmd);
	}

	private TextCommandResult OnCmd(TextCommandCallingArgs textCommandCallingArgs)
	{
		if (IsOpened())
		{
			TryClose();
		}
		else
		{
			TryOpen();
		}
		return TextCommandResult.Success();
	}

	private void Compose()
	{
		string code = ((playstyle == "creativebuilding") ? Lang.Get("start-creativeintro") : Lang.Get("start-survivalintro"));
		CairoFont font = CairoFont.WhiteSmallText().WithLineHeightMultiplier(1.149999976158142);
		RichTextComponentBase[] comps = VtmlUtil.Richtextify(capi, code, font, didClickLink);
		ElementBounds bounds = ElementBounds.Fixed(0.0, 0.0, 400.0, 300.0);
		bounds.ParentBounds = ElementBounds.Empty;
		GuiElementRichtext elem = new GuiElementRichtext(capi, comps, bounds);
		elem.BeforeCalcBounds();
		bounds.ParentBounds = null;
		float y = (float)(elem.Bounds.fixedY + elem.Bounds.fixedHeight);
		ClearComposers();
		base.SingleComposer = capi.Gui.CreateCompo("helpdialog", ElementStdBounds.AutosizedMainDialog).AddShadedDialogBG(ElementStdBounds.DialogBackground().WithFixedPadding(GuiStyle.ElementToDialogPadding, GuiStyle.ElementToDialogPadding), withTitleBar: false).BeginChildElements()
			.AddInteractiveElement(elem)
			.AddSmallButton(Lang.Get("button-close"), OnClose, ElementStdBounds.MenuButton((y + 50f) / 80f).WithAlignment(EnumDialogArea.RightFixed).WithFixedPadding(6.0))
			.AddSmallButton(Lang.Get("button-close-noshow"), OnCloseAndDontShow, ElementStdBounds.MenuButton((y + 50f) / 80f).WithAlignment(EnumDialogArea.LeftFixed).WithFixedPadding(6.0))
			.EndChildElements()
			.Compose();
	}

	private void didClickLink(LinkTextComponent component)
	{
		TryClose();
		component.HandleLink();
	}

	public override void OnGuiOpened()
	{
		Compose();
		base.OnGuiOpened();
	}

	private bool OnCloseAndDontShow()
	{
		TryClose();
		if (playstyle == "creativebuilding")
		{
			ClientSettings.ShowCreativeHelpDialog = false;
		}
		else
		{
			ClientSettings.ShowSurvivalHelpDialog = false;
		}
		return true;
	}

	private bool OnClose()
	{
		TryClose();
		return true;
	}

	public override void OnLevelFinalize()
	{
		playstyle = (capi.World as ClientMain).ServerInfo.Playstyle;
	}
}
