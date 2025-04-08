using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemUnloadChunks : ClientSystem
{
	public override string Name => "uc";

	public SystemUnloadChunks(ClientMain game)
		: base(game)
	{
		game.PacketHandlers[11] = HandleChunkUnload;
	}

	private void HandleChunkUnload(Packet_Server packet)
	{
		int chunkMapSizeX = game.WorldMap.index3dMulX;
		int chunkMapSizeZ = game.WorldMap.index3dMulZ;
		HashSet<Vec2i> horCoods = new HashSet<Vec2i>();
		int count = packet.UnloadChunk.GetXCount();
		for (int j = 0; j < count; j++)
		{
			int cx2 = packet.UnloadChunk.X[j];
			int cy2 = packet.UnloadChunk.Y[j];
			int cz2 = packet.UnloadChunk.Z[j];
			if (cy2 < 1024)
			{
				horCoods.Add(new Vec2i(cx2, cz2));
			}
			long posIndex2 = MapUtil.Index3dL(cx2, cy2, cz2, chunkMapSizeX, chunkMapSizeZ);
			ClientChunk clientchunk2 = null;
			lock (game.WorldMap.chunksLock)
			{
				game.WorldMap.chunks.TryGetValue(posIndex2, out clientchunk2);
			}
			if (clientchunk2 != null)
			{
				UnloadChunk(clientchunk2);
				RuntimeStats.chunksUnloaded++;
			}
		}
		game.Logger.VerboseDebug("Entities and pool locations removed. Removing from chunk dict");
		lock (game.WorldMap.chunksLock)
		{
			for (int i = 0; i < count; i++)
			{
				long posIndex = MapUtil.Index3dL(packet.UnloadChunk.X[i], packet.UnloadChunk.Y[i], packet.UnloadChunk.Z[i], chunkMapSizeX, chunkMapSizeZ);
				ClientChunk clientchunk = null;
				game.WorldMap.chunks.TryGetValue(posIndex, out clientchunk);
				clientchunk?.Dispose();
				game.WorldMap.chunks.Remove(posIndex);
			}
		}
		foreach (Vec2i item in horCoods)
		{
			int cx = item.X;
			int cz = item.Y;
			bool anyfound = false;
			int cy = 0;
			while (!anyfound && cy < game.WorldMap.ChunkMapSizeY)
			{
				anyfound |= game.WorldMap.GetChunk(cx, cy, cz) != null;
				cy++;
			}
			if (!anyfound)
			{
				game.WorldMap.MapChunks.Remove(game.WorldMap.MapChunkIndex2D(cx, cz));
			}
		}
		ScreenManager.FrameProfiler.Mark("doneUnlCh");
	}

	private void UnloadChunk(ClientChunk clientchunk)
	{
		if (clientchunk == null)
		{
			return;
		}
		clientchunk.RemoveDataPoolLocations(game.chunkRenderer);
		for (int i = 0; i < clientchunk.EntitiesCount; i++)
		{
			Entity entity = clientchunk.Entities[i];
			if (entity != null && (game.EntityPlayer == null || entity.EntityId != game.EntityPlayer.EntityId))
			{
				EntityDespawnData reason = new EntityDespawnData
				{
					Reason = EnumDespawnReason.Unload
				};
				game.eventManager?.TriggerEntityDespawn(entity, reason);
				entity.OnEntityDespawn(reason);
				game.RemoveEntityRenderer(entity);
				game.LoadedEntities.Remove(entity.EntityId);
			}
		}
		foreach (KeyValuePair<BlockPos, BlockEntity> blockEntity in clientchunk.BlockEntities)
		{
			blockEntity.Value.OnBlockUnloaded();
		}
	}

	public override void Dispose(ClientMain game)
	{
		foreach (KeyValuePair<long, ClientChunk> chunk in game.WorldMap.chunks)
		{
			UnloadChunk(chunk.Value);
		}
		game.EntityPlayer?.Properties.Client?.Renderer?.Dispose();
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
