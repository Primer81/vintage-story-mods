using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ServerSystemRelight : ServerSystem
{
	public ChunkIlluminator chunkIlluminator;

	public ServerSystemRelight(ServerMain server)
		: base(server)
	{
	}

	public override void OnBeginGameReady(SaveGame savegame)
	{
		chunkIlluminator = new ChunkIlluminator(server.WorldMap, new BlockAccessorRelaxed(server.WorldMap, server, synchronize: false, relight: false), MagicNum.ServerChunkSize);
		chunkIlluminator.InitForWorld(server.Blocks, (ushort)server.sunBrightness, server.WorldMap.MapSizeX, server.WorldMap.MapSizeY, server.WorldMap.MapSizeZ);
	}

	public override void OnSeparateThreadTick()
	{
		ProcessLightingQueue();
	}

	public override int GetUpdateInterval()
	{
		return 10;
	}

	public void ProcessLightingQueue()
	{
		while (server.WorldMap.LightingTasks.Count > 0)
		{
			UpdateLightingTask task = null;
			lock (server.WorldMap.LightingTasksLock)
			{
				task = server.WorldMap.LightingTasks.Dequeue();
			}
			if (task == null)
			{
				break;
			}
			if (server.WorldMap.IsValidPos(task.pos))
			{
				ProcessLightingTask(task, task.pos);
				if (server.Suspended)
				{
					break;
				}
			}
		}
	}

	public void ProcessLightingTask(UpdateLightingTask task, BlockPos pos)
	{
		int oldLightAbsorb = 0;
		int newLightAbsorb = 0;
		bool changedLightSource = false;
		int posX = task.pos.X;
		int posY = task.pos.InternalY;
		int posZ = task.pos.Z;
		HashSet<long> chunksDirty = new HashSet<long>();
		if (task.absorbUpdate)
		{
			oldLightAbsorb = task.oldAbsorb;
			newLightAbsorb = task.newAbsorb;
		}
		else if (task.removeLightHsv != null)
		{
			changedLightSource = true;
			chunksDirty.AddRange(chunkIlluminator.RemoveBlockLight(task.removeLightHsv, posX, posY, posZ));
		}
		else
		{
			int oldblockid = task.oldBlockId;
			int newblockid = task.newBlockId;
			Block block = server.Blocks[oldblockid];
			Block newBlock = server.Blocks[newblockid];
			byte[] oldLightHsv = block.GetLightHsv(server.BlockAccessor, pos);
			byte[] newLightHsv = newBlock.GetLightHsv(server.BlockAccessor, pos);
			if (oldLightHsv[2] > 0)
			{
				changedLightSource = true;
				chunksDirty.AddRange(chunkIlluminator.RemoveBlockLight(oldLightHsv, pos.X, pos.InternalY, pos.Z));
			}
			if (newLightHsv[2] > 0)
			{
				changedLightSource = true;
				chunksDirty.AddRange(chunkIlluminator.PlaceBlockLight(newLightHsv, pos.X, pos.InternalY, pos.Z));
			}
			oldLightAbsorb = block.GetLightAbsorption(server.BlockAccessor, pos);
			newLightAbsorb = newBlock.GetLightAbsorption(server.BlockAccessor, pos);
			if (oldLightHsv[2] == 0 && newLightHsv[2] == 0 && oldLightAbsorb != newLightAbsorb)
			{
				chunksDirty.AddRange(chunkIlluminator.UpdateBlockLight(oldLightAbsorb, newLightAbsorb, pos.X, pos.InternalY, pos.Z));
			}
			server.WorldMap.MarkChunksDirty(pos, GameMath.Max(1, newLightHsv[2], oldLightHsv[2]));
		}
		bool requireRelight = newLightAbsorb != oldLightAbsorb;
		if (requireRelight || changedLightSource)
		{
			for (int i = 0; i < 6; i++)
			{
				Vec3i vec = BlockFacing.ALLNORMALI[i];
				long neibindex3d = server.WorldMap.ChunkIndex3D((pos.X + vec.X) / 32, (pos.InternalY + vec.Y) / 32, (pos.Z + vec.Z) / 32);
				chunksDirty.Add(neibindex3d);
			}
		}
		if (requireRelight)
		{
			chunksDirty.AddRange(chunkIlluminator.UpdateSunLight(pos.X, pos.InternalY, pos.Z, oldLightAbsorb, newLightAbsorb));
		}
		foreach (long index3d in chunksDirty)
		{
			server.WorldMap.GetServerChunk(index3d)?.MarkModified();
		}
	}
}
