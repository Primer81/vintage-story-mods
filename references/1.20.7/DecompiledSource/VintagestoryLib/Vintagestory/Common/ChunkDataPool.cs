using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace Vintagestory.Common;

public class ChunkDataPool : IShutDownMonitor
{
	protected List<int[]> datas = new List<int[]>();

	protected int chunksize;

	protected int quantityRequestsSinceLastSlowDispose;

	public int SlowDisposeThreshold = 1000;

	public int CacheSize = 1500;

	public ChunkData BlackHoleData;

	public IChunkBlocks OnlyAirBlocksData;

	public ServerMain server;

	public virtual bool ShuttingDown => server.RunPhase >= EnumServerRunPhase.Shutdown;

	public virtual GameMain Game => server;

	public virtual ILogger Logger => ServerMain.Logger;

	protected ChunkDataPool()
	{
	}

	public ChunkDataPool(int chunksize, ServerMain serverMain)
	{
		this.chunksize = chunksize;
		BlackHoleData = ChunkData.CreateNew(chunksize, this);
		OnlyAirBlocksData = NoChunkData.CreateNew(chunksize);
		server = serverMain;
	}

	public void FreeAll()
	{
		lock (datas)
		{
			datas.Clear();
		}
	}

	public virtual ChunkData Request()
	{
		quantityRequestsSinceLastSlowDispose++;
		return ChunkData.CreateNew(chunksize, this);
	}

	public void Free(ChunkData cdata)
	{
		FreeArraysAndReset(cdata);
	}

	public void FreeArrays(ChunkDataLayer layer)
	{
		lock (datas)
		{
			layer.Clear(datas);
		}
	}

	public void FreeArraysAndReset(ChunkData cdata)
	{
		lock (datas)
		{
			if (datas.Count < CacheSize * 2)
			{
				cdata.EmptyAndReuseArrays(datas);
			}
			else
			{
				cdata.EmptyAndReuseArrays(null);
			}
		}
	}

	internal void Return(int[] released)
	{
		if (released == null)
		{
			throw new Exception("attempting to return null to pool");
		}
		lock (datas)
		{
			if (datas.Count < CacheSize * 2)
			{
				datas.Add(released);
			}
		}
	}

	public void SlowDispose()
	{
		if (quantityRequestsSinceLastSlowDispose > 50)
		{
			quantityRequestsSinceLastSlowDispose = 0;
			return;
		}
		quantityRequestsSinceLastSlowDispose = 0;
		lock (datas)
		{
			if (datas.Count > SlowDisposeThreshold * 4)
			{
				for (int i = 0; i < SlowDisposeThreshold * 2; i++)
				{
					datas.RemoveAt(datas.Count - 1);
				}
			}
		}
	}

	public int CountFree()
	{
		return datas.Count;
	}

	internal int[] NewData()
	{
		int[] result;
		lock (datas)
		{
			if (datas.Count == 0)
			{
				result = new int[1024];
			}
			else
			{
				result = datas[datas.Count - 1];
				datas.RemoveAt(datas.Count - 1);
				for (int i = 0; i < result.Length; i += 4)
				{
					result[i] = 0;
					result[i + 1] = 0;
					result[i + 2] = 0;
					result[i + 3] = 0;
				}
			}
		}
		return result;
	}

	internal int[] NewData_NoClear()
	{
		int[] result;
		lock (datas)
		{
			if (datas.Count == 0)
			{
				result = new int[1024];
			}
			else
			{
				result = datas[datas.Count - 1];
				datas.RemoveAt(datas.Count - 1);
			}
		}
		return result;
	}
}
