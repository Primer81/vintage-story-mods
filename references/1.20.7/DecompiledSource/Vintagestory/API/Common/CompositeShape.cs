#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

//
// Summary:
//     Holds shape data to create 3D representations of objects. Also allows shapes
//     to be overlayed on top of one another recursively.
[DocumentAsJson]
public class CompositeShape
{
    //
    // Summary:
    //     The path to this shape file.
    [DocumentAsJson]
    public AssetLocation Base;

    //
    // Summary:
    //     The format/filetype of this shape.
    [DocumentAsJson]
    public EnumShapeFormat Format;

    //
    // Summary:
    //     Whether or not to insert baked in textures for mesh formats such as gltf into
    //     the texture atlas.
    [DocumentAsJson]
    public bool InsertBakedTextures;

    //
    // Summary:
    //     How much, in degrees, should this shape be rotated around the X axis?
    [DocumentAsJson]
    public float rotateX;

    //
    // Summary:
    //     How much, in degrees, should this shape be rotated around the Y axis?
    [DocumentAsJson]
    public float rotateY;

    //
    // Summary:
    //     How much, in degrees, should this shape be rotated around the Z axis?
    [DocumentAsJson]
    public float rotateZ;

    //
    // Summary:
    //     How much should this shape be offset on X axis?
    [DocumentAsJson]
    public float offsetX;

    //
    // Summary:
    //     How much should this shape be offset on Y axis?
    [DocumentAsJson]
    public float offsetY;

    //
    // Summary:
    //     How much should this shape be offset on Z axis?
    [DocumentAsJson]
    public float offsetZ;

    //
    // Summary:
    //     The scale of this shape on all axes.
    [DocumentAsJson]
    public float Scale = 1f;

    //
    // Summary:
    //     The block shape may consists of any amount of alternatives, one of which will
    //     be randomly chosen when the shape is chosen.
    [DocumentAsJson]
    public CompositeShape[] Alternates;

    //
    // Summary:
    //     Includes the base shape
    public CompositeShape[] BakedAlternates;

    //
    // Summary:
    //     The shape will render all overlays on top of this shape. Can be used to group
    //     multiple shapes into one composite shape.
    [DocumentAsJson]
    public CompositeShape[] Overlays;

    //
    // Summary:
    //     If true, the shape is created from a voxelized version of the first defined texture
    [DocumentAsJson]
    public bool VoxelizeTexture;

    //
    // Summary:
    //     If non zero will only tesselate the first n elements of the shape
    [DocumentAsJson]
    public int? QuantityElements;

    //
    // Summary:
    //     If set will only tesselate elements with given name
    [DocumentAsJson]
    public string[] SelectiveElements;

    //
    // Summary:
    //     If set will not tesselate elements with given name
    public string[] IgnoreElements;

    public Vec3f RotateXYZCopy => new Vec3f(rotateX, rotateY, rotateZ);

    public Vec3f OffsetXYZCopy => new Vec3f(offsetX, offsetY, offsetZ);

    public override int GetHashCode()
    {
        int num = Base.GetHashCode() + ("@" + rotateX + "/" + rotateY + "/" + rotateZ + "o" + offsetX + "/" + offsetY + "/" + offsetZ).GetHashCode();
        if (Overlays != null)
        {
            for (int i = 0; i < Overlays.Length; i++)
            {
                num ^= Overlays[i].GetHashCode();
            }
        }

        return num;
    }

    public override string ToString()
    {
        return Base.ToString();
    }

    //
    // Summary:
    //     Creates a deep copy of the composite shape
    public CompositeShape Clone()
    {
        CompositeShape[] array = null;
        if (Alternates != null)
        {
            array = new CompositeShape[Alternates.Length];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = Alternates[i].CloneWithoutAlternatesNorOverlays();
            }
        }

