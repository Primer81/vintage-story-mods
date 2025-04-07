#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

//
// Summary:
//     Special class to handle the vertex flagging in a very nicely compressed space.
//
//     Bit 0-7: Glow level
//     Bit 8-10: Z-Offset
//     Bit 11: Reflective bit
//     Bit 12: Lod 0 Bit
//     Bit 13-24: X/Y/Z Normals
//     Bit 25, 26, 27, 28: Wind mode
//     Bit 29, 30, 31: Wind data (also sometimes used for other data, e.g. reflection
//     mode if Reflective bit is set, or additional water surface data if this is a
//     water block)
[DocumentAsJson]
[JsonObject(MemberSerialization.OptIn)]
public class VertexFlags
{
    //
    // Summary:
    //     Bit 0..7
    public const int GlowLevelBitMask = 255;

    public const int ZOffsetBitPos = 8;

    //
    // Summary:
    //     Bit 8..10
    public const int ZOffsetBitMask = 1792;

    //
    // Summary:
    //     Bit 11. Note if this is set to 1, then WindData has a different meaning,
    public const int ReflectiveBitMask = 2048;

    //
    // Summary:
    //     Bit 12
    public const int Lod0BitMask = 4096;

    public const int NormalBitPos = 13;

    //
    // Summary:
    //     Bit 13..24
    public const int NormalBitMask = 33546240;

    //
    // Summary:
    //     Bit 25..28
    public const int WindModeBitsMask = 503316480;

    public const int WindModeBitsPos = 25;

    //
    // Summary:
    //     Bit 29..31 Note that WindData is sometimes used for other purposes if WindMode
    //     == 0, for example it can hold reflections data, see EnumReflectiveMode.
    //     Also worth noting that WindMode and WindData have totally different meanings
    //     for liquid water
    public const int WindDataBitsMask = -536870912;

    public const int WindDataBitsPos = 29;

    //
    // Summary:
    //     Bit 26..31
    public const int WindBitsMask = -33554432;

    public const int LiquidIsLavaBitMask = 33554432;

    public const int LiquidWeakFoamBitMask = 67108864;

    public const int LiquidWeakWaveBitMask = 134217728;

    public const int LiquidFullAlphaBitMask = 268435456;

    public const int LiquidExposedToSkyBitMask = 536870912;

    public const int ClearWindBitsMask = 33554431;

    public const int ClearWindModeBitsMask = -503316481;

    public const int ClearWindDataBitsMask = 536870911;

    public const int ClearZOffsetMask = -1793;

    public const int ClearNormalBitMask = -33546241;

    private int all;

    private byte glowLevel;

    private byte zOffset;

    private bool reflective;

    private bool lod0;

    private short normal;

    private EnumWindBitMode windMode;

    private byte windData;

    private const int nValueBitMask = 14;

    private const int nXValueBitMask = 114688;

    private const int nYValueBitMask = 1835008;

    private const int nZValueBitMask = 29360128;

    private const int nXSignBitPos = 12;

    private const int nYSignBitPos = 16;

    private const int nZSignBitPos = 20;

    //
    // Summary:
    //     Sets all the vertex flags from one integer.
    [JsonProperty]
    public int All
    {
        get
        {
            return all;
        }
        set
        {
            glowLevel = (byte)((uint)value & 0xFFu);
            zOffset = (byte)((uint)(value >> 8) & 7u);
            reflective = ((value >> 11) & 1) != 0;
            lod0 = ((value >> 12) & 1) != 0;
            normal = (short)((value >> 13) & 0xFFF);
            windMode = (EnumWindBitMode)((value >> 25) & 0xF);
            windData = (byte)((uint)(value >> 29) & 7u);
            all = value;
        }
    }

    [JsonProperty]
    public byte GlowLevel
    {
        get
        {
            return glowLevel;
        }
        set
        {
            glowLevel = value;
            UpdateAll();
        }
    }

    [JsonProperty]
    public byte ZOffset
    {
        get
        {
            return zOffset;
        }
        set
        {
            zOffset = value;
            UpdateAll();
        }
    }

    [JsonProperty]
    public bool Reflective
    {
        get
        {
            return reflective;
        }
        set
        {
            reflective = value;
            UpdateAll();
        }
    }

    [JsonProperty]
    public bool Lod0
    {
        get
        {
            return lod0;
        }
        set
        {
            lod0 = value;
            UpdateAll();
        }
    }

    [JsonProperty]
    public short Normal
    {
        get
        {
            return normal;
        }
        set
        {
            normal = value;
            UpdateAll();
        }
    }

    [JsonProperty]
    public EnumWindBitMode WindMode
    {
        get
        {
            return windMode;
        }
        set
        {
            windMode = value;
            UpdateAll();
        }
    }

