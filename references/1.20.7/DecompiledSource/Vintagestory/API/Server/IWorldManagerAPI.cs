#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Server;

//
// Summary:
//     Methods to modify the game world
public interface IWorldManagerAPI
{
    //
    // Summary:
    //     Returns a (cloned) list of all currently loaded map chunks. The key is the 2d
    //     index of the map chunk, can be turned into an x/z coord
    Dictionary<long, IMapChunk> AllLoadedMapchunks { get; }

    //
    // Summary:
    //     Returns a (cloned) list of all currently loaded map regions. The key is the 2d
    //     index of the map region, can be turned into an x/z coord
    Dictionary<long, IMapRegion> AllLoadedMapRegions { get; }

    //
    // Summary:
    //     Returns a (cloned) list of all currently loaded chunks. The key is the 3d index
    //     of the chunk, can be turned into an x/y/z coord. Warning: This locks the loaded
    //     chunk dictionary during the clone, preventing other threads from updating it.
    //     In other words: Using this method often will have a significant performance impact.
    Dictionary<long, IServerChunk> AllLoadedChunks { get; }

    //
    // Summary:
    //     Amount of chunk columns currently in the generating queue
    int CurrentGeneratingChunkCount { get; }

    int ChunkDeletionsInQueue { get; }

    //
    // Summary:
    //     The worlds savegame object. If you change these values they will be permanently
    //     stored
    ISaveGame SaveGame { get; }

    //
    // Summary:
    //     The currently selected playstyle
    PlayStyle CurrentPlayStyle { get; }

    //
    // Summary:
    //     Completely disables automatic generation of chunks that normally builds up a
    //     radius of chunks around the player.
    bool AutoGenerateChunks { get; set; }

    //
    // Summary:
    //     Disables sending of normal chunks to all players except for force loaded ones
    //     using ForceLoadChunkColumn
    bool SendChunks { get; set; }

    //
    // Summary:
    //     Width of the current world
    int MapSizeX { get; }

    //
    // Summary:
    //     Height of the current world
    int MapSizeY { get; }

    //
    // Summary:
    //     Length of the current world
    int MapSizeZ { get; }

    //
    // Summary:
    //     Width/Length/Height in blocks of a region on the server
    int RegionSize { get; }

    //
    // Summary:
    //     Width/Length/Height in blocks of a chunk on the server
    int ChunkSize { get; }

    //
    // Summary:
    //     Get the seed used to generate the current world
    //
    // Value:
    //     The map seed
    int Seed { get; }

    //
    // Summary:
    //     The current world filename
    string CurrentWorldName { get; }

    //
    // Summary:
    //     Retrieves the default spawnpoint (x/y/z coordinate)
    //
    // Value:
    //     Default spawnpoint
    int[] DefaultSpawnPosition { get; }

    //
    // Summary:
    //     Allows setting a 32 float array that defines the brightness of each block light
    //     level. Has to be set before any players join or any chunks are generated.
    //
    // Parameters:
    //   lightLevels:
    void SetBlockLightLevels(float[] lightLevels);

    //
    // Summary:
    //     Allows setting a 32 float array that defines the brightness of each sun light
    //     level. Has to be set before any players join or any chunks are generated.
    //
    // Parameters:
    //   lightLevels:
    void SetSunLightLevels(float[] lightLevels);

    //
    // Summary:
    //     Sets the default light range of sunlight. Default is 24. Has to be set before
    //     any players join or any chunks are generated.
    //
    // Parameters:
    //   lightlevel:
    void SetSunBrightness(int lightlevel);

    //
    // Summary:
    //     Sets the default sea level for the world to be generated. Currently used by the
    //     client to calculate the correct temperature/rainfall values for climate tinting.
    //
    //
    // Parameters:
    //   sealevel:
    void SetSeaLevel(int sealevel);

    //
    // Summary:
    //     Gets the Server map region at given coordinate. Returns null if it's not loaded
    //     or does not exist yet
    //
    // Parameters:
    //   regionX:
    //
    //   regionZ:
    IMapRegion GetMapRegion(int regionX, int regionZ);

