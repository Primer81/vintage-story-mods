#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

//
// Summary:
//     A registerable object with variants, i.e. an item, a block or an entity
public abstract class RegistryObject
{
    //
    // Summary:
    //     A unique domain + code of the object. Must be globally unique for all items /
    //     all blocks / all entities.
    public AssetLocation Code;

    //
    // Summary:
    //     Variant values as resolved from blocktype/itemtype. NOT set for entities - use
    //     entity.Properties.VariantStrict instead.
    public OrderedDictionary<string, string> VariantStrict = new OrderedDictionary<string, string>();

    //
    // Summary:
    //     Variant values as resolved from blocktype/itemtype. Will not throw an null pointer
    //     exception when the key does not exist, but return null instead. NOT set for entities
    //     - use entity.Properties.Variant instead
    public RelaxedReadOnlyDictionary<string, string> Variant;

    //
    // Summary:
    //     The class handeling the object
    public string Class;

    public RegistryObject()
    {
        Variant = new RelaxedReadOnlyDictionary<string, string>(VariantStrict);
    }

    //
    // Summary:
    //     Returns a new assetlocation with an equal domain and the given path
    //
    // Parameters:
    //   path:
    public AssetLocation CodeWithPath(string path)
    {
        return Code.CopyWithPath(path);
    }

    //
    // Summary:
    //     Removes componentsToRemove parts from the blocks code end by splitting it up
    //     at every occurence of a dash ('-'). Right to left.
    //
    // Parameters:
    //   componentsToRemove:
    public string CodeWithoutParts(int componentsToRemove)
    {
        int num = Code.Path.Length;
        int length = 0;
        while (--num > 0 && componentsToRemove > 0)
        {
            if (Code.Path[num] == '-')
            {
                length = num;
                componentsToRemove--;
            }
        }

        return Code.Path.Substring(0, length);
    }

    //
    // Summary:
    //     Removes componentsToRemove parts from the blocks code beginning by splitting
    //     it up at every occurence of a dash ('-'). Left to Right
    //
    // Parameters:
    //   componentsToRemove:
    public string CodeEndWithoutParts(int componentsToRemove)
    {
        int num = 0;
        int num2 = 0;
        while (++num < Code.Path.Length && componentsToRemove > 0)
        {
            if (Code.Path[num] == '-')
            {
                num2 = num + 1;
                componentsToRemove--;
            }
        }

        return Code.Path.Substring(num2, Code.Path.Length - num2);
    }

    //
    // Summary:
    //     Replaces the last parts from the blocks code and replaces it with components
    //     by splitting it up at every occurence of a dash ('-')
    //
    // Parameters:
    //   components:
    public AssetLocation CodeWithParts(params string[] components)
    {
        if (Code == null)
        {
            return null;
        }

        AssetLocation assetLocation = Code.CopyWithPath(CodeWithoutParts(components.Length));
        for (int i = 0; i < components.Length; i++)
        {
            assetLocation.Path = assetLocation.Path + "-" + components[i];
        }

        return assetLocation;
    }

    //
    // Summary:
    //     More efficient version of CodeWithParts if there is only a single parameter
    //
    // Parameters:
    //   component:
    public AssetLocation CodeWithParts(string component)
    {
        if (Code == null)
        {
            return null;
        }

        return Code.CopyWithPath(CodeWithoutParts(1) + "-" + component);
    }

    public AssetLocation CodeWithVariant(string type, string value)
    {
        StringBuilder stringBuilder = new StringBuilder(FirstCodePart());
        foreach (KeyValuePair<string, string> item in Variant)
        {
            stringBuilder.Append("-");
            if (item.Key == type)
            {
                stringBuilder.Append(value);
            }
            else
            {
                stringBuilder.Append(item.Value);
            }
        }

        return new AssetLocation(Code.Domain, stringBuilder.ToString());
    }

    public AssetLocation CodeWithVariants(Dictionary<string, string> valuesByType)
    {
        StringBuilder stringBuilder = new StringBuilder(FirstCodePart());
        foreach (KeyValuePair<string, string> item in Variant)
        {
            stringBuilder.Append("-");
            if (valuesByType.TryGetValue(item.Key, out var value))
            {
                stringBuilder.Append(value);
            }
            else
            {
                stringBuilder.Append(item.Value);
            }
        }

        return new AssetLocation(Code.Domain, stringBuilder.ToString());
    }

    public AssetLocation CodeWithVariants(string[] types, string[] values)
    {
        StringBuilder stringBuilder = new StringBuilder(FirstCodePart());
        foreach (KeyValuePair<string, string> item in Variant)
        {
            stringBuilder.Append("-");
            int num = types.IndexOf(item.Key);
            if (num >= 0)
            {
                stringBuilder.Append(values[num]);
            }
            else
            {
                stringBuilder.Append(item.Value);
            }
        }

        return new AssetLocation(Code.Domain, stringBuilder.ToString());
    }

