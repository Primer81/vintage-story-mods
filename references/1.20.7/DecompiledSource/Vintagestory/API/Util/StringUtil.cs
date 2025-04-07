#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Globalization;
using System.Numerics;
using System.Text;
using Vintagestory.API.Config;

namespace Vintagestory.API.Util;

public static class StringUtil
{
    public unsafe static int GetNonRandomizedHashCode(this string str)
    {
        fixed (char* ptr = str)
        {
            uint num = 352654597u;
            uint num2 = num;
            uint* ptr2 = (uint*)ptr;
            int num3 = str.Length;
            while (num3 > 2)
            {
                num3 -= 4;
                num = (BitOperations.RotateLeft(num, 5) + num) ^ *ptr2;
                num2 = (BitOperations.RotateLeft(num2, 5) + num2) ^ ptr2[1];
                ptr2 += 2;
            }

            if (num3 > 0)
            {
                num2 = (BitOperations.RotateLeft(num2, 5) + num2) ^ *ptr2;
            }

            return (int)(num + num2 * 1566083941);
        }
    }

    //
    // Summary:
    //     IMPORTANT! This method should be used for every IndexOf operation in our code
    //     (except possibly in localised output to the user). This is important in order
    //     to avoid any culture-specific different results even when indexing GLSL shader
    //     code or other code strings, etc., or other strings in English, when the current
    //     culture is a different language (Known issue in the Thai language which has no
    //     spaces and treats punctuation marks as invisible, see https://github.com/dotnet/runtime/issues/59120)
    //
    //     See also: https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings
    //
    //
    // Parameters:
    //   a:
    //
    //   b:
    public static int IndexOfOrdinal(this string a, string b)
    {
        return a.IndexOf(b, StringComparison.Ordinal);
    }

    //
    // Summary:
    //     IMPORTANT! This method should be used for every StartsWith operation in our code
    //     (except possibly in localised output to the user). This is important in order
    //     to avoid any culture-specific different results even when examining strings in
    //     English, when the user machine's current culture is a different language (Known
    //     issue in the Thai language which has no spaces and treats punctuation marks as
    //     invisible, see https://github.com/dotnet/runtime/issues/59120)
    //     See also: https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings
    //
    //
    // Parameters:
    //   a:
    //
    //   b:
    public static bool StartsWithOrdinal(this string a, string b)
    {
        return a.StartsWith(b, StringComparison.Ordinal);
    }

    //
    // Summary:
    //     IMPORTANT! This method should be used for every EndsWith operation in our code
    //     (except possibly in localised output to the user). This is important in order
    //     to avoid any culture-specific different results even when examining strings in
    //     English, when the user machine's current culture is a different language (Known
    //     issue in the Thai language which has no spaces and treats punctuation marks as
    //     invisible, see https://github.com/dotnet/runtime/issues/59120)
    //     See also: https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings
    //
    //
    // Parameters:
    //   a:
    //
    //   b:
    public static bool EndsWithOrdinal(this string a, string b)
    {
        return a.EndsWith(b, StringComparison.Ordinal);
    }

    //
    // Summary:
    //     This should be used for every string comparison when ordering strings (except
    //     possibly in localised output to the user) in order to avoid any culture specific
    //     string comparison issues in certain languages (worst in the Thai language which
    //     has no spaces and treats punctuation marks as invisible)
    //     See also: https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings
    //
    //
    // Parameters:
    //   a:
    //
    //   b:
    public static int CompareOrdinal(this string a, string b)
    {
        return string.CompareOrdinal(a, b);
    }

    //
    // Summary:
    //     Convert the first character to an uppercase one
    //
    // Parameters:
    //   text:
    public static string UcFirst(this string text)
    {
        return text.Substring(0, 1).ToUpperInvariant() + text.Substring(1);
    }

    public static bool ToBool(this string text, bool defaultValue = false)
    {
        switch (text?.ToLowerInvariant())
        {
            case "true":
            case "yes":
            case "1":
                return true;
            case "false":
            case "no":
            case "0":
                return false;
            default:
                return defaultValue;
        }
    }

    public static string RemoveFileEnding(this string text)
    {
        return text.Substring(0, text.IndexOf('.'));
    }

