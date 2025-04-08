using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

internal class WorldgenWorldAccessor : IServerWorldAccessor, IWorldAccessor
{
	private IServerWorldAccessor waBase;

	private BlockAccessorWorldGen blockAccessorWorldGen;

	public IBlockAccessor BlockAccessor => blockAccessorWorldGen;

	public ITreeAttribute Config => waBase.Config;

	public EntityPos DefaultSpawnPosition => waBase.DefaultSpawnPosition;

	public FrameProfilerUtil FrameProfiler => waBase.FrameProfiler;

	public ICoreAPI Api => waBase.Api;

	public IChunkProvider ChunkProvider => waBase.ChunkProvider;

	public ILandClaimAPI Claims => waBase.Claims;

	public long[] LoadedChunkIndices => waBase.LoadedChunkIndices;

	public long[] LoadedMapChunkIndices => waBase.LoadedMapChunkIndices;

	public float[] BlockLightLevels => waBase.BlockLightLevels;

	public float[] SunLightLevels => waBase.SunLightLevels;

	public int SeaLevel => waBase.SeaLevel;

	public int Seed => waBase.Seed;

	public string SavegameIdentifier => waBase.SavegameIdentifier;

	public int SunBrightness => waBase.SunBrightness;

	public bool EntityDebugMode => waBase.EntityDebugMode;

	public IAssetManager AssetManager => waBase.AssetManager;

	public ILogger Logger => waBase.Logger;

	public EnumAppSide Side => waBase.Side;

	public IBulkBlockAccessor BulkBlockAccessor => waBase.BulkBlockAccessor;

	public IClassRegistryAPI ClassRegistry => waBase.ClassRegistry;

	public IGameCalendar Calendar => waBase.Calendar;

	public CollisionTester CollisionTester => waBase.CollisionTester;

	public Random Rand => waBase.Rand;

	public long ElapsedMilliseconds => waBase.ElapsedMilliseconds;

	public List<CollectibleObject> Collectibles => waBase.Collectibles;

	public IList<Block> Blocks => waBase.Blocks;

	public IList<Item> Items => waBase.Items;

	public List<EntityProperties> EntityTypes => waBase.EntityTypes;

	public List<string> EntityTypeCodes => waBase.EntityTypeCodes;

	public List<GridRecipe> GridRecipes => waBase.GridRecipes;

	public int DefaultEntityTrackingRange => waBase.DefaultEntityTrackingRange;

	public IPlayer[] AllOnlinePlayers => waBase.AllOnlinePlayers;

	public IPlayer[] AllPlayers => waBase.AllPlayers;

	public AABBIntersectionTest InteresectionTester => waBase.InteresectionTester;

	public ConcurrentDictionary<long, Entity> LoadedEntities => waBase.LoadedEntities;

	public OrderedDictionary<AssetLocation, ITreeGenerator> TreeGenerators => waBase.TreeGenerators;

	public Dictionary<string, string> RemappedEntities => waBase.RemappedEntities;

	public WorldgenWorldAccessor(IServerWorldAccessor worldAccessor, BlockAccessorWorldGen blockAccessorWorldGen)
	{
		waBase = worldAccessor;
		this.blockAccessorWorldGen = blockAccessorWorldGen;
	}

	public void CreateExplosion(BlockPos pos, EnumBlastType blastType, double destructionRadius, double injureRadius, float blockDropChanceMultiplier = 1f)
	{
		waBase.CreateExplosion(pos, blastType, destructionRadius, injureRadius, blockDropChanceMultiplier);
	}

	public void DespawnEntity(Entity entity, EntityDespawnData reason)
	{
		waBase.DespawnEntity(entity, reason);
	}

	public Block GetBlock(int blockId)
	{
		return waBase.GetBlock(blockId);
	}

	public Block GetBlock(AssetLocation blockCode)
	{
		return waBase.GetBlock(blockCode);
	}

	public IBlockAccessor GetBlockAccessor(bool synchronize, bool relight, bool strict, bool debug = false)
	{
		return waBase.GetBlockAccessor(synchronize, relight, strict, debug);
	}

	public IBulkBlockAccessor GetBlockAccessorBulkMinimalUpdate(bool synchronize, bool debug = false)
	{
		return waBase.GetBlockAccessorBulkMinimalUpdate(synchronize, debug);
	}

	public IBulkBlockAccessor GetBlockAccessorBulkUpdate(bool synchronize, bool relight, bool debug = false)
	{
		return waBase.GetBlockAccessorBulkUpdate(synchronize, relight, debug);
	}

	public IBulkBlockAccessor GetBlockAccessorMapChunkLoading(bool synchronize, bool debug = false)
	{
		return waBase.GetBlockAccessorMapChunkLoading(synchronize, debug);
	}

