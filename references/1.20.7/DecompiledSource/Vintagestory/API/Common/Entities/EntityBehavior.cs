#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common.Entities;

//
// Summary:
//     Defines a basic entity behavior that can be attached to entities
public abstract class EntityBehavior
{
    public Entity entity;

    public string ProfilerName { get; private set; }

    public EntityBehavior(Entity entity)
    {
        this.entity = entity;
        ProfilerName = "done-behavior-" + PropertyName();
    }

    //
    // Summary:
    //     Initializes the entity.
    //     If your code modifies the supplied attributes (not recommended!), then your changes
    //     will apply to all entities of the same type.
    //
    // Parameters:
    //   properties:
    //     The properties of this entity.
    //
    //   attributes:
    //     The attributes of this entity.
    public virtual void Initialize(EntityProperties properties, JsonObject attributes)
    {
    }

    //
    // Summary:
    //     Called after initializing all the behaviors in case they need to cross-refer
    //     to each other or set some initial values only at spawn-time
    public virtual void AfterInitialized(bool onFirstSpawn)
    {
    }

    //
    // Summary:
    //     The event fired when a game ticks over.
    //
    // Parameters:
    //   deltaTime:
    public virtual void OnGameTick(float deltaTime)
    {
    }

    //
    // Summary:
    //     The event fired when the entity is spawned (not called when loaded from the savegame).
    public virtual void OnEntitySpawn()
    {
    }

    //
    // Summary:
    //     The event fired when the entity is loaded from disk (not called during spawn)
    public virtual void OnEntityLoaded()
    {
    }

    //
    // Summary:
    //     The event fired when the entity is despawned.
    //
    // Parameters:
    //   despawn:
    //     The reason the entity despawned.
    public virtual void OnEntityDespawn(EntityDespawnData despawn)
    {
    }

    //
    // Summary:
    //     The name of the property tied to this entity behavior.
    public abstract string PropertyName();

    //
    // Summary:
    //     The event fired when the entity recieves damage.
    //
    // Parameters:
    //   damageSource:
    //     The source of the damage
    //
    //   damage:
    //     The amount of the damage.
    public virtual void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
    {
    }

    //
    // Summary:
    //     When the entity got revived (only for players and traders currently)
    public virtual void OnEntityRevive()
    {
    }

    //
    // Summary:
    //     The event fired when the entity falls to the ground.
    //
    // Parameters:
    //   lastTerrainContact:
    //     the point which the entity was previously on the ground.
    //
    //   withYMotion:
    //     The vertical motion the entity had before landing on the ground.
    public virtual void OnFallToGround(Vec3d lastTerrainContact, double withYMotion)
    {
    }

    //
    // Summary:
    //     The event fired when the entity recieves saturation.
    //
    // Parameters:
    //   saturation:
    //     The amount of saturation recieved.
    //
    //   foodCat:
    //     The category of food recieved.
    //
    //   saturationLossDelay:
    //     The delay before the loss of saturation.
    //
    //   nutritionGainMultiplier:
    public virtual void OnEntityReceiveSaturation(float saturation, EnumFoodCategory foodCat = EnumFoodCategory.Unknown, float saturationLossDelay = 10f, float nutritionGainMultiplier = 1f)
    {
    }

    //
    // Summary:
    //     The event fired when the server position is changed.
    //
    // Parameters:
    //   isTeleport:
    //     Whether or not this entity was teleported.
    //
    //   handled:
    //     How this event is handled.
    public virtual void OnReceivedServerPos(bool isTeleport, ref EnumHandling handled)
    {
    }

