#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

//
// Summary:
//     Holds data about a texture. Also allows textures to be overlayed on top of one
//     another.
[DocumentAsJson]
public class CompositeTexture
{
    public const char AlphaSeparator = 'å';

    public const string AlphaSeparatorRegexSearch = "å\\d+";

    public const string OverlaysSeparator = "++";

    public const char BlendmodeSeparator = '~';

    //
    // Summary:
    //     The basic texture for this composite texture
    [DocumentAsJson]
    public AssetLocation Base;

    //
    // Summary:
    //     A set of textures to overlay above this texture. The base texture may be overlayed
    //     with any quantity of textures. These are baked together during texture atlas
    //     creation.
    [DocumentAsJson]
    public BlendedOverlayTexture[] BlendedOverlays;

    //
    // Summary:
    //     The texture may consists of any amount of alternatives, one of which will be
    //     randomly chosen when the block is placed in the world.
    [DocumentAsJson]
    public CompositeTexture[] Alternates;

    //
    // Summary:
    //     A way of basic support for connected textures. Textures should be named numerically
    //     from 1 to Vintagestory.API.Client.CompositeTexture.TilesWidth squared.
    //     E.g., if Vintagestory.API.Client.CompositeTexture.TilesWidth is 3, the order
    //     follows the pattern of:
    //     1 2 3
    //     4 5 6
    //     7 8 9
    [DocumentAsJson]
    public CompositeTexture[] Tiles;

    //
    // Summary:
    //     The number of tiles in one direction that make up the full connected textures
    //     defined in Vintagestory.API.Client.CompositeTexture.Tiles.
    [DocumentAsJson]
    public int TilesWidth;

    //
    // Summary:
    //     BakedCompositeTexture is an expanded, atlas friendly version of CompositeTexture.
    //     Required during texture atlas generation.
    public BakedCompositeTexture Baked;

    //
    // Summary:
    //     Rotation of the texture may only be a multiple of 90
    [DocumentAsJson]
    public int Rotation;

    //
    // Summary:
    //     Can be used to modify the opacity of the texture. 255 is fully opaque, 0 is fully
    //     transparent.
    [DocumentAsJson]
    public int Alpha = 255;

    [ThreadStatic]
    public static Dictionary<AssetLocation, CompositeTexture> basicTexturesCache;

    [ThreadStatic]
    public static Dictionary<AssetLocation, List<IAsset>> wildcardsCache;

    public AssetLocation WildCardNoFiles;

    //
    // Summary:
    //     Obsolete. Use Vintagestory.API.Client.CompositeTexture.BlendedOverlays instead.
    [DocumentAsJson]
    public AssetLocation[] Overlays
    {
        set
        {
            BlendedOverlays = value.Select((AssetLocation o) => new BlendedOverlayTexture
            {
                Base = o
            }).ToArray();
        }
    }

    public AssetLocation AnyWildCardNoFiles
    {
        get
        {
            if (WildCardNoFiles != null)
            {
                return WildCardNoFiles;
            }

            if (Alternates != null)
            {
                AssetLocation assetLocation = Alternates.Select((CompositeTexture ct) => ct.WildCardNoFiles).FirstOrDefault();
                if (assetLocation != null)
                {
                    return assetLocation;
                }
            }

            return null;
        }
    }

    //
    // Summary:
    //     Creates a new empty composite texture
    public CompositeTexture()
    {
    }

    //
    // Summary:
    //     Creates a new empty composite texture with given base texture
    //
    // Parameters:
    //   Base:
    public CompositeTexture(AssetLocation Base)
    {
        this.Base = Base;
    }

    //
    // Summary:
    //     Creates a deep copy of the texture
    public CompositeTexture Clone()
    {
        CompositeTexture[] array = null;
        if (Alternates != null)
        {
            array = new CompositeTexture[Alternates.Length];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = Alternates[i].CloneWithoutAlternates();
            }
        }

        CompositeTexture[] array2 = null;
        if (Tiles != null)
        {
            array2 = new CompositeTexture[Tiles.Length];
            for (int j = 0; j < array2.Length; j++)
            {
                array2[j] = array2[j].CloneWithoutAlternates();
            }
        }