	public IBlockAccessorPrefetch GetBlockAccessorPrefetch(bool synchronize, bool relight)
	{
		return waBase.GetBlockAccessorPrefetch(synchronize, relight);
	}

	public IBlockAccessorRevertable GetBlockAccessorRevertable(bool synchronize, bool relight, bool debug = false)
	{
		return waBase.GetBlockAccessorRevertable(synchronize, relight, debug);
	}

	public ICachingBlockAccessor GetCachingBlockAccessor(bool synchronize, bool relight)
	{
		return waBase.GetCachingBlockAccessor(synchronize, relight);
	}

	public Entity[] GetEntitiesAround(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
	{
		return waBase.GetEntitiesAround(position, horRange, vertRange, matches);
	}

	public Entity[] GetEntitiesInsideCuboid(BlockPos startPos, BlockPos endPos, ActionConsumable<Entity> matches = null)
	{
		return waBase.GetEntitiesInsideCuboid(startPos, endPos, matches);
	}

	public Entity GetEntityById(long entityId)
	{
		return waBase.GetEntityById(entityId);
	}

	public EntityProperties GetEntityType(AssetLocation entityCode)
	{
		return waBase.GetEntityType(entityCode);
	}

	public Entity[] GetIntersectingEntities(BlockPos basePos, Cuboidf[] collisionBoxes, ActionConsumable<Entity> matches = null)
	{
		return waBase.GetIntersectingEntities(basePos, collisionBoxes, matches);
	}

	public Item GetItem(int itemId)
	{
		return waBase.GetItem(itemId);
	}

	public Item GetItem(AssetLocation itemCode)
	{
		return waBase.GetItem(itemCode);
	}

	public IBlockAccessor GetLockFreeBlockAccessor()
	{
		return waBase.GetLockFreeBlockAccessor();
	}

	public Entity GetNearestEntity(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null)
	{
		return waBase.GetNearestEntity(position, horRange, vertRange, matches);
	}

	public IPlayer[] GetPlayersAround(Vec3d position, float horRange, float vertRange, ActionConsumable<IPlayer> matches = null)
	{
		return waBase.GetPlayersAround(position, horRange, vertRange, matches);
	}

	public void HighlightBlocks(IPlayer player, int highlightSlotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1f)
	{
		waBase.HighlightBlocks(player, highlightSlotId, blocks, colors, mode, shape, scale);
	}

	public void HighlightBlocks(IPlayer player, int highlightSlotId, List<BlockPos> blocks, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary)
	{
		waBase.HighlightBlocks(player, highlightSlotId, blocks, mode, shape);
	}

	public bool IsFullyLoadedChunk(BlockPos pos)
	{
		return waBase.IsFullyLoadedChunk(pos);
	}

	public IPlayer NearestPlayer(double x, double y, double z)
	{
		return waBase.NearestPlayer(x, y, z);
	}

	public IPlayer PlayerByUid(string playerUid)
	{
		return waBase.PlayerByUid(playerUid);
	}

	public bool PlayerHasPrivilege(int clientid, string privilege)
	{
		return waBase.PlayerHasPrivilege(clientid, privilege);
	}

	public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		waBase.PlaySoundAt(location, posx, posy, posz, dualCallByPlayer, randomizePitch, range, volume);
	}

	public void PlaySoundAt(AssetLocation location, BlockPos pos, double yOffsetFromCenter, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		waBase.PlaySoundAt(location, pos, yOffsetFromCenter, ignorePlayerUid, randomizePitch, range, volume);
	}

