#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Server;

//
// Summary:
//     Contains methods to hook into various server processes
public interface IServerEventAPI : IEventAPI
{
    //
    // Summary:
    //     Called when just loaded (or generated) a full chunkcolumn
    event ChunkColumnBeginLoadChunkThread BeginChunkColumnLoadChunkThread;

    //
    // Summary:
    //     Called whenever the server loaded from disk or fully generated a chunkcolumn
    event ChunkColumnLoadedDelegate ChunkColumnLoaded;

    //
    // Summary:
    //     Called just before a chunk column is about to get unloaded. On shutdown this
    //     method is called for all loaded chunks, so this method can get called tens of
    //     thousands of times there, beware
    event ChunkColumnUnloadDelegate ChunkColumnUnloaded;

    //
    // Summary:
    //     Registers a handler to be called every time a player uses a block. The methods
    //     return value determines if the player may place/break this block.
    event CanUseDelegate CanUseBlock;

    //
    // Summary:
    //     Called when the server attempts to spawn given entity. Return false to deny spawning.
    event TrySpawnEntityDelegate OnTrySpawnEntity;

    //
    // Summary:
    //     Called when a player interacts with an entity
    event OnInteractDelegate OnPlayerInteractEntity;

    //
    // Summary:
    //     Called when a new player joins
    event PlayerDelegate PlayerCreate;

    //
    // Summary:
    //     Called when a player got respawned
    event PlayerDelegate PlayerRespawn;

    //
    // Summary:
    //     Called when a player joins
    event PlayerDelegate PlayerJoin;

    //
    // Summary:
    //     Called when a player joins and his client is now fully loaded and ready to play
    event PlayerDelegate PlayerNowPlaying;

    //
    // Summary:
    //     Called when a player intentionally leaves
    event PlayerDelegate PlayerLeave;

    //
    // Summary:
    //     Called whenever a player disconnects (timeout, leave, disconnect, kick, etc.).
    event PlayerDelegate PlayerDisconnect;

    //
    // Summary:
    //     Called when a player wrote a chat message
    event PlayerChatDelegate PlayerChat;

    //
    // Summary:
    //     Called when a player died
    event PlayerDeathDelegate PlayerDeath;

    //
    // Summary:
    //     Whenever a player switched his game mode or has it switched for him
    event PlayerDelegate PlayerSwitchGameMode;

    //
    // Summary:
    //     Fired before a player changes their active slot (such as selected hotbar slot).
    //     Allows for the event to be cancelled depending on the return value.
    event Vintagestory.API.Common.Func<IServerPlayer, ActiveSlotChangeEventArgs, EnumHandling> BeforeActiveSlotChanged;

    //
    // Summary:
    //     Fired after a player changes their active slot (such as selected hotbar slot).
    event Action<IServerPlayer, ActiveSlotChangeEventArgs> AfterActiveSlotChanged;

    //
    // Summary:
    //     Triggered after assets have been loaded and parsed and registered, but before
    //     they are declared to be ready - e.g. you can add more behaviors here, or make
    //     other code-based changes to properties read from JSONs
    //     Note: modsystems should register for this in a Start() method not StartServerSide():
    //     the AssetsFinalizer event is fired before StartServerSide() is reached
    [Obsolete("Override Method Modsystem.AssetsFinalize instead")]
    event Action AssetsFinalizers;

    //
    // Summary:
    //     Triggered after the game world data has been loaded. At this point all blocks
    //     are loaded and the Map size is known.
    //
    //     In 1.17+ do NOT use this server event to add or update behaviors or attributes
    //     or other fixed properties of any block, item or entity, in code (additional to
    //     what is read from JSON). Instead, code which needs to do that should be registered
    //     for event sapi.Event.AssetsFinalizers. See VSSurvivalMod system BlockReinforcement.cs
    //     for an example.
    event Action SaveGameLoaded;

    //
    // Summary:
    //     Triggered after a savegame has been created - i.e. when a new world was created
    event Action SaveGameCreated;

    //
    // Summary:
    //     Triggered when starting up worldgen during server startup (as the final stage
    //     of the WorldReady EnumServerRunPhase)
    event Action WorldgenStartup;

    //
    // Summary:
    //     Triggered when a new multithreaded physics thread starts (for example, use this
    //     to initialise any ThreadStatic element which must be initialised per-thread)
    event Action PhysicsThreadStart;

    //
    // Summary:
    //     Triggered before the game world data is being saved to disk
    event Action GameWorldSave;

    //
    // Summary:
    //     Called when something wants to pause the server, e.g. the autosave system. This
    //     method will be called every 50ms until all delegates return Ready state. Timeout
    //     is 60 seconds.
    event SuspendServerDelegate ServerSuspend;

    //
    // Summary:
    //     Called when something wants to resume execution of the server, e.g. the autosave
    //     system
    event ResumeServerDelegate ServerResume;

