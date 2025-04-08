using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class ChunkDataLayer
{
	private const int chunksize = 32;

	protected const int length = 32768;

	public const int INTSIZE = 32;

	public const int SLICESIZE = 1024;

	public const int DATASLICES = 15;

	protected int[][] dataBits;

	public int[] palette;

	public volatile int paletteCount;

	protected int bitsize;

	public FastRWLock readWriteLock;

	public Func<int, int> Get;

	protected int[] dataBit0;

	protected int[] dataBit1;

	protected int[] dataBit2;

	protected int[] dataBit3;

	private int setBlockIndexCached;

	private int setBlockValueCached;

	protected ChunkDataPool pool;

	protected static byte[] emptyCompressed = new byte[4];

	[ThreadStatic]
	private static int[] paletteBitmap;

	[ThreadStatic]
	private static int[] paletteValuesBuilder;

	public ChunkDataLayer(ChunkDataPool chunkDataPool)
	{
		Get = GetFromBits0;
		readWriteLock = new FastRWLock(chunkDataPool);
		pool = chunkDataPool;
	}

	protected int GetGeneralCase(int index3d)
	{
		int intIndex = (index3d & 0x7FFF) / 32;
		int bitValue = 1;
		int idx = 0;
		readWriteLock.AcquireReadLock();
		for (int i = 0; i < bitsize; i++)
		{
			idx += ((dataBits[i][intIndex] >> index3d) & 1) * bitValue;
			bitValue *= 2;
		}
		readWriteLock.ReleaseReadLock();
		return palette[idx];
	}

	protected int GetFromBits0(int index3d)
	{
		return 0;
	}

	private int GetFromBits1(int index3d)
	{
		int intIndex = (index3d & 0x7FFF) / 32;
		return palette[(dataBit0[intIndex] >> index3d) & 1];
	}

	private int GetFromBits2(int index3d)
	{
		int intIndex = (index3d & 0x7FFF) / 32;
		readWriteLock.AcquireReadLock();
		int result = palette[((dataBit0[intIndex] >> index3d) & 1) + 2 * ((dataBit1[intIndex] >> index3d) & 1)];
		readWriteLock.ReleaseReadLock();
		return result;
	}

	private int GetFromBits3(int index3d)
	{
		int intIndex = (index3d & 0x7FFF) / 32;
		readWriteLock.AcquireReadLock();
		int result = palette[((dataBit0[intIndex] >> index3d) & 1) + 2 * ((dataBit1[intIndex] >> index3d) & 1) + 4 * ((dataBit2[intIndex] >> index3d) & 1)];
		readWriteLock.ReleaseReadLock();
		return result;
	}

	private int GetFromBits4(int index3d)
	{
		int intIndex = (index3d & 0x7FFF) / 32;
		readWriteLock.AcquireReadLock();
		int result = palette[((dataBit0[intIndex] >> index3d) & 1) + 2 * ((dataBit1[intIndex] >> index3d) & 1) + 4 * ((dataBit2[intIndex] >> index3d) & 1) + 8 * ((dataBit3[intIndex] >> index3d) & 1)];
		readWriteLock.ReleaseReadLock();
		return result;
	}

	private int GetFromBits5(int index3d)
	{
		int intIndex = (index3d & 0x7FFF) / 32;
		readWriteLock.AcquireReadLock();
		int result = palette[((dataBit0[intIndex] >> index3d) & 1) + 2 * ((dataBit1[intIndex] >> index3d) & 1) + 4 * ((dataBit2[intIndex] >> index3d) & 1) + 8 * ((dataBit3[intIndex] >> index3d) & 1) + 16 * ((dataBits[4][intIndex] >> index3d) & 1)];
		readWriteLock.ReleaseReadLock();
		return result;
	}

	private Func<int, int> selectDelegate(int newBlockBitsize)
	{
		if (palette == null)
		{
			return GetFromBits0;
		}
		switch (newBlockBitsize)
		{
		case 0:
			return GetFromBits0;
		case 1:
			dataBit0 = dataBits[0];
			return GetFromBits1;
		case 2:
			dataBit0 = dataBits[0];
			dataBit1 = dataBits[1];
			return GetFromBits2;
		case 3:
			dataBit0 = dataBits[0];
			dataBit1 = dataBits[1];
			dataBit2 = dataBits[2];
			return GetFromBits3;
		case 4:
			dataBit0 = dataBits[0];
			dataBit1 = dataBits[1];
			dataBit2 = dataBits[2];
			dataBit3 = dataBits[3];
			return GetFromBits4;
		case 5:
			dataBit0 = dataBits[0];
			dataBit1 = dataBits[1];
			dataBit2 = dataBits[2];
			dataBit3 = dataBits[3];
			return GetFromBits5;
		default:
			dataBit0 = dataBits[0];
			return GetGeneralCase;
		}
	}

	internal int[][] CopyData()
	{
		readWriteLock.AcquireReadLock();
		int[][] dataCopy = new int[bitsize][];
		for (int j = 0; j < dataCopy.Length; j++)
		{
			int[] newarray = (dataCopy[j] = pool.NewData_NoClear());
			int[] oldArray = dataBits[j];
			for (int i = 0; i < newarray.Length; i += 4)
			{
				newarray[i] = oldArray[i];
				newarray[i + 1] = oldArray[i + 1];
				newarray[i + 2] = oldArray[i + 2];
				newarray[i + 3] = oldArray[i + 3];
			}
		}
		readWriteLock.ReleaseReadLock();
		return dataCopy;
	}

	public int GetUnsafe_PaletteCheck(int index3d)
	{
		if (palette == null)
		{
			return 0;
		}
		return GetUnsafe(index3d);
	}

	public int GetUnsafe(int index3d)
	{
		try
		{
			int intIndex = index3d / 32;
			switch (bitsize)
			{
			case 0:
				return 0;
			case 1:
				return palette[(dataBit0[intIndex] >> index3d) & 1];
			case 2:
				return palette[((dataBit0[intIndex] >> index3d) & 1) + 2 * ((dataBit1[intIndex] >> index3d) & 1)];
			case 3:
				return palette[((dataBit0[intIndex] >> index3d) & 1) + 2 * ((dataBit1[intIndex] >> index3d) & 1) + 4 * ((dataBit2[intIndex] >> index3d) & 1)];
			case 4:
				return palette[((dataBit0[intIndex] >> index3d) & 1) + 2 * ((dataBit1[intIndex] >> index3d) & 1) + 4 * ((dataBit2[intIndex] >> index3d) & 1) + 8 * ((dataBit3[intIndex] >> index3d) & 1)];
			case 5:
				return palette[((dataBit0[intIndex] >> index3d) & 1) + 2 * ((dataBit1[intIndex] >> index3d) & 1) + 4 * ((dataBit2[intIndex] >> index3d) & 1) + 8 * ((dataBit3[intIndex] >> index3d) & 1) + 16 * ((dataBits[4][intIndex] >> index3d) & 1)];
			default:
			{
				int bitValue = 1;
				int idx = (dataBit0[intIndex] >> index3d) & 1;
				for (int i = 1; i < bitsize; i++)
				{
					bitValue *= 2;
					idx += ((dataBits[i][intIndex] >> index3d) & 1) * bitValue;
				}
				return palette[idx];
			}
			}
		}
		catch (NullReferenceException)
		{
			if (palette == null)
			{
				throw new Exception("ChunkDataLayer: palette was null, bitsize is " + bitsize);
			}
			if (bitsize > 0 && dataBit0 == null)
			{
				throw new Exception("ChunkDataLayer: dataBit0 was null, bitsize is " + bitsize + ", dataBits[0] null:" + (dataBits[0] == null));
			}
			if (bitsize > 1 && dataBit1 == null)
			{
				throw new Exception("ChunkDataLayer: dataBit1 was null, bitsize is " + bitsize + ", dataBits[1] null:" + (dataBits[1] == null));
			}
			if (bitsize > 2 && dataBit2 == null)
			{
				throw new Exception("ChunkDataLayer: dataBit2 was null, bitsize is " + bitsize + ", dataBits[2] null:" + (dataBits[2] == null));
			}
			if (bitsize > 3 && dataBit3 == null)
			{
				throw new Exception("ChunkDataLayer: dataBit3 was null, bitsize is " + bitsize + ", dataBits[3] null:" + (dataBits[3] == null));
			}
			if (bitsize > 4 && dataBits[4] == null)
			{
				throw new Exception("ChunkDataLayer: dataBits[4] was null, bitsize is " + bitsize);
			}
			throw new Exception("ChunkDataLayer: other null exception, bitsize is " + bitsize);
		}
	}

	public void Set(int index3d, int value)
	{
		if (index3d == (index3d & 0x7FFF))
		{
			int paletteIndex;
			if (palette != null)
			{
				if (value != 0)
				{
					if (value == setBlockValueCached)
					{
						paletteIndex = setBlockIndexCached;
					}
					else
					{
						int count = paletteCount;
						paletteIndex = 1;
						while (true)
						{
							if (paletteIndex < count)
							{
								if (palette[paletteIndex] == value)
								{
									break;
								}
								paletteIndex++;
								continue;
							}
							lock (palette)
							{
								if (paletteIndex == palette.Length)
								{
									paletteIndex = MakeSpaceInPalette();
								}
								palette[paletteIndex] = value;
								paletteCount++;
							}
							break;
						}
						setBlockIndexCached = paletteIndex;
						setBlockValueCached = value;
					}
				}
				else
				{
					if (palette.Length == 1)
					{
						return;
					}
					paletteIndex = 0;
				}
			}
			else
			{
				if (value == 0)
				{
					return;
				}
				NewDataBitsWithFirstValue(value);
				paletteIndex = 1;
			}
			int bitMask = 1 << index3d;
			int unsetMask = ~bitMask;
			index3d /= 32;
			readWriteLock.AcquireWriteLock();
			if (((uint)paletteIndex & (true ? 1u : 0u)) != 0)
			{
				dataBit0[index3d] |= bitMask;
			}
			else
			{
				dataBit0[index3d] &= unsetMask;
			}
			for (int i = 1; i < bitsize; i++)
			{
				if ((paletteIndex & (1 << i)) != 0)
				{
					dataBits[i][index3d] |= bitMask;
				}
				else
				{
					dataBits[i][index3d] &= unsetMask;
				}
			}
			readWriteLock.ReleaseWriteLock();
			return;
		}
		throw new IndexOutOfRangeException("Chunk blocks index3d must be between 0 and " + 32767 + ", was " + index3d);
	}

	public void SetUnsafe(int index3d, int value)
	{
		int bitMask = 1 << index3d;
		index3d /= 32;
		int unsetMask = ~bitMask;
		if (value != 0)
		{
			int paletteIndex;
			if (value == setBlockValueCached)
			{
				paletteIndex = setBlockIndexCached;
			}
			else if (palette == null)
			{
				NewDataBitsWithFirstValue(value);
				paletteIndex = 1;
			}
			else
			{
				int count = paletteCount;
				paletteIndex = 1;
				while (true)
				{
					if (paletteIndex < count)
					{
						if (palette[paletteIndex] == value)
						{
							break;
						}
						paletteIndex++;
						continue;
					}
					if (paletteIndex == palette.Length)
					{
						paletteIndex = MakeSpaceInPalette();
					}
					palette[paletteIndex] = value;
					paletteCount++;
					break;
				}
				setBlockIndexCached = paletteIndex;
				setBlockValueCached = value;
			}
			if (((uint)paletteIndex & (true ? 1u : 0u)) != 0)
			{
				dataBit0[index3d] |= bitMask;
			}
			else
			{
				dataBit0[index3d] &= unsetMask;
			}
			for (int i = 1; i < bitsize; i++)
			{
				if ((paletteIndex & (1 << i)) != 0)
				{
					dataBits[i][index3d] |= bitMask;
				}
				else
				{
					dataBits[i][index3d] &= unsetMask;
				}
			}
		}
		else
		{
			dataBit0[index3d] &= unsetMask;
			for (int j = 1; j < bitsize; j++)
			{
				dataBits[j][index3d] &= unsetMask;
			}
		}
	}

	public void SetZero(int index3d)
	{
		if (palette != null)
		{
			int num = 1 << index3d;
			index3d /= 32;
			int unsetMask = ~num;
			dataBit0[index3d] &= unsetMask;
			for (int i = 1; i < bitsize; i++)
			{
				dataBits[i][index3d] &= unsetMask;
			}
		}
	}

	public void SetBulk(int index3d, int lenX, int lenZ, int value)
	{
		int paletteIndex;
		if (value != 0)
		{
			if (value == setBlockValueCached)
			{
				paletteIndex = setBlockIndexCached;
			}
			else if (paletteCount == 0)
			{
				NewDataBitsWithFirstValue(value);
				paletteIndex = 1;
			}
			else
			{
				int count = paletteCount;
				paletteIndex = 1;
				while (true)
				{
					if (paletteIndex < count)
					{
						if (palette[paletteIndex] == value)
						{
							break;
						}
						paletteIndex++;
						continue;
					}
					if (paletteIndex == palette.Length)
					{
						paletteIndex = MakeSpaceInPalette();
					}
					palette[paletteIndex] = value;
					paletteCount++;
					break;
				}
			}
		}
		else
		{
			paletteIndex = 0;
		}
		int intIndex = index3d / 32;
		for (int z = 0; z < lenZ; z++)
		{
			dataBit0[intIndex] = -(paletteIndex & 1);
			for (int i = 1; i < bitsize; i++)
			{
				dataBits[i][intIndex] = -((paletteIndex >> i) & 1);
			}
			intIndex++;
		}
	}

	protected void NewDataBitsWithFirstValue(int value)
	{
		if (dataBits == null)
		{
			dataBits = new int[15][];
		}
		dataBit0 = (dataBits[0] = pool.NewData());
		setBlockIndexCached = 1;
		setBlockValueCached = value;
		palette = new int[2];
		paletteCount = 2;
		palette[1] = value;
		Get = GetFromBits1;
		bitsize = 1;
	}

	protected int MakeSpaceInPalette()
	{
		if (bitsize > 6 && CleanUpPalette())
		{
			return paletteCount;
		}
		int[] bp = palette;
		int currentLength = bp.Length;
		int[] newArray = new int[currentLength * 2];
		for (int i = 0; i < bp.Length; i++)
		{
			newArray[i] = bp[i];
		}
		palette = newArray;
		dataBits[bitsize] = pool.NewData();
		Get = selectDelegate(bitsize + 1);
		bitsize++;
		return currentLength;
	}

	private bool CleanUpPalette()
	{
		if (pool.server == null)
		{
			if (bitsize < 14)
			{
				return false;
			}
			throw new Exception("Oops, a client chunk had so many changes that it exceeded the maximum size.  That's not your fault!  Re-joining the game should fix it.  If you see this message repeated, please report it as a bug");
		}
		if (paletteBitmap == null)
		{
			paletteBitmap = new int[1024];
			paletteValuesBuilder = new int[32];
		}
		for (int n = 0; n < paletteBitmap.Length; n++)
		{
			paletteBitmap[n] = 0;
		}
		readWriteLock.AcquireReadLock();
		for (int m = 0; m < 1024; m++)
		{
			for (int k4 = 0; k4 < paletteValuesBuilder.Length; k4++)
			{
				paletteValuesBuilder[k4] = 0;
			}
			for (int j2 = 0; j2 < bitsize; j2++)
			{
				int bits = dataBits[j2][m];
				for (int k3 = 0; k3 < paletteValuesBuilder.Length; k3++)
				{
					paletteValuesBuilder[k3] |= ((bits >> k3) & 1) << j2;
				}
			}
			for (int k2 = 0; k2 < paletteValuesBuilder.Length; k2++)
			{
				int paletteValue = paletteValuesBuilder[k2];
				paletteBitmap[paletteValue / 32] |= 1 << paletteValue % 32;
			}
		}
		readWriteLock.ReleaseReadLock();
		int allUsed = -1;
		int maxCount = paletteCount / 32;
		for (int l = 0; l < maxCount; l++)
		{
			allUsed &= paletteBitmap[l];
		}
		if (allUsed == -1)
		{
			return false;
		}
		CleanUnusedValuesFromEndOfPalette();
		int paletteFlags = 0;
		for (int k = 0; k < paletteCount; k++)
		{
			if (k % 32 == 0)
			{
				paletteFlags = paletteBitmap[k / 32];
				if (paletteFlags == -1)
				{
					k += 31;
					continue;
				}
			}
			if ((paletteFlags & (1 << k)) == 0)
			{
				DeleteFromPalette(k);
				paletteBitmap[k / 32] |= 1 << k;
				CleanUnusedValuesFromEndOfPalette();
			}
		}
		int newBitsize = CalcBitsize(paletteCount + 1);
		int oldBitsize = bitsize;
		if (newBitsize < oldBitsize)
		{
			int[] newPalette = new int[1 << newBitsize];
			for (int j = 0; j < newPalette.Length; j++)
			{
				newPalette[j] = palette[j];
			}
			bitsize = newBitsize;
			palette = newPalette;
			Get = selectDelegate(newBitsize);
			for (int i = newBitsize; i < oldBitsize; i++)
			{
				pool.Return(dataBits[i]);
				dataBits[i] = null;
			}
		}
		return true;
	}

	private int CalcBitsize(int paletteCount)
	{
		if (paletteCount == 0)
		{
			return 0;
		}
		int bc = paletteCount - 1;
		int lbs = 1;
		while ((bc >>= 1) > 0)
		{
			lbs++;
		}
		return lbs;
	}

	private void CleanUnusedValuesFromEndOfPalette()
	{
		int v2 = paletteCount - 1;
		for (int i = v2 / 32; i >= 0; i--)
		{
			int paletteFlags = paletteBitmap[i];
			if (paletteFlags == 0)
			{
				paletteCount -= 32;
			}
			else
			{
				int j = 31;
				if (i == v2 / 32 && v2 % 32 < j)
				{
					j = v2 % 32;
				}
				while (j >= 0)
				{
					if ((paletteFlags & (1 << j)) != 0)
					{
						return;
					}
					paletteCount--;
					j--;
				}
			}
		}
	}

	internal void FillWithInitialValue(int value)
	{
		NewDataBitsWithFirstValue(value);
		int[] array = dataBit0;
		for (int i = 0; i < array.Length; i += 4)
		{
			array[i] = -1;
			array[i + 1] = -1;
			array[i + 2] = -1;
			array[i + 3] = -1;
		}
	}

	public void Clear(List<int[]> datas)
	{
		Get = GetFromBits0;
		int bbs = bitsize;
		bitsize = 0;
		if (dataBits != null && datas != null)
		{
			readWriteLock.WaitUntilFree();
			for (int i = 0; i < bbs; i++)
			{
				if (dataBits[i] != null)
				{
					datas.Add(dataBits[i]);
				}
			}
		}
		setBlockIndexCached = 0;
		setBlockValueCached = 0;
	}

	public void PopulateWithAir()
	{
		if (dataBits == null)
		{
			dataBits = new int[15][];
		}
		dataBit0 = (dataBits[0] = pool.NewData());
		setBlockIndexCached = 0;
		setBlockValueCached = 0;
		palette = new int[2];
		paletteCount = 1;
		Get = GetFromBits1;
		bitsize = 1;
	}

	public static byte[] Compress(ChunkDataLayer layer, int[] arrayStatic)
	{
		if (layer == null || layer.palette == null)
		{
			return emptyCompressed;
		}
		return layer.CompressUsing(arrayStatic);
	}

	private byte[] CompressUsing(int[] arrayStatic)
	{
		int ptr = 0;
		readWriteLock.AcquireReadLock();
		for (int i = 0; i < bitsize; i++)
		{
			ArrayConvert.IntToInt(dataBits[i], arrayStatic, ptr);
			ptr += 1024;
		}
		readWriteLock.ReleaseReadLock();
		return Compression.CompressAndCombine(arrayStatic, palette, paletteCount);
	}

	internal byte[] CompressSeparate(ref byte[] paletteCompressed, int[] arrayStatic, int chunkdataVersion)
	{
		int ptr = 0;
		lock (palette ?? ((object)readWriteLock))
		{
			readWriteLock.AcquireReadLock();
			for (int i = 0; i < bitsize; i++)
			{
				ArrayConvert.IntToInt(dataBits[i], arrayStatic, ptr);
				ptr += 1024;
			}
			readWriteLock.ReleaseReadLock();
			if (bitsize != CalcBitsize(paletteCount))
			{
				if (bitsize == 0)
				{
					paletteCompressed = emptyCompressed;
					return emptyCompressed;
				}
				throw new Exception("Likely code error! Compressing light mismatch: paletteCount " + paletteCount + ", databits " + bitsize);
			}
			paletteCompressed = Compression.Compress(palette, paletteCount, chunkdataVersion);
		}
		return Compression.Compress(arrayStatic, ptr, chunkdataVersion);
	}

	internal void Decompress(byte[] layerCompressed)
	{
		paletteCount = 0;
		palette = Compression.DecompressCombined(layerCompressed, ref dataBits, ref paletteCount, pool.NewData);
		if (palette != null)
		{
			int bbs = 0;
			int bc = palette.Length;
			while ((bc >>= 1) > 0)
			{
				bbs++;
			}
			Get = selectDelegate(bbs);
			bitsize = bbs;
		}
		else
		{
			bitsize = 0;
		}
	}

	internal void DecompressSeparate(byte[] dataCompressed, byte[] paletteCompressed)
	{
		palette = Compression.DecompressToInts(paletteCompressed, ref paletteCount);
		if (palette != null)
		{
			if (palette.Length == 0)
			{
				paletteCount = 0;
				palette = null;
				Get = GetFromBits0;
				bitsize = 0;
				return;
			}
			int lbs = 0;
			int bc = palette.Length;
			while ((bc >>= 1) > 0)
			{
				lbs++;
			}
			int lbsCheck = Compression.Decompress(dataCompressed, ref dataBits, pool.NewData);
			if (lbs != lbsCheck)
			{
				if (lbs < lbsCheck)
				{
					pool.Logger.Debug("Info: decompressed " + lbsCheck + " databits while palette length was only " + palette.Length + ", pc " + paletteCount);
				}
				else
				{
					pool.Logger.Error("Corrupted light data?  Decompressed " + lbsCheck + " databits while palette length was " + palette.Length + ", pc " + paletteCount);
				}
				while (lbs > lbsCheck)
				{
					dataBits[lbsCheck++] = pool.NewData();
				}
			}
			Get = selectDelegate(lbs);
			bitsize = lbs;
		}
		else
		{
			Get = GetFromBits0;
			bitsize = 0;
		}
	}

	internal void PopulateFrom(ushort[] oldValues, byte[] oldLightSat)
	{
		if (dataBits == null)
		{
			dataBits = new int[15][];
		}
		dataBit0 = (dataBits[0] = pool.NewData());
		palette = new int[2];
		paletteCount = 1;
		bitsize = 1;
		Get = GetFromBits1;
		readWriteLock.AcquireWriteLock();
		for (int i = 0; i < 32768; i++)
		{
			byte lightSatTmp = oldLightSat[i];
			int value = oldValues[i] | ((lightSatTmp & 0xFFF8) << 13);
			if (value != 0)
			{
				SetUnsafe(i, value);
			}
		}
		bool freeArrays = false;
		if (paletteCount == 1)
		{
			paletteCount = 0;
			palette = null;
			freeArrays = true;
			dataBits[0] = null;
			dataBit0 = null;
			bitsize = 0;
		}
		readWriteLock.ReleaseWriteLock();
		if (freeArrays)
		{
			pool.FreeArrays(this);
		}
	}

	internal bool HasContents()
	{
		int bbs = bitsize;
		for (int j = 0; j < bbs; j++)
		{
			int[] array = dataBits[j];
			for (int i = 0; i < array.Length; i += 4)
			{
				if (array[i] != 0)
				{
					return true;
				}
				if (array[i + 1] != 0)
				{
					return true;
				}
				if (array[i + 2] != 0)
				{
					return true;
				}
				if (array[i + 3] != 0)
				{
					return true;
				}
			}
		}
		return false;
	}

	internal void CopyBlocksTo(int[] blocksOut)
	{
		readWriteLock.AcquireReadLock();
		int bbs = bitsize;
		for (int index3d = 0; index3d < blocksOut.Length; index3d += 32)
		{
			int intIndex = index3d / 32;
			for (int bitIndex = 0; bitIndex < 32; bitIndex++)
			{
				int idx = 0;
				int bitValue = 1;
				for (int i = 0; i < bbs; i++)
				{
					idx += ((dataBits[i][intIndex] >> bitIndex) & 1) * bitValue;
					bitValue *= 2;
				}
				blocksOut[index3d + bitIndex] = palette[idx];
			}
		}
		readWriteLock.ReleaseReadLock();
	}

	internal BlockPos FindFirst(List<int> searchIds)
	{
		for (int i = 1; i < paletteCount; i++)
		{
			if (!searchIds.Contains(palette[i]))
			{
				continue;
			}
			for (int intIndex = 0; intIndex < 1024; intIndex++)
			{
				int searchResult = RapidValueSearch(intIndex, i);
				if (searchResult == 0)
				{
					continue;
				}
				for (int j = 0; j < 32; j++)
				{
					if ((searchResult & (1 << j)) != 0)
					{
						return new BlockPos(j, intIndex / 32, intIndex % 32);
					}
				}
			}
		}
		return null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private int RapidValueSearch(int intIndex, int needle)
	{
		int searchResult = -1;
		for (int i = 0; i < bitsize; i++)
		{
			searchResult &= (((needle & (1 << i)) != 0) ? dataBits[i][intIndex] : (~dataBits[i][intIndex]));
		}
		return searchResult;
	}

	internal bool Contains(int id)
	{
		for (int i = 0; i < paletteCount; i++)
		{
			if (palette[i] != id)
			{
				continue;
			}
			for (int intIndex = 0; intIndex < 1024; intIndex++)
			{
				if (RapidValueSearch(intIndex, i) != 0)
				{
					return true;
				}
			}
			return false;
		}
		return false;
	}

	internal void ListAllPaletteValues(List<int> list)
	{
		for (int i = 0; i < paletteCount; i++)
		{
			list.Add(palette[i]);
		}
	}

	protected void Write(int paletteIndex, int intIndex, int mask)
	{
		int unsetMask = ~mask;
		readWriteLock.AcquireWriteLock();
		for (int i = 0; i < bitsize; i++)
		{
			if ((paletteIndex & (1 << i)) != 0)
			{
				dataBits[i][intIndex] |= mask;
			}
			else
			{
				dataBits[i][intIndex] &= unsetMask;
			}
		}
		readWriteLock.ReleaseWriteLock();
	}

	protected void DeleteFromPalette(int deletePosition)
	{
		int search = paletteCount - 1;
		readWriteLock.AcquireWriteLock();
		int bbs = bitsize;
		for (int index3d = 0; index3d < 32768; index3d += 32)
		{
			int intIndex = index3d / 32;
			int mask = -1;
			for (int j = 0; j < bbs; j++)
			{
				int v = dataBits[j][intIndex];
				int searchBit = (search >> j) & 1;
				mask &= searchBit * v + (1 - searchBit) * ~v;
			}
			if (mask == 0)
			{
				continue;
			}
			int unsetMask = ~mask;
			for (int i = 0; i < bbs; i++)
			{
				if ((deletePosition & (1 << i)) != 0)
				{
					dataBits[i][intIndex] |= mask;
				}
				else
				{
					dataBits[i][intIndex] &= unsetMask;
				}
			}
		}
		palette[deletePosition] = palette[search];
		paletteCount--;
		readWriteLock.ReleaseWriteLock();
	}

	public void ClearPaletteOutsideMaxValue(int maxValue)
	{
		int[] bp = palette;
		if (bp == null)
		{
			return;
		}
		int count = paletteCount;
		for (int i = 0; i < count; i++)
		{
			if (bp[i] >= maxValue)
			{
				bp[i] = 0;
			}
		}
	}
}
