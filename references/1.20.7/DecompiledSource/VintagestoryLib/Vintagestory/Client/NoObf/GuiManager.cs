using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf;

public class GuiManager : ClientSystem
{
	public static bool DEBUG_PRINT_INTERACTIONS;

	internal InventoryItemRenderer inventoryItemRenderer;

	private bool ignoreFocusEvents;

	private GuiDialog prevMousedOverDialog;

	private bool didHoverSlotEventTrigger;

	private ItemSlot prevHoverSlot;

	public override string Name => "gdm";

	public IWorldAccessor World => game;

	public GuiManager(ClientMain game)
		: base(game)
	{
		inventoryItemRenderer = new InventoryItemRenderer(game);
		game.eventManager.OnGameWindowFocus.Add(FocusChanged);
		game.eventManager.OnDialogOpened.Add(OnGuiOpened);
		game.eventManager.OnDialogClosed.Add(OnGuiClosed);
		RegisterDefaultDialogs();
		game.eventManager.RegisterRenderer(OnBeforeRenderFrame3D, EnumRenderStage.Before, Name, 0.1);
		game.eventManager.RegisterRenderer(OnFinalizeFrame, EnumRenderStage.Done, Name, 0.1);
		game.Logger.Notification("Initialized GUI Manager");
	}

	public override void OnServerIdentificationReceived()
	{
		game.eventManager?.RegisterRenderer(OnRenderFrameGUI, EnumRenderStage.Ortho, Name, 1.0);
	}

	private void FocusChanged(bool focus)
	{
	}

	public void RegisterDefaultDialogs()
	{
		game.RegisterDialog(new HudEntityNameTags(game.api), new GuiDialogEscapeMenu(game.api), new HudIngameError(game.api), new HudIngameDiscovery(game.api), new HudDialogChat(game.api), new HudElementInteractionHelp(game.api), new HudHotbar(game.api), new HudStatbar(game.api), new GuiDialogInventory(game.api), new GuiDialogCharacter(game.api), new GuiDialogConfirmRemapping(game.api), new GuiDialogMacroEditor(game.api), new HudDebugScreen(game.api), new HudElementCoordinates(game.api), new HudElementBlockAndEntityInfo(game.api), new GuiDialogTickProfiler(game.api), new HudDisconnected(game.api), new HudNotMinecraft(game.api), new GuiDialogTransformEditor(game.api), new GuiDialogSelboxEditor(game.api), new GuiDialogToolMode(game.api), new GuiDialogDead(game.api), new GuiDialogFirstlaunchInfo(game.api), new HudMouseTools(game.api), new HudDropItem(game.api));
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.GuiDialog;
	}

	internal void OnEscapePressed()
	{
		bool allClosed = true;
		for (int i = 0; i < game.OpenedGuis.Count; i++)
		{
			bool didClose = game.OpenedGuis[i].OnEscapePressed();
			allClosed = allClosed && didClose;
			if (didClose)
			{
				i--;
			}
		}
	}

	internal void OnGuiClosed(GuiDialog dialog)
	{
		game.OpenedGuis.Remove(dialog);
		if (dialog.UnregisterOnClose)
		{
			game.LoadedGuis.Remove(dialog);
		}
		bool anyDialogOpened = game.DialogsOpened > 0;
		if (game.player == null)
		{
			return;
		}
		ClientPlayerInventoryManager plrInv = game.player.inventoryMgr;
		if (plrInv.currentHoveredSlot != null)
		{
			InventoryBase inventory = plrInv.currentHoveredSlot.Inventory;
			if ((inventory != null && !inventory.HasOpened(game.player)) || !anyDialogOpened)
			{
				plrInv.currentHoveredSlot = null;
			}
		}
		if (game.OpenedGuis.FirstOrDefault((GuiDialog dlg) => dlg.Focused) == null)
		{
			GuiDialog fdlg = game.OpenedGuis.FirstOrDefault((GuiDialog dlg) => dlg.Focusable);
			RequestFocus(fdlg);
		}
	}

	internal void OnGuiOpened(GuiDialog dialog)
	{
		if (game.OpenedGuis.Contains(dialog))
		{
			game.OpenedGuis.Remove(dialog);
		}
		int index = game.OpenedGuis.FindIndex((GuiDialog d) => dialog.DrawOrder >= d.DrawOrder);
		if (index >= 0)
		{
			game.OpenedGuis.Insert(index, dialog);
		}
		else
		{
			game.OpenedGuis.Add(dialog);
		}
	}

