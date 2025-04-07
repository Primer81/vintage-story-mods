#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.MathTools;

//
// Summary:
//     It's generally pretty hard to get a neatly normalized coherent noise function
//     due to the way perlin/open simplex works (gauss curve) and how random numbers
//     are generated. So instead of trying to find the perfect normalization factor
//     and instead try to perform some approximate normalization this class allows a
//     small overflow and brings it down very close to the [0, 1] range using tanh().
//     Returns values in a range of [0..1]
public class NormalizedSimplexNoise
{
    public struct ColumnNoise
    {
        private struct OctaveEntry
        {
            public SimplexNoiseOctave Octave;

            public double X;

            public double FrequencyY;

            public double Z;

            public double Amplitude;

            public double Threshold;

            public double StopBound;
        }

        private OctaveEntry[] orderedOctaveEntries;

        public double UncurvedBound { get; private set; }

        public double BoundMin { get; private set; }

        public double BoundMax { get; private set; }

        public ColumnNoise(NormalizedSimplexNoise terrainNoise, double relativeYFrequency, double[] amplitudes, double[] thresholds, double noiseX, double noiseZ)
        {
            int num = terrainNoise.frequencies.Length;
            int num2 = 0;
            double[] array = new double[num];
            int[] array2 = new int[num];
            double num3 = 0.0;
            for (int num4 = num - 1; num4 >= 0; num4--)
            {
                array[num4] = Math.Max(0.0, Math.Abs(amplitudes[num4]) - thresholds[num4]) * 1.1845758506756423;
                num3 += array[num4];
                if (array[num4] != 0.0)
                {
                    array2[num2] = num4;
                    for (int num5 = num2 - 1; num5 >= 0; num5--)
                    {
                        if (array[array2[num5 + 1]] > array[array2[num5]])
                        {
                            int num6 = array2[num5];
                            array2[num5] = array2[num5 + 1];
                            array2[num5 + 1] = num6;
                        }
                    }

                    num2++;
                }
            }

            UncurvedBound = num3;
            BoundMin = NoiseValueCurve(0.0 - num3);
            BoundMax = NoiseValueCurve(num3);
            orderedOctaveEntries = new OctaveEntry[num2];
            double num7 = 0.0;
            for (int num8 = num2 - 1; num8 >= 0; num8--)
            {
                int num9 = array2[num8];
                num7 += array[num9];
                double num10 = terrainNoise.frequencies[num9];
                orderedOctaveEntries[num8] = new OctaveEntry
                {
                    Octave = terrainNoise.octaves[num9],
                    X = noiseX * num10,
                    Z = noiseZ * num10,
                    FrequencyY = num10 * relativeYFrequency,
                    Amplitude = amplitudes[num9] * 1.2,
                    Threshold = thresholds[num9] * 1.2,
                    StopBound = num7
                };
            }
        }

        public double NoiseSign(double y, double inverseCurvedThresholder)
        {
            double num = inverseCurvedThresholder;
            for (int i = 0; i < orderedOctaveEntries.Length; i++)
            {
                ref OctaveEntry reference = ref orderedOctaveEntries[i];
                if (num >= reference.StopBound || num <= 0.0 - reference.StopBound)
                {
                    break;
                }

                double value = reference.Octave.Evaluate(reference.X, y * reference.FrequencyY, reference.Z) * reference.Amplitude;
                num += ApplyThresholding(value, reference.Threshold);
            }

            return num;
        }

        public double Noise(double y)
        {
            double num = 0.0;
            for (int i = 0; i < orderedOctaveEntries.Length; i++)
            {
                ref OctaveEntry reference = ref orderedOctaveEntries[i];
                double value = reference.Octave.Evaluate(reference.X, y * reference.FrequencyY, reference.Z) * reference.Amplitude;
                num += ApplyThresholding(value, reference.Threshold);
            }

            return NoiseValueCurve(num);
        }
    }

    private const double VALUE_MULTIPLIER = 1.2;

    public double[] scaledAmplitudes2D;

    public double[] scaledAmplitudes3D;

    public double[] inputAmplitudes;

    public double[] frequencies;

    public SimplexNoiseOctave[] octaves;

    public NormalizedSimplexNoise(double[] inputAmplitudes, double[] frequencies, long seed)
    {
        this.frequencies = frequencies;
        this.inputAmplitudes = inputAmplitudes;
        octaves = new SimplexNoiseOctave[inputAmplitudes.Length];
        for (int i = 0; i < octaves.Length; i++)
        {
            octaves[i] = new SimplexNoiseOctave(seed * 65599 + i);
        }

        CalculateAmplitudes(inputAmplitudes);
    }

