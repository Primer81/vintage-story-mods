#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

//
// Summary:
//     Basic class for block entities - a data structures to hold custom information
//     for blocks, e.g. for chests to hold it's contents
public abstract class BlockEntity
{
    protected List<long> TickHandlers;

    protected List<long> CallbackHandlers;

    //
    // Summary:
    //     The core API added to the block. Accessable after initialization.
    public ICoreAPI Api;

    //
    // Summary:
    //     Position of the block for this block entity
    public BlockPos Pos;

    //
    // Summary:
    //     List of block entity behaviors associated with this block entity
    public List<BlockEntityBehavior> Behaviors = new List<BlockEntityBehavior>();

    private ITreeAttribute missingBlockTree;

    //
    // Summary:
    //     The block type at the position of the block entity. This poperty is updated by
    //     the engine if ExchangeBlock is called
    public Block Block { get; set; }

    //
    // Summary:
    //     Creats an empty instance. Use initialize to initialize it with the api.
    public BlockEntity()
    {
    }

    public T GetBehavior<T>() where T : class
    {
        for (int i = 0; i < Behaviors.Count; i++)
        {
            if (Behaviors[i] is T)
            {
                return Behaviors[i] as T;
            }
        }

        return null;
    }

    //
    // Summary:
    //     This method is called right after the block entity was spawned or right after
    //     it was loaded from a newly loaded chunk. You do have access to the world and
    //     its blocks at this point. However if this block entity already existed then FromTreeAttributes
    //     is called first! You should still call the base method to sets the this.api field
    //
    //
    // Parameters:
    //   api:
    public virtual void Initialize(ICoreAPI api)
    {
        Api = api;
        FrameProfilerUtil frameProfiler = api.World.FrameProfiler;
        if (frameProfiler != null && frameProfiler.Enabled)
        {
            foreach (BlockEntityBehavior behavior in Behaviors)
            {
                behavior.Initialize(api, behavior.properties);
                api.World.FrameProfiler.Mark("initbebehavior-" + behavior.GetType());
            }

            return;
        }

        foreach (BlockEntityBehavior behavior2 in Behaviors)
        {
            behavior2.Initialize(api, behavior2.properties);
        }
    }

    public virtual void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
    {
        Block = block;
        BlockEntityBehaviorType[] blockEntityBehaviors = block.BlockEntityBehaviors;
        foreach (BlockEntityBehaviorType blockEntityBehaviorType in blockEntityBehaviors)
        {
            if (worldForResolve.ClassRegistry.GetBlockEntityBehaviorClass(blockEntityBehaviorType.Name) == null)
            {
                worldForResolve.Logger.Warning(Lang.Get("Block entity behavior {0} for block {1} not found", blockEntityBehaviorType.Name, block.Code));
                continue;
            }

            if (blockEntityBehaviorType.properties == null)
            {
                blockEntityBehaviorType.properties = new JsonObject(new JObject());
            }

            BlockEntityBehavior blockEntityBehavior = worldForResolve.ClassRegistry.CreateBlockEntityBehavior(this, blockEntityBehaviorType.Name);
            blockEntityBehavior.properties = blockEntityBehaviorType.properties;
            Behaviors.Add(blockEntityBehavior);
        }
    }

    //
    // Summary:
    //     Registers a game tick listener that does the disposing for you when the Block
    //     is removed
    //
    // Parameters:
    //   onGameTick:
    //
    //   millisecondInterval:
    //
    //   initialDelayOffsetMs:
    public virtual long RegisterGameTickListener(Action<float> onGameTick, int millisecondInterval, int initialDelayOffsetMs = 0)
    {
        if (Dimensions.ShouldNotTick(Pos, Api))
        {
            return 0L;
        }

        long num = Api.Event.RegisterGameTickListener(onGameTick, TickingExceptionHandler, millisecondInterval, initialDelayOffsetMs);
        if (TickHandlers == null)
        {
            TickHandlers = new List<long>(1);
        }

        TickHandlers.Add(num);
        return num;
    }

