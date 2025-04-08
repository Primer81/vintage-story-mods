using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common.Database;

namespace Vintagestory.Common;

public class BlockAccessorRelaxedBulkUpdate : BlockAccessorBase, IBulkBlockAccessor, IBlockAccessor
{
	protected bool synchronize;

	protected bool relight;

	protected bool debug;

	protected bool storeOldBlockEntityData;

	public readonly Dictionary<BlockPos, BlockUpdate> StagedBlocks = new Dictionary<BlockPos, BlockUpdate>();

	public readonly Dictionary<BlockPos, BlockUpdate> LightSources = new Dictionary<BlockPos, BlockUpdate>();

	private readonly Queue<BlockBreakTask> _blockBreakTasks = new Queue<BlockBreakTask>();

	protected readonly HashSet<ChunkPosCompact> dirtyChunkPositions = new HashSet<ChunkPosCompact>();

	public bool ReadFromStagedByDefault { get; set; }

	Dictionary<BlockPos, BlockUpdate> IBulkBlockAccessor.StagedBlocks => StagedBlocks;

	public event Action<IBulkBlockAccessor> BeforeCommit;

	public BlockAccessorRelaxedBulkUpdate(WorldMap worldmap, IWorldAccessor worldAccessor, bool synchronize, bool relight, bool debug)
		: base(worldmap, worldAccessor)
	{
		this.synchronize = synchronize;
		this.relight = relight;
		this.debug = debug;
	}

	public override int GetBlockId(int posX, int posY, int posZ, int layer)
	{
		if (ReadFromStagedByDefault && StagedBlocks.TryGetValue(new BlockPos(posX, posY, posZ), out var bd))
		{
			switch (layer)
			{
			default:
				if (bd.NewSolidBlockId >= 0)
				{
					return bd.NewSolidBlockId;
				}
				break;
			case 2:
			case 3:
				if (bd.NewFluidBlockId >= 0)
				{
					return bd.NewFluidBlockId;
				}
				break;
			case 4:
				return GetMostSolidBlock(posX, posY, posZ).Id;
			}
		}
		return GetNonStagedBlockId(posX, posY, posZ, layer);
	}

	public override int GetBlockId(BlockPos pos, int layer)
	{
		if (ReadFromStagedByDefault && StagedBlocks.TryGetValue(pos, out var bd))
		{
			switch (layer)
			{
			default:
				if (bd.NewSolidBlockId >= 0)
				{
					return bd.NewSolidBlockId;
				}
				break;
			case 2:
			case 3:
				if (bd.NewFluidBlockId >= 0)
				{
					return bd.NewFluidBlockId;
				}
				break;
			case 4:
				return GetMostSolidBlock(pos).Id;
			}
		}
		return GetNonStagedBlockId(pos.X, pos.InternalY, pos.Z, layer);
	}

	public override Block GetMostSolidBlock(int x, int y, int z)
	{
		if (ReadFromStagedByDefault && StagedBlocks.TryGetValue(new BlockPos(x, y, z), out var bd))
		{
			if (bd.NewSolidBlockId >= 0)
			{
				return worldmap.Blocks[bd.NewSolidBlockId];
			}
			if (bd.NewFluidBlockId > 0)
			{
				Block block = worldmap.Blocks[bd.NewFluidBlockId];
				if (block.SideSolid.Any)
				{
					return block;
				}
			}
		}
		return base.GetMostSolidBlock(x, y, z);
	}

