#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Globalization;

namespace Vintagestory.API.MathTools;

//
// Summary:
//     Many utility methods and fields for working with colors
public class ColorUtil
{
    //
    // Summary:
    //     Converts HSV (extracted from light and lightSat) to RGBA
    public class LightUtil
    {
        private readonly float[] blockLightlevels;

        private readonly float[] sunLightlevels;

        private readonly byte[] hueLevels;

        private readonly byte[] satLevels;

        private readonly byte[] blockLightlevelsByte;

        private readonly byte[] sunLightlevelsByte;

        public LightUtil(float[] blockLights, float[] sunLights, byte[] hues, byte[] sats)
        {
            blockLightlevels = blockLights;
            sunLightlevels = sunLights;
            hueLevels = hues;
            satLevels = sats;
            blockLightlevelsByte = new byte[blockLightlevels.Length];
            for (int i = 0; i < blockLightlevels.Length; i++)
            {
                blockLightlevelsByte[i] = (byte)(blockLightlevels[i] * 255.999f);
            }

            sunLightlevelsByte = new byte[sunLightlevels.Length];
            for (int j = 0; j < sunLightlevels.Length; j++)
            {
                sunLightlevelsByte[j] = (byte)(sunLightlevels[j] * 255.999f);
            }
        }

        public int ToRgba(uint light, int lightSat)
        {
            byte b = blockLightlevelsByte[(light >> 5) & 0x1F];
            byte b2 = sunLightlevelsByte[light & 0x1F];
            if (lightSat == 0)
            {
                return (b2 << 24) | (b << 16) | (b << 8) | b;
            }

            byte num = hueLevels[light >> 10];
            byte b3 = satLevels[lightSat];
            int num2 = num / 43;
            int num3 = (num - num2 * 43) * 6;
            int num4 = b * (255 - b3) >> 8;
            int num5 = b * (255 - (b3 * num3 >> 8)) >> 8;
            int num6 = b * (255 - (b3 * (255 - num3) >> 8)) >> 8;
            return num2 switch
            {
                0 => (b2 << 24) | (num4 << 16) | (num6 << 8) | b,
                1 => (b2 << 24) | (num4 << 16) | (b << 8) | num5,
                2 => (b2 << 24) | (num6 << 16) | (b << 8) | num4,
                3 => (b2 << 24) | (b << 16) | (num5 << 8) | num4,
                4 => (b2 << 24) | (b << 16) | (num4 << 8) | num6,
                _ => (b2 << 24) | (num5 << 16) | (num4 << 8) | b,
            };
        }
    }

    //
    // Summary:
    //     Amount of bits per block that are available to store the hue value
    public const int HueBits = 6;

    //
    // Summary:
    //     Amount of bits per block that are available to store the saturation value
    public const int SatBits = 3;

    //
    // Summary:
    //     Amount of bits per block that are available to store the brightness value
    public const int BrightBits = 5;

    public const int HueMul = 4;

    public const int SatMul = 32;

    public const int BrightMul = 8;

    public static int HueQuantities = (int)Math.Pow(2.0, 6.0);

    public static int SatQuantities = (int)Math.Pow(2.0, 3.0);

    public static int BrightQuantities = (int)Math.Pow(2.0, 5.0);

    //
    // Summary:
    //     255 << 24
    public const int OpaqueAlpha = -16777216;

    //
    // Summary:
    //     ~(255 << 24)
    public const int ClearAlpha = 16777215;

    //
    // Summary:
    //     White opaque color as normalized float values (0..1)
    public static readonly Vec3f WhiteRgbVec = new Vec3f(1f, 1f, 1f);

    //
    // Summary:
    //     White opaque color as normalized float values (0..1)
    public static readonly Vec4f WhiteArgbVec = new Vec4f(1f, 1f, 1f, 1f);

    //
    // Summary:
    //     White opaque color as normalized float values (0..1)
    public static readonly float[] WhiteArgbFloat = new float[4] { 1f, 1f, 1f, 1f };

    //
    // Summary:
    //     White opaque color as normalized float values (0..1)
    public static readonly double[] WhiteArgbDouble = new double[4] { 1.0, 1.0, 1.0, 1.0 };

