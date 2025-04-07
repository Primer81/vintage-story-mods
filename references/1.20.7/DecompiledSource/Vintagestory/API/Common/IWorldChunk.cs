#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IWorldChunk
{
    bool Empty { get; set; }

    //
    // Summary:
    //     Holds a reference to the current map data of this chunk column
    IMapChunk MapChunk { get; }

    //
    // Summary:
    //     Holds all the blockids for each coordinate, access via index: (y * chunksize
    //     + z) * chunksize + x
    IChunkBlocks Data { get; }

    //
    // Summary:
    //     Use Vintagestory.API.Common.IWorldChunk.Data instead
    [Obsolete("Use Data field")]
    IChunkBlocks Blocks { get; }

    //
    // Summary:
    //     Holds all the lighting data for each coordinate, access via index: (y * chunksize
    //     + z) * chunksize + x
    IChunkLight Lighting { get; }

    //
    // Summary:
    //     Faster (non-blocking) access to blocks at the cost of sometimes returning 0 instead
    //     of the real block. Use Vintagestory.API.Common.IWorldChunk.Data if you need reliable
    //     block access. Also should only be used for reading. Currently used for the particle
    //     system.
    IChunkBlocks MaybeBlocks { get; }

    //
    // Summary:
    //     An array holding all Entities currently residing in this chunk. This array may
    //     be larger than the amount of entities in the chunk.
    Entity[] Entities { get; }

    //
    // Summary:
    //     Actual count of entities in this chunk
    int EntitiesCount { get; }

    //
    // Summary:
    //     An array holding block Entities currently residing in this chunk. This array
    //     may be larger than the amount of block entities in the chunk.
    Dictionary<BlockPos, BlockEntity> BlockEntities { get; set; }

    //
    // Summary:
    //     Returns a list of a in-chunk indexed positions of all light sources in this chunk
    HashSet<int> LightPositions { get; set; }

    //
    // Summary:
    //     Whether this chunk got unloaded
    bool Disposed { get; }

    //
    // Summary:
    //     Can be used to store non-serialized mod data that is only serialized into the
    //     standard moddata dictionary on unload. This prevents the need for constant serializing/deserializing.
    //     Useful when storing large amounts of data. Is not populated on chunk load, you
    //     need to populate it with stored data yourself using GetModData()
    Dictionary<string, object> LiveModData { get; set; }

    //
    // Summary:
    //     Blockdata and Light might be compressed, always call this method if you want
    //     to access these
    void Unpack();

    //
    // Summary:
    //     Like Unpack(), except it must be used readonly: the calling code promises not
    //     to write any changes to this chunk's blocks or lighting
    bool Unpack_ReadOnly();

    //
    // Summary:
    //     Like Unpack_ReadOnly(), except it actually reads and returns the block ID at
    //     index
    //     (Returns 0 if the chunk was disposed)
    int UnpackAndReadBlock(int index, int layer);

    //
    // Summary:
    //     Like Unpack_ReadOnly(), except it actually reads and returns the Light at index
    //
    //     (Returns 0 if the chunk was disposed)
    ushort Unpack_AndReadLight(int index);

    //
    // Summary:
    //     A version of Unpack_AndReadLight which also returns the lightSat
    //     (Returns 0 if the chunk was disposed)
    ushort Unpack_AndReadLight(int index, out int lightSat);

    //
    // Summary:
    //     Marks this chunk as modified. If called on server side it will be stored to disk
    //     on the next autosave or during shutdown, if called on client not much happens
    //     (but it will be preserved from packing for next ~8 seconds)
    void MarkModified();

    //
    // Summary:
    //     Marks this chunk as recently accessed. This will prevent the chunk from getting
    //     compressed by the in-memory chunk compression algorithm
    void MarkFresh();

    //
    // Summary:
    //     Adds an entity to the chunk.
    //
    // Parameters:
    //   entity:
    //     The entity to add.
    void AddEntity(Entity entity);

    //
    // Summary:
    //     Removes an entity from the chunk.
    //
    // Parameters:
    //   entityId:
    //     the ID for the entity
    //
    // Returns:
    //     Whether or not the entity was removed.
    bool RemoveEntity(long entityId);

    //
    // Summary:
    //     Allows setting of arbitrary, permanantly stored moddata of this chunk. When set
    //     on the server before the chunk is sent to the client, the data will also be sent
    //     to the client. When set on the client the data is discarded once the chunk gets
    //     unloaded
    //
    // Parameters:
    //   key:
    //
    //   data:
    void SetModdata(string key, byte[] data);

    //
    // Summary:
    //     Removes the permanently stored data.
    //
    // Parameters:
    //   key:
    void RemoveModdata(string key);

    //
    // Summary:
    //     Retrieve arbitrary, permantly stored mod data
    //
    // Parameters:
    //   key:
    byte[] GetModdata(string key);

    //
    // Summary:
    //     Allows setting of arbitrary, permanantly stored moddata of this chunk. When set
    //     on the server before the chunk is sent to the client, the data will also be sent
    //     to the client. When set on the client the data is discarded once the chunk gets
    //     unloaded
    //
    // Parameters:
    //   key:
    //
    //   data:
    //
    // Type parameters:
    //   T:
    void SetModdata<T>(string key, T data);

    //
    // Summary:
    //     Retrieve arbitrary, permantly stored mod data
    //
    // Parameters:
    //   key:
    //
    //   defaultValue:
    //
    // Type parameters:
    //   T:
    T GetModdata<T>(string key, T defaultValue = default(T));

    //
    // Summary:
    //     Retrieve a block from this chunk ignoring ice/water layer, performs Unpack()
    //     and a modulo operation on the position arg to get a local position in the 0..chunksize
    //     range (it's your job to pick out the right chunk before calling this method)
    //
    //
    // Parameters:
    //   world:
    //
    //   position:
    Block GetLocalBlockAtBlockPos(IWorldAccessor world, BlockPos position);

    Block GetLocalBlockAtBlockPos(IWorldAccessor world, int posX, int posY, int posZ, int layer);

    //
    // Summary:
    //     As GetLocalBlockAtBlockPos except lock-free, use it inside paired LockForReading(true/false)
    //     calls
    //
    // Parameters:
    //   world:
    //
    //   position:
    //
    //   layer:
    Block GetLocalBlockAtBlockPos_LockFree(IWorldAccessor world, BlockPos position, int layer = 0);

    //
    // Summary:
    //     Retrieve a block entity from this chunk
    //
    // Parameters:
    //   pos:
    BlockEntity GetLocalBlockEntityAtBlockPos(BlockPos pos);

    //
    // Summary:
    //     Sets a decor block to the side of an existing block. Use air block (id 0) to
    //     remove a decor.
    //
    // Parameters:
    //   index3d:
    //
    //   onFace:
    //
    //   block:
    //
    // Returns:
    //     False if there already exists a block in this position and facing
    bool SetDecor(Block block, int index3d, BlockFacing onFace);

    //
    // Summary:
    //     Sets a decor block to a specific sub-position on the side of an existing block.
    //     Use air block (id 0) to remove a decor.
    //
    // Parameters:
    //   block:
    //
    //   index3d:
    //
    //   faceAndSubposition:
    //
    // Returns:
    //     False if there already exists a block in this position and facing
    bool SetDecor(Block block, int index3d, int faceAndSubposition);

    //
    // Summary:
    //     If allowed by a player action, removes all decors at given position and calls
    //     OnBrokenAsDecor() on all selected decors and drops the items that are returned
    //     from Block.GetDrops()
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   side:
    //     If null, all the decor blocks on all sides are removed
    //
    //   decorIndex:
    //     If not null breaks only this part of the decor for give face. Requires side to
    //     be set.
    bool BreakDecor(IWorldAccessor world, BlockPos pos, BlockFacing side = null, int? decorIndex = null);

    //
    // Summary:
    //     Removes a decor block from given position, saves a few cpu cycles by not calculating
    //     index3d
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   index3d:
    //
    //   callOnBrokenAsDecor:
    //     When set to true it will call block.OnBrokenAsDecor(...) which is used to drop
    //     the decors of that block
    void BreakAllDecorFast(IWorldAccessor world, BlockPos pos, int index3d, bool callOnBrokenAsDecor = true);

    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    Block[] GetDecors(IBlockAccessor blockAccessor, BlockPos pos);

    //
    // Parameters:
    //   blockAccessor:
    //
    //   position:
    Dictionary<int, Block> GetSubDecors(IBlockAccessor blockAccessor, BlockPos position);

    Block GetDecor(IBlockAccessor blockAccessor, BlockPos pos, int decorIndex);

    //
    // Summary:
    //     Set entire Decors for a chunk - used in Server->Client updates
    //
    // Parameters:
    //   newDecors:
    void SetDecors(Dictionary<int, Block> newDecors);

    //
    // Summary:
    //     Adds extra selection boxes in case a decor block is attached at given position
    //
    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    //
    //   orig:
    Cuboidf[] AdjustSelectionBoxForDecor(IBlockAccessor blockAccessor, BlockPos pos, Cuboidf[] orig);

    //
    // Summary:
    //     Only to be implemented client side
    void FinishLightDoubleBuffering();

    //
    // Summary:
    //     Returns the higher light absorption between solids and fluids block layers
    //
    // Parameters:
    //   index3d:
    //
    //   blockPos:
    //
    //   blockTypes:
    int GetLightAbsorptionAt(int index3d, BlockPos blockPos, IList<Block> blockTypes);

    //
    // Summary:
    //     For bulk chunk GetBlock operations, allows the chunkDataLayers to be pre-locked
    //     for reading, instead of entering and releasing one lock per read
    //     Best used mainly on the server side unless you know what you are doing. The client-side
    //     Chunk Tesselator can need read-access to a chunk at any time so making heavy
    //     use of this would cause rendering delays on the client
    //     Make sure always to call ReleaseBulkReadLock() when finished. Use a try/finally
    //     block if necessary, and complete all read operations within 8 seconds
    void AcquireBlockReadLock();

    //
    // Summary:
    //     For bulk chunk GetBlock operations, allows the chunkDataLayers to be pre-locked
    //     for reading, instead of entering and releasing one lock per read
    //     Make sure always to call ReleaseBulkReadLock() when finished. Use a try/finally
    //     block if necessary, and complete all read operations within 8 seconds
    void ReleaseBlockReadLock();

    //
    // Summary:
    //     Free up chunk data and pool
    void Dispose();
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