    //
    // Summary:
    //     Gets the Server map region at given coordinate. Returns null if it's not loaded
    //     or does not exist yet
    //
    // Parameters:
    //   index2d:
    IMapRegion GetMapRegion(long index2d);

    //
    // Summary:
    //     Gets the Server map chunk at given coordinate. Returns null if it's not loaded
    //     or does not exist yet
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkZ:
    IServerMapChunk GetMapChunk(int chunkX, int chunkZ);

    //
    // Summary:
    //     Gets the Server map chunk at given coordinate index. Returns null if it's not
    //     loaded or does not exist yet
    //
    // Parameters:
    //   index2d:
    IMapChunk GetMapChunk(long index2d);

    //
    // Summary:
    //     Gets the Server chunk at given coordinate. Returns null if it's not loaded or
    //     does not exist yet
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkY:
    //
    //   chunkZ:
    IServerChunk GetChunk(int chunkX, int chunkY, int chunkZ);

    //
    // Summary:
    //     Gets the Server chunk at given coordinate. Returns null if it's not loaded or
    //     does not exist yet
    //
    // Parameters:
    //   pos:
    IServerChunk GetChunk(BlockPos pos);

    long ChunkIndex3D(int chunkX, int chunkY, int chunkZ);

    long MapRegionIndex2D(int regionX, int regionZ);

    long MapRegionIndex2DByBlockPos(int posX, int posZ);

    Vec3i MapRegionPosFromIndex2D(long index2d);

    Vec2i MapChunkPosFromChunkIndex2D(long index2d);

    long MapChunkIndex2D(int chunkX, int chunkZ);

    //
    // Summary:
    //     Gets the Server chunk at given coordinate. Returns null if it's not loaded or
    //     does not exist yet
    //
    // Parameters:
    //   index3d:
    IServerChunk GetChunk(long index3d);

    //
    // Summary:
    //     Returns a number that is guaranteed to be unique for the current world every
    //     time it is called. Curently use for entity herding behavior.
    long GetNextUniqueId();

    //
    // Summary:
    //     Asynchronly high priority load a chunk column at given coordinate.
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkZ:
    //
    //   options:
    //     Additional loading options
    [Obsolete("Use LoadChunkColumnPriority()")]
    void LoadChunkColumnFast(int chunkX, int chunkZ, ChunkLoadOptions options = null);

    //
    // Summary:
    //     Asynchronly high priority load a chunk column at given coordinate.
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkZ:
    //
    //   options:
    //     Additional loading options
    void LoadChunkColumnPriority(int chunkX, int chunkZ, ChunkLoadOptions options = null);

    //
    // Summary:
    //     Asynchronly high priority load an area of chunk columns at given coordinates.
    //     Make sure that X1<=X2 and Z1<=Z2
    //
    // Parameters:
    //   chunkX1:
    //
    //   chunkZ1:
    //
    //   chunkX2:
    //
    //   chunkZ2:
    //
    //   options:
    //     Additional loading options
    [Obsolete("Use LoadChunkColumnPriority()")]
    void LoadChunkColumnFast(int chunkX1, int chunkZ1, int chunkX2, int chunkZ2, ChunkLoadOptions options = null);

    //
    // Summary:
    //     Asynchronly high priority load an area of chunk columns at given coordinates.
    //     Make sure that X1<=X2 and Z1<=Z2
    //
    // Parameters:
    //   chunkX1:
    //
    //   chunkZ1:
    //
    //   chunkX2:
    //
    //   chunkZ2:
    //
    //   options:
    //     Additional loading options
    void LoadChunkColumnPriority(int chunkX1, int chunkZ1, int chunkX2, int chunkZ2, ChunkLoadOptions options = null);

    //
    // Summary:
    //     Asynchronly normal priority load a chunk column at given coordinate. No effect
    //     when already loaded.
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkZ:
    //
    //   keepLoaded:
    //     If true, the chunk will never get unloaded unless UnloadChunkColumn() is called
    void LoadChunkColumn(int chunkX, int chunkZ, bool keepLoaded = false);

