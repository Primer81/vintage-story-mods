#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

//
// Summary:
//     Interface that allows custom model model meshing for items, blocks and entities
//
//     Texturing crash course:
//     1. Block, Item and Entity textures are loaded from json files in the form of
//     a CompositeTexture instance
//     2. After connecting to a game server, the client inserts all of these textures
//     into their type-respective texture atlasses
//     3. After insertion a "texture sub-id" is left behind in the CompositeTexture.Baked
//     Property
//     4. You can now find the position of the texture inside the atlas through the
//     Block/Item/Entity-TextureAtlasPositions arrays (teturesubid is the array key)
//
//
//     Shape Tesselation crash course:
//     1. Block and Item shapes are loaded from json files in the form of a CompositeShape
//     instance
//     2. A CompositeShape instance hold some block/item specific information as well
//     as an identifier to a Shape instance
//     4. After connecting to a game server, the client loads all shapes from the shape
//     folder then finds each blocks/items shape by its shape identifier
//     5. Result is a MeshData instance that holds all vertices, UV coords, colors and
//     etc. for each block
//     6. That meshdata instance is
//     a) Held as-is in memory for using during chunk tesselation (you can get a reference
//     to it through getDefaultBlockMesh())
//     b) "Compiled" to a Model for use during rendering in the gui.
//     Model Compilation means all it's mesh data is uploaded onto the graphcis through
//     a VAO and a ModelRef instance is left behind which
//     can be used by the RenderAPI to render it.
public interface ITesselatorAPI
{
    //
    // Summary:
    //     Tesselates a block for you using given blocks shape and texture configuration
    //
    //
    // Parameters:
    //   block:
    //
    //   modeldata:
    void TesselateBlock(Block block, out MeshData modeldata);

    //
    // Summary:
    //     Tesselates an item for you using given items shape and texture configuration
    //
    //
    // Parameters:
    //   item:
    //
    //   modeldata:
    void TesselateItem(Item item, out MeshData modeldata);

    //
    // Summary:
    //     Tesselates an item for you using given items shape and texture configuration
    //
    //
    // Parameters:
    //   item:
    //
    //   forShape:
    //
    //   modeldata:
    void TesselateItem(Item item, CompositeShape forShape, out MeshData modeldata);

    //
    // Summary:
    //     Tesselates an item for you using the items shape and your own defined texture
    //     configuration. You need to implement the ITextureSource yourself.
    //
    // Parameters:
    //   item:
    //
    //   modeldata:
    //
    //   texSource:
    void TesselateItem(Item item, out MeshData modeldata, ITexPositionSource texSource);

    //
    // Summary:
    //     Tesselate a shape based on its composite shape file
    //
    // Parameters:
    //   type:
    //
    //   name:
    //
    //   compositeShape:
    //
    //   modeldata:
    //
    //   texSource:
    //
    //   generalGlowLevel:
    //
    //   climateColorMapIndex:
    //
    //   seasonColorMapIndex:
    //
    //   quantityElements:
    //
    //   selectiveElements:
    void TesselateShape(string type, AssetLocation name, CompositeShape compositeShape, out MeshData modeldata, ITexPositionSource texSource, int generalGlowLevel = 0, byte climateColorMapIndex = 0, byte seasonColorMapIndex = 0, int? quantityElements = null, string[] selectiveElements = null);

    //
    // Summary:
    //     Turns a shape into a mesh data object that you can feed into the chunk tesselator
    //     or upload to the graphics card for rendering. Uses the given collectible texture
    //     configuration as texture source.
    //
    // Parameters:
    //   textureSourceCollectible:
    //
    //   shape:
    //
    //   modeldata:
    //
    //   meshRotationDeg:
    //
    //   quantityElements:
    //
    //   selectiveElements:
    void TesselateShape(CollectibleObject textureSourceCollectible, Shape shape, out MeshData modeldata, Vec3f meshRotationDeg = null, int? quantityElements = null, string[] selectiveElements = null);

