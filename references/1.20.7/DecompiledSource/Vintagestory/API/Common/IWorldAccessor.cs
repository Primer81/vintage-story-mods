#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

//
// Summary:
//     Important interface to access the game world.
public interface IWorldAccessor
{
    //
    // Summary:
    //     The current world config
    ITreeAttribute Config { get; }

    //
    // Summary:
    //     The default spawn position as sent by the server (usually the map middle). Does
    //     not take player specific spawn point into account
    EntityPos DefaultSpawnPosition { get; }

    //
    // Summary:
    //     Gets the frame profiler utility.
    FrameProfilerUtil FrameProfiler { get; }

    //
    // Summary:
    //     The api interface
    ICoreAPI Api { get; }

    IChunkProvider ChunkProvider { get; }

    //
    // Summary:
    //     The land claiming api interface
    ILandClaimAPI Claims { get; }

    //
    // Summary:
    //     Returns a list all loaded chunk positions in the form of a long index. Code to
    //     turn that into x/y/z coords: Vec3i coords = new Vec3i( (int)(chunkIndex3d % ChunkMapSizeX),
    //     (int)(chunkIndex3d / ((long)ChunkMapSizeX * ChunkMapSizeZ)), (int)((chunkIndex3d
    //     / ChunkMapSizeX) % ChunkMapSizeZ) ); Retrieving the list is not a very fast process,
    //     not suggested to be called every frame
    long[] LoadedChunkIndices { get; }

    //
    // Summary:
    //     Returns a list all loaded chunk positions in the form of a long index
    long[] LoadedMapChunkIndices { get; }

    //
    // Summary:
    //     The currently configured block light brightness levels
    float[] BlockLightLevels { get; }

    //
    // Summary:
    //     The currently configured sun light brightness levels
    float[] SunLightLevels { get; }

    //
    // Summary:
    //     The currently configured sea level (y-coordinate)
    int SeaLevel { get; }

    //
    // Summary:
    //     The world seed. Accessible on the server and the client
    int Seed { get; }

    //
    // Summary:
    //     A globally unique identifier for this savegame
    string SavegameIdentifier { get; }

    //
    // Summary:
    //     The currently configured max sun light level
    int SunBrightness { get; }

    //
    // Summary:
    //     Whether the current side (client/server) is in entity debug mode
    bool EntityDebugMode { get; }

    //
    // Summary:
    //     Loaded game assets
    IAssetManager AssetManager { get; }

    //
    // Summary:
    //     Logging Utility
    ILogger Logger { get; }

    //
    // Summary:
    //     The current side (client/server)
    EnumAppSide Side { get; }

    //
    // Summary:
    //     Access blocks and other world data from loaded chunks, fault tolerant
    IBlockAccessor BlockAccessor { get; }

    //
    // Summary:
    //     Fault tolerant bulk block access to the worlds block data. Since this is a single
    //     bulk block access instance the cached data is shared for everything accessing
    //     this method, hence should only be accessed from the main thread and any changed
    //     comitted within the same game tick. You can however use the WorldManager api
    //     to get your own instance of a bulk block accessor
    IBulkBlockAccessor BulkBlockAccessor { get; }

    //
    // Summary:
    //     Interface to create instance of certain classes
    IClassRegistryAPI ClassRegistry { get; }

    //
    // Summary:
    //     Interface to access the game calendar. On the server side only available after
    //     run stage 'LoadGamePre' (before that it is null)
    IGameCalendar Calendar { get; }

    //
    // Summary:
    //     For collision testing in the main thread
    CollisionTester CollisionTester { get; }

    //
    // Summary:
    //     Just a random number generator. Makes use of ThreadLocal for thread safety.
    Random Rand { get; }

    //
    // Summary:
    //     Amount of milliseconds ellapsed since startup
    long ElapsedMilliseconds { get; }

    //
    // Summary:
    //     List of all loaded blocks and items without placeholders
    List<CollectibleObject> Collectibles { get; }

    //
    // Summary:
    //     List of all loaded blocks. The array index is the block id. Some may be null
    //     or placeholders (then block.code is null). Client-side none are null, what was
    //     null return as air blocks.
    IList<Block> Blocks { get; }