    //
    // Summary:
    //     Generates chunk at given coordinate, completely bypassing any existing world
    //     data and caching methods, in other words generates, a chunk from scratch without
    //     keeping it in the list of loaded chunks
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkZ:
    //
    //   options:
    void PeekChunkColumn(int chunkX, int chunkZ, ChunkPeekOptions options);

    //
    // Summary:
    //     Asynchrounly checks if this chunk is currently loaded or in the savegame database.
    //     Calls the callback method with true or false once done looking up. Does not load
    //     the actual chunk data.
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkY:
    //
    //   chunkZ:
    //
    //   onTested:
    void TestChunkExists(int chunkX, int chunkY, int chunkZ, Action<bool> onTested);

    //
    // Summary:
    //     Asynchrounly checks if this map chunk is currently loaded or in the savegame
    //     database. Calls the callback method with true or false once done looking up.
    //     Does not load the actual map chunk data.
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkZ:
    //
    //   onTested:
    void TestMapChunkExists(int chunkX, int chunkZ, Action<bool> onTested);

    //
    // Summary:
    //     Asynchrounly checks if this mapregion is currently loaded or in the savegame
    //     database. Calls the callback method with true or false once done looking up.
    //     Does not load the actual map region data.
    //
    // Parameters:
    //   regionX:
    //
    //   regionZ:
    //
    //   onTested:
    void TestMapRegionExists(int regionX, int regionZ, Action<bool> onTested);

    //
    // Summary:
    //     Send or Resend a loaded chunk to all connected players. Has no effect when the
    //     chunk is not loaded
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkY:
    //
    //   chunkZ:
    //
    //   onlyIfInRange:
    //     If true, the chunk will not be sent to connected players that are out of range
    //     from that chunk
    void BroadcastChunk(int chunkX, int chunkY, int chunkZ, bool onlyIfInRange = true);

    //
    // Summary:
    //     Returns true if the server sent chunk at given coords to player and has it not
    //     unloaded yet
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkY:
    //
    //   chunkZ:
    //
    //   player:
    bool HasChunk(int chunkX, int chunkY, int chunkZ, IServerPlayer player);

    //
    // Summary:
    //     Send or Resend a loaded chunk to a connected player. Has no effect when the chunk
    //     is not loaded
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkY:
    //
    //   chunkZ:
    //
    //   player:
    //
    //   onlyIfInRange:
    //     If true, the chunk will not be sent to connected players that are out of range
    //     from that chunk
    void SendChunk(int chunkX, int chunkY, int chunkZ, IServerPlayer player, bool onlyIfInRange = true);

    //
    // Summary:
    //     Send or resent a loaded map chunk to all connected players. Has no effect when
    //     the map chunk is not loaded
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkZ:
    //
    //   onlyIfInRange:
    void ResendMapChunk(int chunkX, int chunkZ, bool onlyIfInRange);

    //
    // Summary:
    //     Unloads a column of chunks at given coordinate independent of any nearby players
    //     and sends an appropriate unload packet to the player
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkZ:
    void UnloadChunkColumn(int chunkX, int chunkZ);

    //
    // Summary:
    //     Deletes a column of chunks at given coordinate from the save file. Also deletes
    //     the map chunk at the same coordinate (but keeps the map region). Also unloads
    //     the chunk in the same process. Also deletes all entities in this chunk
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkZ:
    void DeleteChunkColumn(int chunkX, int chunkZ);

    //
    // Summary:
    //     Deletes a map region at given coordinate from the save file
    //
    // Parameters:
    //   regionX:
    //
    //   regionZ:
    void DeleteMapRegion(int regionX, int regionZ);

    //
    // Summary:
    //     Finds the first y position that is solid ground to stand on. Returns null if
    //     the chunk is not loaded.
    //
    // Parameters:
    //   posX:
    //
    //   posZ:
    int? GetSurfacePosY(int posX, int posZ);

    //
    // Summary:
    //     Permanently sets the default spawnpoint
    //
    // Parameters:
    //   x:
    //     X coordinate of new spawnpoint
    //
    //   y:
    //     Y coordinate of new spawnpoint
    //
    //   z:
    //     Z coordinate of new spawnpoint
    void SetDefaultSpawnPosition(int x, int y, int z);

