#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

public class EntityProperties
{
    //
    // Summary:
    //     Assigned on registering the entity type
    public int Id;

    public string Color;

    //
    // Summary:
    //     The entity code in the code.
    public AssetLocation Code;

    //
    // Summary:
    //     Variant values as resolved from blocktype/itemtype or entitytype
    public OrderedDictionary<string, string> Variant = new OrderedDictionary<string, string>();

    //
    // Summary:
    //     The classification of the entity.
    public string Class;

    //
    // Summary:
    //     Natural habitat of the entity. Decides whether to apply gravity or not
    public EnumHabitat Habitat = EnumHabitat.Land;

    //
    // Summary:
    //     The size of the entity's hitbox (default: 0.2f/0.2f)
    public Vec2f CollisionBoxSize = new Vec2f(0.2f, 0.2f);

    //
    // Summary:
    //     The size of the hitbox while the entity is dead.
    public Vec2f DeadCollisionBoxSize = new Vec2f(0.3f, 0.3f);

    //
    // Summary:
    //     The size of the entity's hitbox (default: null, i.e. same as collision box)
    public Vec2f SelectionBoxSize;

    //
    // Summary:
    //     The size of the hitbox while the entity is dead. (default: null, i.e. same as
    //     dead collision box)
    public Vec2f DeadSelectionBoxSize;

    //
    // Summary:
    //     How high the camera should be placed if this entity were to be controlled by
    //     the player
    public double EyeHeight;

    public double SwimmingEyeHeight;

    //
    // Summary:
    //     The mass of this type of entity in kilograms, on average - defaults to 25kg (medium-low)
    //     if not set by the asset
    public float Weight = 25f;

    //
    // Summary:
    //     If true the entity can climb on walls
    public bool CanClimb;

    //
    // Summary:
    //     If true the entity can climb anywhere.
    public bool CanClimbAnywhere;

    //
    // Summary:
    //     Whether the entity should take fall damage
    public bool FallDamage = true;

    //
    // Summary:
    //     If less than one, mitigates fall damage (e.g. could be used for mountainous creatures);
    //     if more than one, increases fall damage (e.g fragile creatures?)
    public float FallDamageMultiplier = 1f;

    public float ClimbTouchDistance;

    //
    // Summary:
    //     Should the model in question rotate if climbing?
    public bool RotateModelOnClimb;

    //
    // Summary:
    //     The resistance to being pushed back by an impact.
    public float KnockbackResistance;

    //
    // Summary:
    //     The attributes of the entity. These are the Attributes read from the entity type's
    //     JSON file.
    //     If your code modifies these Attributes (not recommended!), the changes will apply
    //     to all entities of the same type.
    public JsonObject Attributes;

    //
    // Summary:
    //     The client properties of the entity.
    public EntityClientProperties Client;

    //
    // Summary:
    //     The server properties of the entity.
    public EntityServerProperties Server;

    //
    // Summary:
    //     The sounds that this entity can make.
    public Dictionary<string, AssetLocation> Sounds;

    //
    // Summary:
    //     The sounds this entity can make after being resolved.
    public Dictionary<string, AssetLocation[]> ResolvedSounds = new Dictionary<string, AssetLocation[]>();

    //
    // Summary:
    //     The chance that an idle sound will play for the entity.
    public float IdleSoundChance = 0.3f;

    //
    // Summary:
    //     The sound range for the idle sound in blocks.
    public float IdleSoundRange = 24f;

    //
    // Summary:
    //     The drops for the entity when they are killed.
    public BlockDropItemStack[] Drops;

    public byte[] DropsPacket;

    //
    // Summary:
    //     The collision box they have.
    public Cuboidf SpawnCollisionBox => new Cuboidf
    {
        X1 = (0f - CollisionBoxSize.X) / 2f,
        Z1 = (0f - CollisionBoxSize.X) / 2f,
        X2 = CollisionBoxSize.X / 2f,
        Z2 = CollisionBoxSize.X / 2f,
        Y2 = CollisionBoxSize.Y
    };

    //
    // Summary:
    //     Creates a copy of this object.
    public EntityProperties Clone()
    {
        BlockDropItemStack[] array;
        if (Drops == null)
        {
            array = null;
        }
        else
        {
            array = new BlockDropItemStack[Drops.Length];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = Drops[i].Clone();
            }
        }

