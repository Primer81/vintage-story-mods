#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

//
// Summary:
//     The arguments from a client or sever command
public class CmdArgs
{
    private List<string> args = new List<string>();

    //
    // Summary:
    //     Returns the n-th arugment
    //
    // Parameters:
    //   index:
    public string this[int index]
    {
        get
        {
            return args[index];
        }
        set
        {
            args[index] = value;
        }
    }

    //
    // Summary:
    //     Amount of arguments passed
    public int Length => args.Count;

    //
    // Summary:
    //     Creates a new instance of the CmdArgs util with no arguments
    public CmdArgs()
    {
    }

    //
    // Summary:
    //     Creates a new instance of the CmdArgs util
    //
    // Parameters:
    //   joinedargs:
    public CmdArgs(string joinedargs)
    {
        string joinedargs2 = Regex.Replace(joinedargs.Trim(), "\\s+", " ");
        Push(joinedargs2);
    }

    public void Push(string joinedargs)
    {
        string[] collection = new string[0];
        if (joinedargs.Length > 0)
        {
            collection = joinedargs.Split(' ');
        }

        args.AddRange(collection);
    }

    //
    // Summary:
    //     Creates a new instance of the CmdArgs util
    //
    // Parameters:
    //   args:
    public CmdArgs(string[] args)
    {
        this.args = new List<string>(args);
    }

    //
    // Summary:
    //     Returns all remaining arguments as single merged string, concatenated with spaces
    public string PopAll()
    {
        string result = string.Join(" ", args.ToArray(), 0, args.Count);
        args.Clear();
        return result;
    }

    //
    // Summary:
    //     Returns the first char of the first argument
    //
    // Parameters:
    //   defaultValue:
    public char? PeekChar(char? defaultValue = null)
    {
        if (args.Count == 0)
        {
            return defaultValue;
        }

        if (args[0].Length == 0)
        {
            return '\0';
        }

        return args[0][0];
    }

    //
    // Summary:
    //     Remove the first character from the first argument and returns it
    //
    // Parameters:
    //   defaultValue:
    public char? PopChar(char? defaultValue = null)
    {
        if (args.Count == 0)
        {
            return defaultValue;
        }

        string text = args[0];
        args[0] = args[0].Substring(1);
        return text[0];
    }

    //
    // Summary:
    //     Removes the first argument and returns it, scans until it encounters a white
    //     space
    public string PopWord(string defaultValue = null)
    {
        if (args.Count == 0)
        {
            return defaultValue;
        }

        string result = args[0];
        args.RemoveAt(0);
        return result;
    }

    //
    // Summary:
    //     Removes the first argument and returns it
    public string PeekWord(string defaultValue = null)
    {
        if (args.Count == 0)
        {
            return defaultValue;
        }

        return args[0];
    }

    public string PopUntil(char endChar)
    {
        StringBuilder stringBuilder = new StringBuilder();
        string text = PopAll();
        for (int i = 0; i < text.Length && text[i] != endChar; i++)
        {
            stringBuilder.Append(text[i]);
        }

        Push(text.Substring(stringBuilder.Length).TrimStart());
        return stringBuilder.ToString();
    }

    public string PopCodeBlock(char blockOpenChar, char blockCloseChar, out string parseErrorMsg)
    {
        parseErrorMsg = null;
        string text = PopAll();
        StringBuilder stringBuilder = new StringBuilder();
        int num = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == blockOpenChar)
            {
                num++;
            }

