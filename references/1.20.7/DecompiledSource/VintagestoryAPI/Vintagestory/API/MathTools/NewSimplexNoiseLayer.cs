using System;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.MathTools;

public static class NewSimplexNoiseLayer
{
	public const double OldToNewFrequency = 0.6123724356957945;

	public const double MaxYSlope_ImprovedXZ = 5.0;

	private const long PrimeX = 5910200641878280303L;

	private const long PrimeY = 6452764530575939509L;

	private const long PrimeZ = 6614699811220273867L;

	private const long HashMultiplier = 6026932503003350773L;

	private const long SeedFlip3D = -5968755714895566377L;

	private const double Root3Over3 = 0.577350269189626;

	private const double FallbackRotate3 = 2.0 / 3.0;

	private const double Rotate3Orthogonalizer = -0.211324865405187;

	private const int NGrads3DExponent = 8;

	private const int NGrads3D = 256;

	private const double Normalizer3D = 0.2781926117527186;

	private static readonly float[] Gradients3D;

	public static float Evaluate_ImprovedXZ(long seed, double x, double y, double z)
	{
		double num = x + z;
		double s2 = num * -0.211324865405187;
		double yy = y * 0.577350269189626;
		double xr = x + s2 + yy;
		double zr = z + s2 + yy;
		double yr = num * -0.577350269189626 + yy;
		return Noise3_UnrotatedBase(seed, xr, yr, zr);
	}