    //
    // Summary:
    //     Turns a shape into a mesh data object that you can feed into the chunk tesselator
    //     or upload to the graphics card for rendering. Can be used to supply a custom
    //     texture source.
    //
    // Parameters:
    //   typeForLogging:
    //
    //   shapeBase:
    //
    //   modeldata:
    //
    //   texSource:
    //
    //   meshRotationDeg:
    //
    //   generalGlowLevel:
    //
    //   climateColorMapId:
    //
    //   seasonColorMapId:
    //
    //   quantityElements:
    //
    //   selectiveElements:
    void TesselateShape(string typeForLogging, Shape shapeBase, out MeshData modeldata, ITexPositionSource texSource, Vec3f meshRotationDeg = null, int generalGlowLevel = 0, byte climateColorMapId = 0, byte seasonColorMapId = 0, int? quantityElements = null, string[] selectiveElements = null);

    //
    // Summary:
    //     Turns a shape into a mesh data object that you can feed into the chunk tesselator
    //     or upload to the graphics card for rendering. Can be used to supply a custom
    //     texture source. Will add a customints array to the meshdata that holds each elements
    //     JointId for all its vertices (you will have to manually set the jointid for each
    //     element though)
    //
    // Parameters:
    //   typeForLogging:
    //
    //   shapeBase:
    //
    //   modeldata:
    //
    //   texSource:
    //
    //   rotation:
    //
    //   quantityElements:
    //
    //   selectiveElements:
    void TesselateShapeWithJointIds(string typeForLogging, Shape shapeBase, out MeshData modeldata, ITexPositionSource texSource, Vec3f rotation, int? quantityElements = null, string[] selectiveElements = null);

    //
    // Summary:
    //     Turns a shape into a mesh data object that you can feed into the chunk tesselator
    //     or upload to the graphics card for rendering. Can be used to supply a custom
    //     texture source. Will add a customints array to the meshdata that holds each elements
    //     JointId for all its vertices (you will have to manually set the jointid for each
    //     element though)
    //
    // Parameters:
    //   meta:
    //
    //   shapeBase:
    //
    //   modeldata:
    void TesselateShape(TesselationMetaData meta, Shape shapeBase, out MeshData modeldata);

    //
    // Summary:
    //     A helper method that turns a flat texture into its 1-voxel thick voxelized form
    //
    //
    // Parameters:
    //   texture:
    //
    //   atlasSize:
    //
    //   atlasPos:
    MeshData VoxelizeTexture(CompositeTexture texture, Size2i atlasSize, TextureAtlasPosition atlasPos);

    //
    // Summary:
    //     A helper method that turns a flat texture into its 1-voxel thick voxelized form
    //
    //
    // Parameters:
    //   texturePixels:
    //
    //   width:
    //
    //   height:
    //
    //   atlasSize:
    //
    //   pos:
    MeshData VoxelizeTexture(int[] texturePixels, int width, int height, Size2i atlasSize, TextureAtlasPosition pos);

    //
    // Summary:
    //     Returns the texture source from given block. This can be used to obtain the positions
    //     of the textures in the block texture atlas.
    //
    // Parameters:
    //   block:
    //
    //   altTextureNumber:
    //
    //   returnNullWhenMissing:
    ITexPositionSource GetTextureSource(Block block, int altTextureNumber = 0, bool returnNullWhenMissing = false);

    [Obsolete("Use GetTextureSource instead")]
    ITexPositionSource GetTexSource(Block block, int altTextureNumber = 0, bool returnNullWhenMissing = false);

    //
    // Summary:
    //     Returns the texture source from given item. This can be used to obtain the positions
    //     of the textures in the item texture atlas.
    //
    // Parameters:
    //   item:
    //
    //   returnNullWhenMissing:
    ITexPositionSource GetTextureSource(Item item, bool returnNullWhenMissing = false);

    //
    // Summary:
    //     Returns the texture source from given entity. This can be used to obtain the
    //     positions of the textures in the entity texture atlas.
    //
    // Parameters:
    //   entity:
    //
    //   extraTextures:
    //
    //   altTextureNumber:
    //
    //   returnNullWhenMissing:
    ITexPositionSource GetTextureSource(Entity entity, Dictionary<string, CompositeTexture> extraTextures = null, int altTextureNumber = 0, bool returnNullWhenMissing = false);
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