    //
    // Summary:
    //     White opaque argb color as bytes (0..255)
    public static readonly byte[] WhiteArgbBytes = new byte[4] { 255, 255, 255, 255 };

    //
    // Summary:
    //     White opaque ahsv color as bytes (0..255)
    public static readonly byte[] WhiteAhsvBytes = new byte[4] { 255, 0, 0, 255 };

    //
    // Summary:
    //     White opaque argb color
    public const int WhiteArgb = -1;

    //
    // Summary:
    //     White opaque AHSV color
    public static readonly int WhiteAhsl = ToRgba(255, 255, 0, 0);

    //
    // Summary:
    //     Black opaque rgb color
    public static readonly int BlackArgb = ToRgba(255, 0, 0, 0);

    //
    // Summary:
    //     Black opaque rgb color
    public static readonly Vec3f BlackRgbVec = new Vec3f(0f, 0f, 0f);

    //
    // Summary:
    //     Black opaque rgb color
    public static readonly Vec4f BlackArgbVec = new Vec4f(0f, 0f, 0f, 255f);

    //
    // Summary:
    //     White opaque color as normalized float values (0..1)
    public static readonly double[] BlackArgbDouble = new double[4] { 0.0, 0.0, 0.0, 1.0 };

    //
    // Summary:
    //     Reverses the RGB channels, but leaves alpha untouched. Basically turns RGBA into
    //     BGRA and vice versa
    //
    // Parameters:
    //   color:
    public static byte[] ReverseColorBytes(byte[] color)
    {
        return new byte[4]
        {
            color[2],
            color[1],
            color[0],
            color[3]
        };
    }

    //
    // Summary:
    //     Reverses the RGB channels, but leaves alpha untouched. Basically turns RGBA into
    //     BGRA and vice versa
    //
    // Parameters:
    //   color:
    public static int ReverseColorBytes(int color)
    {
        return (int)(color & 0xFF000000u) | ((color >> 16) & 0xFF) | (((color >> 8) & 0xFF) << 8) | ((color & 0xFF) << 16);
    }

    //
    // Summary:
    //     Splits up a 32bit int color into 4 1 byte components, in BGRA order (Alpha channel
    //     at the highest 8 bits)
    //
    // Parameters:
    //   color:
    public static byte[] ToBGRABytes(int color)
    {
        return new byte[4]
        {
            (byte)color,
            (byte)(color >> 8),
            (byte)(color >> 16),
            (byte)(color >> 24)
        };
    }

    //
    // Summary:
    //     Splits up a 32bit int color into 4 1 byte components, in RGBA order
    //
    // Parameters:
    //   color:
    public static byte[] ToRGBABytes(int color)
    {
        return new byte[4]
        {
            (byte)(color >> 16),
            (byte)(color >> 8),
            (byte)color,
            (byte)(color >> 24)
        };
    }

    //
    // Summary:
    //     Returns a 4 element rgb float with values between 0..1
    //
    // Parameters:
    //   color:
    public static float[] ToRGBAFloats(int color)
    {
        return new float[4]
        {
            (float)((color >> 16) & 0xFF) / 255f,
            (float)((color >> 8) & 0xFF) / 255f,
            (float)(color & 0xFF) / 255f,
            (float)((color >> 24) & 0xFF) / 255f
        };
    }

    //
    // Summary:
    //     Returns a 4 element rgb float with values between 0..1
    //
    // Parameters:
    //   color:
    //
    //   outVal:
    public static Vec3f ToRGBVec3f(int color, ref Vec3f outVal)
    {
        return outVal.Set((float)((color >> 16) & 0xFF) / 255f, (float)((color >> 8) & 0xFF) / 255f, (float)(color & 0xFF) / 255f);
    }

    public static Vec4f ToRGBAVec4f(int color, ref Vec4f outVal)
    {
        return outVal.Set((float)((color >> 16) & 0xFF) / 255f, (float)((color >> 8) & 0xFF) / 255f, (float)(color & 0xFF) / 255f, (float)((color >> 24) & 0xFF) / 255f);
    }

    public static Vec4f ToRGBAVec4f(int color)
    {
        return new Vec4f((float)((color >> 16) & 0xFF) / 255f, (float)((color >> 8) & 0xFF) / 255f, (float)(color & 0xFF) / 255f, (float)((color >> 24) & 0xFF) / 255f);
    }