            if (num == 0)
            {
                ReadOnlySpan<char> readOnlySpan = "First character is not ";
                char reference = blockOpenChar;
                parseErrorMsg = Lang.Get(string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference), ". Please consume all input until the block open char"));
                return null;
            }

            if (num > 0)
            {
                stringBuilder.Append(text[i]);
            }

            if (text[i] == blockCloseChar)
            {
                num--;
                if (num <= 0)
                {
                    break;
                }
            }
        }

        if (num > 0)
        {
            ReadOnlySpan<char> readOnlySpan2 = "Incomplete block. At least one ";
            char reference = blockCloseChar;
            parseErrorMsg = Lang.Get(string.Concat(readOnlySpan2, new ReadOnlySpan<char>(in reference), " is missing"));
            return null;
        }

        Push(text.Substring(stringBuilder.Length).TrimStart());
        return stringBuilder.ToString();
    }

    //
    // Summary:
    //     Adds an arg to the beginning
    //
    // Parameters:
    //   arg:
    public void PushSingle(string arg)
    {
        args.Insert(0, arg);
    }

    //
    // Summary:
    //     Adds an arg to the end
    //
    // Parameters:
    //   arg:
    public void AppendSingle(string arg)
    {
        args.Add(arg);
    }

    //
    // Summary:
    //     Tries to retrieve arg at given index as enum value or default if not enough arguments
    //     or not part of the enum
    //
    // Parameters:
    //   defaultValue:
    //
    // Type parameters:
    //   T:
    public T PopEnum<T>(T defaultValue = default(T))
    {
        string text = PopWord();
        if (text == null)
        {
            return defaultValue;
        }

        if (int.TryParse(text, out var result) && Enum.IsDefined(typeof(T), result))
        {
            return (T)Enum.ToObject(typeof(T), result);
        }

        return default(T);
    }

    //
    // Summary:
    //     Tries to retrieve arg at given index as int, or null if not enough arguments
    //     or not an integer
    //
    // Parameters:
    //   defaultValue:
    public int? PopInt(int? defaultValue = null)
    {
        string text = PopWord();
        if (text == null)
        {
            return defaultValue;
        }

        if (TryParseIntFancy(text, out var val))
        {
            return val;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Tries to parse a string to an integer, returns 0 if it fails
    //     Enhancements over system int.TryParse(): (1) the thousands separator (comma)
    //     is ignored (2) hex numbers are possible with 0x prefix (3) uint values over int.MaxValue
    //     are possible (may convert to a negative int, but that can be converted back to
    //     the original uint), more generally any valid long value is permitted
    //     All three enhancements are useful if we ever need, in a command, to set - for
    //     example - a color value (32 bits) from hex or unsigned decimal
    //
    // Parameters:
    //   arg:
    //
    //   val:
    private bool TryParseIntFancy(string arg, out int val)
    {
        arg = arg.Replace(",", "");
        if (arg.StartsWith("0x") && int.TryParse(arg.Substring(2), NumberStyles.HexNumber, GlobalConstants.DefaultCultureInfo, out val))
        {
            return true;
        }

        if (long.TryParse(arg, out var result))
        {
            val = (int)result;
            return true;
        }

        val = 0;
        return false;
    }

    //
    // Summary:
    //     Tries to retrieve arg at given index as long, or null if not enough arguments
    //     or not a long
    //
    // Parameters:
    //   defaultValue:
    public long? PopLong(long? defaultValue = null)
    {
        string text = PopWord();
        if (text == null)
        {
            return defaultValue;
        }

        if (long.TryParse(text, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    //
    // Summary:
    //     Tries to retrieve arg at given index as boolean, or null if not enough arguments
    //     or not an integer
    //     'true', 'yes' and '1' will all be interpreted as true. Parameter trueAlias (with
    //     default value 'on') allows one additional word to be used to signify true. Anything
    //     else will return false.
    //
    // Parameters:
    //   defaultValue:
    //
    //   trueAlias:
    public bool? PopBool(bool? defaultValue = null, string trueAlias = "on")
    {
        string text = PopWord()?.ToLowerInvariant();
        if (text == null)
        {
            return defaultValue;
        }

        int value;
        switch (text)
        {
            default:
                value = ((text == trueAlias) ? 1 : 0);
                break;
            case "1":
            case "yes":
            case "true":
            case "on":
                value = 1;
                break;
        }

        return (byte)value != 0;
    }

    //
    // Summary:
    //     Tries to retrieve arg at given index as int, or null if not enough arguments
    //     or not an integer
    //
    // Parameters:
    //   defaultValue:
    public double? PopDouble(double? defaultValue = null)
    {
        string text = PopWord();
        if (text == null)
        {
            return defaultValue;
        }

        return text.ToDoubleOrNull(defaultValue);
    }

    //
    // Summary:
    //     Tries to retrieve arg at given index as float, or null if not enough arguments
    //     or not a float
    //
    // Parameters:
    //   defaultValue:
    public float? PopFloat(float? defaultValue = null)
    {
        string text = PopWord();
        if (text == null)
        {
            return defaultValue;
        }

        return text.ToFloatOrNull(defaultValue);
    }

    //
    // Summary:
    //     Tries to retrieve 3 int coordinates from the next 3 arguments
    //
    // Parameters:
    //   defaultValue:
    public Vec3i PopVec3i(Vec3i defaultValue = null)
    {
        int? num = PopInt(defaultValue?.X);
        int? num2 = PopInt(defaultValue?.Y);
        int? num3 = PopInt(defaultValue?.Z);
        if (!num.HasValue || !num2.HasValue || !num3.HasValue)
        {
            return defaultValue;
        }

        return new Vec3i(num.Value, num2.Value, num3.Value);
    }

    public Vec3d PopVec3d(Vec3d defaultValue = null)
    {
        double? num = PopDouble(defaultValue?.X);
        double? num2 = PopDouble(defaultValue?.Y);
        double? num3 = PopDouble(defaultValue?.Z);
        if (!num.HasValue || !num2.HasValue || !num3.HasValue)
        {
            return defaultValue;
        }

        return new Vec3d(num.Value, num2.Value, num3.Value);
    }

    //
    // Summary:
    //     Retrieves a player position with following syntax: [coord] [coord] [coord] whereas
    //     [coord] may be ~[decimal] or =[decimal] or [decimal] ~ denotes a position relative
    //     to the player = denotes an absolute position no prefix denots a position relative
    //     to the map middle
    //
    // Parameters:
    //   playerPos:
    //
    //   mapMiddle:
    public Vec3d PopFlexiblePos(Vec3d playerPos, Vec3d mapMiddle)
    {
        if (args.Count < 3)
        {
            return null;
        }

        Vec3d vec3d = new Vec3d();
        for (int i = 0; i < 3; i++)
        {
            switch (PeekChar().Value)
            {
                case '~':
                    {
                        PopChar();
                        double? num;
                        if (PeekChar() != '\0')
                        {
                            num = PopDouble();
                            if (!num.HasValue)
                            {
                                return null;
                            }
                        }
                        else
                        {
                            num = 0.0;
                            PopWord();
                        }

                        vec3d[i] = num.Value + playerPos[i];
                        break;
                    }
                case '=':
                    {
                        PopChar();
                        double? num = PopDouble();
                        if (!num.HasValue)
                        {
                            return null;
                        }

                        vec3d[i] = num.Value;
                        break;
                    }
                default:
                    {
                        double? num = PopDouble();
                        if (!num.HasValue)
                        {
                            return null;
                        }

                        vec3d[i] = num.Value + mapMiddle[i];
                        break;
                    }
            }
        }

        return vec3d;
    }

    //
    // Summary:
    //     Retrieves a player position with following syntax: [coord] [coord] [coord] whereas
    //     [coord] may be ~[decimal] or =[decimal] or [decimal] ~ denotes a position relative
    //     to the player = denotes an absolute position no prefix denots a position relative
    //     to the map middle
    //
    // Parameters:
    //   playerPos:
    //
    //   mapMiddle:
    public Vec2i PopFlexiblePos2D(Vec3d playerPos, Vec3d mapMiddle)
    {
        if (args.Count < 2)
        {
            return null;
        }

        Vec2i vec2i = new Vec2i();
        for (int i = 0; i < 2; i++)
        {
            double? num;
            switch (PeekChar().Value)
            {
                case '~':
                    PopChar();
                    if (PeekChar() != '\0')
                    {
                        num = PopDouble();
                        if (!num.HasValue)
                        {
                            return null;
                        }
                    }
                    else
                    {
                        num = 0.0;
                        PopWord();
                    }

                    vec2i[i] = (int)(num.Value + playerPos[i * 2]);
                    continue;
                case '=':
                    PopChar();
                    num = PopDouble();
                    if (!num.HasValue)
                    {
                        return null;
                    }

                    vec2i[i] = (int)num.Value;
                    continue;
                case '+':
                    PopChar();
                    break;
            }

            num = PopDouble();
            if (!num.HasValue)
            {
                return null;
            }

            vec2i[i] = (int)(num.Value + mapMiddle[i * 2]);
        }

        return vec2i;
    }

    public CmdArgs Clone()
    {
        return new CmdArgs(args.ToArray());
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
