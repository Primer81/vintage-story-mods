#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

//
// Summary:
//     Entity texture Atlas.
public interface ITextureAtlasAPI
{
    TextureAtlasPosition this[AssetLocation textureLocation] { get; }

    //
    // Summary:
    //     The texture atlas position of the "unknown.png" texture
    TextureAtlasPosition UnknownTexturePosition { get; }

    //
    // Summary:
    //     Size of one block texture atlas
    Size2i Size { get; }

    //
    // Summary:
    //     As configured in the clientsettings.json divided by the texture atlas size
    float SubPixelPaddingX { get; }

    float SubPixelPaddingY { get; }

    //
    // Summary:
    //     Returns the default texture atlas position for all blocks, referenced by the
    //     texturesubid
    TextureAtlasPosition[] Positions { get; }

    //
    // Summary:
    //     Returns the list of currently loaded texture atlas ids
    List<LoadedTexture> AtlasTextures { get; }

    //
    // Summary:
    //     Reserves a spot on the texture atlas. Returns true if allocation was successful.
    //     Can be used to render onto it through the Render API
    //
    // Parameters:
    //   width:
    //
    //   height:
    //
    //   textureSubId:
    //
    //   texPos:
    bool AllocateTextureSpace(int width, int height, out int textureSubId, out TextureAtlasPosition texPos);

    //
    // Summary:
    //     Inserts a texture into the texture atlas after the atlas has been generated.
    //     Updates the in-ram texture atlas as well as the in-gpu-ram texture atlas. The
    //     textureSubId can be used to find the TextureAtlasPosition again in case you loose
    //     it ;-)
    //
    // Parameters:
    //   bmp:
    //
    //   textureSubId:
    //
    //   texPos:
    //
    //   alphaTest:
    bool InsertTexture(IBitmap bmp, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f);

    //
    // Summary:
    //     Inserts a texture into the texture atlas after the atlas has been generated.
    //     Updates the in-ram texture atlas as well as the in-gpu-ram texture atlas. The
    //     textureSubId can be used to find the TextureAtlasPosition again in case you loose
    //     it ;-)
    //
    // Parameters:
    //   pngBytes:
    //
    //   textureSubId:
    //
    //   texPos:
    //
    //   alphaTest:
    bool InsertTexture(byte[] pngBytes, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f);

    //
    // Summary:
    //     Loads a bitmap from given asset. Can use ++ syntax for texture overlay and @[int]
    //     for texture rotation
    //
    // Parameters:
    //   path:
    IBitmap LoadCompositeBitmap(AssetLocationAndSource path);

    //
    // Summary:
    //     Same as Vintagestory.API.Client.ITextureAtlasAPI.InsertTexture(Vintagestory.API.Common.IBitmap,System.Int32@,Vintagestory.API.Client.TextureAtlasPosition@,System.Single)
    //     but this method remembers the inserted texure, which you can access using capi.TextureAtlas[path]
    //     A subsequent call to this method will update the texture, but retain the same
    //     texPos. Also a run-time texture reload will reload this texture automatically.
    //
    //
    // Parameters:
    //   path:
    //     Used as reference for caching
    //
    //   textureSubId:
    //
    //   texPos:
    //
    //   onCreate:
    //     The method that should load the bitmap, if required. Can be left null to simply
    //     attempt to load the bmp from path using method Vintagestory.API.Client.ITextureAtlasAPI.LoadCompositeBitmap(Vintagestory.API.Common.AssetLocationAndSource)
    //
    //
    //   alphaTest:
    //
    // Returns:
    //     False if the file was not found or the insert failed
    bool GetOrInsertTexture(AssetLocationAndSource path, out int textureSubId, out TextureAtlasPosition texPos, CreateTextureDelegate onCreate = null, float alphaTest = 0f);

    //
    // Summary:
    //     Same as Vintagestory.API.Client.ITextureAtlasAPI.InsertTexture(Vintagestory.API.Common.IBitmap,System.Int32@,Vintagestory.API.Client.TextureAtlasPosition@,System.Single)
    //     but this method remembers the inserted texure, which you can access using capi.TextureAtlas[path]
    //     A subsequent call to this method will update the texture, but retain the same
    //     texPos. Also a run-time texture reload will reload this texture automatically.
    //
    //
    // Parameters:
    //   path:
    //     Used as reference for caching
    //
    //   textureSubId:
    //
    //   texPos:
    //
    //   onCreate:
    //     The method that should load the bitmap, if required. Can be left null to simply
    //     attempt to load the bmp from path using method Vintagestory.API.Client.ITextureAtlasAPI.LoadCompositeBitmap(Vintagestory.API.Common.AssetLocationAndSource)
    //
    //
    //   alphaTest:
    //
    // Returns:
    //     False if the file was not found or the insert failed
    bool GetOrInsertTexture(AssetLocation path, out int textureSubId, out TextureAtlasPosition texPos, CreateTextureDelegate onCreate = null, float alphaTest = 0f);