    public static int ColorFromRgba(byte[] channels)
    {
        return channels[0] | (channels[1] << 8) | (channels[2] << 16) | (channels[3] << 24);
    }

    public static int ColorFromRgba(Vec4f colorRel)
    {
        return (int)(colorRel.R * 255f) | ((int)(colorRel.G * 255f) << 8) | ((int)(colorRel.B * 255f) << 16) | ((int)(colorRel.A * 255f) << 24);
    }

    //
    // Summary:
    //     Care: the returned value is in true RGBA order, not BGRA as used for example
    //     by VS particle system. Therefore, depending on use, calling code may need to
    //     exchange the r and b parameters to see correct colors rendered in-game.
    public static int ColorFromRgba(int r, int g, int b, int a)
    {
        return r | (g << 8) | (b << 16) | (a << 24);
    }

    public static int FromRGBADoubles(double[] rgba)
    {
        return ColorFromRgba((int)(rgba[0] * 255.0), (int)(rgba[1] * 255.0), (int)(rgba[2] * 255.0), (int)(rgba[3] * 255.0));
    }

    //
    // Summary:
    //     Returns a 4 element rgb double with values between 0..1
    //
    // Parameters:
    //   color:
    public static double[] ToRGBADoubles(int color)
    {
        return new double[4]
        {
            (double)((color >> 16) & 0xFF) / 255.0,
            (double)((color >> 8) & 0xFF) / 255.0,
            (double)(color & 0xFF) / 255.0,
            (double)((color >> 24) & 0xFF) / 255.0
        };
    }

    public static int ColorFromRgba(double[] col)
    {
        return (int)(col[0] * 255.0) | ((int)(col[1] * 255.0) << 8) | ((int)(col[2] * 255.0) << 16) | ((int)(col[3] * 255.0) << 24);
    }

    //
    // Summary:
    //     Multiplies two colors together: c=(a*b)/255
    //
    // Parameters:
    //   color1:
    //
    //   color2:
    public static byte[] ColorMultiply(byte[] color1, byte[] color2)
    {
        return new byte[3]
        {
            (byte)(color1[0] * color2[0] / 255),
            (byte)(color1[1] * color2[1] / 255),
            (byte)(color1[2] * color2[2] / 255)
        };
    }

    //
    // Summary:
    //     Multiplies two colors together c=(a*b)/255
    //
    // Parameters:
    //   color:
    //
    //   color2:
    public static int ColorMultiplyEach(int color, int color2)
    {
        return (((color >> 24) & 0xFF) * ((color2 >> 24) & 0xFF) / 255 << 24) | (((color >> 16) & 0xFF) * ((color2 >> 16) & 0xFF) / 255 << 16) | (((color >> 8) & 0xFF) * ((color2 >> 8) & 0xFF) / 255 << 8) | ((color & 0xFF) * (color2 & 0xFF) / 255);
    }

    //
    // Summary:
    //     Multiplies a float value to the rgb color channels, leaves alpha channel unchanged
    //
    //
    // Parameters:
    //   color:
    //
    //   multiplier:
    public static int ColorMultiply3(int color, float multiplier)
    {
        return (int)(color & 0xFF000000u) | ((int)((float)((color >> 16) & 0xFF) * multiplier) << 16) | ((int)((float)((color >> 8) & 0xFF) * multiplier) << 8) | (int)((float)(color & 0xFF) * multiplier);
    }

    //
    // Summary:
    //     Multiplies a float value to the rgb color channels, leaves alpha channel unchanged.
    //     Makes sure the multiplied value stays within the 0..255 range
    //
    // Parameters:
    //   color:
    //
    //   multiplier:
    public static int ColorMultiply3Clamped(int color, float multiplier)
    {
        return (int)(color & 0xFF000000u) | ((int)GameMath.Clamp((float)((color >> 16) & 0xFF) * multiplier, 0f, 255f) << 16) | ((int)GameMath.Clamp((float)((color >> 8) & 0xFF) * multiplier, 0f, 255f) << 8) | (int)GameMath.Clamp((float)(color & 0xFF) * multiplier, 0f, 255f);
    }

