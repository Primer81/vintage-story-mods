using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class MacroManager : IMacroManager
{
	private ClientMain game;

	public SortedDictionary<int, IMacroBase> MacrosByIndex { get; set; } = new SortedDictionary<int, IMacroBase>();


	public MacroManager(ClientMain game)
	{
		this.game = game;
		LoadMacros();
	}

	public void LoadMacros()
	{
		SortedDictionary<string, Macro> macrosByFilename = new SortedDictionary<string, Macro>();
		foreach (string file in Directory.EnumerateFiles(GamePaths.Macros, "*.json"))
		{
			string contents = File.ReadAllText(file);
			try
			{
				Macro macro2 = JsonConvert.DeserializeObject<Macro>(contents);
				macrosByFilename.Add(file, macro2);
			}
			catch (Exception e)
			{
				ScreenManager.Platform.Logger.Warning("Failed deserializing macro " + file + ": " + e.Message);
			}
		}
		foreach (Macro macro in macrosByFilename.Values)
		{
			MacrosByIndex[macro.Index] = macro;
			SetupHotKey(macro.Index, macro, game);
		}
	}

	private bool SetupHotKey(int macroIndex, IMacroBase macro, ClientMain game)
	{
		if (macro.KeyCombination == null || macro.KeyCombination.KeyCode < 0)
		{
			return false;
		}
		HotKey hotkey = ScreenManager.hotkeyManager.GetHotkeyByKeyCombination(macro.KeyCombination);
		string hotkeyCode = "macro-" + macro.Code;
		if (hotkey != null && hotkey.Code != hotkeyCode)
		{
			ScreenManager.Platform.Logger.Warning("Can't register hotkey {0} for macro {1} because it is aready in use by hotkey {2}", macro.KeyCombination, macro.Code, hotkey.Code);
			return false;
		}
		ScreenManager.hotkeyManager.RegisterHotKey(hotkeyCode, "Macro: " + macro.Name, macro.KeyCombination, HotkeyType.DevTool);
		ScreenManager.hotkeyManager.SetHotKeyHandler(hotkeyCode, delegate
		{
			RunMacro(macroIndex, game);
			return true;
		});
		return true;
	}

	public void DeleteMacro(int macroIndex)
	{
		MacrosByIndex.TryGetValue(macroIndex, out var macro);
		if (macro != null)
		{
			File.Delete(Path.Combine(GamePaths.Macros, macroIndex + "-" + macro.Code + ".json"));
			MacrosByIndex.Remove(macroIndex);
			string hotkeyCode = "macro-" + macro.Code;
			ScreenManager.hotkeyManager.RemoveHotKey(hotkeyCode);
		}
	}

	public void SetMacro(int macroIndex, IMacroBase macro)
	{
		MacrosByIndex[macroIndex] = macro;
		SaveMacro(macroIndex);
		SetupHotKey(macroIndex, macro, game);
	}

	public virtual bool SaveMacro(int macroIndex)
	{
		MacrosByIndex.TryGetValue(macroIndex, out var macro);
		if (macro == null)
		{
			return false;
		}
		string filename = Path.Combine(GamePaths.Macros, macroIndex + "-" + macro.Code + ".json");
		try
		{
			using TextWriter textWriter = new StreamWriter(filename);
			textWriter.Write(JsonConvert.SerializeObject(macro, Formatting.Indented));
			textWriter.Close();
		}
		catch (IOException)
		{
			return false;
		}
		SetupHotKey(macroIndex, macro, game);
		return true;
	}

	public bool RunMacro(int macroIndex, IClientWorldAccessor world)
	{
		if (!MacrosByIndex.ContainsKey(macroIndex))
		{
			return false;
		}
		string[] commands = MacrosByIndex[macroIndex].Commands;
		for (int i = 0; i < commands.Length; i++)
		{
			(world as ClientMain).eventManager?.TriggerNewClientChatLine(GlobalConstants.CurrentChatGroup, commands[i], EnumChatType.Macro, null);
		}
		return true;
	}
}
