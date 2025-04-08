using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace Vintagestory.Common;

public class SettingsClass<T> : ISettingsClass<T>
{
	public Dictionary<string, T> values;

	public T defaultValue;

	public List<SettingsChangedWatcher<T>> Watchers = new List<SettingsChangedWatcher<T>>();

	public bool Dirty;

	public bool ShouldTriggerWatchers = true;

	public T this[string key]
	{
		get
		{
			if (!values.TryGetValue(key, out var val))
			{
				return defaultValue;
			}
			return val;
		}
		set
		{
			Set(key, value, ShouldTriggerWatchers);
		}
	}

	public SettingsClass()
	{
		StringComparer comparer = StringComparer.OrdinalIgnoreCase;
		values = new Dictionary<string, T>(comparer);
	}

	public bool Exists(string key)
	{
		return values.ContainsKey(key);
	}

	public T Get(string key, T defaultValue = default(T))
	{
		if (values.TryGetValue(key, out var val))
		{
			return val;
		}
		return defaultValue;
	}

	public void Set(string key, T value, bool shouldTriggerWatchers)
	{
		if (!values.ContainsKey(key) || !EqualityComparer<T>.Default.Equals(values[key], value))
		{
			values[key] = value;
			if (shouldTriggerWatchers)
			{
				TriggerWatcher(key);
			}
			Dirty = true;
		}
	}

	public void TriggerWatcher(string key)
	{
		string lowerkey = key.ToLowerInvariant();
		T value = values[key];
		foreach (SettingsChangedWatcher<T> watcher in Watchers)
		{
			if (watcher.key == lowerkey)
			{
				watcher.handler(value);
			}
		}
	}

	public void AddWatcher(string key, OnSettingsChanged<T> handler)
	{
		if (handler == null)
		{
			throw new ArgumentNullException("handler cannot be null!");
		}
		Watchers.Add(new SettingsChangedWatcher<T>
		{
			key = key.ToLowerInvariant(),
			handler = handler
		});
	}

	public bool RemoveWatcher(string key, OnSettingsChanged<T> handler)
	{
		for (int i = 0; i < Watchers.Count; i++)
		{
			SettingsChangedWatcher<T> var = Watchers[i];
			if (var.key == key.ToLowerInvariant() && var.handler == handler)
			{
				Watchers.RemoveAt(i);
				return true;
			}
		}
		return false;
	}
}
