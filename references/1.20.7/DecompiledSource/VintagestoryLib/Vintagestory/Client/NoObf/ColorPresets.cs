using System.Collections.Generic;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Client.NoObf;

public class ColorPresets : IColorPresets
{
	private ClientMain game;

	private ICoreClientAPI api;

	private Dictionary<string, int> Preset1;

	private Dictionary<string, int> Preset2;

	private Dictionary<string, int> Preset3;

	private Dictionary<string, int> currentPreset;

	public ColorPresets(ClientMain game, ClientCoreAPI api)
	{
		this.game = game;
		this.api = api;
	}

	public int GetColor(string key)
	{
		if (currentPreset == null)
		{
			OnUpdateSetting();
		}
		if (currentPreset != null && currentPreset.TryGetValue(key, out var result))
		{
			return result;
		}
		return (key.GetHashCode() & 0xFFFFFF) | -16777216;
	}

	public void OnUpdateSetting()
	{
		SetCurrent(api.Settings.Int["guiColorsPreset"]);
		game.eventManager?.TriggerColorPresetChanged();
	}

	private void SetCurrent(int setting)
	{
		switch (setting)
		{
		case 1:
			currentPreset = Preset1;
			break;
		case 2:
			currentPreset = Preset2;
			break;
		case 3:
			currentPreset = Preset3;
			break;
		default:
			currentPreset = Preset1;
			break;
		}
	}

	public void Initialize(IAsset configfile)
	{
		Dictionary<string, Dictionary<string, string>> config = configfile.ToObject<Dictionary<string, Dictionary<string, string>>>();
		foreach (KeyValuePair<string, Dictionary<string, string>> entry in config)
		{
			config[entry.Key.ToLowerInvariant()] = entry.Value;
		}
		InitializeFromConfig(config, ref Preset1, "preset1");
		InitializeFromConfig(config, ref Preset2, "preset2");
		InitializeFromConfig(config, ref Preset3, "preset3");
	}

	private void InitializeFromConfig(Dictionary<string, Dictionary<string, string>> config, ref Dictionary<string, int> dict, string key)
	{
		if (dict == null)
		{
			dict = new Dictionary<string, int>();
		}
		if (!config.TryGetValue(key, out var presetsettings))
		{
			return;
		}
		foreach (KeyValuePair<string, string> entry in presetsettings)
		{
			dict[entry.Key] = HexConvert(entry.Value);
		}
	}

	private int HexConvert(string arg)
	{
		if (arg.StartsWith("0x"))
		{
			arg = arg.Substring(2);
		}
		if (int.TryParse(arg, NumberStyles.HexNumber, GlobalConstants.DefaultCultureInfo, out var val))
		{
			return val;
		}
		return 0;
	}
}