    //
    // Summary:
    //     Multiplies a float value to the rgb color channels
    //
    // Parameters:
    //   color:
    //
    //   blueMul:
    //
    //   redMul:
    //
    //   greenMul:
    //
    //   alphaMul:
    public static int ColorMultiply4(int color, float redMul, float greenMul, float blueMul, float alphaMul)
    {
        return ((int)((float)((color >> 24) & 0xFF) * alphaMul) << 24) | ((int)((float)((color >> 16) & 0xFF) * blueMul) << 16) | ((int)((float)((color >> 8) & 0xFF) * greenMul) << 8) | (int)((float)(color & 0xFF) * redMul);
    }

    //
    // Summary:
    //     Multiplies a float value to every color channel including the alpha component.
    //
    //
    // Parameters:
    //   color:
    //
    //   multiplier:
    public static int ColorMultiply4(int color, float multiplier)
    {
        return ((int)((float)((color >> 24) & 0xFF) * multiplier) << 24) | ((int)((float)((color >> 16) & 0xFF) * multiplier) << 16) | ((int)((float)((color >> 8) & 0xFF) * multiplier) << 8) | (int)((float)(color & 0xFF) * multiplier);
    }

    //
    // Summary:
    //     Averages several colors together in RGB space
    //
    // Parameters:
    //   colors:
    //
    //   weights:
    public static int ColorAverage(int[] colors, float[] weights)
    {
        int num = 0;
        int num2 = 0;
        int num3 = 0;
        for (int i = 0; i < colors.Length; i++)
        {
            float num4 = weights[i];
            if (num4 != 0f)
            {
                num += (int)(num4 * (float)((colors[i] >> 16) & 0xFF));
                num2 += (int)(num4 * (float)((colors[i] >> 8) & 0xFF));
                num3 += (int)(num4 * (float)(colors[i] & 0xFF));
            }
        }

        return (Math.Min(255, num) << 16) + (Math.Min(255, num2) << 8) + Math.Min(255, num3);
    }

    //
    // Summary:
    //     Overlays rgb2 over rgb1 When c2weight = 0 resulting color is color1, when c2weight
    //     = 1 then resulting color is color2 Resulting color alpha value is 100% color1
    //     alpha
    //
    // Parameters:
    //   rgb1:
    //
    //   rgb2:
    //
    //   c2weight:
    public static int ColorOverlay(int rgb1, int rgb2, float c2weight)
    {
        return (((rgb1 >> 24) & 0xFF) << 24) | ((int)((float)((rgb1 >> 16) & 0xFF) * (1f - c2weight) + c2weight * (float)((rgb2 >> 16) & 0xFF)) << 16) | ((int)((float)((rgb1 >> 8) & 0xFF) * (1f - c2weight) + c2weight * (float)((rgb2 >> 8) & 0xFF)) << 8) | (int)((float)(rgb1 & 0xFF) * (1f - c2weight) + c2weight * (float)(rgb2 & 0xFF));
    }

    //
    // Summary:
    //     Overlays rgb1 on top of rgb2, based on their alpha values
    //
    // Parameters:
    //   rgb1:
    //
    //   rgb2:
    public static int ColorOver(int rgb1, int rgb2)
    {
        float num = (float)((rgb1 >> 24) & 0xFF) / 255f;
        float num2 = (float)((rgb2 >> 24) & 0xFF) / 255f;
        if (num == 0f && num2 == 0f)
        {
            return 0;
        }

        return ((int)(255f * (num + num2 * (1f - num))) << 24) | (ValueOverlay((rgb1 >> 16) & 0xFF, num, (rgb2 >> 16) & 0xFF, num2) << 16) | (ValueOverlay((rgb1 >> 8) & 0xFF, num, (rgb2 >> 8) & 0xFF, num2) << 8) | ValueOverlay(rgb1 & 0xFF, num, rgb2 & 0xFF, num2);
    }

    private static int ValueOverlay(int c1, float a1, int c2, float a2)
    {
        return (int)(((float)c1 * a1 + (float)c2 * a2 * (1f - a1)) / (a1 + a2 * (1f - a1)));
    }

