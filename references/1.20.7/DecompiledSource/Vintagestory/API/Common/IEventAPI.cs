#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

//
// Summary:
//     Events that are available on the server and the client
public interface IEventAPI
{
    //
    // Summary:
    //     Called when a player changed dimension
    event PlayerCommonDelegate PlayerDimensionChanged;

    //
    // Summary:
    //     Triggered when block access is tested, allows you to override the engine response
    event TestBlockAccessDelegate OnTestBlockAccess;

    //
    // Summary:
    //     Triggered when a new entity spawned
    event EntityDelegate OnEntitySpawn;

    //
    // Summary:
    //     Triggered when a new entity got loaded (either spawned or loaded from disk)
    event EntityDelegate OnEntityLoaded;

    event EntityDeathDelegate OnEntityDeath;

    //
    // Summary:
    //     Triggered when a new entity despawned
    event EntityDespawnDelegate OnEntityDespawn;

    //
    // Summary:
    //     Called whenever a chunk was marked dirty (as in, its blocks or light values have
    //     been modified or it got newly loaded or newly created)
    event ChunkDirtyDelegate ChunkDirty;

    //
    // Summary:
    //     Called whenever the server loaded from disk or newly generated a map region
    event MapRegionLoadedDelegate MapRegionLoaded;

    //
    // Summary:
    //     Called just before a map region is about to get unloaded. On shutdown this method
    //     is called for all loaded map regions.
    event MapRegionUnloadDelegate MapRegionUnloaded;

    //
    // Summary:
    //     Called whenever any method calls world.BlockAccessor.GetClimateAt(). Used by
    //     the survival mod to modify the rainfall and temperature values to adjust for
    //     seasonal and day/night temperature variations. Be sure to also register to OnGetClimateForDate.
    event OnGetClimateDelegate OnGetClimate;

    //
    // Summary:
    //     Called whenever any method calls world.BlockAccessor.GetWindSpeedAt(). Used by
    //     the survival mod to set the wind speed
    event OnGetWindSpeedDelegate OnGetWindSpeed;

    //
    // Summary:
    //     Called when a player tries to gridcraft something
    event MatchGridRecipeDelegate MatchesGridRecipe;

    //
    // Summary:
    //     There's 2 global event busses, 1 on the client and 1 on the server. This pushes
    //     an event onto the bus.
    //
    // Parameters:
    //   eventName:
    //
    //   data:
    void PushEvent(string eventName, IAttribute data = null);

    //
    // Summary:
    //     Registers a listener on the event bus. This is intended for mods as the game
    //     engine itself does not push any events.
    //
    // Parameters:
    //   OnEvent:
    //     The handler for the events
    //
    //   priority:
    //     Set this to a different value if you want to catch an event before/after another
    //     mod catches it
    //
    //   filterByEventName:
    //     If set, events only with given eventName are received
    void RegisterEventBusListener(EventBusListenerDelegate OnEvent, double priority = 0.5, string filterByEventName = null);

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
    //     Calls given method after every given interval until unregistered. The engine
    //     may call your method slightly later since these event are handled only during
    //     fixed interval game ticks. This overload includes an ErrorHandler callback, triggered
    //     if calling onGameTick throws an exception
    //
    // Parameters:
    //   onGameTick:
    //
    //   errorHandler:
    //
    //   millisecondInterval:
    //
    //   initialDelayOffsetMs:
    //
    // Returns:
    //     listenerId
    long RegisterGameTickListener(Action<float> onGameTick, Action<Exception> errorHandler, int millisecondInterval, int initialDelayOffsetMs = 0);

    //
    // Summary:
    //     Calls given method after every given interval until unregistered. The engine
    //     may call your method slightly later since these event are handled only during
    //     fixed interval game ticks.
    //
    // Parameters:
    //   onGameTick:
    //
    //   pos:
    //
    //   millisecondInterval:
    //
    //   initialDelayOffsetMs:
    //
    // Returns:
    //     listenerId
    long RegisterGameTickListener(Action<IWorldAccessor, BlockPos, float> onGameTick, BlockPos pos, int millisecondInterval, int initialDelayOffsetMs = 0);

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
    //     game ticks. This overload can be used to signify callbacks which do no harm if
    //     registered while the game is paused (otherwise, registering a callback while
    //     paused will produce an error in logs, or an intentional exception in Developer
    //     Mode)
    //
    // Parameters:
    //   OnTimePassed:
    //
    //   millisecondDelay:
    //
    //   permittedWhilePaused:
    //
    // Returns:
    //     listenerId
    long RegisterCallback(Action<float> OnTimePassed, int millisecondDelay, bool permittedWhilePaused);

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
    //     Removes a delayed callback
    //
    // Parameters:
    //   listenerId:
    void UnregisterCallback(long listenerId);

    //
    // Summary:
    //     Removes a game tick listener
    //
    // Parameters:
    //   listenerId:
    void UnregisterGameTickListener(long listenerId);

    //
    // Summary:
    //     Can be used to execute supplied method a frame later or can be called from a
    //     seperate thread to ensure some code is executed in the main thread. Calling this
    //     method is thread safe.
    //
    // Parameters:
    //   action:
    //
    //   code:
    //     Task category identifier for the frame profiler
    void EnqueueMainThreadTask(Action action, string code);

    void TriggerPlayerDimensionChanged(IPlayer player);

    void TriggerEntityDeath(Entity entity, DamageSource damageSourceForDeath);

    bool TriggerMatchesRecipe(IPlayer forPlayer, GridRecipe gridRecipe, ItemSlot[] ingredients, int gridWidth);
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
