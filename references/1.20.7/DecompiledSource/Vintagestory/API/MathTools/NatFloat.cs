#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.IO;

namespace Vintagestory.API.MathTools;

//
// Summary:
//     A more natural random number generator (nature usually doesn't grow by the exact
//     same numbers nor does it completely randomly)
[DocumentAsJson]
public class NatFloat
{
    //
    // Summary:
    //     A full offset to apply to any values returned.
    [DocumentAsJson]
    public float offset;

    //
    // Summary:
    //     The average value for the random float.
    [DocumentAsJson]
    public float avg;

    //
    // Summary:
    //     The variation for the random float.
    [DocumentAsJson]
    public float var;

    //
    // Summary:
    //     The type of distribution to use that determines the commodity of values.
    [DocumentAsJson]
    public EnumDistribution dist;

    [ThreadStatic]
    private static Random threadsafeRand;

    //
    // Summary:
    //     Always 0
    public static NatFloat Zero => new NatFloat(0f, 0f, EnumDistribution.UNIFORM);

    //
    // Summary:
    //     Always 1
    public static NatFloat One => new NatFloat(1f, 0f, EnumDistribution.UNIFORM);

    public NatFloat(float averagevalue, float variance, EnumDistribution distribution)
    {
        avg = averagevalue;
        var = variance;
        dist = distribution;
    }

    public static NatFloat createInvexp(float averagevalue, float variance)
    {
        return new NatFloat(averagevalue, variance, EnumDistribution.INVEXP);
    }

    public static NatFloat createStrongInvexp(float averagevalue, float variance)
    {
        return new NatFloat(averagevalue, variance, EnumDistribution.STRONGINVEXP);
    }

    public static NatFloat createStrongerInvexp(float averagevalue, float variance)
    {
        return new NatFloat(averagevalue, variance, EnumDistribution.STRONGERINVEXP);
    }

    public static NatFloat createUniform(float averagevalue, float variance)
    {
        return new NatFloat(averagevalue, variance, EnumDistribution.UNIFORM);
    }

    public static NatFloat createGauss(float averagevalue, float variance)
    {
        return new NatFloat(averagevalue, variance, EnumDistribution.GAUSSIAN);
    }

    public static NatFloat createNarrowGauss(float averagevalue, float variance)
    {
        return new NatFloat(averagevalue, variance, EnumDistribution.NARROWGAUSSIAN);
    }

    public static NatFloat createInvGauss(float averagevalue, float variance)
    {
        return new NatFloat(averagevalue, variance, EnumDistribution.INVERSEGAUSSIAN);
    }

    public static NatFloat createTri(float averagevalue, float variance)
    {
        return new NatFloat(averagevalue, variance, EnumDistribution.TRIANGLE);
    }

    public static NatFloat createDirac(float averagevalue, float variance)
    {
        return new NatFloat(averagevalue, variance, EnumDistribution.DIRAC);
    }

    public static NatFloat create(EnumDistribution distribution, float averagevalue, float variance)
    {
        return new NatFloat(averagevalue, variance, distribution);
    }

    public NatFloat copyWithOffset(float value)
    {
        NatFloat natFloat = new NatFloat(value, value, dist);
        natFloat.offset += value;
        return natFloat;
    }

    public NatFloat addOffset(float value)
    {
        offset += value;
        return this;
    }

    public NatFloat setOffset(float offset)
    {
        this.offset = offset;
        return this;
    }

    public float nextFloat()
    {
        return nextFloat(1f, threadsafeRand ?? (threadsafeRand = new Random()));
    }

    public float nextFloat(float multiplier)
    {
        return nextFloat(multiplier, threadsafeRand ?? (threadsafeRand = new Random()));
    }

