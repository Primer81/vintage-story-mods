using System;
using System.Runtime.CompilerServices;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common.Database;

public struct ChunkPos
{
	public int X;

	public int Y;

	public int Z;

	public int Dimension;

	private static int chunkMask = 4194303;

	private static int yMask = 511;

	private static int dimMaskLow5Bits = 31;

	private static int dimMaskHigh5Bits = 992;

	public int InternalY => Y + Dimension * 1024;

	public ChunkPos(int xx, int yy, int zz, int dd)
	{
		X = xx;
		Y = yy;
		Z = zz;
		Dimension = dd;
	}

	public ChunkPos(int x, int internalCY, int z)
	{
		X = x;
		Y = internalCY % 1024;
		Z = z;
		Dimension = internalCY / 1024;
	}

	public ChunkPos(Vec3i vec)
	{
		X = vec.X;
		Y = vec.Y % 1024;
		Z = vec.Z;
		Dimension = vec.Y / 1024;
	}

	[Obsolete("Not dimension aware")]
	public static ChunkPos FromPosition(int x, int y, int z)
	{
		ChunkPos result = default(ChunkPos);
		result.X = x / 32;
		result.Y = y / 32;
		result.Z = z / 32;
		result.Dimension = 0;
		return result;
	}

	public static ChunkPos FromPosition(int x, int y, int z, int d)
	{
		ChunkPos result = default(ChunkPos);
		result.X = x / 32;
		result.Y = y / 32;
		result.Z = z / 32;
		result.Dimension = d;
		return result;
	}

	public override int GetHashCode()
	{
		return ((391 + X) * 23 + Y) * 23 + Z + Dimension * 269023;
	}

	public ulong ToChunkIndex()
	{
		return ToChunkIndex(X, Y, Z, Dimension);
	}

	public static ChunkPos FromChunkIndex_saveGamev2(ulong index)
	{
		return new ChunkPos((int)index & chunkMask, (int)(index >> 54) & yMask, (int)(index >> 27) & chunkMask, ((int)(index >> 22) & dimMaskLow5Bits) + ((int)(index >> 44) & dimMaskHigh5Bits));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ulong ToChunkIndex(int x, int y, int z)
	{
		if (y >= 1024)
		{
			throw new Exception("Coding bug in dimensions system: please mention to radfast");
		}
		return (ulong)((uint)x | ((long)z << 27) | ((long)y << 54));
	}

	public static ulong ToChunkIndex(int x, int y, int z, int dim)
	{
		if (y >= 1024)
		{
			throw new Exception("Coding bug in dimensions system: please mention to radfast");
		}
		ulong index = ToChunkIndex(x, y, z);
		if (dim != 0)
		{
			index |= (ulong)((long)(dim & dimMaskLow5Bits) << 22);
			index |= (ulong)((long)(dim & dimMaskHigh5Bits) << 44);
		}
		return index;
	}

	public Vec3i ToVec3i()
	{
		return new Vec3i(X, InternalY, Z);
	}
}
