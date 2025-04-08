using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.Client.NoObf;

public class SystemRenderTerrain : ClientSystem
{
	private long lastPerformanceInfoupdateMilliseconds;

	private bool ready;

	private float msPerSecond = 0.001f;

	private long tesselationStop;

	public override string Name => "ret";

	public SystemRenderTerrain(ClientMain game)
		: base(game)
	{
		lastPerformanceInfoupdateMilliseconds = 0L;
		ClientSettings.Inst.AddWatcher<bool>("smoothShadows", delegate
		{
			RedrawAllBlocks();
		});
		ClientSettings.Inst.AddWatcher<bool>("instancedGrass", delegate
		{
			RedrawAllBlocks();
		});
		game.eventManager.RegisterRenderer(OnRenderBefore, EnumRenderStage.Before, "ret-prep", 0.995);
		game.eventManager.RegisterRenderer(OnRenderOpaque, EnumRenderStage.Opaque, "ret-op", 0.37);
		game.eventManager.RegisterRenderer(OnRenderShadow, EnumRenderStage.ShadowFar, "ret-sf", 0.37);
		game.eventManager.RegisterRenderer(OnRenderShadow, EnumRenderStage.ShadowNear, "ret-sn", 0.37);
		game.eventManager.RegisterRenderer(OnRenderOIT, EnumRenderStage.OIT, "ret-oit", 0.37);
		game.eventManager.RegisterPlayerPropertyChangedWatcher(EnumProperty.PlayerChunkPos, OnPlayerLeaveChunk);
	}

	private void OnPlayerLeaveChunk(TrackedPlayerProperties oldValues, TrackedPlayerProperties newValues)
	{
		int num = newValues.PlayerChunkPos.X - oldValues.PlayerChunkPos.X;
		int dz = newValues.PlayerChunkPos.Z - oldValues.PlayerChunkPos.Z;
		if (!((double)(num * num + dz * dz) > 25.0))
		{
			return;
		}
		List<long> indexes = new List<long>(game.dirtyChunks.Count);
		lock (game.dirtyChunksLock)
		{
			int count = game.dirtyChunks.Count;
			while (count-- > 0)
			{
				indexes.Add(game.dirtyChunks.Dequeue());
			}
		}
		lock (game.dirtyChunksLastLock)
		{
			for (int i = 0; i < indexes.Count; i++)
			{
				game.dirtyChunksLast.Enqueue(indexes[i]);
			}
		}
	}

	public override void OnSeperateThreadGameTick(float dt)
	{
		base.OnSeperateThreadGameTick(dt);
		if (ready)
		{
			game.chunkRenderer.OnSeperateThreadTick(dt);
		}
	}

	public override void OnBlockTexturesLoaded()
	{
		game.chunkRenderer = new ChunkRenderer(game.BlockAtlasManager.AtlasTextures.Select((LoadedTexture t) => t.TextureId).ToArray(), game);
	}

	public void OnRenderBefore(float deltaTime)
	{
		game.chunkRenderer.OnRenderBefore(deltaTime);
	}

	public void OnRenderOpaque(float deltaTime)
	{
		ready = true;
		if (game.Width != 0)
		{
			if (game.ShouldRedrawAllBlocks)
			{
				game.ShouldRedrawAllBlocks = false;
				RedrawAllBlocks();
			}
			game.chunkRenderer.OnBeforeRenderOpaque(deltaTime);
			game.chunkRenderer.RenderOpaque(deltaTime);
			UpdatePerformanceInfo(deltaTime);
		}
	}

	private void OnRenderShadow(float dt)
	{
		game.chunkRenderer.RenderShadow(dt);
	}

	public void OnRenderOIT(float deltaTime)
	{
		if (game.Width != 0)
		{
			game.chunkRenderer.RenderOIT(deltaTime);
		}
	}

	public override void Dispose(ClientMain game)
	{
		if (base.game.chunkRenderer != null)
		{
			base.game.chunkRenderer.Dispose();
		}
	}

	public void RedrawAllBlocks()
	{
		game.Platform.Logger.Notification("Redrawing all blocks");
		UniqueQueue<long> dirtyChunks = new UniqueQueue<long>();
		lock (game.WorldMap.chunksLock)
		{
			foreach (long index3d in game.WorldMap.chunks.Keys)
			{
				dirtyChunks.Enqueue(index3d);
			}
		}
		game.dirtyChunks = dirtyChunks;
	}

	internal void UpdatePerformanceInfo(float dt)
	{
		if ((float)(game.Platform.EllapsedMs - lastPerformanceInfoupdateMilliseconds) * msPerSecond >= 1f)
		{
			if (RuntimeStats.chunksTesselatedPerSecond == 0 && tesselationStop == 0L && RuntimeStats.chunksTesselatedTotal > 0)
			{
				tesselationStop = lastPerformanceInfoupdateMilliseconds;
			}
			lastPerformanceInfoupdateMilliseconds = game.Platform.EllapsedMs;
			long usedVideoMemory = 0L;
			long renderedTris = 0L;
			long allocatedTris = 0L;
			game.chunkRenderer.GetStats(out usedVideoMemory, out renderedTris, out allocatedTris);
			RuntimeStats.availableTriangles = (int)allocatedTris;
			RuntimeStats.renderedTriangles = (int)renderedTris;
			string videoMem = (usedVideoMemory / 1024 / 1024).ToString("#.# MB");
			game.DebugScreenInfo["triangles"] = "Terrain GPU Mem: " + videoMem + " in " + game.chunkRenderer.QuantityModelDataPools() + " pools, Tris: " + RuntimeStats.renderedTriangles.ToString("N0") + " / " + RuntimeStats.availableTriangles.ToString("N0");
			if (game.extendedDebugInfo)
			{
				game.DebugScreenInfo["gpumemfrag"] = "Terrain GPU Mem Fragmentation: " + (game.chunkRenderer.CalcFragmentation() * 100f).ToString("#.#") + "%";
			}
			else
			{
				game.DebugScreenInfo["gpumemfrag"] = "";
			}
			int avtessPerS = (int)((float)RuntimeStats.chunksTesselatedTotal * 1000f / ((float)(tesselationStop - RuntimeStats.tesselationStart) + 0.0001f));
			if (avtessPerS < 0)
			{
				avtessPerS = 0;
			}
			game.DebugScreenInfo["chunkstats"] = "Chunks rec=" + RuntimeStats.chunksReceived + ", ld=" + game.WorldMap.chunks.Count + ", tess/s=" + RuntimeStats.chunksTesselatedPerSecond + " (eo " + (int)((float)RuntimeStats.chunksTesselatedEdgeOnly * 100f / ((float)RuntimeStats.chunksTesselatedPerSecond + 0.0001f) + 0.5f) + "%), avtess/s=" + avtessPerS + ", tq=" + RuntimeStats.chunksAwaitingTesselation + ", p=" + RuntimeStats.chunksAwaitingPooling + ", rend=" + game.chunkRenderer.QuantityRenderingChunks + ", unl=" + RuntimeStats.chunksUnloaded;
			RuntimeStats.chunksTesselatedPerSecond = 0;
			RuntimeStats.chunksTesselatedEdgeOnly = 0;
		}
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Render;
	}
}
