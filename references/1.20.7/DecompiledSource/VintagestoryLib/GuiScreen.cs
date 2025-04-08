using System;
using Vintagestory.API.Client;
using Vintagestory.Client;
using Vintagestory.Client.NoObf;

public class GuiScreen
{
	public GuiComposer ElementComposer;

	public ScreenManager ScreenManager;

	public GuiScreen ParentScreen;

	public bool ShowMainMenu;

	public string UnfocusedMouseCursor = "normal";

	public string FocusedMouseCursor;

	protected int tabIndex;

	protected ElementBounds dlgBounds;

	public virtual bool ShouldDisposePreviousScreen { get; } = true;


	public bool IsOpened => ScreenManager.CurrentScreen == this;

	public bool RenderBg { get; set; } = true;


	public GuiScreen(ScreenManager screenManager, GuiScreen parentScreen)
	{
		ScreenManager = screenManager;
		ParentScreen = parentScreen;
	}

	protected GuiComposer dialogBase(string name, double unScWidth = -1.0, double unScHeight = -1.0)
	{
		int windowHeight = ScreenManager.GamePlatform.WindowSize.Height;
		int windowWidth = ScreenManager.GamePlatform.WindowSize.Width;
		if (unScWidth < 0.0)
		{
			unScWidth = Math.Max(400.0, (double)windowWidth * 0.5) / (double)ClientSettings.GUIScale + 40.0;
		}
		if (unScHeight < 0.0)
		{
			unScHeight = (float)Math.Max(300, windowHeight) / ClientSettings.GUIScale - 120f;
		}
		double p = GuiStyle.ElementToDialogPadding;
		ElementBounds innerbounds = ElementBounds.Fixed(0.0, 0.0, unScWidth, unScHeight);
		dlgBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.FixedMiddle);
		ElementBounds bgBounds = innerbounds.ForkBoundingParent(p, p / 2.0, p, p);
		GuiComposer guiComposer = ScreenManager.GuiComposers.Create(name, dlgBounds).AddShadedDialogBG(bgBounds, withTitleBar: false).BeginChildElements(innerbounds);
		guiComposer.OnComposed += Cmp_OnRecomposed;
		return guiComposer;
	}

	private void Cmp_OnRecomposed()
	{
		double windowWidth = ScreenManager.GamePlatform.WindowSize.Width;
		double menuWidth = ScreenManager.guiMainmenuLeft.Width;
		dlgBounds.absOffsetX = menuWidth + (windowWidth - menuWidth - dlgBounds.OuterWidth) / 2.0;
	}

	public void BubbleUpEvent(string eventCode)
	{
		BubbleUpEvent(eventCode, null);
	}

	public virtual void Refresh()
	{
	}

	public void BubbleUpEvent(string eventCode, object arg)
	{
		GuiScreen cur = this;
		while (!cur.OnEvent(eventCode, arg))
		{
			cur = cur.ParentScreen;
			if (cur == null)
			{
				break;
			}
		}
	}

	public virtual bool OnEvent(string eventCode, object arg)
	{
		return false;
	}

	public virtual void RenderToPrimary(float dt)
	{
		FocusedMouseCursor = null;
	}

	public virtual void RenderAfterPostProcessing(float dt)
	{
	}

	public virtual void RenderAfterFinalComposition(float dt)
	{
	}

	public virtual void RenderAfterBlit(float dt)
	{
	}

	public virtual void RenderToDefaultFramebuffer(float dt)
	{
		ElementComposer.Render(dt);
		if (ElementComposer.MouseOverCursor != null)
		{
			FocusedMouseCursor = ElementComposer.MouseOverCursor;
		}
		ScreenManager.RenderMainMenuParts(dt, ElementComposer.Bounds, ShowMainMenu);
		if (ScreenManager.mainMenuComposer.MouseOverCursor != null)
		{
			FocusedMouseCursor = ScreenManager.mainMenuComposer.MouseOverCursor;
		}
		ElementComposer.PostRender(dt);
		ScreenManager.GamePlatform.UseMouseCursor((FocusedMouseCursor != null) ? FocusedMouseCursor : UnfocusedMouseCursor);
	}

	public virtual bool OnFileDrop(string filename)
	{
		return false;
	}

	public virtual void OnKeyDown(KeyEvent e)
	{
		if (ElementComposer != null)
		{
			ElementComposer.OnKeyDown(e, haveFocus: true);
		}
		if (!e.Handled && e.KeyCode == 52)
		{
			ElementComposer.FocusElement(++tabIndex);
			if (tabIndex > ElementComposer.MaxTabIndex)
			{
				ElementComposer.FocusElement(0);
				tabIndex = 0;
			}
			e.Handled = true;
		}
	}

	public virtual void OnKeyPress(KeyEvent e)
	{
		if (ElementComposer != null)
		{
			ElementComposer.OnKeyPress(e);
		}
	}

	public virtual void OnKeyUp(KeyEvent e)
	{
	}

	public virtual void OnMouseDown(MouseEvent e)
	{
		if (ShowMainMenu)
		{
			ScreenManager.guiMainmenuLeft.OnMouseDown(e);
			if (e.Handled)
			{
				return;
			}
		}
		if (ElementComposer != null)
		{
			ElementComposer.OnMouseDown(e);
			_ = e.Handled;
		}
	}

	public virtual void OnMouseUp(MouseEvent e)
	{
		if (ShowMainMenu)
		{
			ScreenManager.guiMainmenuLeft.OnMouseUp(e);
			if (e.Handled)
			{
				return;
			}
		}
		if (ElementComposer != null)
		{
			ElementComposer.OnMouseUp(e);
			_ = e.Handled;
		}
	}

	public virtual void OnMouseMove(MouseEvent e)
	{
		if (ShowMainMenu)
		{
			ScreenManager.guiMainmenuLeft.OnMouseMove(e);
			if (e.Handled)
			{
				return;
			}
		}
		if (ElementComposer != null)
		{
			ElementComposer.OnMouseMove(e);
			_ = e.Handled;
		}
	}

	public virtual void OnMouseWheel(MouseWheelEventArgs e)
	{
		if (ElementComposer != null)
		{
			ElementComposer.OnMouseWheel(e);
			_ = e.IsHandled;
		}
	}

	public virtual bool OnBackPressed()
	{
		return true;
	}

	public virtual void OnWindowClosed()
	{
	}

	public virtual void OnFocusChanged(bool focus)
	{
	}

	public virtual void OnScreenUnload()
	{
	}

	public virtual void OnScreenLoaded()
	{
		if (!ScreenManager.sessionManager.IsCachedSessionKeyValid())
		{
			ScreenManager.Platform.Logger.Notification("Cached session key is invalid, require login");
			ScreenManager.Platform.ToggleOffscreenBuffer(enable: true);
			ScreenManager.LoadAndCacheScreen(typeof(GuiScreenLogin));
		}
	}

	public virtual void ReloadWorld(string reason)
	{
	}

	public virtual void Dispose()
	{
		ElementComposer?.Dispose();
	}
}