    //
    // Summary:
    //     gets the drops for this specific entity.
    //
    // Parameters:
    //   world:
    //     The world of this entity
    //
    //   pos:
    //     The block position of the entity.
    //
    //   byPlayer:
    //     The player this entity was killed by.
    //
    //   handling:
    //     How this event was handled.
    //
    // Returns:
    //     the items dropped from this entity
    public virtual ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }

    //
    // Summary:
    //     The event fired when the state of the entity is changed.
    //
    // Parameters:
    //   beforeState:
    //     The previous state.
    //
    //   handling:
    //     How this event was handled.
    public virtual void OnStateChanged(EnumEntityState beforeState, ref EnumHandling handling)
    {
    }

    //
    // Summary:
    //     The notify method bubbled up from entity.Notify()
    //
    // Parameters:
    //   key:
    //
    //   data:
    public virtual void Notify(string key, object data)
    {
    }

    //
    // Summary:
    //     Gets the information text when highlighting this entity.
    //
    // Parameters:
    //   infotext:
    //     The supplied stringbuilder information.
    public virtual void GetInfoText(StringBuilder infotext)
    {
    }

    //
    // Summary:
    //     The event fired when the entity dies.
    //
    // Parameters:
    //   damageSourceForDeath:
    //     The source of damage for the entity.
    public virtual void OnEntityDeath(DamageSource damageSourceForDeath)
    {
    }

    //
    // Summary:
    //     The event fired when the entity is interacted with by the player.
    //
    // Parameters:
    //   byEntity:
    //     The entity it was interacted with.
    //
    //   itemslot:
    //     The item slot involved (if any)
    //
    //   hitPosition:
    //     The hit position of the entity.
    //
    //   mode:
    //     The interaction mode for the entity.
    //
    //   handled:
    //     How this event is handled.
    public virtual void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
    {
    }

    //
    // Summary:
    //     The event fired when the server receives a packet.
    //
    // Parameters:
    //   player:
    //     The server player.
    //
    //   packetid:
    //     the packet id.
    //
    //   data:
    //     The data contents.
    //
    //   handled:
    //     How this event is handled.
    public virtual void OnReceivedClientPacket(IServerPlayer player, int packetid, byte[] data, ref EnumHandling handled)
    {
    }

    //
    // Summary:
    //     The event fired when the client receives a packet.
    //
    // Parameters:
    //   packetid:
    //
    //   data:
    //
    //   handled:
    public virtual void OnReceivedServerPacket(int packetid, byte[] data, ref EnumHandling handled)
    {
    }

    //
    // Summary:
    //     Called when a player looks at the entity with interaction help enabled
    //
    // Parameters:
    //   world:
    //
    //   es:
    //
    //   player:
    //
    //   handled:
    public virtual WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
    {
        handled = EnumHandling.PassThrough;
        return null;
    }

    public virtual void DidAttack(DamageSource source, EntityAgent targetEntity, ref EnumHandling handled)
    {
        handled = EnumHandling.PassThrough;
    }

    public virtual void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
    {
    }

    public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, bool resolveImports)
    {
    }

    public virtual void ToBytes(bool forClient)
    {
    }

    //
    // Summary:
    //     This method is not called on the server side
    //
    // Parameters:
    //   isSync:
    public virtual void FromBytes(bool isSync)
    {
    }

    //
    // Summary:
    //     Can be used by the /entity command or maybe other commands, to test behaviors
    //
    //     The argument will be an object provided by TextCommandCallingArgs, which can
    //     then be cast to the desired type e.g. int
    public virtual void TestCommand(object arg)
    {
    }

    public virtual bool TryGiveItemStack(ItemStack itemstack, ref EnumHandling handling)
    {
        return false;
    }

    public virtual void OnTesselation(ref Shape entityShape, string shapePathForLogging, ref bool shapeIsCloned, ref string[] willDeleteElements)
    {
    }

    public virtual ITexPositionSource GetTextureSource(ref EnumHandling handling)
    {
        return null;
    }

    public virtual bool IntersectsRay(Ray ray, AABBIntersectionTest interesectionTester, out double intersectionDistance, ref int selectionBoxIndex, ref EnumHandling handled)
    {
        intersectionDistance = 0.0;
        return false;
    }

    public virtual void OnTesselated()
    {
    }

    public virtual void UpdateColSelBoxes()
    {
    }

    public virtual float GetTouchDistance(ref EnumHandling handling)
    {
        return 0f;
    }

    public virtual string GetName(ref EnumHandling handling)
    {
        return null;
    }
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