    //
    // Summary:
    //     Combines two HSV colors by converting them to rgb then back to hsv. Uses the
    //     brightness as a weighting factor. Also leaves the brightness at the max of both
    //     hsv colors.
    //
    // Parameters:
    //   h1:
    //
    //   s1:
    //
    //   v1:
    //
    //   h2:
    //
    //   s2:
    //
    //   v2:
    //
    // Returns:
    //     Combined HSV Color
    public static int[] ColorCombineHSV(int h1, int s1, int v1, int h2, int s2, int v2)
    {
        int[] array = Hsv2RgbInts(h1, s1, v1);
        int[] array2 = Hsv2RgbInts(h2, s2, v2);
        float num = (float)v1 / (float)(v1 + v2);
        float num2 = 1f - num;
        int[] array3 = RgbToHsvInts((int)((float)array[0] * num + (float)array2[0] * num2), (int)((float)array[1] * num + (float)array2[1] * num2), (int)((float)array[2] * num + (float)array2[2] * num2));
        array3[2] = Math.Max(v1, v2);
        return array3;
    }

    //
    // Summary:
    //     Removes HSV2 from HSV1 by converting them to rgb then back to hsv. Uses the brightness
    //     as a weighting factor. Leaves brightness unchanged.
    //
    // Parameters:
    //   h1:
    //
    //   s1:
    //
    //   v1:
    //
    //   h2:
    //
    //   s2:
    //
    //   v2:
    public static int[] ColorSubstractHSV(int h1, int s1, int v1, int h2, int s2, int v2)
    {
        int[] array = Hsv2RgbInts(h1, s1, v1);
        int[] array2 = Hsv2RgbInts(h2, s2, v2);
        float num = (float)(v1 + v2) / (float)v1;
        float num2 = 1f - (float)v1 / (float)(v1 + v2);
        int[] array3 = RgbToHsvInts((int)((float)array[0] * num - (float)array2[0] * num2), (int)((float)array[1] * num - (float)array2[1] * num2), (int)((float)array[2] * num - (float)array2[2] * num2));
        array3[2] = v1;
        return array3;
    }

    //
    // Summary:
    //     Pack the 4 color components into a single ARGB 32bit int
    //
    // Parameters:
    //   a:
    //
    //   r:
    //
    //   g:
    //
    //   b:
    public static int ToRgba(int a, int r, int g, int b)
    {
        return (a << 24) | (r << 16) | (g << 8) | b;
    }

    //
    // Summary:
    //     Returns alpha value of given color
    //
    // Parameters:
    //   color:
    public static byte ColorA(int color)
    {
        return (byte)(color >> 24);
    }

    //
    // Summary:
    //     Returns red value of given color
    //
    // Parameters:
    //   color:
    public static byte ColorR(int color)
    {
        return (byte)(color >> 16);
    }

    //
    // Summary:
    //     Returns green value of given color
    //
    // Parameters:
    //   color:
    public static byte ColorG(int color)
    {
        return (byte)(color >> 8);
    }

    //
    // Summary:
    //     Returns blue value of given color
    //
    // Parameters:
    //   color:
    public static byte ColorB(int color)
    {
        return (byte)color;
    }

    //
    // Summary:
    //     Returns human a readable string of given color
    //
    // Parameters:
    //   color:
    public static string ColorToString(int color)
    {
        return ColorR(color) + ", " + ColorG(color) + ", " + ColorB(color) + ", " + ColorA(color);
    }

    //
    // Summary:
    //     Turn a string hex color (with #) into a single int
    //
    // Parameters:
    //   hex:
    public static int Hex2Int(string hex)
    {
        return int.Parse(hex.Substring(1), NumberStyles.HexNumber);
    }

    //
    // Summary:
    //     Turn a color int into its string hex version, including preceeding #
    //
    // Parameters:
    //   color:
    public static string Int2Hex(int color)
    {
        return $"#{ColorR(color):X2}{ColorG(color):X2}{ColorB(color):X2}";
    }

    public static string Int2HexBGR(int color)
    {
        return string.Format("#{2:X2}{1:X2}{0:X2}", ColorR(color), ColorG(color), ColorB(color));
    }

    //
    // Summary:
    //     Turn a color int into its string hex version, including preceeding #, including
    //     alpha channel
    //
    // Parameters:
    //   color:
    public static string Int2HexRgba(int color)
    {
        return $"#{ColorR(color):X2}{ColorG(color):X2}{ColorB(color):X2}{ColorA(color):X2}";
    }

