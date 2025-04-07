#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.MathTools;

//
// Summary:
//     A large set of useful game mathematics functions
public static class GameMath
{
    //
    // Summary:
    //     360°
    public const float TWOPI = MathF.PI * 2f;

    //
    // Summary:
    //     180°
    public const float PI = MathF.PI;

    //
    // Summary:
    //     90°
    public const float PIHALF = MathF.PI / 2f;

    public const double DEG2RAD_DOUBLE = Math.PI / 180.0;

    public const float DEG2RAD = MathF.PI / 180f;

    public const float RAD2DEG = 180f / MathF.PI;

    private const uint murmurseed = 144u;

    private static int SIN_BITS;

    private static int SIN_MASK;

    private static int SIN_COUNT;

    private static float radFull;

    private static float radToIndex;

    private static float degFull;

    private static float degToIndex;

    private static float[] sinValues;

    private static float[] cosValues;

    private static readonly int OaatIterations;

    public static float Sin(float value)
    {
        return (float)Math.Sin(value);
    }

    public static float Cos(float value)
    {
        return (float)Math.Cos(value);
    }

    public static float Acos(float value)
    {
        return (float)Math.Acos(value);
    }

    public static float Asin(float value)
    {
        return (float)Math.Asin(value);
    }

    public static float Tan(float value)
    {
        return (float)Math.Tan(value);
    }

    public static double Sin(double value)
    {
        return Math.Sin(value);
    }

    public static double Cos(double value)
    {
        return Math.Cos(value);
    }

    public static double Acos(double value)
    {
        return Math.Acos(value);
    }

    public static double Asin(double value)
    {
        return Math.Asin(value);
    }

    public static double Tan(double value)
    {
        return Math.Tan(value);
    }

    //
    // Summary:
    //     Faster Sin at the cost of lower accuracy
    //
    // Parameters:
    //   rad:
    public static float FastSin(float rad)
    {
        return sinValues[(int)(rad * radToIndex) & SIN_MASK];
    }

    //
    // Summary:
    //     Faster Cos at the cost of lower accuracy
    //
    // Parameters:
    //   rad:
    public static float FastCos(float rad)
    {
        return cosValues[(int)(rad * radToIndex) & SIN_MASK];
    }

    //
    // Summary:
    //     Faster Sin at the cost of lower accuracy
    //
    // Parameters:
    //   deg:
    public static float FastSinDeg(float deg)
    {
        return sinValues[(int)(deg * degToIndex) & SIN_MASK];
    }

    //
    // Summary:
    //     Faster Cos at the cost of lower accuracy
    //
    // Parameters:
    //   deg:
    public static float FastCosDeg(float deg)
    {
        return cosValues[(int)(deg * degToIndex) & SIN_MASK];
    }

    static GameMath()
    {
        OaatIterations = 3;
        SIN_BITS = 12;
        SIN_MASK = ~(-1 << SIN_BITS);
        SIN_COUNT = SIN_MASK + 1;
        radFull = MathF.PI * 2f;
        degFull = 360f;
        radToIndex = (float)SIN_COUNT / radFull;
        degToIndex = (float)SIN_COUNT / degFull;
        sinValues = new float[SIN_COUNT];
        cosValues = new float[SIN_COUNT];
        for (int i = 0; i < SIN_COUNT; i++)
        {
            sinValues[i] = (float)Math.Sin(((float)i + 0.5f) / (float)SIN_COUNT * radFull);
            cosValues[i] = (float)Math.Cos(((float)i + 0.5f) / (float)SIN_COUNT * radFull);
        }

        for (int j = 0; j < 360; j += 90)
        {
            sinValues[(int)((float)j * degToIndex) & SIN_MASK] = (float)Math.Sin((double)j * Math.PI / 180.0);
            cosValues[(int)((float)j * degToIndex) & SIN_MASK] = (float)Math.Cos((double)j * Math.PI / 180.0);
        }
    }

    public static float Sqrt(float value)
    {
        return (float)Math.Sqrt(value);
    }

