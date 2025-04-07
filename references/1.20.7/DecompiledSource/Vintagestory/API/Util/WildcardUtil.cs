#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;

namespace Vintagestory.API.Util;

public static class WildcardUtil
{
    //
    // Summary:
    //     Returns a new AssetLocation with the wildcards (*) being filled with the blocks
    //     other Code parts, if the wildcard matches. Example this block is trapdoor-up-north.
    //     search is *-up-*, replace is *-down-*, in this case this method will return trapdoor-down-north.
    //
    //
    // Parameters:
    //   code:
    //
    //   search:
    //
    //   replace:
    public static AssetLocation WildCardReplace(this AssetLocation code, AssetLocation search, AssetLocation replace)
    {
        if (search == code)
        {
            return search;
        }

        if (code == null || (search.Domain != "*" && search.Domain != code.Domain))
        {
            return null;
        }

        string text = Regex.Escape(search.Path).Replace("\\*", "(.*)");
        Match match = Regex.Match(code.Path, "^" + text + "$");
        if (!match.Success)
        {
            return null;
        }

        string text2 = replace.Path;
        for (int i = 1; i < match.Groups.Count; i++)
        {
            CaptureCollection captures = match.Groups[i].Captures;
            for (int j = 0; j < captures.Count; j++)
            {
                Capture capture = captures[j];
                int startIndex = text2.IndexOf('*');
                text2 = text2.Remove(startIndex, 1).Insert(startIndex, capture.Value);
            }
        }

        return new AssetLocation(code.Domain, text2);
    }

    public static bool Match(string needle, string haystack)
    {
        return fastMatch(needle, haystack);
    }

    public static bool Match(string[] needles, string haystack)
    {
        for (int i = 0; i < needles.Length; i++)
        {
            if (fastMatch(needles[i], haystack))
            {
                return true;
            }
        }

        return false;
    }

    public static bool Match(AssetLocation needle, AssetLocation haystack)
    {
        if (needle.Domain != "*" && needle.Domain != haystack.Domain)
        {
            return false;
        }

        return fastMatch(needle.Path, haystack.Path);
    }

    //
    // Summary:
    //     Checks whether or not the wildcard matches for inCode, for example, returns true
    //     for wildcard rock-* and inCode rock-granite
    //
    // Parameters:
    //   wildCard:
    //
    //   inCode:
    //
    //   allowedVariants:
    public static bool Match(AssetLocation wildCard, AssetLocation inCode, string[] allowedVariants)
    {
        if (wildCard.Equals(inCode))
        {
            return true;
        }

        int num;
        if (inCode == null || (wildCard.Domain != "*" && !wildCard.Domain.Equals(inCode.Domain)) || ((num = wildCard.Path.IndexOf('*')) == -1 && wildCard.Path.IndexOf('(') == -1))
        {
            return false;
        }

        if (num == wildCard.Path.Length - 1)
        {
            if (!StringUtil.FastStartsWith(inCode.Path, wildCard.Path, num))
            {
                return false;
            }
        }
        else
        {
            if (!StringUtil.FastStartsWith(inCode.Path, wildCard.Path, num))
            {
                return false;
            }

            string text = Regex.Escape(wildCard.Path).Replace("\\*", "(.*)");
            if (!Regex.IsMatch(inCode.Path, "^" + text + "$", RegexOptions.None))
            {
                return false;
            }
        }

        if (allowedVariants != null && !MatchesVariants(wildCard, inCode, allowedVariants))
        {
            return false;
        }

        return true;
    }

    public static bool MatchesVariants(AssetLocation wildCard, AssetLocation inCode, string[] allowedVariants)
    {
        int num = wildCard.Path.IndexOf('*');
        int num2 = wildCard.Path.Length - num - 1;
        if (inCode.Path.Length <= num)
        {
            return false;
        }

        string text = inCode.Path.Substring(num);
        if (text.Length - num2 <= 0)
        {
            return false;
        }

        string value = text.Substring(0, text.Length - num2);
        return allowedVariants.Contains(value);
    }

    //
    // Summary:
    //     Extract the value matched by the wildcard. For exammple for rock-* and inCode
    //     rock-granite, this method will return 'granite' Returns null if the wildcard
    //     does not match
    //
    // Parameters:
    //   wildCard:
    //
    //   inCode:
    public static string GetWildcardValue(AssetLocation wildCard, AssetLocation inCode)
    {
        if (inCode == null || (wildCard.Domain != "*" && !wildCard.Domain.Equals(inCode.Domain)))
        {
            return null;
        }

        if (!wildCard.Path.Contains('*'))
        {
            return null;
        }

        string text = Regex.Escape(wildCard.Path).Replace("\\*", "(.*)");
        Match match = Regex.Match(inCode.Path, "^" + text + "$", RegexOptions.None);
        if (!match.Success)
        {
            return null;
        }

        return match.Groups[1].Captures[0].Value;
    }

