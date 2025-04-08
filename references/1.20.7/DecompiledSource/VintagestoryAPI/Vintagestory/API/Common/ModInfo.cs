using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

/// <summary>
/// Meta data for a specific mod folder, archive, source file or assembly.
/// Either loaded from a "modinfo.json" or from the assembly's
/// <see cref="T:Vintagestory.API.Common.ModInfoAttribute" />.
/// </summary>
public class ModInfo : IComparable<ModInfo>
{
	private class DependenciesConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return typeof(IEnumerable<ModDependency>).IsAssignableFrom(objectType);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return (from prop in JObject.Load(reader).Properties()
				select new ModDependency(prop.Name, (string?)prop.Value)).ToList().AsReadOnly();
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			foreach (ModDependency dependency in (IEnumerable<ModDependency>)value)
			{
				writer.WritePropertyName(dependency.ModID);
				writer.WriteValue(dependency.Version);
			}
			writer.WriteEndObject();
		}
	}

	private class ReadOnlyListConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			if (objectType.IsGenericType)
			{
				return objectType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);
			}
			return false;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			Type elementType = objectType.GetGenericArguments()[0];
			IEnumerable<object> elements = from e in JArray.Load(reader)
				select e.ToObject(elementType);
			Type listType = typeof(List<>).MakeGenericType(elementType);
			IList list = (IList)Activator.CreateInstance(listType);
			foreach (object element in elements)
			{
				list.Add(element);
			}
			return listType.GetMethod("AsReadOnly").Invoke(list, new object[0]);
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			Type elementType = value.GetType().GetGenericArguments()[0];
			writer.WriteStartArray();
			foreach (object element in (IEnumerable)value)
			{
				serializer.Serialize(writer, element, elementType);
			}
			writer.WriteEndArray();
		}
	}

	private IReadOnlyList<string> _authors = new string[0];

	/// <summary> The type of this mod. Can be "Theme", "Content" or "Code". </summary>
	[JsonRequired]
	public EnumModType Type;

	/// <summary>
	/// If the mod is a texture pack that changes topsoil grass textures, define the texture size here
	/// </summary>
	[JsonProperty]
	public int TextureSize = 32;

	/// <summary> The name of this mod. For example "My Example Mod". </summary>
	[JsonRequired]
	public string Name;

	/// <summary> The version of this mod. For example "2.10.4". (optional) </summary>
	[JsonProperty]
	public string Version = "";

	/// <summary>
	/// The network version of this mod. Change this number when a user that has an older version of your mod should not be allowed to connected to server with a newer version. If not set, the version value is used.
	/// </summary>
	[JsonProperty]
	public string NetworkVersion;

	/// <summary> A short description of what this mod does. (optional) </summary>
	[JsonProperty]
	public string Description = "";

	/// <summary> Location of the website or project site of this mod. (optional) </summary>
	[JsonProperty]
	public string Website = "";

	/// <summary> Not exposed as a JsonProperty, only coded mods can set this to true </summary>
	public bool CoreMod;

	/// <summary>
	/// The mod id (domain) of this mod. For example "myexamplemod".
	/// (Optional. Uses mod name (converted to lowercase, stripped of
	/// whitespace and special characters) if missing.)
	/// </summary>
	[JsonProperty]
	public string ModID { get; set; }

	/// <summary> Names of people working on this mod. (optional) </summary>
	[JsonProperty]
	public IReadOnlyList<string> Authors
	{
		get
		{
			return _authors;
		}
		set
		{
			IEnumerable<string> authors = value ?? Enumerable.Empty<string>();
			_authors = authors.ToList().AsReadOnly();
		}
	}

	/// <summary> Names of people contributing to this mod. (optional) </summary>
	[JsonProperty]
	public IReadOnlyList<string> Contributors { get; set; } = new List<string>().AsReadOnly();


	/// <summary>
	/// Which side(s) this mod runs on. Can be "Server", "Client" or "Universal".
	/// (Optional. Universal (both server and client) by default.)
	/// </summary>
	[JsonProperty]
	[JsonConverter(typeof(StringEnumConverter))]
	public EnumAppSide Side { get; set; } = EnumAppSide.Universal;


	/// <summary>
	/// If set to false and the mod is universal, clients don't need the mod
	/// to join. (Optional. True by default.)
	/// </summary>
	[JsonProperty]
	public bool RequiredOnClient { get; set; } = true;


	/// <summary>
	/// If set to false and the mod is universal, the mod is not disabled
	/// if it's not present on the server. (Optional. True by default.)
	/// </summary>
	[JsonProperty]
	public bool RequiredOnServer { get; set; } = true;


	/// <summary> List of mods (and versions) this mod depends on. </summary>
	[JsonProperty]
	[JsonConverter(typeof(DependenciesConverter))]
	public IReadOnlyList<ModDependency> Dependencies { get; set; } = new List<ModDependency>().AsReadOnly();


	public ModInfo()
	{
	}

	public ModInfo(EnumModType type, string name, string modID, string version, string description, IEnumerable<string> authors, IEnumerable<string> contributors, string website, EnumAppSide side, bool requiredOnClient, bool requiredOnServer, IEnumerable<ModDependency> dependencies)
	{
		Type = type;
		Name = name ?? throw new ArgumentNullException("name");
		ModID = modID ?? throw new ArgumentNullException("modID");
		Version = version ?? "";
		Description = description ?? "";
		Authors = ReadOnlyCopy<string>(authors);
		Contributors = ReadOnlyCopy<string>(contributors);
		Website = website ?? "";
		Side = side;
		RequiredOnClient = requiredOnClient;
		RequiredOnServer = requiredOnServer;
		Dependencies = ReadOnlyCopy<ModDependency>(dependencies);
		static IReadOnlyList<T> ReadOnlyCopy<T>(IEnumerable<T> elements)
		{
			return (elements ?? Enumerable.Empty<T>()).ToList().AsReadOnly();
		}
	}

	[OnDeserialized]
	private void OnDeserialized(StreamingContext ctx)
	{
		ModID = ModID ?? ToModID(Name);
	}

	public void Init()
	{
		if (NetworkVersion == null)
		{
			NetworkVersion = Version;
		}
	}

	/// <summary>
	/// Attempts to convert the specified mod name to a mod ID, stripping any
	/// non-alphanumerical (including spaces and dashes) and lowercasing letters.
	/// </summary>
	public static string ToModID(string name)
	{
		if (name == null)
		{
			return null;
		}
		StringBuilder sb = new StringBuilder(name.Length);
		for (int i = 0; i < name.Length; i++)
		{
			char chr = name[i];
			bool num = (chr >= 'a' && chr <= 'z') || (chr >= 'A' && chr <= 'Z');
			bool isDigit = chr >= '0' && chr <= '9';
			if (num || isDigit)
			{
				sb.Append(char.ToLower(chr));
			}
			if (isDigit && i == 0)
			{
				throw new ArgumentException("Can't convert '" + name + "' to a mod ID automatically, because it starts with a number, which is illegal", "name");
			}
		}
		return sb.ToString();
	}

	/// <summary>
	/// Returns whether the specified domain is valid.
	///
	/// Tests if the string is non-null, has a length of at least 1, starts with
	/// a basic lowercase letter and contains only lowercase letters and numbers.
	/// </summary>
	public static bool IsValidModID(string str)
	{
		if (string.IsNullOrEmpty(str))
		{
			return false;
		}
		for (int i = 0; i < str.Length; i++)
		{
			char chr = str[i];
			bool num = chr >= 'a' && chr <= 'z';
			bool isDigit = chr >= '0' && chr <= '9';
			if (!num && (!isDigit || i == 0))
			{
				return false;
			}
		}
		return true;
	}

	public int CompareTo(ModInfo other)
	{
		int r = ModID.CompareOrdinal(other.ModID);
		if (r != 0)
		{
			return r;
		}
		if (GameVersion.IsNewerVersionThan(Version, other.Version))
		{
			return -1;
		}
		if (GameVersion.IsLowerVersionThan(Version, other.Version))
		{
			return 1;
		}
		return 0;
	}
}
