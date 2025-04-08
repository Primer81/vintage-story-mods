using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class BlockAccessorWorldGen : BlockAccessorBase, IWorldGenBlockAccessor, IBlockAccessor
{
	internal ChunkServerThread chunkdbthread;

	internal ServerMain server;

	[ThreadStatic]
	private static ServerChunk chunkCached;

	[ThreadStatic]
	private static long cachedChunkIndex3d;

	[ThreadStatic]
	private static ServerMapChunk mapchunkCached;

	[ThreadStatic]
	private static long cachedChunkIndex2d;

	private IServerWorldAccessor worldgenWorldAccessor;

	public IServerWorldAccessor WorldgenWorldAccessor => worldgenWorldAccessor ?? (worldgenWorldAccessor = new WorldgenWorldAccessor((IServerWorldAccessor)worldAccessor, this));

	public BlockAccessorWorldGen(ServerMain server, ChunkServerThread chunkdbthread)
		: base(server.WorldMap, null)
	{
		this.chunkdbthread = chunkdbthread;
		this.server = server;
		worldAccessor = server;
	}

	public void ScheduleBlockLightUpdate(BlockPos pos, int oldBlockid, int newBlockId)
	{
		ServerMapChunk mc = (ServerMapChunk)GetMapChunk(pos.X / 32, pos.Z / 32);
		if (mc == null)
		{
			ServerMain.Logger.Worldgen("Mapchunk was null when scheduling a blocklight update at " + pos);
			return;
		}
		if (mc.ScheduledBlockLightUpdates == null)
		{
			mc.ScheduledBlockLightUpdates = new List<Vec4i>();
		}
		mc.ScheduledBlockLightUpdates.Add(new Vec4i(pos, newBlockId));
	}

	public void RunScheduledBlockLightUpdates(int chunkx, int chunkz)
	{
		ServerMapChunk mc = (ServerMapChunk)GetMapChunk(chunkx, chunkz);
		if (mc == null)
		{
			ServerMain.Logger.Worldgen("Mapchunk was null when attempting scheduled blocklight updates at " + chunkx + "," + chunkz);
			return;
		}
		List<Vec4i> scheduledBlockLightUpdates = mc.ScheduledBlockLightUpdates;
		if (scheduledBlockLightUpdates == null || scheduledBlockLightUpdates.Count == 0)
		{
			return;
		}
		BlockPos pos = new BlockPos();
		foreach (Vec4i posAndBlockId in scheduledBlockLightUpdates)
		{
			Block block = server.Blocks[posAndBlockId.W];
			pos.SetAndCorrectDimension(posAndBlockId.X, posAndBlockId.Y, posAndBlockId.Z);
			byte[] lightHsv = block.GetLightHsv(this, pos);
			if (lightHsv[2] > 0)
			{
				server.WorldMap.chunkIlluminatorWorldGen.PlaceBlockLight(lightHsv, pos.X, pos.InternalY, pos.Z);
			}
		}
		mc.ScheduledBlockLightUpdates = null;
	}

	public void ScheduleBlockUpdate(BlockPos pos)
	{
		ChunkColumnLoadRequest req = chunkdbthread.GetChunkRequestAtPos(pos.X, pos.Z);
		if (req?.MapChunk != null)
		{
			req.MapChunk.ScheduledBlockUpdates.Add(pos.Copy());
		}
	}

	public override IMapChunk GetMapChunk(Vec2i chunkPos)
	{
		return GetMapChunk(chunkPos.X, chunkPos.Y);
	}

	public override IMapChunk GetMapChunk(int chunkX, int chunkZ)
	{
		long index2d = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
		if (cachedChunkIndex2d == index2d)
		{
			return mapchunkCached;
		}
		ServerMapChunk mapchunk = chunkdbthread.GetMapChunk(index2d);
		if (mapchunk != null)
		{
			cachedChunkIndex2d = index2d;
			mapchunkCached = mapchunk;
		}
		return mapchunk;
	}

	public override IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		return chunkdbthread.GetGeneratingChunk(chunkX, chunkY, chunkZ);
	}

	[Obsolete("Please use BlockPos version instead for dimension awareness")]
	public override IWorldChunk GetChunkAtBlockPos(int posX, int posY, int posZ)
	{
		return chunkdbthread.GetGeneratingChunk(posX / 32, posY / 32, posZ / 32);
	}

	public override IWorldChunk GetChunkAtBlockPos(BlockPos pos)
	{
		return chunkdbthread.GetGeneratingChunk(pos.X / 32, pos.Y / 32, pos.Z / 32);
	}

	public override IMapRegion GetMapRegion(int regionX, int regionZ)
	{
		return chunkdbthread.GetMapRegion(regionX, regionZ);
	}

	public override int GetBlockId(int posX, int posY, int posZ, int layer)
	{
		long nowChunkIndex3d = worldmap.ChunkIndex3D(posX / 32, posY / 32, posZ / 32);
		ServerChunk chunk;
		if (cachedChunkIndex3d == nowChunkIndex3d)
		{
			chunk = chunkCached;
		}
		else
		{
			chunk = chunkdbthread.GetGeneratingChunkAtPos(posX, posY, posZ);
			if (chunk == null)
			{
				chunk = worldmap.GetChunkAtPos(posX, posY, posZ) as ServerChunk;
			}
			if (chunk != null)
			{
				chunk.Unpack();
				cachedChunkIndex3d = nowChunkIndex3d;
				chunkCached = chunk;
			}
		}
		if (chunk != null)
		{
			return chunk.Data.GetBlockId(worldmap.ChunkSizedIndex3D(posX & MagicNum.ServerChunkSizeMask, posY & MagicNum.ServerChunkSizeMask, posZ & MagicNum.ServerChunkSizeMask), layer);
		}
		if (RuntimeEnv.DebugOutOfRangeBlockAccess)
		{
			ServerMain.Logger.Notification("Tried to get block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}). ", posX, posY, posZ, posX / MagicNum.ServerChunkSize, posY / MagicNum.ServerChunkSize, posZ / MagicNum.ServerChunkSize);
			ServerMain.Logger.Notification(new StackTrace()?.ToString() ?? "");
		}
		else
		{
			ServerMain.Logger.Notification("Tried to get block outside generating chunks! Set RuntimeEnv.DebugOutOfRangeBlockAccess to debug.");
		}
		return 0;
	}

	public override Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4)
	{
		if (posX < 0 || posY < 0 || posZ < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			return null;
		}
		ServerChunk chunk = chunkdbthread.GetGeneratingChunkAtPos(posX, posY, posZ);
		if (chunk != null)
		{
			chunk.Unpack();
			return worldmap.Blocks[chunk.Data[worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F)]];
		}
		return null;
	}

	public override void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack = null)
	{
		Block newBlock = worldmap.Blocks[blockId];
		if (newBlock.ForFluidsLayer)
		{
			SetFluidBlock(blockId, pos);
			return;
		}
		ServerChunk chunk2 = chunkdbthread.GetGeneratingChunkAtPos(pos);
		if (chunk2 != null)
		{
			SetSolidBlock(chunk2, pos, newBlock, blockId);
		}
		else if (worldmap.GetChunkAtPos(pos.X, pos.Y, pos.Z) is ServerChunk chunk)
		{
			int prevBlockID = SetSolidBlock(chunk, pos, newBlock, blockId);
			if (newBlock.LightHsv != null && newBlock.LightHsv[2] > 0)
			{
				ScheduleBlockLightUpdate(pos, prevBlockID, blockId);
			}
		}
		else if (RuntimeEnv.DebugOutOfRangeBlockAccess)
		{
			ServerMain.Logger.Notification("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}) when placing {6}", pos.X, pos.Y, pos.Z, pos.X / MagicNum.ServerChunkSize, pos.Y / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize, worldAccessor.GetBlock(blockId));
			ServerMain.Logger.VerboseDebug("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}) when placing {6}", pos.X, pos.Y, pos.Z, pos.X / MagicNum.ServerChunkSize, pos.Y / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize, worldAccessor.GetBlock(blockId));
			ServerMain.Logger.VerboseDebug(new StackTrace()?.ToString() ?? "");
		}
		else
		{
			ServerMain.Logger.Notification("Tried to set block outside generating chunks! Set RuntimeEnv.DebugOutOfRangeBlockAccess to debug.");
		}
	}

	protected int SetSolidBlock(ServerChunk chunk, BlockPos pos, Block newBlock, int blockId)
	{
		chunk.Unpack();
		int index3d = worldmap.ChunkSizedIndex3D(pos.X & MagicNum.ServerChunkSizeMask, pos.Y & MagicNum.ServerChunkSizeMask, pos.Z & MagicNum.ServerChunkSizeMask);
		int prevBlockID = chunk.Data.GetBlockId(index3d, 1);
		if (prevBlockID != 0 && worldmap.Blocks[prevBlockID].EntityClass != null)
		{
			chunk.RemoveBlockEntity(pos);
			((ServerMapChunk)chunk.MapChunk).NewBlockEntities.Remove(pos);
		}
		chunk.Data[index3d] = blockId;
		if (newBlock.DisplacesLiquids(this, pos))
		{
			chunk.Data.SetFluid(index3d, 0);
		}
		chunk.DirtyForSaving = true;
		return prevBlockID;
	}

	public override void SetBlock(int blockId, BlockPos pos, int layer)
	{
		switch (layer)
		{
		case 2:
			SetFluidBlock(blockId, pos);
			break;
		case 1:
			SetBlock(blockId, pos);
			break;
		default:
			throw new ArgumentException("Layer must be solid or fluid");
		}
	}

	public void SetFluidBlock(int blockId, BlockPos pos)
	{
		ServerChunk chunk2 = chunkdbthread.GetGeneratingChunkAtPos(pos);
		if (chunk2 != null)
		{
			chunk2.Unpack();
			int index3d = worldmap.ChunkSizedIndex3D(pos.X & MagicNum.ServerChunkSizeMask, pos.Y & MagicNum.ServerChunkSizeMask, pos.Z & MagicNum.ServerChunkSizeMask);
			chunk2.Data.SetFluid(index3d, blockId);
		}
		else if (worldmap.GetChunkAtPos(pos.X, pos.Y, pos.Z) is ServerChunk chunk)
		{
			chunk.Unpack();
			int index3d2 = worldmap.ChunkSizedIndex3D(pos.X & MagicNum.ServerChunkSizeMask, pos.Y & MagicNum.ServerChunkSizeMask, pos.Z & MagicNum.ServerChunkSizeMask);
			chunk.Data.SetFluid(index3d2, blockId);
			chunk.DirtyForSaving = true;
		}
		else if (RuntimeEnv.DebugOutOfRangeBlockAccess)
		{
			ServerMain.Logger.Notification("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}) when placing {6}", pos.X, pos.Y, pos.Z, pos.X / MagicNum.ServerChunkSize, pos.Y / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize, worldAccessor.GetBlock(blockId));
			ServerMain.Logger.VerboseDebug("Tried to set block outside generating chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5}) when placing {6}", pos.X, pos.Y, pos.Z, pos.X / MagicNum.ServerChunkSize, pos.Y / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize, worldAccessor.GetBlock(blockId));
			ServerMain.Logger.VerboseDebug(new StackTrace()?.ToString() ?? "");
		}
		else
		{
			ServerMain.Logger.Notification("Tried to set block outside generating chunks! Set RuntimeEnv.DebugOutOfRangeBlockAccess to debug.");
		}
	}

	public override List<BlockUpdate> Commit()
	{
		return null;
	}

	public override void ExchangeBlock(int blockId, BlockPos pos)
	{
		SetBlock(blockId, pos);
	}

	public override void MarkChunkDecorsModified(BlockPos pos)
	{
		if (chunkdbthread.GetGeneratingChunkAtPos(pos) == null)
		{
			base.MarkChunkDecorsModified(pos);
		}
	}

	public override void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null)
	{
		ServerChunk chunk = chunkdbthread.GetGeneratingChunkAtPos(position);
		if (chunk != null)
		{
			BlockEntity entity = ServerMain.ClassRegistry.CreateBlockEntity(classname);
			Block block = chunk.GetLocalBlockAtBlockPos(server, position);
			entity.CreateBehaviors(block, server);
			entity.Pos = position.Copy();
			chunk.AddBlockEntity(entity);
			((ServerMapChunk)chunk.MapChunk).NewBlockEntities.Add(position.Copy());
		}
	}

	public void AddEntity(Entity entity)
	{
		ServerChunk chunk = chunkdbthread.GetGeneratingChunkAtPos(entity.ServerPos.AsBlockPos);
		if (chunk != null)
		{
			entity.EntityId = ++server.SaveGameData.LastEntityId;
			chunk.AddEntity(entity);
		}
	}

	public override BlockEntity GetBlockEntity(BlockPos position)
	{
		return chunkdbthread.GetGeneratingChunkAtPos(position)?.GetLocalBlockEntityAtBlockPos(position);
	}

	public override void RemoveBlockEntity(BlockPos position)
	{
		chunkdbthread.GetGeneratingChunkAtPos(position)?.RemoveBlockEntity(position);
	}

	public void BeginColumn()
	{
		cachedChunkIndex3d = -1L;
		cachedChunkIndex2d = -1L;
	}

	public static void ThreadDispose()
	{
		chunkCached = null;
		mapchunkCached = null;
	}

	protected override ChunkData[] LoadChunksToCache(int mincx, int mincy, int mincz, int maxcx, int maxcy, int maxcz, Action<int, int, int> onChunkMissing)
	{
		int cxCount = maxcx - mincx + 1;
		int cyCount = maxcy - mincy + 1;
		int czCount = maxcz - mincz + 1;
		ChunkData[] chunks = new ChunkData[cxCount * cyCount * czCount];
		for (int cy = mincy; cy <= maxcy; cy++)
		{
			int ciy = (cy - mincy) * czCount - mincz;
			for (int cz = mincz; cz <= maxcz; cz++)
			{
				int chunkIndexBase = (ciy + cz) * cxCount - mincx;
				for (int cx = mincx; cx <= maxcx; cx++)
				{
					IWorldChunk chunk = chunkdbthread.GetGeneratingChunk(cx, cy, cz);
					if (chunk == null)
					{
						chunk = worldmap.GetChunk(cx, cy, cz);
					}
					if (chunk == null)
					{
						chunks[chunkIndexBase + cx] = null;
						onChunkMissing?.Invoke(cx, cy, cz);
					}
					else
					{
						chunk.Unpack();
						chunks[chunkIndexBase + cx] = chunk.Data as ChunkData;
					}
				}
			}
		}
		return chunks;
	}
}
