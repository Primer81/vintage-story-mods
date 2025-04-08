using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class BlockAccessorMapChunkLoading : BlockAccessorRelaxedBulkUpdate, IBulkBlockAccessor, IBlockAccessor
{
	private int chunkX;

	private int chunkZ;

	private IWorldChunk[] chunks;

	public BlockAccessorMapChunkLoading(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool debug)
		: base(worldmap, worldAccessor, synchronize, relight: false, debug)
	{
		base.debug = debug;
	}

	public new void SetChunks(Vec2i chunkCoord, IWorldChunk[] chunksCol)
	{
		chunks = chunksCol;
		chunkX = chunkCoord.X;
		chunkZ = chunkCoord.Y;
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
		dirtyChunkPositions.Clear();
		int prevChunkY = -99999;
		foreach (KeyValuePair<BlockPos, BlockUpdate> val in StagedBlocks)
		{
			int newblockid = val.Value.NewSolidBlockId;
			BlockPos pos = val.Key;
			int chunkY = pos.Y / 32;
			if (chunkY != prevChunkY)
			{
				chunk = chunks[chunkY];
				chunk.Unpack();
				chunk.MarkModified();
				int belowChunkY = (pos.Y - 1) / 32;
				if (belowChunkY != chunkY && belowChunkY >= 0)
				{
					chunks[belowChunkY].MarkModified();
				}
				if (newblockid > 0 || val.Value.NewFluidBlockId > 0)
				{
					chunk.Empty = false;
				}
				prevChunkY = chunkY;
			}
			int index3d = worldmap.ChunkSizedIndex3D(pos.X & 0x1F, pos.Y & 0x1F, pos.Z & 0x1F);
			Block newBlock = null;
			if (val.Value.NewSolidBlockId >= 0)
			{
				val.Value.OldBlockId = chunk.Data[index3d];
				chunk.Data[index3d] = newblockid;
				newBlock = worldmap.Blocks[newblockid];
			}
			if (val.Value.NewFluidBlockId >= 0)
			{
				if (val.Value.NewSolidBlockId < 0)
				{
					val.Value.OldBlockId = chunk.Data.GetFluid(index3d);
				}
				chunk.Data.SetFluid(index3d, val.Value.NewFluidBlockId);
				if (val.Value.NewFluidBlockId > 0 || newBlock == null)
				{
					newBlock = worldmap.Blocks[val.Value.NewFluidBlockId];
				}
			}
			UpdateRainHeightMap(worldmap.Blocks[val.Value.OldBlockId], newBlock, pos, chunk.MapChunk);
		}
		StagedBlocks.Clear();
	}

	protected override int GetNonStagedBlockId(int posX, int posY, int posZ, int layer)
	{
		if ((posX | posY | posZ) < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			return 0;
		}
		return ((posX / 32 != chunkX || posZ / 32 != chunkZ) ? worldmap.GetChunkAtPos(posX, posY, posZ) : chunks[posY / 32])?.UnpackAndReadBlock(worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F), layer) ?? 0;
	}
}
