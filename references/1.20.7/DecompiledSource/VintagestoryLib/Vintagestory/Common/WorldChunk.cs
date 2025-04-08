using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public abstract class WorldChunk : IWorldChunk
{
	public bool WasModified;

	protected ChunkDataPool datapool;

	protected ChunkData chunkdata;

	protected int chunkdataVersion;

	public long lastReadOrWrite;

	protected bool PotentialBlockOrLightingChanges;

	public byte[] blocksCompressedTmp;

	public byte[] lightCompressedTmp;

	public byte[] lightSatCompressedTmp;

	public byte[] fluidsCompressedTmp;

	public Entity[] Entities;

	public Dictionary<BlockPos, BlockEntity> BlockEntities = new Dictionary<BlockPos, BlockEntity>();

	public Dictionary<int, Block> Decors;

	internal object packUnpackLock = new object();

	private int _disposed;

	public byte[] blocksCompressed { get; set; }

	public byte[] lightCompressed { get; set; }

	public byte[] lightSatCompressed { get; set; }

	public byte[] fluidsCompressed { get; set; }

	public int EntitiesCount { get; set; }

	[Obsolete("Use Data field")]
	public IChunkBlocks Blocks => chunkdata;

	public IChunkBlocks Data => chunkdata;

	public IChunkLight Lighting => chunkdata;

	public IChunkBlocks MaybeBlocks { get; set; }

	public bool Empty { get; set; }

	public abstract IMapChunk MapChunk { get; }

	Entity[] IWorldChunk.Entities => Entities;

	Dictionary<BlockPos, BlockEntity> IWorldChunk.BlockEntities
	{
		get
		{
			return BlockEntities;
		}
		set
		{
			BlockEntities = value;
		}
	}

	public abstract HashSet<int> LightPositions { get; set; }

	public abstract Dictionary<string, byte[]> ModData { get; set; }

	public bool Disposed
	{
		get
		{
			return _disposed != 0;
		}
		set
		{
			_disposed = (value ? 1 : 0);
		}
	}

	public Dictionary<string, object> LiveModData { get; set; }

	public virtual void MarkModified()
	{
		WasModified = true;
		lastReadOrWrite = Environment.TickCount;
	}

	public virtual bool IsPacked()
	{
		return chunkdata == null;
	}

	public virtual void TryPackAndCommit(int chunkTTL = 8000)
	{
		if (Environment.TickCount - lastReadOrWrite >= chunkTTL)
		{
			Pack();
			TryCommitPackAndFree(chunkTTL);
		}
	}

	public virtual void Pack()
	{
		if (Disposed)
		{
			return;
		}
		lock (packUnpackLock)
		{
			if (chunkdata != null)
			{
				if (PotentialBlockOrLightingChanges)
				{
					chunkdata.CompressInto(ref blocksCompressedTmp, ref lightCompressedTmp, ref lightSatCompressedTmp, ref fluidsCompressedTmp, 2);
					return;
				}
				blocksCompressedTmp = blocksCompressed;
				lightCompressedTmp = lightCompressed;
				lightSatCompressedTmp = lightSatCompressed;
				fluidsCompressedTmp = fluidsCompressed;
			}
		}
	}

	public virtual bool TryCommitPackAndFree(int chunkTTL = 8000)
	{
		if (Disposed)
		{
			return false;
		}
		lock (packUnpackLock)
		{
			if (blocksCompressedTmp == null)
			{
				return false;
			}
			if (Environment.TickCount - lastReadOrWrite < chunkTTL)
			{
				blocksCompressedTmp = null;
				lightCompressedTmp = null;
				lightSatCompressedTmp = null;
				fluidsCompressedTmp = null;
				return false;
			}
			blocksCompressed = blocksCompressedTmp;
			blocksCompressedTmp = null;
			lightCompressed = lightCompressedTmp;
			lightCompressedTmp = null;
			lightSatCompressed = lightSatCompressedTmp;
			lightSatCompressedTmp = null;
			fluidsCompressed = fluidsCompressedTmp;
			fluidsCompressedTmp = null;
			if (chunkdata != null && blocksCompressed != null)
			{
				if (WasModified)
				{
					UpdateEmptyFlag();
				}
				datapool.Free(chunkdata);
				MaybeBlocks = datapool.OnlyAirBlocksData;
				chunkdata = null;
			}
			chunkdataVersion = 2;
			WasModified = false;
			PotentialBlockOrLightingChanges = false;
		}
		return true;
	}

	public virtual void Unpack()
	{
		if (Disposed)
		{
			return;
		}
		lock (packUnpackLock)
		{
			bool num = chunkdata == null;
			unpackNoLock();
			if (num)
			{
				blocksCompressed = null;
				lightCompressed = null;
				lightSatCompressed = null;
				fluidsCompressed = null;
			}
			PotentialBlockOrLightingChanges = true;
		}
	}

	protected virtual void UpdateForVersion()
	{
		PotentialBlockOrLightingChanges = true;
	}

	public virtual bool Unpack_ReadOnly()
	{
		if (Disposed)
		{
			return false;
		}
		lock (packUnpackLock)
		{
			bool result = chunkdata == null;
			unpackNoLock();
			return result;
		}
	}

	public virtual int UnpackAndReadBlock(int index, int layer)
	{
		if (Disposed)
		{
			return 0;
		}
		lock (packUnpackLock)
		{
			unpackNoLock();
			return chunkdata.GetBlockId(index, layer);
		}
	}

	public virtual ushort Unpack_AndReadLight(int index)
	{
		if (Disposed)
		{
			return 0;
		}
		lock (packUnpackLock)
		{
			unpackNoLock();
			return chunkdata.ReadLight(index);
		}
	}

	public virtual ushort Unpack_AndReadLight(int index, out int lightSat)
	{
		if (Disposed)
		{
			lightSat = 0;
			return 0;
		}
		lock (packUnpackLock)
		{
			unpackNoLock();
			return chunkdata.ReadLight(index, out lightSat);
		}
	}

	public virtual void Unpack_MaybeNullData()
	{
		lock (packUnpackLock)
		{
			lastReadOrWrite = Environment.TickCount;
			bool num = chunkdata == null;
			unpackNoLock();
			if (num)
			{
				blocksCompressed = null;
				lightCompressed = null;
				lightSatCompressed = null;
				fluidsCompressed = null;
			}
		}
	}

	private void unpackNoLock()
	{
		lastReadOrWrite = Environment.TickCount;
		if (chunkdata == null)
		{
			chunkdata = datapool.Request();
			chunkdata.DecompressFrom(blocksCompressed, lightCompressed, lightSatCompressed, fluidsCompressed, chunkdataVersion);
			MaybeBlocks = chunkdata;
			if (chunkdataVersion < 2)
			{
				UpdateForVersion();
			}
		}
	}

	public void AcquireBlockReadLock()
	{
		Unpack_ReadOnly();
		Data.TakeBulkReadLock();
	}

	public void ReleaseBlockReadLock()
	{
		Data.ReleaseBulkReadLock();
	}

	public virtual void UpdateEmptyFlag()
	{
		Empty = chunkdata.IsEmpty();
	}

	public virtual void MarkFresh()
	{
		lastReadOrWrite = Environment.TickCount;
	}

	internal virtual void AddBlockEntity(BlockEntity blockEntity)
	{
		lock (packUnpackLock)
		{
			BlockEntities[blockEntity.Pos] = blockEntity;
		}
	}

	public virtual bool RemoveBlockEntity(BlockPos pos)
	{
		lock (packUnpackLock)
		{
			return BlockEntities.Remove(pos);
		}
	}

	public virtual void AddEntity(Entity entity)
	{
		lock (packUnpackLock)
		{
			if (Entities == null)
			{
				Entities = new Entity[32];
			}
			for (int i = 0; i < EntitiesCount; i++)
			{
				if (Entities[i].EntityId == entity.EntityId)
				{
					Entities[i] = entity;
					return;
				}
			}
			if (EntitiesCount >= Entities.Length)
			{
				Array.Resize(ref Entities, EntitiesCount + 32);
			}
			Entities[EntitiesCount] = entity;
			EntitiesCount++;
		}
	}

	public virtual bool RemoveEntity(long entityId)
	{
		lock (packUnpackLock)
		{
			Entity[] Entities;
			if ((Entities = this.Entities) == null)
			{
				return false;
			}
			int EntitiesCount = this.EntitiesCount;
			for (int i = 0; i < Entities.Length; i++)
			{
				Entity e = Entities[i];
				if (e == null)
				{
					if (i >= EntitiesCount)
					{
						break;
					}
				}
				else if (e.EntityId == entityId)
				{
					for (int j = i + 1; j < Entities.Length && j < EntitiesCount; j++)
					{
						Entities[j - 1] = Entities[j];
					}
					Entities[EntitiesCount - 1] = null;
					this.EntitiesCount--;
					return true;
				}
			}
		}
		return false;
	}

	public void SetModdata(string key, byte[] data)
	{
		ModData[key] = data;
		MarkModified();
	}

	public void RemoveModdata(string key)
	{
		ModData.Remove(key);
		MarkModified();
	}

	public byte[] GetModdata(string key)
	{
		ModData.TryGetValue(key, out var data);
		return data;
	}

	public void SetModdata<T>(string key, T data)
	{
		SetModdata(key, SerializerUtil.Serialize(data));
	}

	public T GetModdata<T>(string key, T defaultValue = default(T))
	{
		byte[] data = GetModdata(key);
		if (data == null)
		{
			return defaultValue;
		}
		return SerializerUtil.Deserialize<T>(data);
	}

	public void Dispose()
	{
		if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
		{
			return;
		}
		lock (packUnpackLock)
		{
			ChunkData oldData = chunkdata;
			chunkdata = datapool.BlackHoleData;
			MaybeBlocks = datapool.OnlyAirBlocksData;
			Empty = true;
			if (oldData != null)
			{
				datapool.Free(oldData);
			}
		}
	}

	public Block GetLocalBlockAtBlockPos(IWorldAccessor world, BlockPos position)
	{
		return GetLocalBlockAtBlockPos(world, position.X, position.Y, position.Z);
	}

	public Block GetLocalBlockAtBlockPos(IWorldAccessor world, int posX, int posY, int posZ, int layer = 0)
	{
		int lx = posX % 32;
		int ly = posY % 32;
		int lz = posZ % 32;
		return world.Blocks[UnpackAndReadBlock((ly * 32 + lz) * 32 + lx, layer)];
	}

	public Block GetLocalBlockAtBlockPos_LockFree(IWorldAccessor world, BlockPos pos, int layer = 0)
	{
		int lx = pos.X % 32;
		int ly = pos.Y % 32;
		int lz = pos.Z % 32;
		int blockId = chunkdata.GetBlockIdUnsafe((ly * 32 + lz) * 32 + lx, layer);
		return world.Blocks[blockId];
	}

	public BlockEntity GetLocalBlockEntityAtBlockPos(BlockPos position)
	{
		BlockEntities.TryGetValue(position, out var blockEntity);
		return blockEntity;
	}

	public virtual void FinishLightDoubleBuffering()
	{
	}

	public int GetLightAbsorptionAt(int index3d, BlockPos blockPos, IList<Block> blockTypes)
	{
		int solidBlockId = chunkdata.GetSolidBlock(index3d);
		int fluidBlockId = chunkdata.GetFluid(index3d);
		if (solidBlockId == 0)
		{
			return blockTypes[fluidBlockId].LightAbsorption;
		}
		int absSolid = blockTypes[solidBlockId].GetLightAbsorption(this, blockPos);
		if (fluidBlockId == 0)
		{
			return absSolid;
		}
		int absFluid = blockTypes[fluidBlockId].LightAbsorption;
		return Math.Max(absSolid, absFluid);
	}

	public bool SetDecor(Block block, int index3d, BlockFacing onFace)
	{
		if (block == null)
		{
			return false;
		}
		index3d += DecorBits.FaceToIndex(onFace);
		SetDecorInternal(index3d, block);
		return true;
	}

	public bool SetDecor(Block block, int index3d, int faceAndSubposition)
	{
		if (block == null)
		{
			return false;
		}
		int packedIndex = index3d + DecorBits.FaceAndSubpositionToIndex(faceAndSubposition);
		SetDecorInternal(packedIndex, block);
		return true;
	}

	private void SetDecorInternal(int packedIndex, Block block)
	{
		if (Decors == null)
		{
			Decors = new Dictionary<int, Block>();
		}
		lock (Decors)
		{
			if (block.Id == 0)
			{
				Decors.Remove(packedIndex);
			}
			else
			{
				Decors[packedIndex] = block;
			}
		}
	}

	public Dictionary<int, Block> GetSubDecors(IBlockAccessor blockAccessor, BlockPos position)
	{
		if (Decors == null || Decors.Count == 0)
		{
			return null;
		}
		int index3d = ToIndex3d(position);
		Dictionary<int, Block> decors = new Dictionary<int, Block>();
		foreach (KeyValuePair<int, Block> val in Decors)
		{
			int packedIndex = val.Key;
			if (DecorBits.Index3dFromIndex(packedIndex) == index3d)
			{
				decors[DecorBits.FaceAndSubpositionFromIndex(packedIndex)] = val.Value;
			}
		}
		return decors;
	}

	public Block[] GetDecors(IBlockAccessor blockAccessor, BlockPos position)
	{
		if (Decors == null || Decors.Count == 0)
		{
			return null;
		}
		int index3d = ToIndex3d(position);
		Block[] decors = new Block[6];
		foreach (KeyValuePair<int, Block> val in Decors)
		{
			int packedIndex = val.Key;
			if (DecorBits.Index3dFromIndex(packedIndex) == index3d)
			{
				decors[DecorBits.FaceFromIndex(packedIndex)] = val.Value;
			}
		}
		return decors;
	}

	public Block GetDecor(IBlockAccessor blockAccessor, BlockPos position, int faceAndSubposition)
	{
		if (Decors == null || Decors.Count == 0)
		{
			return null;
		}
		int packedIndex = ToIndex3d(position) + DecorBits.FaceAndSubpositionToIndex(faceAndSubposition);
		return TryGetDecor(ref packedIndex, BlockFacing.NORTH);
	}

	public bool BreakDecor(IWorldAccessor world, BlockPos pos, BlockFacing side = null, int? faceAndSubposition = null)
	{
		if (Decors == null || Decors.Count == 0)
		{
			return false;
		}
		int index3d = ToIndex3d(pos);
		if (side == null && !faceAndSubposition.HasValue)
		{
			List<int> toRemove = new List<int>();
			foreach (KeyValuePair<int, Block> val in Decors)
			{
				int packedIndex = val.Key;
				if (DecorBits.Index3dFromIndex(packedIndex) == index3d)
				{
					Block value = val.Value;
					toRemove.Add(packedIndex);
					value.OnBrokenAsDecor(world, pos, DecorBits.FacingFromIndex(packedIndex));
				}
			}
			lock (Decors)
			{
				foreach (int ix in toRemove)
				{
					Decors.Remove(ix);
				}
			}
			return true;
		}
		index3d += (faceAndSubposition.HasValue ? DecorBits.FaceAndSubpositionToIndex(faceAndSubposition.Value) : DecorBits.FaceToIndex(side));
		Block decor = TryGetDecor(ref index3d, BlockFacing.NORTH);
		if (decor == null)
		{
			return false;
		}
		decor.OnBrokenAsDecor(world, pos, side);
		lock (Decors)
		{
			Decors.Remove(index3d);
		}
		return true;
	}

	public bool BreakDecorPart(IWorldAccessor world, BlockPos pos, BlockFacing side, int faceAndSubposition)
	{
		return setDecorPart(world, pos, side, faceAndSubposition, callBlockBroken: true);
	}

	public bool RemoveDecorPart(IWorldAccessor world, BlockPos pos, BlockFacing side, int faceAndSubposition)
	{
		return setDecorPart(world, pos, side, faceAndSubposition, callBlockBroken: false);
	}

	private bool setDecorPart(IWorldAccessor world, BlockPos pos, BlockFacing side, int faceAndSubposition, bool callBlockBroken)
	{
		if (Decors == null || Decors.Count == 0)
		{
			return false;
		}
		int packedIndex = ToIndex3d(pos) + DecorBits.FaceAndSubpositionToIndex(faceAndSubposition);
		Block decor = TryGetDecor(ref packedIndex, BlockFacing.NORTH);
		if (decor == null)
		{
			return false;
		}
		if (callBlockBroken)
		{
			decor.OnBrokenAsDecor(world, pos, side);
		}
		lock (Decors)
		{
			Decors.Remove(packedIndex);
		}
		return true;
	}

	public void BreakAllDecorFast(IWorldAccessor world, BlockPos pos, int index3d, bool callOnBrokenAsDecor = true)
	{
		if (Decors == null)
		{
			return;
		}
		List<int> toRemove = new List<int>();
		foreach (KeyValuePair<int, Block> val in Decors)
		{
			int packedIndex = val.Key;
			if (DecorBits.Index3dFromIndex(packedIndex) == index3d)
			{
				toRemove.Add(packedIndex);
				if (callOnBrokenAsDecor)
				{
					val.Value.OnBrokenAsDecor(world, pos, DecorBits.FacingFromIndex(packedIndex));
				}
			}
		}
		lock (Decors)
		{
			foreach (int ix in toRemove)
			{
				Decors.Remove(ix);
			}
		}
	}

	public Cuboidf[] AdjustSelectionBoxForDecor(IBlockAccessor blockAccessor, BlockPos position, Cuboidf[] orig)
	{
		if (Decors == null || Decors.Count == 0)
		{
			return orig;
		}
		Cuboidf box = orig[0];
		int index3d = ToIndex3d(position);
		bool changed = false;
		foreach (KeyValuePair<int, Block> val in Decors)
		{
			int packedIndex = val.Key;
			if (DecorBits.Index3dFromIndex(packedIndex) != index3d)
			{
				continue;
			}
			float thickness = val.Value.DecorThickness;
			if (thickness > 0f)
			{
				if (!changed)
				{
					changed = true;
					box = box.Clone();
				}
				box.Expand(DecorBits.FacingFromIndex(packedIndex), thickness);
			}
		}
		if (!changed)
		{
			return orig;
		}
		return new Cuboidf[1] { box };
	}

	public List<Cuboidf> GetDecorSelectionBoxes(IBlockAccessor blockAccessor, BlockPos position)
	{
		int chunkEdge = 31;
		int lx = position.X % 32;
		int num = position.Y % 32;
		int lz = position.Z % 32;
		List<Cuboidf> result = new List<Cuboidf>();
		int index = (num * 32 + lz) * 32 + lx;
		if (lz == 0)
		{
			((WorldChunk)blockAccessor.GetChunk(position.X / 32, position.InternalY / 32, (position.Z - 1) / 32))?.AddDecorSelectionBox(result, index + chunkEdge * 32, BlockFacing.NORTH);
		}
		else
		{
			AddDecorSelectionBox(result, index - 32, BlockFacing.NORTH);
		}
		if (lz == chunkEdge)
		{
			((WorldChunk)blockAccessor.GetChunk(position.X / 32, position.InternalY / 32, (position.Z + 1) / 32))?.AddDecorSelectionBox(result, index - chunkEdge * 32, BlockFacing.SOUTH);
		}
		else
		{
			AddDecorSelectionBox(result, index + 32, BlockFacing.SOUTH);
		}
		if (lx == 0)
		{
			((WorldChunk)blockAccessor.GetChunk((position.X - 1) / 32, position.InternalY / 32, position.Z / 32))?.AddDecorSelectionBox(result, index + chunkEdge, BlockFacing.WEST);
		}
		else
		{
			AddDecorSelectionBox(result, index - 1, BlockFacing.WEST);
		}
		if (lx == chunkEdge)
		{
			((WorldChunk)blockAccessor.GetChunk((position.X + 1) / 32, position.InternalY / 32, position.Z / 32))?.AddDecorSelectionBox(result, index - chunkEdge, BlockFacing.EAST);
		}
		else
		{
			AddDecorSelectionBox(result, index + 1, BlockFacing.EAST);
		}
		if (num == 0)
		{
			((WorldChunk)blockAccessor.GetChunk(position.X / 32, (position.InternalY - 1) / 32, position.Z / 32))?.AddDecorSelectionBox(result, index + chunkEdge * 32 * 32, BlockFacing.DOWN);
		}
		else
		{
			AddDecorSelectionBox(result, index - 1024, BlockFacing.DOWN);
		}
		if (num == chunkEdge)
		{
			((WorldChunk)blockAccessor.GetChunk(position.X / 32, (position.InternalY + 1) / 32, position.Z / 32))?.AddDecorSelectionBox(result, lz * 32 + lx, BlockFacing.UP);
		}
		else
		{
			AddDecorSelectionBox(result, index + 1024, BlockFacing.UP);
		}
		return result;
	}

	private void AddDecorSelectionBox(List<Cuboidf> result, int index, BlockFacing face)
	{
		if (Decors == null)
		{
			return;
		}
		Block block = TryGetDecor(ref index, face.Opposite);
		if (block != null)
		{
			float thickness = block.DecorThickness;
			if (thickness != 0f)
			{
				DecorSelectionBox box = face.Index switch
				{
					0 => new DecorSelectionBox(0f, 0f, 0f, 1f, 1f, thickness), 
					1 => new DecorSelectionBox(1f - thickness, 0f, 0f, 1f, 1f, 1f), 
					2 => new DecorSelectionBox(0f, 0f, 1f - thickness, 1f, 1f, 1f), 
					3 => new DecorSelectionBox(0f, 0f, 0f, thickness, 1f, 1f), 
					4 => new DecorSelectionBox(0f, 1f - thickness, 0f, 1f, 1f, 1f), 
					5 => new DecorSelectionBox(0f, 0f, 0f, 1f, thickness, 1f), 
					_ => null, 
				};
				box.PosAdjust = face.Normali;
				result.Add(box);
			}
		}
	}

	public Block TryGetDecor(ref int index, BlockFacing face)
	{
		int packedIndexBase = (index & -458753) + DecorBits.FaceToIndex(face);
		for (int rotationData = 0; rotationData <= 7; rotationData++)
		{
			if (Decors.TryGetValue(packedIndexBase + (rotationData << 16), out var block) && block != null)
			{
				index = packedIndexBase + (rotationData << 16);
				return block;
			}
		}
		return null;
	}

	public void SetDecors(Dictionary<int, Block> newDecors)
	{
		Decors = newDecors;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ToIndex3d(BlockPos pos)
	{
		int lx = pos.X % 32;
		int num = pos.Y % 32;
		int lz = pos.Z % 32;
		return (num * 32 + lz) * 32 + lx;
	}
}
