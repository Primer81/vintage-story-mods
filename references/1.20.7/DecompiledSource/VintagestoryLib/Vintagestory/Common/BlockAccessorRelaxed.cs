using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class BlockAccessorRelaxed : BlockAccessorBase
{
	protected bool synchronize;

	protected bool relight;

	public BlockAccessorRelaxed(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool relight)
		: base(worldmap, worldAccessor)
	{
		this.synchronize = synchronize;
		this.relight = relight;
	}

	public override int GetBlockId(int posX, int posY, int posZ, int layer)
	{
		if ((posX | posY | posZ) < 0)
		{
			return 0;
		}
		IWorldChunk chunk = worldmap.GetChunkAtPos(posX, posY, posZ);
		if (chunk != null)
		{
			int index3d = worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F);
			return chunk.UnpackAndReadBlock(index3d, layer);
		}
		return 0;
	}

	public override Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4)
	{
		if ((posX | posY | posZ) < 0)
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

	public override void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack = null)
	{
		if ((pos.X | pos.Y | pos.Z) >= 0)
		{
			IWorldChunk chunk = worldmap.GetChunk(pos);
			if (chunk != null)
			{
				chunk.Unpack();
				SetBlockInternal(blockId, pos, chunk, synchronize, relight, 0, byItemstack);
			}
		}
	}

	public override void SetBlock(int blockId, BlockPos pos, int layer)
	{
		if ((pos.X | pos.Y | pos.Z) >= 0)
		{
			IWorldChunk chunk = worldmap.GetChunk(pos);
			if (chunk != null)
			{
				chunk.Unpack();
				SetBlockInternal(blockId, pos, chunk, synchronize, relight, layer);
			}
		}
	}

	public override void ExchangeBlock(int blockId, BlockPos pos)
	{
		if ((pos.X | pos.Y | pos.Z) < 0)
		{
			return;
		}
		IWorldChunk chunk = worldmap.GetChunk(pos);
		if (chunk != null)
		{
			chunk.Unpack();
			int index3d = worldmap.ChunkSizedIndex3D(pos.X & 0x1F, pos.Y & 0x1F, pos.Z & 0x1F);
			Block block = worldmap.Blocks[blockId];
			int oldblockid;
			if (block.ForFluidsLayer)
			{
				oldblockid = (chunk.Data as ChunkData).GetFluid(index3d);
				(chunk.Data as ChunkData).SetFluid(index3d, blockId);
			}
			else
			{
				oldblockid = (chunk.Data as ChunkData).GetSolidBlock(index3d);
				chunk.Data[index3d] = blockId;
			}
			worldmap.MarkChunkDirty(pos.X / 32, pos.InternalY / 32, pos.Z / 32, priority: true);
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
				worldmap.UpdateLighting(oldblockid, blockId, pos);
			}
		}
	}
}
