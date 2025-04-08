using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public class HotkeyManager
{
	public OrderedDictionary<string, HotKey> HotKeys = new OrderedDictionary<string, HotKey>();

	public bool ShouldTriggerHotkeys = true;

	private event OnHotKeyDelegate listeners;

	public virtual void RegisterDefaultHotKeys()
	{
		HotKeys.Clear();
		RegisterHotKey("primarymouse", Lang.Get("Primary mouse button"), EnumMouseButton.Left, HotkeyType.MouseControls);
		RegisterHotKey("secondarymouse", Lang.Get("Second mouse button"), EnumMouseButton.Right, HotkeyType.MouseControls);
		RegisterHotKey("middlemouse", Lang.Get("Middle mouse button"), EnumMouseButton.Middle, HotkeyType.MouseControls);
		RegisterHotKey("walkforward", Lang.Get("Walk forward"), GlKeys.W, HotkeyType.MovementControls);
		RegisterHotKey("walkbackward", Lang.Get("Walk backward"), GlKeys.S, HotkeyType.MovementControls);
		RegisterHotKey("walkleft", Lang.Get("Walk left"), GlKeys.A, HotkeyType.MovementControls);
		RegisterHotKey("walkright", Lang.Get("Walk right"), GlKeys.D, HotkeyType.MovementControls);
		RegisterHotKey("sneak", Lang.Get("Sneak"), GlKeys.LShift, HotkeyType.MovementControls);
		RegisterHotKey("sprint", Lang.Get("Sprint"), GlKeys.LControl, HotkeyType.MovementControls);
		RegisterHotKey("shift", Lang.Get("Shift-click"), GlKeys.LShift, HotkeyType.MouseModifiers);
		RegisterHotKey("ctrl", Lang.Get("Ctrl-click"), GlKeys.LControl, HotkeyType.MouseModifiers);
		RegisterHotKey("jump", Lang.Get("Jump"), GlKeys.Space, HotkeyType.MovementControls);
		RegisterHotKey("sitdown", Lang.Get("Sit down"), GlKeys.G, HotkeyType.MovementControls);
		RegisterHotKey("inventorydialog", Lang.Get("Open Inventory"), GlKeys.E);
		RegisterHotKey("characterdialog", Lang.Get("Open character Inventory"), GlKeys.C);
		RegisterHotKey("dropitem", Lang.Get("Drop one item"), GlKeys.Q);
		RegisterHotKey("dropitems", Lang.Get("Drop all items"), GlKeys.Q, HotkeyType.CharacterControls, altPressed: false, ctrlPressed: true);
		RegisterHotKey("toolmodeselect", Lang.Get("Select Tool Mode"), GlKeys.F);
		RegisterHotKey("coordinateshud", Lang.Get("Show/Hide distance to spawn"), GlKeys.V, HotkeyType.HelpAndOverlays, altPressed: false, ctrlPressed: true);
		RegisterHotKey("blockinfohud", Lang.Get("Show/Hide block and entity info overlay"), GlKeys.B, HotkeyType.HelpAndOverlays, altPressed: false, ctrlPressed: true);
		RegisterHotKey("blockinteractionhelp", Lang.Get("Show/Hide block and entity interaction info overlay"), GlKeys.N, HotkeyType.HelpAndOverlays, altPressed: false, ctrlPressed: true);
		RegisterHotKey("escapemenudialog", Lang.Get("Show/Hide escape menu dialog"), GlKeys.Escape, HotkeyType.GUIOrOtherControls);
		RegisterHotKey("togglehud", Lang.Get("Hide/Show HUD"), GlKeys.F4, HotkeyType.GUIOrOtherControls);
		RegisterHotKey("cyclecamera", Lang.Get("First-, Third-person or Overhead camera"), GlKeys.F5, HotkeyType.GUIOrOtherControls);
		RegisterHotKey("zoomout", Lang.Get("3rd Person Camera: Zoom out"), GlKeys.Minus, HotkeyType.GUIOrOtherControls);
		RegisterHotKey("zoomin", Lang.Get("3rd Person Camera: Zoom in"), GlKeys.Plus, HotkeyType.GUIOrOtherControls);
		RegisterHotKey("togglemousecontrol", Lang.Get("Lock/Unlock Mouse Cursor"), GlKeys.AltLeft, HotkeyType.GUIOrOtherControls);
		RegisterHotKey("beginchat", Lang.Get("Chat: Begin Typing"), GlKeys.T, HotkeyType.GUIOrOtherControls);
		RegisterHotKey("beginclientcommand", Lang.Get("Chat: Begin Typing a client command"), GlKeys.Period, HotkeyType.GUIOrOtherControls);
		RegisterHotKey("beginservercommand", Lang.Get("Chat: Begin Typing a server command"), GlKeys.Slash, HotkeyType.GUIOrOtherControls);
		RegisterHotKey("chatdialog", Lang.Get("Chat: Show/Hide chat dialog"), GlKeys.Tab, HotkeyType.GUIOrOtherControls);
		RegisterHotKey("macroeditor", Lang.Get("Open Macro Editor"), GlKeys.M, HotkeyType.GUIOrOtherControls, altPressed: false, ctrlPressed: true);
		RegisterHotKey("togglefullscreen", Lang.Get("Toggle Fullscreen mode"), GlKeys.F11, HotkeyType.GUIOrOtherControls);
		RegisterHotKey("screenshot", Lang.Get("Take screenshot"), GlKeys.F12, HotkeyType.GUIOrOtherControls);
		RegisterHotKey("megascreenshot", Lang.Get("Take mega screenshot"), GlKeys.F12, HotkeyType.GUIOrOtherControls, altPressed: false, ctrlPressed: true);
		RegisterHotKey("fliphandslots", Lang.Get("Flip left/right hand contents"), GlKeys.X, HotkeyType.InventoryHotkeys);
		RegisterHotKey("hotbarslot1", Lang.Get("Select Hotbar Slot {0}", 1), GlKeys.Number1, HotkeyType.InventoryHotkeys);
		RegisterHotKey("hotbarslot2", Lang.Get("Select Hotbar Slot {0}", 2), GlKeys.Number2, HotkeyType.InventoryHotkeys);
		RegisterHotKey("hotbarslot3", Lang.Get("Select Hotbar Slot {0}", 3), GlKeys.Number3, HotkeyType.InventoryHotkeys);
		RegisterHotKey("hotbarslot4", Lang.Get("Select Hotbar Slot {0}", 4), GlKeys.Number4, HotkeyType.InventoryHotkeys);
		RegisterHotKey("hotbarslot5", Lang.Get("Select Hotbar Slot {0}", 5), GlKeys.Number5, HotkeyType.InventoryHotkeys);
		RegisterHotKey("hotbarslot6", Lang.Get("Select Hotbar Slot {0}", 6), GlKeys.Number6, HotkeyType.InventoryHotkeys);
		RegisterHotKey("hotbarslot7", Lang.Get("Select Hotbar Slot {0}", 7), GlKeys.Number7, HotkeyType.InventoryHotkeys);
		RegisterHotKey("hotbarslot8", Lang.Get("Select Hotbar Slot {0}", 8), GlKeys.Number8, HotkeyType.InventoryHotkeys);
		RegisterHotKey("hotbarslot9", Lang.Get("Select Hotbar Slot {0}", 9), GlKeys.Number9, HotkeyType.InventoryHotkeys);
		RegisterHotKey("hotbarslot10", Lang.Get("Select Hotbar Slot {0}", 10), GlKeys.Number0, HotkeyType.InventoryHotkeys);
		RegisterHotKey("hotbarslot11", Lang.Get("Select Backpack Slot {0}", 1), GlKeys.Number1, HotkeyType.InventoryHotkeys, altPressed: false, ctrlPressed: true);
		RegisterHotKey("hotbarslot12", Lang.Get("Select Backpack Slot {0}", 2), GlKeys.Number2, HotkeyType.InventoryHotkeys, altPressed: false, ctrlPressed: true);
		RegisterHotKey("hotbarslot13", Lang.Get("Select Backpack Slot {0}", 3), GlKeys.Number3, HotkeyType.InventoryHotkeys, altPressed: false, ctrlPressed: true);
		RegisterHotKey("hotbarslot14", Lang.Get("Select Backpack Slot {0}", 4), GlKeys.Number4, HotkeyType.InventoryHotkeys, altPressed: false, ctrlPressed: true);
		RegisterHotKey("decspeed", Lang.Get("-1 Fly/Move Speed"), GlKeys.F1, HotkeyType.CreativeOrSpectatorTool);
		RegisterHotKey("incspeed", Lang.Get("+1 Fly/Move Speed"), GlKeys.F2, HotkeyType.CreativeOrSpectatorTool);
		RegisterHotKey("decspeedfrac", Lang.Get("-0.1 Fly/Move Speed"), GlKeys.F1, HotkeyType.CreativeOrSpectatorTool, altPressed: false, ctrlPressed: false, shiftPressed: true);
		RegisterHotKey("incspeedfrac", Lang.Get("+0.1 Fly/Move Speed"), GlKeys.F2, HotkeyType.CreativeOrSpectatorTool, altPressed: false, ctrlPressed: false, shiftPressed: true);
		RegisterHotKey("cycleflymodes", Lang.Get("Cycle through 3 fly modes"), GlKeys.F3, HotkeyType.CreativeTool);
		RegisterHotKey("fly", Lang.Get("Fly Mode On/Off"), 51, 51, HotkeyType.CreativeTool);
		RegisterHotKey("rendermetablocks", Lang.Get("Show/Hide Meta Blocks"), GlKeys.F4, HotkeyType.CreativeTool, altPressed: false, ctrlPressed: true);
		RegisterHotKey("fpsgraph", Lang.Get("FPS graph"), GlKeys.F3, HotkeyType.DevTool, altPressed: true);
		RegisterHotKey("debugscreenandgraph", Lang.Get("Debug screen + FPS graph"), GlKeys.F3, HotkeyType.DevTool, altPressed: false, ctrlPressed: true);
		RegisterHotKey("reloadworld", Lang.Get("Reload world"), GlKeys.F1, HotkeyType.DevTool, altPressed: false, ctrlPressed: true);
		RegisterHotKey("reloadshaders", Lang.Get("Reload shaders"), GlKeys.F1, HotkeyType.DevTool, altPressed: true);
		RegisterHotKey("reloadtextures", Lang.Get("Reload textures"), GlKeys.F2, HotkeyType.DevTool, altPressed: true);
		RegisterHotKey("compactheap", Lang.Get("Compact large object heap"), GlKeys.F8, HotkeyType.DevTool, altPressed: true);
		RegisterHotKey("recomposeallguis", Lang.Get("Recompose all dialogs"), GlKeys.F9, HotkeyType.DevTool, altPressed: true);
		RegisterHotKey("cycledialogoutlines", Lang.Get("Cycle Dialog Outline Modes"), GlKeys.F10, HotkeyType.DevTool, altPressed: true);
		RegisterHotKey("tickprofiler", Lang.Get("Toggle Tick Profiler"), GlKeys.F10, HotkeyType.DevTool, altPressed: false, ctrlPressed: true);
		RegisterHotKey("pickblock", Lang.Get("Pick block"), EnumMouseButton.Middle, HotkeyType.CreativeTool);
		HotKeys["reloadworld"].IsGlobalHotkey = true;
		HotKeys["togglefullscreen"].IsGlobalHotkey = true;
		HotKeys["cycledialogoutlines"].IsGlobalHotkey = true;
		HotKeys["recomposeallguis"].IsGlobalHotkey = true;
		HotKeys["compactheap"].IsGlobalHotkey = true;
		HotKeys["screenshot"].IsGlobalHotkey = true;
		HotKeys["megascreenshot"].IsGlobalHotkey = true;
		HotKeys["primarymouse"].IsGlobalHotkey = true;
		HotKeys["secondarymouse"].IsGlobalHotkey = true;
	}

	internal void ResetKeyMapping()
	{
		foreach (HotKey hk in HotKeys.Values)
		{
			hk.CurrentMapping = hk.DefaultMapping.Clone();
			ClientSettings.Inst.SetKeyMapping(hk.Code, hk.CurrentMapping);
		}
	}

	internal bool TriggerGlobalHotKey(KeyEvent keyEventargs, IWorldAccessor world, IPlayer player, bool keyUp)
	{
		if (!ShouldTriggerHotkeys)
		{
			return false;
		}
		if (TriggerHotKey(keyEventargs, world, player, allowCharacterControls: false, isGlobal: true, fallBack: false, keyUp))
		{
			return true;
		}
		return TriggerHotKey(keyEventargs, world, player, allowCharacterControls: false, isGlobal: true, fallBack: true, keyUp);
	}

	public bool TriggerHotKey(KeyEvent keyEventargs, IWorldAccessor world, IPlayer player, bool allowCharacterControls, bool keyUp)
	{
		if (!ShouldTriggerHotkeys)
		{
			return false;
		}
		if (TriggerHotKey(keyEventargs, world, player, allowCharacterControls, isGlobal: false, fallBack: false, keyUp))
		{
			return true;
		}
		return TriggerHotKey(keyEventargs, world, player, allowCharacterControls, isGlobal: false, fallBack: true, keyUp);
	}

	private bool TriggerHotKey(KeyEvent keyEventargs, IWorldAccessor world, IPlayer player, bool allowCharacterControls, bool isGlobal, bool fallBack, bool keyup)
	{
		foreach (HotKey hotkey in HotKeys.ValuesOrdered)
		{
			if (hotkey.CurrentMapping.KeyCode == keyEventargs.KeyCode && (!keyup || hotkey.TriggerOnUpAlso) && (hotkey.KeyCombinationType != HotkeyType.CreativeTool || player == null || player.WorldData.CurrentGameMode == EnumGameMode.Creative) && (hotkey.KeyCombinationType != HotkeyType.CreativeOrSpectatorTool || player == null || player.WorldData.CurrentGameMode == EnumGameMode.Creative || player.WorldData.CurrentGameMode == EnumGameMode.Spectator) && (!isGlobal || hotkey.IsGlobalHotkey) && (fallBack ? hotkey.FallbackDidPress(keyEventargs, world, player, allowCharacterControls) : hotkey.DidPress(keyEventargs, world, player, allowCharacterControls)) && hotkey.Handler != null)
			{
				keyEventargs.Handled = true;
				hotkey.CurrentMapping.OnKeyUp = keyup;
				if (hotkey.Handler(hotkey.CurrentMapping))
				{
					this.listeners?.Invoke(hotkey.Code, hotkey.CurrentMapping);
					return true;
				}
			}
		}
		return false;
	}

	public bool IsHotKeyRegistered(KeyCombination keyCombMap)
	{
		return HotKeys.Values.Any((HotKey kc) => kc.CurrentMapping.ToString() == keyCombMap.ToString());
	}

	public HotKey GetHotkeyByKeyCombination(KeyCombination keyCombMap)
	{
		return HotKeys.Values.FirstOrDefault((HotKey kc) => kc.CurrentMapping.ToString() == keyCombMap.ToString());
	}

	public HotKey GetHotKeyByCode(string code)
	{
		if (code == null)
		{
			return null;
		}
		return HotKeys.TryGetValue(code);
	}

	public void RemoveHotKey(string code)
	{
		HotKeys.Remove(code);
	}

	public void RegisterHotKey(HotKey keyComb)
	{
		keyComb.SetDefaultMapping();
		HotKeys[keyComb.Code] = keyComb;
	}

	public void RegisterHotKey(string code, string name, KeyCombination keyComb, HotkeyType type = HotkeyType.CharacterControls)
	{
		RegisterHotKey(code, name, keyComb.KeyCode, keyComb.SecondKeyCode, type, keyComb.Alt, keyComb.Ctrl, keyComb.Shift);
	}

	public void RegisterHotKey(string code, string name, GlKeys key, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false, bool insertFirst = false)
	{
		RegisterHotKey(code, name, (int)key, type, altPressed, ctrlPressed, shiftPressed, insertFirst);
	}

	public void RegisterHotKey(string code, string name, EnumMouseButton button, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false, bool insertFirst = false)
	{
		RegisterHotKey(code, name, (int)(button + 240), type, altPressed, ctrlPressed, shiftPressed, insertFirst, triggerOnUpAlso: true);
	}

	public void RegisterHotKey(string code, string name, int keyCode, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false, bool insertFirst = false, bool triggerOnUpAlso = false)
	{
		HotKey hKey = new HotKey
		{
			Code = code,
			Name = name,
			KeyCombinationType = type,
			CurrentMapping = new KeyCombination
			{
				KeyCode = keyCode,
				Ctrl = ctrlPressed,
				Alt = altPressed,
				Shift = shiftPressed
			},
			TriggerOnUpAlso = triggerOnUpAlso
		};
		if (insertFirst)
		{
			HotKeys.Insert(0, code, hKey);
		}
		else
		{
			HotKeys[code] = hKey;
		}
		hKey.SetDefaultMapping();
		KeyCombination comb = null;
		if (ClientSettings.KeyMapping.TryGetValue(code, out comb))
		{
			hKey.CurrentMapping = comb;
		}
	}

	public void RegisterHotKey(string code, string name, int keyCode, int? keyCode2, HotkeyType type = HotkeyType.CharacterControls, bool altPressed = false, bool ctrlPressed = false, bool shiftPressed = false)
	{
		HotKeys[code] = new HotKey
		{
			Code = code,
			Name = name,
			KeyCombinationType = type,
			CurrentMapping = new KeyCombination
			{
				KeyCode = keyCode,
				SecondKeyCode = keyCode2,
				Ctrl = ctrlPressed,
				Alt = altPressed,
				Shift = shiftPressed
			}
		};
		HotKeys[code].SetDefaultMapping();
		if (ClientSettings.KeyMapping.TryGetValue(code, out var comb))
		{
			HotKeys[code].CurrentMapping = comb;
		}
	}

	public void SetHotKeyHandler(string code, ActionConsumable<KeyCombination> handler, bool isIngameHotkey = true)
	{
		if (HotKeys.ContainsKey(code))
		{
			HotKeys[code].Handler = handler;
			HotKeys[code].IsIngameHotkey = isIngameHotkey;
		}
	}

	public void ClearInGameHotKeyHandlers()
	{
		foreach (HotKey hotkey in HotKeys.Values)
		{
			if (hotkey.IsIngameHotkey)
			{
				hotkey.Handler = null;
			}
		}
		this.listeners = null;
	}

	public void AddHotkeyListener(OnHotKeyDelegate handler)
	{
		listeners += handler;
	}

	public bool OnMouseButton(ClientMain game, EnumMouseButton button, int modifiers, bool buttonDown)
	{
		KeyEvent args = new KeyEvent
		{
			KeyCode = (int)(button + 240)
		};
		args.CtrlPressed = (modifiers & 2) != 0;
		args.ShiftPressed = (modifiers & 1) != 0;
		args.AltPressed = (modifiers & 4) != 0;
		args.CommandPressed = (modifiers & 8) != 0;
		if (TriggerHotKey(args, game, game.player, game.AllowCharacterControl, !buttonDown))
		{
			return true;
		}
		return false;
	}
}