	internal void RequestFocus(GuiDialog dialog)
	{
		if (!game.LoadedGuis.Contains(dialog))
		{
			game.Logger.Error("The dialog {0} requested focus, but was not added yet. Missing call to api.Gui.RegisterDialog()", dialog.DebugName);
		}
		else
		{
			if (ignoreFocusEvents || !dialog.IsOpened())
			{
				return;
			}
			Move(game.LoadedGuis, dialog, game.LoadedGuis.FindIndex((GuiDialog d) => d.InputOrder == dialog.InputOrder && d.DrawOrder == dialog.DrawOrder));
			Move(game.OpenedGuis, dialog, game.OpenedGuis.FindIndex((GuiDialog d) => d.DrawOrder == dialog.DrawOrder));
			ignoreFocusEvents = true;
			foreach (GuiDialog item in game.LoadedGuis.Where((GuiDialog d) => d != dialog).ToList())
			{
				item.UnFocus();
			}
			dialog.Focus();
			ignoreFocusEvents = false;
		}
	}

	private void Move<T>(List<T> list, T element, int to)
	{
		int from = list.FindIndex((T e) => e.Equals(element));
		if (from == -1)
		{
			return;
		}
		if (from > to)
		{
			for (int i = from; i > to; i--)
			{
				list[i] = list[i - 1];
			}
		}
		else if (from < to)
		{
			for (int j = from; j < to; j++)
			{
				list[j] = list[j + 1];
			}
		}
		list[to] = element;
	}

	public override void OnBlockTexturesLoaded()
	{
		foreach (GuiDialog item in game.LoadedGuis.ToList())
		{
			item.OnBlockTexturesLoaded();
		}
	}

	internal override void OnLevelFinalize()
	{
		foreach (GuiDialog item in game.LoadedGuis.ToList())
		{
			item.OnLevelFinalize();
		}
	}

	public override void OnOwnPlayerDataReceived()
	{
		foreach (GuiDialog item in game.LoadedGuis.ToList())
		{
			item.OnOwnPlayerDataReceived();
		}
	}

	public void OnBeforeRenderFrame3D(float deltaTime)
	{
		foreach (GuiDialog dialog in Enumerable.Reverse(game.OpenedGuis))
		{
			if (dialog.ShouldReceiveRenderEvents())
			{
				dialog.OnBeforeRenderFrame3D(deltaTime);
			}
		}
	}

	public void OnRenderFrameGUI(float deltaTime)
	{
		game.GlPushMatrix();
		string focusedMouseCursor = null;
		foreach (GuiDialog dialog in Enumerable.Reverse(game.OpenedGuis))
		{
			if (dialog.ShouldReceiveRenderEvents())
			{
				dialog.OnRenderGUI(deltaTime);
				game.Platform.CheckGlError(dialog.DebugName);
				game.GlTranslate(0.0, 0.0, dialog.ZSize);
				if (dialog.MouseOverCursor != null)
				{
					focusedMouseCursor = dialog.MouseOverCursor;
				}
				ScreenManager.FrameProfiler.Mark("rendGui" + dialog.DebugName);
			}
		}
		game.Platform.UseMouseCursor((focusedMouseCursor != null) ? focusedMouseCursor : "normal");
		game.GlPopMatrix();
		ScreenManager.FrameProfiler.Mark("rendGuiDone");
	}

	public void OnFinalizeFrame(float dt)
	{
		foreach (GuiDialog dialog in game.LoadedGuis.ToList())
		{
			dialog.OnFinalizeFrame(dt);
			ScreenManager.FrameProfiler.Mark("gdm-finFr-" + dialog.DebugName);
		}
	}

	public override void OnKeyDown(KeyEvent args)
	{
		int eKey = args.KeyCode;
		List<GuiDialog> dialogs = game.OpenedGuis.ToList();
		foreach (GuiDialog dialog2 in dialogs)
		{
			if (dialog2.CaptureAllInputs())
			{
				dialog2.OnKeyDown(args);
				if (args.Handled)
				{
					return;
				}
			}
		}
		if (eKey == 50 && game.DialogsOpened > 0)
		{
			OnEscapePressed();
			args.Handled = true;
			return;
		}
		foreach (GuiDialog dialog in dialogs)
		{
			if (dialog.ShouldReceiveKeyboardEvents())
			{
				dialog.OnKeyDown(args);
				if (args.Handled)
				{
					break;
				}
			}
		}
	}

	public override void OnKeyUp(KeyEvent args)
	{
		foreach (GuiDialog dialog in game.LoadedGuis.ToList())
		{
			if (dialog.ShouldReceiveKeyboardEvents())
			{
				dialog.OnKeyUp(args);
				if (args.Handled)
				{
					break;
				}
			}
		}
	}

