using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class ChunkData : IChunkBlocks, IChunkLight, IEnumerable<int>, IEnumerable
{
	protected const uint LIGHTSATMASK = 7u;

	protected const uint NON_LIGHTSAT_MASK = 4294508543u;

	protected const uint SUNLIGHT_MASK = 4294901791u;

	protected const int NON_SUNLIGHT_MASK = -32;

	protected const int SUNLIGHT_MASK_INT = 31;

	protected const int NON_SUNLIGHT_MASK_INT = 65504;

	public const int NON_LIGHTSAT_MASK_INT = 65528;

	private const int chunksize = 32;

	protected const int length = 32768;

	protected const int INTSIZE = 32;

	public BlockChunkDataLayer blocksLayer;

	public BlockChunkDataLayer fluidsLayer;

	public ChunkDataLayer lightLayer;

	protected ChunkDataPool pool;

	[ThreadStatic]
	private static int[][] blocksTemp;

	[ThreadStatic]
	private static int[] arrayStatic;

	[ThreadStatic]
	private static ushort[] oldBlocks;

	[ThreadStatic]
	private static ushort[] oldLight;

	[ThreadStatic]
	private static byte[] oldLightSat;

	[ThreadStatic]
	private static byte[] oldBlocksTemp;

	public int this[int index3d]
	{
		get
		{
			int id = GetSolidBlock(index3d);
			if (id != 0)
			{
				return id;
			}
			return GetFluid(index3d);
		}
		set
		{
			if (blocksLayer == null)
			{
				blocksLayer = new BlockChunkDataLayer(pool);
			}
			blocksLayer.Set(index3d, value);
		}
	}

	public int Length => 32768;

	protected ChunkData(ChunkDataPool chunkDataPool)
	{
		pool = chunkDataPool;
	}

	public static ChunkData CreateNew(int chunksize, ChunkDataPool chunkDataPool)
	{
		if (32768 != chunksize * chunksize * chunksize)
		{
			throw new Exception("Server and client chunksizes do not match, this isn't going to work!");
		}
		return new ChunkData(chunkDataPool);
	}

	public int GetBlockId(int index, int layer)
	{
		switch (layer)
		{
		default:
		{
			int id = GetSolidBlock(index);
			if (id != 0)
			{
				return id;
			}
			return GetFluid(index);
		}
		case 1:
			return GetSolidBlock(index);
		case 2:
			return GetFluid(index);
		case 3:
		{
			int blockId2 = GetFluid(index);
			if (blockId2 != 0)
			{
				return blockId2;
			}
			return GetSolidBlock(index);
		}
		case 4:
		{
			int blockId = GetFluid(index);
			if (blockId == 0 || !pool.Game.Blocks[blockId].SideSolid.Any)
			{
				blockId = GetSolidBlock(index);
			}
			return blockId;
		}
		}
	}

	public int GetSolidBlock(int index3d)
	{
		return blocksLayer?.Get(index3d) ?? 0;
	}

	public int GetFluid(int index3d)
	{
		return fluidsLayer?.Get(index3d) ?? 0;
	}

	public void SetFluid(int index3d, int value)
	{
		if (fluidsLayer == null)
		{
			fluidsLayer = new BlockChunkDataLayer(pool);
		}
		fluidsLayer.Set(index3d, value);
	}

	public void SetLight(int index3d, uint value)
	{
		if (lightLayer == null)
		{
			lightLayer = new ChunkDataLayer(pool);
		}
		lightLayer.Set(index3d, (int)value);
	}

	public int GetBlockIdUnsafe(int index3d)
	{
		return blocksLayer?.GetUnsafe(index3d) ?? 0;
	}

	public int GetBlockIdUnsafe(int index3d, int layer)
	{
		switch (layer)
		{
		default:
		{
			int id = blocksLayer?.GetUnsafe_PaletteCheck(index3d) ?? 0;
			if (id != 0)
			{
				return id;
			}
			return fluidsLayer?.GetUnsafe_PaletteCheck(index3d) ?? 0;
		}
		case 1:
			return blocksLayer?.GetUnsafe_PaletteCheck(index3d) ?? 0;
		case 2:
			return fluidsLayer?.GetUnsafe_PaletteCheck(index3d) ?? 0;
		case 3:
		{
			int blockId2 = fluidsLayer?.GetUnsafe_PaletteCheck(index3d) ?? 0;
			if (blockId2 != 0)
			{
				return blockId2;
			}
			return blocksLayer?.GetUnsafe_PaletteCheck(index3d) ?? 0;
		}
		case 4:
		{
			int blockId = fluidsLayer?.GetUnsafe_PaletteCheck(index3d) ?? 0;
			if (blockId == 0 || !pool.Game.Blocks[blockId].SideSolid.Any)
			{
				blockId = blocksLayer?.GetUnsafe_PaletteCheck(index3d) ?? 0;
			}
			return blockId;
		}
		}
	}

	public void TakeBulkReadLock()
	{
		blocksLayer?.readWriteLock.AcquireReadLock();
		fluidsLayer?.readWriteLock.AcquireReadLock();
	}

	public void ReleaseBulkReadLock()
	{
		fluidsLayer?.readWriteLock.ReleaseReadLock();
		blocksLayer?.readWriteLock.ReleaseReadLock();
	}

	public void SetBlockBulk(int index3d, int lenX, int lenZ, int value)
	{
		if (blocksLayer == null)
		{
			if (value == 0)
			{
				return;
			}
			blocksLayer = new BlockChunkDataLayer(pool);
		}
		blocksLayer.SetBulk(index3d, lenX, lenZ, value);
	}

	public void SetBlockUnsafe(int index3d, int value)
	{
		if (blocksLayer == null)
		{
			if (value == 0)
			{
				return;
			}
			blocksLayer = new BlockChunkDataLayer(pool);
		}
		blocksLayer.SetUnsafe(index3d, value);
	}

	public void SetBlockAir(int index3d)
	{
		blocksLayer?.SetZero(index3d);
	}

	internal virtual void EmptyAndReuseArrays(List<int[]> datas)
	{
		EmptyBlocksData(datas);
		EmptyLightData(datas);
		EmptyFluidsData(datas);
	}

	private void EmptyBlocksData(List<int[]> datas)
	{
		ChunkDataLayer old = blocksLayer;
		if (old != null)
		{
			blocksLayer = null;
			old.Clear(datas);
		}
	}

	private void EmptyFluidsData(List<int[]> datas)
	{
		ChunkDataLayer old = fluidsLayer;
		if (old != null)
		{
			fluidsLayer = null;
			old.Clear(datas);
		}
	}

	private void EmptyLightData(List<int[]> datas)
	{
		ChunkDataLayer old = lightLayer;
		if (old != null)
		{
			lightLayer = null;
			old.Clear(datas);
		}
	}

	public void ClearBlocks()
	{
		pool.FreeArraysAndReset(this);
		blocksLayer = null;
	}

	public void ClearBlocksAndPrepare()
	{
		ClearBlocks();
		blocksLayer = new BlockChunkDataLayer(pool);
		blocksLayer.PopulateWithAir();
	}

	public void ClearWithSunlight(ushort sunlight)
	{
		pool.FreeArraysAndReset(this);
		lightLayer = new ChunkDataLayer(pool);
		lightLayer.FillWithInitialValue(sunlight);
	}

	public void FloodWithSunlight(ushort sunlight)
	{
		ClearLight();
		lightLayer = new ChunkDataLayer(pool);
		lightLayer.FillWithInitialValue(sunlight);
	}

	public void ClearAllSunlight()
	{
		if (lightLayer != null)
		{
			for (int i = 0; i < 32768; i++)
			{
				lightLayer.Set(i, lightLayer.Get(i) & -32);
			}
		}
	}

	public void ClearLight()
	{
		if (lightLayer != null)
		{
			ChunkDataLayer old = lightLayer;
			lightLayer = null;
			pool.FreeArrays(old);
		}
	}

	public IEnumerator<int> GetEnumerator()
	{
		return new BlocksCompositeIdEnumerator(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new BlocksCompositeIdEnumerator(this);
	}

	internal void CompressInto(ref byte[] blocksCompressed, ref byte[] lightCompressed, ref byte[] lightPaletteCompressed, ref byte[] fluidsCompressed, int chunkdataVersion)
	{
		if (arrayStatic == null)
		{
			arrayStatic = new int[15360];
		}
		blocksCompressed = ChunkDataLayer.Compress(blocksLayer, arrayStatic);
		fluidsCompressed = ChunkDataLayer.Compress(fluidsLayer, arrayStatic);
		ChunkDataLayer lightLayer = this.lightLayer;
		if (lightLayer == null)
		{
			lightCompressed = new byte[0];
			lightPaletteCompressed = new byte[0];
		}
		else
		{
			lightCompressed = lightLayer.CompressSeparate(ref lightPaletteCompressed, arrayStatic, chunkdataVersion);
		}
	}

	internal void DecompressFrom(byte[] blocksCompressed, byte[] lightCompressed, byte[] lightPaletteCompressed, byte[] fluidsCompressed, int chunkdataVersion)
	{
		if (chunkdataVersion == 0)
		{
			OldStyleUnpack(blocksCompressed, lightCompressed, lightPaletteCompressed);
			return;
		}
		if (blocksCompressed != null)
		{
			blocksLayer = new BlockChunkDataLayer(pool);
			blocksLayer.Decompress(blocksCompressed);
		}
		else
		{
			blocksLayer = null;
		}
		if (fluidsCompressed != null)
		{
			fluidsLayer = new BlockChunkDataLayer(pool);
			fluidsLayer.Decompress(fluidsCompressed);
		}
		else
		{
			fluidsLayer = null;
		}
		UpdateFluids();
		if (lightCompressed == null || lightPaletteCompressed == null || lightCompressed.Length == 0 || lightPaletteCompressed.Length == 0)
		{
			lightLayer = null;
			return;
		}
		ChunkDataLayer lightLayerNew = new ChunkDataLayer(pool);
		lightLayerNew.DecompressSeparate(lightCompressed, lightPaletteCompressed);
		lightLayer = lightLayerNew;
	}

	private void OldStyleUnpack(byte[] blocksCompressed, byte[] lightCompressed, byte[] lightSatCompressed)
	{
		if (oldBlocksTemp == null)
		{
			oldBlocksTemp = new byte[65536];
		}
		if (oldBlocks == null)
		{
			oldBlocks = new ushort[32768];
		}
		if (oldLight == null)
		{
			oldLight = new ushort[32768];
		}
		if (oldLightSat == null)
		{
			oldLightSat = new byte[32768];
		}
		Compression.DecompressToUshort(blocksCompressed, oldBlocks, oldBlocksTemp, 0);
		Compression.DecompressToUshort(lightCompressed, oldLight, oldBlocksTemp, 0);
		Compression.Decompress(lightSatCompressed, oldLightSat, 0);
		CreateNewDataFromOld();
	}

	private void CreateNewDataFromOld()
	{
		pool.FreeArraysAndReset(this);
		blocksLayer = new BlockChunkDataLayer(pool);
		blocksLayer.PopulateFrom(oldBlocks, oldLightSat);
		for (int i = 0; i < 32768; i++)
		{
			SetLight(i, (uint)(oldLight[i] | ((oldLightSat[i] & 7) << 16)));
		}
	}

	internal bool IsEmpty()
	{
		if (blocksLayer == null && fluidsLayer == null)
		{
			return true;
		}
		if (blocksLayer != null && blocksLayer.HasContents())
		{
			return false;
		}
		if (fluidsLayer != null && fluidsLayer.HasContents())
		{
			return false;
		}
		return true;
	}

	internal bool HasData()
	{
		return true;
	}

	internal void CopyBlocksTo(int[] blocksOut)
	{
		if (blocksLayer == null || blocksLayer.palette == null)
		{
			for (int i = 0; i < blocksOut.Length; i += 4)
			{
				blocksOut[i] = 0;
				blocksOut[i + 1] = 0;
				blocksOut[i + 2] = 0;
				blocksOut[i + 3] = 0;
			}
		}
		else
		{
			blocksLayer.CopyBlocksTo(blocksOut);
		}
	}

	internal static void UnpackBlocksTo(int[] blocksOut, byte[] blocksCompressed, byte[] lightSatCompressed, int chunkdataVersion)
	{
		if (chunkdataVersion == 0)
		{
			if (oldBlocksTemp == null)
			{
				oldBlocksTemp = new byte[blocksOut.Length * 2];
			}
			if (oldBlocks == null)
			{
				oldBlocks = new ushort[blocksOut.Length];
			}
			if (oldLightSat == null)
			{
				oldLightSat = new byte[blocksOut.Length];
			}
			Compression.DecompressToUshort(blocksCompressed, oldBlocks, oldBlocksTemp, chunkdataVersion);
			Compression.Decompress(lightSatCompressed, oldLightSat, chunkdataVersion);
			for (int i = 0; i < blocksOut.Length; i += 4)
			{
				blocksOut[i] = oldBlocksTemp[i] | ((oldLightSat[i] & 0xFFF8) << 13);
				blocksOut[i + 1] = oldBlocksTemp[i + 1] | ((oldLightSat[i + 1] & 0xFFF8) << 13);
				blocksOut[i + 2] = oldBlocksTemp[i + 2] | ((oldLightSat[i + 2] & 0xFFF8) << 13);
				blocksOut[i + 3] = oldBlocksTemp[i + 3] | ((oldLightSat[i + 3] & 0xFFF8) << 13);
			}
			return;
		}
		if (blocksTemp == null)
		{
			blocksTemp = new int[15][];
			for (int k = 0; k < 15; k++)
			{
				blocksTemp[k] = new int[1024];
			}
		}
		int bpcUnused = 0;
		int[] blocksPalette = Compression.DecompressCombined(blocksCompressed, ref blocksTemp, ref bpcUnused, null);
		if (blocksPalette == null)
		{
			for (int j = 0; j < blocksOut.Length; j += 4)
			{
				blocksOut[j] = 0;
				blocksOut[j + 1] = 0;
				blocksOut[j + 2] = 0;
				blocksOut[j + 3] = 0;
			}
			return;
		}
		int blocksBitsize = 0;
		int bc = blocksPalette.Length;
		while ((bc >>= 1) > 0)
		{
			blocksBitsize++;
		}
		for (int index3d = 0; index3d < blocksOut.Length; index3d += 32)
		{
			int intIndex = index3d / 32;
			for (int bitIndex = 0; bitIndex < 32; bitIndex++)
			{
				int idx = 0;
				int bitValue = 1;
				for (int l = 0; l < blocksBitsize; l++)
				{
					idx += ((blocksTemp[l][intIndex] >> bitIndex) & 1) * bitValue;
					bitValue *= 2;
				}
				blocksOut[index3d + bitIndex] = blocksPalette[idx];
			}
		}
	}

	internal void UpdateFluids()
	{
		if (blocksLayer != null)
		{
			if (fluidsLayer == null)
			{
				fluidsLayer = new BlockChunkDataLayer(pool);
			}
			blocksLayer.UpdateToFluidsLayer(fluidsLayer);
		}
	}

	internal ushort ReadLight(int index, out int lightSat)
	{
		ChunkDataLayer lightLayer = this.lightLayer;
		if (lightLayer == null)
		{
			lightSat = 0;
			return 0;
		}
		uint i = (uint)lightLayer.Get(index);
		lightSat = (int)((i >> 16) & 7);
		return (ushort)i;
	}

	internal ushort ReadLight(int index)
	{
		ChunkDataLayer lightLayer = this.lightLayer;
		if (lightLayer == null)
		{
			return 0;
		}
		return (ushort)lightLayer.Get(index);
	}

	internal uint Light(int index)
	{
		return (uint)(lightLayer?.Get(index) ?? 0);
	}

	public virtual int GetSunlight(int index3d)
	{
		ChunkDataLayer lightLayer = this.lightLayer;
		if (lightLayer == null)
		{
			return 0;
		}
		return lightLayer.Get(index3d) & 0x1F;
	}

	public virtual void SetSunlight(int index3d, int sunLevel)
	{
		ChunkDataLayer lightLayer = this.lightLayer;
		if (lightLayer == null)
		{
			this.lightLayer = new ChunkDataLayer(pool);
			this.lightLayer.Set(index3d, sunLevel);
		}
		else
		{
			lightLayer.Set(index3d, (lightLayer.Get(index3d) & -32) | sunLevel);
		}
	}

	public virtual void SetSunlight_Buffered(int index3d, int sunLevel)
	{
		ChunkDataLayer lightLayer = this.lightLayer;
		if (lightLayer == null)
		{
			this.lightLayer = new ChunkDataLayer(pool);
			this.lightLayer.Set(index3d, sunLevel);
		}
		else
		{
			lightLayer.Set(index3d, (lightLayer.Get(index3d) & -32) | sunLevel);
		}
	}

	public virtual int GetBlocklight(int index3d)
	{
		ChunkDataLayer lightLayer = this.lightLayer;
		if (lightLayer == null)
		{
			return 0;
		}
		return (lightLayer.Get(index3d) >> 5) & 0x1F;
	}

	public virtual void SetBlocklight(int index3d, int lightLevel)
	{
		ChunkDataLayer lightLayer = this.lightLayer;
		if (lightLayer == null)
		{
			this.lightLayer = new ChunkDataLayer(pool);
			this.lightLayer.Set(index3d, lightLevel);
		}
		else
		{
			lightLayer.Set(index3d, (lightLayer.Get(index3d) & 0x1F) | lightLevel);
		}
	}

	public virtual void SetBlocklight_Buffered(int index3d, int lightLevel)
	{
		ChunkDataLayer lightLayer = this.lightLayer;
		if (lightLayer == null)
		{
			this.lightLayer = new ChunkDataLayer(pool);
			this.lightLayer.Set(index3d, lightLevel);
		}
		else
		{
			lightLayer.Set(index3d, (lightLayer.Get(index3d) & 0x1F) | lightLevel);
		}
	}

	internal BlockPos FindFirst(List<int> searchIds)
	{
		if (blocksLayer == null)
		{
			return null;
		}
		return blocksLayer.FindFirst(searchIds);
	}

	public bool ContainsBlock(int id)
	{
		if (blocksLayer == null)
		{
			return false;
		}
		return blocksLayer.Contains(id);
	}

	public void FuzzyListBlockIds(List<int> reusableList)
	{
		blocksLayer?.ListAllPaletteValues(reusableList);
	}
}