    public static string Doubles2Hex(double[] color)
    {
        return $"#{(int)(255.0 * color[0]):X2}{(int)(255.0 * color[1]):X2}{(int)(255.0 * color[2]):X2}";
    }

    //
    // Summary:
    //     Parses a hex string as an rgb(a) color and returns an array of colors normalized
    //     fom 0..1 for use with Cairo. E.g. turns #FF0000 into double[1, 0, 0, 1] and #00FF00CC
    //     into double[0, 1, 0, 0.8]
    //
    // Parameters:
    //   hex:
    public static double[] Hex2Doubles(string hex)
    {
        int num = int.Parse(hex.Substring(1, 2), NumberStyles.HexNumber);
        int num2 = int.Parse(hex.Substring(3, 2), NumberStyles.HexNumber);
        int num3 = int.Parse(hex.Substring(5, 2), NumberStyles.HexNumber);
        int num4 = ((hex.Length < 8) ? 255 : int.Parse(hex.Substring(7, 2), NumberStyles.HexNumber));
        return new double[4]
        {
            (double)num / 255.0,
            (double)num2 / 255.0,
            (double)num3 / 255.0,
            (double)num4 / 255.0
        };
    }

    public static double[] Hex2Doubles(string hex, double opacityRel)
    {
        int num = int.Parse(hex.Substring(1, 2), NumberStyles.HexNumber);
        int num2 = int.Parse(hex.Substring(3, 2), NumberStyles.HexNumber);
        int num3 = int.Parse(hex.Substring(5, 2), NumberStyles.HexNumber);
        return new double[4]
        {
            (double)num / 255.0,
            (double)num2 / 255.0,
            (double)num3 / 255.0,
            opacityRel
        };
    }

    //
    // Summary:
    //     Converts given RGB values into it's respective HSV Representation (all values
    //     in range of 0-255) In the result, V is the MOST significant byte
    //
    // Parameters:
    //   r:
    //
    //   g:
    //
    //   b:
    public static int Rgb2Hsv(float r, float g, float b)
    {
        float num = 0f;
        if (g < b)
        {
            float num2 = g;
            g = b;
            b = num2;
            num = -1f;
        }

        if (r < g)
        {
            float num3 = r;
            r = g;
            g = num3;
            num = -1f / 3f - num;
        }

        float num4 = r - Math.Min(g, b);
        return (int)(255f * Math.Abs(num + (g - b) / (6f * num4 + 1E-20f))) | ((int)(255f * num4 / (r + 1E-20f)) << 8) | ((int)r << 16);
    }

    //
    // Summary:
    //     Converts given RGB value into it's respective HSV Representation (all values
    //     in range of 0-255) In the parameter, R is the most significant byte i.e. this
    //     is for RGB In the result, V is the LEAST significant byte
    //
    // Parameters:
    //   rgb:
    public static int Rgb2HSv(int rgb)
    {
        float num = 0f;
        int num2 = (rgb >> 16) & 0xFF;
        int num3 = (rgb >> 8) & 0xFF;
        int num4 = rgb & 0xFF;
        if (num3 < num4)
        {
            int num5 = num3;
            num3 = num4;
            num4 = num5;
            num = -1f;
        }

        if (num2 < num3)
        {
            int num6 = num2;
            num2 = num3;
            num3 = num6;
            num = -1f / 3f - num;
        }

        float num7 = num2 - Math.Min(num3, num4);
        return ((int)(255f * Math.Abs(num + (float)(num3 - num4) / (6f * num7 + 1E-20f))) << 16) | ((int)(255f * num7 / ((float)num2 + 1E-20f)) << 8) | num2;
    }

    //
    // Summary:
    //     Converts given RGB values into it's respective HSV Representation (all values
    //     in range of 0-255)
    //
    // Parameters:
    //   r:
    //
    //   g:
    //
    //   b:
    public static int[] RgbToHsvInts(int r, int g, int b)
    {
        float num = 0f;
        if (g < b)
        {
            int num2 = g;
            g = b;
            b = num2;
            num = -1f;
        }

        if (r < g)
        {
            int num3 = r;
            r = g;
            g = num3;
            num = -1f / 3f - num;
        }

        float num4 = r - Math.Min(g, b);
        return new int[3]
        {
            (int)(255f * Math.Abs(num + (float)(g - b) / (6f * num4 + 1E-20f))),
            (int)(255f * num4 / ((float)r + 1E-20f)),
            r
        };
    }

