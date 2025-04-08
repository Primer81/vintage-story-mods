using System.Linq;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class GuiHandbookTutorialPage : GuiHandbookTextPage, IFlatListItemInteractable, IFlatListItem
{
	private ICoreClientAPI capi;

	private GuiElementTextButton startStopButton;

	private GuiElementTextButton restartButton;

	private ElementBounds baseBounds;

	private ModSystemTutorial modsys;

	private bool tutorialActive;

	private float prevProgress;

	public override string PageCode => pageCode;

	public override string CategoryCode => "tutorial";

	public override bool IsDuplicate => false;

	public GuiHandbookTutorialPage(ICoreClientAPI capi, string pagecode)
	{
		pageCode = pagecode;
		this.capi = capi;
		Title = Lang.Get("title-" + pagecode);
		Text = "";
		modsys = capi.ModLoader.GetModSystem<ModSystemTutorial>();
		recomposeButton();
	}

	private void recomposeButton()
	{
		tutorialActive = modsys.CurrentTutorial == pageCode.Substring("tutorial-".Length);
		startStopButton?.Dispose();
		restartButton?.Dispose();
		baseBounds = ElementBounds.Fixed(0.0, 0.0, 400.0, 100.0).WithParent(capi.Gui.WindowBounds);
		startStopButton = new GuiElementTextButton(capi, Lang.Get("Start"), CairoFont.WhiteSmallText(), CairoFont.WhiteSmallText(), onStartStopTutorial, ElementBounds.Fixed(0, 0).WithFixedPadding(6.0, 3.0).WithParent(baseBounds));
		restartButton = null;
		prevProgress = modsys.GetTutorialProgress(pageCode.Substring("tutorial-".Length));
		if (tutorialActive)
		{
			startStopButton.Text = Lang.Get("Stop Tutorial");
		}
		else if (prevProgress >= 1f)
		{
			startStopButton = null;
		}
		else
		{
			startStopButton.Text = Lang.Get((prevProgress > 0f) ? "button-tutorial-resume" : "Start Tutorial");
		}
		if (startStopButton != null)
		{
			compose(startStopButton);
		}
		if ((!tutorialActive && prevProgress > 0f) || prevProgress >= 1f)
		{
			ElementBounds rbounds = ElementBounds.Fixed(0, 0).WithFixedPadding(6.0, 3.0).WithParent(baseBounds);
			restartButton = new GuiElementTextButton(capi, Lang.Get("Restart"), CairoFont.WhiteSmallText(), CairoFont.WhiteSmallText(), onRestartTutorial, rbounds);
			compose(restartButton);
			rbounds.fixedX -= restartButton.Bounds.OuterWidth / (double)RuntimeEnv.GUIScale + 5.0;
			rbounds.CalcWorldBounds();
		}
	}

	private bool onRestartTutorial()
	{
		modsys.StartTutorial(pageCode.Substring("tutorial-".Length), restart: true);
		capi.Event.EnqueueMainThreadTask(delegate
		{
			capi.Gui.LoadedGuis.FirstOrDefault((GuiDialog dlg) => dlg is GuiDialogSurvivalHandbook)?.TryClose();
		}, "closehandbook");
		recomposeButton();
		return true;
	}

	private void compose(GuiElementTextButton button)
	{
		ImageSurface surface = new ImageSurface(Format.Argb32, 1, 1);
		Context ctx = new Context(surface);
		button.BeforeCalcBounds();
		button.ComposeElements(ctx, surface);
		ctx.Dispose();
		surface.Dispose();
	}

	private bool onStartStopTutorial()
	{
		if (!tutorialActive)
		{
			tutorialActive = true;
			modsys.StartTutorial(pageCode.Substring("tutorial-".Length));
			capi.Event.EnqueueMainThreadTask(delegate
			{
				capi.Gui.LoadedGuis.FirstOrDefault((GuiDialog dlg) => dlg is GuiDialogSurvivalHandbook)?.TryClose();
			}, "closehandbook");
		}
		else
		{
			tutorialActive = false;
			modsys.StopActiveTutorial();
			recomposeButton();
		}
		return true;
	}

	public override void RenderListEntryTo(ICoreClientAPI capi, float dt, double x, double y, double cellWdith, double cellHeight)
	{
		base.RenderListEntryTo(capi, dt, x, y, cellWdith, cellHeight);
		if (startStopButton == null && restartButton == null)
		{
			recomposeButton();
		}
		if (modsys.GetTutorialProgress(pageCode.Substring("tutorial-".Length)) != prevProgress)
		{
			recomposeButton();
		}
		baseBounds.absFixedX = x + cellWdith - (startStopButton?.Bounds.OuterWidth ?? 0.0) - 10.0;
		baseBounds.absFixedY = y;
		startStopButton?.RenderInteractiveElements(dt);
		if (restartButton != null)
		{
			restartButton?.RenderInteractiveElements(dt);
		}
		if (tutorialActive && "tutorial-" + modsys.CurrentTutorial != pageCode)
		{
			tutorialActive = false;
			recomposeButton();
		}
	}

	public override void ComposePage(GuiComposer detailViewGui, ElementBounds textBounds, ItemStack[] allstacks, ActionConsumable<string> openDetailPageFor)
	{
		RichTextComponentBase[] comps = capi.ModLoader.GetModSystem<ModSystemTutorial>().GetPageText(pageCode, skipOld: false);
		detailViewGui.AddRichtext(comps, textBounds, "richtext");
	}

	public override float GetTextMatchWeight(string text)
	{
		return 0f;
	}

	public override void Dispose()
	{
		startStopButton?.Dispose();
		startStopButton = null;
		restartButton?.Dispose();
		restartButton = null;
	}

	public void OnMouseMove(ICoreClientAPI api, MouseEvent args)
	{
		startStopButton?.OnMouseMove(api, args);
		if (!args.Handled)
		{
			restartButton?.OnMouseMove(api, args);
		}
	}

	public void OnMouseDown(ICoreClientAPI api, MouseEvent args)
	{
		startStopButton?.OnMouseDown(api, args);
		if (!args.Handled)
		{
			restartButton?.OnMouseDown(api, args);
		}
	}

	public void OnMouseUp(ICoreClientAPI api, MouseEvent args)
	{
		startStopButton?.OnMouseUp(api, args);
		if (!args.Handled)
		{
			restartButton?.OnMouseUp(api, args);
		}
	}
}