    //
    // Summary:
    //     Get the ID of a certain BlockType
    //
    // Parameters:
    //   name:
    //     Name of the BlockType
    //
    // Returns:
    //     ID of the BlockType
    int GetBlockId(AssetLocation name);

    //
    // Summary:
    //     Floods the chunk column with sunlight. Only works on full chunk columns.
    //
    // Parameters:
    //   chunks:
    //
    //   chunkX:
    //
    //   chunkZ:
    void SunFloodChunkColumnForWorldGen(IWorldChunk[] chunks, int chunkX, int chunkZ);

    //
    // Summary:
    //     Spreads the chunk columns light into neighbour chunks and vice versa. Only works
    //     on full chunk columns.
    //
    // Parameters:
    //   chunks:
    //
    //   chunkX:
    //
    //   chunkZ:
    void SunFloodChunkColumnNeighboursForWorldGen(IWorldChunk[] chunks, int chunkX, int chunkZ);

    //
    // Summary:
    //     Does a complete relighting of the cuboid deliminated by given min/max pos. Completely
    //     resends all affected chunk columns to all connected nearby clients.
    //
    // Parameters:
    //   minPos:
    //
    //   maxPos:
    void FullRelight(BlockPos minPos, BlockPos maxPos);

    //
    // Summary:
    //     Does a complete relighting of the cuboid deliminated by given min/max pos. Can
    //     completely resend all affected chunk columns to all connected nearby clients.
    //
    //
    // Parameters:
    //   minPos:
    //
    //   maxPos:
    //
    //   sendToClients:
    void FullRelight(BlockPos minPos, BlockPos maxPos, bool sendToClients);

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
    [Obsolete("Use api.World.GetBlockAccessor instead")]
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
    [Obsolete("Use api.World.GetBlockAccessorBulkUpdate instead")]
    IBulkBlockAccessor GetBlockAccessorBulkUpdate(bool synchronize, bool relight, bool debug = false);

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
    [Obsolete("Use api.World.GetBlockAccessorRevertable instead")]
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
    [Obsolete("Use api.World.GetBlockAccessorPrefetch instead")]
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
    [Obsolete("Use api.World.GetCachingBlockAccessor instead")]
    ICachingBlockAccessor GetCachingBlockAccessor(bool synchronize, bool relight);

    //
    // Summary:
    //     Creates columns of empty chunks in the specified dimension
    //
    // Parameters:
    //   cx:
    //
    //   cz:
    //
    //   dim:
    void CreateChunkColumnForDimension(int cx, int cz, int dim);

    //
    // Summary:
    //     Loads chunk columns for the specified dimension
    //
    // Parameters:
    //   cx:
    //
    //   cz:
    //
    //   dim:
    void LoadChunkColumnForDimension(int cx, int cz, int dim);

    //
    // Summary:
    //     API access to force send a chunk column in any dimension
    //
    // Parameters:
    //   player:
    //
    //   cx:
    //
    //   cz:
    //
    //   dimension:
    void ForceSendChunkColumn(IServerPlayer player, int cx, int cz, int dimension);

    //
    // Summary:
    //     Test if the given map region exists, can only be called before EnumServerRunPhase.RunGame.
    //     Used in InitWorldgen to check chunks before the games is running
    //
    // Parameters:
    //   regionX:
    //
    //   regionZ:
    bool BlockingTestMapRegionExists(int regionX, int regionZ);

    //
    // Summary:
    //     Test if the given map chunk exists, can only be called before EnumServerRunPhase.RunGame.
    //     Used in InitWorldgen to check chunks before the games is running
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkZ:
    bool BlockingTestMapChunkExists(int chunkX, int chunkZ);

    //
    // Summary:
    //     Load the given map chunk , can only be called before EnumServerRunPhase.RunGame.
    //     This only loads and deserializes the chunk data, you need to call .Dispose()
    //     after you do not need them anymore Used in InitWorldgen to check chunks before
    //     the games is running
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkZ:
    IServerChunk[] BlockingLoadChunkColumn(int chunkX, int chunkZ);
}
#if false // Decompilation log
'182' items in cache
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
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Newtonsoft.Json.dll'
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
