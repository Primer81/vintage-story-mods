#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

//
// Summary:
//     Allows for definitions of behaviors of a block that can be applied to any block
public abstract class BlockBehavior : CollectibleBehavior
{
    //
    // Summary:
    //     The block for this behavior instance.
    public Block block;

    public BlockBehavior(Block block)
        : base(block)
    {
        this.block = block;
    }

    //
    // Summary:
    //     Called when a survival player has broken the block. The default behavior removes
    //     the block and spawns the block drops.
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   byPlayer:
    //
    //   handling:
    public virtual void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
    }

    //
    // Summary:
    //     When the player has presed the middle mouse click on the block. The default behavior
    //     returns an itemstack with the block itself
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   handling:
    public virtual ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }

    //
    // Summary:
    //     Is called before a block is broken, should return what items this block should
    //     drop. Return null or empty array for no drops. The default behavior drops whatever
    //     block.Drops is set to.
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   byPlayer:
    //
    //   dropChanceMultiplier:
    //
    //   handling:
    public virtual ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }

    //
    // Summary:
    //     Called when any of it's 6 neighbour blocks has been changed
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   neibpos:
    //
    //   handling:
    public virtual void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
    }

    //
    // Summary:
    //     Used by torches and other blocks to check if it can attach itself to that block.
    //     The default behavior tests for SideSolid[blockFace.Index]
    //
    // Parameters:
    //   world:
    //
    //   block:
    //
    //   pos:
    //
    //   blockFace:
    //
    //   handling:
    //
    //   attachmentArea:
    public virtual bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handling, Cuboidi attachmentArea = null)
    {
        handling = EnumHandling.PassThrough;
        return false;
    }

    //
    // Summary:
    //     Should return if supplied entitytype is allowed to spawn on this block
    //
    // Parameters:
    //   blockAccessor:
    //
    //   pos:
    //
    //   type:
    //
    //   sc:
    //
    //   handling:
    public virtual bool CanCreatureSpawnOn(IBlockAccessor blockAccessor, BlockPos pos, EntityProperties type, BaseSpawnConditions sc, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return false;
    }

    //
    // Summary:
    //     For any block that can be rotated, this method should be implemented to return
    //     the correct rotated block code. It is used by the world edit tool for allowing
    //     block data rotations
    //
    // Parameters:
    //   angle:
    //
    //   handling:
    public virtual AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }

    //
    // Summary:
    //     For any block that can be flipped upside down, this method should be implemented
    //     to return the correctly flipped block code. It is used by the world edit tool
    //     for allowing block data rotations
    //
    // Parameters:
    //   handling:
    public virtual AssetLocation GetVerticallyFlippedBlockCode(ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }

    //
    // Summary:
    //     For any block that can be flipped vertically, this method should be implemented
    //     to return the correctly flipped block code. It is used by the world edit tool
    //     for allowing block data rotations
    //
    // Parameters:
    //   axis:
    //
    //   handling:
    public virtual AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return null;
    }

    //
    // Summary:
    //     Used to determine if a block should be treated like air when placing blocks.
    //     (e.g. used for tallgrass)
    //
    // Parameters:
    //   block:
    //
    //   handling:
    public virtual bool IsReplacableBy(Block block, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return false;
    }

    //
    // Summary:
    //     Everytime the player moves by 8 blocks (or rather leaves the current 8-grid),
    //     a scan of all blocks 32x32x32 blocks around the player is initiated and this
    //     method is called. If the method returns true, the block is registered to a client
    //     side game ticking for spawning particles and such. This method will be called
    //     everytime the player left his current 8-grid area. The default behavior is to
    //     return true if block.ParticleProperties are set
    //
    // Parameters:
    //   world:
    //
    //   byPlayer:
    //
    //   pos:
    //
    //   handling:
    public virtual bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer byPlayer, BlockPos pos, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return false;
    }

    public virtual void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
    {
    }

    //
    // Summary:
    //     Always called when a block has been removed through whatever method, except during
    //     worldgen or via ExchangeBlock() For Worldgen you might be able to use TryPlaceBlockForWorldGen()
    //     to attach custom behaviors during placement/removal The default behavior is to
    //     delete the block entity, if this block has any
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   handling:
    public virtual void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
    }

    public virtual void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs, ref EnumHandling handled)
    {
    }

    //
    // Summary:
    //     When a player does a right click while targeting this placed block. Should return
    //     true if the event is handled, so that other events can occur, e.g. eating a held
    //     item if the block is not interactable with.
    //
    // Parameters:
    //   world:
    //
    //   byPlayer:
    //
    //   blockSel:
    //
    //   handling:
    public virtual bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return false;
    }

    //
    // Summary:
    //     Called by the block info HUD for displaying additional information
    //
    // Parameters:
    //   world:
    //
    //   pos:
    //
    //   forPlayer:
    public virtual string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
    {
        return "";
    }

    public virtual void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
    }

    public virtual WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return new WorldInteraction[0];
    }

    public virtual void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
    }

    public virtual bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return false;
    }

    public virtual bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return false;
    }

    //
    // Summary:
    //     Step 1: Called when the player attempts to place this block. The default behavior
    //     calls Block.DoPlaceBlock(). If returned true and default behavior has not been
    //     prevented, the game will next call CanPlaceBlock(). If that method also returns
    //     true and default behavior has not been overriden, DoPlaceBlock() will get called.
    //
    //
    // Parameters:
    //   world:
    //
    //   byPlayer:
    //
    //   itemstack:
    //
    //   blockSel:
    //
    //   handling:
    //
    //   failureCode:
    public virtual bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
    {
        handling = EnumHandling.PassThrough;
        return false;
    }

    //
    // Summary:
    //     Step 2: Test if the block can be placed
    //
    // Parameters:
    //   world:
    //
    //   byPlayer:
    //
    //   blockSel:
    //
    //   handling:
    //
    //   failureCode:
    public virtual bool CanPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
    {
        handling = EnumHandling.PassThrough;
        return false;
    }

    //
    // Summary:
    //     Step 3: Place the block. Return false if it cannot be placed (but you should
    //     rather return false in CanPlaceBlock).
    //
    // Parameters:
    //   world:
    //
    //   byPlayer:
    //
    //   blockSel:
    //
    //   byItemStack:
    //
    //   handling:
    public virtual bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return false;
    }

    //
    // Summary:
    //     Step 4: Block was placed. Always called when a block has been placed through
    //     whatever method, except during worldgen or via ExchangeBlock() Be aware that
    //     the vanilla OnBlockPlaced block behavior is to spawn the block entity if any
    //     is associated with this block, so this code will not get executed if you set
    //     handled to PreventDefault or Last
    //
    // Parameters:
    //   world:
    //
    //   blockPos:
    //
    //   handling:
    public virtual void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
    }

    public virtual void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe, ref EnumHandling handled)
    {
        handled = EnumHandling.PassThrough;
    }

    public virtual string GetHeldBlockInfo(IWorldAccessor world, ItemSlot inSlot)
    {
        return "";
    }

    public virtual AssetLocation GetSnowCoveredBlockCode(float snowLevel)
    {
        return null;
    }

    //
    // Summary:
    //     If this is less than 1.0, will slow down mining of the given block (e.g. used
    //     for reinforced blocks)
    public virtual float GetMiningSpeedModifier(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
    {
        return 1f;
    }

    public virtual void GetPlacedBlockName(StringBuilder sb, IWorldAccessor world, BlockPos pos)
    {
    }

    [Obsolete("Use GetRetention() instead")]
    public virtual int GetHeatRetention(BlockPos pos, BlockFacing facing, ref EnumHandling handled)
    {
        return 0;
    }

    public virtual int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type, ref EnumHandling handled)
    {
        return 0;
    }

    public virtual float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos, ref EnumHandling handled)
    {
        return 0f;
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
