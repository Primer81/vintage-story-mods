using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace Vintagestory.Client;

public class GuiScreenDisconnected : GuiScreen
{
	public GuiScreenDisconnected(string reason, ScreenManager screenManager, GuiScreen parentScreen, string title = "server-disconnected")
		: base(screenManager, parentScreen)
	{
		ScreenManager.GuiComposers.ClearCache();
		ElementComposer = ScreenManager.GuiComposers.Create("mainmenu-disconnected", ElementBounds.Fixed(EnumDialogArea.CenterTop, 0.0, 20.0, 710.0, 330.0).WithAlignment(EnumDialogArea.CenterMiddle).WithFixedMargin(5.0)).AddShadedDialogBG(ElementBounds.Fill, withTitleBar: false, 5.0, 1f).AddRichtext(Lang.Get(title), CairoFont.WhiteSmallishText().WithOrientation(EnumTextOrientation.Center), ElementBounds.Fixed(EnumDialogArea.CenterTop, 0.0, 20.0, 690.0, 60.0), didClickLink, "centertext")
			.AddRichtext(reason, CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), ElementBounds.Fixed(EnumDialogArea.CenterTop, 0.0, 65.0, 690.0, 450.0), didClickLink, "centertextreason")
			.AddButton(Lang.Get("Back to main menu"), OnBack, ElementStdBounds.MenuButton(4.5f).WithFixedPadding(5.0, 3.0).WithFixedMargin(5.0))
			.EndChildElements();
		ElementComposer.GetRichtext("centertextreason").MaxHeight = 450;
		ElementComposer.Compose();
	}

	private void didClickLink(LinkTextComponent link)
	{
		ScreenManager.StartMainMenu();
		ScreenManager.EnqueueMainThreadTask(delegate
		{
			ScreenManager.api.Gui.OpenLink(link.Href);
		});
	}

	private bool OnBack()
	{
		ScreenManager.StartMainMenu();
		return true;
	}

	public override void RenderToDefaultFramebuffer(float dt)
	{
		if (ScreenManager.KeyboardKeyState[50])
		{
			ScreenManager.StartMainMenu();
			return;
		}
		ElementComposer.Render(dt);
		ScreenManager.RenderMainMenuParts(dt, ElementComposer.Bounds, withMainMenu: false);
		if (ScreenManager.mainMenuComposer.MouseOverCursor != null)
		{
			FocusedMouseCursor = ScreenManager.mainMenuComposer.MouseOverCursor;
		}
		ElementComposer.PostRender(dt);
	}
}
