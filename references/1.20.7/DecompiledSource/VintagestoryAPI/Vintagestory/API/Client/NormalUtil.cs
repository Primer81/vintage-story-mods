using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public static class NormalUtil
{
	public static int NegBit = 512;

	public static int tenBitMask = 1023;

	public static int nineBitMask = 511;

	public static int tenthBitMask = 512;

	public static void FromPackedNormal(int normal, ref Vec4f toFill)
	{
		int normal2 = normal >> 10;
		int normal3 = normal >> 20;
		bool xNeg = (tenthBitMask & normal) > 0;
		bool yNeg = (tenthBitMask & normal2) > 0;
		bool zNeg = (tenthBitMask & normal3) > 0;
		toFill.X = (float)(xNeg ? (~normal & nineBitMask) : (normal & nineBitMask)) / 512f;
		toFill.Y = (float)(yNeg ? (~normal2 & nineBitMask) : (normal2 & nineBitMask)) / 512f;
		toFill.Z = (float)(zNeg ? (~normal3 & nineBitMask) : (normal3 & nineBitMask)) / 512f;
		toFill.W = normal >> 30;
	}

	public static void FromPackedNormal(int normal, ref float[] toFill)
	{
		int normal2 = normal >> 10;
		int normal3 = normal >> 20;
		bool xNeg = (tenthBitMask & normal) > 0;
		bool yNeg = (tenthBitMask & normal2) > 0;
		bool zNeg = (tenthBitMask & normal3) > 0;
		toFill[0] = (float)(xNeg ? (~normal & nineBitMask) : (normal & nineBitMask)) / 512f;
		toFill[1] = (float)(yNeg ? (~normal2 & nineBitMask) : (normal2 & nineBitMask)) / 512f;
		toFill[2] = (float)(zNeg ? (~normal3 & nineBitMask) : (normal3 & nineBitMask)) / 512f;
		toFill[3] = normal >> 30;
	}

	public static int PackNormal(Vec4f normal)
	{
		bool num = normal.X < 0f;
		bool yNeg = normal.Y < 0f;
		bool zNeg = normal.Z < 0f;
		int normalX = (int)Math.Abs(normal.X * 511f);
		int normalY = (int)Math.Abs(normal.Y * 511f);
		int normalZ = (int)Math.Abs(normal.Z * 511f);
		return (num ? (NegBit | (~normalX & nineBitMask)) : normalX) | ((yNeg ? (NegBit | (~normalY & nineBitMask)) : normalY) << 10) | ((zNeg ? (NegBit | (~normalZ & nineBitMask)) : normalZ) << 20) | ((int)normal.W << 30);
	}

	public static int PackNormal(float x, float y, float z)
	{
		bool num = x < 0f;
		bool yNeg = y < 0f;
		bool zNeg = z < 0f;
		int num2 = (num ? (NegBit | (~(int)Math.Abs(x * 511f) & nineBitMask)) : ((int)(x * 511f) & nineBitMask));
		int normalY = (yNeg ? (NegBit | (~(int)Math.Abs(y * 511f) & nineBitMask)) : ((int)(y * 511f) & nineBitMask));
		int normalZ = (zNeg ? (NegBit | (~(int)Math.Abs(z * 511f) & nineBitMask)) : ((int)(z * 511f) & nineBitMask));
		return num2 | (normalY << 10) | (normalZ << 20);
	}

	internal static int PackNormal(float[] normal)
	{
		bool num = normal[0] < 0f;
		bool yNeg = normal[1] < 0f;
		bool zNeg = normal[2] < 0f;
		int normalX = (int)Math.Abs(normal[0] * 511f);
		int normalY = (int)Math.Abs(normal[1] * 511f);
		int normalZ = (int)Math.Abs(normal[2] * 511f);
		return (num ? (NegBit | (~normalX & nineBitMask)) : normalX) | ((yNeg ? (NegBit | (~normalY & nineBitMask)) : normalY) << 10) | ((zNeg ? (NegBit | (~normalZ & nineBitMask)) : normalZ) << 20) | ((int)normal[3] << 30);
	}

	internal static void FromPackedNormal(int normal, ref double[] toFill)
	{
		int normal2 = normal >> 10;
		int normal3 = normal >> 20;
		bool xNeg = (tenthBitMask & normal) > 0;
		bool yNeg = (tenthBitMask & normal2) > 0;
		bool zNeg = (tenthBitMask & normal3) > 0;
		toFill[0] = (float)(xNeg ? (~normal & nineBitMask) : (normal & nineBitMask)) / 512f;
		toFill[1] = (float)(yNeg ? (~normal2 & nineBitMask) : (normal2 & nineBitMask)) / 512f;
		toFill[2] = (float)(zNeg ? (~normal3 & nineBitMask) : (normal3 & nineBitMask)) / 512f;
		toFill[3] = normal >> 30;
	}

	internal static int PackNormal(double[] normal)
	{
		bool num = normal[0] < 0.0;
		bool yNeg = normal[1] < 0.0;
		bool zNeg = normal[2] < 0.0;
		int normalX = (int)Math.Abs(normal[0] * 512.0);
		int normalY = (int)Math.Abs(normal[1] * 512.0);
		int normalZ = (int)Math.Abs(normal[2] * 512.0);
		return (num ? (NegBit | (~normalX & tenBitMask)) : normalX) | ((yNeg ? (NegBit | (~normalY & tenBitMask)) : normalY) << 10) | ((zNeg ? (NegBit | (~normalZ & tenBitMask)) : normalZ) << 20) | ((int)normal[3] << 30);
	}
}