        CompositeTexture compositeTexture = new CompositeTexture
        {
            Base = Base.Clone(),
            Alternates = array,
            Tiles = array2,
            Rotation = Rotation,
            Alpha = Alpha,
            TilesWidth = TilesWidth
        };
        if (BlendedOverlays != null)
        {
            compositeTexture.BlendedOverlays = new BlendedOverlayTexture[BlendedOverlays.Length];
            for (int k = 0; k < compositeTexture.BlendedOverlays.Length; k++)
            {
                compositeTexture.BlendedOverlays[k] = BlendedOverlays[k].Clone();
            }
        }

        return compositeTexture;
    }

    internal CompositeTexture CloneWithoutAlternates()
    {
        CompositeTexture compositeTexture = new CompositeTexture
        {
            Base = Base.Clone(),
            Rotation = Rotation,
            Alpha = Alpha,
            TilesWidth = TilesWidth
        };
        if (BlendedOverlays != null)
        {
            compositeTexture.BlendedOverlays = new BlendedOverlayTexture[BlendedOverlays.Length];
            for (int i = 0; i < compositeTexture.BlendedOverlays.Length; i++)
            {
                compositeTexture.BlendedOverlays[i] = BlendedOverlays[i].Clone();
            }
        }

        if (Tiles != null)
        {
            compositeTexture.Tiles = new CompositeTexture[Tiles.Length];
            for (int j = 0; j < compositeTexture.Tiles.Length; j++)
            {
                compositeTexture.Tiles[j] = compositeTexture.Tiles[j].CloneWithoutAlternates();
            }
        }

        return compositeTexture;
    }

    //
    // Summary:
    //     Tests whether this is a basic CompositeTexture with an asset location only, no
    //     rotation, alpha, alternates or overlays
    public bool IsBasic()
    {
        if (Rotation != 0 || Alpha != 255)
        {
            return false;
        }

        if (Alternates == null && BlendedOverlays == null)
        {
            return Tiles == null;
        }

        return false;
    }

    //
    // Summary:
    //     Expands the Composite Texture to a texture atlas friendly version and populates
    //     the Baked field. This method is called by the texture atlas managers. Won't have
    //     any effect if called after the texture atlasses have been created.
    public void Bake(IAssetManager assetManager)
    {
        if (Baked == null)
        {
            Baked = Bake(assetManager, this);
        }
    }

    //
    // Summary:
    //     Expands the Composite Texture to a texture atlas friendly version and populates
    //     the Baked field. This method can be called after the game world has loaded.
    //
    // Parameters:
    //   capi:
    //     The Client API
    //
    //   intoAtlas:
    //     The atlas to insert the baked texture.
    public void RuntimeBake(ICoreClientAPI capi, ITextureAtlasAPI intoAtlas)
    {
        Baked = Bake(capi.Assets, this);
        RuntimeInsert(capi, intoAtlas, Baked);
        if (Baked.BakedVariants != null)
        {
            BakedCompositeTexture[] bakedVariants = Baked.BakedVariants;
            foreach (BakedCompositeTexture btex in bakedVariants)
            {
                RuntimeInsert(capi, intoAtlas, btex);
            }
        }
    }

    private bool RuntimeInsert(ICoreClientAPI capi, ITextureAtlasAPI intoAtlas, BakedCompositeTexture btex)
    {
        BitmapRef bitmapRef = capi.Assets.Get(btex.BakedName).ToBitmap(capi);
        if (intoAtlas.InsertTexture(bitmapRef, out var textureSubId, out var _))
        {
            btex.TextureSubId = textureSubId;
            capi.Render.RemoveTexture(btex.BakedName);
            return true;
        }

        bitmapRef.Dispose();
        return false;
    }

    //
    // Summary:
    //     Expands a CompositeTexture to a texture atlas friendly version and populates
    //     the Baked field
    //
    // Parameters:
    //   assetManager:
    //
    //   ct:
    public static BakedCompositeTexture Bake(IAssetManager assetManager, CompositeTexture ct)
    {
        BakedCompositeTexture bakedCompositeTexture = new BakedCompositeTexture();
        ct.WildCardNoFiles = null;
        if (ct.Base.EndsWithWildCard)
        {
            if (wildcardsCache == null)
            {
                wildcardsCache = new Dictionary<AssetLocation, List<IAsset>>();
            }

            if (!wildcardsCache.TryGetValue(ct.Base, out var value))
            {
                List<IAsset> list = (wildcardsCache[ct.Base] = assetManager.GetManyInCategory("textures", ct.Base.Path.Substring(0, ct.Base.Path.Length - 1), ct.Base.Domain));
                value = list;
            }

            if (value.Count == 0)
            {
                ct.WildCardNoFiles = ct.Base;
                ct.Base = new AssetLocation("unknown");
            }
            else if (value.Count == 1)
            {
                ct.Base = value[0].Location.CloneWithoutPrefixAndEnding("textures/".Length);
            }
            else
            {
                int num = ((ct.Alternates != null) ? ct.Alternates.Length : 0);
                CompositeTexture[] array = new CompositeTexture[num + value.Count - 1];
                if (ct.Alternates != null)
                {
                    Array.Copy(ct.Alternates, array, ct.Alternates.Length);
                }

                if (basicTexturesCache == null)
                {
                    basicTexturesCache = new Dictionary<AssetLocation, CompositeTexture>();
                }

                for (int i = 0; i < value.Count; i++)
                {
                    AssetLocation assetLocation = value[i].Location.CloneWithoutPrefixAndEnding("textures/".Length);
                    if (i == 0)
                    {
                        ct.Base = assetLocation;
                        continue;
                    }

                    CompositeTexture value2;
                    if (ct.Rotation == 0 && ct.Alpha == 255)
                    {
                        if (!basicTexturesCache.TryGetValue(assetLocation, out value2))
                        {
                            CompositeTexture compositeTexture2 = (basicTexturesCache[assetLocation] = new CompositeTexture(assetLocation));
                            value2 = compositeTexture2;
                        }
                    }
                    else
                    {
                        value2 = new CompositeTexture(assetLocation);
                        value2.Rotation = ct.Rotation;
                        value2.Alpha = ct.Alpha;
                    }

                    array[num + i - 1] = value2;
                }

                ct.Alternates = array;
            }
        }

        bakedCompositeTexture.BakedName = ct.Base.Clone();
        if (ct.BlendedOverlays != null)
        {
            bakedCompositeTexture.TextureFilenames = new AssetLocation[ct.BlendedOverlays.Length + 1];
            bakedCompositeTexture.TextureFilenames[0] = ct.Base;
            for (int j = 0; j < ct.BlendedOverlays.Length; j++)
            {
                BlendedOverlayTexture blendedOverlayTexture = ct.BlendedOverlays[j];
                bakedCompositeTexture.TextureFilenames[j + 1] = blendedOverlayTexture.Base;
                AssetLocation bakedName = bakedCompositeTexture.BakedName;
                string[] obj = new string[5] { bakedName.Path, "++", null, null, null };
                int blendMode = (int)blendedOverlayTexture.BlendMode;
                obj[2] = blendMode.ToString();
                obj[3] = "~";
                obj[4] = blendedOverlayTexture.Base.ToShortString();
                bakedName.Path = string.Concat(obj);
            }
        }
        else
        {
            bakedCompositeTexture.TextureFilenames = new AssetLocation[1] { ct.Base };
        }

        if (ct.Rotation != 0)
        {
            if (ct.Rotation != 90 && ct.Rotation != 180 && ct.Rotation != 270)
            {
                throw new Exception(string.Concat("Texture definition ", ct.Base, " has a rotation thats not 0, 90, 180 or 270. These are the only allowed values!"));
            }

            AssetLocation bakedName2 = bakedCompositeTexture.BakedName;
            bakedName2.Path = bakedName2.Path + "@" + ct.Rotation;
        }

        if (ct.Alpha != 255)
        {
            if (ct.Alpha < 0 || ct.Alpha > 255)
            {
                throw new Exception(string.Concat("Texture definition ", ct.Base, " has a alpha value outside the 0..255 range."));
            }

            AssetLocation bakedName3 = bakedCompositeTexture.BakedName;
            bakedName3.Path = bakedName3.Path + "å" + ct.Alpha;
        }

        if (ct.Alternates != null)
        {
            bakedCompositeTexture.BakedVariants = new BakedCompositeTexture[ct.Alternates.Length + 1];
            bakedCompositeTexture.BakedVariants[0] = bakedCompositeTexture;
            for (int k = 0; k < ct.Alternates.Length; k++)
            {
                bakedCompositeTexture.BakedVariants[k + 1] = Bake(assetManager, ct.Alternates[k]);
            }
        }

        if (ct.Tiles != null)
        {
            List<BakedCompositeTexture> list2 = new List<BakedCompositeTexture>();
            for (int l = 0; l < ct.Tiles.Length; l++)
            {
                if (ct.Tiles[l].Base.EndsWithWildCard)
                {
                    if (wildcardsCache == null)
                    {
                        wildcardsCache = new Dictionary<AssetLocation, List<IAsset>>();
                    }

                    string text = ct.Base.Path.Substring(0, ct.Base.Path.Length - 1);
                    List<IAsset> list = (wildcardsCache[ct.Base] = assetManager.GetManyInCategory("textures", text, ct.Base.Domain));
                    List<IAsset> source = list;
                    int len = "textures".Length + text.Length + "/".Length;
                    List<IAsset> list3 = source.OrderBy((IAsset asset) => asset.Location.Path.Substring(len).RemoveFileEnding().ToInt()).ToList();
                    for (int m = 0; m < list3.Count; m++)
                    {
                        CompositeTexture compositeTexture3 = new CompositeTexture(list3[m].Location.CloneWithoutPrefixAndEnding("textures/".Length));
                        compositeTexture3.Rotation = ct.Rotation;
                        compositeTexture3.Alpha = ct.Alpha;
                        BakedCompositeTexture bakedCompositeTexture2 = Bake(assetManager, compositeTexture3);
                        bakedCompositeTexture2.TilesWidth = ct.TilesWidth;
                        list2.Add(bakedCompositeTexture2);
                    }
                }
                else
                {
                    BakedCompositeTexture bakedCompositeTexture3 = Bake(assetManager, ct.Tiles[l]);
                    bakedCompositeTexture3.TilesWidth = ct.TilesWidth;
                    list2.Add(bakedCompositeTexture3);
                }
            }

            bakedCompositeTexture.BakedTiles = list2.ToArray();
        }

        return bakedCompositeTexture;
    }

    public override string ToString()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(Base.ToString());
        stringBuilder.Append("@");
        stringBuilder.Append(Rotation);
        stringBuilder.Append("a");
        stringBuilder.Append(Alpha);
        if (Alternates != null)
        {
            stringBuilder.Append("alts:");
            CompositeTexture[] alternates = Alternates;
            for (int i = 0; i < alternates.Length; i++)
            {
                alternates[i].ToString(stringBuilder);
                stringBuilder.Append(",");
            }
        }

        if (BlendedOverlays != null)
        {
            stringBuilder.Append("ovs:");
            BlendedOverlayTexture[] blendedOverlays = BlendedOverlays;
            for (int i = 0; i < blendedOverlays.Length; i++)
            {
                blendedOverlays[i].ToString(stringBuilder);
                stringBuilder.Append(",");
            }
        }

        return stringBuilder.ToString();
    }

    public void ToString(StringBuilder sb)
    {
        sb.Append(Base.ToString());
        sb.Append("@");
        sb.Append(Rotation);
        sb.Append("a");
        sb.Append(Alpha);
        if (Alternates != null)
        {
            sb.Append("alts:");
            CompositeTexture[] alternates = Alternates;
            foreach (CompositeTexture compositeTexture in alternates)
            {
                sb.Append(compositeTexture.ToString());
                sb.Append(",");
            }
        }

        if (BlendedOverlays != null)
        {
            sb.Append("ovs:");
            BlendedOverlayTexture[] blendedOverlays = BlendedOverlays;
            foreach (BlendedOverlayTexture blendedOverlayTexture in blendedOverlays)
            {
                sb.Append(blendedOverlayTexture.ToString());
                sb.Append(",");
            }
        }
    }

    public void FillPlaceholder(string search, string replace)
    {
        Base.Path = Base.Path.Replace(search, replace);
        if (BlendedOverlays != null)
        {
            BlendedOverlays.Foreach(delegate (BlendedOverlayTexture ov)
            {
                ov.Base.Path = ov.Base.Path.Replace(search, replace);
            });
        }

        if (Alternates != null)
        {
            Alternates.Foreach(delegate (CompositeTexture alt)
            {
                alt.FillPlaceholder(search, replace);
            });
        }

        if (Tiles != null)
        {
            Tiles.Foreach(delegate (CompositeTexture tile)
            {
                tile.FillPlaceholder(search, replace);
            });
        }
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
