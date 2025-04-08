using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public sealed class ClientWorldMap : WorldMap, IChunkProvider, ILandClaimAPI
{
	private ClientMain game;

	private ClientChunk EmptyChunk;

	internal ChunkIlluminator chunkIlluminator;

	internal ClientChunkDataPool chunkDataPool;

	public int ClientChunkSize;

	public int ServerChunkSize;

	public int MapChunkSize;

	public int regionSize;

	public int MaxViewDistance;

	internal ConcurrentDictionary<long, ClientMapRegion> MapRegions = new ConcurrentDictionary<long, ClientMapRegion>();

	internal Dictionary<long, ClientMapChunk> MapChunks = new Dictionary<long, ClientMapChunk>();

	internal object chunksLock = new object();

	internal Dictionary<long, ClientChunk> chunks = new Dictionary<long, ClientChunk>();

	internal Dictionary<int, IMiniDimension> dimensions = new Dictionary<int, IMiniDimension>();

	private Vec3i mapsize = new Vec3i();

	public List<LandClaim> LandClaims = new List<LandClaim>();

	public Dictionary<string, PlayerRole> RolesByCode = new Dictionary<string, PlayerRole>();

	private int prevChunkX = -1;

	private int prevChunkY = -1;

	private int prevChunkZ = -1;

	private IWorldChunk prevChunk;

	private object LerpedClimateMapsLock = new object();

	private LimitedDictionary<long, int[]> LerpedClimateMaps = new LimitedDictionary<long, int[]>(10);

	public IBlockAccessor RelaxedBlockAccess;

	public IBlockAccessor CachingBlockAccess;

	public IBulkBlockAccessor BulkBlockAccess;

	public IBlockAccessor NoRelightBulkBlockAccess;

	public IBlockAccessor BulkMinimalBlockAccess;

	public object LightingTasksLock = new object();

	public Queue<UpdateLightingTask> LightingTasks = new Queue<UpdateLightingTask>();

	private bool lightsGo;

	private bool blockTexturesGo;

	private int regionMapSizeX;

	private int regionMapSizeY;

	private int regionMapSizeZ;

	private int[] placeHolderClimateMap;

	public static int seaLevel = 110;

	public override Vec3i MapSize => mapsize;

	ILogger IChunkProvider.Logger => ScreenManager.Platform.Logger;

	public override ILogger Logger => ScreenManager.Platform.Logger;

	public override int ChunkSize => ClientChunkSize;

	public override int ChunkSizeMask => ClientChunkSize - 1;

	public int MapRegionSizeInChunks => RegionSize / ServerChunkSize;

	public override int MapSizeX => mapsize.X;

	public override int MapSizeY => mapsize.Y;

	public override int MapSizeZ => mapsize.Z;

	internal int MapChunkMapSizeX => mapsize.X / ServerChunkSize;

	internal int MapChunkMapSizeY => mapsize.Y / ServerChunkSize;

	internal int MapChunkMapSizeZ => mapsize.Z / ServerChunkSize;

	public override int RegionMapSizeX => regionMapSizeX;

	public override int RegionMapSizeY => regionMapSizeY;

	public override int RegionMapSizeZ => regionMapSizeZ;

	public override IList<Block> Blocks => game.Blocks;

	public override Dictionary<AssetLocation, Block> BlocksByCode => game.BlocksByCode;

	public override IWorldAccessor World => game;

	public override int RegionSize => regionSize;

	public override List<LandClaim> All => LandClaims;

	public override bool DebugClaimPrivileges => false;

	public ClientWorldMap(ClientMain game)
	{
		this.game = game;
		ClientChunkSize = 32;
		chunkDataPool = new ClientChunkDataPool(ClientChunkSize, game);
		game.RegisterGameTickListener(delegate
		{
			chunkDataPool.SlowDispose();
		}, 1033);
		ClientSettings.Inst.AddWatcher<int>("optimizeRamMode", updateChunkDataPoolTresholds);
		updateChunkDataPoolTresholds(ClientSettings.OptimizeRamMode);
		chunkIlluminator = new ChunkIlluminator(this, new BlockAccessorRelaxed(this, game, synchronize: false, relight: false), ClientChunkSize);
		RelaxedBlockAccess = new BlockAccessorRelaxed(this, game, synchronize: false, relight: true);
		CachingBlockAccess = new BlockAccessorCaching(this, game, synchronize: false, relight: true);
		BulkBlockAccess = new BlockAccessorRelaxedBulkUpdate(this, game, synchronize: false, relight: true, debug: false);
		NoRelightBulkBlockAccess = new BlockAccessorRelaxedBulkUpdate(this, game, synchronize: false, relight: false, debug: false);
		BulkMinimalBlockAccess = new BlockAccessorBulkMinimalUpdate(this, game, synchronize: false, debug: false);
	}

	private void updateChunkDataPoolTresholds(int optimizerammode)
	{
		switch (optimizerammode)
		{
		case 2:
			chunkDataPool.CacheSize = 1500;
			chunkDataPool.SlowDisposeThreshold = 1000;
			break;
		case 1:
			chunkDataPool.CacheSize = 2000;
			chunkDataPool.SlowDisposeThreshold = 1350;
			break;
		default:
			chunkDataPool.CacheSize = 5000;
			chunkDataPool.SlowDisposeThreshold = 3500;
			break;
		}
	}

	private void switchRedAndBlueChannels(int[] pixels)
	{
		for (int i = 0; i < pixels.Length; i++)
		{
			int color = pixels[i];
			int r = color & 0xFF;
			int b = (color >> 16) & 0xFF;
			pixels[i] = (int)(color & 0xFF00FF00u) | (r << 16) | b;
		}
	}

	public void OnLightLevelsReceived()
	{
		if (blockTexturesGo)
		{
			OnBlocksAndLightLevelsReceived();
		}
		else
		{
			lightsGo = true;
		}
	}

	public void OnBlocksAndLightLevelsReceived()
	{
		EmptyChunk = ClientChunk.CreateNew(chunkDataPool);
		ushort sunLit = (ushort)SunBrightness;
		EmptyChunk.Lighting.FloodWithSunlight(sunLit);
		chunkIlluminator.InitForWorld(game.Blocks, sunLit, MapSizeX, MapSizeY, MapSizeZ);
		EmptyChunk.Empty = true;
	}

	public void BlockTexturesLoaded()
	{
		PopulateColorMaps();
		if (lightsGo)
		{
			OnBlocksAndLightLevelsReceived();
		}
		else
		{
			blockTexturesGo = true;
		}
	}

	public int LoadColorMaps()
	{
		int rectid = 0;
		foreach (KeyValuePair<string, ColorMap> val in game.ColorMaps)
		{
			if (game.disposed)
			{
				return rectid;
			}
			ColorMap cmap = val.Value;
			if (cmap.Texture?.Base == null)
			{
				game.Logger.Warning("Incorrect texture definition for color map entry {0}", game.ColorMaps.IndexOfKey(val.Key));
				cmap.LoadIntoBlockTextureAtlas = false;
				continue;
			}
			AssetLocationAndSource loc = new AssetLocationAndSource(cmap.Texture.Base.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
			loc.AddToAllAtlasses = true;
			BitmapRef map = game.Platform.CreateBitmapFromPng(game.AssetManager.Get(loc));
			if (game.disposed)
			{
				return rectid;
			}
			if (cmap.LoadIntoBlockTextureAtlas)
			{
				cmap.BlockAtlasTextureSubId = game.BlockAtlasManager.GetOrAddTextureLocation(loc);
				loc.loadedAlready = 2;
				cmap.RectIndex = rectid + (cmap.ExtraFlags << 6);
				rectid++;
			}
			if (game.disposed)
			{
				return rectid;
			}
			cmap.Pixels = map.Pixels;
			cmap.OuterSize = new Size2i(map.Width, map.Height);
			switchRedAndBlueChannels(cmap.Pixels);
		}
		return rectid;
	}

	public void PopulateColorMaps()
	{
		float[] mapRects = (game.shUniforms.ColorMapRects4 = new float[160]);
		int i = 0;
		foreach (KeyValuePair<string, ColorMap> colorMap in game.ColorMaps)
		{
			ColorMap cmap = colorMap.Value;
			if (cmap.LoadIntoBlockTextureAtlas)
			{
				float padx = (float)cmap.Padding / (float)game.BlockAtlasManager.Size.Width;
				float pady = (float)cmap.Padding / (float)game.BlockAtlasManager.Size.Height;
				TextureAtlasPosition texPos = game.BlockAtlasManager.Positions[cmap.BlockAtlasTextureSubId];
				mapRects[i++] = texPos.x1 + padx;
				mapRects[i++] = texPos.y1 + pady;
				mapRects[i++] = texPos.x2 - texPos.x1 - 2f * padx;
				mapRects[i++] = texPos.y2 - texPos.y1 - 2f * pady;
			}
		}
	}

	public long MapRegionIndex2DFromClientChunkCoord(int chunkX, int chunkZ)
	{
		chunkX *= ClientChunkSize;
		chunkZ *= ClientChunkSize;
		return MapRegionIndex2D(chunkX / ServerChunkSize / MapRegionSizeInChunks, chunkZ / ServerChunkSize / MapRegionSizeInChunks);
	}

	public int[] LoadOrCreateLerpedClimateMap(int chunkX, int chunkZ)
	{
		lock (LerpedClimateMapsLock)
		{
			long index2d = MapRegionIndex2DFromClientChunkCoord(chunkX, chunkZ);
			int[] lerpedClimateMap = LerpedClimateMaps[index2d];
			if (lerpedClimateMap == null)
			{
				_ = chunkX * ClientChunkSize / ServerChunkSize / MapRegionSizeInChunks;
				_ = chunkZ * ClientChunkSize / ServerChunkSize / MapRegionSizeInChunks;
				ClientMapRegion mapRegion = null;
				game.WorldMap.MapRegions.TryGetValue(index2d, out mapRegion);
				if (mapRegion == null || mapRegion.ClimateMap == null || mapRegion.ClimateMap.InnerSize <= 0)
				{
					if (placeHolderClimateMap == null)
					{
						placeHolderClimateMap = new int[RegionSize * RegionSize];
						placeHolderClimateMap.Fill(11842740);
					}
					return placeHolderClimateMap;
				}
				lerpedClimateMap = GameMath.BiLerpColorMap(mapRegion.ClimateMap, RegionSize / mapRegion.ClimateMap.InnerSize);
				LerpedClimateMaps[index2d] = lerpedClimateMap;
			}
			return lerpedClimateMap;
		}
	}

	public ColorMapData getColorMapData(Block block, int posX, int posY, int posZ)
	{
		int rndX = GameMath.MurmurHash3Mod(posX, 0, posZ, 3);
		int rndZ = GameMath.MurmurHash3Mod(posX, 1, posZ, 3);
		int climate = GetClimate(posX + rndX, posZ + rndZ);
		int temp = Climate.GetAdjustedTemperature((climate >> 16) & 0xFF, posY - seaLevel);
		int rain = Climate.GetRainFall((climate >> 8) & 0xFF, posY);
		int seasonMapIndex = 0;
		if (block.SeasonColorMap != null && game.ColorMaps.TryGetValue(block.SeasonColorMap, out var sval))
		{
			seasonMapIndex = sval.RectIndex + 1;
		}
		int climateMapIndex = 0;
		if (block.ClimateColorMap != null && game.ColorMaps.TryGetValue(block.ClimateColorMap, out var cval))
		{
			climateMapIndex = cval.RectIndex + 1;
		}
		return new ColorMapData((byte)seasonMapIndex, (byte)climateMapIndex, (byte)temp, (byte)rain, block.Frostable);
	}

	public int ApplyColorMapOnRgba(string climateColorMap, string seasonColorMap, int color, int posX, int posY, int posZ, bool flipRb)
	{
		ColorMap climateMap = ((climateColorMap == null) ? null : game.ColorMaps[climateColorMap]);
		ColorMap seasonMap = ((seasonColorMap == null) ? null : game.ColorMaps[seasonColorMap]);
		return ApplyColorMapOnRgba(climateMap, seasonMap, color, posX, posY, posZ, flipRb);
	}

	public int ApplyColorMapOnRgba(ColorMap climateMap, ColorMap seasonMap, int color, int posX, int posY, int posZ, bool flipRb)
	{
		int rndX = GameMath.MurmurHash3Mod(posX, 0, posZ, 3);
		int rndZ = GameMath.MurmurHash3Mod(posX, 1, posZ, 3);
		int climate = GetClimate(posX + rndX, posZ + rndZ);
		int temp = (climate >> 16) & 0xFF;
		int rain = Climate.GetRainFall((climate >> 8) & 0xFF, posY);
		EnumHemisphere hemi = game.Calendar.GetHemisphere(new BlockPos(posX, posY, posZ));
		return ApplyColorMapOnRgba(climateMap, seasonMap, color, rain, temp, flipRb, (float)GameMath.MurmurHash3Mod(posX, posY, posZ, 100) / 100f, (hemi == EnumHemisphere.South) ? 0.5f : 0f, posY - seaLevel);
	}

	public int ApplyColorMapOnRgba(string climateColorMap, string seasonColorMap, int color, int rain, int temp, bool flipRb, float seasonYPixelRel = 0f, float seasonXOffset = 0f)
	{
		ColorMap climateMap = ((climateColorMap == null) ? null : game.ColorMaps[climateColorMap]);
		ColorMap seasonMap = ((seasonColorMap == null) ? null : game.ColorMaps[seasonColorMap]);
		return ApplyColorMapOnRgba(climateMap, seasonMap, color, rain, temp, flipRb, seasonYPixelRel, seasonXOffset, 0);
	}

	public int ApplyColorMapOnRgba(ColorMap climateMap, ColorMap seasonMap, int color, int rain, int temp, bool flipRb, float seasonYPixelRel, float seasonXOffset, int heightAboveSealevel)
	{
		int tintColor = -1;
		if (climateMap != null)
		{
			float winner = climateMap.OuterSize.Width - 2 * climateMap.Padding;
			float hinner = climateMap.OuterSize.Height - 2 * climateMap.Padding;
			int x2 = (int)GameMath.Clamp((float)Climate.GetAdjustedTemperature(temp, heightAboveSealevel) / 255f * winner, -climateMap.Padding, climateMap.OuterSize.Width - 1);
			int y2 = (int)GameMath.Clamp((float)rain / 255f * hinner, -climateMap.Padding, climateMap.OuterSize.Height - 1);
			tintColor = climateMap.Pixels[(y2 + climateMap.Padding) * climateMap.OuterSize.Width + x2 + climateMap.Padding];
			if (flipRb)
			{
				int r2 = tintColor & 0xFF;
				int g2 = (tintColor >> 8) & 0xFF;
				int b2 = (tintColor >> 16) & 0xFF;
				tintColor = (((tintColor >> 24) & 0xFF) << 24) | (r2 << 16) | (g2 << 8) | b2;
			}
		}
		if (seasonMap != null)
		{
			int x = (int)(GameMath.Mod(game.Calendar.YearRel + seasonXOffset, 1f) * (float)(seasonMap.OuterSize.Width - seasonMap.Padding));
			int y = (int)(seasonYPixelRel * (float)seasonMap.OuterSize.Height);
			int seasonColor = seasonMap.Pixels[(y + seasonMap.Padding) * seasonMap.OuterSize.Width + x + seasonMap.Padding];
			if (flipRb)
			{
				int r = seasonColor & 0xFF;
				int g = (seasonColor >> 8) & 0xFF;
				int b = (seasonColor >> 16) & 0xFF;
				seasonColor = (((seasonColor >> 24) & 0xFF) << 24) | (r << 16) | (g << 8) | b;
			}
			float seasonWeight = GameMath.Clamp(0.5f - GameMath.Cos((float)temp / 42f) / 2.3f + (float)(Math.Max(0, 128 - temp) / 512) - (float)(Math.Max(0, temp - 130) / 200), 0f, 1f);
			tintColor = ColorUtil.ColorOverlay(tintColor, seasonColor, seasonWeight);
		}
		return ColorUtil.ColorMultiplyEach(color, tintColor);
	}

	public int GetClimate(int posX, int posZ)
	{
		if (posX < 0 || posZ < 0 || posX >= MapSizeX || posZ >= MapSizeZ)
		{
			return 0;
		}
		return LoadOrCreateLerpedClimateMap(posX / ClientChunkSize, posZ / ClientChunkSize)[posZ % RegionSize * RegionSize + posX % RegionSize];
	}

	public int GetClimateFast(int[] map, int inRegionX, int inRegionZ)
	{
		return map[inRegionZ * RegionSize + inRegionX];
	}

	internal ClientChunk GetChunkAtBlockPos(int posX, int posY, int posZ)
	{
		int x = posX >> 5;
		int cy = posY >> 5;
		int cz = posZ >> 5;
		long index3d = MapUtil.Index3dL(x, cy, cz, index3dMulX, index3dMulZ);
		ClientChunk chunk = null;
		lock (chunksLock)
		{
			chunks.TryGetValue(index3d, out chunk);
			return chunk;
		}
	}

	public override IWorldChunk GetChunk(long index3d)
	{
		ClientChunk chunk = null;
		lock (chunksLock)
		{
			chunks.TryGetValue(index3d, out chunk);
			return chunk;
		}
	}

	public override WorldChunk GetChunk(BlockPos pos)
	{
		return GetClientChunk(pos.X / ClientChunkSize, pos.InternalY / ClientChunkSize, pos.Z / ClientChunkSize);
	}

	internal ClientChunk GetClientChunkAtBlockPos(BlockPos pos)
	{
		return GetClientChunk(pos.X / ClientChunkSize, pos.InternalY / ClientChunkSize, pos.Z / ClientChunkSize);
	}

	internal ClientChunk GetClientChunk(int chunkX, int chunkY, int chunkZ)
	{
		long index3d = MapUtil.Index3dL(chunkX, chunkY, chunkZ, index3dMulX, index3dMulZ);
		ClientChunk chunk = null;
		lock (chunksLock)
		{
			chunks.TryGetValue(index3d, out chunk);
			return chunk;
		}
	}

	public override IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		long index3d = MapUtil.Index3dL(chunkX, chunkY, chunkZ, index3dMulX, index3dMulZ);
		ClientChunk chunk = null;
		lock (chunksLock)
		{
			chunks.TryGetValue(index3d, out chunk);
			return chunk;
		}
	}

	internal ClientChunk GetClientChunk(long index3d)
	{
		ClientChunk chunk = null;
		lock (chunksLock)
		{
			chunks.TryGetValue(index3d, out chunk);
			return chunk;
		}
	}

	internal void LoadChunkFromPacket(Packet_ServerChunk p)
	{
		int cx = p.X;
		int cy = p.Y;
		int cz = p.Z;
		byte[] blocks = p.Blocks;
		byte[] light = p.Light;
		byte[] lightSat = p.LightSat;
		byte[] liquid = p.Liquids;
		long chunkIndex3d = MapUtil.Index3dL(cx, cy, cz, index3dMulX, index3dMulZ);
		ClientChunk chunk = null;
		try
		{
			chunk = ClientChunk.CreateNewCompressed(chunkDataPool, blocks, light, lightSat, liquid, p.Moddata, p.Compver);
			chunk.Empty = p.Empty > 0;
			chunk.clientmapchunk = GetMapChunk(cx, cz) as ClientMapChunk;
			chunk.LightPositions = new HashSet<int>();
			for (int j = 0; j < p.LightPositionsCount; j++)
			{
				chunk.LightPositions.Add(p.LightPositions[j]);
			}
		}
		catch (Exception e)
		{
			game.Logger.Error("Unable to load client chunk at chunk coordinates {0},{1},{2}. Will ignore and replace with empty chunk. Thrown exception: {3}", cx, cy, cz, e.ToString());
			chunk = ClientChunk.CreateNew(chunkDataPool);
		}
		chunk.PreLoadBlockEntitiesFromPacket(p.BlockEntities, p.BlockEntitiesCount, game);
		if (p.DecorsPos != null && p.DecorsIds != null)
		{
			if (p.DecorsIdsCount < p.DecorsPosCount)
			{
				p.DecorsPosCount = p.DecorsIdsCount;
			}
			chunk.Decors = new Dictionary<int, Block>(p.DecorsPosCount);
			for (int i = 0; i < p.DecorsPosCount; i++)
			{
				chunk.Decors[p.DecorsPos[i]] = game.GetBlock(p.DecorsIds[i]);
			}
		}
		game.EnqueueMainThreadTask(delegate
		{
			bool flag = false;
			lock (chunksLock)
			{
				flag = chunks.ContainsKey(chunkIndex3d);
			}
			if (flag)
			{
				OverloadChunkMT(p, chunk);
			}
			else
			{
				loadChunkMT(p, chunk);
			}
		}, "loadchunk");
	}

	private void loadChunkMT(Packet_ServerChunk p, ClientChunk chunk)
	{
		int cx = p.X;
		int cy = p.Y;
		int cz = p.Z;
		long chunkIndex3d = MapUtil.Index3dL(cx, cy, cz, index3dMulX, index3dMulZ);
		lock (chunksLock)
		{
			chunks[chunkIndex3d] = chunk;
		}
		chunk.InitBlockEntitiesFromPacket(game);
		chunk.LoadEntitiesFromPacket(p.Entities, p.EntitiesCount, game);
		chunk.loadedFromServer = true;
		Vec3d pos = game.player?.Entity?.Pos?.XYZ;
		bool priority = pos != null && pos.HorizontalSquareDistanceTo(cx * 32, cz * 32) < 4096f;
		MarkChunkDirty(cx, cy, cz, priority, sunRelight: false, null, fireEvent: false);
		if (cy / 1024 == 1)
		{
			GetOrCreateDimension(cx, cy, cz).ReceiveClientChunk(chunkIndex3d, chunk, World);
		}
		else
		{
			SetChunksAroundDirty(cx, cy, cz);
		}
		Vec3i vec = new Vec3i(cx, cy, cz);
		game.api.eventapi.TriggerChunkDirty(vec, chunk, EnumChunkDirtyReason.NewlyLoaded);
		game.eventManager?.TriggerChunkLoaded(vec);
	}

	public IMiniDimension GetOrCreateDimension(int subDimensionId, Vec3d pos)
	{
		if (!dimensions.TryGetValue(subDimensionId, out var dim))
		{
			dim = new BlockAccessorMovable((BlockAccessorBase)World.BlockAccessor, pos);
			dimensions[subDimensionId] = dim;
			dim.SetSubDimensionId(subDimensionId);
		}
		return dim;
	}

	public IMiniDimension GetOrCreateDimension(int cx, int cy, int cz)
	{
		int subDimensionId = BlockAccessorMovable.CalcSubDimensionId(cx, cz);
		return GetOrCreateDimension(subDimensionId, new Vec3d(cx * 32 % 16384, cy % 1024 * 32, cz * 32 % 16384));
	}

	private void OverloadChunkMT(Packet_ServerChunk p, ClientChunk newchunk)
	{
		int cx = p.X;
		int cy = p.Y;
		int cz = p.Z;
		long chunkIndex3d = MapUtil.Index3dL(cx, cy, cz, index3dMulX, index3dMulZ);
		ClientChunk prevchunk;
		lock (chunksLock)
		{
			chunks.TryGetValue(chunkIndex3d, out prevchunk);
		}
		if (prevchunk == null)
		{
			loadChunkMT(p, newchunk);
			return;
		}
		prevchunk.loadedFromServer = false;
		if (game.Platform.EllapsedMs - prevchunk.lastTesselationMs < 500)
		{
			game.EnqueueMainThreadTask(delegate
			{
				OverloadChunkMT(p, newchunk);
			}, "overloadchunkrequeue");
			return;
		}
		lock (chunksLock)
		{
			prevchunk.RemoveDataPoolLocations(game.chunkRenderer);
			for (int i = 0; i < prevchunk.EntitiesCount; i++)
			{
				Entity entity = prevchunk.Entities[i];
				if (entity != null && game.EntityPlayer.EntityId != entity.EntityId)
				{
					game.LoadedEntities.Remove(entity.EntityId);
					game.RemoveEntityRenderer(entity);
					entity.OnEntityDespawn(new EntityDespawnData
					{
						Reason = EnumDespawnReason.Unload
					});
					game.eventManager?.TriggerEntityDespawn(entity, new EntityDespawnData
					{
						Reason = EnumDespawnReason.Unload
					});
				}
			}
			foreach (KeyValuePair<BlockPos, BlockEntity> blockEntity in prevchunk.BlockEntities)
			{
				blockEntity.Value?.OnBlockUnloaded();
			}
			chunks[chunkIndex3d] = newchunk;
			newchunk.LoadEntitiesFromPacket(p.Entities, p.EntitiesCount, game);
			newchunk.InitBlockEntitiesFromPacket(game);
			newchunk.loadedFromServer = true;
			newchunk.quantityOverloads++;
		}
		if (!game.IsPaused)
		{
			game.RegisterCallback(delegate
			{
				prevchunk.TryPackAndCommit();
			}, 5000);
		}
		game.eventManager?.TriggerChunkLoaded(new Vec3i(cx, cy, cz));
		if (cy / 1024 == 1)
		{
			GetOrCreateDimension(cx, cy, cz).ReceiveClientChunk(chunkIndex3d, newchunk, World);
		}
		MarkChunkDirty(cx, cy, cz);
		SetChunksAroundDirty(cx, cy, cz);
	}

	internal void GetNeighbouringChunks(ClientChunk[] neibchunks, int chunkX, int chunkY, int chunkZ)
	{
		lock (chunksLock)
		{
			int index = 0;
			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dy = -1; dy <= 1; dy++)
				{
					for (int dz = -1; dz <= 1; dz++)
					{
						long chunkIndex3d = MapUtil.Index3dL(chunkX + dx, chunkY + dy, chunkZ + dz, index3dMulX, index3dMulZ);
						ClientChunk chunk = null;
						chunks.TryGetValue(chunkIndex3d, out chunk);
						if (chunk == null || chunk.Empty)
						{
							chunk = EmptyChunk;
						}
						if (!chunk.ChunkHasData())
						{
							throw new Exception($"GEC: Chunk {chunkX + dx} {chunkY + dy} {chunkZ + dz} has no more block data.");
						}
						neibchunks[index++] = chunk;
					}
				}
			}
		}
	}

	public void SetChunkDirty(long index3d, bool priority = false, bool relight = false, bool edgeOnly = false)
	{
		ClientChunk chunk = null;
		lock (chunksLock)
		{
			chunks.TryGetValue(index3d, out chunk);
		}
		if (chunk == null)
		{
			return;
		}
		chunk.shouldSunRelight |= relight;
		if (!relight)
		{
			chunk.FinishLightDoubleBuffering();
		}
		if (priority)
		{
			lock (game.dirtyChunksPriorityLock)
			{
				if (edgeOnly)
				{
					if (!game.dirtyChunksPriority.Contains(index3d))
					{
						game.dirtyChunksPriority.Enqueue(index3d | long.MinValue);
					}
				}
				else
				{
					game.dirtyChunksPriority.Enqueue(index3d);
				}
				return;
			}
		}
		lock (game.dirtyChunksLock)
		{
			if (edgeOnly)
			{
				if (!game.dirtyChunks.Contains(index3d))
				{
					game.dirtyChunks.Enqueue(index3d | long.MinValue);
				}
			}
			else
			{
				game.dirtyChunks.Enqueue(index3d);
			}
		}
	}

	public override void MarkChunkDirty(int cx, int cy, int cz, bool priority = false, bool sunRelight = false, Action OnRetesselated = null, bool fireEvent = true, bool edgeOnly = false)
	{
		if (!IsValidChunkPos(cx, cy, cz))
		{
			return;
		}
		long index3d = MapUtil.Index3dL(cx, cy, cz, index3dMulX, index3dMulZ);
		ClientChunk chunk = null;
		lock (chunksLock)
		{
			chunks.TryGetValue(index3d, out chunk);
		}
		if (chunk == null)
		{
			return;
		}
		int qDrawn = chunk.quantityDrawn;
		if (chunk.enquedForRedraw)
		{
			if (OnRetesselated != null)
			{
				game.eventManager?.RegisterOnChunkRetesselated(new Vec3i(cx, cy, cz), qDrawn, OnRetesselated);
			}
			if (fireEvent)
			{
				game.api.eventapi.TriggerChunkDirty(new Vec3i(cx, cy, cz), chunk, EnumChunkDirtyReason.MarkedDirty);
			}
			return;
		}
		chunk.shouldSunRelight = sunRelight;
		if (fireEvent)
		{
			game.api.eventapi.TriggerChunkDirty(new Vec3i(cx, cy, cz), chunk, EnumChunkDirtyReason.MarkedDirty);
		}
		int dist = Math.Max(Math.Abs(cx - game.player.Entity.Pos.XInt / 32), Math.Abs(cz - game.player.Entity.Pos.ZInt / 32));
		if ((priority && dist <= 2) || cy / 1024 == 1)
		{
			lock (game.dirtyChunksPriorityLock)
			{
				if (edgeOnly)
				{
					if (!game.dirtyChunksPriority.Contains(index3d))
					{
						game.dirtyChunksPriority.Enqueue(index3d | long.MinValue);
					}
				}
				else
				{
					game.dirtyChunksPriority.Enqueue(index3d);
					chunk.enquedForRedraw = true;
				}
				if (OnRetesselated != null)
				{
					game.eventManager?.RegisterOnChunkRetesselated(new Vec3i(cx, cy, cz), chunk.quantityDrawn, OnRetesselated);
				}
				return;
			}
		}
		lock (game.dirtyChunksLock)
		{
			if (edgeOnly)
			{
				if (!game.dirtyChunks.Contains(index3d))
				{
					game.dirtyChunks.Enqueue(index3d | long.MinValue);
				}
			}
			else
			{
				game.dirtyChunks.Enqueue(index3d);
				chunk.enquedForRedraw = true;
			}
			if (OnRetesselated != null)
			{
				game.eventManager?.RegisterOnChunkRetesselated(new Vec3i(cx, cy, cz), chunk.quantityDrawn, OnRetesselated);
			}
		}
	}

	public void SetChunksAroundDirty(int cx, int cy, int cz)
	{
		if (IsValidChunkPos(cx, cy, cz))
		{
			MarkChunkDirty_OnNeighbourChunkLoad(cx, cy, cz);
		}
		if (IsValidChunkPos(cx - 1, cy, cz))
		{
			MarkChunkDirty_OnNeighbourChunkLoad(cx - 1, cy, cz);
		}
		if (IsValidChunkPos(cx + 1, cy, cz))
		{
			MarkChunkDirty_OnNeighbourChunkLoad(cx + 1, cy, cz);
		}
		if (BlockAccessorMovable.ChunkCoordsInSameDimension(cy, cy - 1) && IsValidChunkPos(cx, cy - 1, cz))
		{
			MarkChunkDirty_OnNeighbourChunkLoad(cx, cy - 1, cz);
		}
		if (BlockAccessorMovable.ChunkCoordsInSameDimension(cy, cy + 1) && IsValidChunkPos(cx, cy + 1, cz))
		{
			MarkChunkDirty_OnNeighbourChunkLoad(cx, cy + 1, cz);
		}
		if (IsValidChunkPos(cx, cy, cz - 1))
		{
			MarkChunkDirty_OnNeighbourChunkLoad(cx, cy, cz - 1);
		}
		if (IsValidChunkPos(cx, cy, cz + 1))
		{
			MarkChunkDirty_OnNeighbourChunkLoad(cx, cy, cz + 1);
		}
	}

	private void MarkChunkDirty_OnNeighbourChunkLoad(int cx, int cy, int cz)
	{
		MarkChunkDirty(cx, cy, cz, priority: false, sunRelight: false, null, fireEvent: false, edgeOnly: true);
	}

	public bool IsValidChunkPosFast(int chunkX, int chunkY, int chunkZ)
	{
		if (chunkX >= 0 && chunkY >= 0 && chunkZ >= 0 && chunkX < base.ChunkMapSizeX && chunkY < chunkMapSizeY)
		{
			return chunkZ < base.ChunkMapSizeZ;
		}
		return false;
	}

	public bool IsChunkRendered(int cx, int cy, int cz)
	{
		ClientChunk chunk = null;
		lock (chunksLock)
		{
			chunks.TryGetValue(MapUtil.Index3dL(cx, cy, cz, index3dMulX, index3dMulZ), out chunk);
		}
		if (chunk != null)
		{
			return chunk.quantityDrawn > 0;
		}
		return false;
	}

	public int UncheckedGetBlockId(int x, int y, int z)
	{
		ClientChunk chunk = GetChunkAtBlockPos(x, y, z);
		if (chunk != null)
		{
			int pos = MapUtil.Index3d(x & 0x1F, y & 0x1F, z & 0x1F, 32, 32);
			chunk.Unpack();
			return chunk.Data[pos];
		}
		return 0;
	}

	IWorldChunk IChunkProvider.GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		ClientChunk chunk = GetClientChunk(chunkX, chunkY, chunkZ);
		chunk?.Unpack();
		return chunk;
	}

	IWorldChunk IChunkProvider.GetUnpackedChunkFast(int chunkX, int chunkY, int chunkZ, bool notRecentlyAccessed)
	{
		ClientChunk chunk = null;
		lock (chunksLock)
		{
			if (chunkX == prevChunkX && chunkY == prevChunkY && chunkZ == prevChunkZ)
			{
				if (notRecentlyAccessed && prevChunk != null)
				{
					prevChunk.Unpack();
				}
				return prevChunk;
			}
			prevChunkX = chunkX;
			prevChunkY = chunkY;
			prevChunkZ = chunkZ;
			long index3d = MapUtil.Index3dL(chunkX, chunkY, chunkZ, index3dMulX, index3dMulZ);
			chunks.TryGetValue(index3d, out chunk);
			prevChunk = chunk;
		}
		chunk?.Unpack();
		return chunk;
	}

	public override IWorldChunk GetChunkNonLocking(int chunkX, int chunkY, int chunkZ)
	{
		long index3d = MapUtil.Index3dL(chunkX, chunkY, chunkZ, index3dMulX, index3dMulZ);
		ClientChunk chunk = null;
		chunks.TryGetValue(index3d, out chunk);
		return chunk;
	}

	public void OnMapSizeReceived(Vec3i mapSize, Vec3i regionMapSize)
	{
		mapsize = new Vec3i(mapSize.X, mapSize.Y, mapSize.Z);
		chunks = new Dictionary<long, ClientChunk>();
		chunkMapSizeY = mapSize.Y / 32;
		index3dMulX = 2097152;
		index3dMulZ = 2097152;
		regionMapSizeX = regionMapSize.X;
		regionMapSizeY = regionMapSize.Y;
		regionMapSizeZ = regionMapSize.Z;
	}

	public override IWorldChunk GetChunkAtPos(int posX, int posY, int posZ)
	{
		return GetChunkAtBlockPos(posX, posY, posZ);
	}

	public override void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null)
	{
		ClientChunk chunk = (ClientChunk)GetChunk(position);
		if (chunk != null)
		{
			Block block = chunk.GetLocalBlockAtBlockPos(game, position);
			BlockEntity entity = ClientMain.ClassRegistry.CreateBlockEntity(classname);
			entity.Pos = position.Copy();
			entity.CreateBehaviors(block, game);
			entity.Initialize(game.api);
			chunk.AddBlockEntity(entity);
			entity.OnBlockPlaced(byItemStack);
			chunk.MarkModified();
			MarkBlockEntityDirty(entity.Pos);
		}
	}

	public override void SpawnBlockEntity(BlockEntity be)
	{
		ClientChunk chunk = GetChunkAtBlockPos(be.Pos.X, be.Pos.Y, be.Pos.Z);
		if (chunk != null)
		{
			chunk.AddBlockEntity(be);
			chunk.MarkModified();
			MarkBlockEntityDirty(be.Pos);
		}
	}

	public override void RemoveBlockEntity(BlockPos position)
	{
		ClientChunk chunk = GetClientChunkAtBlockPos(position);
		if (chunk != null)
		{
			GetBlockEntity(position)?.OnBlockRemoved();
			chunk.RemoveBlockEntity(position);
		}
	}

	public override BlockEntity GetBlockEntity(BlockPos position)
	{
		return GetClientChunkAtBlockPos(position)?.GetLocalBlockEntityAtBlockPos(position);
	}

	public override void SendSetBlock(int blockId, int posX, int posY, int posZ)
	{
	}

	public override void SendExchangeBlock(int blockId, int posX, int posY, int posZ)
	{
	}

	public override void UpdateLighting(int oldblockid, int newblockid, BlockPos pos)
	{
		lock (LightingTasksLock)
		{
			LightingTasks.Enqueue(new UpdateLightingTask
			{
				oldBlockId = oldblockid,
				newBlockId = newblockid,
				pos = pos
			});
		}
		game.eventManager?.TriggerLightingUpdate(oldblockid, newblockid, pos);
	}

	public override void RemoveBlockLight(byte[] oldLightHsV, BlockPos pos)
	{
		lock (LightingTasksLock)
		{
			LightingTasks.Enqueue(new UpdateLightingTask
			{
				removeLightHsv = oldLightHsV,
				pos = pos
			});
		}
		game.eventManager?.TriggerLightingUpdate(0, 0, pos);
	}

	public override void UpdateLightingAfterAbsorptionChange(int oldAbsorption, int newAbsorption, BlockPos pos)
	{
		lock (LightingTasksLock)
		{
			LightingTasks.Enqueue(new UpdateLightingTask
			{
				oldBlockId = 0,
				newBlockId = 0,
				oldAbsorb = (byte)oldAbsorption,
				newAbsorb = (byte)newAbsorption,
				pos = pos,
				absorbUpdate = true
			});
		}
		game.eventManager?.TriggerLightingUpdate(0, 0, pos);
	}

	public override void UpdateLightingBulk(Dictionary<BlockPos, BlockUpdate> blockUpdates)
	{
		game.ShouldTesselateTerrain = false;
		lock (LightingTasksLock)
		{
			foreach (KeyValuePair<BlockPos, BlockUpdate> val in blockUpdates)
			{
				int id = ((val.Value.NewFluidBlockId >= 0) ? val.Value.NewFluidBlockId : val.Value.NewSolidBlockId);
				if (id >= 0)
				{
					LightingTasks.Enqueue(new UpdateLightingTask
					{
						oldBlockId = val.Value.OldBlockId,
						newBlockId = id,
						pos = val.Key
					});
				}
			}
		}
		game.ShouldTesselateTerrain = true;
		game.eventManager?.TriggerLightingUpdate(0, 0, null, blockUpdates);
	}

	public override void SendBlockUpdateBulk(IEnumerable<BlockPos> blockUpdates, bool doRelight)
	{
	}

	public override void SendBlockUpdateBulkMinimal(Dictionary<BlockPos, BlockUpdate> blockUpdates)
	{
	}

	public override void MarkBlockDirty(BlockPos pos, IPlayer skipPlayer = null)
	{
		game.eventManager?.TriggerBlockChanged(game, pos, null);
		MarkChunkDirty(pos.X / ClientChunkSize, pos.InternalY / ClientChunkSize, pos.Z / ClientChunkSize, priority: true);
	}

	public override void MarkBlockModified(BlockPos pos, bool doRelight = true)
	{
		game.eventManager?.TriggerBlockChanged(game, pos, null);
		MarkChunkDirty(pos.X / ClientChunkSize, pos.InternalY / ClientChunkSize, pos.Z / ClientChunkSize, priority: true);
	}

	public override void MarkBlockDirty(BlockPos pos, Action OnRetesselated)
	{
		game.eventManager?.TriggerBlockChanged(game, pos, null);
		MarkChunkDirty(pos.X / ClientChunkSize, pos.InternalY / ClientChunkSize, pos.Z / ClientChunkSize, priority: true, sunRelight: false, OnRetesselated);
	}

	public override void MarkBlockEntityDirty(BlockPos pos)
	{
	}

	public override void TriggerNeighbourBlockUpdate(BlockPos pos)
	{
	}

	public override IMapRegion GetMapRegion(int regionX, int regionZ)
	{
		ClientMapRegion reg = null;
		MapRegions.TryGetValue(MapRegionIndex2D(regionX, regionZ), out reg);
		return reg;
	}

	public override IMapChunk GetMapChunk(int chunkX, int chunkZ)
	{
		ClientMapChunk mpc = null;
		long index2d = MapChunkIndex2D(chunkX, chunkZ);
		MapChunks.TryGetValue(index2d, out mpc);
		return mpc;
	}

	public void UnloadMapRegion(int regionX, int regionZ)
	{
		long regionIndex = MapRegionIndex2D(regionX, regionZ);
		if (MapRegions.TryGetValue(regionIndex, out var oldregion))
		{
			game.api.eventapi.TriggerMapregionUnloaded(new Vec2i(regionX, regionZ), oldregion);
			MapRegions.Remove(regionIndex);
		}
	}

	public override ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.WorldGenValues, double totalDays = 0.0)
	{
		int climate = GetClimate(pos.X, pos.Z);
		float heightRel = ((float)pos.Y - (float)seaLevel) / ((float)MapSizeY - (float)seaLevel);
		float temp = Climate.GetScaledAdjustedTemperatureFloatClient((climate >> 16) & 0xFF, pos.Y - seaLevel);
		float rain = Climate.GetRainFall((climate >> 8) & 0xFF, pos.Y);
		float fertility = (float)Climate.GetFertility((int)rain, temp, heightRel) / 255f;
		rain /= 255f;
		ClimateCondition outclimate = new ClimateCondition
		{
			Temperature = temp,
			Rainfall = rain,
			WorldgenRainfall = rain,
			WorldGenTemperature = temp,
			Fertility = fertility,
			GeologicActivity = (float)(climate & 0xFF) / 255f
		};
		if (mode == EnumGetClimateMode.NowValues)
		{
			totalDays = game.Calendar.TotalDays;
		}
		game.eventManager?.TriggerOnGetClimate(ref outclimate, pos, mode, totalDays);
		return outclimate;
	}

	public override ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition baseClimate, EnumGetClimateMode mode, double totalDays)
	{
		baseClimate.Rainfall = baseClimate.WorldgenRainfall;
		baseClimate.Temperature = baseClimate.WorldGenTemperature;
		game.eventManager?.TriggerOnGetClimate(ref baseClimate, pos, mode, totalDays);
		return baseClimate;
	}

	public override ClimateCondition GetClimateAt(BlockPos pos, int climate)
	{
		float temp = Climate.GetScaledAdjustedTemperatureFloatClient((climate >> 16) & 0xFF, pos.Y - seaLevel);
		float rain = Climate.GetRainFall((climate >> 8) & 0xFF, pos.Y);
		float heightRel = ((float)pos.Y - (float)seaLevel) / ((float)MapSizeY - (float)seaLevel);
		ClimateCondition outclimate = new ClimateCondition
		{
			Temperature = temp,
			Rainfall = rain / 255f,
			Fertility = (float)Climate.GetFertility((int)rain, temp, heightRel) / 255f
		};
		game.eventManager?.TriggerOnGetClimate(ref outclimate, pos, EnumGetClimateMode.NowValues, game.Calendar.TotalDays);
		return outclimate;
	}

	public override Vec3d GetWindSpeedAt(BlockPos pos)
	{
		return GetWindSpeedAt(new Vec3d(pos.X, pos.Y, pos.Z));
	}

	public override Vec3d GetWindSpeedAt(Vec3d pos)
	{
		Vec3d windspeed = new Vec3d();
		game.eventManager?.TriggerOnGetWindSpeed(pos, ref windspeed);
		return windspeed;
	}

	public override void DamageBlock(BlockPos pos, BlockFacing facing, float damage, IPlayer dualCallByPlayer = null)
	{
		Block block = RelaxedBlockAccess.GetBlock(pos);
		if (block.Id != 0)
		{
			game.damagedBlocks.TryGetValue(pos, out var blockDamage);
			if (blockDamage == null)
			{
				blockDamage = new BlockDamage
				{
					Position = pos,
					Block = block,
					Facing = facing,
					RemainingResistance = block.GetResistance(RelaxedBlockAccess, pos),
					LastBreakEllapsedMs = game.ElapsedMilliseconds,
					ByPlayer = game.player
				};
				game.damagedBlocks[pos.Copy()] = blockDamage;
			}
			blockDamage.RemainingResistance = GameMath.Clamp(blockDamage.RemainingResistance - damage, 0f, blockDamage.RemainingResistance);
			blockDamage.Facing = facing;
			if (blockDamage.Block != block)
			{
				blockDamage.RemainingResistance = block.GetResistance(RelaxedBlockAccess, pos);
			}
			blockDamage.Block = block;
			if (!(blockDamage.RemainingResistance <= 0f))
			{
				game.eventManager?.TriggerBlockBreaking(blockDamage);
			}
			blockDamage.LastBreakEllapsedMs = game.ElapsedMilliseconds;
		}
	}

	public override void MarkDecorsDirty(BlockPos pos)
	{
	}

	internal IPlayerRole GetRole(string roleCode)
	{
		RolesByCode.TryGetValue(roleCode, out var role);
		return role;
	}

	public void Add(LandClaim claim)
	{
		throw new InvalidOperationException("Not available on the client");
	}

	public bool Remove(LandClaim claim)
	{
		throw new InvalidOperationException("Not available on the client");
	}

	internal void Dispose()
	{
		(CachingBlockAccess as ICachingBlockAccessor)?.Dispose();
	}

	public override void SendDecorUpdateBulk(IEnumerable<BlockPos> updatedDecorPositions)
	{
		throw new NotImplementedException();
	}
}
