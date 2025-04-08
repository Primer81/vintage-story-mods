using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client;

public class HotkeyCapturer
{
	public bool WasCancelled;

	public KeyCombination CapturedKeyComb;

	internal KeyCombination CapturingKeyComb;

	private int? controlKeyCode;

	private int? altKeyCode;

	private int? shiftKeyCode;

	private int? otherKeyCode;

	private int? secondotherKeyCode;

	private long lastKeyUpMs;

	public bool BeginCapture()
	{
		WasCancelled = false;
		if (CapturingKeyComb != null)
		{
			return false;
		}
		CapturedKeyComb = null;
		CapturingKeyComb = new KeyCombination();
		controlKeyCode = null;
		altKeyCode = null;
		shiftKeyCode = null;
		otherKeyCode = null;
		secondotherKeyCode = null;
		ScreenManager.hotkeyManager.ShouldTriggerHotkeys = false;
		return true;
	}

	public bool IsCapturing()
	{
		return CapturingKeyComb != null;
	}

	public void EndCapture(bool wasCancelled = false)
	{
		controlKeyCode = null;
		altKeyCode = null;
		shiftKeyCode = null;
		otherKeyCode = null;
		secondotherKeyCode = null;
		CapturingKeyComb = null;
		WasCancelled = wasCancelled;
		ScreenManager.hotkeyManager.ShouldTriggerHotkeys = true;
	}

	public bool OnKeyDown(KeyEvent eventArgs)
	{
		if (CapturingKeyComb == null)
		{
			return false;
		}
		eventArgs.Handled = true;
		return HandleKeyCode(eventArgs.KeyCode);
	}

	public bool OnKeyUp(KeyEvent eventArgs, Action OnCaptureEnded)
	{
		if (CapturingKeyComb == null || (!otherKeyCode.HasValue && !IsShiftCtrlOrAlt(CapturingKeyComb.KeyCode)))
		{
			return false;
		}
		eventArgs.Handled = true;
		return HandleCaptureEnded(OnCaptureEnded);
	}

	public bool OnMouseDown(MouseEvent eventArgs)
	{
		if (CapturingKeyComb == null)
		{
			return false;
		}
		eventArgs.Handled = true;
		return HandleKeyCode((int)(eventArgs.Button + 240));
	}

	public bool OnMouseUp(MouseEvent eventArgs, Action OnCaptureEnded)
	{
		if (CapturingKeyComb == null || !otherKeyCode.HasValue)
		{
			return false;
		}
		eventArgs.Handled = true;
		return HandleCaptureEnded(OnCaptureEnded);
	}

	private bool IsShiftCtrlOrAlt(int keyCode)
	{
		switch (keyCode)
		{
		case 3:
		case 4:
			return true;
		case 5:
		case 6:
			return true;
		case 1:
		case 2:
			return true;
		default:
			return false;
		}
	}

	private bool HandleKeyCode(int keyCode)
	{
		switch (keyCode)
		{
		case 50:
			EndCapture();
			WasCancelled = true;
			return true;
		case 3:
		case 4:
			controlKeyCode = keyCode;
			InterpretKeyPresses();
			return true;
		case 5:
		case 6:
			altKeyCode = keyCode;
			InterpretKeyPresses();
			return true;
		case 1:
		case 2:
			shiftKeyCode = keyCode;
			InterpretKeyPresses();
			return true;
		default:
			if (!otherKeyCode.HasValue)
			{
				otherKeyCode = keyCode;
			}
			else
			{
				if (keyCode != otherKeyCode)
				{
					return true;
				}
				secondotherKeyCode = keyCode;
			}
			InterpretKeyPresses();
			return true;
		}
	}

	private bool HandleCaptureEnded(Action OnCaptureEnded)
	{
		InterpretKeyPresses();
		if (secondotherKeyCode.HasValue)
		{
			CapturedKeyComb = CapturingKeyComb.Clone();
			EndCapture();
			OnCaptureEnded();
			lastKeyUpMs = ScreenManager.Platform.EllapsedMs;
			return true;
		}
		lastKeyUpMs = ScreenManager.Platform.EllapsedMs;
		ScreenManager.EnqueueMainThreadTask(delegate
		{
			TryEndCapture(OnCaptureEnded);
		});
		return true;
	}

	private void TryEndCapture(Action OnCaptureEnded)
	{
		if (!IsCapturing())
		{
			return;
		}
		if (ScreenManager.Platform.EllapsedMs - lastKeyUpMs > 150)
		{
			CapturedKeyComb = CapturingKeyComb.Clone();
			EndCapture();
			OnCaptureEnded();
			lastKeyUpMs = ScreenManager.Platform.EllapsedMs;
		}
		else
		{
			ScreenManager.EnqueueMainThreadTask(delegate
			{
				TryEndCapture(OnCaptureEnded);
			});
		}
	}

	private void InterpretKeyPresses()
	{
		if (!otherKeyCode.HasValue)
		{
			if (shiftKeyCode.HasValue)
			{
				CapturingKeyComb.Ctrl = controlKeyCode.HasValue;
				CapturingKeyComb.Alt = altKeyCode.HasValue;
				CapturingKeyComb.Shift = false;
				CapturingKeyComb.KeyCode = shiftKeyCode.Value;
			}
			else if (altKeyCode.HasValue)
			{
				CapturingKeyComb.Ctrl = controlKeyCode.HasValue;
				CapturingKeyComb.Alt = false;
				CapturingKeyComb.Shift = false;
				CapturingKeyComb.KeyCode = altKeyCode.Value;
			}
			else if (controlKeyCode.HasValue)
			{
				CapturingKeyComb.Ctrl = false;
				CapturingKeyComb.Alt = false;
				CapturingKeyComb.Shift = false;
				CapturingKeyComb.KeyCode = controlKeyCode.Value;
			}
		}
		else
		{
			CapturingKeyComb.KeyCode = otherKeyCode.Value;
			CapturingKeyComb.SecondKeyCode = secondotherKeyCode;
			CapturingKeyComb.Ctrl = controlKeyCode.HasValue;
			CapturingKeyComb.Alt = altKeyCode.HasValue;
			CapturingKeyComb.Shift = shiftKeyCode.HasValue;
		}
	}
}
