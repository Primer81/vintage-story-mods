using System.Collections.Generic;

namespace Vintagestory.API.Client;

/// <summary>
/// Setting interface for multiple settings.
/// </summary>
public interface ISettings
{
	/// <summary>
	/// Setting collection for boolean values.
	/// </summary>
	ISettingsClass<bool> Bool { get; }

	/// <summary>
	/// Setting collection for integer values.
	/// </summary>
	ISettingsClass<int> Int { get; }

	/// <summary>
	/// Setting collection for float values.
	/// </summary>
	ISettingsClass<float> Float { get; }

	/// <summary>
	/// Setting collection for string values.
	/// </summary>
	ISettingsClass<string> String { get; }

	/// <summary>
	/// Setting collection for string list values.
	/// </summary>
	ISettingsClass<List<string>> Strings { get; }

	/// <summary>
	/// Setting watcher for changes in values for a given setting.
	/// </summary>
	/// <typeparam name="T">The type of the value that was changed.</typeparam>
	/// <param name="key">Key to the setting</param>
	/// <param name="OnValueChanged">the OnValueChanged event fired.</param>
	void AddWatcher<T>(string key, OnSettingsChanged<T> OnValueChanged);
}
