using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Server;

public sealed class ServerWorldMap : WorldMap, IChunkProvider, ILandClaimAPI
{
	internal ServerMain server;

	private Vec3i mapsize = new Vec3i();

	public ChunkIlluminator chunkIlluminatorWorldGen;

	public ChunkIlluminator chunkIlluminatorMainThread;

	public IBlockAccessor StrictBlockAccess;

	public IBlockAccessor RelaxedBlockAccess;

	public BlockAccessorRelaxedBulkUpdate BulkBlockAccess;

	public IBlockAccessor RawRelaxedBlockAccess;

	public BlockAccessorPrefetch PrefetchBlockAccess;

	private int prevChunkX = -1;

	private int prevChunkY = -1;

	private int prevChunkZ = -1;

	private IWorldChunk prevChunk;

	public object LightingTasksLock = new object();

	public Queue<UpdateLightingTask> LightingTasks = new Queue<UpdateLightingTask>();

	private int regionMapSizeX;

	private int regionMapSizeY;

	private int regionMapSizeZ;

	public override ILogger Logger => ServerMain.Logger;

	ILogger IChunkProvider.Logger => ServerMain.Logger;

	public override IList<Block> Blocks => server.Blocks;

	public override Dictionary<AssetLocation, Block> BlocksByCode => server.BlocksByCode;

	public override int ChunkSize => 32;

	public override int RegionSize => 32 * MagicNum.ChunkRegionSizeInChunks;

	public override Vec3i MapSize => mapsize;

	public override int MapSizeX => mapsize.X;

	public override int MapSizeY => mapsize.Y;

	public override int MapSizeZ => mapsize.Z;

	public override int RegionMapSizeX => regionMapSizeX;

	public override int RegionMapSizeY => regionMapSizeY;

	public override int RegionMapSizeZ => regionMapSizeZ;

	public override IWorldAccessor World => server;

	public override int ChunkSizeMask => 31;

	public override List<LandClaim> All => server.SaveGameData.LandClaims;

	public override bool DebugClaimPrivileges => server.DebugPrivileges;

	public ServerWorldMap(ServerMain server)
	{
		this.server = server;
		chunkIlluminatorWorldGen = new ChunkIlluminator(null, new BlockAccessorRelaxed(this, server, synchronize: false, relight: false), MagicNum.ServerChunkSize);
		chunkIlluminatorMainThread = new ChunkIlluminator(this, new BlockAccessorRelaxed(this, server, synchronize: false, relight: false), MagicNum.ServerChunkSize);
		RelaxedBlockAccess = new BlockAccessorRelaxed(this, server, synchronize: true, relight: true);
		RawRelaxedBlockAccess = new BlockAccessorRelaxed(this, server, synchronize: false, relight: false);
		StrictBlockAccess = new BlockAccessorStrict(this, server, synchronize: true, relight: true, debug: false);
		BulkBlockAccess = new BlockAccessorRelaxedBulkUpdate(this, server, synchronize: true, relight: true, debug: false);
		PrefetchBlockAccess = new BlockAccessorPrefetch(this, server, synchronize: true, relight: true);
	}

	public void Init(int sizex, int sizey, int sizez)
	{
		mapsize = new Vec3i(sizex, sizey, sizez);
		chunkMapSizeY = sizey / 32;
		index3dMulX = 2097152;
		index3dMulZ = 2097152;
		chunkIlluminatorWorldGen.InitForWorld(server.Blocks, (ushort)server.sunBrightness, sizex, sizey, sizez);
		chunkIlluminatorMainThread.InitForWorld(server.Blocks, (ushort)server.sunBrightness, sizex, sizey, sizez);
		if (GameVersion.IsAtLeastVersion(server.SaveGameData.CreatedGameVersion, "1.12.9"))
		{
			regionMapSizeX = (int)Math.Ceiling((double)mapsize.X / (double)MagicNum.MapRegionSize);
			regionMapSizeY = (int)Math.Ceiling((double)mapsize.Y / (double)MagicNum.MapRegionSize);
			regionMapSizeZ = (int)Math.Ceiling((double)mapsize.Z / (double)MagicNum.MapRegionSize);
		}
		else
		{
			regionMapSizeX = mapsize.X / MagicNum.MapRegionSize;
			regionMapSizeY = mapsize.Y / MagicNum.MapRegionSize;
			regionMapSizeZ = mapsize.Z / MagicNum.MapRegionSize;
		}
		RebuildLandClaimPartitions();
	}

