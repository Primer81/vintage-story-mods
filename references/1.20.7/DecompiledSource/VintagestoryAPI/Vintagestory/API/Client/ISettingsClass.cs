namespace Vintagestory.API.Client;

/// <summary>
/// Setting interface.
/// </summary>
/// <typeparam name="T">The type of the given setting.</typeparam>
public interface ISettingsClass<T>
{
	/// <summary>
	/// Gets and sets the setting with the provided key.
	/// </summary>
	/// <param name="key">The key to the setting.</param>
	/// <returns>The current value of the given key.</returns>
	T this[string key] { get; set; }

	/// <summary>
	/// Gets the setting with the provided key
	/// </summary>
	/// <param name="key"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	T Get(string key, T defaultValue = default(T));

	/// <summary>
	/// Sets the setting with key to the provided value: if shouldTriggerWatchers is false, the watchers will not be triggered
	/// </summary>
	/// <param name="key"></param>
	/// <param name="value"></param>
	/// <param name="shouldTriggerWatchers"></param>
	void Set(string key, T value, bool shouldTriggerWatchers);

	/// <summary>
	/// Does this setting exist?
	/// </summary>
	/// <param name="key">The key to check on a setting.</param>
	/// <returns>Whether the setting exists or not.</returns>
	bool Exists(string key);

	/// <summary>
	/// Setting watcher for changes in values for a given setting.
	/// </summary>
	/// <param name="key">Key to the setting</param>
	/// <param name="OnValueChanged">the OnValueChanged event fired.</param>
	void AddWatcher(string key, OnSettingsChanged<T> OnValueChanged);

	/// <summary>
	/// Removes a previously assigned watcher
	/// </summary>
	/// <param name="key"></param>
	/// <param name="handler"></param>
	/// <returns>True if successfully removed</returns>
	bool RemoveWatcher(string key, OnSettingsChanged<T> handler);
}