    //
    // Summary:
    //     Same as Vintagestory.API.Client.ITextureAtlasAPI.InsertTexture(Vintagestory.API.Common.IBitmap,System.Int32@,Vintagestory.API.Client.TextureAtlasPosition@,System.Single)
    //     but this method remembers the inserted texure, which you can access using capi.TextureAtlas[path]
    //     A subsequent call to this method will update the texture, but retain the same
    //     texPos. Also a run-time texture reload will reload this texture automatically.
    //
    //
    // Parameters:
    //   path:
    //
    //   bmp:
    //
    //   textureSubId:
    //
    //   texPos:
    //
    //   alphaTest:
    [Obsolete("Use GetOrInsertTexture() instead. It's more efficient to load the bmp only if the texture was not found in the cache")]
    bool InsertTextureCached(AssetLocation path, IBitmap bmp, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f);

    //
    // Summary:
    //     Same as Vintagestory.API.Client.ITextureAtlasAPI.InsertTexture(Vintagestory.API.Common.IBitmap,System.Int32@,Vintagestory.API.Client.TextureAtlasPosition@,System.Single)
    //     but this method remembers the inserted texure, which you can access using capi.TextureAtlas[path]
    //     A subsequent call to this method will update the texture, but retain the same
    //     texPos. Also a run-time texture reload will reload this texture automatically.
    //
    //
    // Parameters:
    //   path:
    //
    //   pngBytes:
    //
    //   textureSubId:
    //
    //   texPos:
    //
    //   alphaTest:
    bool InsertTextureCached(AssetLocation path, byte[] pngBytes, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f);

    bool GetOrInsertTexture(CompositeTexture ct, out int textureSubId, out TextureAtlasPosition texPos, float alphaTest = 0f);

    //
    // Summary:
    //     Deallocates a previously allocated texture space
    //
    // Parameters:
    //   textureSubId:
    void FreeTextureSpace(int textureSubId);

    //
    // Summary:
    //     Returns an rgba value picked randomly inside the given texture (defined by its
    //     sub-id)
    //
    // Parameters:
    //   textureSubId:
    int GetRandomColor(int textureSubId);

    //
    // Summary:
    //     Regenerates the mipmaps for one of the atlas textures, given by its array index
    //
    //
    // Parameters:
    //   atlasIndex:
    void RegenMipMaps(int atlasIndex);

    //
    // Summary:
    //     Returns one of 30 random rgba values inside the given texture (defined by its
    //     sub-id)
    //
    // Parameters:
    //   textureSubId:
    //
    //   rndIndex:
    //     0..29 for a specific random pixel, or -1 to randomize, which is the same as calling
    //     GetRandomColor without the rndIndex argument
    int GetRandomColor(int textureSubId, int rndIndex);

    //
    // Summary:
    //     Returns one of 30 random rgba values inside the given texture (defined by its
    //     sub-id)
    //
    // Parameters:
    //   texPos:
    //
    //   rndIndex:
    //     0..29 for a specific random pixel, or -1 to randomize, which is the same as calling
    //     GetRandomColor without the rndIndex argument
    int GetRandomColor(TextureAtlasPosition texPos, int rndIndex);

    //
    // Summary:
    //     Get the random colors array for the specified TextureAtlasPosition, creating
    //     it if necessary
    //
    // Parameters:
    //   texPos:
    int[] GetRandomColors(TextureAtlasPosition texPos);

    //
    // Summary:
    //     Returns you an average rgba value picked inside the texture subid
    //
    // Parameters:
    //   textureSubId:
    int GetAverageColor(int textureSubId);

    //
    // Summary:
    //     Renders given texture into the texture atlas at given location
    //
    // Parameters:
    //   intoAtlasTextureId:
    //
    //   fromTexture:
    //
    //   sourceX:
    //
    //   sourceY:
    //
    //   sourceWidth:
    //
    //   sourceHeight:
    //
    //   targetX:
    //
    //   targetY:
    //
    //   alphaTest:
    void RenderTextureIntoAtlas(int intoAtlasTextureId, LoadedTexture fromTexture, float sourceX, float sourceY, float sourceWidth, float sourceHeight, float targetX, float targetY, float alphaTest = 0.005f);
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