    //
    // Summary:
    //     List of all loaded items. The array index is the item id. Some may be placeholders
    //     (then item.code is null). Server-side, some may be null; client-side, a check
    //     for item == null is not necessary.
    IList<Item> Items { get; }

    //
    // Summary:
    //     List of all loaded entity types.
    List<EntityProperties> EntityTypes { get; }

    //
    // Summary:
    //     List of the codes of all loaded entity types, in the AssetLocation short string
    //     format (e.g. "creature" for entities with domain game:, "domain:creature" for
    //     entities with other domains)
    List<string> EntityTypeCodes { get; }

    //
    // Summary:
    //     List of all loaded crafting recipes
    List<GridRecipe> GridRecipes { get; }

    //
    // Summary:
    //     The range in blocks within a client will receive regular updates for an entity
    int DefaultEntityTrackingRange { get; }

    //
    // Summary:
    //     Gets a list of all online players. Warning: Also returns currently connecting
    //     player whose player data may not have been fully initialized. Check for player.ConnectionState
    //     to avoid these.
    //
    // Returns:
    //     Array containing the IDs of online players
    IPlayer[] AllOnlinePlayers { get; }

    //
    // Summary:
    //     Gets a list of all players that connected to this server at least once while
    //     the server was running. When called client side you will receive the same as
    //     AllOnlinePlayers
    //
    // Returns:
    //     Array containing the IDs of online players
    IPlayer[] AllPlayers { get; }

    //
    // Summary:
    //     Utility for testing intersections. Only access from main thread.
    AABBIntersectionTest InteresectionTester { get; }

    //
    // Summary:
    //     Retrieve a previously registered recipe registry
    //
    // Parameters:
    //   code:
    RecipeRegistryBase GetRecipeRegistry(string code);

    //
    // Summary:
    //     Retrieve the item class from given item id
    //
    // Parameters:
    //   itemId:
    Item GetItem(int itemId);

    //
    // Summary:
    //     Retrieve the block class from given block id
    //
    // Parameters:
    //   blockId:
    Block GetBlock(int blockId);

    //
    // Summary:
    //     Returns all blocktypes matching given wildcard
    //
    // Parameters:
    //   wildcard:
    Block[] SearchBlocks(AssetLocation wildcard);

    //
    // Summary:
    //     Returns all item types matching given wildcard
    //
    // Parameters:
    //   wildcard:
    Item[] SearchItems(AssetLocation wildcard);

    //
    // Summary:
    //     Retrieve the item class from given item code. Will return null if the item does
    //     not exist.
    //
    // Parameters:
    //   itemCode:
    Item GetItem(AssetLocation itemCode);

    //
    // Summary:
    //     Retrieve the block class from given block code. Will return null if the block
    //     does not exist. Logs a warning if block does not exist
    //
    // Parameters:
    //   blockCode:
    Block GetBlock(AssetLocation blockCode);

    //
    // Summary:
    //     Retrieve the entity class from given entity code. Will return null if the entity
    //     does not exist.
    //
    // Parameters:
    //   entityCode:
    EntityProperties GetEntityType(AssetLocation entityCode);

    //
    // Summary:
    //     Spawns a dropped itemstack at given position. Will immediately disappear if stacksize==0
    //     Returns the entity spawned (may be null!)
    //
    // Parameters:
    //   itemstack:
    //
    //   position:
    //
    //   velocity:
    Entity SpawnItemEntity(ItemStack itemstack, Vec3d position, Vec3d velocity = null);

    //
    // Summary:
    //     Spawns a dropped itemstack at given position. Will immediately disappear if stacksize==0
    //     Returns the entity spawned (may be null!)
    //
    // Parameters:
    //   itemstack:
    //
    //   position:
    //
    //   velocity:
    Entity SpawnItemEntity(ItemStack itemstack, BlockPos pos, Vec3d velocity = null);

    //
    // Summary:
    //     Creates a new entity. It's the responsibility of the given Entity to call set
    //     it's EntityType. This should be done inside it's Initialize method before base.Initialize
    //     is called.
    //
    // Parameters:
    //   entity:
    void SpawnEntity(Entity entity);