        Dictionary<string, AssetLocation> dictionary = new Dictionary<string, AssetLocation>();
        foreach (KeyValuePair<string, AssetLocation> sound in Sounds)
        {
            dictionary[sound.Key] = sound.Value.Clone();
        }

        Dictionary<string, AssetLocation[]> dictionary2 = new Dictionary<string, AssetLocation[]>();
        foreach (KeyValuePair<string, AssetLocation[]> resolvedSound in ResolvedSounds)
        {
            AssetLocation[] value = resolvedSound.Value;
            dictionary2[resolvedSound.Key] = new AssetLocation[value.Length];
            for (int j = 0; j < value.Length; j++)
            {
                dictionary2[resolvedSound.Key][j] = value[j].Clone();
            }
        }

        if (!(Attributes is JsonObject_ReadOnly) && Attributes != null)
        {
            Attributes = new JsonObject_ReadOnly(Attributes);
        }

        return new EntityProperties
        {
            Code = Code.Clone(),
            Class = Class,
            Color = Color,
            Habitat = Habitat,
            CollisionBoxSize = CollisionBoxSize.Clone(),
            DeadCollisionBoxSize = DeadCollisionBoxSize.Clone(),
            SelectionBoxSize = SelectionBoxSize?.Clone(),
            DeadSelectionBoxSize = DeadSelectionBoxSize?.Clone(),
            CanClimb = CanClimb,
            Weight = Weight,
            CanClimbAnywhere = CanClimbAnywhere,
            FallDamage = FallDamage,
            FallDamageMultiplier = FallDamageMultiplier,
            ClimbTouchDistance = ClimbTouchDistance,
            RotateModelOnClimb = RotateModelOnClimb,
            KnockbackResistance = KnockbackResistance,
            Attributes = Attributes,
            Sounds = new Dictionary<string, AssetLocation>(Sounds),
            IdleSoundChance = IdleSoundChance,
            IdleSoundRange = IdleSoundRange,
            Drops = array,
            EyeHeight = EyeHeight,
            SwimmingEyeHeight = SwimmingEyeHeight,
            Client = (Client?.Clone() as EntityClientProperties),
            Server = (Server?.Clone() as EntityServerProperties),
            Variant = new OrderedDictionary<string, string>(Variant)
        };
    }

    //
    // Summary:
    //     Initalizes the properties for the entity.
    //
    // Parameters:
    //   entity:
    //     the entity to tie this to.
    //
    //   api:
    //     The Core API
    public void Initialize(Entity entity, ICoreAPI api)
    {
        if (api.Side.IsClient())
        {
            if (Client == null)
            {
                return;
            }

            Client.loadBehaviors(entity, this, api.World);
        }
        else if (Server != null)
        {
            Server.loadBehaviors(entity, this, api.World);
        }

        Client?.Init(Code, api.World);
        InitSounds(api.Assets);
    }

    //
    // Summary:
    //     Initializes the sounds for this entity type.
    //
    // Parameters:
    //   assetManager:
    public void InitSounds(IAssetManager assetManager)
    {
        if (Sounds == null)
        {
            return;
        }

        foreach (KeyValuePair<string, AssetLocation> sound in Sounds)
        {
            if (sound.Value.Path.EndsWith('*'))
            {
                List<IAsset> manyInCategory = assetManager.GetManyInCategory("sounds", sound.Value.Path.Substring(0, sound.Value.Path.Length - 1), sound.Value.Domain);
                AssetLocation[] array = new AssetLocation[manyInCategory.Count];
                int num = 0;
                foreach (IAsset item in manyInCategory)
                {
                    array[num++] = item.Location;
                }

                ResolvedSounds[sound.Key] = array;
            }
            else
            {
                ResolvedSounds[sound.Key] = new AssetLocation[1] { sound.Value.Clone().WithPathPrefix("sounds/") };
            }
        }
    }

    internal void PopulateDrops(IWorldAccessor worldForResolve)
    {
        using (MemoryStream input = new MemoryStream(DropsPacket))
        {
            BinaryReader binaryReader = new BinaryReader(input);
            BlockDropItemStack[] array = new BlockDropItemStack[binaryReader.ReadInt32()];
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new BlockDropItemStack();
                array[i].FromBytes(binaryReader, worldForResolve.ClassRegistry);
                array[i].Resolve(worldForResolve, "decode entity drops for ", Code);
            }

            Drops = array;
        }

        DropsPacket = null;
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