    //
    // Summary:
    //     Converts given HSV value into it's respective RGB Representation (all values
    //     in range of 0-255) R is the most significant byte i.e. this is RGB
    //
    // Parameters:
    //   hsv:
    public static int Hsv2Rgb(int hsv)
    {
        int num = hsv & 0xFF;
        if (num == 0)
        {
            return 0;
        }

        int num2 = (hsv >> 8) & 0xFF;
        if (num2 == 0)
        {
            return (num << 16) | (num << 8) | num;
        }

        int num3 = (hsv >> 16) & 0xFF;
        int num4 = num3 / 43;
        int num5 = (num3 - num4 * 43) * 6;
        int num6 = num * (255 - num2) >> 8;
        int num7 = num * (255 - (num2 * num5 >> 8)) >> 8;
        int num8 = num * (255 - (num2 * (255 - num5) >> 8)) >> 8;
        return num4 switch
        {
            0 => (num << 16) | (num8 << 8) | num6,
            1 => (num7 << 16) | (num << 8) | num6,
            2 => (num6 << 16) | (num << 8) | num8,
            3 => (num6 << 16) | (num7 << 8) | num,
            4 => (num8 << 16) | (num6 << 8) | num,
            _ => (num << 16) | (num6 << 8) | num7,
        };
    }

    //
    // Summary:
    //     Converts given HSV values into it's respective RGB Representation (all values
    //     in range of 0-255) R is the most significant byte i.e. this is RGB
    //
    // Parameters:
    //   h:
    //
    //   s:
    //
    //   v:
    public static int HsvToRgb(int h, int s, int v)
    {
        if (v == 0)
        {
            return 0;
        }

        if (s == 0)
        {
            return (v << 16) | (v << 8) | v;
        }

        int num = h / 43;
        int num2 = (h - num * 43) * 6;
        int num3 = v * (255 - s) >> 8;
        int num4 = v * (255 - (s * num2 >> 8)) >> 8;
        int num5 = v * (255 - (s * (255 - num2) >> 8)) >> 8;
        return num switch
        {
            0 => (v << 16) | (num5 << 8) | num3,
            1 => (num4 << 16) | (v << 8) | num3,
            2 => (num3 << 16) | (v << 8) | num5,
            3 => (num3 << 16) | (num4 << 8) | v,
            4 => (num5 << 16) | (num3 << 8) | v,
            _ => (v << 16) | (num3 << 8) | num4,
        };
    }

    //
    // Summary:
    //     Returns a fully opaque gray color with given brightness
    //
    // Parameters:
    //   brightness:
    public static int GrayscaleColor(byte brightness)
    {
        return -16777216 | (brightness << 16) | (brightness << 8) | brightness;
    }

    //
    // Summary:
    //     Converts given HSB values into it's respective ARGB Representation (all values
    //     in range of 0-255, alpha always 255) R is the LEAST significant byte i.e. the
    //     result is BGR
    //
    // Parameters:
    //   h:
    //
    //   s:
    //
    //   v:
    public static int HsvToRgba(int h, int s, int v)
    {
        return HsvToRgba(h, s, v, -16777216);
    }

    //
    // Summary:
    //     Converts given HSV values into its respective ARGB Representation (all values
    //     in range of 0-255) R is the LEAST significant byte i.e. the result is BGR
    //
    // Parameters:
    //   h:
    //
    //   s:
    //
    //   v:
    //
    //   a:
    public static int HsvToRgba(int h, int s, int v, int a)
    {
        if (v == 0)
        {
            return a << 24;
        }

        if (s == 0)
        {
            return (a << 24) | (v << 16) | (v << 8) | v;
        }

        int num = h / 43;
        int num2 = (h - num * 43) * 6;
        int num3 = v * (255 - s) >> 8;
        int num4 = v * (255 - (s * num2 >> 8)) >> 8;
        int num5 = v * (255 - (s * (255 - num2) >> 8)) >> 8;
        return num switch
        {
            0 => (a << 24) | (num3 << 16) | (num5 << 8) | v,
            1 => (a << 24) | (num3 << 16) | (v << 8) | num4,
            2 => (a << 24) | (num5 << 16) | (v << 8) | num3,
            3 => (a << 24) | (v << 16) | (num4 << 8) | num3,
            4 => (a << 24) | (v << 16) | (num3 << 8) | num5,
            _ => (a << 24) | (num4 << 16) | (num3 << 8) | v,
        };
    }