	public ChunkPos MapRegionPosFromIndex2D(long index)
	{
		return new ChunkPos((int)(index % RegionMapSizeX), 0, (int)(index / RegionMapSizeX), 0);
	}

	public void MapRegionPosFromIndex2D(long index, out int x, out int z)
	{
		x = (int)(index % RegionMapSizeX);
		z = (int)(index / RegionMapSizeX);
	}

	public Vec2i MapChunkPosFromChunkIndex2D(long chunkIndex2d)
	{
		return new Vec2i((int)(chunkIndex2d % base.ChunkMapSizeX), (int)(chunkIndex2d / base.ChunkMapSizeX));
	}

	public Dictionary<long, WorldChunk> PositionsToUniqueChunks(List<BlockPos> positions)
	{
		FastSetOfLongs indices = new FastSetOfLongs();
		foreach (BlockPos pos in positions)
		{
			indices.Add(ChunkIndex3D(pos.X / 32, pos.InternalY / 32, pos.Z / 32));
		}
		Dictionary<long, WorldChunk> result = new Dictionary<long, WorldChunk>(indices.Count);
		foreach (long chunkIndex in indices)
		{
			result.Add(chunkIndex, GetChunk(chunkIndex) as WorldChunk);
		}
		return result;
	}

	public override IWorldChunk GetChunkAtPos(int posX, int posY, int posZ)
	{
		return GetServerChunk(posX / MagicNum.ServerChunkSize, posY / MagicNum.ServerChunkSize, posZ / MagicNum.ServerChunkSize);
	}

	public override WorldChunk GetChunk(BlockPos pos)
	{
		return GetServerChunk(pos.X / MagicNum.ServerChunkSize, pos.InternalY / MagicNum.ServerChunkSize, pos.Z / MagicNum.ServerChunkSize);
	}

	public override IWorldChunk GetChunk(long index3d)
	{
		return GetServerChunk(index3d);
	}

	public ServerChunk GetServerChunk(int chunkX, int chunkY, int chunkZ)
	{
		return GetServerChunk(ChunkIndex3D(chunkX, chunkY, chunkZ));
	}

	public ServerChunk GetServerChunk(long chunkIndex3d)
	{
		return server.GetLoadedChunk(chunkIndex3d);
	}

