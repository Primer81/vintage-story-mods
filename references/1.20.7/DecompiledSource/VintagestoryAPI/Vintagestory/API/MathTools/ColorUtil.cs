using System;
using System.Globalization;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Many utility methods and fields for working with colors
/// </summary>
public class ColorUtil
{
	/// <summary>
	/// Converts HSV (extracted from light and lightSat) to RGBA
	/// </summary>
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
			for (int j = 0; j < blockLightlevels.Length; j++)
			{
				blockLightlevelsByte[j] = (byte)(blockLightlevels[j] * 255.999f);
			}
			sunLightlevelsByte = new byte[sunLightlevels.Length];
			for (int i = 0; i < sunLightlevels.Length; i++)
			{
				sunLightlevelsByte[i] = (byte)(sunLightlevels[i] * 255.999f);
			}
		}

		public int ToRgba(uint light, int lightSat)
		{
			byte v = blockLightlevelsByte[(light >> 5) & 0x1F];
			byte a = sunLightlevelsByte[light & 0x1F];
			if (lightSat == 0)
			{
				return (a << 24) | (v << 16) | (v << 8) | v;
			}
			byte num = hueLevels[light >> 10];
			byte s = satLevels[lightSat];
			int region = num / 43;
			int remainder = (num - region * 43) * 6;
			int p = v * (255 - s) >> 8;
			int q = v * (255 - (s * remainder >> 8)) >> 8;
			int t = v * (255 - (s * (255 - remainder) >> 8)) >> 8;
			return region switch
			{
				0 => (a << 24) | (p << 16) | (t << 8) | v, 
				1 => (a << 24) | (p << 16) | (v << 8) | q, 
				2 => (a << 24) | (t << 16) | (v << 8) | p, 
				3 => (a << 24) | (v << 16) | (q << 8) | p, 
				4 => (a << 24) | (v << 16) | (p << 8) | t, 
				_ => (a << 24) | (q << 16) | (p << 8) | v, 
			};
		}
	}

	/// <summary>
	/// Amount of bits per block that are available to store the hue value
	/// </summary>
	public const int HueBits = 6;

	/// <summary>
	/// Amount of bits per block that are available to store the saturation value
	/// </summary>
	public const int SatBits = 3;

	/// <summary>
	/// Amount of bits per block that are available to store the brightness value
	/// </summary>
	public const int BrightBits = 5;

	public const int HueMul = 4;

	public const int SatMul = 32;

	public const int BrightMul = 8;

	public static int HueQuantities = (int)Math.Pow(2.0, 6.0);

	public static int SatQuantities = (int)Math.Pow(2.0, 3.0);

	public static int BrightQuantities = (int)Math.Pow(2.0, 5.0);

	/// <summary>
	/// 255 &lt;&lt; 24
	/// </summary>
	public const int OpaqueAlpha = -16777216;

	/// <summary>
	/// ~(255 &lt;&lt; 24)
	/// </summary>
	public const int ClearAlpha = 16777215;

	/// <summary>
	/// White opaque color as normalized float values (0..1)
	/// </summary>
	public static readonly Vec3f WhiteRgbVec = new Vec3f(1f, 1f, 1f);

	/// <summary>
	/// White opaque color as normalized float values (0..1)
	/// </summary>
	public static readonly Vec4f WhiteArgbVec = new Vec4f(1f, 1f, 1f, 1f);

	/// <summary>
	/// White opaque color as normalized float values (0..1)
	/// </summary>
	public static readonly float[] WhiteArgbFloat = new float[4] { 1f, 1f, 1f, 1f };

	/// <summary>
	/// White opaque color as normalized float values (0..1)
	/// </summary>
	public static readonly double[] WhiteArgbDouble = new double[4] { 1.0, 1.0, 1.0, 1.0 };

	/// <summary>
	/// White opaque argb color as bytes (0..255)
	/// </summary>
	public static readonly byte[] WhiteArgbBytes = new byte[4] { 255, 255, 255, 255 };

	/// <summary>
	/// White opaque ahsv color as bytes (0..255)
	/// </summary>
	public static readonly byte[] WhiteAhsvBytes = new byte[4] { 255, 0, 0, 255 };

	/// <summary>
	/// White opaque argb color
	/// </summary>
	public const int WhiteArgb = -1;

	/// <summary>
	/// White opaque AHSV color
	/// </summary>
	public static readonly int WhiteAhsl = ToRgba(255, 255, 0, 0);

	/// <summary>
	/// Black opaque rgb color
	/// </summary>
	public static readonly int BlackArgb = ToRgba(255, 0, 0, 0);

	/// <summary>
	/// Black opaque rgb color
	/// </summary>
	public static readonly Vec3f BlackRgbVec = new Vec3f(0f, 0f, 0f);

	/// <summary>
	/// Black opaque rgb color
	/// </summary>
	public static readonly Vec4f BlackArgbVec = new Vec4f(0f, 0f, 0f, 255f);

	/// <summary>
	/// White opaque color as normalized float values (0..1)
	/// </summary>
	public static readonly double[] BlackArgbDouble = new double[4] { 0.0, 0.0, 0.0, 1.0 };

	/// <summary>
	/// Reverses the RGB channels, but leaves alpha untouched. Basically turns RGBA into BGRA and vice versa
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Reverses the RGB channels, but leaves alpha untouched. Basically turns RGBA into BGRA and vice versa
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
	public static int ReverseColorBytes(int color)
	{
		return (int)(color & 0xFF000000u) | ((color >> 16) & 0xFF) | (((color >> 8) & 0xFF) << 8) | ((color & 0xFF) << 16);
	}

	/// <summary>
	/// Splits up a 32bit int color into 4 1 byte components, in BGRA order (Alpha channel at the highest 8 bits)
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Splits up a 32bit int color into 4 1 byte components, in RGBA order
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Returns a 4 element rgb float with values between 0..1
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Returns a 4 element rgb float with values between 0..1
	/// </summary>
	/// <param name="color"></param>
	/// <param name="outVal"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Care: the returned value is in true RGBA order, not BGRA as used for example by VS particle system.  Therefore, depending on use, calling code may need to exchange the r and b parameters to see correct colors rendered in-game.
	/// </summary>
	public static int ColorFromRgba(int r, int g, int b, int a)
	{
		return r | (g << 8) | (b << 16) | (a << 24);
	}

	public static int FromRGBADoubles(double[] rgba)
	{
		return ColorFromRgba((int)(rgba[0] * 255.0), (int)(rgba[1] * 255.0), (int)(rgba[2] * 255.0), (int)(rgba[3] * 255.0));
	}

	/// <summary>
	/// Returns a 4 element rgb double with values between 0..1
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Multiplies two colors together: c=(a*b)/255
	/// </summary>
	/// <param name="color1"></param>
	/// <param name="color2"></param>
	/// <returns></returns>
	public static byte[] ColorMultiply(byte[] color1, byte[] color2)
	{
		return new byte[3]
		{
			(byte)(color1[0] * color2[0] / 255),
			(byte)(color1[1] * color2[1] / 255),
			(byte)(color1[2] * color2[2] / 255)
		};
	}

	/// <summary>
	/// Multiplies two colors together c=(a*b)/255
	/// </summary>
	/// <param name="color"></param>
	/// <param name="color2"></param>
	/// <returns></returns>
	public static int ColorMultiplyEach(int color, int color2)
	{
		return (((color >> 24) & 0xFF) * ((color2 >> 24) & 0xFF) / 255 << 24) | (((color >> 16) & 0xFF) * ((color2 >> 16) & 0xFF) / 255 << 16) | (((color >> 8) & 0xFF) * ((color2 >> 8) & 0xFF) / 255 << 8) | ((color & 0xFF) * (color2 & 0xFF) / 255);
	}

	/// <summary>
	/// Multiplies a float value to the rgb color channels, leaves alpha channel unchanged
	/// </summary>
	/// <param name="color"></param>
	/// <param name="multiplier"></param>
	/// <returns></returns>
	public static int ColorMultiply3(int color, float multiplier)
	{
		return (int)(color & 0xFF000000u) | ((int)((float)((color >> 16) & 0xFF) * multiplier) << 16) | ((int)((float)((color >> 8) & 0xFF) * multiplier) << 8) | (int)((float)(color & 0xFF) * multiplier);
	}

	/// <summary>
	/// Multiplies a float value to the rgb color channels, leaves alpha channel unchanged. Makes sure the multiplied value stays within the 0..255 range
	/// </summary>
	/// <param name="color"></param>
	/// <param name="multiplier"></param>
	/// <returns></returns>
	public static int ColorMultiply3Clamped(int color, float multiplier)
	{
		return (int)(color & 0xFF000000u) | ((int)GameMath.Clamp((float)((color >> 16) & 0xFF) * multiplier, 0f, 255f) << 16) | ((int)GameMath.Clamp((float)((color >> 8) & 0xFF) * multiplier, 0f, 255f) << 8) | (int)GameMath.Clamp((float)(color & 0xFF) * multiplier, 0f, 255f);
	}

	/// <summary>
	/// Multiplies a float value to the rgb color channels
	/// </summary>
	/// <param name="color"></param>
	/// <param name="blueMul"></param>
	/// <param name="redMul"></param>
	/// <param name="greenMul"></param>
	/// <param name="alphaMul"></param>
	/// <returns></returns>
	public static int ColorMultiply4(int color, float redMul, float greenMul, float blueMul, float alphaMul)
	{
		return ((int)((float)((color >> 24) & 0xFF) * alphaMul) << 24) | ((int)((float)((color >> 16) & 0xFF) * blueMul) << 16) | ((int)((float)((color >> 8) & 0xFF) * greenMul) << 8) | (int)((float)(color & 0xFF) * redMul);
	}

	/// <summary>
	/// Multiplies a float value to every color channel including the alpha component.
	/// </summary>
	/// <param name="color"></param>
	/// <param name="multiplier"></param>
	/// <returns></returns>
	public static int ColorMultiply4(int color, float multiplier)
	{
		return ((int)((float)((color >> 24) & 0xFF) * multiplier) << 24) | ((int)((float)((color >> 16) & 0xFF) * multiplier) << 16) | ((int)((float)((color >> 8) & 0xFF) * multiplier) << 8) | (int)((float)(color & 0xFF) * multiplier);
	}

	/// <summary>
	/// Averages several colors together in RGB space
	/// </summary>
	/// <param name="colors"></param>
	/// <param name="weights"></param>
	/// <returns></returns>
	public static int ColorAverage(int[] colors, float[] weights)
	{
		int r = 0;
		int g = 0;
		int b = 0;
		for (int i = 0; i < colors.Length; i++)
		{
			float w = weights[i];
			if (w != 0f)
			{
				r += (int)(w * (float)((colors[i] >> 16) & 0xFF));
				g += (int)(w * (float)((colors[i] >> 8) & 0xFF));
				b += (int)(w * (float)(colors[i] & 0xFF));
			}
		}
		return (Math.Min(255, r) << 16) + (Math.Min(255, g) << 8) + Math.Min(255, b);
	}

	/// <summary>
	/// Overlays rgb2 over rgb1
	/// When c2weight = 0 resulting color is color1, when c2weight = 1 then resulting color is color2
	/// Resulting color alpha value is 100% color1 alpha
	/// </summary>
	/// <param name="rgb1"></param>
	/// <param name="rgb2"></param>
	/// <param name="c2weight"></param>
	/// <returns></returns>
	public static int ColorOverlay(int rgb1, int rgb2, float c2weight)
	{
		return (((rgb1 >> 24) & 0xFF) << 24) | ((int)((float)((rgb1 >> 16) & 0xFF) * (1f - c2weight) + c2weight * (float)((rgb2 >> 16) & 0xFF)) << 16) | ((int)((float)((rgb1 >> 8) & 0xFF) * (1f - c2weight) + c2weight * (float)((rgb2 >> 8) & 0xFF)) << 8) | (int)((float)(rgb1 & 0xFF) * (1f - c2weight) + c2weight * (float)(rgb2 & 0xFF));
	}

	/// <summary>
	/// Overlays rgb1 on top of rgb2, based on their alpha values
	/// </summary>
	/// <param name="rgb1"></param>
	/// <param name="rgb2"></param>
	/// <returns></returns>
	public static int ColorOver(int rgb1, int rgb2)
	{
		float a1 = (float)((rgb1 >> 24) & 0xFF) / 255f;
		float a2 = (float)((rgb2 >> 24) & 0xFF) / 255f;
		if (a1 == 0f && a2 == 0f)
		{
			return 0;
		}
		return ((int)(255f * (a1 + a2 * (1f - a1))) << 24) | (ValueOverlay((rgb1 >> 16) & 0xFF, a1, (rgb2 >> 16) & 0xFF, a2) << 16) | (ValueOverlay((rgb1 >> 8) & 0xFF, a1, (rgb2 >> 8) & 0xFF, a2) << 8) | ValueOverlay(rgb1 & 0xFF, a1, rgb2 & 0xFF, a2);
	}

	private static int ValueOverlay(int c1, float a1, int c2, float a2)
	{
		return (int)(((float)c1 * a1 + (float)c2 * a2 * (1f - a1)) / (a1 + a2 * (1f - a1)));
	}

	/// <summary>
	/// Combines two HSV colors by converting them to rgb then back to hsv. Uses the brightness as a weighting factor. Also leaves the brightness at the max of both hsv colors.
	/// </summary>
	/// <param name="h1"></param>
	/// <param name="s1"></param>
	/// <param name="v1"></param>
	/// <param name="h2"></param>
	/// <param name="s2"></param>
	/// <param name="v2"></param>
	/// <returns>Combined HSV Color</returns>
	public static int[] ColorCombineHSV(int h1, int s1, int v1, int h2, int s2, int v2)
	{
		int[] rgb1 = Hsv2RgbInts(h1, s1, v1);
		int[] rgb2 = Hsv2RgbInts(h2, s2, v2);
		float leftweight = (float)v1 / (float)(v1 + v2);
		float rightweight = 1f - leftweight;
		int[] array = RgbToHsvInts((int)((float)rgb1[0] * leftweight + (float)rgb2[0] * rightweight), (int)((float)rgb1[1] * leftweight + (float)rgb2[1] * rightweight), (int)((float)rgb1[2] * leftweight + (float)rgb2[2] * rightweight));
		array[2] = Math.Max(v1, v2);
		return array;
	}

	/// <summary>
	/// Removes HSV2 from HSV1 by converting them to rgb then back to hsv. Uses the brightness as a weighting factor. Leaves brightness unchanged.
	/// </summary>
	/// <param name="h1"></param>
	/// <param name="s1"></param>
	/// <param name="v1"></param>
	/// <param name="h2"></param>
	/// <param name="s2"></param>
	/// <param name="v2"></param>
	/// <returns></returns>
	public static int[] ColorSubstractHSV(int h1, int s1, int v1, int h2, int s2, int v2)
	{
		int[] rgb1 = Hsv2RgbInts(h1, s1, v1);
		int[] rgb2 = Hsv2RgbInts(h2, s2, v2);
		float leftweight = (float)(v1 + v2) / (float)v1;
		float rightweight = 1f - (float)v1 / (float)(v1 + v2);
		int[] array = RgbToHsvInts((int)((float)rgb1[0] * leftweight - (float)rgb2[0] * rightweight), (int)((float)rgb1[1] * leftweight - (float)rgb2[1] * rightweight), (int)((float)rgb1[2] * leftweight - (float)rgb2[2] * rightweight));
		array[2] = v1;
		return array;
	}

	/// <summary>
	/// Pack the 4 color components into a single ARGB 32bit int
	/// </summary>
	/// <param name="a"></param>
	/// <param name="r"></param>
	/// <param name="g"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static int ToRgba(int a, int r, int g, int b)
	{
		return (a << 24) | (r << 16) | (g << 8) | b;
	}

	/// <summary>
	/// Returns alpha value of given color
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
	public static byte ColorA(int color)
	{
		return (byte)(color >> 24);
	}

	/// <summary>
	/// Returns red value of given color
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
	public static byte ColorR(int color)
	{
		return (byte)(color >> 16);
	}

	/// <summary>
	/// Returns green value of given color
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
	public static byte ColorG(int color)
	{
		return (byte)(color >> 8);
	}

	/// <summary>
	/// Returns blue value of given color
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
	public static byte ColorB(int color)
	{
		return (byte)color;
	}

	/// <summary>
	/// Returns human a readable string of given color
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
	public static string ColorToString(int color)
	{
		return ColorR(color) + ", " + ColorG(color) + ", " + ColorB(color) + ", " + ColorA(color);
	}

	/// <summary>
	/// Turn a string hex color (with #) into a single int
	/// </summary>
	/// <param name="hex"></param>
	/// <returns></returns>
	public static int Hex2Int(string hex)
	{
		return int.Parse(hex.Substring(1), NumberStyles.HexNumber);
	}

	/// <summary>
	/// Turn a color int into its string hex version, including preceeding #
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
	public static string Int2Hex(int color)
	{
		return $"#{ColorR(color):X2}{ColorG(color):X2}{ColorB(color):X2}";
	}

	public static string Int2HexBGR(int color)
	{
		return string.Format("#{2:X2}{1:X2}{0:X2}", ColorR(color), ColorG(color), ColorB(color));
	}

	/// <summary>
	/// Turn a color int into its string hex version, including preceeding #, including alpha channel
	/// </summary>
	/// <param name="color"></param>
	/// <returns></returns>
	public static string Int2HexRgba(int color)
	{
		return $"#{ColorR(color):X2}{ColorG(color):X2}{ColorB(color):X2}{ColorA(color):X2}";
	}

	public static string Doubles2Hex(double[] color)
	{
		return $"#{(int)(255.0 * color[0]):X2}{(int)(255.0 * color[1]):X2}{(int)(255.0 * color[2]):X2}";
	}

	/// <summary>
	/// Parses a hex string as an rgb(a) color and returns an array of colors normalized fom 0..1 for use with Cairo. E.g. turns #FF0000 into double[1, 0, 0, 1] and #00FF00CC into double[0, 1, 0, 0.8]
	/// </summary>
	/// <param name="hex"></param>
	/// <returns></returns>
	public static double[] Hex2Doubles(string hex)
	{
		int r = int.Parse(hex.Substring(1, 2), NumberStyles.HexNumber);
		int g = int.Parse(hex.Substring(3, 2), NumberStyles.HexNumber);
		int b = int.Parse(hex.Substring(5, 2), NumberStyles.HexNumber);
		int a = ((hex.Length < 8) ? 255 : int.Parse(hex.Substring(7, 2), NumberStyles.HexNumber));
		return new double[4]
		{
			(double)r / 255.0,
			(double)g / 255.0,
			(double)b / 255.0,
			(double)a / 255.0
		};
	}

	public static double[] Hex2Doubles(string hex, double opacityRel)
	{
		int r = int.Parse(hex.Substring(1, 2), NumberStyles.HexNumber);
		int g = int.Parse(hex.Substring(3, 2), NumberStyles.HexNumber);
		int b = int.Parse(hex.Substring(5, 2), NumberStyles.HexNumber);
		return new double[4]
		{
			(double)r / 255.0,
			(double)g / 255.0,
			(double)b / 255.0,
			opacityRel
		};
	}

	/// <summary>
	/// Converts given RGB values into it's respective HSV Representation (all values in range of 0-255)
	/// In the result, V is the MOST significant byte
	/// </summary>
	/// <param name="r"></param>
	/// <param name="g"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static int Rgb2Hsv(float r, float g, float b)
	{
		float K = 0f;
		if (g < b)
		{
			float num = g;
			g = b;
			b = num;
			K = -1f;
		}
		if (r < g)
		{
			float num2 = r;
			r = g;
			g = num2;
			K = -1f / 3f - K;
		}
		float chroma = r - Math.Min(g, b);
		return (int)(255f * Math.Abs(K + (g - b) / (6f * chroma + 1E-20f))) | ((int)(255f * chroma / (r + 1E-20f)) << 8) | ((int)r << 16);
	}

	/// <summary>
	/// Converts given RGB value into it's respective HSV Representation (all values in range of 0-255)
	/// In the parameter, R is the most significant byte i.e. this is for RGB
	/// In the result, V is the LEAST significant byte
	/// </summary>
	/// <param name="rgb"></param>
	/// <returns></returns>
	public static int Rgb2HSv(int rgb)
	{
		float K = 0f;
		int r = (rgb >> 16) & 0xFF;
		int g = (rgb >> 8) & 0xFF;
		int b = rgb & 0xFF;
		if (g < b)
		{
			int num = g;
			g = b;
			b = num;
			K = -1f;
		}
		if (r < g)
		{
			int num2 = r;
			r = g;
			g = num2;
			K = -1f / 3f - K;
		}
		float chroma = r - Math.Min(g, b);
		return ((int)(255f * Math.Abs(K + (float)(g - b) / (6f * chroma + 1E-20f))) << 16) | ((int)(255f * chroma / ((float)r + 1E-20f)) << 8) | r;
	}

	/// <summary>
	/// Converts given RGB values into it's respective HSV Representation (all values in range of 0-255)
	/// </summary>
	/// <param name="r"></param>
	/// <param name="g"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static int[] RgbToHsvInts(int r, int g, int b)
	{
		float K = 0f;
		if (g < b)
		{
			int num = g;
			g = b;
			b = num;
			K = -1f;
		}
		if (r < g)
		{
			int num2 = r;
			r = g;
			g = num2;
			K = -1f / 3f - K;
		}
		float chroma = r - Math.Min(g, b);
		return new int[3]
		{
			(int)(255f * Math.Abs(K + (float)(g - b) / (6f * chroma + 1E-20f))),
			(int)(255f * chroma / ((float)r + 1E-20f)),
			r
		};
	}

	/// <summary>
	/// Converts given HSV value into it's respective RGB Representation (all values in range of 0-255)
	/// R is the most significant byte i.e. this is RGB
	/// </summary>
	/// <param name="hsv"></param>
	/// <returns></returns>
	public static int Hsv2Rgb(int hsv)
	{
		int v = hsv & 0xFF;
		if (v == 0)
		{
			return 0;
		}
		int s = (hsv >> 8) & 0xFF;
		if (s == 0)
		{
			return (v << 16) | (v << 8) | v;
		}
		int num = (hsv >> 16) & 0xFF;
		int region = num / 43;
		int remainder = (num - region * 43) * 6;
		int p = v * (255 - s) >> 8;
		int q = v * (255 - (s * remainder >> 8)) >> 8;
		int t = v * (255 - (s * (255 - remainder) >> 8)) >> 8;
		return region switch
		{
			0 => (v << 16) | (t << 8) | p, 
			1 => (q << 16) | (v << 8) | p, 
			2 => (p << 16) | (v << 8) | t, 
			3 => (p << 16) | (q << 8) | v, 
			4 => (t << 16) | (p << 8) | v, 
			_ => (v << 16) | (p << 8) | q, 
		};
	}

	/// <summary>
	/// Converts given HSV values into it's respective RGB Representation (all values in range of 0-255)
	/// R is the most significant byte i.e. this is RGB
	/// </summary>
	/// <param name="h"></param>
	/// <param name="s"></param>
	/// <param name="v"></param>
	/// <returns></returns>
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
		int region = h / 43;
		int remainder = (h - region * 43) * 6;
		int p = v * (255 - s) >> 8;
		int q = v * (255 - (s * remainder >> 8)) >> 8;
		int t = v * (255 - (s * (255 - remainder) >> 8)) >> 8;
		return region switch
		{
			0 => (v << 16) | (t << 8) | p, 
			1 => (q << 16) | (v << 8) | p, 
			2 => (p << 16) | (v << 8) | t, 
			3 => (p << 16) | (q << 8) | v, 
			4 => (t << 16) | (p << 8) | v, 
			_ => (v << 16) | (p << 8) | q, 
		};
	}

	/// <summary>
	/// Returns a fully opaque gray color with given brightness
	/// </summary>
	/// <param name="brightness"></param>
	/// <returns></returns>
	public static int GrayscaleColor(byte brightness)
	{
		return -16777216 | (brightness << 16) | (brightness << 8) | brightness;
	}

	/// <summary>
	/// Converts given HSB values into it's respective ARGB Representation (all values in range of 0-255, alpha always 255)
	/// R is the LEAST significant byte i.e. the result is BGR
	/// </summary>
	/// <param name="h"></param>
	/// <param name="s"></param>
	/// <param name="v"></param>
	/// <returns></returns>
	public static int HsvToRgba(int h, int s, int v)
	{
		return HsvToRgba(h, s, v, -16777216);
	}

	/// <summary>
	/// Converts given HSV values into its respective ARGB Representation (all values in range of 0-255)
	/// R is the LEAST significant byte i.e. the result is BGR
	/// </summary>
	/// <param name="h"></param>
	/// <param name="s"></param>
	/// <param name="v"></param>
	/// <param name="a"></param>
	/// <returns></returns>
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
		int region = h / 43;
		int remainder = (h - region * 43) * 6;
		int p = v * (255 - s) >> 8;
		int q = v * (255 - (s * remainder >> 8)) >> 8;
		int t = v * (255 - (s * (255 - remainder) >> 8)) >> 8;
		return region switch
		{
			0 => (a << 24) | (p << 16) | (t << 8) | v, 
			1 => (a << 24) | (p << 16) | (v << 8) | q, 
			2 => (a << 24) | (t << 16) | (v << 8) | p, 
			3 => (a << 24) | (v << 16) | (q << 8) | p, 
			4 => (a << 24) | (v << 16) | (p << 8) | t, 
			_ => (a << 24) | (q << 16) | (p << 8) | v, 
		};
	}

	/// <summary>
	/// Converts given HSV values into its respective RGB representation (all values in range of 0-255)
	/// R is the first byte in the resulting array
	/// </summary>
	/// <param name="h"></param>
	/// <param name="s"></param>
	/// <param name="v"></param>
	/// <returns></returns>
	public static int[] Hsv2RgbInts(int h, int s, int v)
	{
		if (s == 0 || v == 0)
		{
			return new int[3] { v, v, v };
		}
		int region = h / 43;
		int remainder = (h - region * 43) * 6;
		int p = v * (255 - s) >> 8;
		int q = v * (255 - (s * remainder >> 8)) >> 8;
		int t = v * (255 - (s * (255 - remainder) >> 8)) >> 8;
		return region switch
		{
			0 => new int[3] { v, t, p }, 
			1 => new int[3] { q, v, p }, 
			2 => new int[3] { p, v, t }, 
			3 => new int[3] { p, q, v }, 
			4 => new int[3] { t, p, v }, 
			_ => new int[3] { v, p, q }, 
		};
	}

	/// <summary>
	/// Converts given HSVA values into its respective RGBA Representation (all values in range of 0-255)
	/// R is the first byte in the resulting array
	/// </summary>
	/// <param name="hsva"></param>
	/// <returns></returns>
	public static byte[] HSVa2RGBaBytes(byte[] hsva)
	{
		int h = hsva[0];
		int s = hsva[1];
		int v = hsva[2];
		if (s == 0 || v == 0)
		{
			return new byte[4]
			{
				(byte)v,
				(byte)v,
				(byte)v,
				hsva[3]
			};
		}
		int region = h / 43;
		int remainder = (hsva[0] - region * 43) * 6;
		int p = v * (255 - s) >> 8;
		int q = v * (255 - (s * remainder >> 8)) >> 8;
		int t = v * (255 - (s * (255 - remainder) >> 8)) >> 8;
		return region switch
		{
			0 => new byte[4]
			{
				hsva[2],
				(byte)t,
				(byte)p,
				hsva[3]
			}, 
			1 => new byte[4]
			{
				(byte)q,
				hsva[2],
				(byte)p,
				hsva[3]
			}, 
			2 => new byte[4]
			{
				(byte)p,
				hsva[2],
				(byte)t,
				hsva[3]
			}, 
			3 => new byte[4]
			{
				(byte)p,
				(byte)q,
				hsva[2],
				hsva[3]
			}, 
			4 => new byte[4]
			{
				(byte)t,
				(byte)p,
				hsva[2],
				hsva[3]
			}, 
			_ => new byte[4]
			{
				hsva[2],
				(byte)p,
				(byte)q,
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
