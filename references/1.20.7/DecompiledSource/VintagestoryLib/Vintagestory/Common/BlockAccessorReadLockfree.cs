using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class BlockAccessorReadLockfree : IBlockAccessor
{
	protected const int chunksize = 32;

	protected const int chunksizemask = 31;

	internal WorldMap worldmap;

	internal IWorldAccessor worldAccessor;

	public int MapSizeX => worldmap.MapSizeX;

	public int MapSizeY => worldmap.MapSizeY;

	public int MapSizeZ => worldmap.MapSizeZ;

	public int ChunkSize => 32;

	public int RegionSize => worldmap.RegionSize;

	public Vec3i MapSize => worldmap.MapSize;

	public int RegionMapSizeX => worldmap.RegionMapSizeX;

	public int RegionMapSizeY => worldmap.RegionMapSizeY;

	public int RegionMapSizeZ => worldmap.RegionMapSizeZ;

	public bool UpdateSnowAccumMap
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public BlockAccessorReadLockfree(WorldMap worldmap, IWorldAccessor worldAccessor)
	{
		this.worldmap = worldmap;
		this.worldAccessor = worldAccessor;
	}

	[Obsolete("Calling code should specify the appropriate BlockLayersAccess.  Default is fine in many cases, but BlockLayersAccess.Fluids should be used to check for water, lake ice or lava reliably")]
	public virtual int GetBlockId(int posX, int posY, int posZ)
	{
		return GetBlockId(posX, posY, posZ, 0);
	}

	[Obsolete("Calling code should specify the appropriate BlockLayersAccess.  Default is fine in many cases, but BlockLayersAccess.Fluids should be used to check for water, lake ice or lava reliably")]
	public virtual int GetBlockId(BlockPos pos)
	{
		return GetBlockId(pos.X, pos.Y, pos.Z, 0);
	}

	[Obsolete("Not dimension aware, and calling code should specify the appropriate BlockLayersAccess.  Default is fine in many cases, but BlockLayersAccess.Fluids should be used to check for water, lake ice or lava reliably")]
	public virtual Block GetBlock(int posX, int posY, int posZ)
	{
		return GetBlockRaw(posX, posY, posZ);
	}

	public virtual Block GetBlock(BlockPos pos)
	{
		return GetBlock(pos, 0);
	}

	public void BreakBlock(BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		throw new NotImplementedException();
	}

	public List<BlockUpdate> Commit()
	{
		throw new NotImplementedException();
	}

	public void DamageBlock(BlockPos pos, BlockFacing facing, float damage)
	{
		throw new NotImplementedException();
	}

	public void ExchangeBlock(int blockId, BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public Block GetBlockRaw(int x, int y, int z, int layer = 0)
	{
		return worldmap.Blocks[GetBlockId(x, y, z, layer)];
	}

	public Block GetBlock(int x, int y, int z, int layer)
	{
		return GetBlockRaw(x, y, z, layer);
	}

	public Block GetBlock(BlockPos pos, int layer)
	{
		return worldmap.Blocks[GetBlockId(pos, layer)];
	}

	public Block GetMostSolidBlock(BlockPos pos)
	{
		return GetMostSolidBlock(pos.X, pos.InternalY, pos.Z);
	}

	public Block GetMostSolidBlock(int posX, int posY, int posZ)
	{
		if ((posX | posY | posZ) < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			return worldmap.Blocks[0];
		}
		IWorldChunk chunk = worldmap.GetChunkNonLocking(posX / 32, posY / 32, posZ / 32);
		if (chunk == null)
		{
			return worldmap.Blocks[0];
		}
		int index3d = worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F);
		int blockId = chunk.MaybeBlocks.GetFluid(index3d);
		if (blockId == 0 || !worldmap.Blocks[blockId].SideSolid.Any)
		{
			blockId = chunk.MaybeBlocks[index3d];
		}
		return worldmap.Blocks[blockId];
	}

	public bool IsSideSolid(int x, int y, int z, BlockFacing facing)
	{
		int blockId = GetBlockId(x, y, z, 2);
		if (blockId == 0 || !worldmap.Blocks[blockId].SideSolid.Any)
		{
			blockId = GetBlockId(x, y, z, 1);
		}
		return worldmap.Blocks[blockId].SideSolid[facing.Index];
	}

	public Block GetBlock(int blockId)
	{
		return worldmap.Blocks[blockId];
	}

	public Block GetBlock(AssetLocation code)
	{
		worldmap.BlocksByCode.TryGetValue(code, out var block);
		return block;
	}

	public BlockEntity GetBlockEntity(BlockPos position)
	{
		return GetChunkAtBlockPos(position)?.GetLocalBlockEntityAtBlockPos(position);
	}

	public int GetBlockId(int posX, int posY, int posZ, int layer)
	{
		if ((posX | posY | posZ) < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			return 0;
		}
		return worldmap.GetChunkNonLocking(posX / 32, posY / 32, posZ / 32)?.MaybeBlocks.GetBlockId(worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F), layer) ?? 0;
	}

	public int GetBlockId(BlockPos pos, int layer)
	{
		if ((pos.X | pos.Y | pos.Z) < 0 || (pos.dimension == 0 && (pos.X >= worldmap.MapSizeX || pos.Y >= worldmap.MapSizeY || pos.Z >= worldmap.MapSizeZ)))
		{
			return 0;
		}
		return worldmap.GetChunkNonLocking(pos.X / 32, pos.Y / 32, pos.Z / 32)?.MaybeBlocks.GetBlockId(worldmap.ChunkSizedIndex3D(pos.X & 0x1F, pos.Y & 0x1F, pos.Z & 0x1F), layer) ?? 0;
	}

	public Block GetBlockOrNull(int posX, int posY, int posZ, int layer = 4)
	{
		if ((posX | posY | posZ) < 0 || posX >= worldmap.MapSizeX || posZ >= worldmap.MapSizeZ)
		{
			return null;
		}
		IWorldChunk chunk = worldmap.GetChunkNonLocking(posX / 32, posY / 32, posZ / 32);
		if (chunk != null)
		{
			return worldmap.Blocks[chunk.MaybeBlocks[worldmap.ChunkSizedIndex3D(posX & 0x1F, posY & 0x1F, posZ & 0x1F)]];
		}
		return null;
	}

	public IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		return worldmap.GetChunkNonLocking(chunkX, chunkY, chunkZ);
	}

	public IWorldChunk GetChunk(long chunkIndex3D)
	{
		throw new NotImplementedException();
	}

	public IWorldChunk GetChunkAtBlockPos(int posX, int posY, int posZ)
	{
		return worldmap.GetChunkNonLocking(posX / 32, posY / 32, posZ / 32);
	}

	public IWorldChunk GetChunkAtBlockPos(BlockPos pos)
	{
		return worldmap.GetChunkNonLocking(pos.X / 32, pos.Y / 32, pos.Z / 32);
	}

	public virtual void MarkChunkDecorsModified(BlockPos pos)
	{
		worldmap.MarkDecorsDirty(pos);
	}

	public ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.NowValues, double totalDays = 0.0)
	{
		throw new NotImplementedException();
	}

	public ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition baseClimate, EnumGetClimateMode mode, double totalDays)
	{
		throw new NotImplementedException();
	}

	public ClimateCondition GetClimateAt(BlockPos pos, int climate)
	{
		throw new NotImplementedException();
	}

	public int GetDistanceToRainFall(BlockPos pos, int horziontalSearchWidth = 4, int verticalSearchWidth = 1)
	{
		throw new NotImplementedException();
	}

	public int GetLightLevel(BlockPos pos, EnumLightLevelType type)
	{
		throw new NotImplementedException();
	}

	public int GetLightLevel(int x, int y, int z, EnumLightLevelType type)
	{
		throw new NotImplementedException();
	}

	public Vec4f GetLightRGBs(int posX, int posY, int posZ)
	{
		return worldmap.GetLightRGBSVec4f(posX, posY, posZ);
	}

	public int GetLightRGBsAsInt(int posX, int posY, int posZ)
	{
		WorldMap worldmap = this.worldmap;
		if (!IsValidPos(posX, posY, posZ))
		{
			return ColorUtil.HsvToRgba(0, 0, 0, (int)(worldmap.SunLightLevels[worldmap.SunBrightness] * 255f));
		}
		IWorldChunk chunk = worldmap.GetChunkNonLocking(posX / 32, posY / 32, posZ / 32);
		if (chunk == null)
		{
			return ColorUtil.HsvToRgba(0, 0, 0, (int)(worldmap.SunLightLevels[worldmap.SunBrightness] * 255f));
		}
		int chunkSizeMask = worldmap.ChunkSizeMask;
		int index3d = MapUtil.Index3d(posX & chunkSizeMask, posY & chunkSizeMask, posZ & chunkSizeMask, 32, 32);
		int blocksat;
		ushort num = chunk.Unpack_AndReadLight(index3d, out blocksat);
		int sunl = num & 0x1F;
		int blockl = (num >> 5) & 0x1F;
		int blockhue = num >> 10;
		int sunb = (int)(worldmap.SunLightLevels[sunl] * 255f);
		byte h = worldmap.hueLevels[blockhue];
		int blocks = worldmap.satLevels[blocksat];
		int blockb = (int)(worldmap.BlockLightLevels[blockl] * 255f);
		return ColorUtil.HsvToRgba(h, blocks, blockb, sunb);
	}

	public Vec4f GetLightRGBs(BlockPos pos)
	{
		return worldmap.GetLightRGBSVec4f(pos.X, pos.Y, pos.Z);
	}

	public IMapChunk GetMapChunk(Vec2i chunkPos)
	{
		return worldmap.GetMapChunk(chunkPos.X, chunkPos.Y);
	}

	public IMapChunk GetMapChunk(int chunkX, int chunkZ)
	{
		return worldmap.GetMapChunk(chunkX, chunkZ);
	}

	public IMapChunk GetMapChunkAtBlockPos(BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public IMapRegion GetMapRegion(int regionX, int regionZ)
	{
		throw new NotImplementedException();
	}

	public int GetRainMapHeightAt(BlockPos pos)
	{
		IMapChunk mapchunk = GetMapChunk(pos.X / 32, pos.Z / 32);
		if (mapchunk == null || !worldmap.IsValidPos(pos.X, 0, pos.Z))
		{
			return 0;
		}
		int index2d = pos.Z % 32 * 32 + pos.X % 32;
		return mapchunk.WorldGenTerrainHeightMap[index2d];
	}

	public int GetRainMapHeightAt(int posX, int posZ)
	{
		IMapChunk mapchunk = GetMapChunk(posX / 32, posZ / 32);
		if (mapchunk == null || !worldmap.IsValidPos(posX, 0, posZ))
		{
			return 0;
		}
		int index2d = posZ % 32 * 32 + posX % 32;
		return mapchunk.RainHeightMap[index2d];
	}

	public int GetTerrainMapheightAt(BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public bool IsNotTraversable(double x, double y, double z)
	{
		throw new NotImplementedException();
	}

	public bool IsNotTraversable(double x, double y, double z, int dimension)
	{
		throw new NotImplementedException();
	}

	public bool IsNotTraversable(BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public bool IsValidPos(int posX, int posY, int posZ)
	{
		return worldmap.IsValidPos(posX, posY, posZ);
	}

	public bool IsValidPos(BlockPos pos)
	{
		return worldmap.IsValidPos(pos.X, pos.Y, pos.Z);
	}

	public void MarkAbsorptionChanged(int oldAbsorption, int newAbsorption, BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public void MarkBlockDirty(BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public void MarkBlockDirty(BlockPos pos, Action OnRetesselated)
	{
		throw new NotImplementedException();
	}

	public void MarkBlockEntityDirty(BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public void RemoveBlockEntity(BlockPos position)
	{
		throw new NotImplementedException();
	}

	public void RemoveBlockLight(byte[] oldLightHsV, BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public void Rollback()
	{
		throw new NotImplementedException();
	}

	public void SearchBlocks(BlockPos minPos, BlockPos maxPos, ActionConsumable<Block, BlockPos> onBlock, Action<int, int, int> onChunkMissing = null)
	{
		throw new NotImplementedException();
	}

	public void SearchFluidBlocks(BlockPos minPos, BlockPos maxPos, ActionConsumable<Block, BlockPos> onBlock, Action<int, int, int> onChunkMissing = null)
	{
		throw new NotImplementedException();
	}

	public void SetBlock(int blockId, BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public void SetFluidBlock(int fluidBlockId, BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack)
	{
		throw new NotImplementedException();
	}

	public void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null)
	{
		throw new NotImplementedException();
	}

	public void TriggerNeighbourBlockUpdate(BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public void WalkBlocks(BlockPos minPos, BlockPos maxPos, Action<Block, int, int, int> onBlock, bool centerOrder = false)
	{
		int num = GameMath.Clamp(minPos.X, 0, worldmap.MapSizeX);
		int miny = GameMath.Clamp(minPos.Y, 0, worldmap.MapSizeY);
		int minz = GameMath.Clamp(minPos.Z, 0, worldmap.MapSizeZ);
		int maxx = GameMath.Clamp(maxPos.X, 0, worldmap.MapSizeX);
		int maxy = GameMath.Clamp(maxPos.Y, 0, worldmap.MapSizeY);
		int maxz = GameMath.Clamp(maxPos.Z, 0, worldmap.MapSizeZ);
		int dimensionOffsetY = minPos.dimension * 1024;
		for (int x = num; x <= maxx; x++)
		{
			for (int y = miny; y <= maxy; y++)
			{
				for (int z = minz; z <= maxz; z++)
				{
					int index3d = (y % 32 * 32 + z % 32) * 32 + x % 32;
					int cx = x / 32;
					int cy = y / 32 + dimensionOffsetY;
					int cz = z / 32;
					IWorldChunk chunk = worldmap.GetChunkNonLocking(cx, cy, cz);
					if (chunk != null)
					{
						Block block = worldmap.Blocks[chunk.MaybeBlocks[index3d]];
						onBlock(block, x, y, z);
					}
				}
			}
		}
	}

	public void WalkStructures(BlockPos pos, Action<GeneratedStructure> onStructure)
	{
		throw new NotImplementedException();
	}

	public void WalkStructures(BlockPos minpos, BlockPos maxpos, Action<GeneratedStructure> onStructure)
	{
		throw new NotImplementedException();
	}

	public Vec3d GetWindSpeedAt(Vec3d pos)
	{
		throw new NotImplementedException();
	}

	public Vec3d GetWindSpeedAt(BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public void MarkBlockModified(BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public void MarkBlockDirty(BlockPos pos, IPlayer skipPlayer = null)
	{
		throw new NotImplementedException();
	}

	public bool SetDecor(Block block, BlockPos position, BlockFacing onFace)
	{
		throw new NotImplementedException();
	}

	public bool SetDecor(Block block, BlockPos position, int subPosition)
	{
		throw new NotImplementedException();
	}

	public Dictionary<int, Block> GetSubDecors(BlockPos position)
	{
		throw new NotImplementedException();
	}

	public Block[] GetDecors(BlockPos position)
	{
		throw new NotImplementedException();
	}

	public void BreakDecor(BlockPos pos, BlockFacing side = null)
	{
		throw new NotImplementedException();
	}

	public Block GetDecor(BlockPos pos, int faceAndSubposition)
	{
		throw new NotImplementedException();
	}

	public void BreakDecorPart(BlockPos pos, BlockFacing side, int faceAndSubposition)
	{
		throw new NotImplementedException();
	}

	public bool BreakDecor(BlockPos pos, BlockFacing side = null, int? faceAndSubposition = null)
	{
		throw new NotImplementedException();
	}

	public void SpawnBlockEntity(BlockEntity be)
	{
		throw new NotImplementedException();
	}

	public void SetBlock(int blockId, BlockPos pos, int layer)
	{
		throw new NotImplementedException();
	}

	public T GetInterface<T>(BlockPos pos)
	{
		throw new NotImplementedException();
	}

	public T GetBlockEntity<T>(BlockPos position) where T : BlockEntity
	{
		return GetBlockEntity(position) as T;
	}

	public IMiniDimension CreateMiniDimension(Vec3d position)
	{
		throw new NotImplementedException();
	}
}
