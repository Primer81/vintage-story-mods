using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public abstract class SettingsBaseNoObf
{
	protected SettingsClass<string> StringSettings = new SettingsClass<string>();

	protected SettingsClass<List<string>> StringListSettings = new SettingsClass<List<string>>();

	protected SettingsClass<int> IntSettings = new SettingsClass<int>();

	protected SettingsClass<float> FloatSettings = new SettingsClass<float>();

	protected SettingsClass<bool> BoolSettings = new SettingsClass<bool>();

	[JsonProperty]
	protected Dictionary<string, KeyCombination> keyMapping;

	[JsonProperty]
	protected Dictionary<string, Vec2i> dialogPositions = new Dictionary<string, Vec2i>();

	public bool OtherDirty;

	public bool ShouldTriggerWatchers
	{
		set
		{
			StringSettings.ShouldTriggerWatchers = (StringListSettings.ShouldTriggerWatchers = (IntSettings.ShouldTriggerWatchers = (FloatSettings.ShouldTriggerWatchers = (BoolSettings.ShouldTriggerWatchers = value))));
		}
	}

	public ISettingsClass<bool> Bool => BoolSettings;

	public ISettingsClass<int> Int => IntSettings;

	public ISettingsClass<float> Float => FloatSettings;

	public ISettingsClass<string> String => StringSettings;

	public ISettingsClass<List<string>> Strings => StringListSettings;

	[JsonProperty]
	protected Dictionary<string, string> stringSettings
	{
		get
		{
			return StringSettings.values;
		}
		set
		{
			StringSettings.values = value;
		}
	}

	[JsonProperty]
	protected Dictionary<string, int> intSettings
	{
		get
		{
			return IntSettings.values;
		}
		set
		{
			IntSettings.values = value;
		}
	}

	[JsonProperty]
	protected Dictionary<string, bool> boolSettings
	{
		get
		{
			return BoolSettings.values;
		}
		set
		{
			BoolSettings.values = value;
		}
	}

	[JsonProperty]
	protected Dictionary<string, float> floatSettings
	{
		get
		{
			return FloatSettings.values;
		}
		set
		{
			FloatSettings.values = value;
		}
	}

	[JsonProperty]
	protected Dictionary<string, List<string>> stringListSettings
	{
		get
		{
			return StringListSettings.values;
		}
		set
		{
			StringListSettings.values = value;
		}
	}
}