    public float nextFloat(float multiplier, Random rand)
    {
        switch (dist)
        {
            case EnumDistribution.UNIFORM:
                {
                    float num = (float)rand.NextDouble() - 0.5f;
                    return offset + multiplier * (avg + num * 2f * var);
                }
            case EnumDistribution.GAUSSIAN:
                {
                    float num = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 3f;
                    num -= 0.5f;
                    return offset + multiplier * (avg + num * 2f * var);
                }
            case EnumDistribution.NARROWGAUSSIAN:
                {
                    float num = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 6f;
                    num -= 0.5f;
                    return offset + multiplier * (avg + num * 2f * var);
                }
            case EnumDistribution.VERYNARROWGAUSSIAN:
                {
                    float num = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 12f;
                    num -= 0.5f;
                    return offset + multiplier * (avg + num * 2f * var);
                }
            case EnumDistribution.INVEXP:
                {
                    float num = (float)(rand.NextDouble() * rand.NextDouble());
                    return offset + multiplier * (avg + num * var);
                }
            case EnumDistribution.STRONGINVEXP:
                {
                    float num = (float)(rand.NextDouble() * rand.NextDouble() * rand.NextDouble());
                    return offset + multiplier * (avg + num * var);
                }
            case EnumDistribution.STRONGERINVEXP:
                {
                    float num = (float)(rand.NextDouble() * rand.NextDouble() * rand.NextDouble() * rand.NextDouble());
                    return offset + multiplier * (avg + num * var);
                }
            case EnumDistribution.INVERSEGAUSSIAN:
                {
                    float num = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 3f;
                    num = ((!(num > 0.5f)) ? (num + 0.5f) : (num - 0.5f));
                    num -= 0.5f;
                    return offset + multiplier * (avg + 2f * num * var);
                }
            case EnumDistribution.NARROWINVERSEGAUSSIAN:
                {
                    float num = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 6f;
                    num = ((!(num > 0.5f)) ? (num + 0.5f) : (num - 0.5f));
                    num -= 0.5f;
                    return offset + multiplier * (avg + 2f * num * var);
                }
            case EnumDistribution.DIRAC:
                {
                    float num = (float)rand.NextDouble() - 0.5f;
                    float result = offset + multiplier * (avg + num * 2f * var);
                    avg = 0f;
                    var = 0f;
                    return result;
                }
            case EnumDistribution.TRIANGLE:
                {
                    float num = (float)(rand.NextDouble() + rand.NextDouble()) / 2f;
                    num -= 0.5f;
                    return offset + multiplier * (avg + num * 2f * var);
                }
            default:
                return 0f;
        }
    }

    public float nextFloat(float multiplier, IRandom rand)
    {
        switch (dist)
        {
            case EnumDistribution.UNIFORM:
                {
                    float num = rand.NextFloat() - 0.5f;
                    return offset + multiplier * (avg + num * 2f * var);
                }
            case EnumDistribution.GAUSSIAN:
                {
                    float num = (rand.NextFloat() + rand.NextFloat() + rand.NextFloat()) / 3f;
                    num -= 0.5f;
                    return offset + multiplier * (avg + num * 2f * var);
                }
            case EnumDistribution.NARROWGAUSSIAN:
                {
                    float num = (rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat()) / 6f;
                    num -= 0.5f;
                    return offset + multiplier * (avg + num * 2f * var);
                }
            case EnumDistribution.INVEXP:
                {
                    float num = rand.NextFloat() * rand.NextFloat();
                    return offset + multiplier * (avg + num * var);
                }
            case EnumDistribution.STRONGINVEXP:
                {
                    float num = rand.NextFloat() * rand.NextFloat() * rand.NextFloat();
                    return offset + multiplier * (avg + num * var);
                }
            case EnumDistribution.STRONGERINVEXP:
                {
                    float num = rand.NextFloat() * rand.NextFloat() * rand.NextFloat() * rand.NextFloat();
                    return offset + multiplier * (avg + num * var);
                }
            case EnumDistribution.INVERSEGAUSSIAN:
                {
                    float num = (rand.NextFloat() + rand.NextFloat() + rand.NextFloat()) / 3f;
                    num = ((!(num > 0.5f)) ? (num + 0.5f) : (num - 0.5f));
                    num -= 0.5f;
                    return offset + multiplier * (avg + 2f * num * var);
                }
            case EnumDistribution.NARROWINVERSEGAUSSIAN:
                {
                    float num = (rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat()) / 6f;
                    num = ((!(num > 0.5f)) ? (num + 0.5f) : (num - 0.5f));
                    num -= 0.5f;
                    return offset + multiplier * (avg + 2f * num * var);
                }
            case EnumDistribution.DIRAC:
                {
                    float num = rand.NextFloat() - 0.5f;
                    float result = offset + multiplier * (avg + num * 2f * var);
                    avg = 0f;
                    var = 0f;
                    return result;
                }
            case EnumDistribution.TRIANGLE:
                {
                    float num = (rand.NextFloat() + rand.NextFloat()) / 2f;
                    num -= 0.5f;
                    return offset + multiplier * (avg + num * 2f * var);
                }
            default:
                return 0f;
        }
    }

    //
    // Summary:
    //     Clamps supplied value to avg-var and avg+var
    //
    // Parameters:
    //   value:
    public float ClampToRange(float value)
    {
        EnumDistribution enumDistribution = dist;
        if ((uint)(enumDistribution - 6) <= 2u)
        {
            return Math.Min(value, value + var);
        }

        float val = avg - var;
        float val2 = avg + var;
        return GameMath.Clamp(value, Math.Min(val, val2), Math.Max(val, val2));
    }

    public static NatFloat createFromBytes(BinaryReader reader)
    {
        NatFloat zero = Zero;
        zero.FromBytes(reader);
        return zero;
    }

    public NatFloat Clone()
    {
        return (NatFloat)MemberwiseClone();
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write(offset);
        writer.Write(avg);
        writer.Write(var);
        writer.Write((byte)dist);
    }

    public void FromBytes(BinaryReader reader)
    {
        offset = reader.ReadSingle();
        avg = reader.ReadSingle();
        var = reader.ReadSingle();
        dist = (EnumDistribution)reader.ReadByte();
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
