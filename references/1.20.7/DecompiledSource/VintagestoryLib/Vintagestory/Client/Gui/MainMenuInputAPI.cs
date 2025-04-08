using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.Gui;

public class MainMenuInputAPI : IInputAPI
{
	private ScreenManager screenManager;

	public bool[] KeyboardKeyState => ScreenManager.KeyboardKeyState;

	public bool[] KeyboardKeyStateRaw
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public MouseButtonState MouseButton
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public MouseButtonState InWorldMouseButton
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public bool MouseWorldInteractAnyway { get; set; }

	public bool MouseGrabbed => false;

	public OrderedDictionary<string, HotKey> HotKeys => ScreenManager.hotkeyManager.HotKeys;

	public int MouseX => screenManager.GetMouseCurrentX();

	public int MouseY => screenManager.GetMouseCurrentY();

	public float MouseYaw
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public float MousePitch
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public string ClipboardText
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public event MouseEventDelegate OnMouseDown;

	public event MouseEventDelegate OnMouseUp;

	public event MouseEventDelegate OnMouseMove;

	public event KeyEventDelegate OnKeyDown;

	public event KeyEventDelegate OnKeyUp;

	public event OnEntityAction InWorldAction;

	public MainMenuInputAPI(ScreenManager screenManager)
	{
		this.screenManager = screenManager;
	}

	public HotKey GetHotKeyByCode(string toggleKeyCombinationCode)
	{
		return ScreenManager.hotkeyManager.GetHotKeyByCode(toggleKeyCombinationCode);
	}

	public void RegisterHotKey(string hotkeyCode, string name, GlKeys key, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false)
	{
		throw new NotImplementedException();
	}

	public void RegisterHotKeyFirst(string hotkeyCode, string name, GlKeys key, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false)
	{
		throw new NotImplementedException();
	}

	public void SetHotKeyHandler(string hotkeyCode, ActionConsumable<KeyCombination> handler)
	{
		throw new NotImplementedException();
	}

	public void TriggerOnMouseClickSlot(ItemSlot itemSlot)
	{
		throw new NotImplementedException();
	}

	public void TriggerOnMouseEnterSlot(ItemSlot slot)
	{
		throw new NotImplementedException();
	}

	public void TriggerOnMouseLeaveSlot(ItemSlot itemSlot)
	{
		throw new NotImplementedException();
	}

	public bool IsHotKeyPressed(string hotKeyCode)
	{
		throw new NotImplementedException();
	}

	public bool IsHotKeyPressed(HotKey hotKey)
	{
		throw new NotImplementedException();
	}

	public void TriggerMouseDown(MouseEvent ev)
	{
		if (this.OnMouseDown == null)
		{
			return;
		}
		Delegate[] invocationList = this.OnMouseDown.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((MouseEventDelegate)invocationList[i])(ev);
			if (ev.Handled)
			{
				break;
			}
		}
	}

	public void TriggerMouseUp(MouseEvent ev)
	{
		if (this.OnMouseUp == null)
		{
			return;
		}
		Delegate[] invocationList = this.OnMouseUp.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((MouseEventDelegate)invocationList[i])(ev);
			if (ev.Handled)
			{
				break;
			}
		}
	}

	public void TriggerMouseMove(MouseEvent ev)
	{
		if (this.OnMouseMove == null)
		{
			return;
		}
		Delegate[] invocationList = this.OnMouseMove.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((MouseEventDelegate)invocationList[i])(ev);
			if (ev.Handled)
			{
				break;
			}
		}
	}

	public void TriggerKeyUp(KeyEvent ev)
	{
		if (this.OnKeyUp == null)
		{
			return;
		}
		Delegate[] invocationList = this.OnKeyUp.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((KeyEventDelegate)invocationList[i])(ev);
			if (ev.Handled)
			{
				break;
			}
		}
	}

	public void TriggerKeyDown(KeyEvent ev)
	{
		if (this.OnKeyDown == null)
		{
			return;
		}
		Delegate[] invocationList = this.OnKeyDown.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((KeyEventDelegate)invocationList[i])(ev);
			if (ev.Handled)
			{
				break;
			}
		}
	}

	public void AddHotkeyListener(OnHotKeyDelegate handler)
	{
		throw new NotImplementedException();
	}
}
