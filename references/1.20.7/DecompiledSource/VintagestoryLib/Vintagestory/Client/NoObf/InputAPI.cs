using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.NoObf;

public class InputAPI : IInputAPI
{
	private ClientMain game;

	public bool[] KeyboardKeyState => game.KeyboardState;

	public int MouseX => game.MouseCurrentX;

	public int MouseY => game.MouseCurrentY;

	public bool MouseWorldInteractAnyway
	{
		get
		{
			return game.mouseWorldInteractAnyway;
		}
		set
		{
			game.mouseWorldInteractAnyway = value;
		}
	}

	public OrderedDictionary<string, HotKey> HotKeys => ScreenManager.hotkeyManager.HotKeys;

	[Obsolete("This is the raw state of mouse button presses. It by-passes the hotkeys configuration system. In almost all situations InWorldMouseButton should be used instead")]
	public MouseButtonState MouseButton => game.MouseStateRaw;

	public MouseButtonState InWorldMouseButton => game.InWorldMouseState;

	public bool[] KeyboardKeyStateRaw => game.KeyboardStateRaw;

	public bool MouseGrabbed => game.MouseGrabbed;

	public float MouseYaw
	{
		get
		{
			return game.mouseYaw;
		}
		set
		{
			game.mouseYaw = value;
		}
	}

	public float MousePitch
	{
		get
		{
			return game.mousePitch;
		}
		set
		{
			game.mousePitch = value;
		}
	}

	public string ClipboardText
	{
		get
		{
			return game.Platform.XPlatInterface.GetClipboardText();
		}
		set
		{
			game.Platform.XPlatInterface.SetClipboardText(value);
		}
	}

	public event OnEntityAction InWorldAction;

	public InputAPI(ClientMain game)
	{
		this.game = game;
	}

	public void TriggerInWorldAction(EnumEntityAction action, bool on, ref EnumHandling handling)
	{
		if (this.InWorldAction == null)
		{
			return;
		}
		Delegate[] invocationList = this.InWorldAction.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			((OnEntityAction)invocationList[i])(action, on, ref handling);
			if (handling == EnumHandling.PreventSubsequent)
			{
				break;
			}
		}
	}

	public void TriggerOnMouseEnterSlot(ItemSlot slot)
	{
		game.eventManager?.TriggerOnMouseEnterSlot(game, slot);
	}

	public void TriggerOnMouseLeaveSlot(ItemSlot itemSlot)
	{
		game.eventManager?.TriggerOnMouseLeaveSlot(game, itemSlot);
	}

	public void TriggerOnMouseClickSlot(ItemSlot itemSlot)
	{
		foreach (GuiDialog loadedGui in game.LoadedGuis)
		{
			if (loadedGui.OnMouseClickSlot(itemSlot))
			{
				return;
			}
		}
		for (int i = 0; i < game.clientSystems.Length && !game.clientSystems[i].OnMouseClickSlot(itemSlot); i++)
		{
		}
	}

	public void RegisterHotKey(string hotkeyCode, string name, GlKeys key, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false)
	{
		ScreenManager.hotkeyManager.RegisterHotKey(hotkeyCode, name, key, type, altPressed, ctrlPressed, shiftPressed);
	}

	public void RegisterHotKeyFirst(string hotkeyCode, string name, GlKeys key, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false)
	{
		ScreenManager.hotkeyManager.RegisterHotKey(hotkeyCode, name, key, type, altPressed, ctrlPressed, shiftPressed, insertFirst: true);
	}

	public void SetHotKeyHandler(string hotkeyCode, ActionConsumable<KeyCombination> handler)
	{
		ScreenManager.hotkeyManager.SetHotKeyHandler(hotkeyCode, handler);
	}

	public HotKey GetHotKeyByCode(string toggleKeyCombinationCode)
	{
		return ScreenManager.hotkeyManager.GetHotKeyByCode(toggleKeyCombinationCode);
	}

	public void AddHotkeyListener(OnHotKeyDelegate handler)
	{
		ScreenManager.hotkeyManager.AddHotkeyListener(handler);
	}

	public bool IsHotKeyPressed(string hotKeyCode)
	{
		return IsHotKeyPressed(game.api.Input.GetHotKeyByCode(hotKeyCode));
	}

	public bool IsHotKeyPressed(HotKey hotKey)
	{
		bool num = KeyboardKeyState[hotKey.CurrentMapping.KeyCode];
		bool sec = !hotKey.CurrentMapping.SecondKeyCode.HasValue || KeyboardKeyState[hotKey.CurrentMapping.SecondKeyCode.Value];
		bool alt = !hotKey.CurrentMapping.Alt || KeyboardKeyState[5];
		bool ctrl = !hotKey.CurrentMapping.Ctrl || KeyboardKeyState[3];
		bool shift = !hotKey.CurrentMapping.Shift || KeyboardKeyState[1];
		return num && sec && alt && ctrl && shift;
	}
}
