using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common.Database;

namespace Vintagestory.Common;

public class BlockAccessorBulkMinimalUpdate : BlockAccessorRelaxedBulkUpdate
{
	protected HashSet<Xyz> dirtyNeighbourChunkPositions;

	public BlockAccessorBulkMinimalUpdate(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool debug)
		: base(worldmap, worldAccessor, synchronize, relight: false, debug)
	{
		base.debug = debug;
		if (worldAccessor.Side == EnumAppSide.Client)
		{
			dirtyNeighbourChunkPositions = new HashSet<Xyz>();
		}
	}

	public override List<BlockUpdate> Commit()
	{
		FastCommit();
		return null;
	}

	public void FastCommit()
	{
		base.ReadFromStagedByDefault = false;
		IWorldChunk chunk = null;
		int prevChunkX = -1;
		int prevChunkY = -1;
		int prevChunkZ = -1;
		dirtyChunkPositions.Clear();
		dirtyNeighbourChunkPositions?.Clear();
		WorldMap worldmap = base.worldmap;
		IList<Block> blockList = worldmap.Blocks;
		foreach (KeyValuePair<BlockPos, BlockUpdate> val in StagedBlocks)
		{
			BlockUpdate blockUpdate = val.Value;
			int newblockid = blockUpdate.NewSolidBlockId;
			if (newblockid < 0 && blockUpdate.NewFluidBlockId < 0)
			{
				continue;
			}
			BlockPos pos = val.Key;
			int chunkX = pos.X / 32;
			int chunkY = pos.Y / 32;
			int chunkZ = pos.Z / 32;
			if (dirtyNeighbourChunkPositions != null)
			{
				if ((pos.X + 1) % 32 < 2)
				{
					dirtyNeighbourChunkPositions.Add(new Xyz((pos.X % 32 == 0) ? (chunkX - 1) : (chunkX + 1), chunkY, chunkZ));
				}
				if ((pos.Y + 1) % 32 < 2)
				{
					dirtyNeighbourChunkPositions.Add(new Xyz(chunkX, (pos.Y % 32 == 0) ? (chunkY - 1) : (chunkY + 1), chunkZ));
				}
				if ((pos.Z + 1) % 32 < 2)
				{
					dirtyNeighbourChunkPositions.Add(new Xyz(chunkX, chunkY, (pos.Z % 32 == 0) ? (chunkZ - 1) : (chunkZ + 1)));
				}
			}
			if (chunkX != prevChunkX || chunkY != prevChunkY || chunkZ != prevChunkZ)
			{
				chunk = worldmap.GetChunk(prevChunkX = chunkX, prevChunkY = chunkY, prevChunkZ = chunkZ);
				if (chunk == null)
				{
					continue;
				}
				chunk.Unpack();
				dirtyChunkPositions.Add(new ChunkPosCompact(chunkX, chunkY, chunkZ));
				int belowChunkY = (pos.Y - 1) / 32;
				if (belowChunkY != chunkY && belowChunkY >= 0)
				{
					dirtyChunkPositions.Add(new ChunkPosCompact(chunkX, belowChunkY, chunkZ));
				}
				if (newblockid > 0 || blockUpdate.NewFluidBlockId > 0)
				{
					chunk.Empty = false;
				}
			}
			if (chunk == null)
			{
				continue;
			}
			int index3d = worldmap.ChunkSizedIndex3D(pos.X & 0x1F, pos.Y & 0x1F, pos.Z & 0x1F);
			Block newBlock = null;
			if (blockUpdate.NewSolidBlockId >= 0)
			{
				int oldid = (blockUpdate.OldBlockId = chunk.Data[index3d]);
				if (!blockUpdate.ExchangeOnly)
				{
					blockList[oldid].OnBlockRemoved(worldAccessor, pos);
				}
				chunk.Data[index3d] = newblockid;
				newBlock = blockList[newblockid];
				if (!blockUpdate.ExchangeOnly)
				{
					newBlock.OnBlockPlaced(worldAccessor, pos);
				}
			}
			if (blockUpdate.NewFluidBlockId >= 0)
			{
				if (blockUpdate.NewSolidBlockId < 0)
				{
					blockUpdate.OldBlockId = chunk.Data.GetFluid(index3d);
				}
				chunk.Data.SetFluid(index3d, blockUpdate.NewFluidBlockId);
				if (blockUpdate.NewFluidBlockId > 0 || newBlock == null)
				{
					newBlock = blockList[blockUpdate.NewFluidBlockId];
				}
			}
			if (blockUpdate.ExchangeOnly && newBlock.EntityClass != null)
			{
				chunk.GetLocalBlockEntityAtBlockPos(pos)?.OnExchanged(newBlock);
			}
			UpdateRainHeightMap(blockList[blockUpdate.OldBlockId], newBlock, pos, chunk.MapChunk);
		}
		foreach (ChunkPosCompact cp2 in dirtyChunkPositions)
		{
			worldmap.MarkChunkDirty(cp2.X, cp2.Y, cp2.Z);
		}
		if (dirtyNeighbourChunkPositions != null)
		{
			foreach (Xyz cp in dirtyNeighbourChunkPositions)
			{
				worldmap.MarkChunkDirty(cp.X, cp.Y, cp.Z, priority: false, sunRelight: false, null, fireDirtyEvent: false, edgeOnly: true);
			}
		}
		if (synchronize)
		{
			worldmap.SendBlockUpdateBulkMinimal(StagedBlocks);
		}
		StagedBlocks.Clear();
		dirtyChunkPositions.Clear();
	}
}