    //
    // Summary:
    //     Registers a method to be called every time a player places a block
    event BlockPlacedDelegate DidPlaceBlock;

    //
    // Summary:
    //     Registers a handler to be called every time a player places a block. The methods
    //     return value determines if the player may place/break this block. When returning
    //     false the client will be notified and the action reverted
    event CanPlaceOrBreakDelegate CanPlaceOrBreakBlock;

    //
    // Summary:
    //     Called when a block should got broken now (that has been broken by a player).
    //     Set handling to PreventDefault to handle the block breaking yourself. Otherwise
    //     the engine will break the block (= either call heldItemstack.Collectible.OnBlockBrokenWith
    //     when player holds something in his hands or block.OnBlockBroken).
    event BlockBreakDelegate BreakBlock;

    //
    // Summary:
    //     Registers a method to be called every time a player deletes a block. Called after
    //     the block was already broken
    event BlockBrokenDelegate DidBreakBlock;

    //
    // Summary:
    //     Registers a method to be called every time a player uses a block
    event BlockUsedDelegate DidUseBlock;

    //
    // Summary:
    //     Returns the list of currently registered map chunk generator handlers for given
    //     world type. Returns an array of handler lists. Each element in the array represents
    //     all the handlers for one worldgenpass (see EnumWorldGenPass) When world type
    //     is null, all handlers are returned
    //
    // Parameters:
    //   worldType:
    //     "standard" for the vanilla world generator
    IWorldGenHandler GetRegisteredWorldGenHandlers(string worldType);

    bool TriggerTrySpawnEntity(IBlockAccessor blockAccessor, ref EntityProperties properties, Vec3d position, long herdId);

    //
    // Summary:
    //     If you require neighbour chunk data during world generation, you have to register
    //     to this event to receive access to the chunk generator thread. This method is
    //     only called once during server startup.
    //
    // Parameters:
    //   handler:
    void GetWorldgenBlockAccessor(WorldGenThreadDelegate handler);

    //
    // Summary:
    //     Triggered before the first chunk, map chunk or map region is generated, given
    //     that the passed on world type has been selected. Called right after the save
    //     game has been loaded.
    //
    // Parameters:
    //   handler:
    //
    //   forWorldType:
    void InitWorldGenerator(Action handler, string forWorldType);

    //
    // Summary:
    //     Event that is triggered whenever a new column of chunks is being generated. It
    //     is always called before the ChunkGenerator event
    //
    // Parameters:
    //   handler:
    //
    //   forWorldType:
    //     For which world types to use this generator
    void MapChunkGeneration(MapChunkGeneratorDelegate handler, string forWorldType);

    //
    // Summary:
    //     Event that is triggered whenever a new 16x16 section of column of chunks is being
    //     generated. It is always called before the ChunkGenerator and before the MapChunkGeneration
    //     event
    //
    // Parameters:
    //   handler:
    //
    //   forWorldType:
    //     For which world types to use this generator
    void MapRegionGeneration(MapRegionGeneratorDelegate handler, string forWorldType);

    //
    // Summary:
    //     Vintagestory uses this method to generate the basic terrain (base terrain + rock
    //     strata + caves) in full columns. Only called once in pass EnumWorldGenPass.TerrainNoise.
    //     Register to this event if you need acces to a whole chunk column during inital
    //     generation.
    //
    // Parameters:
    //   handler:
    //
    //   pass:
    //
    //   forWorldType:
    //     For which world types to use this generator
    void ChunkColumnGeneration(ChunkColumnGenerationDelegate handler, EnumWorldGenPass pass, string forWorldType);

    //
    // Summary:
    //     Registers a method to be called by certain special worldgen triggers, for example
    //     Resonance Archives entrance staircase
    void WorldgenHook(WorldGenHookDelegate handler, string forWorldType, string hook);

    //
    // Summary:
    //     Trigger the special worldgen hook, with the name "hook", if it exists
    void TriggerWorldgenHook(string hook, IBlockAccessor blockAccessor, BlockPos pos, string param);

    //
    // Summary:
    //     Triggered whenever the server enters a new run phase. Since mods are only loaded
    //     during run phase "LoadGamePre" registering to any earlier event will get triggered.
    //
    //
    // Parameters:
    //   runPhase:
    //
    //   handler:
    void ServerRunPhase(EnumServerRunPhase runPhase, Action handler);

    //
    // Summary:
    //     Registers a method to be called every given interval
    //
    // Parameters:
    //   handler:
    //
    //   interval:
    void Timer(Action handler, double interval);

    object TriggerInitWorldGen();

    //
    // Summary:
    //     Triggers an immediate ClientAwarenessEvent for the specified player
    //
    // Parameters:
    //   player:
    void PlayerChunkTransition(IServerPlayer player);
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
