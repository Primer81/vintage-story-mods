#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

//
// Summary:
//     Provides read/write access to the blocks of a world
public interface IBlockAccessor
{
    //
    // Summary:
    //     Width, Length and Height of a chunk
    [Obsolete("Use GlobalConstants.ChunkSize instead.  Fetching a property in inner-loop code is needlessly inefficient!")]
    int ChunkSize { get; }

    //
    // Summary:
    //     Width and Length of a region in blocks
    int RegionSize { get; }

    //
    // Summary:
    //     X Size of the world in blocks
    int MapSizeX { get; }

    //
    // Summary:
    //     Y Size of the world in blocks
    int MapSizeY { get; }

    //
    // Summary:
    //     Z Size of the world in blocks
    int MapSizeZ { get; }

    int RegionMapSizeX { get; }

    int RegionMapSizeY { get; }

    int RegionMapSizeZ { get; }

    //
    // Summary:
    //     Whether to update the snow accum map on a SetBlock()
    bool UpdateSnowAccumMap { get; set; }

    //
    // Summary:
    //     Size of the world in blocks
    Vec3i MapSize { get; }

    //
    // Summary:
    //     Retrieve chunk at given chunk position (= divide block position by chunk size)
    //
    //     For dimension awareness, chunkY would need to be based on BlockPos.InternalY
    //     / chunksize or else explicitly include the dimensionId multiplied by GlobalConstants.DimensionSizeInChunks
    //
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkY:
    //
    //   chunkZ:
    IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ);

    //
    // Summary:
    //     Retrieve chunk at given chunk position, returns null if chunk is not loaded
    //
    // Parameters:
    //   chunkIndex3D:
    IWorldChunk GetChunk(long chunkIndex3D);

    //
    // Summary:
    //     Retrieves a map region at given region position, returns null if region is not
    //     loaded
    //
    // Parameters:
    //   regionX:
    //
    //   regionZ:
    IMapRegion GetMapRegion(int regionX, int regionZ);

    //
    // Summary:
    //     Retrieve chunk at given block position, returns null if chunk is not loaded
    //
    // Parameters:
    //   pos:
    IWorldChunk GetChunkAtBlockPos(BlockPos pos);

    //
    // Summary:
    //     Get the block id of the block at the given world coordinate
    //
    // Parameters:
    //   pos:
    int GetBlockId(BlockPos pos);

    //
    // Summary:
    //     Get the block type of the block at the given world coordinate, dimension aware.
    //     Will never return null. For air blocks or invalid coordinates you'll get a block
    //     instance with block code "air" and id 0 Same as Vintagestory.API.Common.IBlockAccessor.GetBlock(Vintagestory.API.MathTools.BlockPos,System.Int32)
    //     with BlockLayersAccess.Default as layer
    //
    // Parameters:
    //   pos:
    Block GetBlock(BlockPos pos);

    //
    // Summary:
    //     Direct raw coordinate access to blocks. PLEASE NOTE: The caller has to ensure
    //     dimension awareness (i.e. pos.InternalY when using BlockPos)
    //     Gets the block type of the block at the given world coordinate.
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
    //   layer:
    //     The block layer to retrieve from. See also Vintagestory.API.Common.BlockLayersAccess
    //
    //
    // Returns:
    //     Never null. For unpopulated locations or invalid coordinates you'll get a block
    //     instance with block code "air" and id 0
    Block GetBlockRaw(int x, int y, int z, int layer = 0);

    //
    // Summary:
    //     Get block type at given world coordinate, dimension aware. Will never return
    //     null. For airblocks or invalid coordinates you'll get a block instance with block
    //     code "air" and id 0
    //     Reads the block from the specified layer(s), see BlockLayersAccess documentation
    //     for details.
    //
    // Parameters:
    //   pos:
    //
    //   layer:
    //     blocks layer e.g. solid, fluid etc.
    Block GetBlock(BlockPos pos, int layer);

    //
    // Summary:
    //     Same as Vintagestory.API.Common.IBlockAccessor.GetBlock(Vintagestory.API.MathTools.BlockPos,System.Int32)
    //     with BlockLayersAccess.MostSolid as layer
    //
    // Parameters:
    //   pos:
    Block GetMostSolidBlock(BlockPos pos);

    //
    // Summary:
    //     Retrieve chunk at given block position, returns null if chunk is not loaded
    //
    // Parameters:
    //   posX:
    //
    //   posY:
    //
    //   posZ:
    [Obsolete("Please use BlockPos version instead, for dimension awareness")]
    IWorldChunk GetChunkAtBlockPos(int posX, int posY, int posZ);

    //
    // Summary:
    //     Get the block id of the block at the given world coordinate
    //
    // Parameters:
    //   posX:
    //
    //   posY:
    //
    //   posZ:
    [Obsolete("Please use BlockPos version instead, for dimension awareness")]
    int GetBlockId(int posX, int posY, int posZ);

    //
    // Summary:
    //     Get the block type of the block at the given world coordinate. For invalid or
    //     unloaded coordinates this method returns null.
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
    //   layer:
    //     Block layer
    //
    // Returns:
    //     ID of the block at the given position
    [Obsolete("Please use BlockPos version instead, for dimension awareness")]
    Block GetBlockOrNull(int x, int y, int z, int layer = 4);

    //
    // Summary:
    //     Get the block type of the block at the given world coordinate. Will never return
    //     null. For airblocks or invalid coordinates you'll get a block instance with block
    //     code "air" and id 0
    //     Reads the block from the specified layer(s), see Vintagestory.API.Common.BlockLayersAccess
    //     documentation for details.
    //     If this must be used even though it's deprecated, please consider using .GetBlockRaw()
    //     instead where calling code is explicitly dimension-aware
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
    //   layer:
    //     blocks layer e.g. solid, fluid etc.
    //
    // Returns:
    //     ID of the block at the given position
    [Obsolete("Please use BlockPos version instead, for dimension awareness")]
    Block GetBlock(int x, int y, int z, int layer);

    //
    // Summary:
    //     Get the block type of the block at the given world coordinate. Will never return
    //     null. For air blocks or invalid coordinates you'll get a block instance with
    //     block code "air" and id 0 Same as Vintagestory.API.Common.IBlockAccessor.GetBlock(Vintagestory.API.MathTools.BlockPos,System.Int32)
    //     with BlockLayersAccess.Default as layer
    //
    // Parameters:
    //   posX:
    //
    //   posY:
    //
    //   posZ:
    [Obsolete("Please use BlockPos version instead, for dimension awareness")]
    Block GetBlock(int posX, int posY, int posZ);

    //
    // Summary:
    //     Same as Vintagestory.API.Common.IBlockAccessor.GetBlock(System.Int32,System.Int32,System.Int32,System.Int32)
    //     with BlockLayersAccess.MostSolid as layer
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    [Obsolete("Please use BlockPos version instead, for dimension awareness")]
    Block GetMostSolidBlock(int x, int y, int z);

    //
    // Summary:
    //     A method to iterate over blocks in an area. Less overhead than when calling GetBlock(pos)
    //     many times. If there is liquids in the liquid layer, the onBlock method will
    //     be called twice. Currently used for more efficient collision testing.
    //     Currently NOT dimensionally aware
    //
    // Parameters:
    //   minPos:
    //
    //   maxPos:
    //
    //   onBlock:
    //     The method in which you want to check for the block, whatever it may be.
    //
    //   centerOrder:
    //     If true, the blocks will be ordered by the distance to the center position
    void WalkBlocks(BlockPos minPos, BlockPos maxPos, Action<Block, int, int, int> onBlock, bool centerOrder = false);

    //
    // Summary:
    //     A method to search for a given block in an area
    //     Currently NOT dimensionally aware
    //
    // Parameters:
    //   minPos:
    //
    //   maxPos:
    //
    //   onBlock:
    //     Return false to stop the search
    //
    //   onChunkMissing:
    //     Called when a missing/unloaded chunk was encountered
    void SearchBlocks(BlockPos minPos, BlockPos maxPos, ActionConsumable<Block, BlockPos> onBlock, Action<int, int, int> onChunkMissing = null);

    //
    // Summary:
    //     A method to search for a given fluid block in an area
    //     Currently NOT dimensionally aware
    //
    // Parameters:
    //   minPos:
    //
    //   maxPos:
    //
    //   onBlock:
    //     Return false to stop the search
    //
    //   onChunkMissing:
    //     Called when a missing/unloaded chunk was encountered
    void SearchFluidBlocks(BlockPos minPos, BlockPos maxPos, ActionConsumable<Block, BlockPos> onBlock, Action<int, int, int> onChunkMissing = null);

    //
    // Summary:
    //     Calls given handler if it encounters one or more generated structure at given
    //     position (read from mapregions, assuming a max structure size of 256x256x256)
    //
    //
    // Parameters:
    //   pos:
    //
    //   onStructure:
    void WalkStructures(BlockPos pos, Action<GeneratedStructure> onStructure);

    //
    // Summary:
    //     Calls given handler if it encounters one or more generated structure that intersect
    //     any position inside minpos->maxpos (read from mapregions, assuming a max structure
    //     size of 256x256x256)
    //
    // Parameters:
    //   minpos:
    //
    //   maxpos:
    //
    //   onStructure:
    void WalkStructures(BlockPos minpos, BlockPos maxpos, Action<GeneratedStructure> onStructure);

    //
    // Summary:
    //     Set a block at the given position. Use blockid 0 to clear that position from
    //     any blocks. Marks the chunk dirty so that it gets saved to disk during shutdown
    //     or next autosave. If called with a fluid block, the fluid will automatically
    //     get set in the fluid layer, and the solid layer will be emptied.
    //
    // Parameters:
    //   blockId:
    //
    //   pos:
    void SetBlock(int blockId, BlockPos pos);

    //
    // Summary:
    //     Sets a block to given layer. Can only use "BlockLayersAccess.Solid" or "BlockLayersAccess.Liquid".
    //     Use id 0 to clear a block from given position. Marks the chunk dirty so that
    //     it gets saved to disk during shutdown or next autosave.
    //
    // Parameters:
    //   blockId:
    //
    //   pos:
    //
    //   layer:
    void SetBlock(int blockId, BlockPos pos, int layer);

    //
    // Summary:
    //     Set a block at the given position. Use blockid 0 to clear that position from
    //     any blocks. Marks the chunk dirty so that it gets saved to disk during shutdown
    //     or next autosave. If called with a fluid block, the fluid will automatically
    //     get set in the fluid layer, and the solid layer will be emptied.
    //
    // Parameters:
    //   blockId:
    //
    //   pos:
    //
    //   byItemstack:
    //     If set then it will be passed onto the block.OnBlockPlaced method
    void SetBlock(int blockId, BlockPos pos, ItemStack byItemstack);

    //
    // Summary:
    //     Set a block at the given position without calling OnBlockRemoved or OnBlockPlaced,
    //     which prevents any block entity from being removed or placed. Marks the chunk
    //     dirty so that it gets saved to disk during shutdown or next autosave. Should
    //     only be used if you want to prevent any block entity deletion at this position.
    //
    //     This also, for example, does not change a block's reinforcement level, useful
    //     for openable blocks such as doors, gates etc
    void ExchangeBlock(int blockId, BlockPos pos);

    //
    // Summary:
    //     Removes the block at given position and calls Block.GetDrops(), Block.OnBreakBlock()
    //     and Block.OnNeighbourBlockChange() for all neighbours. Drops the items that are
    //     return from Block.GetDrops()
    //
    // Parameters:
    //   pos:
    //
    //   byPlayer:
    //
    //   dropQuantityMultiplier:
    void BreakBlock(BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f);

    //
    // Summary:
    //     Client Side: Will render the block breaking decal on that block. If the remaining
    //     block resistance reaches 0, will call break block Server Side: Broadcasts a package
    //     to all nearby clients to update the block damage of this block for rendering
    //     the decal (note: there is currently no server side list of current block damages,
    //     these are client side only at the moemnt)
    //
    // Parameters:
    //   pos:
    //
    //   facing:
    //
    //   damage:
    void DamageBlock(BlockPos pos, BlockFacing facing, float damage);

    //
    // Summary:
    //     Get the Block object of a certain block ID. Returns null when not found.
    //
    // Parameters:
    //   blockId:
    //     The block ID to search for
    //
    // Returns:
    //     BlockType object
    Block GetBlock(int blockId);

    //
    // Summary:
    //     Get the Block object of for given block code. Returns null when not found.
    //
    // Parameters:
    //   code:
    Block GetBlock(AssetLocation code);

    //
    // Summary:
    //     Spawn block entity at this position. Does not place it's corresponding block,
    //     you have to this yourself.
    //
    // Parameters:
    //   classname:
    //
    //   position:
    //
    //   byItemStack:
    void SpawnBlockEntity(string classname, BlockPos position, ItemStack byItemStack = null);

    //
    // Summary:
    //     Adds pre-initialized block entity to the world. Does not call CreateBehaviors/Initialize/OnBlockPlaced
    //     on the block entity. This is a very low level method for block entity spawning,
    //     normally you'd want to use Vintagestory.API.Common.IBlockAccessor.SpawnBlockEntity(System.String,Vintagestory.API.MathTools.BlockPos,Vintagestory.API.Common.ItemStack)
    //
    //
    // Parameters:
    //   be:
    void SpawnBlockEntity(BlockEntity be);

    //
    // Summary:
    //     Permanently removes any block entity at this postion. Does not remove it's corresponding
    //     block, you have to do this yourself. Marks the chunk dirty so that it gets saved
    //     to disk during shutdown or next autosave.
    //
    // Parameters:
    //   position:
    void RemoveBlockEntity(BlockPos position);

    //
    // Summary:
    //     Retrieve the block entity at given position. Returns null if there is no block
    //     entity at this position
    //
    // Parameters:
    //   position:
    BlockEntity GetBlockEntity(BlockPos position);

    //
    // Summary:
    //     Retrieve the block entity at given position. Returns null if there is no block
    //     entity at this position
    //
    // Parameters:
    //   position:
    //
    // Type parameters:
    //   T:
    T GetBlockEntity<T>(BlockPos position) where T : BlockEntity;

    //
    // Summary:
    //     Checks if the position is inside the maps boundaries
    //
    // Parameters:
    //   posX:
    //
    //   posY:
    //
    //   posZ:
    [Obsolete("Please use BlockPos version instead, for dimension awareness")]
    bool IsValidPos(int posX, int posY, int posZ);

    //
    // Summary:
    //     Checks if the position is inside the maps boundaries
    //
    // Parameters:
    //   pos:
    bool IsValidPos(BlockPos pos);

    //
    // Summary:
    //     Checks if this position can be traversed by a normal player (returns false for
    //     outside map or not yet loaded chunks)
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    [Obsolete("Better to use dimension-aware version")]
    bool IsNotTraversable(double x, double y, double z);

    //
    // Summary:
    //     Checks if this position can be traversed by a normal player (returns false for
    //     outside map or not yet loaded chunks) Dimension-aware version
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    bool IsNotTraversable(double x, double y, double z, int dim);

    //
    // Summary:
    //     Checks if this position can be traversed by a normal player (returns false for
    //     outside map or not yet loaded chunks)
    //
    // Parameters:
    //   pos:
    bool IsNotTraversable(BlockPos pos);

    //
    // Summary:
    //     Calling this method has no effect in normal block acessors except for: - Bulk
    //     update block accessor: Sets all blocks, relight all affected one chunks in one
    //     go and send blockupdates to clients in a packed format. - World gen block accessor:
    //     To Recalculate the heightmap in of all updated blocks in one go - Revertable
    //     block accessor: Same as bulk update block accessor plus stores a new history
    //     state.
    //
    // Returns:
    //     List of changed blocks
    List<BlockUpdate> Commit();

    //
    // Summary:
    //     For the bulk update block accessor reverts all the SetBlocks currently called
    //     since the last Commit()
    void Rollback();

    //
    // Summary:
    //     Server side call: Resends the block entity data (if present) to all clients.
    //     Triggers a block changed event on the client once received , but will not redraw
    //     the chunk. Marks also the chunk dirty so that it gets saved to disk during shutdown
    //     or next autosave.
    //     Client side call: No effect
    //
    // Parameters:
    //   pos:
    void MarkBlockEntityDirty(BlockPos pos);

    //
    // Summary:
    //     Triggers the method OnNeighbourBlockChange() to all neighbour blocks at given
    //     position
    //
    // Parameters:
    //   pos:
    void TriggerNeighbourBlockUpdate(BlockPos pos);

    //
    // Summary:
    //     Server side: Sends that block to the client (via bulk packet). Through that packet
    //     the client will do a SetBlock on that position (which triggers a redraw if oldblockid
    //     != newblockid).
    //     Client side: Triggers a block changed event and redraws the chunk
    //
    // Parameters:
    //   skipPlayer:
    //     Server side: Does not send the update to this player, Client Side: No effect
    //
    //
    //   pos:
    void MarkBlockDirty(BlockPos pos, IPlayer skipPlayer = null);

    //
    // Summary:
    //     Server side: Triggers a OnNeighbourBlockChange on that position and sends that
    //     block to the client (via bulk packet), through that packet the client will do
    //     a SetBlock on that position (which triggers a redraw if oldblockid != newblockid).
    //
    //     Client side: Triggers a block changed event and redraws the chunk. Deletes and
    //     re-create block entities
    //
    // Parameters:
    //   pos:
    void MarkBlockModified(BlockPos pos);

    //
    // Summary:
    //     Server Side: Same as MarkBlockDirty()
    //     Client Side: Same as MarkBlockDirty(), but also calls supplied delegate after
    //     the chunk has been re-retesselated. This can be used i.e. for block entities
    //     to dynamically switch between static models and dynamic models at exactly the
    //     right timing
    //
    // Parameters:
    //   pos:
    //
    //   OnRetesselated:
    void MarkBlockDirty(BlockPos pos, Action OnRetesselated);

    //
    // Summary:
    //     Returns the light level (0..32) at given position. If the chunk at that position
    //     is not loaded this method will return the default sunlight value
    //
    // Parameters:
    //   pos:
    //
    //   type:
    int GetLightLevel(BlockPos pos, EnumLightLevelType type);

    //
    // Summary:
    //     Returns the light level (0..32) at given position. If the chunk at that position
    //     is not loaded this method will return the default sunlight value
    //     Note this is not currently dimensionally aware
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   type:
    int GetLightLevel(int x, int y, int z, EnumLightLevelType type);

    //
    // Summary:
    //     Returns the light values at given position. XYZ component = block light rgb,
    //     W component = sun light brightness
    //     Note this is not currently dimensionally aware
    //
    // Parameters:
    //   posX:
    //
    //   posY:
    //
    //   posZ:
    Vec4f GetLightRGBs(int posX, int posY, int posZ);

    //
    // Summary:
    //     Returns the light values at given position. XYZ component = block light rgb,
    //     W component = sun light brightness
    //
    // Parameters:
    //   pos:
    Vec4f GetLightRGBs(BlockPos pos);

    //
    // Summary:
    //     Returns the light values at given position. bit 0-23: block rgb light, bit 24-31:
    //     sun light brightness
    //
    // Parameters:
    //   posX:
    //
    //   posY:
    //
    //   posZ:
    int GetLightRGBsAsInt(int posX, int posY, int posZ);

    //
    // Summary:
    //     Returns the topmost solid surface position at given x/z coordinate as it was
    //     during world generation. This map is not updated after placing/removing blocks
    //
    //     Note this is meaningless in dimensions other than the normal world
    //
    // Parameters:
    //   pos:
    int GetTerrainMapheightAt(BlockPos pos);

    //
    // Summary:
    //     Returns the topmost non-rain-permeable position at given x/z coordinate. This
    //     map is always updated after placing/removing blocks
    //     Note this is meaningless in dimensions other than the normal world
    //
    // Parameters:
    //   pos:
    int GetRainMapHeightAt(BlockPos pos);

    //
    // Summary:
    //     Returns a number of how many blocks away there is rain fall. Does a cheap 2D
    //     bfs up to x blocks away. Returns 99 if none was found within given blocks
    //
    // Parameters:
    //   pos:
    //
    //   horziontalSearchWidth:
    //     Horizontal search distance, 4 default
    //
    //   verticalSearchWidth:
    //     Vertical search distance, 1 default
    int GetDistanceToRainFall(BlockPos pos, int horziontalSearchWidth = 4, int verticalSearchWidth = 1);

    //
    // Summary:
    //     Returns the topmost non-rain-permeable position at given x/z coordinate. This
    //     map is always updated after placing/removing blocks
    //     Note this is meaningless in dimensions other than the normal world
    //
    // Parameters:
    //   posX:
    //
    //   posZ:
    int GetRainMapHeightAt(int posX, int posZ);

    //
    // Summary:
    //     Returns the map chunk at given chunk position
    //
    // Parameters:
    //   chunkPos:
    IMapChunk GetMapChunk(Vec2i chunkPos);

    //
    // Summary:
    //     Returns the map chunk at given chunk position
    //
    // Parameters:
    //   chunkX:
    //
    //   chunkZ:
    IMapChunk GetMapChunk(int chunkX, int chunkZ);

    //
    // Summary:
    //     Returns the map chunk at given block position
    //
    // Parameters:
    //   pos:
    IMapChunk GetMapChunkAtBlockPos(BlockPos pos);

    //
    // Summary:
    //     Returns the position's current climate conditions
    //
    // Parameters:
    //   pos:
    //
    //   mode:
    //     WorldGenValues = values as determined by the worldgenerator, NowValues = additionally
    //     modified to take season, day/night and hemisphere into account
    //
    //   totalDays:
    //     When mode == ForSuppliedDateValues then supply here the date. Not used param
    //     otherwise
    ClimateCondition GetClimateAt(BlockPos pos, EnumGetClimateMode mode = EnumGetClimateMode.NowValues, double totalDays = 0.0);

    //
    // Summary:
    //     Returns the position's climate conditions at specified date, making use of previously
    //     obtained worldgen climate conditions
    ClimateCondition GetClimateAt(BlockPos pos, ClimateCondition baseClimate, EnumGetClimateMode mode, double totalDays);

    //
    // Summary:
    //     Fast shortcut method for the clound renderer
    //
    // Parameters:
    //   pos:
    //
    //   climate:
    ClimateCondition GetClimateAt(BlockPos pos, int climate);

    //
    // Summary:
    //     Retrieves the wind speed for given position
    //
    // Parameters:
    //   pos:
    Vec3d GetWindSpeedAt(Vec3d pos);

    //
    // Summary:
    //     Retrieves the wind speed for given position
    //
    // Parameters:
    //   pos:
    Vec3d GetWindSpeedAt(BlockPos pos);

    //
    // Summary:
    //     Used by the chisel block when enough chiseled have been removed and the blocks
    //     light absorption changes as a result of that
    //
    // Parameters:
    //   oldAbsorption:
    //
    //   newAbsorption:
    //
    //   pos:
    void MarkAbsorptionChanged(int oldAbsorption, int newAbsorption, BlockPos pos);

    //
    // Summary:
    //     Call this on OnBlockBroken() when your block entity modifies the blocks light
    //     range. That way the lighting task can still retrieve the block entity before
    //     its gone.
    //
    // Parameters:
    //   oldLightHsV:
    //
    //   pos:
    void RemoveBlockLight(byte[] oldLightHsV, BlockPos pos);

    //
    // Summary:
    //     Add a decor block to the side of an existing block. Use air block (id 0) to remove
    //     a decor.
    //
    // Parameters:
    //   position:
    //
    //   onFace:
    //
    //   block:
    //
    // Returns:
    //     True if the decor was sucessfully set
    bool SetDecor(Block block, BlockPos position, BlockFacing onFace);

    //
    // Summary:
    //     Add a decor block to a specific sub-position on the side of an existing block.
    //     Use air block (id 0) to remove a decor.
    //
    // Parameters:
    //   position:
    //
    //   block:
    //
    //   decorIndex:
    //     You can get this value via Vintagestory.API.Common.BlockSelection.ToDecorIndex
    //     or via constructor Vintagestory.API.Common.DecorBits.#ctor(Vintagestory.API.MathTools.BlockFacing)
    //     or Vintagestory.API.Common.DecorBits.#ctor(Vintagestory.API.MathTools.BlockFacing,System.Int32,System.Int32,System.Int32).
    //     It can include a subPosition for cave art etc.
    //
    // Returns:
    //     True if the decor was sucessfully set
    bool SetDecor(Block block, BlockPos position, int decorIndex);

    //
    // Summary:
    //     Get a list of all decors at this position
    //
    // Parameters:
    //   position:
    //
    // Returns:
    //     null if this block has no decors. Otherwise a 6 element long list of decor blocks,
    //     any of which may be null if not set
    [Obsolete("Use Dictionary<int, Block> GetSubDecors(BlockPos position)")]
    Block[] GetDecors(BlockPos position);

    //
    // Summary:
    //     Get a list of all decors at this position
    //
    // Parameters:
    //   position:
    //
    // Returns:
    //     null if this block position has no decors. Otherwise, a Dictionary with the index
    //     being the faceAndSubposition (subposition used for cave art etc.), see Vintagestory.API.Common.DecorBits
    Dictionary<int, Block> GetSubDecors(BlockPos position);

    //
    // Summary:
    //     Retrieves a single decor at given position
    //
    // Parameters:
    //   pos:
    //
    //   decorIndex:
    //     You can get this value via Vintagestory.API.Common.BlockSelection.ToDecorIndex
    //     or via constructor Vintagestory.API.Common.DecorBits.#ctor(Vintagestory.API.MathTools.BlockFacing)
    //     or Vintagestory.API.Common.DecorBits.#ctor(Vintagestory.API.MathTools.BlockFacing,System.Int32,System.Int32,System.Int32)
    Block GetDecor(BlockPos pos, int decorIndex);

    //
    // Summary:
    //     Removes all decors at given position, drops items if set
    //
    // Parameters:
    //   pos:
    //
    //   side:
    //     If not null, breaks all the decor on given block face, otherwise the decor blocks
    //     on all sides are removed
    //
    //   decorIndex:
    //     If not null breaks only this part of the decor for give face. You can get this
    //     value via Vintagestory.API.Common.BlockSelection.ToDecorIndex or via constructor
    //     Vintagestory.API.Common.DecorBits.#ctor(Vintagestory.API.MathTools.BlockFacing)
    //     or Vintagestory.API.Common.DecorBits.#ctor(Vintagestory.API.MathTools.BlockFacing,System.Int32,System.Int32,System.Int32)
    //
    //
    // Returns:
    //     True if a decor was removed
    bool BreakDecor(BlockPos pos, BlockFacing side = null, int? decorIndex = null);

    //
    // Summary:
    //     Server: Marks this position as required for resending to the client Client: No
    //     effect
    //
    // Parameters:
    //   pos:
    void MarkChunkDecorsModified(BlockPos pos);

    //
    // Summary:
    //     Tests whether a side at the specified position is solid - testing both fluids
    //     layer (which could be ice) and solid blocks layer
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   facing:
    bool IsSideSolid(int x, int y, int z, BlockFacing facing);

    //
    // Summary:
    //     Used by World Edit to create previews, ships etc.
    IMiniDimension CreateMiniDimension(Vec3d position);
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
