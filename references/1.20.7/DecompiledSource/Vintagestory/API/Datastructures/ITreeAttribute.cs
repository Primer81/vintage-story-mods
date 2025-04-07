#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.API.Datastructures;

//
// Summary:
//     Represents a List of nestable Attributes
public interface ITreeAttribute : IAttribute, IEnumerable<KeyValuePair<string, IAttribute>>, IEnumerable
{
    //
    // Summary:
    //     Will return null if given attribute does not exist
    //
    // Parameters:
    //   key:
    IAttribute this[string key] { get; set; }

    //
    // Summary:
    //     Amount of elements in this Tree attribute
    int Count { get; }

    //
    // Summary:
    //     Returns all values inside this tree attributes
    IAttribute[] Values { get; }

    //
    // Summary:
    //     True if this attribute exists
    //
    // Parameters:
    //   key:
    bool HasAttribute(string key);

    //
    // Summary:
    //     Similar to TryGetValue for a Dictionary
    //
    // Parameters:
    //   key:
    //
    //   value:
    bool TryGetAttribute(string key, out IAttribute value);

    //
    // Summary:
    //     Removes an attribute
    //
    // Parameters:
    //   key:
    void RemoveAttribute(string key);

    //
    // Summary:
    //     Creates a bool attribute with given key and value
    //
    // Parameters:
    //   key:
    //
    //   value:
    void SetBool(string key, bool value);

    //
    // Summary:
    //     Creates an int attribute with given key and value
    //
    // Parameters:
    //   key:
    //
    //   value:
    void SetInt(string key, int value);

    //
    // Summary:
    //     Creates a long attribute with given key and value
    //
    // Parameters:
    //   key:
    //
    //   value:
    void SetLong(string key, long value);

    //
    // Summary:
    //     Creates a double attribute with given key and value
    //
    // Parameters:
    //   key:
    //
    //   value:
    void SetDouble(string key, double value);

    //
    // Summary:
    //     Creates a float attribute with given key and value
    //
    // Parameters:
    //   key:
    //
    //   value:
    void SetFloat(string key, float value);

    //
    // Summary:
    //     Creates a string attribute with given key and value
    //
    // Parameters:
    //   key:
    //
    //   value:
    void SetString(string key, string value);

    //
    // Summary:
    //     Creates a byte[] attribute with given key and value
    //
    // Parameters:
    //   key:
    //
    //   value:
    void SetBytes(string key, byte[] value);

    //
    // Summary:
    //     Sets given item stack with given key
    //
    // Parameters:
    //   key:
    //
    //   itemstack:
    void SetItemstack(string key, ItemStack itemstack);

    //
    // Summary:
    //     Retrieves a bool or null if the key is not found
    //
    // Parameters:
    //   key:
    bool? TryGetBool(string key);

    //
    // Summary:
    //     Retrieves a bool or default value if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    bool GetBool(string key, bool defaultValue = false);

    //
    // Summary:
    //     Retrieves an int or null if the key is not found
    //
    // Parameters:
    //   key:
    int? TryGetInt(string key);

    //
    // Summary:
    //     Retrieves an int or default value if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    int GetInt(string key, int defaultValue = 0);

    //
    // Summary:
    //     Same as (int)Vintagestory.API.Datastructures.ITreeAttribute.GetDecimal(System.String,System.Double)
    //
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    int GetAsInt(string key, int defaultValue = 0);

    //
    // Summary:
    //     Returns true/false, for whatever type of attribute is found for given key
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    bool GetAsBool(string key, bool defaultValue = false);

    //
    // Summary:
    //     Retrieves an int, float, long or double value. Whatever attribute is found for
    //     given key
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    double GetDecimal(string key, double defaultValue = 0.0);

    //
    // Summary:
    //     Retrieves the value of given attribute, independent of attribute type
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    string GetAsString(string key, string defaultValue = null);

    //
    // Summary:
    //     Retrieves a long or null value if key is not found
    //
    // Parameters:
    //   key:
    long? TryGetLong(string key);

    //
    // Summary:
    //     Retrieves a long or default value if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    long GetLong(string key, long defaultValue = 0L);

    //
    // Summary:
    //     Retrieves a float or null if the key is not found
    //
    // Parameters:
    //   key:
    float? TryGetFloat(string key);

    //
    // Summary:
    //     Retrieves a float or defaultvalue if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    float GetFloat(string key, float defaultValue = 0f);

    //
    // Summary:
    //     Retrieves a double or null if key is not found
    //
    // Parameters:
    //   key:
    double? TryGetDouble(string key);

    //
    // Summary:
    //     Retrieves a double or defaultValue if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    double GetDouble(string key, double defaultValue = 0.0);

    //
    // Summary:
    //     Retrieves a string or defaultValue if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    string GetString(string key, string defaultValue = null);

    //
    // Summary:
    //     Retrieves a byte array or defaultValue if key is not found
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    byte[] GetBytes(string key, byte[] defaultValue = null);

    //
    // Summary:
    //     Retrieves an itemstack or defaultValue if key is not found. Be sure to call stack.ResolveBlockOrItem()
    //     after retrieving it.
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    ItemStack GetItemstack(string key, ItemStack defaultValue = null);

    //
    // Summary:
    //     Retrieves an attribute tree or null if key is not found
    //
    // Parameters:
    //   key:
    ITreeAttribute GetTreeAttribute(string key);

    //
    // Summary:
    //     Retrieves an attribute tree or adds it if key is not found. Throws an exception
    //     if the key does exist but is not a tree.
    //
    // Parameters:
    //   key:
    ITreeAttribute GetOrAddTreeAttribute(string key);

    //
    // Summary:
    //     Creates a deep copy of the attribute tree
    new ITreeAttribute Clone();

    //
    // Summary:
    //     Merges trees (it will overwrite existing values)
    //
    // Parameters:
    //   tree:
    void MergeTree(ITreeAttribute tree);

    //
    // Summary:
    //     Returns a ITreeAttribute sorted alphabetically by key. Does not modify the existing
    //     ITreeAttribute
    //
    // Parameters:
    //   recursive:
    OrderedDictionary<string, IAttribute> SortedCopy(bool recursive = false);

    bool Equals(IWorldAccessor worldForResolve, IAttribute attr, params string[] ignoreSubTrees);

    bool IsSubSetOf(IWorldAccessor worldForResolve, IAttribute other);

    int GetHashCode(string[] ignoredAttributes);
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