        CompositeShape compositeShape = CloneWithoutAlternates();
        compositeShape.Alternates = array;
        return compositeShape;
    }

    //
    // Summary:
    //     Creates a deep copy of the shape, but omitting its alternates (used to populate
    //     the alternates)
    public CompositeShape CloneWithoutAlternates()
    {
        CompositeShape[] array = null;
        if (Overlays != null)
        {
            array = new CompositeShape[Overlays.Length];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = Overlays[i].CloneWithoutAlternatesNorOverlays();
            }
        }

        CompositeShape compositeShape = CloneWithoutAlternatesNorOverlays();
        compositeShape.Overlays = array;
        return compositeShape;
    }

    internal CompositeShape CloneWithoutAlternatesNorOverlays()
    {
        return new CompositeShape
        {
            Base = Base?.Clone(),
            Format = Format,
            InsertBakedTextures = InsertBakedTextures,
            rotateX = rotateX,
            rotateY = rotateY,
            rotateZ = rotateZ,
            offsetX = offsetX,
            offsetY = offsetY,
            offsetZ = offsetZ,
            Scale = Scale,
            VoxelizeTexture = VoxelizeTexture,
            QuantityElements = QuantityElements,
            SelectiveElements = (string[])SelectiveElements?.Clone(),
            IgnoreElements = (string[])IgnoreElements?.Clone()
        };
    }

    //
    // Summary:
    //     Expands the Composite Shape and populates the Baked field
    public void LoadAlternates(IAssetManager assetManager, ILogger logger)
    {
        List<CompositeShape> list = new List<CompositeShape>();
        if (Base.Path.EndsWith('*'))
        {
            list.AddRange(resolveShapeWildCards(this, assetManager, logger, addCubeIfNone: true));
        }
        else
        {
            list.Add(this);
        }

        if (Alternates != null)
        {
            CompositeShape[] alternates = Alternates;
            foreach (CompositeShape compositeShape in alternates)
            {
                if (compositeShape.Base == null)
                {
                    compositeShape.Base = Base.Clone();
                }

                if (compositeShape.Base.Path.EndsWith('*'))
                {
                    list.AddRange(resolveShapeWildCards(compositeShape, assetManager, logger, addCubeIfNone: false));
                }
                else
                {
                    list.Add(compositeShape);
                }
            }
        }

        Base = list[0].Base;
        if (list.Count == 1)
        {
            return;
        }

        Alternates = new CompositeShape[list.Count - 1];
        for (int j = 0; j < list.Count - 1; j++)
        {
            Alternates[j] = list[j + 1];
        }

        BakedAlternates = new CompositeShape[Alternates.Length + 1];
        BakedAlternates[0] = CloneWithoutAlternates();
        for (int k = 0; k < Alternates.Length; k++)
        {
            CompositeShape compositeShape2 = (BakedAlternates[k + 1] = Alternates[k]);
            if (compositeShape2.Base == null)
            {
                compositeShape2.Base = Base.Clone();
            }

            if (!compositeShape2.QuantityElements.HasValue)
            {
                compositeShape2.QuantityElements = QuantityElements;
            }

            if (compositeShape2.SelectiveElements == null)
            {
                compositeShape2.SelectiveElements = SelectiveElements;
            }

            if (compositeShape2.IgnoreElements == null)
            {
                compositeShape2.IgnoreElements = IgnoreElements;
            }
        }
    }

    private CompositeShape[] resolveShapeWildCards(CompositeShape shape, IAssetManager assetManager, ILogger logger, bool addCubeIfNone)
    {
        List<IAsset> manyInCategory = assetManager.GetManyInCategory("shapes", shape.Base.Path.Substring(0, Base.Path.Length - 1), shape.Base.Domain);
        if (manyInCategory.Count == 0)
        {
            if (addCubeIfNone)
            {
                logger.Warning("Could not find any variants for wildcard shape {0}, will use standard cube shape.", shape.Base.Path);
                return new CompositeShape[1]
                {
                    new CompositeShape
                    {
                        Base = new AssetLocation("block/basic/cube")
                    }
                };
            }

            logger.Warning("Could not find any variants for wildcard shape {0}.", shape.Base.Path);
            return new CompositeShape[0];
        }

        CompositeShape[] array = new CompositeShape[manyInCategory.Count];
        int num = 0;
        foreach (IAsset item in manyInCategory)
        {
            AssetLocation assetLocation = item.Location.CopyWithPath(item.Location.Path.Substring("shapes/".Length));
            assetLocation.RemoveEnding();
            array[num++] = new CompositeShape
            {
                Base = assetLocation,
                rotateX = shape.rotateX,
                rotateY = shape.rotateY,
                rotateZ = shape.rotateZ,
                Scale = shape.Scale,
                QuantityElements = shape.QuantityElements,
                SelectiveElements = shape.SelectiveElements,
                IgnoreElements = shape.IgnoreElements
            };
        }

        return array;
    }
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