    public static float Sqrt(double value)
    {
        return (float)Math.Sqrt(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float RootSumOfSquares(float a, float b, float c)
    {
        return (float)Math.Sqrt(a * a + b * b + c * c);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double SumOfSquares(double a, double b, double c)
    {
        return a * a + b * b + c * c;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Square(double a)
    {
        return a * a;
    }

    //
    // Summary:
    //     Force val to be inside a certain range
    //
    // Parameters:
    //   val:
    //
    //   min:
    //
    //   max:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(float val, float min, float max)
    {
        if (!(val < min))
        {
            if (!(val > max))
            {
                return val;
            }

            return max;
        }

        return min;
    }

    //
    // Summary:
    //     Force val to be inside a certain range
    //
    // Parameters:
    //   val:
    //
    //   min:
    //
    //   max:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(int val, int min, int max)
    {
        if (val >= min)
        {
            if (val <= max)
            {
                return val;
            }

            return max;
        }

        return min;
    }

    //
    // Summary:
    //     Force val to be inside a certain range
    //
    // Parameters:
    //   val:
    //
    //   min:
    //
    //   max:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte Clamp(byte val, byte min, byte max)
    {
        if (val >= min)
        {
            if (val <= max)
            {
                return val;
            }

            return max;
        }

        return min;
    }

    //
    // Summary:
    //     Force val to be inside a certain range
    //
    // Parameters:
    //   val:
    //
    //   min:
    //
    //   max:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Clamp(double val, double min, double max)
    {
        if (!(val < min))
        {
            if (!(val > max))
            {
                return val;
            }

            return max;
        }

        return min;
    }

    //
    // Summary:
    //     Force val to be outside a certain range
    //
    // Parameters:
    //   val:
    //
    //   atLeastNeg:
    //
    //   atLeastPos:
    public static int InverseClamp(int val, int atLeastNeg, int atLeastPos)
    {
        if (val >= atLeastPos)
        {
            if (val <= atLeastNeg)
            {
                return val;
            }

            return atLeastNeg;
        }

        return atLeastPos;
    }

    //
    // Summary:
    //     C#'s %-Operation is actually not modulo but remainder, so this is the actual
    //     modulo function that ensures positive numbers as return value
    //
    // Parameters:
    //   k:
    //
    //   n:
    public static int Mod(int k, int n)
    {
        if ((k %= n) >= 0)
        {
            return k;
        }

        return k + n;
    }

    public static uint Mod(uint k, uint n)
    {
        if ((k %= n) >= 0)
        {
            return k;
        }

        return k + n;
    }

    //
    // Summary:
    //     C#'s %-Operation is actually not modulo but remainder, so this is the actual
    //     modulo function that ensures positive numbers as return value
    //
    // Parameters:
    //   k:
    //
    //   n:
    public static float Mod(float k, float n)
    {
        if (!((k %= n) < 0f))
        {
            return k;
        }

        return k + n;
    }

    //
    // Summary:
    //     C#'s %-Operation is actually not modulo but remainder, so this is the actual
    //     modulo function that ensures positive numbers as return value
    //
    // Parameters:
    //   k:
    //
    //   n:
    public static double Mod(double k, double n)
    {
        if (!((k %= n) < 0.0))
        {
            return k;
        }

        return k + n;
    }

    //
    // Summary:
    //     Treats given value as a statistical average. Example: 2.1 will turn into 2 90%
    //     of the times and into 3 10% of times.
    //
    // Parameters:
    //   rand:
    //
    //   value:
    public static int RoundRandom(Random rand, float value)
    {
        return (int)value + ((rand.NextDouble() < (double)(value - (float)(int)value)) ? 1 : 0);
    }

    //
    // Summary:
    //     Treats given value as a statistical average. Example: 2.1 will turn into 2 90%
    //     of the times and into 3 10% of times.
    //
    // Parameters:
    //   rand:
    //
    //   value:
    public static int RoundRandom(IRandom rand, float value)
    {
        return (int)value + ((rand.NextDouble() < (double)(value - (float)(int)value)) ? 1 : 0);
    }

    //
    // Summary:
    //     Returns the shortest distance between 2 angles See also https://stackoverflow.com/a/14498790/1873041
    //
    //
    // Parameters:
    //   start:
    //
    //   end:
    public static float AngleDegDistance(float start, float end)
    {
        return ((end - start) % 360f + 540f) % 360f - 180f;
    }

    //
    // Summary:
    //     Returns the shortest distance between 2 angles See also https://stackoverflow.com/a/14498790/1873041
    //
    //
    // Parameters:
    //   start:
    //
    //   end:
    public static float AngleRadDistance(float start, float end)
    {
        return ((end - start) % (MathF.PI * 2f) + MathF.PI * 2f + MathF.PI) % (MathF.PI * 2f) - MathF.PI;
    }

    //
    // Summary:
    //     For angles in radians, normalise to the range 0 to 2 * PI and also, if barely
    //     close to a right angle, set it to a right angle (fixes loss of precision after
    //     multiple rotation operations etc.)
    //
    // Parameters:
    //   angleRad:
    public static float NormaliseAngleRad(float angleRad)
    {
        float num = 3.2E-06f;
        float num2 = Mod(angleRad, MathF.PI * 2f);
        if ((num2 + num) % (MathF.PI / 2f) <= num * 2f)
        {
            num2 = (float)((int)((num2 + num) / (MathF.PI / 2f)) % 4) * (MathF.PI / 2f);
        }

        return num2;
    }

    //
    // Summary:
    //     Returns the smallest number, ignoring the sign of either value. Examples:
    //     Smallest(1, 3) returns 1 Smallest(-20, 3) returns 3
    //
    // Parameters:
    //   a:
    //
    //   b:
    public static double Smallest(double a, double b)
    {
        double num = Math.Abs(a);
        double num2 = Math.Abs(b);
        if (num < num2)
        {
            return a;
        }

        return b;
    }

    //
    // Summary:
    //     Returns the smallest number, ignoring the sign of either value
    //
    // Parameters:
    //   a:
    //
    //   b:
    public static double Largest(double a, double b)
    {
        double num = Math.Abs(a);
        double num2 = Math.Abs(b);
        if (num > num2)
        {
            return a;
        }

        return b;
    }

    //
    // Summary:
    //     Returns the shortest distance between 2 values that are cyclical (e.g. angles,
    //     daytime hours, etc.) See also https://stackoverflow.com/a/14498790/1873041
    //
    // Parameters:
    //   start:
    //
    //   end:
    //
    //   period:
    public static float CyclicValueDistance(float start, float end, float period)
    {
        return ((end - start) % period + period * 1.5f) % period - period / 2f;
    }

    //
    // Summary:
    //     Returns the shortest distance between 2 values that are cyclical (e.g. angles,
    //     daytime hours, etc.) See also https://stackoverflow.com/a/14498790/1873041
    //
    // Parameters:
    //   start:
    //
    //   end:
    //
    //   period:
    public static double CyclicValueDistance(double start, double end, double period)
    {
        return ((end - start) % period + period * 1.5) % period - period / 2.0;
    }

    //
    // Summary:
    //     Generates a gaussian blur kernel to be used when blurring something
    //
    // Parameters:
    //   sigma:
    //
    //   size:
    public static double[,] GenGaussKernel(double sigma = 1.0, int size = 5)
    {
        double[,] array = new double[size, size];
        double num = (double)size / 2.0;
        double num2 = 0.0;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                array[i, j] = Math.Exp(-0.5 * (Math.Pow(((double)i - num) / sigma, 2.0) + Math.Pow(((double)j - num) / sigma, 2.0))) / (Math.PI * 2.0 * sigma * sigma);
                num2 += array[i, j];
            }
        }

        for (int k = 0; k < size; k++)
        {
            for (int l = 0; l < size; l++)
            {
                array[k, l] /= num2;
            }
        }

        return array;
    }

    //
    // Summary:
    //     Does linear interpolation on a 2d map for each of the 4 bytes individually (e.g.
    //     RGBA color). It's basically a bilinear zoom of an image like you know it from
    //     common image editing software. Only intended for square images. The resulting
    //     map will be without any paddding (also requires at least 1 padding at bottom
    //     and left side)
    //
    // Parameters:
    //   map:
    //
    //   zoom:
    public static int[] BiLerpColorMap(IntDataMap2D map, int zoom)
    {
        int innerSize = map.InnerSize;
        int num = innerSize * zoom;
        int[] array = new int[num * num];
        int topLeftPadding = map.TopLeftPadding;
        for (int i = 0; i < innerSize; i++)
        {
            for (int j = 0; j < innerSize; j++)
            {
                int leftTop = map.Data[(j + topLeftPadding) * map.Size + i + topLeftPadding];
                int rightTop = map.Data[(j + topLeftPadding) * map.Size + i + 1 + topLeftPadding];
                int leftBottom = map.Data[(j + 1 + topLeftPadding) * map.Size + i + topLeftPadding];
                int rightBottom = map.Data[(j + 1 + topLeftPadding) * map.Size + i + 1 + topLeftPadding];
                for (int k = 0; k < zoom; k++)
                {
                    int num2 = j * zoom + k;
                    for (int l = 0; l < zoom; l++)
                    {
                        int num3 = i * zoom + l;
                        array[num2 * num + num3] = BiLerpRgbColor((float)l / (float)zoom, (float)k / (float)zoom, leftTop, rightTop, leftBottom, rightBottom);
                    }
                }
            }
        }

        return array;
    }

    //
    // Summary:
    //     Linear Interpolates one selected bytes of the 4 ints
    //
    // Parameters:
    //   lx:
    //
    //   ly:
    //
    //   byteIndex:
    //     0, 1, 2 or 3
    //
    //   leftTop:
    //
    //   rightTop:
    //
    //   leftBottom:
    //
    //   rightBottom:
    public static byte BiLerpByte(float lx, float ly, int byteIndex, int leftTop, int rightTop, int leftBottom, int rightBottom)
    {
        byte left = LerpByte(lx, (byte)(leftTop >> byteIndex * 8), (byte)(rightTop >> byteIndex * 8));
        byte right = LerpByte(lx, (byte)(leftBottom >> byteIndex * 8), (byte)(rightBottom >> byteIndex * 8));
        return LerpByte(ly, left, right);
    }

    //
    // Summary:
    //     Linear Interpolates one selected bytes of the 4 ints
    //
    // Parameters:
    //   lx:
    //
    //   ly:
    //
    //   byteIndex:
    //     0, 1, 2 or 3
    //
    //   leftTop:
    //
    //   rightTop:
    //
    //   leftBottom:
    //
    //   rightBottom:
    public static byte BiSerpByte(float lx, float ly, int byteIndex, int leftTop, int rightTop, int leftBottom, int rightBottom)
    {
        return BiLerpByte(SmoothStep(lx), SmoothStep(ly), byteIndex, leftTop, rightTop, leftBottom, rightBottom);
    }

    //
    // Summary:
    //     Linear Interpolates the bytes of the int individually (i.e. interpolates RGB
    //     values individually)
    //
    // Parameters:
    //   lx:
    //
    //   ly:
    //
    //   leftTop:
    //
    //   rightTop:
    //
    //   leftBottom:
    //
    //   rightBottom:
    public static int BiLerpRgbaColor(float lx, float ly, int leftTop, int rightTop, int leftBottom, int rightBottom)
    {
        return BiLerpRgbColor(lx, ly, leftTop, rightTop, leftBottom, rightBottom) + (BiLerpAndMask(leftTop >> 4, rightTop >> 4, leftBottom >> 4, rightBottom >> 4, lx, ly, 267386880) << 4);
    }

    //
    // Summary:
    //     Linear Interpolates the lower 3 bytes of the int individually (i.e. interpolates
    //     RGB values individually)
    //
    // Parameters:
    //   lx:
    //
    //   ly:
    //
    //   leftTop:
    //
    //   rightTop:
    //
    //   leftBottom:
    //
    //   rightBottom:
    public static int BiLerpRgbColor(float lx, float ly, int leftTop, int rightTop, int leftBottom, int rightBottom)
    {
        return BiLerpAndMask(leftTop, rightTop, leftBottom, rightBottom, lx, ly, 255) + BiLerpAndMask(leftTop, rightTop, leftBottom, rightBottom, lx, ly, 65280) + BiLerpAndMask(leftTop, rightTop, leftBottom, rightBottom, lx, ly, 16711680);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int BiLerpAndMask(int leftTop, int rightTop, int leftBottom, int rightBottom, float lx, float ly, int mask)
    {
        return (int)Lerp(Lerp(leftTop & mask, rightTop & mask, lx), Lerp(leftBottom & mask, rightBottom & mask, lx), ly) & mask;
    }

    //
    // Summary:
    //     Smoothstep Interpolates the lower 3 bytes of the int individually (i.e. interpolates
    //     RGB values individually)
    //
    // Parameters:
    //   lx:
    //
    //   ly:
    //
    //   leftTop:
    //
    //   rightTop:
    //
    //   leftBottom:
    //
    //   rightBottom:
    public static int BiSerpRgbColor(float lx, float ly, int leftTop, int rightTop, int leftBottom, int rightBottom)
    {
        return BiLerpRgbColor(SmoothStep(lx), SmoothStep(ly), leftTop, rightTop, leftBottom, rightBottom);
    }

    //
    // Summary:
    //     Linear Interpolates the lower 3 bytes of the int individually (i.e. interpolates
    //     RGB values individually)
    //
    // Parameters:
    //   lx:
    //
    //   left:
    //
    //   right:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LerpRgbColor(float lx, int left, int right)
    {
        return (int)((1f - lx) * (float)(left & 0xFF) + lx * (float)(right & 0xFF)) + ((int)((1f - lx) * (float)(left & 0xFF00) + lx * (float)(right & 0xFF00)) & 0xFF00) + ((int)((1f - lx) * (float)(left & 0xFF0000) + lx * (float)(right & 0xFF0000)) & 0xFF0000);
    }

    //
    // Summary:
    //     Linear Interpolates the 4 bytes of the int individually (i.e. interpolates RGB
    //     values individually)
    //
    // Parameters:
    //   lx:
    //
    //   left:
    //
    //   right:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LerpRgbaColor(float lx, int left, int right)
    {
        return (int)((1f - lx) * (float)(left & 0xFF) + lx * (float)(right & 0xFF)) + ((int)((1f - lx) * (float)(left & 0xFF00) + lx * (float)(right & 0xFF00)) & 0xFF00) + ((int)((1f - lx) * (float)(left & 0xFF0000) + lx * (float)(right & 0xFF0000)) & 0xFF0000) + ((int)((1f - lx) * (float)((left >> 24) & 0xFF) + lx * (float)((right >> 24) & 0xFF)) << 24);
    }

    //
    // Summary:
    //     Linear Interpolates a single byte
    //
    // Parameters:
    //   lx:
    //
    //   left:
    //
    //   right:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte LerpByte(float lx, byte left, byte right)
    {
        return (byte)((1f - lx) * (float)(int)left + lx * (float)(int)right);
    }

    //
    // Summary:
    //     Basic Bilinear Lerp
    //
    // Parameters:
    //   topleft:
    //
    //   topright:
    //
    //   botleft:
    //
    //   botright:
    //
    //   x:
    //
    //   z:
    public static float BiLerp(float topleft, float topright, float botleft, float botright, float x, float z)
    {
        float num = topleft + (topright - topleft) * x;
        float num2 = botleft + (botright - botleft) * x;
        return num + (num2 - num) * z;
    }

    //
    // Summary:
    //     Basic Bilinear Lerp
    //
    // Parameters:
    //   topleft:
    //
    //   topright:
    //
    //   botleft:
    //
    //   botright:
    //
    //   x:
    //
    //   z:
    public static double BiLerp(double topleft, double topright, double botleft, double botright, double x, double z)
    {
        double num = topleft + (topright - topleft) * x;
        double num2 = botleft + (botright - botleft) * x;
        return num + (num2 - num) * z;
    }

    //
    // Summary:
    //     Same as Vintagestory.API.MathTools.GameMath.Lerp(System.Single,System.Single,System.Single)
    //
    //
    // Parameters:
    //   v0:
    //
    //   v1:
    //
    //   t:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Mix(float v0, float v1, float t)
    {
        return v0 + (v1 - v0) * t;
    }

    //
    // Summary:
    //     Same as Vintagestory.API.MathTools.GameMath.Lerp(System.Single,System.Single,System.Single)
    //
    //
    // Parameters:
    //   v0:
    //
    //   v1:
    //
    //   t:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Mix(int v0, int v1, float t)
    {
        return (int)((float)v0 + (float)(v1 - v0) * t);
    }

    //
    // Summary:
    //     Basic Lerp
    //
    // Parameters:
    //   v0:
    //
    //   v1:
    //
    //   t:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float v0, float v1, float t)
    {
        return v0 + (v1 - v0) * t;
    }

    //
    // Summary:
    //     Basic Lerp
    //
    // Parameters:
    //   v0:
    //
    //   v1:
    //
    //   t:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Lerp(double v0, double v1, double t)
    {
        return v0 + (v1 - v0) * t;
    }

    //
    // Summary:
    //     Smooth Interpolation using inlined Smoothstep
    //
    // Parameters:
    //   v0:
    //
    //   v1:
    //
    //   t:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Serp(float v0, float v1, float t)
    {
        return v0 + (v1 - v0) * t * t * (3f - 2f * t);
    }

    //
    // Summary:
    //     Unlike the other implementation here, which uses the default "uniform" treatment
    //     of t, this computation is used to calculate the same values but introduces the
    //     ability to "parameterize" the t values used in the calculation. This is based
    //     on Figure 3 from http://www.cemyuksel.com/research/catmullrom_param/catmullrom.pdf
    //
    //
    // Parameters:
    //   t:
    //     the actual interpolation ratio from 0 to 1 representing the position between
    //     p1 and p2 to interpolate the value.
    //
    //   p:
    //     An array of double values of length 4, where interpolation occurs from p1 to
    //     p2.
    //
    //   time:
    //     An array of time measures of length 4, corresponding to each p value.
    public static double CPCatmullRomSplineLerp(double t, double[] p, double[] time)
    {
        double num = p[0] * (time[1] - t) / (time[1] - time[0]) + p[1] * (t - time[0]) / (time[1] - time[0]);
        double num2 = p[1] * (time[2] - t) / (time[2] - time[1]) + p[2] * (t - time[1]) / (time[2] - time[1]);
        double num3 = p[2] * (time[3] - t) / (time[3] - time[2]) + p[3] * (t - time[2]) / (time[3] - time[2]);
        double num4 = num * (time[2] - t) / (time[2] - time[0]) + num2 * (t - time[0]) / (time[2] - time[0]);
        double num5 = num2 * (time[3] - t) / (time[3] - time[1]) + num3 * (t - time[1]) / (time[3] - time[1]);
        return num4 * (time[2] - t) / (time[2] - time[1]) + num5 * (t - time[1]) / (time[2] - time[1]);
    }

    //
    // Summary:
    //     Better Lerp but more CPU intensive, see also https://en.wikipedia.org/wiki/Smoothstep
    //
    //
    // Parameters:
    //   x:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SmoothStep(float x)
    {
        return x * x * (3f - 2f * x);
    }

    //
    // Summary:
    //     Better Lerp but more CPU intensive, see also https://en.wikipedia.org/wiki/Smoothstep
    //
    //
    // Parameters:
    //   x:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double SmoothStep(double x)
    {
        return x * x * (3.0 - 2.0 * x);
    }

    //
    // Summary:
    //     Better Lerp but more CPU intensive, see also https://en.wikipedia.org/wiki/Smoothstep
    //
    //
    // Parameters:
    //   edge0:
    //
    //   edge1:
    //
    //   x:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Smootherstep(float edge0, float edge1, float x)
    {
        x = Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return x * x * x * (x * (x * 6f - 15f) + 10f);
    }

    //
    // Summary:
    //     Better Lerp but more CPU intensive, see also https://en.wikipedia.org/wiki/Smoothstep
    //
    //
    // Parameters:
    //   edge0:
    //
    //   edge1:
    //
    //   x:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Smootherstep(double edge0, double edge1, double x)
    {
        x = Clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
        return x * x * x * (x * (x * 6.0 - 15.0) + 10.0);
    }

    //
    // Summary:
    //     Better Lerp but more CPU intensive, see also https://en.wikipedia.org/wiki/Smoothstep.
    //     x must be in range of 0..1
    //
    // Parameters:
    //   x:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Smootherstep(double x)
    {
        return x * x * x * (x * (x * 6.0 - 15.0) + 10.0);
    }

    //
    // Summary:
    //     Returns a value between 0..1. Returns 0 if val is smaller than left or greater
    //     than right. For val == (left+right)/2 the return value is 1. Every other value
    //     is a linear interpolation based on the distance to the middle value. Ascii art
    //     representation: 1 | /\ | / \ 0.5| / \ | / \ | / \ 0 __/__________\______ left
    //     right
    //
    // Parameters:
    //   val:
    //
    //   left:
    //
    //   right:
    public static float TriangleStep(int val, int left, int right)
    {
        float num = (left + right) / 2;
        float num2 = (right - left) / 2;
        return Math.Max(0f, 1f - Math.Abs((float)val - num) / num2);
    }

    //
    // Summary:
    //     Returns a value between 0..1. Returns 0 if val is smaller than left or greater
    //     than right. For val == (left+right)/2 the return value is 1. Every other value
    //     is a linear interpolation based on the distance to the middle value. Ascii art
    //     representation: 1 | /\ | / \ 0.5| / \ | / \ | / \ 0 __/__________\______ left
    //     right
    //
    // Parameters:
    //   val:
    //
    //   left:
    //
    //   right:
    public static float TriangleStep(float val, float left, float right)
    {
        float num = (left + right) / 2f;
        float num2 = (right - left) / 2f;
        return Math.Max(0f, 1f - Math.Abs(val - num) / num2);
    }

    //
    // Summary:
    //     Same as TriangleStep but skipping the step to calc mid and range.
    //
    // Parameters:
    //   val:
    //
    //   mid:
    //
    //   range:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float TriangleStepFast(int val, int mid, int range)
    {
        return Math.Max(0, 1 - Math.Abs(val - mid) / range);
    }

    public static double Max(params double[] values)
    {
        double num = values[0];
        for (int i = 0; i < values.Length; i++)
        {
            num = Math.Max(num, values[i]);
        }

        return num;
    }

    public static float Max(params float[] values)
    {
        float num = values[0];
        for (int i = 0; i < values.Length; i++)
        {
            num = Math.Max(num, values[i]);
        }

        return num;
    }

    public static int Max(params int[] values)
    {
        int num = values[0];
        for (int i = 0; i < values.Length; i++)
        {
            num = Math.Max(num, values[i]);
        }

        return num;
    }

    public static int Min(params int[] values)
    {
        int num = values[0];
        for (int i = 0; i < values.Length; i++)
        {
            num = Math.Min(num, values[i]);
        }

        return num;
    }

    public static float Min(params float[] values)
    {
        float num = values[0];
        for (int i = 0; i < values.Length; i++)
        {
            num = Math.Min(num, values[i]);
        }

        return num;
    }

    public static float SmoothMin(float a, float b, float smoothingFactor)
    {
        float num = Math.Max(smoothingFactor - Math.Abs(a - b), 0f) / smoothingFactor;
        return Math.Min(a, b) - num * num * smoothingFactor * 0.25f;
    }

    public static float SmoothMax(float a, float b, float smoothingFactor)
    {
        float num = Math.Max(smoothingFactor - Math.Abs(a - b), 0f) / smoothingFactor;
        return Math.Max(a, b) + num * num * smoothingFactor * 0.25f;
    }

    public static double SmoothMin(double a, double b, double smoothingFactor)
    {
        double num = Math.Max(smoothingFactor - Math.Abs(a - b), 0.0) / smoothingFactor;
        return Math.Min(a, b) - num * num * smoothingFactor * 0.25;
    }

    public static double SmoothMax(double a, double b, double smoothingFactor)
    {
        double num = Math.Max(smoothingFactor - Math.Abs(a - b), 0.0) / smoothingFactor;
        return Math.Max(a, b) + num * num * smoothingFactor * 0.25;
    }

    public static uint Crc32(string input)
    {
        return Crc32(Encoding.UTF8.GetBytes(input));
    }

    public static uint Crc32(byte[] input)
    {
        return Crc32Algorithm.Compute(input);
    }

    //
    // Summary:
    //     Pretty much taken directly from the string.GetHashCode() implementation, but
    //     on these methods the documentation states: "You should never persist or use a
    //     hash code outside the application domain in which it was created, [...]." Hence,
    //     this is one basic 32bit bit implementation that can be used in a platform independent,
    //     persistent way.
    //
    // Parameters:
    //   text:
    public unsafe static int DotNetStringHash(string text)
    {
        fixed (char* ptr = text)
        {
            int num = 352654597;
            int num2 = num;
            int* ptr2 = (int*)ptr;
            int num3;
            for (num3 = text.Length; num3 > 2; num3 -= 4)
            {
                num = ((num << 5) + num + (num >> 27)) ^ *ptr2;
                num2 = ((num2 << 5) + num2 + (num2 >> 27)) ^ ptr2[1];
                ptr2 += 2;
            }

            if (num3 > 0)
            {
                num = ((num << 5) + num + (num >> 27)) ^ *ptr2;
            }

            return num + num2 * 1566083941;
        }
    }

    //
    // Summary:
    //     See also https://msdn.microsoft.com/en-us/library/system.security.cryptography.md5%28v=vs.110%29.aspx
    //
    //
    // Parameters:
    //   input:
    public static string Md5Hash(string input)
    {
        using MD5 mD = MD5.Create();
        byte[] array = mD.ComputeHash(Encoding.UTF8.GetBytes(input));
        StringBuilder stringBuilder = new StringBuilder();
        for (int i = 0; i < array.Length; i++)
        {
            stringBuilder.Append(array[i].ToString("x2"));
        }

        return stringBuilder.ToString();
    }

    //
    // Summary:
    //     A single iteration of Bob Jenkins' One-At-A-Time hashing algorithm - optimised
    //     version for the vector hashes
    //
    // Parameters:
    //   x:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int oaatHashMany(int x)
    {
        for (int num = OaatIterations; num > 0; num--)
        {
            x += x << 10;
            x ^= x >> 6;
            x += x << 3;
            x ^= x >> 11;
            x += x << 15;
        }

        return x;
    }

    //
    // Summary:
    //     A single iteration of Bob Jenkins' One-At-A-Time hashing algorithm.
    //
    // Parameters:
    //   x:
    //
    //   count:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int oaatHashMany(int x, int count)
    {
        for (int num = count; num > 0; num--)
        {
            x += x << 10;
            x ^= x >> 6;
            x += x << 3;
            x ^= x >> 11;
            x += x << 15;
        }

        return x;
    }

    //
    // Summary:
    //     Bob Jenkins' One-At-A-Time hashing algorithm
    //
    // Parameters:
    //   x:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint oaatHashUMany(uint x)
    {
        for (int num = OaatIterations; num > 0; num--)
        {
            x += x << 10;
            x ^= x >> 6;
            x += x << 3;
            x ^= x >> 11;
            x += x << 15;
        }

        return x;
    }

    //
    // Summary:
    //     Bob Jenkins' One-At-A-Time hashing algorithm. Fast, but not very random.
    //
    // Parameters:
    //   v:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int oaatHash(Vec2i v)
    {
        return oaatHashMany(v.X ^ oaatHashMany(v.Y));
    }

    //
    // Summary:
    //     Bob Jenkins' One-At-A-Time hashing algorithm. Fast, but not very random.
    //
    // Parameters:
    //   x:
    //
    //   y:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int oaatHash(int x, int y)
    {
        return oaatHashMany(x ^ oaatHashMany(y));
    }

    //
    // Summary:
    //     Bob Jenkins' One-At-A-Time hashing algorithm. Fast, but not very random.
    //
    // Parameters:
    //   v:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int oaatHash(Vec3i v)
    {
        return oaatHashMany(v.X) ^ oaatHashMany(v.Y) ^ oaatHashMany(v.Z);
    }

    //
    // Summary:
    //     Bob Jenkins' One-At-A-Time hashing algorithm. Fast, but not very random.
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int oaatHash(int x, int y, int z)
    {
        return oaatHashMany(x) ^ oaatHashMany(y) ^ oaatHashMany(z);
    }

    //
    // Summary:
    //     Bob Jenkins' One-At-A-Time hashing algorithm. Fast, but not very random.
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint oaatHashU(int x, int y, int z)
    {
        return oaatHashUMany((uint)x) ^ oaatHashUMany((uint)y) ^ oaatHashUMany((uint)z);
    }

    //
    // Summary:
    //     Bob Jenkins' One-At-A-Time hashing algorithm. Fast, but not very random.
    //
    // Parameters:
    //   v:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int oaatHash(Vec4i v)
    {
        return oaatHashMany(v.X) ^ oaatHashMany(v.Y) ^ oaatHashMany(v.Z) ^ oaatHashMany(v.W);
    }

    //
    // Summary:
    //     A really bad, but very fast hashing method.
    //
    // Parameters:
    //   x:
    //
    //   y:
    public static float PrettyBadHash(int x, int y)
    {
        return (float)Mod(((double)x * 12.9898 + (double)y * 78.233) * 43758.5453, 1.0);
    }

    //
    // Summary:
    //     A not so fast, but higher quality than oaatHash(). See also https://en.wikipedia.org/wiki/MurmurHash.
    //     Includes a modulo operation.
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   mod:
    public static int MurmurHash3Mod(int x, int y, int z, int mod)
    {
        return Mod(MurmurHash3(x, y, z), mod);
    }

    //
    // Summary:
    //     A not so fast, but higher quality than oaatHash(). See also https://en.wikipedia.org/wiki/MurmurHash
    //
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public static int MurmurHash3(int x, int y, int z)
    {
        return (int)fmix((rotl32((rotl32((rotl32(0x90u ^ (rotl32((uint)(x * -862048943), 15) * 461845907), 13) * 5 + 3864292196u) ^ (rotl32((uint)(y * -862048943), 15) * 461845907), 13) * 5 + 3864292196u) ^ (rotl32((uint)(z * -862048943), 15) * 461845907), 13) * 5 + 3864292196u) ^ 3u);
    }

    private static uint rotl32(uint x, int r)
    {
        return (x << r) | (x >> 32 - r);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint fmix(uint h)
    {
        h = (h ^ (h >> 16)) * 2246822507u;
        h = (h ^ (h >> 13)) * 3266489909u;
        return h ^ (h >> 16);
    }

    //
    // Summary:
    //     Quasirandom sequence by Martin Roberts (http://extremelearning.com.au/unreasonable-effectiveness-of-quasirandom-sequences/)
    //
    //
    // Parameters:
    //   n:
    public static double R2Sequence1D(int n)
    {
        double num = 1.618033988749895;
        double num2 = 1.0 / num;
        return (0.5 + num2 * (double)n) % 1.0;
    }

    //
    // Summary:
    //     Quasirandom sequence by Martin Roberts (http://extremelearning.com.au/unreasonable-effectiveness-of-quasirandom-sequences/)
    //
    //
    // Parameters:
    //   n:
    public static Vec2d R2Sequence2D(int n)
    {
        double num = 1.324717957244746;
        double num2 = 1.0 / num;
        double num3 = 1.0 / (num * num);
        return new Vec2d((0.5 + num2 * (double)n) % 1.0, (0.5 + num3 * (double)n) % 1.0);
    }

    //
    // Summary:
    //     Quasirandom sequence by Martin Roberts (http://extremelearning.com.au/unreasonable-effectiveness-of-quasirandom-sequences/)
    //
    //
    // Parameters:
    //   n:
    public static Vec3d R2Sequence3D(int n)
    {
        double num = 1.2207440846057596;
        double num2 = 1.0 / num;
        double num3 = 1.0 / (num * num);
        double num4 = 1.0 / (num * num * num);
        return new Vec3d((0.5 + num2 * (double)n) % 1.0, (0.5 + num3 * (double)n) % 1.0, (0.5 + num4 * (double)n) % 1.0);
    }

    //
    // Summary:
    //     Assigns the value of x1 to x2 and vice versa
    //
    // Parameters:
    //   x1:
    //
    //   x2:
    public static void FlipVal(ref int x1, ref int x2)
    {
        int num = x1;
        x2 = x1;
        x1 = num;
    }

    //
    // Summary:
    //     Assigns the value of x1 to x2 and vice versa
    //
    // Parameters:
    //   x1:
    //
    //   x2:
    public static void FlipVal(ref double x1, ref double x2)
    {
        double num = x1;
        x2 = x1;
        x1 = num;
    }

    //
    // Summary:
    //     Performs a Fisher-Yates shuffle in linear time or O(n)
    //
    // Parameters:
    //   rand:
    //
    //   array:
    //
    // Type parameters:
    //   T:
    public static void Shuffle<T>(Random rand, T[] array)
    {
        int num = array.Length;
        while (num > 1)
        {
            int num2 = rand.Next(num);
            num--;
            T val = array[num];
            array[num] = array[num2];
            array[num2] = val;
        }
    }

    //
    // Summary:
    //     Performs a Fisher-Yates shuffle in linear time or O(n)
    //
    // Parameters:
    //   rand:
    //
    //   array:
    //
    // Type parameters:
    //   T:
    public static void Shuffle<T>(Random rand, List<T> array)
    {
        int num = array.Count;
        while (num > 1)
        {
            int index = rand.Next(num);
            num--;
            T value = array[num];
            array[num] = array[index];
            array[index] = value;
        }
    }

    //
    // Summary:
    //     Performs a Fisher-Yates shuffle in linear time or O(n)
    //
    // Parameters:
    //   rand:
    //
    //   array:
    //
    // Type parameters:
    //   T:
    public static void Shuffle<T>(LCGRandom rand, List<T> array)
    {
        int num = array.Count;
        while (num > 1)
        {
            int index = rand.NextInt(num);
            num--;
            T value = array[num];
            array[num] = array[index];
            array[index] = value;
        }
    }

    //
    // Summary:
    //     Plot a 3d line, see also http://members.chello.at/~easyfilter/bresenham.html
    //
    //
    // Parameters:
    //   x0:
    //
    //   y0:
    //
    //   z0:
    //
    //   x1:
    //
    //   y1:
    //
    //   z1:
    //
    //   onPlot:
    public static void BresenHamPlotLine3d(int x0, int y0, int z0, int x1, int y1, int z1, PlotDelegate3D onPlot)
    {
        int num = Math.Abs(x1 - x0);
        int num2 = ((x0 < x1) ? 1 : (-1));
        int num3 = Math.Abs(y1 - y0);
        int num4 = ((y0 < y1) ? 1 : (-1));
        int num5 = Math.Abs(z1 - z0);
        int num6 = ((z0 < z1) ? 1 : (-1));
        int num7 = Max(num, num3, num5);
        int num8 = num7;
        x1 = (y1 = (z1 = num7 / 2));
        while (true)
        {
            onPlot(x0, y0, z0);
            if (num8-- != 0)
            {
                x1 -= num;
                if (x1 < 0)
                {
                    x1 += num7;
                    x0 += num2;
                }

                y1 -= num3;
                if (y1 < 0)
                {
                    y1 += num7;
                    y0 += num4;
                }

                z1 -= num5;
                if (z1 < 0)
                {
                    z1 += num7;
                    z0 += num6;
                }

                continue;
            }

            break;
        }
    }

    //
    // Summary:
    //     Plot a 3d line, see also http://members.chello.at/~easyfilter/bresenham.html
    //
    //
    // Parameters:
    //   x0:
    //
    //   y0:
    //
    //   z0:
    //
    //   x1:
    //
    //   y1:
    //
    //   z1:
    //
    //   onPlot:
    public static void BresenHamPlotLine3d(int x0, int y0, int z0, int x1, int y1, int z1, PlotDelegate3DBlockPos onPlot)
    {
        int num = Math.Abs(x1 - x0);
        int num2 = ((x0 < x1) ? 1 : (-1));
        int num3 = Math.Abs(y1 - y0);
        int num4 = ((y0 < y1) ? 1 : (-1));
        int num5 = Math.Abs(z1 - z0);
        int num6 = ((z0 < z1) ? 1 : (-1));
        int num7 = Max(num, num3, num5);
        int num8 = num7;
        x1 = (y1 = (z1 = num7 / 2));
        BlockPos blockPos = new BlockPos();
        while (true)
        {
            blockPos.Set(x0, y0, z0);
            onPlot(blockPos);
            if (num8-- != 0)
            {
                x1 -= num;
                if (x1 < 0)
                {
                    x1 += num7;
                    x0 += num2;
                }

                y1 -= num3;
                if (y1 < 0)
                {
                    y1 += num7;
                    y0 += num4;
                }

                z1 -= num5;
                if (z1 < 0)
                {
                    z1 += num7;
                    z0 += num6;
                }

                continue;
            }

            break;
        }
    }

    //
    // Summary:
    //     Plot a 2d line, see also http://members.chello.at/~easyfilter/bresenham.html
    //
    //
    // Parameters:
    //   x0:
    //
    //   y0:
    //
    //   x1:
    //
    //   y1:
    //
    //   onPlot:
    public static void BresenHamPlotLine2d(int x0, int y0, int x1, int y1, PlotDelegate2D onPlot)
    {
        int num = Math.Abs(x1 - x0);
        int num2 = ((x0 < x1) ? 1 : (-1));
        int num3 = -Math.Abs(y1 - y0);
        int num4 = ((y0 < y1) ? 1 : (-1));
        int num5 = num + num3;
        while (true)
        {
            onPlot(x0, y0);
            if (x0 != x1 || y0 != y1)
            {
                int num6 = 2 * num5;
                if (num6 >= num3)
                {
                    num5 += num3;
                    x0 += num2;
                }

                if (num6 <= num)
                {
                    num5 += num;
                    y0 += num4;
                }

                continue;
            }

            break;
        }
    }

    public static Vec3f ToEulerAngles(Vec4f q)
    {
        Vec3d vec3d = ToEulerAngles(new Vec4d(q.X, q.Y, q.Z, q.W));
        return new Vec3f((float)vec3d.X, (float)vec3d.Y, (float)vec3d.Z);
    }

    public static Vec3d ToEulerAngles(Vec4d q)
    {
        Vec3d vec3d = new Vec3d();
        double y = 2.0 * (q.W * q.X + q.Y * q.Z);
        double x = 1.0 - 2.0 * (q.X * q.X + q.Y * q.Y);
        vec3d.X = Math.Atan2(y, x);
        double d = 2.0 * (q.W * q.Y - q.Z * q.X);
        vec3d.Y = Math.Asin(d);
        double y2 = 2.0 * (q.W * q.Z + q.X * q.Y);
        double x2 = 1.0 - 2.0 * (q.Y * q.Y + q.Z * q.Z);
        vec3d.Z = Math.Atan2(y2, x2);
        return vec3d;
    }

    public static int IntFromBools(int[] intBools)
    {
        int num = 0;
        int num2 = intBools.Length;
        while (num2-- != 0)
        {
            if (intBools[num2] != 0)
            {
                num += 1 << num2;
            }
        }

        return num;
    }

    public static int IntFromBools(bool[] bools)
    {
        int num = 0;
        int num2 = bools.Length;
        while (num2-- != 0)
        {
            if (bools[num2])
            {
                num += 1 << num2;
            }
        }

        return num;
    }

    public static void BoolsFromInt(bool[] bools, int v)
    {
        int num = bools.Length;
        while (num-- != 0)
        {
            bools[num] = (v & (1 << num)) != 0;
        }
    }

    //
    // Summary:
    //     Map a value from one range to another
    //
    // Parameters:
    //   value:
    //
    //   fromMin:
    //
    //   fromMax:
    //
    //   toMin:
    //
    //   toMax:
    public static T Map<T>(T value, T fromMin, T fromMax, T toMin, T toMax) where T : INumber<T>
    {
        return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
    }
}
#if false // Decompilation log
'180' items in cache
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
Could not find by name: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
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
