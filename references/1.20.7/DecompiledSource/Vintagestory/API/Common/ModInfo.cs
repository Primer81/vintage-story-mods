#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

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

//
// Summary:
//     Meta data for a specific mod folder, archive, source file or assembly. Either
//     loaded from a "modinfo.json" or from the assembly's Vintagestory.API.Common.ModInfoAttribute.
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
            foreach (ModDependency item in (IEnumerable<ModDependency>)value)
            {
                writer.WritePropertyName(item.ModID);
                writer.WriteValue(item.Version);
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
            IEnumerable<object> enumerable = from e in JArray.Load(reader)
                                             select e.ToObject(elementType);
            Type type = typeof(List<>).MakeGenericType(elementType);
            IList list = (IList)Activator.CreateInstance(type);
            foreach (object item in enumerable)
            {
                list.Add(item);
            }

            return type.GetMethod("AsReadOnly").Invoke(list, new object[0]);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type objectType = value.GetType().GetGenericArguments()[0];
            writer.WriteStartArray();
            foreach (object item in (IEnumerable)value)
            {
                serializer.Serialize(writer, item, objectType);
            }

            writer.WriteEndArray();
        }
    }

    private IReadOnlyList<string> _authors = new string[0];

    //
    // Summary:
    //     The type of this mod. Can be "Theme", "Content" or "Code".
    [JsonRequired]
    public EnumModType Type;

    //
    // Summary:
    //     If the mod is a texture pack that changes topsoil grass textures, define the
    //     texture size here
    [JsonProperty]
    public int TextureSize = 32;

    //
    // Summary:
    //     The name of this mod. For example "My Example Mod".
    [JsonRequired]
    public string Name;

    //
    // Summary:
    //     The version of this mod. For example "2.10.4". (optional)
    [JsonProperty]
    public string Version = "";

    //
    // Summary:
    //     The network version of this mod. Change this number when a user that has an older
    //     version of your mod should not be allowed to connected to server with a newer
    //     version. If not set, the version value is used.
    [JsonProperty]
    public string NetworkVersion;

    //
    // Summary:
    //     A short description of what this mod does. (optional)
    [JsonProperty]
    public string Description = "";

    //
    // Summary:
    //     Location of the website or project site of this mod. (optional)
    [JsonProperty]
    public string Website = "";

    //
    // Summary:
    //     Not exposed as a JsonProperty, only coded mods can set this to true
    public bool CoreMod;

    //
    // Summary:
    //     The mod id (domain) of this mod. For example "myexamplemod". (Optional. Uses
    //     mod name (converted to lowercase, stripped of whitespace and special characters)
    //     if missing.)
    [JsonProperty]
    public string ModID { get; set; }

    //
    // Summary:
    //     Names of people working on this mod. (optional)
    [JsonProperty]
    public IReadOnlyList<string> Authors
    {
        get
        {
            return _authors;
        }
        set
        {
            IEnumerable<string> source = value ?? Enumerable.Empty<string>();
            _authors = source.ToList().AsReadOnly();
        }
    }

    //
    // Summary:
    //     Names of people contributing to this mod. (optional)
    [JsonProperty]
    public IReadOnlyList<string> Contributors { get; set; } = new List<string>().AsReadOnly();


    //
    // Summary:
    //     Which side(s) this mod runs on. Can be "Server", "Client" or "Universal". (Optional.
    //     Universal (both server and client) by default.)
    [JsonProperty]
    [JsonConverter(typeof(StringEnumConverter))]
    public EnumAppSide Side { get; set; } = EnumAppSide.Universal;


    //
    // Summary:
    //     If set to false and the mod is universal, clients don't need the mod to join.
    //     (Optional. True by default.)
    [JsonProperty]
    public bool RequiredOnClient { get; set; } = true;


    //
    // Summary:
    //     If set to false and the mod is universal, the mod is not disabled if it's not
    //     present on the server. (Optional. True by default.)
    [JsonProperty]
    public bool RequiredOnServer { get; set; } = true;


    //
    // Summary:
    //     List of mods (and versions) this mod depends on.
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

    //
    // Summary:
    //     Attempts to convert the specified mod name to a mod ID, stripping any non-alphanumerical
    //     (including spaces and dashes) and lowercasing letters.
    public static string ToModID(string name)
    {
        if (name == null)
        {
            return null;
        }

        StringBuilder stringBuilder = new StringBuilder(name.Length);
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            bool num = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
            bool flag = c >= '0' && c <= '9';
            if (num || flag)
            {
                stringBuilder.Append(char.ToLower(c));
            }

            if (flag && i == 0)
            {
                throw new ArgumentException("Can't convert '" + name + "' to a mod ID automatically, because it starts with a number, which is illegal", "name");
            }
        }

        return stringBuilder.ToString();
    }

    //
    // Summary:
    //     Returns whether the specified domain is valid. Tests if the string is non-null,
    //     has a length of at least 1, starts with a basic lowercase letter and contains
    //     only lowercase letters and numbers.
    public static bool IsValidModID(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return false;
        }

        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
            bool num = c >= 'a' && c <= 'z';
            bool flag = c >= '0' && c <= '9';
            if (!num && (!flag || i == 0))
            {
                return false;
            }
        }

        return true;
    }

    public int CompareTo(ModInfo other)
    {
        int num = ModID.CompareOrdinal(other.ModID);
        if (num != 0)
        {
            return num;
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
#if false // Decompilation log
'182' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Newtonsoft.Json.dll'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
