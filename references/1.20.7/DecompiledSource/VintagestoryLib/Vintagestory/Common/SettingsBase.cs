using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Vintagestory.API.Client;

namespace Vintagestory.Common;

[JsonObject(MemberSerialization.OptIn)]
public abstract class SettingsBase : SettingsBaseNoObf, ISettings
{
	protected bool isnewfile;

	public abstract string FileName { get; }

	public abstract string TempFileName { get; }

	public abstract string BkpFileName { get; }

	public bool IsDirty
	{
		get
		{
			if (!BoolSettings.Dirty && !StringSettings.Dirty && !StringListSettings.Dirty && !FloatSettings.Dirty && !IntSettings.Dirty)
			{
				return OtherDirty;
			}
			return true;
		}
	}

	public void AddWatcher<T>(string key, OnSettingsChanged<T> handler)
	{
		if (typeof(T) == typeof(bool))
		{
			BoolSettings.AddWatcher(key, handler as OnSettingsChanged<bool>);
		}
		if (typeof(T) == typeof(string))
		{
			StringSettings.AddWatcher(key, handler as OnSettingsChanged<string>);
		}
		if (typeof(T) == typeof(int))
		{
			IntSettings.AddWatcher(key, handler as OnSettingsChanged<int>);
		}
		if (typeof(T) == typeof(float))
		{
			FloatSettings.AddWatcher(key, handler as OnSettingsChanged<float>);
		}
		if (typeof(T) == typeof(List<string>))
		{
			StringListSettings.AddWatcher(key, handler as OnSettingsChanged<List<string>>);
		}
	}

	public virtual void ClearWatchers()
	{
		StringListSettings.Watchers.Clear();
		BoolSettings.Watchers.Clear();
		IntSettings.Watchers.Clear();
		FloatSettings.Watchers.Clear();
		StringSettings.Watchers.Clear();
	}

	public string GetStringSetting(string key, string defaultValue = null)
	{
		string value = defaultValue;
		base.stringSettings.TryGetValue(key.ToLowerInvariant(), out value);
		return value;
	}

	public List<string> GetStringListSetting(string key, List<string> defaultValue = null)
	{
		List<string> value = defaultValue;
		base.stringListSettings.TryGetValue(key.ToLowerInvariant(), out value);
		return value;
	}

	public int GetIntSetting(string key)
	{
		int value = 0;
		base.intSettings.TryGetValue(key.ToLowerInvariant(), out value);
		return value;
	}

	public float GetFloatSetting(string key)
	{
		float value = 0f;
		base.floatSettings.TryGetValue(key.ToLowerInvariant(), out value);
		return value;
	}

	public bool GetBoolSetting(string key)
	{
		bool value = false;
		base.boolSettings.TryGetValue(key.ToLowerInvariant(), out value);
		return value;
	}

	public bool HasSetting(string name)
	{
		name = name.ToLowerInvariant();
		if (!base.stringSettings.ContainsKey(name) && !base.intSettings.ContainsKey(name) && !base.floatSettings.ContainsKey(name))
		{
			return base.boolSettings.ContainsKey(name);
		}
		return true;
	}

	public Type GetSettingType(string name)
	{
		name = name.ToLowerInvariant();
		if (base.stringSettings.ContainsKey(name))
		{
			return typeof(string);
		}
		if (base.intSettings.ContainsKey(name))
		{
			return typeof(int);
		}
		if (base.floatSettings.ContainsKey(name))
		{
			return typeof(float);
		}
		if (base.boolSettings.ContainsKey(name))
		{
			return typeof(bool);
		}
		return null;
	}

	internal object GetSetting(string name)
	{
		name = name.ToLowerInvariant();
		if (base.stringSettings.ContainsKey(name))
		{
			return GetStringSetting(name);
		}
		if (base.intSettings.ContainsKey(name))
		{
			return GetIntSetting(name);
		}
		if (base.floatSettings.ContainsKey(name))
		{
			return GetFloatSetting(name);
		}
		if (base.boolSettings.ContainsKey(name))
		{
			return GetBoolSetting(name);
		}
		return null;
	}

	protected SettingsBase()
	{
		StringComparer comparer = StringComparer.OrdinalIgnoreCase;
		base.stringSettings = new Dictionary<string, string>(comparer);
		base.intSettings = new Dictionary<string, int>(comparer);
		base.boolSettings = new Dictionary<string, bool>(comparer);
		base.floatSettings = new Dictionary<string, float>(comparer);
		base.stringListSettings = new Dictionary<string, List<string>>(comparer);
	}

	[OnDeserializing]
	internal void OnDeserializingMethod(StreamingContext context)
	{
		LoadDefaultValues();
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		StringComparer comparer = StringComparer.OrdinalIgnoreCase;
		if (base.stringSettings == null)
		{
			base.stringSettings = new Dictionary<string, string>(comparer);
		}
		if (base.intSettings == null)
		{
			base.intSettings = new Dictionary<string, int>(comparer);
		}
		if (base.boolSettings == null)
		{
			base.boolSettings = new Dictionary<string, bool>(comparer);
		}
		if (base.floatSettings == null)
		{
			base.floatSettings = new Dictionary<string, float>(comparer);
		}
		if (base.stringListSettings == null)
		{
			base.stringListSettings = new Dictionary<string, List<string>>(comparer);
		}
		DidDeserialize();
	}

	internal virtual void DidDeserialize()
	{
	}

	public virtual void Load()
	{
		LoadDefaultValues();
		if (!File.Exists(FileName) && File.Exists(BkpFileName))
		{
			File.Move(BkpFileName, FileName);
		}
		if (File.Exists(FileName))
		{
			try
			{
				string fileContents;
				using (TextReader textReader = new StreamReader(FileName))
				{
					fileContents = textReader.ReadToEnd();
					textReader.Close();
				}
				JsonConvert.PopulateObject(fileContents, this);
				return;
			}
			catch (Exception)
			{
				isnewfile = true;
				return;
			}
		}
		OtherDirty = true;
		isnewfile = true;
	}

	public virtual bool Save(bool force = false)
	{
		if (!IsDirty && !force)
		{
			return true;
		}
		try
		{
			using (TextWriter textWriter = new StreamWriter(TempFileName))
			{
				textWriter.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
				textWriter.Close();
			}
			if (!File.Exists(FileName))
			{
				File.Move(TempFileName, FileName);
			}
			else
			{
				File.Replace(TempFileName, FileName, BkpFileName);
			}
		}
		catch (IOException)
		{
			return false;
		}
		OtherDirty = false;
		BoolSettings.Dirty = false;
		StringSettings.Dirty = false;
		StringListSettings.Dirty = false;
		FloatSettings.Dirty = false;
		IntSettings.Dirty = false;
		return true;
	}

	public abstract void LoadDefaultValues();
}