    private static bool fastMatch(string needle, string haystack)
    {
        if (haystack == null)
        {
            throw new ArgumentNullException("Text cannot be null");
        }

        if (needle.Length == 0)
        {
            return false;
        }

        if (needle[0] == '@')
        {
            return Regex.IsMatch(haystack, "^" + needle.Substring(1) + "$", RegexOptions.None);
        }

        int length = needle.Length;
        for (int i = 0; i < length; i++)
        {
            char c = needle[i];
            if (c == '*')
            {
                int num = length - 1 - i;
                if (num == 0)
                {
                    return true;
                }

                int num2 = needle.IndexOf('*', i + 1);
                if (num2 >= 0)
                {
                    if (needle.IndexOf('*', num2 + 1) >= 0)
                    {
                        needle = Regex.Escape(needle).Replace("\\*", ".*");
                        return Regex.IsMatch(haystack, "^" + needle + "$", RegexOptions.IgnoreCase);
                    }

                    if (haystack.Length < needle.Length - 2)
                    {
                        return false;
                    }

                    int num3 = length - (num2 + 1);
                    if (!EndsWith(haystack, needle, num3))
                    {
                        return false;
                    }

                    string value = needle.Substring(i + 1, num2 - (i + 1)).ToLowerInvariant();
                    if (i == 0 && num3 == 0)
                    {
                        return haystack.ToLowerInvariant().Contains(value);
                    }

                    return haystack.Substring(i, haystack.Length - i - num3).ToLowerInvariant().Contains(value);
                }

                if (haystack.Length >= needle.Length - 1)
                {
                    return EndsWith(haystack, needle, num);
                }

                return false;
            }

            if (haystack.Length <= i)
            {
                return false;
            }

            char c2 = haystack[i];
            if (c != c2 && char.ToLowerInvariant(c) != char.ToLowerInvariant(c2))
            {
                return false;
            }
        }

        return needle.Length == haystack.Length;
    }

    private static bool EndsWith(string haystack, string needle, int endCharsCount)
    {
        int num = haystack.Length - 1;
        int num2 = needle.Length - 1;
        for (int i = 0; i < endCharsCount; i++)
        {
            char c = haystack[num - i];
            char c2 = needle[num2 - i];
            if (c2 != c && char.ToLowerInvariant(c2) != char.ToLowerInvariant(c))
            {
                return false;
            }
        }

        return true;
    }

    internal static bool fastExactMatch(string needle, string haystack)
    {
        if (haystack.Length != needle.Length)
        {
            return false;
        }

        for (int num = needle.Length - 1; num >= 0; num--)
        {
            char c = needle[num];
            char c2 = haystack[num];
            if (c != c2 && char.ToLowerInvariant(c) != char.ToLowerInvariant(c2))
            {
                return false;
            }
        }

        return true;
    }

    //
    // Summary:
    //     Requires a pre-check that needle.Length is at least 1, and needleAsRegex has
    //     been pre-prepared
    //
    // Parameters:
    //   needle:
    //
    //   haystack:
    //
    //   needleAsRegex:
    //     If it starts with '^' interpret as a regex search string; otherwise special case,
    //     it represents the tailpiece of the needle following a single asterisk
    internal static bool fastMatch(string needle, string haystack, string needleAsRegex)
    {
        int length = needleAsRegex.Length;
        if (length > 0 && needleAsRegex[0] == '^')
        {
            return Regex.IsMatch(haystack, needleAsRegex, RegexOptions.IgnoreCase);
        }

        if (haystack.Length < needle.Length - 1)
        {
            return false;
        }

        if (length != 0 && !EndsWith(haystack, needle, length))
        {
            return false;
        }

        int num = needle.Length - length - 1;
        for (int i = 0; i < num; i++)
        {
            char c = needle[i];
            char c2 = haystack[i];
            if (c != c2 && char.ToLowerInvariant(c) != char.ToLowerInvariant(c2))
            {
                return false;
            }
        }

        return true;
    }

    //
    // Summary:
    //     Returns the needle as a Regex string, if we are going to need to do a Regex search;
    //     alternatively returns some special case values
    //     Special case: return value of null signifies no wildcard, look for exact matches
    //     only
    //     Special case: return value of a non-regex string (not starting '^') represents
    //     the tailpiece part of the needle (the part following a single wildcard)
    //
    // Parameters:
    //   needle:
    internal static string Prepare(string needle)
    {
        if (needle[0] == '@')
        {
            return "^" + needle.Substring(1) + "$";
        }

        int num = needle.IndexOf('*');
        if (num == -1)
        {
            return null;
        }

        if (needle[0] != '^' && needle.IndexOf('*', num + 1) < 0)
        {
            return needle.Substring(num + 1);
        }

        needle = Regex.Escape(needle).Replace("\\*", ".*");
        return "^" + needle + "$";
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