	public static float Evaluate_FallbackOrientation(long seed, double x, double y, double z)
	{
		double r = 2.0 / 3.0 * (x + y + z);
		double xr = x - r;
		double yr = y - r;
		double zr = z - r;
		return Noise3_UnrotatedBase(seed, xr, yr, zr);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float Noise3_UnrotatedBase(long seed, double xr, double yr, double zr)
	{
		int xrb = (int)Math.Floor(xr);
		int yrb = (int)Math.Floor(yr);
		int zrb = (int)Math.Floor(zr);
		float xi = (float)(xr - (double)xrb);
		float yi = (float)(yr - (double)yrb);
		float zi = (float)(zr - (double)zrb);
		long xrbp = xrb * 5910200641878280303L;
		long yrbp = yrb * 6452764530575939509L;
		long zrbp = zrb * 6614699811220273867L;
		long seed2 = seed ^ -5968755714895566377L;
		int xNMask = (int)(-0.5f - xi);
		int yNMask = (int)(-0.5f - yi);
		int zNMask = (int)(-0.5f - zi);
		float x0 = xi + (float)xNMask;
		float y0 = yi + (float)yNMask;
		float z0 = zi + (float)zNMask;
		float a0 = 0.75f - x0 * x0 - y0 * y0 - z0 * z0;
		long hash0 = HashPrimes(seed, xrbp + (xNMask & 0x5205402B9270C86FL), yrbp + (yNMask & 0x598CD327003817B5L), zrbp + (zNMask & 0x5BCC226E9FA0BACBL));
		float value = a0 * a0 * (a0 * a0) * Grad(hash0, x0, y0, z0);
		float x1 = xi - 0.5f;
		float y1 = yi - 0.5f;
		float z1 = zi - 0.5f;
		float a1 = 0.75f - x1 * x1 - y1 * y1 - z1 * z1;
		value += a1 * a1 * (a1 * a1) * Grad(HashPrimes(seed2, xrbp + 5910200641878280303L, yrbp + 6452764530575939509L, zrbp + 6614699811220273867L), x1, y1, z1);
		float xAFlipMask0 = (float)((xNMask | 1) << 1) * x1;
		float yAFlipMask0 = (float)((yNMask | 1) << 1) * y1;
		float zAFlipMask0 = (float)((zNMask | 1) << 1) * z1;
		float xAFlipMask1 = (float)(-2 - (xNMask << 2)) * x1 - 1f;
		float yAFlipMask1 = (float)(-2 - (yNMask << 2)) * y1 - 1f;
		float zAFlipMask1 = (float)(-2 - (zNMask << 2)) * z1 - 1f;
		bool skip5 = false;
		float a2 = xAFlipMask0 + a0;
		if (a2 > 0f)
		{
			value += a2 * a2 * (a2 * a2) * Grad(HashPrimes(seed, xrbp + (~xNMask & 0x5205402B9270C86FL), yrbp + (yNMask & 0x598CD327003817B5L), zrbp + (zNMask & 0x5BCC226E9FA0BACBL)), x0 - (float)(xNMask | 1), y0, z0);
		}
		else
		{
			float a3 = yAFlipMask0 + zAFlipMask0 + a0;
			if (a3 > 0f)
			{
				value += a3 * a3 * (a3 * a3) * Grad(HashPrimes(seed, xrbp + (xNMask & 0x5205402B9270C86FL), yrbp + (~yNMask & 0x598CD327003817B5L), zrbp + (~zNMask & 0x5BCC226E9FA0BACBL)), x0, y0 - (float)(yNMask | 1), z0 - (float)(zNMask | 1));
			}
			float a4 = xAFlipMask1 + a1;
			if (a4 > 0f)
			{
				value += a4 * a4 * (a4 * a4) * Grad(HashPrimes(seed2, xrbp + (xNMask & -6626342789952991010L), yrbp + 6452764530575939509L, zrbp + 6614699811220273867L), (float)(xNMask | 1) + x1, y1, z1);
				skip5 = true;
			}
		}
		bool skip6 = false;
		float a6 = yAFlipMask0 + a0;
		if (a6 > 0f)
		{
			value += a6 * a6 * (a6 * a6) * Grad(HashPrimes(seed, xrbp + (xNMask & 0x5205402B9270C86FL), yrbp + (~yNMask & 0x598CD327003817B5L), zrbp + (zNMask & 0x5BCC226E9FA0BACBL)), x0, y0 - (float)(yNMask | 1), z0);
		}
		else
		{
			float a7 = xAFlipMask0 + zAFlipMask0 + a0;
			if (a7 > 0f)
			{
				value += a7 * a7 * (a7 * a7) * Grad(HashPrimes(seed, xrbp + (~xNMask & 0x5205402B9270C86FL), yrbp + (yNMask & 0x598CD327003817B5L), zrbp + (~zNMask & 0x5BCC226E9FA0BACBL)), x0 - (float)(xNMask | 1), y0, z0 - (float)(zNMask | 1));
			}
			float a8 = yAFlipMask1 + a1;
			if (a8 > 0f)
			{
				value += a8 * a8 * (a8 * a8) * Grad(HashPrimes(seed2, xrbp + 5910200641878280303L, yrbp + (yNMask & -5541215012557672598L), zrbp + 6614699811220273867L), x1, (float)(yNMask | 1) + y1, z1);
				skip6 = true;
			}
		}
		bool skipD = false;
		float aA = zAFlipMask0 + a0;
		if (aA > 0f)
		{
			value += aA * aA * (aA * aA) * Grad(HashPrimes(seed, xrbp + (xNMask & 0x5205402B9270C86FL), yrbp + (yNMask & 0x598CD327003817B5L), zrbp + (~zNMask & 0x5BCC226E9FA0BACBL)), x0, y0, z0 - (float)(zNMask | 1));
		}
		else
		{
			float aB = xAFlipMask0 + yAFlipMask0 + a0;
			if (aB > 0f)
			{
				value += aB * aB * (aB * aB) * Grad(HashPrimes(seed, xrbp + (~xNMask & 0x5205402B9270C86FL), yrbp + (~yNMask & 0x598CD327003817B5L), zrbp + (zNMask & 0x5BCC226E9FA0BACBL)), x0 - (float)(xNMask | 1), y0 - (float)(yNMask | 1), z0);
			}
			float aC = zAFlipMask1 + a1;
			if (aC > 0f)
			{
				value += aC * aC * (aC * aC) * Grad(HashPrimes(seed2, xrbp + 5910200641878280303L, yrbp + 6452764530575939509L, zrbp + (zNMask & -5217344451269003882L)), x1, y1, (float)(zNMask | 1) + z1);
				skipD = true;
			}
		}
		if (!skip5)
		{
			float a5 = yAFlipMask1 + zAFlipMask1 + a1;
			if (a5 > 0f)
			{
				value += a5 * a5 * (a5 * a5) * Grad(HashPrimes(seed2, xrbp + 5910200641878280303L, yrbp + (yNMask & -5541215012557672598L), zrbp + (zNMask & -5217344451269003882L)), x1, (float)(yNMask | 1) + y1, (float)(zNMask | 1) + z1);
			}
		}
		if (!skip6)
		{
			float a9 = xAFlipMask1 + zAFlipMask1 + a1;
			if (a9 > 0f)
			{
				value += a9 * a9 * (a9 * a9) * Grad(HashPrimes(seed2, xrbp + (xNMask & -6626342789952991010L), yrbp + 6452764530575939509L, zrbp + (zNMask & -5217344451269003882L)), (float)(xNMask | 1) + x1, y1, (float)(zNMask | 1) + z1);
			}
		}
		if (!skipD)
		{
			float aD = xAFlipMask1 + yAFlipMask1 + a1;
			if (aD > 0f)
			{
				value += aD * aD * (aD * aD) * Grad(HashPrimes(seed2, xrbp + (xNMask & -6626342789952991010L), yrbp + (yNMask & -5541215012557672598L), zrbp + 6614699811220273867L), (float)(xNMask | 1) + x1, (float)(yNMask | 1) + y1, z1);
			}
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static long HashPrimes(long seed, long xsvp, long ysvp, long zsvp)
	{
		return seed ^ xsvp ^ ysvp ^ zsvp;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static float Grad(long hash, float dx, float dy, float dz)
	{
		hash *= 6026932503003350773L;
		hash ^= hash >> 58;
		int gi = (int)hash & 0x3FC;
		return Gradients3D[gi | 0] * dx + Gradients3D[gi | 1] * dy + Gradients3D[gi | 2] * dz;
	}

	static NewSimplexNoiseLayer()
	{
		Gradients3D = new float[1024];
		float[] grad3 = new float[192]
		{
			2.2247448f, 2.2247448f, -1f, 0f, 2.2247448f, 2.2247448f, 1f, 0f, 3.0862665f, 1.1721513f,
			0f, 0f, 1.1721513f, 3.0862665f, 0f, 0f, -2.2247448f, 2.2247448f, -1f, 0f,
			-2.2247448f, 2.2247448f, 1f, 0f, -1.1721513f, 3.0862665f, 0f, 0f, -3.0862665f, 1.1721513f,
			0f, 0f, -1f, -2.2247448f, -2.2247448f, 0f, 1f, -2.2247448f, -2.2247448f, 0f,
			0f, -3.0862665f, -1.1721513f, 0f, 0f, -1.1721513f, -3.0862665f, 0f, -1f, -2.2247448f,
			2.2247448f, 0f, 1f, -2.2247448f, 2.2247448f, 0f, 0f, -1.1721513f, 3.0862665f, 0f,
			0f, -3.0862665f, 1.1721513f, 0f, -2.2247448f, -2.2247448f, -1f, 0f, -2.2247448f, -2.2247448f,
			1f, 0f, -3.0862665f, -1.1721513f, 0f, 0f, -1.1721513f, -3.0862665f, 0f, 0f,
			-2.2247448f, -1f, -2.2247448f, 0f, -2.2247448f, 1f, -2.2247448f, 0f, -1.1721513f, 0f,
			-3.0862665f, 0f, -3.0862665f, 0f, -1.1721513f, 0f, -2.2247448f, -1f, 2.2247448f, 0f,
			-2.2247448f, 1f, 2.2247448f, 0f, -3.0862665f, 0f, 1.1721513f, 0f, -1.1721513f, 0f,
			3.0862665f, 0f, -1f, 2.2247448f, -2.2247448f, 0f, 1f, 2.2247448f, -2.2247448f, 0f,
			0f, 1.1721513f, -3.0862665f, 0f, 0f, 3.0862665f, -1.1721513f, 0f, -1f, 2.2247448f,
			2.2247448f, 0f, 1f, 2.2247448f, 2.2247448f, 0f, 0f, 3.0862665f, 1.1721513f, 0f,
			0f, 1.1721513f, 3.0862665f, 0f, 2.2247448f, -2.2247448f, -1f, 0f, 2.2247448f, -2.2247448f,
			1f, 0f, 1.1721513f, -3.0862665f, 0f, 0f, 3.0862665f, -1.1721513f, 0f, 0f,
			2.2247448f, -1f, -2.2247448f, 0f, 2.2247448f, 1f, -2.2247448f, 0f, 3.0862665f, 0f,
			-1.1721513f, 0f, 1.1721513f, 0f, -3.0862665f, 0f, 2.2247448f, -1f, 2.2247448f, 0f,
			2.2247448f, 1f, 2.2247448f, 0f, 1.1721513f, 0f, 3.0862665f, 0f, 3.0862665f, 0f,
			1.1721513f, 0f
		};
		for (int j = 0; j < grad3.Length; j++)
		{
			grad3[j] = (float)((double)grad3[j] / 0.2781926117527186);
		}
		int i = 0;
		int k = 0;
		while (i < Gradients3D.Length)
		{
			if (k == grad3.Length)
			{
				k = 0;
			}
			Gradients3D[i] = grad3[k];
			i++;
			k++;
		}
	}
}
