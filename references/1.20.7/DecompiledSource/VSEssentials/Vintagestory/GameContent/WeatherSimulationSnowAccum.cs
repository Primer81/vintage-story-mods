using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class WeatherSimulationSnowAccum
{
	private int[][] randomShuffles;

	private ICoreServerAPI sapi;

	private WeatherSystemBase ws;

	private Thread snowLayerScannerThread;

	private bool isShuttingDown;

	private UniqueQueue<Vec2i> chunkColsstoCheckQueue = new UniqueQueue<Vec2i>();

	private UniqueQueue<UpdateSnowLayerChunk> updateSnowLayerQueue = new UniqueQueue<UpdateSnowLayerChunk>();

	private const int chunksize = 32;

	private int regionsize;

	internal float accum;

	public bool ProcessChunks = true;

	public bool enabled;

	private IBulkBlockAccessor ba;

	private IBulkBlockAccessor cuba;

	private bool shouldPauseThread;

	private bool isThreadPaused;

	public WeatherSimulationSnowAccum(ICoreServerAPI sapi, WeatherSystemBase ws)
	{
		this.sapi = sapi;
		this.ws = ws;
		ba = sapi.World.GetBlockAccessorBulkMinimalUpdate(synchronize: true);
		ba.UpdateSnowAccumMap = false;
		cuba = sapi.World.GetBlockAccessorMapChunkLoading(synchronize: false);
		cuba.UpdateSnowAccumMap = false;
		initRandomShuffles();
		sapi.Event.BeginChunkColumnLoadChunkThread += Event_BeginChunkColLoadChunkThread;
		sapi.Event.ChunkColumnLoaded += Event_ChunkColumnLoaded;
		sapi.Event.SaveGameLoaded += Event_SaveGameLoaded;
		sapi.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, delegate
		{
			isShuttingDown = true;
		});
		sapi.Event.RegisterGameTickListener(OnServerTick3s, 3000);
		sapi.Event.RegisterGameTickListener(OnServerTick100ms, 100);
		sapi.Event.ServerSuspend += Event_ServerSuspend;
		sapi.Event.ServerResume += Event_ServerResume;
		snowLayerScannerThread = TyronThreadPool.CreateDedicatedThread(onThreadStart, "snowlayerScanner");
	}

	private void Event_ServerResume()
	{
		shouldPauseThread = false;
	}

	private EnumSuspendState Event_ServerSuspend()
	{
		shouldPauseThread = true;
		if (isThreadPaused || !enabled)
		{
			return EnumSuspendState.Ready;
		}
		return EnumSuspendState.Wait;
	}

	private void Event_SaveGameLoaded()
	{
		regionsize = sapi.WorldManager.RegionSize;
		if (regionsize == 0)
		{
			sapi.Logger.Notification("Warning: region size was 0 for Snow Accum system");
			regionsize = 16;
		}
		enabled = sapi.World.Config.GetBool("snowAccum", defaultValue: true);
		GlobalConstants.MeltingFreezingEnabled = enabled;
		if (enabled)
		{
			snowLayerScannerThread.Start();
		}
	}

	private void OnServerTick3s(float dt)
	{
		if (!ProcessChunks || !enabled)
		{
			return;
		}
		foreach (KeyValuePair<long, IMapChunk> val in sapi.WorldManager.AllLoadedMapchunks)
		{
			Vec2i chunkCoord = sapi.WorldManager.MapChunkPosFromChunkIndex2D(val.Key);
			lock (chunkColsstoCheckQueue)
			{
				chunkColsstoCheckQueue.Enqueue(chunkCoord);
			}
		}
	}

	public void AddToCheckQueue(Vec2i chunkCoord)
	{
		lock (chunkColsstoCheckQueue)
		{
			chunkColsstoCheckQueue.Enqueue(chunkCoord);
		}
	}

	private void OnServerTick100ms(float dt)
	{
		accum += dt;
		if (updateSnowLayerQueue.Count <= 5 && (!(accum > 1f) || updateSnowLayerQueue.Count <= 0))
		{
			return;
		}
		accum = 0f;
		int cnt = 0;
		int max = 10;
		UpdateSnowLayerChunk[] q = new UpdateSnowLayerChunk[max];
		lock (updateSnowLayerQueue)
		{
			while (updateSnowLayerQueue.Count > 0)
			{
				q[cnt] = updateSnowLayerQueue.Dequeue();
				cnt++;
				if (cnt >= max)
				{
					break;
				}
			}
		}
		for (int i = 0; i < cnt; i++)
		{
			IMapChunk mc = sapi.WorldManager.GetMapChunk(q[i].Coords.X, q[i].Coords.Y);
			if (mc != null)
			{
				processBlockUpdates(mc, q[i], ba);
			}
		}
		ba.Commit();
	}

	internal void processBlockUpdates(IMapChunk mc, UpdateSnowLayerChunk updateChunk, IBulkBlockAccessor ba)
	{
		Dictionary<BlockPos, BlockIdAndSnowLevel> setBlocks = updateChunk.SetBlocks;
		double lastSnowAccumUpdateTotalHours = updateChunk.LastSnowAccumUpdateTotalHours;
		Vec2i tmpVec = new Vec2i();
		foreach (KeyValuePair<BlockPos, BlockIdAndSnowLevel> sval in setBlocks)
		{
			Block newblock = sval.Value.Block;
			float snowLevel = sval.Value.SnowLevel;
			Block hereblock = ba.GetBlock(sval.Key);
			tmpVec.Set(sval.Key.X, sval.Key.Z);
			if (!(snowLevel > 0f) || mc.SnowAccum.ContainsKey(tmpVec))
			{
				hereblock.PerformSnowLevelUpdate(ba, sval.Key, newblock, snowLevel);
			}
		}
		mc.SetModdata("lastSnowAccumUpdateTotalHours", SerializerUtil.Serialize(lastSnowAccumUpdateTotalHours));
		mc.MarkDirty();
	}

	private void Event_ChunkColumnLoaded(Vec2i chunkCoord, IWorldChunk[] chunks)
	{
		if (!ProcessChunks)
		{
			return;
		}
		int regionX = chunkCoord.X * 32 / regionsize;
		int regionZ = chunkCoord.Y * 32 / regionsize;
		WeatherSimulationRegion simregion = ws.getOrCreateWeatherSimForRegion(regionX, regionZ);
		if (sapi.WorldManager.GetMapChunk(chunkCoord.X, chunkCoord.Y) == null || simregion == null)
		{
			return;
		}
		lock (chunkColsstoCheckQueue)
		{
			chunkColsstoCheckQueue.Enqueue(chunkCoord);
		}
	}

	private void Event_BeginChunkColLoadChunkThread(IServerMapChunk mc, int chunkX, int chunkZ, IWorldChunk[] chunks)
	{
		if (ProcessChunks)
		{
			int regionX = chunkX * 32 / regionsize;
			int regionZ = chunkZ * 32 / regionsize;
			WeatherSimulationRegion simregion = ws.getOrCreateWeatherSimForRegion(regionX, regionZ);
			if (simregion != null)
			{
				TryImmediateSnowUpdate(simregion, mc, new Vec2i(chunkX, chunkZ), chunks);
			}
		}
	}

	private bool TryImmediateSnowUpdate(WeatherSimulationRegion simregion, IServerMapChunk mc, Vec2i chunkCoord, IWorldChunk[] chunksCol)
	{
		UpdateSnowLayerChunk dummy = new UpdateSnowLayerChunk
		{
			Coords = chunkCoord
		};
		lock (updateSnowLayerQueue)
		{
			if (updateSnowLayerQueue.Contains(dummy))
			{
				return false;
			}
		}
		if (ws.api.World.Calendar.TotalHours - simregion.LastUpdateTotalHours > 1.0)
		{
			return false;
		}
		UpdateSnowLayerChunk ch = GetSnowUpdate(simregion, mc, chunkCoord, chunksCol);
		if (ch == null)
		{
			return true;
		}
		if (ch.SetBlocks.Count == 0)
		{
			return true;
		}
		cuba.SetChunks(chunkCoord, chunksCol);
		processBlockUpdates(mc, ch, cuba);
		cuba.Commit();
		lock (updateSnowLayerQueue)
		{
			updateSnowLayerQueue.Enqueue(dummy);
		}
		return true;
	}

	private void onThreadStart()
	{
		FrameProfilerUtil FrameProfiler = new FrameProfilerUtil("[Thread snowaccum] ");
		while (!isShuttingDown)
		{
			Thread.Sleep(5);
			if (shouldPauseThread)
			{
				isThreadPaused = true;
				continue;
			}
			isThreadPaused = false;
			FrameProfiler.Begin(null);
			int i = 0;
			while (chunkColsstoCheckQueue.Count > 0 && i++ < 10)
			{
				Vec2i chunkCoord;
				lock (chunkColsstoCheckQueue)
				{
					chunkCoord = chunkColsstoCheckQueue.Dequeue();
				}
				int regionX = chunkCoord.X * 32 / regionsize;
				int regionZ = chunkCoord.Y * 32 / regionsize;
				WeatherSimulationRegion sim = ws.getOrCreateWeatherSimForRegion(regionX, regionZ);
				IServerMapChunk mc = sapi.WorldManager.GetMapChunk(chunkCoord.X, chunkCoord.Y);
				if (mc != null && sim != null)
				{
					UpdateSnowLayerOffThread(sim, mc, chunkCoord);
					FrameProfiler.Mark("update " + chunkCoord);
				}
			}
			FrameProfiler.OffThreadEnd();
		}
	}

	private void initRandomShuffles()
	{
		randomShuffles = new int[50][];
		for (int i = 0; i < randomShuffles.Length; i++)
		{
			int[] coords = (randomShuffles[i] = new int[1024]);
			for (int j = 0; j < coords.Length; j++)
			{
				coords[j] = j;
			}
			GameMath.Shuffle(sapi.World.Rand, coords);
		}
	}

	public void UpdateSnowLayerOffThread(WeatherSimulationRegion simregion, IServerMapChunk mc, Vec2i chunkPos)
	{
		UpdateSnowLayerChunk ch = new UpdateSnowLayerChunk
		{
			Coords = chunkPos
		};
		lock (updateSnowLayerQueue)
		{
			if (updateSnowLayerQueue.Contains(ch))
			{
				return;
			}
		}
		if (simregion == null || ws.api.World.Calendar.TotalHours - simregion.LastUpdateTotalHours > 1.0)
		{
			return;
		}
		ch = GetSnowUpdate(simregion, mc, chunkPos, null);
		if (ch == null)
		{
			return;
		}
		lock (updateSnowLayerQueue)
		{
			updateSnowLayerQueue.Enqueue(ch);
		}
	}

	private UpdateSnowLayerChunk GetSnowUpdate(WeatherSimulationRegion simregion, IServerMapChunk mc, Vec2i chunkPos, IWorldChunk[] chunksCol)
	{
		double lastSnowAccumUpdateTotalHours = mc.GetModdata("lastSnowAccumUpdateTotalHours", 0.0);
		double startTotalHours = lastSnowAccumUpdateTotalHours;
		int reso = WeatherSimulationRegion.snowAccumResolution;
		SnowAccumSnapshot sumsnapshot = new SnowAccumSnapshot
		{
			SnowAccumulationByRegionCorner = new FloatDataMap3D(reso, reso, reso)
		};
		float[] sumdata = sumsnapshot.SnowAccumulationByRegionCorner.Data;
		float max = (float)ws.GeneralConfig.SnowLayerBlocks.Count + 0.6f;
		int len = simregion.SnowAccumSnapshots.Length;
		int i = simregion.SnowAccumSnapshots.EndPosition;
		int newCount = 0;
		lock (WeatherSimulationRegion.snowAccumSnapshotLock)
		{
			while (len-- > 0)
			{
				SnowAccumSnapshot hoursnapshot = simregion.SnowAccumSnapshots[i];
				i = (i + 1) % simregion.SnowAccumSnapshots.Length;
				if (hoursnapshot != null && !(lastSnowAccumUpdateTotalHours >= hoursnapshot.TotalHours))
				{
					float[] snowaccumdata = hoursnapshot.SnowAccumulationByRegionCorner.Data;
					for (int j = 0; j < snowaccumdata.Length; j++)
					{
						sumdata[j] = GameMath.Clamp(sumdata[j] + snowaccumdata[j], 0f - max, max);
					}
					lastSnowAccumUpdateTotalHours = Math.Max(lastSnowAccumUpdateTotalHours, hoursnapshot.TotalHours);
					newCount++;
				}
			}
		}
		if (newCount == 0)
		{
			return null;
		}
		bool ignoreOldAccum = false;
		if (lastSnowAccumUpdateTotalHours - startTotalHours >= (double)((float)sapi.World.Calendar.DaysPerYear * sapi.World.Calendar.HoursPerDay))
		{
			ignoreOldAccum = true;
		}
		UpdateSnowLayerChunk ch = UpdateSnowLayer(sumsnapshot, ignoreOldAccum, mc, chunkPos, chunksCol);
		if (ch != null)
		{
			ch.LastSnowAccumUpdateTotalHours = lastSnowAccumUpdateTotalHours;
			ch.Coords = chunkPos.Copy();
		}
		return ch;
	}

	public UpdateSnowLayerChunk UpdateSnowLayer(SnowAccumSnapshot sumsnapshot, bool ignoreOldAccum, IServerMapChunk mc, Vec2i chunkPos, IWorldChunk[] chunksCol)
	{
		UpdateSnowLayerChunk updateChunk = new UpdateSnowLayerChunk();
		OrderedDictionary<Block, int> layers = ws.GeneralConfig.SnowLayerBlocks;
		int chunkX = chunkPos.X;
		int chunkZ = chunkPos.Y;
		int regionX = chunkX * 32 / regionsize;
		int num = chunkZ * 32 / regionsize;
		int regionBasePosX = regionX * regionsize;
		int regionBasePosZ = num * regionsize;
		BlockPos pos = new BlockPos();
		BlockPos placePos = new BlockPos();
		float aboveSeaLevelHeight = sapi.World.BlockAccessor.MapSizeY - sapi.World.SeaLevel;
		int[] posIndices = randomShuffles[sapi.World.Rand.Next(randomShuffles.Length)];
		int prevChunkY = -99999;
		IWorldChunk chunk = null;
		int maxY = sapi.World.BlockAccessor.MapSizeY - 1;
		foreach (int posIndex in posIndices)
		{
			int posY = GameMath.Clamp(mc.RainHeightMap[posIndex], 0, maxY);
			int chunkY = posY / 32;
			pos.Set(chunkX * 32 + posIndex % 32, posY, chunkZ * 32 + posIndex / 32);
			if (prevChunkY != chunkY)
			{
				chunk = ((chunksCol != null) ? chunksCol[chunkY] : null) ?? sapi.WorldManager.GetChunk(chunkX, chunkY, chunkZ);
				prevChunkY = chunkY;
			}
			if (chunk == null)
			{
				return null;
			}
			float relx = (float)(pos.X - regionBasePosX) / (float)regionsize;
			float rely = GameMath.Clamp((float)(pos.Y - sapi.World.SeaLevel) / aboveSeaLevelHeight, 0f, 1f);
			float relz = (float)(pos.Z - regionBasePosZ) / (float)regionsize;
			Block block = chunk.GetLocalBlockAtBlockPos(sapi.World, pos);
			Block blockf = chunk.GetLocalBlockAtBlockPos(sapi.World, pos.X, pos.Y, pos.Z, 2);
			if (blockf.Id != 0)
			{
				if (!blockf.IsLiquid())
				{
					block = blockf;
				}
				else if (block.GetSnowLevel(pos) == 0f)
				{
					continue;
				}
			}
			float hereAccum = 0f;
			Vec2i vec = new Vec2i(pos.X, pos.Z);
			if (!ignoreOldAccum && !mc.SnowAccum.TryGetValue(vec, out hereAccum))
			{
				hereAccum = block.GetSnowLevel(pos);
			}
			float nowAccum = hereAccum + sumsnapshot.GetAvgSnowAccumByRegionCorner(relx, rely, relz);
			mc.SnowAccum[vec] = GameMath.Clamp(nowAccum, -1f, (float)ws.GeneralConfig.SnowLayerBlocks.Count + 0.6f);
			float hereShouldLevel = nowAccum - (float)GameMath.MurmurHash3Mod(pos.X, 0, pos.Z, 150) / 300f;
			float shouldIndexf = GameMath.Clamp(hereShouldLevel - 1.1f, -1f, ws.GeneralConfig.SnowLayerBlocks.Count - 1);
			int shouldIndex = ((shouldIndexf < 0f) ? (-1) : ((int)shouldIndexf));
			placePos.Set(pos.X, Math.Min(pos.Y + 1, sapi.World.BlockAccessor.MapSizeY - 1), pos.Z);
			chunkY = placePos.Y / 32;
			if (prevChunkY != chunkY)
			{
				chunk = ((chunksCol != null) ? chunksCol[chunkY] : null) ?? sapi.WorldManager.GetChunk(chunkX, chunkY, chunkZ);
				prevChunkY = chunkY;
			}
			if (chunk == null)
			{
				return null;
			}
			Block upBlock = chunk.GetLocalBlockAtBlockPos(sapi.World, placePos);
			Block upblockf = chunk.GetLocalBlockAtBlockPos(sapi.World, placePos.X, placePos.Y, placePos.Z, 2);
			if (upblockf.Id != 0)
			{
				if (!upblockf.IsLiquid())
				{
					upBlock = upblockf;
				}
				else if (upBlock.GetSnowLevel(pos) == 0f)
				{
					continue;
				}
			}
			placePos.Set(pos);
			Block newblock = block.GetSnowCoveredVariant(placePos, hereShouldLevel);
			if (newblock != null)
			{
				if (block.Id != newblock.Id && upBlock.Replaceable > 6000)
				{
					updateChunk.SetBlocks[placePos.Copy()] = new BlockIdAndSnowLevel(newblock, hereShouldLevel);
				}
			}
			else
			{
				if (!block.AllowSnowCoverage(sapi.World, placePos))
				{
					continue;
				}
				placePos.Set(pos.X, pos.Y + 1, pos.Z);
				if (upBlock.Id != 0)
				{
					newblock = upBlock.GetSnowCoveredVariant(placePos, hereShouldLevel);
					if (newblock != null && upBlock.Id != newblock.Id)
					{
						updateChunk.SetBlocks[placePos.Copy()] = new BlockIdAndSnowLevel(newblock, hereShouldLevel);
					}
				}
				else if (shouldIndex >= 0)
				{
					Block toPlaceBlock = layers.GetKeyAtIndex(shouldIndex);
					updateChunk.SetBlocks[placePos.Copy()] = new BlockIdAndSnowLevel(toPlaceBlock, hereShouldLevel);
				}
			}
		}
		return updateChunk;
	}
}