    //
    // Summary:
    //     Loads a previously created entity into the loadedEntities list. Used when a chunk
    //     is loaded.
    //
    // Parameters:
    //   entity:
    //
    //   fromChunkIndex3d:
    bool LoadEntity(Entity entity, long fromChunkIndex3d);

    //
    // Summary:
    //     Removes an entity from its old chunk and adds it to the chunk with newChunkIndex3d
    //
    //
    // Parameters:
    //   entity:
    //
    //   newChunkIndex3d:
    void UpdateEntityChunk(Entity entity, long newChunkIndex3d);

    //
    // Summary:
    //     Retrieve all entities within given range and given matcher method. If now matcher
    //     method is supplied, all entities are returned.
    //
    // Parameters:
    //   position:
    //
    //   horRange:
    //
    //   vertRange:
    //
    //   matches:
    Entity[] GetEntitiesAround(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null);

    //
    // Summary:
    //     Retrieve all entities within a cuboid bound by startPos and endPos. If now matcher
    //     method is supplied, all entities are returned.
    //
    // Parameters:
    //   startPos:
    //
    //   endPos:
    //
    //   matches:
    Entity[] GetEntitiesInsideCuboid(BlockPos startPos, BlockPos endPos, ActionConsumable<Entity> matches = null);

    //
    // Summary:
    //     Retrieve all players within given range and given matcher method. This method
    //     is faster than when using GetEntitiesAround with a matcher for players
    //
    // Parameters:
    //   position:
    //
    //   horRange:
    //
    //   vertRange:
    //
    //   matches:
    IPlayer[] GetPlayersAround(Vec3d position, float horRange, float vertRange, ActionConsumable<IPlayer> matches = null);

    //
    // Summary:
    //     Retrieve the nearest entity within given range and given matcher method
    //
    // Parameters:
    //   position:
    //
    //   horRange:
    //
    //   vertRange:
    //
    //   matches:
    Entity GetNearestEntity(Vec3d position, float horRange, float vertRange, ActionConsumable<Entity> matches = null);

    //
    // Summary:
    //     Retrieve an entity by its unique id, returns null if no such entity exists or
    //     hasn't been loaded
    //
    // Parameters:
    //   entityId:
    Entity GetEntityById(long entityId);

    //
    // Summary:
    //     Retrieves the first found entity that intersects any of the supplied collisionboxes
    //     offseted by basePos. This is a helper method for you to determine if you can
    //     place a block at given position. You can also implement it yourself with intersection
    //     testing and GetEntitiesAround()
    //
    // Parameters:
    //   collisionBoxes:
    //
    //   basePos:
    //
    //   matches:
    Entity[] GetIntersectingEntities(BlockPos basePos, Cuboidf[] collisionBoxes, ActionConsumable<Entity> matches = null);

    //
    // Summary:
    //     Find the nearest player to the given position
    //
    // Parameters:
    //   x:
    //     x coordinate
    //
    //   y:
    //     y coordinate
    //
    //   z:
    //     z coordinate
    //
    // Returns:
    //     ID of the nearest player
    IPlayer NearestPlayer(double x, double y, double z);

    //
    // Summary:
    //     Retrieves the worldplayer data object of given player. When called server side
    //     the player does not need to be connected.
    //
    // Parameters:
    //   playerUid:
    IPlayer PlayerByUid(string playerUid);

