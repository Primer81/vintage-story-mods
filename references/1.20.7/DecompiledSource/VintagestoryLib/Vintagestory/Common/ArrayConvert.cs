using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public static class ArrayConvert
{
	public static byte[] UshortToByte(ushort[] shorts)
	{
		byte[] output = new byte[shorts.Length * 2];
		UshortToByte(shorts, output);
		return output;
	}

	public unsafe static void UshortToByte(ushort[] shorts, byte[] output)
	{
		fixed (byte* pByte = output)
		{
			ushort* pUshort = (ushort*)pByte;
			for (int i = 0; i < shorts.Length; i++)
			{
				pUshort[i] = shorts[i];
			}
		}
	}

	internal static ushort[] ByteToUshort(byte[] data)
	{
		ushort[] output = new ushort[data.Length / 2];
		ByteToUshort(data, output);
		return output;
	}

	internal static ushort[] ByteToUshort(byte[] data, int length)
	{
		ushort[] output = new ushort[length / 2];
		ByteToUshort(data, output);
		return output;
	}

	internal unsafe static void ByteToUshort(byte[] data, ushort[] output)
	{
		fixed (byte* pByte = data)
		{
			ushort* pUShort = (ushort*)pByte;
			int len = output.Length / 2;
			len *= 2;
			for (int i = 0; i < len; i += 2)
			{
				output[i] = pUShort[i];
				output[i + 1] = pUShort[i + 1];
			}
			if (len < output.Length)
			{
				output[len] = pUShort[len];
			}
		}
	}

	internal static byte[] ByteToByte(byte[] data, int length)
	{
		byte[] output = new byte[length];
		if (length == 0)
		{
			return output;
		}
		int len = length / 4 * 4;
		int i;
		for (i = 0; i < len; i += 4)
		{
			output[i] = data[i];
			output[i + 1] = data[i + 1];
			output[i + 2] = data[i + 2];
			output[i + 3] = data[i + 3];
		}
		while (i < length)
		{
			output[i] = data[i++];
		}
		return output;
	}

	internal static byte[] ByteToByte(byte[] data, int length, int reserveOffset)
	{
		byte[] output = new byte[length + reserveOffset];
		if (length == 0)
		{
			return output;
		}
		int j = reserveOffset;
		int len = length / 4 * 4;
		int i;
		for (i = 0; i < len; i += 4)
		{
			output[j] = data[i];
			output[j + 1] = data[i + 1];
			output[j + 2] = data[i + 2];
			output[j + 3] = data[i + 3];
			j += 4;
		}
		while (i < length)
		{
			output[j++] = data[i++];
		}
		return output;
	}

	internal static byte[] Build(int lengthA, byte[] dataA, byte[] data, int length)
	{
		byte[] output = new byte[length + lengthA + 4];
		if (length + lengthA == 0)
		{
			return output;
		}
		output[0] = (byte)lengthA;
		output[1] = (byte)(lengthA >> 8);
		output[2] = (byte)(lengthA >> 16);
		output[3] = (byte)(lengthA >> 24);
		int j = 4;
		int len = lengthA / 4 * 4;
		int i;
		for (i = 0; i < len; i += 4)
		{
			output[j] = dataA[i];
			output[j + 1] = dataA[i + 1];
			output[j + 2] = dataA[i + 2];
			output[j + 3] = dataA[i + 3];
			j += 4;
		}
		while (i < lengthA)
		{
			output[j++] = dataA[i++];
		}
		len = length / 4 * 4;
		for (i = 0; i < len; i += 4)
		{
			output[j] = data[i];
			output[j + 1] = data[i + 1];
			output[j + 2] = data[i + 2];
			output[j + 3] = data[i + 3];
			j += 4;
		}
		while (i < length)
		{
			output[j++] = data[i++];
		}
		return output;
	}

	internal unsafe static byte[] Build(int lengthA, int[] intData, byte[] data, int length)
	{
		byte[] output = new byte[length + lengthA * 4 + 4];
		if (length + lengthA == 0)
		{
			return output;
		}
		int intLengthToWrite = -4 * lengthA;
		output[0] = (byte)intLengthToWrite;
		output[1] = (byte)(intLengthToWrite >> 8);
		output[2] = (byte)(intLengthToWrite >> 16);
		output[3] = (byte)(intLengthToWrite >> 24);
		int j = 0;
		fixed (byte* pByte = output)
		{
			int* pInt = (int*)(pByte + 4);
			for (; j < lengthA; j++)
			{
				pInt[j] = intData[j];
			}
		}
		j = (j + 1) * 4;
		int len = length / 4 * 4;
		int i;
		for (i = 0; i < len; i += 4)
		{
			output[j] = data[i];
			output[j + 1] = data[i + 1];
			output[j + 2] = data[i + 2];
			output[j + 3] = data[i + 3];
			j += 4;
		}
		while (i < length)
		{
			output[j++] = data[i++];
		}
		return output;
	}

	internal static int GetInt(byte[] output)
	{
		int a0 = output[0] & 0xFF;
		int a1 = output[1] & 0xFF;
		int a2 = output[2] & 0xFF;
		return ((((output[3] & 0xFF) << 8) + a2 << 8) + a1 << 8) + a0;
	}

	public static byte[] IntToByte(int[] ints)
	{
		byte[] output = new byte[ints.Length * 4];
		IntToByte(ints, output);
		return output;
	}

	public unsafe static void IntToByte(int[] ints, byte[] output)
	{
		fixed (byte* pByte = output)
		{
			int* pInt = (int*)pByte;
			for (int i = 0; i < ints.Length; i++)
			{
				pInt[i] = ints[i];
			}
		}
	}

	public static int[] ByteToInt(byte[] data)
	{
		int[] output = new int[data.Length / 4];
		ByteToInt(data, output);
		return output;
	}

	public unsafe static void ByteToInt(byte[] data, int[] output)
	{
		fixed (byte* pByte = data)
		{
			int* pInt = (int*)pByte;
			for (int i = 0; i < output.Length; i++)
			{
				output[i] = pInt[i];
			}
		}
	}

	public unsafe static void ByteToInt(byte[] data, int[] output, int length)
	{
		fixed (byte* pByte = data)
		{
			int* pInt = (int*)pByte;
			for (int i = 0; i < length; i++)
			{
				output[i] = pInt[i];
			}
		}
	}

	public unsafe static void ByteToInt(byte[] data, int offset, int[] output, int length)
	{
		fixed (byte* pByte = data)
		{
			int* pInt = (int*)pByte;
			pInt += offset / 4;
			for (int i = 0; i < length; i++)
			{
				output[i] = pInt[i];
			}
		}
	}

	public unsafe static void ByteToIntArrays(byte[] data, int[][] output, int count, Func<int[]> newArray)
	{
		for (int k = 0; k < count; k++)
		{
			if (output[k] == null)
			{
				output[k] = newArray();
			}
		}
		fixed (byte* pByte = data)
		{
			int* pInt = (int*)pByte;
			for (int j = 0; j < count; j++)
			{
				int[] dest = output[j];
				for (int i = 0; i < dest.Length; i += 4)
				{
					dest[i] = *pInt;
					dest[i + 1] = pInt[1];
					dest[i + 2] = pInt[2];
					dest[i + 3] = pInt[3];
					pInt += 4;
				}
			}
		}
	}

	public unsafe static void ByteToUint(byte[] data, uint[] output, int length)
	{
		fixed (byte* pByte = data)
		{
			uint* pInt = (uint*)pByte;
			for (int i = 0; i < length; i++)
			{
				output[i] = pInt[i];
			}
		}
	}

	public static T[] Copy<T>(this IEnumerable<T> data, long index, long length)
	{
		T[] result = new T[length];
		Array.Copy(data.ToArray(), index, result, 0L, length);
		return result;
	}

	internal static void IntToInt(int[] src, int[] dest, int offset)
	{
		Array.Copy(src, 0, dest, offset, src.Length);
	}

	public static Vec3f[] ToVec3fs(this byte[] bytes)
	{
		return bytes.ToFloats().ToVec3fs();
	}

	public static Vec2f[] ToVec2fs(this byte[] bytes)
	{
		return bytes.ToFloats().ToVec2fs();
	}

	public static Vec4s[] ToVec4Ss(this byte[] bytes)
	{
		return bytes.ToShorts().ToVec4ss();
	}

	public static Vec4us[] ToVec4uss(this byte[] bytes)
	{
		return bytes.ToUShorts().ToVec4uss();
	}

	public static int[] ToInts(this ushort[] shorts)
	{
		int[] ints = new int[shorts.Length];
		for (int i = 0; i < shorts.Length; i++)
		{
			ints[i] = shorts[i];
		}
		return ints;
	}

	public static Vec4s[] ToVec4ss(this IEnumerable<short> shorts1)
	{
		short[] shorts2 = shorts1.ToArray();
		Vec4s[] vecs = new Vec4s[shorts2.Length / 4];
		for (int i = 0; i < vecs.Length; i++)
		{
			vecs[i] = new Vec4s(shorts2[i * 4], shorts2[i * 4 + 1], shorts2[i * 4 + 2], shorts2[i * 4 + 3]);
		}
		return vecs;
	}

	public static Vec4us[] ToVec4uss(this IEnumerable<ushort> shorts1)
	{
		ushort[] shorts2 = shorts1.ToArray();
		Vec4us[] vecs = new Vec4us[shorts2.Length / 4];
		for (int i = 0; i < vecs.Length; i++)
		{
			vecs[i] = new Vec4us(shorts2[i * 4], shorts2[i * 4 + 1], shorts2[i * 4 + 2], shorts2[i * 4 + 3]);
		}
		return vecs;
	}

	public static Vec3f[] ToVec3fs(this IEnumerable<float> floats1)
	{
		float[] floats2 = floats1.ToArray();
		Vec3f[] vecs = new Vec3f[floats2.Length / 3];
		for (int i = 0; i < vecs.Length; i++)
		{
			vecs[i] = new Vec3f(floats2[i * 3], floats2[i * 3 + 1], floats2[i * 3 + 2]);
		}
		return vecs;
	}

	public static Vec2f[] ToVec2fs(this IEnumerable<float> floats1)
	{
		float[] floats2 = floats1.ToArray();
		Vec2f[] vecs = new Vec2f[floats2.Length / 2];
		for (int i = 0; i < vecs.Length; i++)
		{
			vecs[i] = new Vec2f(floats2[i * 2], floats2[i * 2 + 1]);
		}
		return vecs;
	}

	public static int[] ToInts(this IEnumerable<byte> bytes)
	{
		int[] ints = new int[bytes.Count() / 4];
		for (int i = 0; i < ints.Length; i++)
		{
			ints[i] = BitConverter.ToInt32(bytes.Copy(i * 4, 4L), 0);
		}
		return ints;
	}

	public static ulong[] BytesToULongs(this IEnumerable<byte> bytes)
	{
		return bytes.ToUShorts().FourShortsToULong();
	}

	public static ulong[] FourShortsToULong(this IEnumerable<ushort> shorts1)
	{
		long[] longs = shorts1.FourShortsToLong();
		ulong[] ulongs = new ulong[longs.Count()];
		for (int i = 0; i < ulongs.Length; i++)
		{
			ulongs[i] = (ulong)longs[i];
		}
		return ulongs;
	}

	public static long[] FourShortsToLong(this IEnumerable<ushort> shorts1)
	{
		ushort[] shorts2 = shorts1.ToArray();
		long[] longs = new long[shorts2.Count() / 4];
		for (int i = 0; i < longs.Length; i++)
		{
			long tmp = (shorts2[i * 4] << 16) | shorts2[i * 4 + 1] | (shorts2[i * 4 + 2] << 16) | shorts2[i * 4 + 3];
			longs[i] = tmp;
		}
		return longs;
	}

	public static float[] ToFloats(this IEnumerable<byte> bytes)
	{
		float[] floats = new float[bytes.Count() / 4];
		for (int i = 0; i < floats.Length; i++)
		{
			floats[i] = BitConverter.ToSingle(bytes.Copy(i * 4, 4L), 0);
		}
		return floats;
	}

	public static ushort[] ToUShorts(this IEnumerable<byte> bytes)
	{
		ushort[] shorts = new ushort[bytes.Count() / 2];
		for (int i = 0; i < shorts.Length; i++)
		{
			shorts[i] = BitConverter.ToUInt16(bytes.Copy(i * 2, 2L), 0);
		}
		return shorts;
	}

	public static short[] ToShorts(this IEnumerable<byte> bytes)
	{
		short[] shorts = new short[bytes.Count() / 2];
		for (int i = 0; i < shorts.Length; i++)
		{
			shorts[i] = BitConverter.ToInt16(bytes.Copy(i * 2, 2L), 0);
		}
		return shorts;
	}

	public static int GetRoundedUpSize(int value)
	{
		int num = value - 1;
		int num2 = num | (num >> 1);
		int num3 = num2 | (num2 >> 2);
		int num4 = num3 | (num3 >> 4);
		int num5 = num4 | (num4 >> 8);
		return (num5 | (num5 >> 16)) + 1;
	}
}