    //
    // Summary:
    //     Removes a registered game tick listener from the game.
    //
    // Parameters:
    //   listenerId:
    //     the ID of the listener to unregister.
    public virtual void UnregisterGameTickListener(long listenerId)
    {
        Api.Event.UnregisterGameTickListener(listenerId);
        TickHandlers?.Remove(listenerId);
    }

    public virtual void UnregisterAllTickListeners()
    {
        if (TickHandlers == null)
        {
            return;
        }

        foreach (long tickHandler in TickHandlers)
        {
            Api.Event.UnregisterGameTickListener(tickHandler);
        }
    }

    //
    // Summary:
    //     Registers a delayed callback that does the disposing for you when the Block is
    //     removed
    //
    // Parameters:
    //   OnDelayedCallbackTick:
    //
    //   millisecondInterval:
    public virtual long RegisterDelayedCallback(Action<float> OnDelayedCallbackTick, int millisecondInterval)
    {
        long num = Api.Event.RegisterCallback(OnDelayedCallbackTick, millisecondInterval);
        if (CallbackHandlers == null)
        {
            CallbackHandlers = new List<long>();
        }

        CallbackHandlers.Add(num);
        return num;
    }

    //
    // Summary:
    //     Unregisters a callback. This is usually done automatically.
    //
    // Parameters:
    //   listenerId:
    //     The ID of the callback listiner.
    public virtual void UnregisterDelayedCallback(long listenerId)
    {
        Api.Event.UnregisterCallback(listenerId);
        CallbackHandlers?.Remove(listenerId);
    }

    public virtual void TickingExceptionHandler(Exception e)
    {
        if (Api == null)
        {
            throw new Exception("Api was null while ticking a BlockEntity: " + GetType().FullName);
        }

        Api.Logger.Error("At position " + Pos?.ToString() + " for block " + (Block?.Code.ToShortString() ?? "(missing)") + " a " + GetType().Name + " threw an error when ticked:");
        Api.Logger.Error(e);
    }