    //
    // Summary:
    //     Generates the octaves and frequencies using following formulas freq[i] = baseFrequency
    //     * 2^i amp[i] = persistence^i
    //
    // Parameters:
    //   quantityOctaves:
    //
    //   baseFrequency:
    //
    //   persistence:
    //
    //   seed:
    public static NormalizedSimplexNoise FromDefaultOctaves(int quantityOctaves, double baseFrequency, double persistence, long seed)
    {
        double[] array = new double[quantityOctaves];
        double[] array2 = new double[quantityOctaves];
        for (int i = 0; i < quantityOctaves; i++)
        {
            array[i] = Math.Pow(2.0, i) * baseFrequency;
            array2[i] = Math.Pow(persistence, i);
        }

        return new NormalizedSimplexNoise(array2, array, seed);
    }

    internal virtual void CalculateAmplitudes(double[] inputAmplitudes)
    {
        double num = 0.0;
        double num2 = 0.0;
        for (int i = 0; i < inputAmplitudes.Length; i++)
        {
            num += inputAmplitudes[i] * Math.Pow(0.64, i + 1);
            num2 += inputAmplitudes[i] * Math.Pow(0.73, i + 1);
        }

        scaledAmplitudes2D = new double[inputAmplitudes.Length];
        for (int j = 0; j < inputAmplitudes.Length; j++)
        {
            scaledAmplitudes2D[j] = inputAmplitudes[j] / num2;
        }

        scaledAmplitudes3D = new double[inputAmplitudes.Length];
        for (int k = 0; k < inputAmplitudes.Length; k++)
        {
            scaledAmplitudes3D[k] = inputAmplitudes[k] / num;
        }
    }

    //
    // Summary:
    //     2d noise
    //
    // Parameters:
    //   x:
    //
    //   y:
    public virtual double Noise(double x, double y)
    {
        double num = 0.0;
        for (int i = 0; i < scaledAmplitudes2D.Length; i++)
        {
            num += 1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * scaledAmplitudes2D[i];
        }

        return NoiseValueCurve(num);
    }

    public double Noise(double x, double y, double[] thresholds)
    {
        double num = 0.0;
        for (int i = 0; i < scaledAmplitudes2D.Length; i++)
        {
            double num2 = octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * scaledAmplitudes2D[i];
            num += 1.2 * ((num2 > 0.0) ? Math.Max(0.0, num2 - thresholds[i]) : Math.Min(0.0, num2 + thresholds[i]));
        }

        return NoiseValueCurve(num);
    }

    //
    // Summary:
    //     3d noise
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public virtual double Noise(double x, double y, double z)
    {
        double num = 0.0;
        for (int i = 0; i < scaledAmplitudes3D.Length; i++)
        {
            num += 1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i]) * scaledAmplitudes3D[i];
        }

        return NoiseValueCurve(num);
    }

    //
    // Summary:
    //     3d Noise using custom amplitudes
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   amplitudes:
    public virtual double Noise(double x, double y, double z, double[] amplitudes)
    {
        double num = 0.0;
        for (int i = 0; i < scaledAmplitudes3D.Length; i++)
        {
            num += 1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i]) * amplitudes[i];
        }

        return NoiseValueCurve(num);
    }

    public double Noise(double x, double y, double z, double[] amplitudes, double[] thresholds)
    {
        double num = 0.0;
        for (int i = 0; i < scaledAmplitudes3D.Length; i++)
        {
            double num2 = frequencies[i];
            double value = octaves[i].Evaluate(x * num2, y * num2, z * num2) * amplitudes[i];
            num += 1.2 * ApplyThresholding(value, thresholds[i]);
        }

        return NoiseValueCurve(num);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NoiseValueCurve(double value)
    {
        return Math.Tanh(value) * 0.5 + 0.5;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NoiseValueCurveInverse(double value)
    {
        if (value <= 0.0)
        {
            return double.NegativeInfinity;
        }

        if (value >= 1.0)
        {
            return double.PositiveInfinity;
        }

        return 0.5 * Math.Log(value / (1.0 - value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ApplyThresholding(double value, double threshold)
    {
        if (!(value > 0.0))
        {
            return Math.Min(0.0, value + threshold);
        }

        return Math.Max(0.0, value - threshold);
    }

    public ColumnNoise ForColumn(double relativeYFrequency, double[] amplitudes, double[] thresholds, double noiseX, double noiseZ)
    {
        return new ColumnNoise(this, relativeYFrequency, amplitudes, thresholds, noiseX, noiseZ);
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