    //
    // Summary:
    //     Replaces one part from the blocks code and replaces it with components by splitting
    //     it up at every occurence of a dash ('-')
    //
    // Parameters:
    //   part:
    //
    //   atPosition:
    public AssetLocation CodeWithPart(string part, int atPosition = 0)
    {
        if (Code == null)
        {
            return null;
        }

        AssetLocation assetLocation = Code.Clone();
        string[] array = assetLocation.Path.Split('-');
        array[atPosition] = part;
        assetLocation.Path = string.Join("-", array);
        return assetLocation;
    }

    //
    // Summary:
    //     Returns the n-th code part in inverse order. If the code contains no dash ('-')
    //     the whole code is returned. Returns null if posFromRight is too high.
    //
    // Parameters:
    //   posFromRight:
    public string LastCodePart(int posFromRight = 0)
    {
        if (Code == null)
        {
            return null;
        }

        if (posFromRight == 0 && !Code.Path.Contains('-'))
        {
            return Code.Path;
        }

        string[] array = Code.Path.Split('-');
        if (array.Length - 1 - posFromRight < 0)
        {
            return null;
        }

        return array[array.Length - 1 - posFromRight];
    }

    //
    // Summary:
    //     Returns the n-th code part. If the code contains no dash ('-') the whole code
    //     is returned. Returns null if posFromLeft is too high.
    //
    // Parameters:
    //   posFromLeft:
    public string FirstCodePart(int posFromLeft = 0)
    {
        if (Code == null)
        {
            return null;
        }

        if (posFromLeft == 0 && !Code.Path.Contains('-'))
        {
            return Code.Path;
        }

        string[] array = Code.Path.Split('-');
        if (posFromLeft > array.Length - 1)
        {
            return null;
        }

        return array[posFromLeft];
    }

    //
    // Summary:
    //     Returns true if any given wildcard matches the blocks/items code. E.g. water-*
    //     will match all water blocks
    //
    // Parameters:
    //   wildcards:
    public bool WildCardMatch(AssetLocation[] wildcards)
    {
        foreach (AssetLocation wildCard in wildcards)
        {
            if (WildCardMatch(wildCard))
            {
                return true;
            }
        }

        return false;
    }

    //
    // Summary:
    //     Returns true if given wildcard matches the blocks/items code. E.g. water-* will
    //     match all water blocks
    //
    // Parameters:
    //   wildCard:
    public bool WildCardMatch(AssetLocation wildCard)
    {
        if (Code != null)
        {
            return WildcardUtil.Match(wildCard, Code);
        }

        return false;
    }

    //
    // Summary:
    //     Returns true if any given wildcard matches the blocks/items code. E.g. water-*
    //     will match all water blocks
    //
    // Parameters:
    //   wildcards:
    public bool WildCardMatch(string[] wildcards)
    {
        foreach (string wildCard in wildcards)
        {
            if (WildCardMatch(wildCard))
            {
                return true;
            }
        }

        return false;
    }

    //
    // Summary:
    //     Returns true if given wildcard matches the blocks/items code. E.g. water-* will
    //     match all water blocks
    //
    // Parameters:
    //   wildCard:
    public bool WildCardMatch(string wildCard)
    {
        if (Code != null)
        {
            return WildcardUtil.Match(wildCard, Code.Path);
        }

        return false;
    }

    //
    // Summary:
    //     Used by the block loader to replace wildcards with their final values
    //
    // Parameters:
    //   input:
    //
    //   searchReplace:
    public static AssetLocation FillPlaceHolder(AssetLocation input, OrderedDictionary<string, string> searchReplace)
    {
        foreach (KeyValuePair<string, string> item in searchReplace)
        {
            input.Path = FillPlaceHolder(input.Path, item.Key, item.Value);
        }

        return input;
    }

    //
    // Summary:
    //     Used by the block loader to replace wildcards with their final values
    //
    // Parameters:
    //   input:
    //
    //   searchReplace:
    public static string FillPlaceHolder(string input, OrderedDictionary<string, string> searchReplace)
    {
        foreach (KeyValuePair<string, string> item in searchReplace)
        {
            input = FillPlaceHolder(input, item.Key, item.Value);
        }

        return input;
    }

    //
    // Summary:
    //     Used by the block loader to replace wildcards with their final values
    //
    // Parameters:
    //   input:
    //
    //   search:
    //
    //   replace:
    public static string FillPlaceHolder(string input, string search, string replace)
    {
        string pattern = "\\{((" + search + ")|([^\\{\\}]*\\|" + search + ")|(" + search + "\\|[^\\{\\}]*)|([^\\{\\}]*\\|" + search + "\\|[^\\{\\}]*))\\}";
        return Regex.Replace(input, pattern, replace);
    }
}
#if false // Decompilation log
'181' items in cache
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
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
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