    //
    // Summary:
    //     Converts given HSV values into its respective RGB representation (all values
    //     in range of 0-255) R is the first byte in the resulting array
    //
    // Parameters:
    //   h:
    //
    //   s:
    //
    //   v:
    public static int[] Hsv2RgbInts(int h, int s, int v)
    {
        if (s == 0 || v == 0)
        {
            return new int[3] { v, v, v };
        }

        int num = h / 43;
        int num2 = (h - num * 43) * 6;
        int num3 = v * (255 - s) >> 8;
        int num4 = v * (255 - (s * num2 >> 8)) >> 8;
        int num5 = v * (255 - (s * (255 - num2) >> 8)) >> 8;
        return num switch
        {
            0 => new int[3] { v, num5, num3 },
            1 => new int[3] { num4, v, num3 },
            2 => new int[3] { num3, v, num5 },
            3 => new int[3] { num3, num4, v },
            4 => new int[3] { num5, num3, v },
            _ => new int[3] { v, num3, num4 },
        };
    }

    //
    // Summary:
    //     Converts given HSVA values into its respective RGBA Representation (all values
    //     in range of 0-255) R is the first byte in the resulting array
    //
    // Parameters:
    //   hsva:
    public static byte[] HSVa2RGBaBytes(byte[] hsva)
    {
        int num = hsva[0];
        int num2 = hsva[1];
        int num3 = hsva[2];
        if (num2 == 0 || num3 == 0)
        {
            return new byte[4]
            {
                (byte)num3,
                (byte)num3,
                (byte)num3,
                hsva[3]
            };
        }

        int num4 = num / 43;
        int num5 = (hsva[0] - num4 * 43) * 6;
        int num6 = num3 * (255 - num2) >> 8;
        int num7 = num3 * (255 - (num2 * num5 >> 8)) >> 8;
        int num8 = num3 * (255 - (num2 * (255 - num5) >> 8)) >> 8;
        return num4 switch
        {
            0 => new byte[4]
            {
                hsva[2],
                (byte)num8,
                (byte)num6,
                hsva[3]
            },
            1 => new byte[4]
            {
                (byte)num7,
                hsva[2],
                (byte)num6,
                hsva[3]
            },
            2 => new byte[4]
            {
                (byte)num6,
                hsva[2],
                (byte)num8,
                hsva[3]
            },
            3 => new byte[4]
            {
                (byte)num6,
                (byte)num7,
                hsva[2],
                hsva[3]
            },
            4 => new byte[4]
            {
                (byte)num8,
                (byte)num6,
                hsva[2],
                hsva[3]
            },
            _ => new byte[4]
            {
                hsva[2],
                (byte)num6,
                (byte)num7,
                hsva[3]
            },
        };
    }

    public static int[] getIncandescenceColor(int temperature)
    {
        if (temperature < 520)
        {
            return new int[4];
        }

        return new int[4]
        {
            Math.Max(0, Math.Min(255, (temperature - 500) * 255 / 400)),
            Math.Max(0, Math.Min(255, (temperature - 900) * 255 / 200)),
            Math.Max(0, Math.Min(255, (temperature - 1100) * 255 / 200)),
            Math.Max(0, Math.Min(255, (temperature - 525) / 2))
        };
    }

    public static float[] GetIncandescenceColorAsColor4f(int temperature)
    {
        if (temperature < 500)
        {
            return new float[4];
        }

        return new float[4]
        {
            Math.Max(0f, Math.Min(1f, (float)(temperature - 500) / 400f)),
            Math.Max(0f, Math.Min(1f, (float)(temperature - 900) / 200f)),
            Math.Max(0f, Math.Min(1f, (float)(temperature - 1100) / 200f)),
            Math.Max(0f, Math.Min(1f, (float)(temperature - 525) / 2f))
        };
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