	public override IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		return GetServerChunk(ChunkIndex3D(chunkX, chunkY, chunkZ));
	}

	public IWorldChunk GetUnpackedChunkFast(int chunkX, int chunkY, int chunkZ, bool notRecentlyAccessed = false)
	{
		ServerChunk chunk = null;
		lock (server.loadedChunks)
		{
			if (chunkX == prevChunkX && chunkY == prevChunkY && chunkZ == prevChunkZ)
			{
				if (!notRecentlyAccessed)
				{
					return prevChunk;
				}
				chunk = (ServerChunk)prevChunk;
			}
			else
			{
				prevChunkX = chunkX;
				prevChunkY = chunkY;
				prevChunkZ = chunkZ;
				chunk = (ServerChunk)(prevChunk = server.GetLoadedChunk(ChunkIndex3D(chunkX, chunkY, chunkZ)));
			}
		}
		chunk?.Unpack();
		return chunk;
	}

	public override IWorldChunk GetChunkNonLocking(int chunkX, int chunkY, int chunkZ)
	{
		server.loadedChunks.TryGetValue(ChunkIndex3D(chunkX, chunkY, chunkZ), out var chunk);
		return chunk;
	}

	public override IMapRegion GetMapRegion(int regionX, int regionZ)
	{
		server.loadedMapRegions.TryGetValue(MapRegionIndex2D(regionX, regionZ), out var reg);
		return reg;
	}

	public IMapRegion GetMapRegion(BlockPos pos)
	{
		server.loadedMapRegions.TryGetValue(MapRegionIndex2D(pos.X / RegionSize, pos.Z / RegionSize), out var reg);
		return reg;
	}

	public override IMapChunk GetMapChunk(int chunkX, int chunkZ)
	{
		server.loadedMapChunks.TryGetValue(MapChunkIndex2D(chunkX, chunkZ), out var mpc);
		return mpc;
	}

	public override void SendSetBlock(int blockId, int posX, int posY, int posZ)
	{
		server.SendSetBlock(blockId, posX, posY, posZ);
	}

	public override void SendExchangeBlock(int blockId, int posX, int posY, int posZ)
	{
		server.SendSetBlock(blockId, posX, posY, posZ, -1, exchangeOnly: true);
	}

	public override void SendDecorUpdateBulk(IEnumerable<BlockPos> updatedDecorPositions)
	{
		foreach (BlockPos val in updatedDecorPositions)
		{
			MarkDecorsDirty(val);
		}
	}

	public override void SendBlockUpdateBulk(IEnumerable<BlockPos> blockUpdates, bool doRelight)
	{
		foreach (BlockPos val in blockUpdates)
		{
			MarkBlockModified(val, doRelight);
		}
	}

	public override void SendBlockUpdateBulkMinimal(Dictionary<BlockPos, BlockUpdate> updates)
	{
		foreach (KeyValuePair<BlockPos, BlockUpdate> val in updates)
		{
			server.ModifiedBlocksMinimal.Add(val.Key);
		}
	}

	public void SendBlockUpdateExcept(int blockId, int posX, int posY, int posZ, int clientId)
	{
		server.SendSetBlock(blockId, posX, posY, posZ, clientId);
	}

	public int GetTerrainGenSurfacePosY(int posX, int posZ)
	{
		long chunkIndex3d = server.WorldMap.ChunkIndex3D(posX / MagicNum.ServerChunkSize, 0, posZ / MagicNum.ServerChunkSize);
		ServerChunk chunk = GetServerChunk(chunkIndex3d);
		if (chunk == null || chunk.MapChunk == null)
		{
			return 0;
		}
		return chunk.MapChunk.WorldGenTerrainHeightMap[posZ % MagicNum.ServerChunkSize * MagicNum.ServerChunkSize + posX % MagicNum.ServerChunkSize] + 1;
	}

	public void MarkChunksDirty(BlockPos blockPos, int blockRange)
	{
		int num = (blockPos.X - blockRange) / MagicNum.ServerChunkSize;
		int maxcx = (blockPos.X + blockRange) / MagicNum.ServerChunkSize;
		int mincy = (blockPos.Y - blockRange) / MagicNum.ServerChunkSize;
		int maxcy = (blockPos.Y + blockRange) / MagicNum.ServerChunkSize;
		int mincz = (blockPos.Z - blockRange) / MagicNum.ServerChunkSize;
		int maxcz = (blockPos.Z + blockRange) / MagicNum.ServerChunkSize;
		for (int cx = num; cx <= maxcx; cx++)
		{
			for (int cy = mincy; cy <= maxcy; cy++)
			{
				for (int cz = mincz; cz <= maxcz; cz++)
				{
					GetServerChunk(cx, cy, cz)?.MarkModified();
				}
			}
		}
	}

	public override void MarkChunkDirty(int chunkX, int chunkY, int chunkZ, bool priority = false, bool sunRelight = false, Action OnRetesselated = null, bool fireDirtyEvent = true, bool edgeOnly = false)
	{
		ServerChunk chunk = GetServerChunk(chunkX, chunkY, chunkZ);
		if (chunk != null)
		{
			chunk.MarkModified();
			if (fireDirtyEvent)
			{
				server.api.eventapi.TriggerChunkDirty(new Vec3i(chunkX, chunkY, chunkZ), chunk, EnumChunkDirtyReason.MarkedDirty);
			}
		}
	}

	public override void UpdateLighting(int oldblockid, int newblockid, BlockPos pos)
	{
		long mapchunkindex2d = server.WorldMap.MapChunkIndex2D(pos.X / 32, pos.Z / 32);
		server.loadedMapChunks.TryGetValue(mapchunkindex2d, out var mapchunk);
		if (mapchunk == null)
		{
			return;
		}
		mapchunk.MarkFresh();
		lock (LightingTasksLock)
		{
			LightingTasks.Enqueue(new UpdateLightingTask
			{
				oldBlockId = oldblockid,
				newBlockId = newblockid,
				pos = pos.Copy()
			});
		}
	}

	public override void RemoveBlockLight(byte[] oldLightHsV, BlockPos pos)
	{
		lock (LightingTasksLock)
		{
			LightingTasks.Enqueue(new UpdateLightingTask
			{
				removeLightHsv = oldLightHsV,
				pos = pos.Copy()
			});
		}
		server.BroadcastPacket(new Packet_Server
		{
			Id = 72,
			RemoveBlockLight = new Packet_RemoveBlockLight
			{
				PosX = pos.X,
				PosY = pos.Y,
				PosZ = pos.Z,
				LightH = oldLightHsV[0],
				LightS = oldLightHsV[1],
				LightV = oldLightHsV[2]
			}
		});
	}

	public override void UpdateLightingAfterAbsorptionChange(int oldAbsorption, int newAbsorption, BlockPos pos)
	{
		long mapchunkindex2d = server.WorldMap.MapChunkIndex2D(pos.X / 32, pos.Z / 32);
		server.loadedMapChunks.TryGetValue(mapchunkindex2d, out var mapchunk);
		if (mapchunk == null)
		{
			return;
		}
		mapchunk.MarkFresh();
		lock (LightingTasksLock)
		{
			LightingTasks.Enqueue(new UpdateLightingTask
			{
				oldBlockId = 0,
				newBlockId = 0,
				oldAbsorb = (byte)oldAbsorption,
				newAbsorb = (byte)newAbsorption,
				pos = pos.Copy(),
				absorbUpdate = true
			});
		}
	}

	public override void UpdateLightingBulk(Dictionary<BlockPos, BlockUpdate> blockUpdates)
	{
		foreach (KeyValuePair<BlockPos, BlockUpdate> val in blockUpdates)
		{
			int newblockid = val.Value.NewFluidBlockId;
			if (newblockid < 0)
			{
				newblockid = val.Value.NewSolidBlockId;
			}
			if (newblockid >= 0)
			{
				UpdateLighting(val.Value.OldBlockId, newblockid, val.Key);
			}
		}
	}

	public float? GetMaxTimeAwareLightLevelAt(int posX, int posY, int posZ)
	{
		if (!IsValidPos(posX, posY, posZ))
		{
			return server.SunBrightness;
		}
		IWorldChunk chunk = GetChunkAtPos(posX, posY, posZ);
		if (chunk == null)
		{
			return null;
		}
		ushort lightBytes = chunk.Unpack_AndReadLight(ChunkSizedIndex3D(posX % 32, posY % 32, posZ % 32));
		float dayLightStrength = server.Calendar.GetDayLightStrength(posX, posZ);
		return Math.Max((float)(lightBytes & 0x1F) * dayLightStrength, (lightBytes >> 5) & 0x1F);
	}

	public override void PrintChunkMap(Vec2i markChunkPos = null)
	{
		SKBitmap bmp = new SKBitmap(server.WorldMap.ChunkMapSizeX, server.WorldMap.ChunkMapSizeZ);
		SKColor color = new SKColor(0, byte.MaxValue, 0, byte.MaxValue);
		server.loadedChunksLock.AcquireReadLock();
		try
		{
			foreach (long index3d in server.loadedChunks.Keys)
			{
				ChunkPos vec = server.WorldMap.ChunkPosFromChunkIndex3D(index3d);
				if (vec.Dimension <= 0)
				{
					bmp.SetPixel(vec.X, vec.Z, color);
				}
			}
		}
		finally
		{
			server.loadedChunksLock.ReleaseReadLock();
		}
		int i = 0;
		while (File.Exists("serverchunks" + i + ".png"))
		{
			i++;
		}
		if (markChunkPos != null)
		{
			bmp.SetPixel(markChunkPos.X, markChunkPos.Y, new SKColor(byte.MaxValue, 0, 0, byte.MaxValue));
		}
		bmp.Save("serverchunks" + i + ".png");
	}

	IWorldChunk IChunkProvider.GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		return GetServerChunk(chunkX, chunkY, chunkZ);
	}

	public override BlockEntity GetBlockEntity(BlockPos position)
	{
		return GetChunk(position)?.GetLocalBlockEntityAtBlockPos(position);
	}

	public override void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null)
	{
		WorldChunk chunk = GetChunk(position);
		if (chunk != null)
		{
			if (chunk.GetLocalBlockEntityAtBlockPos(position) != null)
			{
				RemoveBlockEntity(position);
			}
			Block block = chunk.GetLocalBlockAtBlockPos(server, position);
			BlockEntity be = ServerMain.ClassRegistry.CreateBlockEntity(classname);
			be.Pos = position.Copy();
			be.CreateBehaviors(block, server);
			be.Initialize(server.api);
			chunk.AddBlockEntity(be);
			be.OnBlockPlaced(byItemStack);
			chunk.MarkModified();
			MarkBlockEntityDirty(be.Pos);
		}
	}

	public override void SpawnBlockEntity(BlockEntity be)
	{
		WorldChunk chunk = GetChunk(be.Pos);
		if (chunk != null)
		{
			if (chunk.GetLocalBlockEntityAtBlockPos(be.Pos) != null)
			{
				RemoveBlockEntity(be.Pos);
			}
			chunk.AddBlockEntity(be);
			chunk.MarkModified();
			MarkBlockEntityDirty(be.Pos);
		}
	}

	public override void RemoveBlockEntity(BlockPos pos)
	{
		WorldChunk chunk = GetChunk(pos);
		if (chunk != null)
		{
			BlockEntity blockEntity = GetBlockEntity(pos);
			chunk.RemoveBlockEntity(pos);
			blockEntity?.OnBlockRemoved();
			chunk.MarkModified();
		}
	}

	public override void MarkBlockModified(BlockPos pos, bool doRelight = true)
	{
		if (doRelight)
		{
			server.ModifiedBlocks.Enqueue(pos);
		}
		else
		{
			server.ModifiedBlocksNoRelight.Enqueue(pos);
		}
	}

	public override void MarkBlockDirty(BlockPos pos, Action onRetesselated)
	{
		server.DirtyBlocks.Enqueue(new Vec4i(pos, -1));
	}

	public override void MarkBlockDirty(BlockPos pos, IPlayer skipPlayer = null)
	{
		server.DirtyBlocks.Enqueue(new Vec4i(pos, (skipPlayer == null) ? (-1) : (skipPlayer as ServerPlayer).ClientId));
	}

	public override void MarkBlockEntityDirty(BlockPos pos)
	{
		server.DirtyBlockEntities.Enqueue(pos.Copy());
		GetServerChunk(pos.X / 32, pos.InternalY / 32, pos.Z / 32)?.MarkModified();
	}

	public override void MarkDecorsDirty(BlockPos pos)
	{
		server.ModifiedDecors.Enqueue(pos.Copy());
	}

	public override void TriggerNeighbourBlockUpdate(BlockPos pos)
	{
		server.UpdatedBlocks.Enqueue(pos.Copy());
	}

	public override ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.NowValues, double totalDays = 0.0)
	{
		ClimateCondition outClimate = getWorldGenClimateAt(pos, mode >= EnumGetClimateMode.ForSuppliedDate_TemperatureOnly);
		if (outClimate == null)
		{
			if (mode != EnumGetClimateMode.ForSuppliedDate_TemperatureOnly)
			{
				return null;
			}
			return new ClimateCondition
			{
				Temperature = 4f,
				WorldGenTemperature = 4f
			};
		}
		if (mode == EnumGetClimateMode.NowValues)
		{
			totalDays = server.Calendar.TotalDays;
		}
		server.EventManager.TriggerOnGetClimate(ref outClimate, pos, mode, totalDays);
		return outClimate;
	}

	public override ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition baseClimate, EnumGetClimateMode mode, double totalDays)
	{
		baseClimate.Temperature = baseClimate.WorldGenTemperature;
		baseClimate.Rainfall = baseClimate.WorldgenRainfall;
		server.EventManager.TriggerOnGetClimate(ref baseClimate, pos, mode, totalDays);
		return baseClimate;
	}

	public override ClimateCondition GetClimateAt(BlockPos pos, int climate)
	{
		float temp = Climate.GetScaledAdjustedTemperatureFloat((climate >> 16) & 0xFF, pos.Y - server.seaLevel);
		float rain = (float)Climate.GetRainFall((climate >> 8) & 0xFF, pos.Y) / 255f;
		float heightRel = ((float)pos.Y - (float)server.seaLevel) / ((float)MapSizeY - (float)server.seaLevel);
		ClimateCondition outclimate = new ClimateCondition
		{
			Temperature = temp,
			Rainfall = rain,
			Fertility = (float)Climate.GetFertility((int)rain, temp, heightRel) / 255f
		};
		server.EventManager.TriggerOnGetClimate(ref outclimate, pos, EnumGetClimateMode.NowValues, server.Calendar.TotalDays);
		return outclimate;
	}

	public override Vec3d GetWindSpeedAt(BlockPos pos)
	{
		return GetWindSpeedAt(new Vec3d(pos.X, pos.Y, pos.Z));
	}

	public override Vec3d GetWindSpeedAt(Vec3d pos)
	{
		Vec3d windspeed = new Vec3d();
		server.EventManager.TriggerOnGetWindSpeed(pos, ref windspeed);
		return windspeed;
	}

	public ClimateCondition getWorldGenClimateAt(BlockPos pos, bool temperatureRainfallOnly)
	{
		if (!IsValidPos(pos))
		{
			return null;
		}
		IMapRegion mapregion = GetMapRegion(pos);
		if (mapregion?.ClimateMap?.Data == null || mapregion.ClimateMap.Size == 0)
		{
			return null;
		}
		float normXInRegionClimate = (float)((double)pos.X / (double)RegionSize % 1.0);
		float normZInRegionClimate = (float)((double)pos.Z / (double)RegionSize % 1.0);
		int climate = mapregion.ClimateMap.GetUnpaddedColorLerpedForNormalizedPos(normXInRegionClimate, normZInRegionClimate);
		float temp = Climate.GetScaledAdjustedTemperatureFloat((climate >> 16) & 0xFF, pos.Y - server.seaLevel);
		float rain = Climate.GetRainFall((climate >> 8) & 0xFF, pos.Y);
		int intRain = (int)rain;
		rain /= 255f;
		ClimateCondition conds = new ClimateCondition
		{
			Temperature = temp,
			Rainfall = rain,
			WorldgenRainfall = rain,
			WorldGenTemperature = temp
		};
		if (!temperatureRainfallOnly)
		{
			float heightRel = ((float)pos.Y - (float)server.seaLevel) / ((float)MapSizeY - (float)server.seaLevel);
			conds.Fertility = (float)Climate.GetFertilityFromUnscaledTemp(intRain, (climate >> 16) & 0xFF, heightRel) / 255f;
			conds.GeologicActivity = (float)(climate & 0xFF) / 255f;
			AddWorldGenForestShrub(conds, mapregion, pos);
		}
		return conds;
	}

	public void AddWorldGenForestShrub(ClimateCondition conds, IMapRegion mapregion, BlockPos pos)
	{
		float normX = (float)((double)pos.X / (double)RegionSize % 1.0);
		float normZ = (float)((double)pos.Z / (double)RegionSize % 1.0);
		int forest = mapregion.ForestMap.GetUnpaddedColorLerpedForNormalizedPos(normX, normZ);
		conds.ForestDensity = (float)forest / 255f;
		int shrub = mapregion.ShrubMap.GetUnpaddedColorLerpedForNormalizedPos(normX, normZ) & 0xFF;
		conds.ShrubDensity = (float)shrub / 255f;
	}

	public long ChunkIndex3dToIndex2d(long index3d)
	{
		long chunkX = index3d % index3dMulX;
		return index3d / index3dMulX % index3dMulZ * base.ChunkMapSizeX + chunkX;
	}

	public override void DamageBlock(BlockPos pos, BlockFacing facing, float damage, IPlayer dualCallByPlayer = null)
	{
		Packet_Server packet = new Packet_Server
		{
			Id = 64,
			BlockDamage = new Packet_BlockDamage
			{
				PosX = pos.X,
				PosY = pos.Y,
				PosZ = pos.Z,
				Damage = CollectibleNet.SerializeFloat(damage),
				Facing = facing.Index
			}
		};
		foreach (ConnectedClient client in server.Clients.Values)
		{
			if (client.ShouldReceiveUpdatesForPos(pos) && (dualCallByPlayer == null || client.Id != dualCallByPlayer.ClientId))
			{
				server.SendPacket(client.Id, packet);
			}
		}
	}

	public void Add(LandClaim claim)
	{
		HashSet<long> regions = new HashSet<long>();
		int regionSize = server.WorldMap.RegionSize;
		foreach (Cuboidi area in claim.Areas)
		{
			int minx = area.MinX / regionSize;
			int maxx = area.MaxX / regionSize;
			int minz = area.MinZ / regionSize;
			int maxz = area.MaxZ / regionSize;
			for (int x = minx; x <= maxx; x++)
			{
				for (int z = minz; z <= maxz; z++)
				{
					regions.Add(server.WorldMap.MapRegionIndex2D(x, z));
				}
			}
		}
		foreach (long index2d in regions)
		{
			if (!LandClaimByRegion.TryGetValue(index2d, out var claims))
			{
				claims = (LandClaimByRegion[index2d] = new List<LandClaim>());
			}
			claims.Add(claim);
		}
		server.SaveGameData.LandClaims.Add(claim);
		BroadcastClaims(null, new LandClaim[1] { claim });
	}

	public bool Remove(LandClaim claim)
	{
		foreach (KeyValuePair<long, List<LandClaim>> item in LandClaimByRegion)
		{
			item.Value.Remove(claim);
		}
		bool num = server.SaveGameData.LandClaims.Remove(claim);
		if (num)
		{
			BroadcastClaims(server.SaveGameData.LandClaims, null);
		}
		return num;
	}

	public void UpdateClaim(LandClaim oldClaim, LandClaim newClaim)
	{
		Remove(oldClaim);
		Add(newClaim);
		BroadcastClaims(server.SaveGameData.LandClaims, null);
	}

	public void BroadcastClaims(IEnumerable<LandClaim> allClaims, IEnumerable<LandClaim> addClaims)
	{
		Packet_LandClaims landClaims = new Packet_LandClaims();
		if (allClaims != null)
		{
			landClaims.SetAllclaims(allClaims.Select(delegate(LandClaim claim)
			{
				Packet_LandClaim packet_LandClaim2 = new Packet_LandClaim();
				packet_LandClaim2.SetData(SerializerUtil.Serialize(claim));
				return packet_LandClaim2;
			}).ToArray());
		}
		if (addClaims != null)
		{
			landClaims.SetAddclaims(addClaims.Select(delegate(LandClaim claim)
			{
				Packet_LandClaim packet_LandClaim = new Packet_LandClaim();
				packet_LandClaim.SetData(SerializerUtil.Serialize(claim));
				return packet_LandClaim;
			}).ToArray());
		}
		server.BroadcastPacket(new Packet_Server
		{
			Id = 75,
			LandClaims = landClaims
		});
	}

	public void SendClaims(IServerPlayer player, IEnumerable<LandClaim> allClaims, IEnumerable<LandClaim> addClaims)
	{
		Packet_LandClaims landClaims = new Packet_LandClaims();
		if (allClaims != null)
		{
			landClaims.SetAllclaims(allClaims.Select(delegate(LandClaim claim)
			{
				Packet_LandClaim packet_LandClaim2 = new Packet_LandClaim();
				packet_LandClaim2.SetData(SerializerUtil.Serialize(claim));
				return packet_LandClaim2;
			}).ToArray());
		}
		if (addClaims != null)
		{
			landClaims.SetAddclaims(addClaims.Select(delegate(LandClaim claim)
			{
				Packet_LandClaim packet_LandClaim = new Packet_LandClaim();
				packet_LandClaim.SetData(SerializerUtil.Serialize(claim));
				return packet_LandClaim;
			}).ToArray());
		}
		server.SendPacket(player, new Packet_Server
		{
			Id = 75,
			LandClaims = landClaims
		});
	}
}