	protected virtual int GetNonStagedBlockId(int posX, int posY, int posZ, int layer)
	{
		if ((posX | posY | posZ) < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			return 0;
		}
		return worldmap.GetChunkAtPos(posX, posY, posZ)?.UnpackAndReadBlock(worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F), layer) ?? 0;
	}

	public override Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4)
	{
		if ((posX | posY | posZ) < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			return null;
		}
		if (ReadFromStagedByDefault && StagedBlocks.TryGetValue(new BlockPos(posX, posY, posZ), out var bd) && bd.NewSolidBlockId >= 0)
		{
			return worldmap.Blocks[bd.NewSolidBlockId];
		}
		IWorldChunk chunk = worldmap.GetChunkAtPos(posX, posY, posZ);
		if (chunk != null)
		{
			return worldmap.Blocks[chunk.UnpackAndReadBlock(worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F), layer)];
		}
		return null;
	}

	public override void SetBlock(int newBlockId, BlockPos pos, ItemStack byItemstack = null)
	{
		if (worldmap.Blocks[newBlockId].ForFluidsLayer)
		{
			SetFluidBlock(newBlockId, pos);
		}
		else if ((pos.X | pos.Y | pos.Z) >= 0 && (pos.dimension != 0 || (pos.X < worldmap.MapSizeX && pos.Y < worldmap.MapSizeY && pos.Z < worldmap.MapSizeZ)))
		{
			if (StagedBlocks.TryGetValue(pos, out var bu))
			{
				bu.NewSolidBlockId = newBlockId;
				bu.ByStack = byItemstack;
				return;
			}
			BlockPos copied = pos.Copy();
			StagedBlocks[copied] = new BlockUpdate
			{
				NewSolidBlockId = newBlockId,
				ByStack = byItemstack,
				Pos = copied
			};
		}
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
		if ((pos.X | pos.Y | pos.Z) >= 0 && (pos.dimension != 0 || (pos.X < worldmap.MapSizeX && pos.Y < worldmap.MapSizeY && pos.Z < worldmap.MapSizeZ)))
		{
			if (StagedBlocks.TryGetValue(pos, out var bu))
			{
				bu.NewFluidBlockId = blockId;
				return;
			}
			BlockPos copied = pos.Copy();
			StagedBlocks[copied] = new BlockUpdate
			{
				NewFluidBlockId = blockId,
				Pos = copied
			};
		}
	}

	public override bool SetDecor(Block block, BlockPos pos, BlockFacing onFace)
	{
		return SetDecor(block, pos, new DecorBits(onFace));
	}

	public override bool SetDecor(Block block, BlockPos pos, int decorIndex)
	{
		if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension == 0 && (pos.X >= worldmap.MapSizeX || pos.Y >= worldmap.MapSizeY || pos.Z >= worldmap.MapSizeZ)))
		{
			return false;
		}
		DecorUpdate decorUpdate2 = default(DecorUpdate);
		decorUpdate2.faceAndSubposition = decorIndex;
		decorUpdate2.decorId = block.Id;
		DecorUpdate decorUpdate = decorUpdate2;
		if (StagedBlocks.TryGetValue(pos, out var blockUpdate))
		{
			BlockUpdate blockUpdate2 = blockUpdate;
			if (blockUpdate2.Decors == null)
			{
				blockUpdate2.Decors = new List<DecorUpdate>();
			}
			blockUpdate.Decors.Add(new DecorUpdate
			{
				faceAndSubposition = decorIndex,
				decorId = block.Id
			});
		}
		else
		{
			BlockPos copied = pos.Copy();
			List<DecorUpdate> list = new List<DecorUpdate>();
			list.Add(decorUpdate);
			StagedBlocks[copied] = new BlockUpdate
			{
				Pos = copied,
				Decors = list
			};
		}
		return true;
	}

	public override List<BlockUpdate> Commit()
	{
		this.BeforeCommit?.Invoke(this);
		ReadFromStagedByDefault = false;
		IWorldChunk chunk = null;
		int prevChunkX = -1;
		int prevChunkY = -1;
		int prevChunkZ = -1;
		List<BlockUpdate> updatedBlocks = new List<BlockUpdate>(StagedBlocks.Count);
		HashSet<BlockPos> updatedBlockPositions = new HashSet<BlockPos>();
		List<BlockPos> updatedDecorPositions = new List<BlockPos>();
		dirtyChunkPositions.Clear();
		WorldMap worldmap = base.worldmap;
		IList<Block> blockList = worldmap.Blocks;
		if (_blockBreakTasks.Count == 0 && StagedBlocks.Count == 0 && LightSources.Count == 0)
		{
			return updatedBlocks;
		}
		foreach (BlockBreakTask val in _blockBreakTasks)
		{
			BlockPos pos5 = val.Pos;
			int chunkX4 = pos5.X / 32;
			int chunkY4 = pos5.InternalY / 32;
			int chunkZ4 = pos5.Z / 32;
			bool newChunk = false;
			if (chunkX4 != prevChunkX || chunkY4 != prevChunkY || chunkZ4 != prevChunkZ)
			{
				chunk = worldmap.GetChunk(prevChunkX = chunkX4, prevChunkY = chunkY4, prevChunkZ = chunkZ4);
				if (chunk == null)
				{
					continue;
				}
				chunk.Unpack();
				newChunk = true;
			}
			if (chunk != null)
			{
				int index3d3 = worldmap.ChunkSizedIndex3D(pos5.X & 0x1F, pos5.Y & 0x1F, pos5.Z & 0x1F);
				blockList[chunk.Data[index3d3]].OnBlockBroken(worldAccessor, val.Pos, val.byPlayer, val.DropQuantityMultiplier);
				if (newChunk)
				{
					dirtyChunkPositions.Add(new ChunkPosCompact(chunkX4, chunkY4, chunkZ4));
				}
			}
		}
		BlockPos key;
		BlockUpdate value;
		foreach (KeyValuePair<BlockPos, BlockUpdate> stagedBlock in StagedBlocks)
		{
			stagedBlock.Deconstruct(out key, out value);
			BlockPos pos4 = key;
			BlockUpdate blockUpdate3 = value;
			int chunkX3 = pos4.X / 32;
			int chunkY3 = pos4.InternalY / 32;
			int chunkZ3 = pos4.Z / 32;
			if (chunkX3 != prevChunkX || chunkY3 != prevChunkY || chunkZ3 != prevChunkZ)
			{
				chunk = worldmap.GetChunk(prevChunkX = chunkX3, prevChunkY = chunkY3, prevChunkZ = chunkZ3);
				if (chunk == null)
				{
					continue;
				}
				chunk.Unpack();
				dirtyChunkPositions.Add(new ChunkPosCompact(chunkX3, chunkY3, chunkZ3));
			}
			if (chunk == null)
			{
				continue;
			}
			int index3d2 = worldmap.ChunkSizedIndex3D(pos4.X & 0x1F, pos4.Y & 0x1F, pos4.Z & 0x1F);
			int newBLockId = ((blockUpdate3.NewSolidBlockId >= 0) ? blockUpdate3.NewSolidBlockId : blockUpdate3.NewFluidBlockId);
			if (newBLockId < 0)
			{
				newBLockId = 0;
			}
			Block newBlock = blockList[newBLockId];
			blockUpdate3.OldBlockId = chunk.Data[index3d2];
			Dictionary<int, Block> decors = chunk.GetSubDecors(this, pos4);
			if (decors != null && decors.Count > 0)
			{
				value = blockUpdate3;
				if (value.OldDecors == null)
				{
					value.OldDecors = new List<DecorUpdate>();
				}
				foreach (var (i, block) in decors)
				{
					blockUpdate3.OldDecors.Add(new DecorUpdate
					{
						faceAndSubposition = i,
						decorId = block.BlockId
					});
				}
			}
			if (storeOldBlockEntityData && worldAccessor.Blocks[blockUpdate3.OldBlockId].EntityClass != null)
			{
				TreeAttribute tree = new TreeAttribute();
				GetBlockEntity(blockUpdate3.Pos)?.ToTreeAttributes(tree);
				blockUpdate3.OldBlockEntityData = tree.ToBytes();
			}
			blockUpdate3.OldFluidBlockId = chunk.Data.GetFluid(index3d2);
			if (blockUpdate3.NewSolidBlockId >= 0)
			{
				chunk.Data[index3d2] = blockUpdate3.NewSolidBlockId;
			}
			if (blockUpdate3.NewFluidBlockId >= 0)
			{
				chunk.Data.SetFluid(index3d2, blockUpdate3.NewFluidBlockId);
				if (blockUpdate3.NewSolidBlockId == 0)
				{
					newBlock = blockList[blockUpdate3.NewFluidBlockId];
				}
			}
			chunk.BreakAllDecorFast(worldAccessor, pos4, index3d2, callOnBrokenAsDecor: false);
			updatedBlocks.Add(blockUpdate3);
			updatedBlockPositions.Add(blockUpdate3.Pos);
			if (blockUpdate3.NewSolidBlockId > 0 || blockUpdate3.NewFluidBlockId > 0)
			{
				chunk.Empty = false;
			}
			if (relight && newBlock.GetLightHsv(this, pos4)[2] > 0)
			{
				LightSources[pos4] = blockUpdate3;
			}
			if (pos4.dimension == 0)
			{
				UpdateRainHeightMap(blockList[blockUpdate3.OldBlockId], newBlock, pos4, chunk.MapChunk);
			}
		}
		foreach (KeyValuePair<BlockPos, BlockUpdate> stagedBlock2 in StagedBlocks)
		{
			stagedBlock2.Deconstruct(out key, out value);
			BlockPos pos3 = key;
			BlockUpdate blockUpdate2 = value;
			int solidBlockId = blockUpdate2.NewSolidBlockId;
			if (solidBlockId < 0 || (blockUpdate2.ExchangeOnly && blockList[solidBlockId].EntityClass == null) || (blockUpdate2.OldBlockId == solidBlockId && (blockUpdate2.ByStack == null || blockList[blockUpdate2.OldBlockId].EntityClass == null)))
			{
				continue;
			}
			int chunkX2 = pos3.X / 32;
			int chunkY2 = pos3.InternalY / 32;
			int chunkZ2 = pos3.Z / 32;
			if (chunkX2 != prevChunkX || chunkY2 != prevChunkY || chunkZ2 != prevChunkZ)
			{
				chunk = worldmap.GetChunk(prevChunkX = chunkX2, prevChunkY = chunkY2, prevChunkZ = chunkZ2);
				if (chunk == null)
				{
					continue;
				}
				chunk.Unpack();
			}
			if (chunk != null)
			{
				if (blockUpdate2.ExchangeOnly)
				{
					chunk.GetLocalBlockEntityAtBlockPos(pos3).OnExchanged(blockList[solidBlockId]);
					continue;
				}
				blockList[blockUpdate2.OldBlockId].OnBlockRemoved(worldmap.World, pos3);
				blockList[solidBlockId].OnBlockPlaced(worldmap.World, pos3, blockUpdate2.ByStack);
			}
		}
		foreach (KeyValuePair<BlockPos, BlockUpdate> item in StagedBlocks.Where((KeyValuePair<BlockPos, BlockUpdate> b) => b.Value.Decors != null))
		{
			item.Deconstruct(out key, out value);
			BlockPos pos2 = key;
			BlockUpdate blockUpdate = value;
			int chunkX = pos2.X / 32;
			int chunkY = pos2.InternalY / 32;
			int chunkZ = pos2.Z / 32;
			if (chunkX != prevChunkX || chunkY != prevChunkY || chunkZ != prevChunkZ)
			{
				chunk = worldmap.GetChunk(prevChunkX = chunkX, prevChunkY = chunkY, prevChunkZ = chunkZ);
				if (chunk == null)
				{
					continue;
				}
				chunk.Unpack();
				dirtyChunkPositions.Add(new ChunkPosCompact(chunkX, chunkY, chunkZ));
			}
			if (chunk == null)
			{
				continue;
			}
			int index3d = worldmap.ChunkSizedIndex3D(pos2.X & 0x1F, pos2.Y & 0x1F, pos2.Z & 0x1F);
			foreach (DecorUpdate decorUpdate in blockUpdate.Decors)
			{
				int newdecorId = decorUpdate.decorId;
				Block newDecorBlock = blockList[newdecorId];
				if (newdecorId > 0)
				{
					chunk.SetDecor(newDecorBlock, index3d, decorUpdate.faceAndSubposition);
					chunk.Empty = false;
				}
			}
			updatedDecorPositions.Add(pos2.Copy());
		}
		if (relight)
		{
			foreach (BlockPos pos in LightSources.Keys)
			{
				StagedBlocks.Remove(pos);
			}
			worldmap.UpdateLightingBulk(StagedBlocks);
			worldmap.UpdateLightingBulk(LightSources);
		}
		foreach (ChunkPosCompact cp in dirtyChunkPositions)
		{
			worldmap.MarkChunkDirty(cp.X, cp.Y, cp.Z, priority: true);
		}
		if (synchronize)
		{
			worldmap.SendBlockUpdateBulk(updatedBlockPositions, relight);
			worldmap.SendDecorUpdateBulk(updatedDecorPositions);
		}
		StagedBlocks.Clear();
		LightSources.Clear();
		dirtyChunkPositions.Clear();
		_blockBreakTasks.Clear();
		return updatedBlocks;
	}

	public override void Rollback()
	{
		StagedBlocks.Clear();
		LightSources.Clear();
		_blockBreakTasks.Clear();
	}

	public override void ExchangeBlock(int blockId, BlockPos pos)
	{
		if ((pos.X | pos.Y | pos.Z) >= 0 && (pos.dimension != 0 || (pos.X < worldmap.MapSizeX && pos.Y < worldmap.MapSizeY && pos.Z < worldmap.MapSizeZ)))
		{
			BlockPos copied = pos.Copy();
			StagedBlocks[copied] = new BlockUpdate
			{
				NewSolidBlockId = blockId,
				Pos = copied,
				ExchangeOnly = true
			};
		}
	}

	public override void BreakBlock(BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		_blockBreakTasks.Enqueue(new BlockBreakTask
		{
			Pos = pos,
			byPlayer = byPlayer,
			DropQuantityMultiplier = dropQuantityMultiplier
		});
	}

	public int GetStagedBlockId(int posX, int posY, int posZ)
	{
		if (StagedBlocks.TryGetValue(new BlockPos(posX, posY, posZ), out var bd) && bd.NewSolidBlockId >= 0)
		{
			return bd.NewSolidBlockId;
		}
		return GetNonStagedBlockId(posX, posY, posZ, 1);
	}

	public int GetStagedBlockId(BlockPos pos)
	{
		if (StagedBlocks.TryGetValue(pos, out var bd) && bd.NewSolidBlockId >= 0)
		{
			return bd.NewSolidBlockId;
		}
		return GetNonStagedBlockId(pos.X, pos.InternalY, pos.Z, 1);
	}

	public void SetChunks(Vec2i chunkCoord, IWorldChunk[] chunksCol)
	{
		throw new NotImplementedException();
	}

	public void PostCommitCleanup(List<BlockUpdate> updatedBlocks)
	{
		FixWaterfalls(updatedBlocks);
	}

	private void FixWaterfalls(List<BlockUpdate> updatedBlocks)
	{
		Dictionary<BlockPos, BlockPos> updateNeighbours = new Dictionary<BlockPos, BlockPos>();
		BlockPos updTmpPos = new BlockPos();
		HashSet<BlockPos> blockPos = new HashSet<BlockPos>(updatedBlocks.Select((BlockUpdate b) => b.Pos).ToList());
		List<int> fluidBlockIds = (from b in worldmap.Blocks
			where b.IsLiquid()
			select b.Id).ToList();
		foreach (BlockUpdate upd in updatedBlocks)
		{
			if (upd.OldFluidBlockId <= 0 || !fluidBlockIds.Contains(upd.OldFluidBlockId))
			{
				continue;
			}
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing face in aLLFACES)
			{
				updTmpPos.Set(upd.Pos).Offset(face);
				if (!blockPos.Contains(updTmpPos))
				{
					updateNeighbours.TryAdd(updTmpPos.Copy(), upd.Pos.Copy());
				}
			}
		}
		int prevChunkX = -1;
		int prevChunkY = -1;
		int prevChunkZ = -1;
		IWorldChunk chunk = null;
		foreach (KeyValuePair<BlockPos, BlockPos> pos in updateNeighbours)
		{
			int chunkX = pos.Value.X / 32;
			int chunkY = pos.Value.InternalY / 32;
			int chunkZ = pos.Value.Z / 32;
			if (chunkX != prevChunkX || chunkY != prevChunkY || chunkZ != prevChunkZ)
			{
				chunk = worldmap.GetChunk(prevChunkX = chunkX, prevChunkY = chunkY, prevChunkZ = chunkZ);
				if (chunk == null)
				{
					continue;
				}
				chunk.Unpack();
				dirtyChunkPositions.Add(new ChunkPosCompact(chunkX, chunkY, chunkZ));
			}
			if (chunk != null)
			{
				int index3d = worldmap.ChunkSizedIndex3D(pos.Key.X & 0x1F, pos.Key.Y & 0x1F, pos.Key.Z & 0x1F);
				Block block = worldmap.Blocks[chunk.Data[index3d]];
				if (block.IsLiquid())
				{
					block.OnNeighbourBlockChange(worldAccessor, pos.Key, pos.Value);
				}
				else
				{
					worldmap.Blocks[chunk.Data.GetFluid(index3d)].OnNeighbourBlockChange(worldAccessor, pos.Key, pos.Value);
				}
			}
		}
		foreach (ChunkPosCompact cp in dirtyChunkPositions)
		{
			worldmap.MarkChunkDirty(cp.X, cp.Y, cp.Z, priority: true);
		}
		dirtyChunkPositions.Clear();
		worldmap.SendBlockUpdateBulk(updateNeighbours.Keys, relight);
	}
}