    [JsonProperty]
    public byte WindData
    {
        get
        {
            return windData;
        }
        set
        {
            windData = value;
            UpdateAll();
        }
    }

    //
    // Summary:
    //     Creates an already bit shifted normal
    //
    // Parameters:
    //   normal:
    public static int PackNormal(Vec3d normal)
    {
        return PackNormal(normal.X, normal.Y, normal.Z);
    }

    //
    // Summary:
    //     Creates an already bit shifted normal
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public static int PackNormal(double x, double y, double z)
    {
        int num = (int)(x * 7.000001) * 2;
        int num2 = (int)(y * 7.000001) * 2;
        int num3 = (int)(z * 7.000001) * 2;
        return (((num < 0) ? (1 - num) : num) << 13) | (((num2 < 0) ? (1 - num2) : num2) << 17) | (((num3 < 0) ? (1 - num3) : num3) << 21);
    }

    //
    // Summary:
    //     Creates an already bit shifted normal
    //
    // Parameters:
    //   normal:
    public static int PackNormal(Vec3f normal)
    {
        int num = (int)(normal.X * 7.000001f) * 2;
        int num2 = (int)(normal.Y * 7.000001f) * 2;
        int num3 = (int)(normal.Z * 7.000001f) * 2;
        return (((num < 0) ? (1 - num) : num) << 13) | (((num2 < 0) ? (1 - num2) : num2) << 17) | (((num3 < 0) ? (1 - num3) : num3) << 21);
    }

    //
    // Summary:
    //     Creates an already bit shifted normal
    //
    // Parameters:
    //   normal:
    public static int PackNormal(Vec3i normal)
    {
        int num = (int)((float)normal.X * 7.000001f) * 2;
        int num2 = (int)((float)normal.Y * 7.000001f) * 2;
        int num3 = (int)((float)normal.Z * 7.000001f) * 2;
        return (((num < 0) ? (1 - num) : num) << 13) | (((num2 < 0) ? (1 - num2) : num2) << 17) | (((num3 < 0) ? (1 - num3) : num3) << 21);
    }

    public static void UnpackNormal(int vertexFlags, float[] intoFloats)
    {
        int num = vertexFlags & 0x1C000;
        int num2 = vertexFlags & 0x1C0000;
        int num3 = vertexFlags & 0x1C00000;
        int num4 = 1 - ((vertexFlags >> 12) & 2);
        int num5 = 1 - ((vertexFlags >> 16) & 2);
        int num6 = 1 - ((vertexFlags >> 20) & 2);
        intoFloats[0] = (float)(num4 * num) / 114688f;
        intoFloats[1] = (float)(num5 * num2) / 1835008f;
        intoFloats[2] = (float)(num6 * num3) / 29360128f;
    }

    public static void UnpackNormal(int vertexFlags, double[] intoDouble)
    {
        int num = vertexFlags & 0x1C000;
        int num2 = vertexFlags & 0x1C0000;
        int num3 = vertexFlags & 0x1C00000;
        int num4 = 1 - ((vertexFlags >> 12) & 2);
        int num5 = 1 - ((vertexFlags >> 16) & 2);
        int num6 = 1 - ((vertexFlags >> 20) & 2);
        intoDouble[0] = (float)(num4 * num) / 114688f;
        intoDouble[1] = (float)(num5 * num2) / 1835008f;
        intoDouble[2] = (float)(num6 * num3) / 29360128f;
    }

    public VertexFlags()
    {
    }

    public VertexFlags(int flags)
    {
        All = flags;
    }

    private void UpdateAll()
    {
        all = glowLevel | ((zOffset & 7) << 8) | (int)((reflective ? 1u : 0u) << 11) | (int)((Lod0 ? 1u : 0u) << 12) | ((normal & 0xFFF) << 13) | ((int)(windMode & (EnumWindBitMode)15) << 25) | ((windData & 7) << 29);
    }

    //
    // Summary:
    //     Clones this set of vertex flags.
    public VertexFlags Clone()
    {
        return new VertexFlags(All);
    }

    public override string ToString()
    {
        return $"Glow: {glowLevel}, ZOffset: {ZOffset}, Reflective: {reflective}, Lod0: {lod0}, Normal: {normal}, WindMode: {WindMode}, WindData: {windData}";
    }

    public static void SetWindMode(ref int flags, int windMode)
    {
        flags |= windMode << 25;
    }

    public static void SetWindData(ref int flags, int windData)
    {
        flags |= windData << 29;
    }

    public static void ReplaceWindData(ref int flags, int windData)
    {
        flags = (flags & 0x1FFFFFFF) | (windData << 29);
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