	public void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		waBase.PlaySoundAt(location, atEntity, dualCallByPlayer, randomizePitch, range, volume);
	}

	public void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer dualCallByPlayer, float pitch, float range = 32f, float volume = 1f)
	{
		waBase.PlaySoundAt(location, atEntity, dualCallByPlayer, pitch, range, volume);
	}

	public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, float pitch, float range = 32f, float volume = 1f)
	{
		waBase.PlaySoundAt(location, posx, posy, posz, dualCallByPlayer, pitch, range, volume);
	}

	public void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, EnumSoundType soundType, float pitch, float range = 32f, float volume = 1f)
	{
		waBase.PlaySoundAt(location, posx, posy, posz, dualCallByPlayer, soundType, pitch, range, volume);
	}

	public void PlaySoundAt(AssetLocation location, IPlayer atPlayer, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		waBase.PlaySoundAt(location, atPlayer, dualCallByPlayer, randomizePitch, range, volume);
	}

	public void PlaySoundFor(AssetLocation location, IPlayer forPlayer, bool randomizePitch = true, float range = 32f, float volume = 1f)
	{
		waBase.PlaySoundFor(location, forPlayer, randomizePitch, range, volume);
	}

	public void PlaySoundFor(AssetLocation location, IPlayer forPlayer, float pitch, float range = 32f, float volume = 1f)
	{
		waBase.PlaySoundFor(location, forPlayer, pitch, range, volume);
	}

	public void RayTraceForSelection(Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
	{
		waBase.RayTraceForSelection(fromPos, toPos, ref blockSelection, ref entitySelection, bfilter, efilter);
	}

	public void RayTraceForSelection(IWorldIntersectionSupplier supplier, Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
	{
		waBase.RayTraceForSelection(supplier, fromPos, toPos, ref blockSelection, ref entitySelection, bfilter, efilter);
	}

	public void RayTraceForSelection(Vec3d fromPos, float pitch, float yaw, float range, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null)
	{
		waBase.RayTraceForSelection(fromPos, pitch, yaw, range, ref blockSelection, ref entitySelection, bfilter, efilter);
	}

	public void RayTraceForSelection(Ray ray, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter filter = null, EntityFilter efilter = null)
	{
		waBase.RayTraceForSelection(ray, ref blockSelection, ref entitySelection, filter, efilter);
	}

	public long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay)
	{
		return waBase.RegisterCallback(OnTimePassed, millisecondDelay);
	}

	public long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay)
	{
		return waBase.RegisterCallback(OnTimePassed, pos, millisecondDelay);
	}

	public long RegisterCallbackUnique(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval)
	{
		return waBase.RegisterCallbackUnique(OnGameTick, pos, millisecondInterval);
	}

	public long RegisterGameTickListener(Action<float> OnGameTick, int millisecondInterval, int initialDelayOffsetMs = 0)
	{
		return waBase.RegisterGameTickListener(OnGameTick, millisecondInterval, initialDelayOffsetMs);
	}

	public Block[] SearchBlocks(AssetLocation wildcard)
	{
		return waBase.SearchBlocks(wildcard);
	}

	public Item[] SearchItems(AssetLocation wildcard)
	{
		return waBase.SearchItems(wildcard);
	}

	public void SpawnCubeParticles(BlockPos blockPos, Vec3d pos, float radius, int quantity, float scale = 1f, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
	{
		waBase.SpawnCubeParticles(blockPos, pos, radius, quantity, scale, dualCallByPlayer, velocity);
	}

	public void SpawnCubeParticles(Vec3d pos, ItemStack item, float radius, int quantity, float scale = 1f, IPlayer dualCallByPlayer = null, Vec3f velocity = null)
	{
		waBase.SpawnCubeParticles(pos, item, radius, quantity, scale, dualCallByPlayer, velocity);
	}

	public void SpawnEntity(Entity entity)
	{
		waBase.SpawnEntity(entity);
	}

	public Entity SpawnItemEntity(ItemStack itemstack, Vec3d position, Vec3d velocity = null)
	{
		return waBase.SpawnItemEntity(itemstack, position, velocity);
	}

	public Entity SpawnItemEntity(ItemStack itemstack, BlockPos pos, Vec3d velocity = null)
	{
		return waBase.SpawnItemEntity(itemstack, pos, velocity);
	}

	public void SpawnParticles(float quantity, int color, Vec3d minPos, Vec3d maxPos, Vec3f minVelocity, Vec3f maxVelocity, float lifeLength, float gravityEffect, float scale = 1f, EnumParticleModel model = EnumParticleModel.Quad, IPlayer dualCallByPlayer = null)
	{
		waBase.SpawnParticles(quantity, color, minPos, maxPos, minVelocity, maxVelocity, lifeLength, gravityEffect, scale, model, dualCallByPlayer);
	}

	public void SpawnParticles(IParticlePropertiesProvider particlePropertiesProvider, IPlayer dualCallByPlayer = null)
	{
		waBase.SpawnParticles(particlePropertiesProvider, dualCallByPlayer);
	}

	public void UnregisterCallback(long listenerId)
	{
		waBase.UnregisterCallback(listenerId);
	}

	public void UnregisterGameTickListener(long listenerId)
	{
		waBase.UnregisterGameTickListener(listenerId);
	}

	public RecipeRegistryBase GetRecipeRegistry(string code)
	{
		throw new NotImplementedException();
	}

	public void UpdateEntityChunk(Entity entity, long newChunkIndex3d)
	{
		waBase.UpdateEntityChunk(entity, newChunkIndex3d);
	}

	public bool LoadEntity(Entity entity, long fromChunkIndex3d)
	{
		throw new InvalidOperationException("Cannot use LoadEntity from within WorldGenBlockAccessor");
	}
}