    //
    // Summary:
    //     Plays given sound at given position.
    //
    // Parameters:
    //   location:
    //     The sound path, without sounds/ prefix or the .ogg ending
    //
    //   posx:
    //
    //   posy:
    //
    //   posz:
    //
    //   dualCallByPlayer:
    //     If this call is made on client and on server, set this the causing playerUID
    //     to prevent double playing. Essentially dualCall will play the sound on the client,
    //     and send it to all other players except source client
    //
    //   randomizePitch:
    //
    //   range:
    //     The range at which the gain will be attenuated to 1% of the supplied volume
    //
    //   volume:
    void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f);

    //
    // Summary:
    //     Plays given sound at given position - dimension aware. Plays at the center of
    //     the BlockPos
    //
    // Parameters:
    //   location:
    //     The sound path, without sounds/ prefix or the .ogg ending
    //
    //   pos:
    //
    //   yOffsetFromCenter:
    //     How much above or below the central Y position of the block to play
    //
    //   dualCallByPlayer:
    //     If this call is made on client and on server, set this the causing playerUID
    //     to prevent double playing. Essentially dualCall will play the sound on the client,
    //     and send it to all other players except source client
    //
    //   randomizePitch:
    //
    //   range:
    //     The range at which the gain will be attenuated to 1% of the supplied volume
    //
    //   volume:
    void PlaySoundAt(AssetLocation location, BlockPos pos, double yOffsetFromCenter, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f);

    //
    // Summary:
    //     Plays given sound at given position.
    //
    // Parameters:
    //   location:
    //     The sound path, without sounds/ prefix or the .ogg ending
    //
    //   atEntity:
    //
    //   dualCallByPlayer:
    //     If this call is made on client and on server, set this the causing playerUID
    //     to prevent double playing. Essentially dualCall will play the sound on the client,
    //     and send it to all other players except source client
    //
    //   randomizePitch:
    //
    //   range:
    //     The range at which the gain will be attenuated to 1% of the supplied volume
    //
    //   volume:
    void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f);

    //
    // Summary:
    //     Plays given sound at given position.
    //
    // Parameters:
    //   location:
    //
    //   atEntity:
    //
    //   dualCallByPlayer:
    //
    //   pitch:
    //
    //   range:
    //
    //   volume:
    void PlaySoundAt(AssetLocation location, Entity atEntity, IPlayer dualCallByPlayer, float pitch, float range = 32f, float volume = 1f);

    //
    // Summary:
    //     Plays given sound at given position.
    //
    // Parameters:
    //   location:
    //
    //   posx:
    //
    //   posy:
    //
    //   posz:
    //
    //   dualCallByPlayer:
    //
    //   pitch:
    //
    //   range:
    //
    //   volume:
    void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, float pitch, float range = 32f, float volume = 1f);

    void PlaySoundAt(AssetLocation location, double posx, double posy, double posz, IPlayer dualCallByPlayer, EnumSoundType soundType, float pitch, float range = 32f, float volume = 1f);

    //
    // Summary:
    //     Plays given sound at given player position.
    //
    // Parameters:
    //   location:
    //     The sound path, without sounds/ prefix or the .ogg ending
    //
    //   atPlayer:
    //
    //   dualCallByPlayer:
    //     If this call is made on client and on server, set this the causing playerUID
    //     to prevent double playing. Essentially dualCall will play the sound on the client,
    //     and send it to all other players except source client
    //
    //   randomizePitch:
    //
    //   range:
    //     The range at which the gain will be attenuated to 1% of the supplied volume
    //
    //   volume:
    void PlaySoundAt(AssetLocation location, IPlayer atPlayer, IPlayer dualCallByPlayer = null, bool randomizePitch = true, float range = 32f, float volume = 1f);

    //
    // Summary:
    //     Plays given sound only for given player. Useful when called server side, for
    //     the client side there is no difference over using PlaySoundAt or PlaySoundFor
    //
    //
    // Parameters:
    //   location:
    //     The sound path, without sounds/ prefix or the .ogg ending
    //
    //   forPlayer:
    //
    //   randomizePitch:
    //
    //   range:
    //     The range at which the gain will be attenuated to 1% of the supplied volume
    //
    //   volume:
    void PlaySoundFor(AssetLocation location, IPlayer forPlayer, bool randomizePitch = true, float range = 32f, float volume = 1f);

    void PlaySoundFor(AssetLocation location, IPlayer forPlayer, float pitch, float range = 32f, float volume = 1f);

    //
    // Summary:
    //     Spawn a bunch of particles
    //
    // Parameters:
    //   quantity:
    //
    //   color:
    //
    //   minPos:
    //
    //   maxPos:
    //
    //   minVelocity:
    //
    //   maxVelocity:
    //
    //   lifeLength:
    //
    //   gravityEffect:
    //
    //   scale:
    //
    //   model:
    //
    //   dualCallByPlayer:
    //     If this call is made on client and on server, set this to the causing playerUID
    //     to prevent double spawning. Essentially dualCall will spawn the particles on
    //     the client, and send it to all other players except source client
    void SpawnParticles(float quantity, int color, Vec3d minPos, Vec3d maxPos, Vec3f minVelocity, Vec3f maxVelocity, float lifeLength, float gravityEffect, float scale = 1f, EnumParticleModel model = EnumParticleModel.Quad, IPlayer dualCallByPlayer = null);

    //
    // Summary:
    //     Spawn a bunch of particles
    //
    // Parameters:
    //   particlePropertiesProvider:
    //
    //   dualCallByPlayer:
    //     If this call is made on client and on server, set this to the causing playerUID
    //     to prevent double spawning. Essentially dualCall will spawn the particles on
    //     the client, and send it to all other players except source client
    void SpawnParticles(IParticlePropertiesProvider particlePropertiesProvider, IPlayer dualCallByPlayer = null);

    //
    // Summary:
    //     Spawn a bunch of particles colored by the block at given position
    //
    // Parameters:
    //   blockPos:
    //     The position of the block to take the color from
    //
    //   pos:
    //     The position where the particles should spawn
    //
    //   radius:
    //
    //   quantity:
    //
    //   scale:
    //
    //   dualCallByPlayer:
    //     If this call is made on client and on server, set this to the causing playerUID
    //     to prevent double spawning. Essentially dualCall will spawn the particles on
    //     the client, and send it to all other players except source client
    //
    //   velocity:
    void SpawnCubeParticles(BlockPos blockPos, Vec3d pos, float radius, int quantity, float scale = 1f, IPlayer dualCallByPlayer = null, Vec3f velocity = null);

    //
    // Summary:
    //     Spawn a bunch of particles colored by given itemstack
    //
    // Parameters:
    //   pos:
    //     The position where the particles should spawn
    //
    //   item:
    //
    //   radius:
    //
    //   quantity:
    //
    //   scale:
    //
    //   dualCallByPlayer:
    //     If this call is made on client and on server, set this to the causing playerUID
    //     to prevent double spawning. Essentially dualCall will spawn the particles on
    //     the client, and send it to all other players except source client
    //
    //   velocity:
    void SpawnCubeParticles(Vec3d pos, ItemStack item, float radius, int quantity, float scale = 1f, IPlayer dualCallByPlayer = null, Vec3f velocity = null);

    //
    // Summary:
    //     Shoots out a virtual ray at between given positions and stops when the ray hits
    //     a block or entity selection box. The block/entity it struck first is then returned
    //     by reference.
    //
    // Parameters:
    //   fromPos:
    //
    //   toPos:
    //
    //   blockSelection:
    //
    //   entitySelection:
    //
    //   bfilter:
    //     Can be used to ignore certain blocks. Return false to ignore
    //
    //   efilter:
    //     Can be used to ignore certain entities. Return false to ignore
    void RayTraceForSelection(Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null);

    //
    // Summary:
    //     Shoots out a virtual ray at between given positions and stops when the ray hits
    //     a block or entity intersection box supplied by given supplier. The block/entity
    //     it struck first is then returned by reference.
    //
    // Parameters:
    //   supplier:
    //
    //   fromPos:
    //
    //   toPos:
    //
    //   blockSelection:
    //
    //   entitySelection:
    //
    //   bfilter:
    //     Can be used to ignore certain blocks. Return false to ignore
    //
    //   efilter:
    //     Can be used to ignore certain entities. Return false to ignore
    void RayTraceForSelection(IWorldIntersectionSupplier supplier, Vec3d fromPos, Vec3d toPos, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null);

    //
    // Summary:
    //     Shoots out a virtual ray at given position and angle and stops when the ray hits
    //     a block or entity selection box. The block/entity it struck first is then returned
    //     by reference.
    //
    // Parameters:
    //   fromPos:
    //
    //   pitch:
    //
    //   yaw:
    //
    //   range:
    //
    //   blockSelection:
    //
    //   entitySelection:
    //
    //   bfilter:
    //     Can be used to ignore certain blocks. Return false to ignore
    //
    //   efilter:
    //     Can be used to ignore certain entities. Return false to ignore
    void RayTraceForSelection(Vec3d fromPos, float pitch, float yaw, float range, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter bfilter = null, EntityFilter efilter = null);

    //
    // Summary:
    //     Shoots out a given ray and stops when the ray hits a block or entity selection
    //     box. The block/entity it struck first is then returned by reference.
    //
    // Parameters:
    //   ray:
    //
    //   blockSelection:
    //
    //   entitySelection:
    //
    //   filter:
    //
    //   efilter:
    //     Can be used to ignore certain entities. Return false to ignore
    void RayTraceForSelection(Ray ray, ref BlockSelection blockSelection, ref EntitySelection entitySelection, BlockFilter filter = null, EntityFilter efilter = null);

    //
    // Summary:
    //     Calls given method after every given interval until unregistered. The engine
    //     may call your method slightly later since these event are handled only during
    //     fixed interval game ticks.
    //
    // Parameters:
    //   onGameTick:
    //
    //   millisecondInterval:
    //
    //   initialDelayOffsetMs:
    //
    // Returns:
    //     listenerId
    long RegisterGameTickListener(Action<float> onGameTick, int millisecondInterval, int initialDelayOffsetMs = 0);

    //
    // Summary:
    //     Removes a game tick listener
    //
    // Parameters:
    //   listenerId:
    void UnregisterGameTickListener(long listenerId);

    //
    // Summary:
    //     Calls given method after supplied amount of milliseconds. The engine may call
    //     your method slightly later since these event are handled only during fixed interval
    //     game ticks.
    //
    // Parameters:
    //   OnTimePassed:
    //
    //   millisecondDelay:
    //
    // Returns:
    //     listenerId
    long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay);

    //
    // Summary:
    //     Calls given method after supplied amount of milliseconds. The engine may call
    //     your method slightly later since these event are handled only during fixed interval
    //     game ticks. Ignores any subsequent registers for the same blockpos while a callback
    //     is still in the queue. Used e.g. for liquid physics to prevent unnecessary multiple
    //     updates
    //
    // Parameters:
    //   OnGameTick:
    //
    //   pos:
    //
    //   millisecondInterval:
    //
    // Returns:
    //     listenerId
    long RegisterCallbackUnique(Action<IWorldAccessor, BlockPos, float> OnGameTick, BlockPos pos, int millisecondInterval);

    //
    // Summary:
    //     Calls given method after supplied amount of milliseconds, lets you supply a block
    //     position to be passed to the method. The engine may call your method slightly
    //     later since these event are handled only during fixed interval game ticks.
    //
    // Parameters:
    //   OnTimePassed:
    //
    //   pos:
    //
    //   millisecondDelay:
    //
    // Returns:
    //     listenerId
    long RegisterCallback(Action<IWorldAccessor, BlockPos, float> OnTimePassed, BlockPos pos, int millisecondDelay);

    //
    // Summary:
    //     Returns true if given client has a privilege. Always returns true on the client.
    //
    //
    // Parameters:
    //   clientid:
    //
    //   privilege:
    bool PlayerHasPrivilege(int clientid, string privilege);

    //
    // Summary:
    //     Removes a delayed callback
    //
    // Parameters:
    //   listenerId:
    void UnregisterCallback(long listenerId);

    //
    // Summary:
    //     Sends given player a list of block positions that should be highlighted
    //
    // Parameters:
    //   player:
    //
    //   highlightSlotId:
    //     for multiple highlights use a different number
    //
    //   blocks:
    //
    //   colors:
    //
    //   mode:
    //
    //   shape:
    //
    //   scale:
    void HighlightBlocks(IPlayer player, int highlightSlotId, List<BlockPos> blocks, List<int> colors, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary, float scale = 1f);

    //
    // Summary:
    //     Sends given player a list of block positions that should be highlighted (using
    //     a default color)
    //
    // Parameters:
    //   player:
    //
    //   highlightSlotId:
    //     for multiple highlights use a different number
    //
    //   blocks:
    //
    //   mode:
    //
    //   shape:
    //     When arbitrary, the blocks list represents the blocks to be highlighted. When
    //     Cube the blocks list should contain 2 positions for start and end
    void HighlightBlocks(IPlayer player, int highlightSlotId, List<BlockPos> blocks, EnumHighlightBlocksMode mode = EnumHighlightBlocksMode.Absolute, EnumHighlightShape shape = EnumHighlightShape.Arbitrary);

    //
    // Summary:
    //     Retrieve a customized interface to access blocks in the loaded game world.
    //
    // Parameters:
    //   synchronize:
    //     Whether or not a call to Setblock should send the update also to all connected
    //     clients
    //
    //   relight:
    //     Whether or not to relight the chunk after a call to SetBlock and the light values
    //     changed by that
    //
    //   strict:
    //     Log an error message if GetBlock/SetBlock was called to an unloaded chunk
    //
    //   debug:
    //     If strict, crashes the server if a unloaded chunk was crashed, prints an exception
    //     and exports a png image of the current loaded chunks
    IBlockAccessor GetBlockAccessor(bool synchronize, bool relight, bool strict, bool debug = false);

    //
    // Summary:
    //     Retrieve a customized interface to access blocks in the loaded game world. Does
    //     not to relight/sync on a SetBlock until Commit() is called. On commit all touched
    //     blocks are relit/synced at once. This method should be used when setting many
    //     blocks (e.g. tree generation, explosion, etc.).
    //
    // Parameters:
    //   synchronize:
    //     Whether or not a call to Setblock should send the update also to all connected
    //     clients
    //
    //   relight:
    //     Whether or not to relight the chunk after the a call to SetBlock and the light
    //     values changed by that
    //
    //   debug:
    IBulkBlockAccessor GetBlockAccessorBulkUpdate(bool synchronize, bool relight, bool debug = false);

    //
    // Summary:
    //     Retrieve a customized interface to access blocks in the loaded game world. Does
    //     not relight. On commit all touched blocks are updated at once. This method is
    //     currently used for the snow accumulation system
    //
    // Parameters:
    //   synchronize:
    //
    //   debug:
    IBulkBlockAccessor GetBlockAccessorBulkMinimalUpdate(bool synchronize, bool debug = false);

    //
    // Summary:
    //     Retrieve a special Bulk blockaccessor which can have the chunks it accesses directly
    //     provided to it from a loading mapchunk. On commit all touched blocks are updated
    //     at once. This method is currently used for the snow accumulation system
    IBulkBlockAccessor GetBlockAccessorMapChunkLoading(bool synchronize, bool debug = false);

    //
    // Summary:
    //     Same as GetBlockAccessorBulkUpdate, additionally, each Commit() stores the previous
    //     state and you can perform undo/redo operations on these.
    //
    // Parameters:
    //   synchronize:
    //     Whether or not a call to Setblock should send the update also to all connected
    //     clients
    //
    //   relight:
    //     Whether or not to relight the chunk after a call to SetBlock and the light values
    //     changed by that
    //
    //   debug:
    IBlockAccessorRevertable GetBlockAccessorRevertable(bool synchronize, bool relight, bool debug = false);

    //
    // Summary:
    //     Same as GetBlockAccessor but you have to call PrefetchBlocks() before using GetBlock().
    //     It pre-loads all blocks in given area resulting in faster GetBlock() access
    //
    // Parameters:
    //   synchronize:
    //     Whether or not a call to Setblock should send the update also to all connected
    //     clients
    //
    //   relight:
    //     Whether or not to relight the chunk after a call to SetBlock and the light values
    //     changed by that
    IBlockAccessorPrefetch GetBlockAccessorPrefetch(bool synchronize, bool relight);

    //
    // Summary:
    //     Same as the normal block accessor but remembers the previous chunk that was accessed.
    //     This can give you a 10-50% performance boosts when you scan many blocks in tight
    //     loops DONT FORGET: Call .Begin() before getting/setting in a tight loop. Not
    //     calling it can cause the game to crash
    //
    // Parameters:
    //   synchronize:
    //
    //   relight:
    ICachingBlockAccessor GetCachingBlockAccessor(bool synchronize, bool relight);

    //
    // Summary:
    //     This block accessor is *read only* and does not use lock() or chunk.Unpack()
    //     in order to make it very fast. This comes at the cost of sometimes reading invalid
    //     data (block id = 0) when the chunk is packed or being packed.
    IBlockAccessor GetLockFreeBlockAccessor();
}
#if false // Decompilation log
'181' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
