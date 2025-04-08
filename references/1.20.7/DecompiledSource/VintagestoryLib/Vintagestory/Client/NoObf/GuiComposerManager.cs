using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class GuiComposerManager : IGuiComposerManager
{
	internal Dictionary<string, GuiComposer> dialogComposers = new Dictionary<string, GuiComposer>();

	private ICoreClientAPI api;

	public Dictionary<string, GuiComposer> Composers => dialogComposers;

	public void ClearCache()
	{
		foreach (KeyValuePair<string, GuiComposer> dialogComposer in dialogComposers)
		{
			dialogComposer.Value.Dispose();
		}
		dialogComposers.Clear();
	}

	public void ClearCached(string dialogName)
	{
		GuiComposer composer = null;
		if (dialogComposers.TryGetValue(dialogName, out composer))
		{
			composer.Dispose();
			dialogComposers.Remove(dialogName);
		}
	}

	public void Dispose(string dialogName)
	{
		GuiComposer composer = null;
		if (dialogComposers.TryGetValue(dialogName, out composer))
		{
			composer?.Dispose();
			dialogComposers.Remove(dialogName);
		}
	}

	public GuiComposerManager(ICoreClientAPI api)
	{
		this.api = api;
	}

	public GuiComposer Create(string dialogName, ElementBounds bounds)
	{
		GuiComposer composer;
		if (dialogComposers.ContainsKey(dialogName))
		{
			composer = dialogComposers[dialogName];
			composer.Dispose();
		}
		if (bounds.ParentBounds == null)
		{
			bounds.ParentBounds = new ElementWindowBounds();
		}
		composer = new GuiComposer(api, bounds, dialogName);
		composer.composerManager = this;
		dialogComposers[dialogName] = composer;
		return composer;
	}

	public void RecomposeAllDialogs()
	{
		Stopwatch watch = Stopwatch.StartNew();
		foreach (GuiComposer composer in dialogComposers.Values)
		{
			watch.Restart();
			composer.Composed = false;
			composer.Compose();
			ScreenManager.Platform.CheckGlError("recomp - " + composer.DialogName);
			ScreenManager.Platform.Logger.Notification("Recomposed dialog {0} in {1}s", composer.DialogName, Math.Round((float)watch.ElapsedMilliseconds / 1000f, 3));
		}
	}

	public void MarkAllDialogsForRecompose()
	{
		foreach (GuiComposer value in dialogComposers.Values)
		{
			value.recomposeOnRender = true;
		}
	}

	public void UnfocusElements()
	{
		UnfocusElementsExcept(null, null);
	}

	public void UnfocusElementsExcept(GuiComposer newFocusedComposer, GuiElement newFocusedElement)
	{
		foreach (GuiComposer composer in dialogComposers.Values)
		{
			if (newFocusedComposer != composer)
			{
				composer.UnfocusOwnElementsExcept(newFocusedElement);
			}
		}
	}
}