	public override void OnKeyPress(KeyEvent args)
	{
		foreach (GuiDialog dialog in game.LoadedGuis.ToList())
		{
			if (dialog.ShouldReceiveKeyboardEvents())
			{
				dialog.OnKeyPress(args);
				if (args.Handled)
				{
					break;
				}
			}
		}
	}

	public override void OnMouseDown(MouseEvent args)
	{
		foreach (GuiDialog dialog in game.LoadedGuis.ToList())
		{
			if (!dialog.ShouldReceiveMouseEvents())
			{
				continue;
			}
			dialog.OnMouseDown(args);
			if (args.Handled)
			{
				if (DEBUG_PRINT_INTERACTIONS)
				{
					game.Logger.Debug("[GuiManager] OnMouseDown handled by {0}", dialog.GetType().Name);
				}
				RequestFocus(dialog);
				break;
			}
		}
	}

	public override void OnMouseUp(MouseEvent args)
	{
		foreach (GuiDialog dialog in game.LoadedGuis.ToList())
		{
			if (!dialog.ShouldReceiveMouseEvents())
			{
				continue;
			}
			dialog.OnMouseUp(args);
			if (args.Handled)
			{
				if (DEBUG_PRINT_INTERACTIONS)
				{
					game.Logger.Debug("[GuiManager] OnMouseUp handled by {0}", dialog.GetType().Name);
				}
				break;
			}
		}
	}

	public override void OnMouseMove(MouseEvent args)
	{
		didHoverSlotEventTrigger = false;
		foreach (GuiDialog dialog in game.LoadedGuis.ToList())
		{
			if (dialog.ShouldReceiveMouseEvents())
			{
				dialog.OnMouseMove(args);
				if (args.Handled)
				{
					OnMouseMoveOver(dialog);
					return;
				}
			}
		}
		OnMouseMoveOver(null);
	}

	private void OnMouseMoveOver(GuiDialog nowMouseOverDialog)
	{
		if ((nowMouseOverDialog != prevMousedOverDialog || nowMouseOverDialog == null) && !didHoverSlotEventTrigger && prevHoverSlot != null)
		{
			game.api.Input.TriggerOnMouseLeaveSlot(prevHoverSlot);
		}
		prevMousedOverDialog = nowMouseOverDialog;
	}

	public override bool OnMouseEnterSlot(ItemSlot slot)
	{
		prevHoverSlot = slot;
		didHoverSlotEventTrigger = true;
		return false;
	}

	public override bool OnMouseLeaveSlot(ItemSlot itemSlot)
	{
		didHoverSlotEventTrigger = true;
		foreach (GuiDialog dialog in game.LoadedGuis)
		{
			if (dialog.ShouldReceiveMouseEvents() && dialog.OnMouseLeaveSlot(itemSlot))
			{
				return true;
			}
		}
		return false;
	}

	public override void OnMouseWheel(MouseWheelEventArgs args)
	{
		foreach (GuiDialog dialog3 in game.OpenedGuis)
		{
			if (dialog3.CaptureAllInputs())
			{
				dialog3.OnMouseWheel(args);
				if (args.IsHandled)
				{
					return;
				}
			}
		}
		foreach (GuiDialog dialog2 in game.LoadedGuis)
		{
			if (!dialog2.IsOpened())
			{
				continue;
			}
			bool inside = false;
			foreach (GuiComposer composer in dialog2.Composers.Values)
			{
				inside |= composer.Bounds.PointInside(game.MouseCurrentX, game.MouseCurrentY);
			}
			if (inside && dialog2.ShouldReceiveMouseEvents())
			{
				dialog2.OnMouseWheel(args);
				if (args.IsHandled)
				{
					return;
				}
			}
		}
		foreach (GuiDialog dialog in game.LoadedGuis)
		{
			if (dialog.ShouldReceiveMouseEvents())
			{
				dialog.OnMouseWheel(args);
				if (args.IsHandled)
				{
					break;
				}
			}
		}
	}

	public override bool CaptureAllInputs()
	{
		foreach (GuiDialog openedGui in game.OpenedGuis)
		{
			if (openedGui.CaptureAllInputs())
			{
				return true;
			}
		}
		return false;
	}

	public override bool CaptureRawMouse()
	{
		foreach (GuiDialog openedGui in game.OpenedGuis)
		{
			if (openedGui.CaptureRawMouse())
			{
				return true;
			}
		}
		return false;
	}

	public void SendPacketClient(Packet_Client packetClient)
	{
		game.SendPacketClient(packetClient);
	}

	public override void Dispose(ClientMain game)
	{
		inventoryItemRenderer?.Dispose();
		foreach (GuiDialog loadedGui in game.LoadedGuis)
		{
			loadedGui?.Dispose();
		}
	}
}