    public static int ToInt(this string text, int defaultValue = 0)
    {
        if (!int.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
        {
            return defaultValue;
        }

        return result;
    }

    public static long ToLong(this string text, long defaultValue = 0L)
    {
        if (!long.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
        {
            return defaultValue;
        }

        return result;
    }

    public static float ToFloat(this string text, float defaultValue = 0f)
    {
        if (!float.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
        {
            return defaultValue;
        }

        return result;
    }

    public static double ToDouble(this string text, double defaultValue = 0.0)
    {
        if (!double.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
        {
            return defaultValue;
        }

        return result;
    }

    public static double? ToDoubleOrNull(this string text, double? defaultValue = 0.0)
    {
        if (!double.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
        {
            return defaultValue;
        }

        return result;
    }

    public static float? ToFloatOrNull(this string text, float? defaultValue = 0f)
    {
        if (!float.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var result))
        {
            return defaultValue;
        }

        return result;
    }

    public static int CountChars(this string text, char c)
    {
        int num = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == c)
            {
                num++;
            }
        }

        return num;
    }

    public static bool ContainsFast(this string value, string reference)
    {
        if (reference.Length > value.Length)
        {
            return false;
        }

        int num = 0;
        for (int i = 0; i < value.Length; i++)
        {
            num = ((value[i] == reference[num]) ? (num + 1) : 0);
            if (num >= reference.Length)
            {
                return true;
            }
        }

        return false;
    }

    public static bool ContainsFast(this string value, char reference)
    {
        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] == reference)
            {
                return true;
            }
        }

        return false;
    }

    public static bool StartsWithFast(this string value, string reference)
    {
        if (reference.Length > value.Length)
        {
            return false;
        }

        for (int num = reference.Length - 1; num >= 0; num--)
        {
            if (value[num] != reference[num])
            {
                return false;
            }
        }

        return true;
    }

    public static bool StartsWithFast(this string value, string reference, int offset)
    {
        if (reference.Length + offset > value.Length)
        {
            return false;
        }

        for (int num = reference.Length + offset - 1; num >= offset; num--)
        {
            if (value[num] != reference[num - offset])
            {
                return false;
            }
        }

        return true;
    }

    public static bool EqualsFast(this string value, string reference)
    {
        if (reference.Length != value.Length)
        {
            return false;
        }

        for (int num = reference.Length - 1; num >= 0; num--)
        {
            if (value[num] != reference[num])
            {
                return false;
            }
        }

        return true;
    }

    //
    // Summary:
    //     A fast case-insensitive string comparison for "ordinal" culture i.e. plain ASCII
    //     comparison used for internal strings such as asset paths
    //
    // Parameters:
    //   value:
    //
    //   reference:
    public static bool EqualsFastIgnoreCase(this string value, string reference)
    {
        if (reference.Length != value.Length)
        {
            return false;
        }

        for (int num = reference.Length - 1; num >= 0; num--)
        {
            char c;
            char c2;
            if ((c = value[num]) != (c2 = reference[num]) && ((c & 0xFFDF) != (c2 & 0xFFDF) || (c & 0xFFDF) < 65 || (c & 0xFFDF) > 90))
            {
                return false;
            }
        }

        return true;
    }

    public static bool FastStartsWith(string value, string reference, int len)
    {
        if (len > reference.Length)
        {
            throw new ArgumentException("reference must be longer than len");
        }

        if (len > value.Length)
        {
            return false;
        }

        for (int i = 0; i < len; i++)
        {
            if (value[i] != reference[i])
            {
                return false;
            }
        }

        return true;
    }

    //
    // Summary:
    //     Removes diacritics and replaces quotation marks, guillemets and brackets with
    //     a blank space. Used to create a search friendly term
    //
    // Parameters:
    //   stIn:
    public static string ToSearchFriendly(this string stIn)
    {
        string text = stIn.Normalize(NormalizationForm.FormD);
        StringBuilder stringBuilder = new StringBuilder();
        foreach (char c in text)
        {
            if (c == '«' || c == '»' || c == '"' || c == '(' || c == ')')
            {
                stringBuilder.Append(' ');
            }
            else if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
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
