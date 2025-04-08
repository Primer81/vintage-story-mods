using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

internal class BlockAccessorStrict : BlockAccessorBase
{
	private bool synchronize;

	private bool relight;

	private bool debug;

	public BlockAccessorStrict(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool relight, bool debug)
		: base(worldmap, worldAccessor)
	{
		this.synchronize = synchronize;
		this.relight = relight;
		this.debug = debug;
	}

	public override int GetBlockId(int posX, int posY, int posZ, int layer)
	{
		if ((posX | posY | posZ) < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			worldmap.Logger.VerboseDebug("Tried to get block outside map! (at pos {0}, {1}, {2})", posX, posY, posZ);
			return 0;
		}
		IWorldChunk chunk = worldmap.GetChunkAtPos(posX, posY, posZ);
		if (chunk != null)
		{
			return chunk.UnpackAndReadBlock(worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F), layer);
		}
		worldmap.Logger.VerboseDebug("Tried to get block outside loaded chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5})", posX, posY, posZ, posX / 32, posY / 32, posZ / 32);
		if (debug)
		{
			worldmap.PrintChunkMap(new Vec2i(posX / 32, posZ / 32));
			throw new AccessViolationException("Tried to get block outside loaded chunks. Current chunk map exported for debug");
		}
		return 0;
	}

	public override Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4)
	{
		if ((posX | posY | posZ) < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			return null;
		}
		IWorldChunk chunk = worldmap.GetChunkAtPos(posX, posY, posZ);
		if (chunk != null)
		{
			return worldmap.Blocks[chunk.UnpackAndReadBlock(worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F), layer)];
		}
		return null;
	}

	public override void SetBlock(int newblockId, BlockPos pos, ItemStack byItemstack = null)
	{
		if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension != 1 && (pos.X >= worldmap.MapSizeX || pos.Y >= worldmap.MapSizeY || pos.Z >= worldmap.MapSizeZ)))
		{
			worldmap.Logger.Notification("Tried to set block outside map! (at pos {0}, {1}, {2})", pos.X, pos.Y, pos.Z);
			return;
		}
		IWorldChunk chunk = worldmap.GetChunk(pos);
		if (chunk != null)
		{
			chunk.Unpack();
			SetBlockInternal(newblockId, pos, chunk, synchronize, relight, 0, byItemstack);
			return;
		}
		worldmap.Logger.VerboseDebug("Tried to set block outside loaded chunks! (at pos {0}, {1}, {2} = chunk {3}, {4}, {5})", pos.X, pos.Y, pos.Z, pos.X / 32, pos.Y / 32, pos.Z / 32);
		if (!debug)
		{
			return;
		}
		worldmap.PrintChunkMap(new Vec2i(pos.X / 32, pos.Z / 32));
		throw new AccessViolationException("Tried to set block outside loaded chunks. Current chunk map exported for debug");
	}

	public override void SetBlock(int fluidBlockid, BlockPos pos, int layer)
	{
		if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension != 1 && (pos.X >= worldmap.MapSizeX || pos.Y >= worldmap.MapSizeY || pos.Z >= worldmap.MapSizeZ)))
		{
			worldmap.Logger.Notification("Tried to set liquid block outside map! (at pos {0}, {1}, {2})", pos.X, pos.Y, pos.Z);
			return;
		}
		IWorldChunk chunk = worldmap.GetChunk(pos);
		if (chunk != null)
		{
			chunk.Unpack();
			SetBlockInternal(fluidBlockid, pos, chunk, synchronize, relight, layer);
		}
	}

	public override void ExchangeBlock(int blockId, BlockPos pos)
	{
		if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension != 1 && (pos.X >= worldmap.MapSizeX || pos.Y >= worldmap.MapSizeY || pos.Z >= worldmap.MapSizeZ)))
		{
			return;
		}
		IWorldChunk chunk = worldmap.GetChunkAtPos(pos.X, pos.InternalY, pos.Z);
		if (chunk != null)
		{
			chunk.Unpack();
			int index3d = worldmap.ChunkSizedIndex3D(pos.X & 0x1F, pos.InternalY & 0x1F, pos.Z & 0x1F);
			int oldblockid = chunk.Data[index3d];
			chunk.Data[index3d] = blockId;
			chunk.MarkModified();
			Block block = worldmap.Blocks[blockId];
			if (!block.ForFluidsLayer)
			{
				chunk.GetLocalBlockEntityAtBlockPos(pos)?.OnExchanged(block);
			}
			if (synchronize)
			{
				worldmap.SendExchangeBlock(blockId, pos.X, pos.InternalY, pos.Z);
			}
			if (relight)
			{
				worldmap.UpdateLighting(oldblockid, blockId, new BlockPos(pos.X, pos.InternalY, pos.Z));
			}
		}
	}
}
