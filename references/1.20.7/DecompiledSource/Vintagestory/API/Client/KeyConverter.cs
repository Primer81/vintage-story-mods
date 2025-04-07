#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

namespace Vintagestory.API.Client;

//
// Summary:
//     Converts key code from OpenTK 4 to GlKeys
public static class KeyConverter
{
    public static readonly int[] NewKeysToGlKeys;

    public static readonly int[] GlKeysToNew;

    static KeyConverter()
    {
        NewKeysToGlKeys = new int[349];
        GlKeysToNew = new int[131];
        NewKeysToGlKeys[32] = 51;
        NewKeysToGlKeys[39] = 125;
        NewKeysToGlKeys[44] = 126;
        NewKeysToGlKeys[45] = 120;
        NewKeysToGlKeys[46] = 127;
        NewKeysToGlKeys[47] = 128;
        NewKeysToGlKeys[48] = 109;
        NewKeysToGlKeys[49] = 110;
        NewKeysToGlKeys[50] = 111;
        NewKeysToGlKeys[51] = 112;
        NewKeysToGlKeys[52] = 113;
        NewKeysToGlKeys[53] = 114;
        NewKeysToGlKeys[54] = 115;
        NewKeysToGlKeys[55] = 116;
        NewKeysToGlKeys[56] = 117;
        NewKeysToGlKeys[57] = 118;
        NewKeysToGlKeys[59] = 124;
        NewKeysToGlKeys[61] = 121;
        NewKeysToGlKeys[65] = 83;
        NewKeysToGlKeys[66] = 84;
        NewKeysToGlKeys[67] = 85;
        NewKeysToGlKeys[68] = 86;
        NewKeysToGlKeys[69] = 87;
        NewKeysToGlKeys[70] = 88;
        NewKeysToGlKeys[71] = 89;
        NewKeysToGlKeys[72] = 90;
        NewKeysToGlKeys[73] = 91;
        NewKeysToGlKeys[74] = 92;
        NewKeysToGlKeys[75] = 93;
        NewKeysToGlKeys[76] = 94;
        NewKeysToGlKeys[77] = 95;
        NewKeysToGlKeys[78] = 96;
        NewKeysToGlKeys[79] = 97;
        NewKeysToGlKeys[80] = 98;
        NewKeysToGlKeys[81] = 99;
        NewKeysToGlKeys[82] = 100;
        NewKeysToGlKeys[83] = 101;
        NewKeysToGlKeys[84] = 102;
        NewKeysToGlKeys[85] = 103;
        NewKeysToGlKeys[86] = 104;
        NewKeysToGlKeys[87] = 105;
        NewKeysToGlKeys[88] = 106;
        NewKeysToGlKeys[89] = 107;
        NewKeysToGlKeys[90] = 108;
        NewKeysToGlKeys[91] = 122;
        NewKeysToGlKeys[92] = 129;
        NewKeysToGlKeys[93] = 123;
        NewKeysToGlKeys[96] = 119;
        NewKeysToGlKeys[256] = 50;
        NewKeysToGlKeys[257] = 49;
        NewKeysToGlKeys[258] = 52;
        NewKeysToGlKeys[259] = 53;
        NewKeysToGlKeys[260] = 54;
        NewKeysToGlKeys[261] = 55;
        NewKeysToGlKeys[262] = 48;
        NewKeysToGlKeys[263] = 47;
        NewKeysToGlKeys[264] = 46;
        NewKeysToGlKeys[265] = 45;
        NewKeysToGlKeys[266] = 56;
        NewKeysToGlKeys[267] = 57;
        NewKeysToGlKeys[268] = 58;
        NewKeysToGlKeys[269] = 59;
        NewKeysToGlKeys[280] = 60;
        NewKeysToGlKeys[281] = 61;
        NewKeysToGlKeys[282] = 64;
        NewKeysToGlKeys[283] = 62;
        NewKeysToGlKeys[284] = 63;
        NewKeysToGlKeys[290] = 10;
        NewKeysToGlKeys[291] = 11;
        NewKeysToGlKeys[292] = 12;
        NewKeysToGlKeys[293] = 13;
        NewKeysToGlKeys[294] = 14;
        NewKeysToGlKeys[295] = 15;
        NewKeysToGlKeys[296] = 16;
        NewKeysToGlKeys[297] = 17;
        NewKeysToGlKeys[298] = 18;
        NewKeysToGlKeys[299] = 19;
        NewKeysToGlKeys[300] = 20;
        NewKeysToGlKeys[301] = 21;
        NewKeysToGlKeys[302] = 22;
        NewKeysToGlKeys[303] = 23;
        NewKeysToGlKeys[304] = 24;
        NewKeysToGlKeys[305] = 25;
        NewKeysToGlKeys[306] = 26;
        NewKeysToGlKeys[307] = 27;
        NewKeysToGlKeys[308] = 28;
        NewKeysToGlKeys[309] = 29;
        NewKeysToGlKeys[310] = 30;
        NewKeysToGlKeys[311] = 31;
        NewKeysToGlKeys[312] = 32;
        NewKeysToGlKeys[313] = 33;
        NewKeysToGlKeys[314] = 34;
        NewKeysToGlKeys[320] = 67;
        NewKeysToGlKeys[321] = 68;
        NewKeysToGlKeys[322] = 69;
        NewKeysToGlKeys[323] = 70;
        NewKeysToGlKeys[324] = 71;
        NewKeysToGlKeys[325] = 72;
        NewKeysToGlKeys[326] = 73;
        NewKeysToGlKeys[327] = 74;
        NewKeysToGlKeys[328] = 75;
        NewKeysToGlKeys[329] = 76;
        NewKeysToGlKeys[330] = 81;
        NewKeysToGlKeys[331] = 77;
        NewKeysToGlKeys[332] = 78;
        NewKeysToGlKeys[333] = 79;
        NewKeysToGlKeys[334] = 80;
        NewKeysToGlKeys[335] = 82;
        NewKeysToGlKeys[340] = 1;
        NewKeysToGlKeys[341] = 3;
        NewKeysToGlKeys[342] = 5;
        NewKeysToGlKeys[343] = 7;
        NewKeysToGlKeys[344] = 2;
        NewKeysToGlKeys[345] = 4;
        NewKeysToGlKeys[346] = 6;
        NewKeysToGlKeys[347] = 8;
        NewKeysToGlKeys[348] = 9;
        for (int i = 0; i < GlKeysToNew.Length; i++)
        {
            GlKeysToNew[i] = -1;
        }

        for (int j = 0; j < NewKeysToGlKeys.Length; j++)
        {
            if (NewKeysToGlKeys[j] != 0)
            {
                GlKeysToNew[NewKeysToGlKeys[j]] = j;
            }
        }
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
