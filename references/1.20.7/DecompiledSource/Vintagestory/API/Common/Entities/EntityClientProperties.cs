#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class EntityClientProperties : EntitySidedProperties
{
    //
    // Summary:
    //     Set by the game client
    public EntityRenderer Renderer;

    //
    // Summary:
    //     Name of there renderer system that draws this entity
    public string RendererName;

    //
    // Summary:
    //     Directory of all available textures. First one will be default one
    //     Note: from game version 1.20.4, this is null on server-side (except during asset
    //     loading start-up stage)
    public IDictionary<string, CompositeTexture> Textures = new FastSmallDictionary<string, CompositeTexture>(0);

    //
    // Summary:
    //     Set by a server at the end of asset loading, immediately prior to setting Textures
    //     to null; relevant to spawning entities with variant textures
    public int TexturesAlternatesCount;

    //
    // Summary:
    //     The glow level for the entity.
    public int GlowLevel;

    //
    // Summary:
    //     Makes entities pitch forward and backwards when stepping
    public bool PitchStep = true;

    //
    // Summary:
    //     The shape of the entity
    public CompositeShape Shape;

    //
    // Summary:
    //     Only loaded for World.EntityTypes instances of EntityProperties, because it makes
    //     no sense to have 1000 loaded entities needing to load 1000 shapes. During entity
    //     load/spawn this value is assigned however On the client it gets set by the EntityTextureAtlasManager
    //     On the server by the EntitySimulation system
    public Shape LoadedShape;

    public Shape[] LoadedAlternateShapes;

    //
    // Summary:
    //     The shape for this particular entity who owns this properties object
    public Shape LoadedShapeForEntity;

    public CompositeShape ShapeForEntity;

    //
    // Summary:
    //     The size of the entity (default: 1f)
    public float Size = 1f;

    //
    // Summary:
    //     The rate at which the entity's size grows with age - used for chicks and other
    //     small baby animals
    public float SizeGrowthFactor;

    //
    // Summary:
    //     The animations of the entity.
    public AnimationMetaData[] Animations;

    public Dictionary<string, AnimationMetaData> AnimationsByMetaCode = new Dictionary<string, AnimationMetaData>(StringComparer.OrdinalIgnoreCase);

    public Dictionary<uint, AnimationMetaData> AnimationsByCrc32 = new Dictionary<uint, AnimationMetaData>();

    //
    // Summary:
    //     Used by various renderers to retrieve the entities texture it should be drawn
    //     with
    public virtual CompositeTexture Texture
    {
        get
        {
            if (Textures != null && Textures.Count != 0)
            {
                return Textures.First().Value;
            }

            return null;
        }
    }

    //
    // Summary:
    //     Returns the first texture in Textures dict
    public CompositeTexture FirstTexture
    {
        get
        {
            if (Textures != null && Textures.Count != 0)
            {
                return Textures.First().Value;
            }

            return null;
        }
    }

    public EntityClientProperties(JsonObject[] behaviors, Dictionary<string, JsonObject> commonConfigs)
        : base(behaviors, commonConfigs)
    {
    }

    public void DetermineLoadedShape(long forEntityId)
    {
        if (LoadedAlternateShapes != null && LoadedAlternateShapes.Length != 0)
        {
            int num = GameMath.MurmurHash3Mod(0, 0, (int)forEntityId, 1 + LoadedAlternateShapes.Length);
            if (num == 0)
            {
                LoadedShapeForEntity = LoadedShape;
                ShapeForEntity = Shape;
            }
            else
            {
                LoadedShapeForEntity = LoadedAlternateShapes[num - 1];
                ShapeForEntity = Shape.Alternates[num - 1];
            }
        }
        else
        {
            LoadedShapeForEntity = LoadedShape;
            ShapeForEntity = Shape;
        }
    }

    //
    // Summary:
    //     Initializes the client properties.
    //
    // Parameters:
    //   entityTypeCode:
    //
    //   world:
    public void Init(AssetLocation entityTypeCode, IWorldAccessor world)
    {
        if (Animations != null)
        {
            for (int i = 0; i < Animations.Length; i++)
            {
                AnimationMetaData animationMetaData = Animations[i];
                animationMetaData.Init();
                if (animationMetaData.Animation != null)
                {
                    AnimationsByMetaCode[animationMetaData.Code] = animationMetaData;
                }

                if (animationMetaData.Animation != null)
                {
                    AnimationsByCrc32[animationMetaData.CodeCrc32] = animationMetaData;
                }
            }
        }

        if (world != null)
        {
            EntityClientProperties entityClientProperties = world.EntityTypes.FirstOrDefault((EntityProperties et) => et.Code.Equals(entityTypeCode))?.Client;
            LoadedShape = entityClientProperties?.LoadedShape;
            LoadedAlternateShapes = entityClientProperties?.LoadedAlternateShapes;
        }
    }

    //
    // Summary:
    //     Does not clone textures, but does clone shapes
    public override EntitySidedProperties Clone()
    {
        AnimationMetaData[] array = null;
        if (Animations != null)
        {
            AnimationMetaData[] animations = Animations;
            array = new AnimationMetaData[animations.Length];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = animations[i].Clone();
            }
        }

        Dictionary<string, AnimationMetaData> dictionary = new Dictionary<string, AnimationMetaData>(StringComparer.OrdinalIgnoreCase);
        Dictionary<uint, AnimationMetaData> dictionary2 = new Dictionary<uint, AnimationMetaData>();
        foreach (KeyValuePair<string, AnimationMetaData> item in AnimationsByMetaCode)
        {
            AnimationMetaData animationMetaData = item.Value.Clone();
            dictionary[item.Key] = animationMetaData;
            dictionary2[animationMetaData.CodeCrc32] = animationMetaData;
        }

        Shape[] array2 = null;
        if (LoadedAlternateShapes != null)
        {
            Shape[] loadedAlternateShapes = LoadedAlternateShapes;
            array2 = new Shape[loadedAlternateShapes.Length];
            for (int j = 0; j < array2.Length; j++)
            {
                array2[j] = loadedAlternateShapes[j].Clone();
            }
        }

        return new EntityClientProperties(BehaviorsAsJsonObj, null)
        {
            Textures = ((Textures == null) ? null : new FastSmallDictionary<string, CompositeTexture>(Textures)),
            TexturesAlternatesCount = TexturesAlternatesCount,
            RendererName = RendererName,
            GlowLevel = GlowLevel,
            PitchStep = PitchStep,
            Size = Size,
            SizeGrowthFactor = SizeGrowthFactor,
            Shape = Shape?.Clone(),
            LoadedAlternateShapes = array2,
            Animations = array,
            AnimationsByMetaCode = dictionary,
            AnimationsByCrc32 = dictionary2
        };
    }

    public virtual void FreeRAMServer()
    {
        CompositeTexture[] array = FirstTexture?.Alternates;
        TexturesAlternatesCount = ((array != null) ? array.Length : 0);
        Textures = null;
        if (Animations != null)
        {
            AnimationMetaData[] animations = Animations;
            for (int i = 0; i < animations.Length; i++)
            {
                animations[i].DeDuplicate();
            }
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