    //
    // Summary:
    //     Called when the block at this position was removed in some way. Removes the game
    //     tick listeners, so still call the base method
    public virtual void OnBlockRemoved()
    {
        UnregisterAllTickListeners();
        if (CallbackHandlers != null)
        {
            foreach (long callbackHandler in CallbackHandlers)
            {
                Api.Event.UnregisterCallback(callbackHandler);
            }
        }

        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.OnBlockRemoved();
        }
    }

    //
    // Summary:
    //     Called when blockAccessor.ExchangeBlock() is used to exchange this block. Make
    //     sure to call the base method when overriding.
    //
    // Parameters:
    //   block:
    public virtual void OnExchanged(Block block)
    {
        if (block != Block)
        {
            MarkDirty(redrawOnClient: true);
        }

        Block = block;
    }

    //
    // Summary:
    //     Called when the block was broken in survival mode or through explosions and similar.
    //     Generally in situations where you probably want to drop the block entity contents,
    //     if it has any
    public virtual void OnBlockBroken(IPlayer byPlayer = null)
    {
        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.OnBlockBroken(byPlayer);
        }
    }

    //
    // Summary:
    //     Called by the undo/redo system, after calling FromTreeAttributes
    public virtual void HistoryStateRestore()
    {
    }

    //
    // Summary:
    //     Called when the chunk the block entity resides in was unloaded. Removes the game
    //     tick listeners, so still call the base method
    public virtual void OnBlockUnloaded()
    {
        try
        {
            if (Api != null)
            {
                UnregisterAllTickListeners();
                if (CallbackHandlers != null)
                {
                    foreach (long callbackHandler in CallbackHandlers)
                    {
                        Api.Event.UnregisterCallback(callbackHandler);
                    }
                }
            }

            foreach (BlockEntityBehavior behavior in Behaviors)
            {
                behavior.OnBlockUnloaded();
            }
        }
        catch (Exception)
        {
            Api.Logger.Error("At position " + Pos?.ToString() + " for block " + (Block?.Code.ToShortString() ?? "(missing)") + " a " + GetType().Name + " threw an error when unloaded");
            throw;
        }
    }

    //
    // Summary:
    //     Called when the block entity just got placed, not called when it was previously
    //     placed and the chunk is loaded. Always called after Initialize()
    public virtual void OnBlockPlaced(ItemStack byItemStack = null)
    {
        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.OnBlockPlaced(byItemStack);
        }
    }

    //
    // Summary:
    //     Called when saving the world or when sending the block entity data to the client.
    //     When overriding, make sure to still call the base method.
    //
    // Parameters:
    //   tree:
    public virtual void ToTreeAttributes(ITreeAttribute tree)
    {
        ICoreAPI api = Api;
        if ((api == null || api.Side != EnumAppSide.Client) && Block.IsMissing)
        {
            foreach (KeyValuePair<string, IAttribute> item in missingBlockTree)
            {
                tree[item.Key] = item.Value;
            }

            return;
        }

        tree.SetInt("posx", Pos.X);
        tree.SetInt("posy", Pos.InternalY);
        tree.SetInt("posz", Pos.Z);
        if (Block != null)
        {
            tree.SetString("blockCode", Block.Code.ToShortString());
        }

        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.ToTreeAttributes(tree);
        }
    }

    //
    // Summary:
    //     Called when loading the world or when receiving block entity from the server.
    //     When overriding, make sure to still call the base method. FromTreeAttributes
    //     is always called before Initialize() is called, so the this.api field is not
    //     yet set!
    //
    // Parameters:
    //   tree:
    //
    //   worldAccessForResolve:
    //     Use this api if you need to resolve blocks/items. Not suggested for other purposes,
    //     as the residing chunk may not be loaded at this point
    public virtual void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        Pos = new BlockPos(tree.GetInt("posx"), tree.GetInt("posy"), tree.GetInt("posz"));
        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.FromTreeAttributes(tree, worldAccessForResolve);
        }

        if (worldAccessForResolve.Side == EnumAppSide.Server && Block.IsMissing)
        {
            missingBlockTree = tree;
        }
    }

    //
    // Summary:
    //     Called whenever a blockentity packet at the blocks position has been received
    //     from the client
    //
    // Parameters:
    //   fromPlayer:
    //
    //   packetid:
    //
    //   data:
    public virtual void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
    {
        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.OnReceivedClientPacket(fromPlayer, packetid, data);
        }
    }

    //
    // Summary:
    //     Called whenever a blockentity packet at the blocks position has been received
    //     from the server
    //
    // Parameters:
    //   packetid:
    //
    //   data:
    public virtual void OnReceivedServerPacket(int packetid, byte[] data)
    {
        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.OnReceivedServerPacket(packetid, data);
        }
    }

    //
    // Summary:
    //     When called on Server: Will resync the block entity with all its TreeAttribute
    //     to the client, but will not resend or redraw the block unless specified. When
    //     called on Client: Triggers a block changed event on the client, but will not
    //     redraw the block unless specified.
    //
    // Parameters:
    //   redrawOnClient:
    //     When true, the block is also marked dirty and thus redrawn. When called serverside
    //     a dirty block packet is sent to the client for it to be redrawn
    //
    //   skipPlayer:
    public virtual void MarkDirty(bool redrawOnClient = false, IPlayer skipPlayer = null)
    {
        if (Api != null)
        {
            Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
            if (redrawOnClient)
            {
                Api.World.BlockAccessor.MarkBlockDirty(Pos, skipPlayer);
            }
        }
    }

    //
    // Summary:
    //     Called by the block info HUD for displaying additional information
    //
    // Parameters:
    //   forPlayer:
    //
    //   dsc:
    public virtual void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.GetBlockInfo(forPlayer, dsc);
        }
    }

    //
    // Summary:
    //     Called by the worldedit schematic exporter so that it can also export the mappings
    //     of items/blocks stored inside blockentities
    //
    // Parameters:
    //   blockIdMapping:
    //
    //   itemIdMapping:
    public virtual void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
    {
        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
        }
    }

    //
    // Summary:
    //     Called by the blockschematic loader so that you may fix any blockid/itemid mappings
    //     against the mapping of the savegame, if you store any collectibles in this blockentity.
    //     Note: Some vanilla blocks resolve randomized contents in this method. Hint: Use
    //     itemstack.FixMapping() to do the job for you.
    //
    // Parameters:
    //   worldForNewMappings:
    //
    //   oldBlockIdMapping:
    //
    //   oldItemIdMapping:
    //
    //   schematicSeed:
    //     If you need some sort of randomness consistency accross an imported schematic,
    //     you can use this value
    [Obsolete("Use the variant with resolveImports parameter")]
    public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
    {
        OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports: true);
    }

    //
    // Summary:
    //     Called by the blockschematic loader so that you may fix any blockid/itemid mappings
    //     against the mapping of the savegame, if you store any collectibles in this blockentity.
    //     Note: Some vanilla blocks resolve randomized contents in this method. Hint: Use
    //     itemstack.FixMapping() to do the job for you.
    //
    // Parameters:
    //   worldForNewMappings:
    //
    //   oldBlockIdMapping:
    //
    //   oldItemIdMapping:
    //
    //   schematicSeed:
    //     If you need some sort of randomness consistency accross an imported schematic,
    //     you can use this value
    //
    //   resolveImports:
    //     Turn it off to spawn structures as they are. For example, in this mode, instead
    //     of traders, their meta spawners will spawn
    public virtual void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
    {
        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);
        }
    }

    //
    // Summary:
    //     Let's you add your own meshes to a chunk. Don't reuse the meshdata instance anywhere
    //     in your code. Return true to skip the default mesh. WARNING! The Tesselator runs
    //     in a seperate thread, so you have to make sure the fields and methods you access
    //     inside this method are thread safe.
    //
    // Parameters:
    //   mesher:
    //     The chunk mesh, add your stuff here
    //
    //   tessThreadTesselator:
    //     If you need to tesselate something, you should use this tesselator, since using
    //     the main thread tesselator can cause race conditions and crash the game
    //
    // Returns:
    //     True to skip default mesh, false to also add the default mesh
    public virtual bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
    {
        bool flag = false;
        for (int i = 0; i < Behaviors.Count; i++)
        {
            flag |= Behaviors[i].OnTesselation(mesher, tessThreadTesselator);
        }

        return flag;
    }

    //
    // Summary:
    //     Called when this block entity was placed by a schematic, either through world
    //     edit or by worldgen
    //
    // Parameters:
    //   api:
    //
    //   blockAccessor:
    //
    //   pos:
    //
    //   replaceBlocks:
    //
    //   centerrockblockid:
    //
    //   layerBlock:
    //     If block.CustomBlockLayerHandler is true and the block is below the surface,
    //     this value is set
    //
    //   resolveImports:
    //     Turn it off to spawn structures as they are. For example, in this mode, instead
    //     of traders, their meta spawners will spawn
    public virtual void OnPlacementBySchematic(ICoreServerAPI api, IBlockAccessor blockAccessor, BlockPos pos, Dictionary<int, Dictionary<int, int>> replaceBlocks, int centerrockblockid, Block layerBlock, bool resolveImports)
    {
        Pos = pos.Copy();
        for (int i = 0; i < Behaviors.Count; i++)
        {
            Behaviors[i].OnPlacementBySchematic(api, blockAccessor, pos, replaceBlocks, centerrockblockid, layerBlock, resolveImports);
        }
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
