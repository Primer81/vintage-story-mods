#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System.IO;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

//
// Summary:
//     Used to add a set of particle properties to a collectible.
[DocumentAsJson]
[JsonObject(MemberSerialization.OptIn)]
public class AdvancedParticleProperties : IParticlePropertiesProvider
{
    //
    // Summary:
    //     The Hue/Saturation/Value/Alpha for the color of the particle.
    [JsonProperty]
    public NatFloat[] HsvaColor = new NatFloat[4]
    {
        NatFloat.createUniform(128f, 128f),
        NatFloat.createUniform(128f, 128f),
        NatFloat.createUniform(128f, 128f),
        NatFloat.createUniform(255f, 0f)
    };

    //
    // Summary:
    //     Offset from the blocks hitboxes top middle position
    [JsonProperty]
    public NatFloat[] PosOffset = new NatFloat[3]
    {
        NatFloat.createUniform(0f, 0f),
        NatFloat.createUniform(0f, 0f),
        NatFloat.createUniform(0f, 0f)
    };

    //
    // Summary:
    //     The base position for the particles.
    public Vec3d basePos = new Vec3d();

    public Vec3f baseVelocity = new Vec3f();

    //
    // Summary:
    //     The base block for the particle.
    public Block block;

    //
    // Summary:
    //     When HsvaColor is null, this is used
    public int Color;

    //
    // Summary:
    //     Gets the position of the particle in world.
    private Vec3d tmpPos = new Vec3d();

    private Vec3f tmpVelo = new Vec3f();

    public bool Async => false;

    //
    // Summary:
    //     Allows each particle to randomly change its velocity over time.
    [JsonProperty]
    public bool RandomVelocityChange { get; set; }

    //
    // Summary:
    //     If true, particle dies if it falls below the rain height at its given location
    [JsonProperty]
    public bool DieOnRainHeightmap { get; set; }

    //
    // Summary:
    //     More particles that spawn from this particle over time. See Vintagestory.API.Common.AdvancedParticleProperties.SecondarySpawnInterval
    //     to control rate.
    [JsonProperty]
    public AdvancedParticleProperties[] SecondaryParticles { get; set; }

    //
    // Summary:
    //     More particles that spawn when this particle dies.
    [JsonProperty]
    public AdvancedParticleProperties[] DeathParticles { get; set; }

    //
    // Summary:
    //     The inverval that the Vintagestory.API.Common.AdvancedParticleProperties.SecondaryParticles
    //     spawn.
    [JsonProperty]
    public NatFloat SecondarySpawnInterval { get; set; } = NatFloat.createUniform(0f, 0f);


    //
    // Summary:
    //     The amount of velocity to be kept when this particle collides with something.
    //     Directional velocity is multipled by (-Bounciness * 0.65) on any collision.
    [JsonProperty]
    public float Bounciness { get; set; }

    //
    // Summary:
    //     Whether or not the particle dies in air.
    [JsonProperty]
    public bool DieInAir { get; set; }

    //
    // Summary:
    //     Whether or not the particle dies in water.
    [JsonProperty]
    public bool DieInLiquid { get; set; }

    //
    // Summary:
    //     Whether or not the particle floats on liquids.
    [JsonProperty]
    public bool SwimOnLiquid { get; set; }

    //
    // Summary:
    //     Whether or not to color the particle by the block it's on.
    [JsonProperty]
    public bool ColorByBlock { get; set; }

    //
    // Summary:
    //     A transforming opacity value.
    [JsonProperty]
    public EvolvingNatFloat OpacityEvolve { get; set; }

    //
    // Summary:
    //     A transforming Red value.
    [JsonProperty]
    public EvolvingNatFloat RedEvolve { get; set; }

    //
    // Summary:
    //     A transforming Green value.
    [JsonProperty]
    public EvolvingNatFloat GreenEvolve { get; set; }

    //
    // Summary:
    //     A transforming Blue value.
    [JsonProperty]
    public EvolvingNatFloat BlueEvolve { get; set; }

    //
    // Summary:
    //     The gravity effect on the particle.
    [JsonProperty]
    public NatFloat GravityEffect { get; set; } = NatFloat.createUniform(1f, 0f);


    //
    // Summary:
    //     The life length, in seconds, of the particle.
    [JsonProperty]
    public NatFloat LifeLength { get; set; } = NatFloat.createUniform(1f, 0f);


    //
    // Summary:
    //     The quantity of the particles given.
    [JsonProperty]
    public NatFloat Quantity { get; set; } = NatFloat.createUniform(1f, 0f);


    //
    // Summary:
    //     The size of the particles given.
    [JsonProperty]
    public NatFloat Size { get; set; } = NatFloat.createUniform(1f, 0f);


    //
    // Summary:
    //     A transforming Size value.
    [JsonProperty]
    public EvolvingNatFloat SizeEvolve { get; set; } = EvolvingNatFloat.createIdentical(0f);


    //
    // Summary:
    //     The velocity of the particles.
    [JsonProperty]
    public NatFloat[] Velocity { get; set; } = new NatFloat[3]
    {
        NatFloat.createUniform(0f, 0.5f),
        NatFloat.createUniform(0f, 0.5f),
        NatFloat.createUniform(0f, 0.5f)
    };


    //
    // Summary:
    //     A dynamic velocity value.
    [JsonProperty]
    public EvolvingNatFloat[] VelocityEvolve { get; set; }

    //
    // Summary:
    //     Sets the base model for the particle.
    [JsonProperty]
    public EnumParticleModel ParticleModel { get; set; } = EnumParticleModel.Cube;


    //
    // Summary:
    //     The level of glow in the particle.
    [JsonProperty]
    public int VertexFlags { get; set; }

    //
    // Summary:
    //     Whether or not the particle is self propelled.
    [JsonProperty]
    public bool SelfPropelled { get; set; }

    //
    // Summary:
    //     Whether or not the particle collides with the terrain.
    [JsonProperty]
    public bool TerrainCollision { get; set; } = true;


    //
    // Summary:
    //     How much the particles are affected by wind.
    [JsonProperty]
    public float WindAffectednes { get; set; }

    public int LightEmission => 0;

    bool IParticlePropertiesProvider.DieInAir => DieInAir;

    bool IParticlePropertiesProvider.DieInLiquid => DieInLiquid;

    bool IParticlePropertiesProvider.SwimOnLiquid => SwimOnLiquid;

    public Vec3d Pos
    {
        get
        {
            tmpPos.Set(basePos.X + (double)PosOffset[0].nextFloat(), basePos.Y + (double)PosOffset[1].nextFloat(), basePos.Z + (double)PosOffset[2].nextFloat());
            return tmpPos;
        }
    }

    //
    // Summary:
    //     gets the quantity released.
    float IParticlePropertiesProvider.Quantity => Quantity.nextFloat();

    //
    // Summary:
    //     Gets the dynamic size of the particle.
    float IParticlePropertiesProvider.Size => Size.nextFloat();

    public Vec3f ParentVelocity { get; set; }

    public float WindAffectednesAtPos { get; set; }

    public float ParentVelocityWeight { get; set; }

    EnumParticleModel IParticlePropertiesProvider.ParticleModel => ParticleModel;

    bool IParticlePropertiesProvider.SelfPropelled => SelfPropelled;

    //
    // Summary:
    //     Gets the secondary spawn interval.
    float IParticlePropertiesProvider.SecondarySpawnInterval => SecondarySpawnInterval.nextFloat();

    bool IParticlePropertiesProvider.TerrainCollision => TerrainCollision;

    float IParticlePropertiesProvider.GravityEffect => GravityEffect.nextFloat();

    float IParticlePropertiesProvider.LifeLength => LifeLength.nextFloat();

    IParticlePropertiesProvider[] IParticlePropertiesProvider.SecondaryParticles => SecondaryParticles;

    IParticlePropertiesProvider[] IParticlePropertiesProvider.DeathParticles => DeathParticles;

    //
    // Summary:
    //     Initializes the particle.
    //
    // Parameters:
    //   api:
    //     The core API.
    public void Init(ICoreAPI api)
    {
    }

    //
    // Summary:
    //     Converts the color to RGBA.
    //
    // Parameters:
    //   capi:
    //     The Core Client API.
    //
    // Returns:
    //     The set RGBA color.
    public int GetRgbaColor(ICoreClientAPI capi)
    {
        if (HsvaColor == null)
        {
            return Color;
        }

        int num = ColorUtil.HsvToRgba((byte)GameMath.Clamp(HsvaColor[0].nextFloat(), 0f, 255f), (byte)GameMath.Clamp(HsvaColor[1].nextFloat(), 0f, 255f), (byte)GameMath.Clamp(HsvaColor[2].nextFloat(), 0f, 255f), (byte)GameMath.Clamp(HsvaColor[3].nextFloat(), 0f, 255f));
        int num2 = num & 0xFF;
        int num3 = (num >> 8) & 0xFF;
        int num4 = (num >> 16) & 0xFF;
        int num5 = (num >> 24) & 0xFF;
        return (num2 << 16) | (num3 << 8) | num4 | (num5 << 24);
    }

    //
    // Summary:
    //     Gets the velocity of the particle.
    public Vec3f GetVelocity(Vec3d pos)
    {
        tmpVelo.Set(baseVelocity.X + Velocity[0].nextFloat(), baseVelocity.Y + Velocity[1].nextFloat(), baseVelocity.Z + Velocity[2].nextFloat());
        return tmpVelo;
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write(basePos.X);
        writer.Write(basePos.Y);
        writer.Write(basePos.Z);
        writer.Write(DieInAir);
        writer.Write(DieInLiquid);
        writer.Write(SwimOnLiquid);
        for (int i = 0; i < 4; i++)
        {
            HsvaColor[i].ToBytes(writer);
        }

        GravityEffect.ToBytes(writer);
        LifeLength.ToBytes(writer);
        for (int j = 0; j < 3; j++)
        {
            PosOffset[j].ToBytes(writer);
        }

        Quantity.ToBytes(writer);
        Size.ToBytes(writer);
        for (int k = 0; k < 3; k++)
        {
            Velocity[k].ToBytes(writer);
        }

        writer.Write((byte)ParticleModel);
        writer.Write(VertexFlags);
        writer.Write(OpacityEvolve == null);
        if (OpacityEvolve != null)
        {
            OpacityEvolve.ToBytes(writer);
        }

        writer.Write(RedEvolve == null);
        if (RedEvolve != null)
        {
            RedEvolve.ToBytes(writer);
        }

        writer.Write(GreenEvolve == null);
        if (GreenEvolve != null)
        {
            GreenEvolve.ToBytes(writer);
        }

        writer.Write(BlueEvolve == null);
        if (BlueEvolve != null)
        {
            BlueEvolve.ToBytes(writer);
        }

        SizeEvolve.ToBytes(writer);
        writer.Write(SelfPropelled);
        writer.Write(TerrainCollision);
        writer.Write(ColorByBlock);
        writer.Write(VelocityEvolve != null);
        if (VelocityEvolve != null)
        {
            for (int l = 0; l < 3; l++)
            {
                VelocityEvolve[l].ToBytes(writer);
            }
        }

        SecondarySpawnInterval.ToBytes(writer);
        if (SecondaryParticles == null)
        {
            writer.Write(0);
        }
        else
        {
            writer.Write(SecondaryParticles.Length);
            for (int m = 0; m < SecondaryParticles.Length; m++)
            {
                SecondaryParticles[m].ToBytes(writer);
            }
        }

        if (DeathParticles == null)
        {
            writer.Write(0);
        }
        else
        {
            writer.Write(DeathParticles.Length);
            for (int n = 0; n < DeathParticles.Length; n++)
            {
                DeathParticles[n].ToBytes(writer);
            }
        }

        writer.Write(WindAffectednes);
        writer.Write(Bounciness);
    }

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        basePos = new Vec3d(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
        DieInAir = reader.ReadBoolean();
        DieInLiquid = reader.ReadBoolean();
        SwimOnLiquid = reader.ReadBoolean();
        HsvaColor = new NatFloat[4]
        {
            NatFloat.createFromBytes(reader),
            NatFloat.createFromBytes(reader),
            NatFloat.createFromBytes(reader),
            NatFloat.createFromBytes(reader)
        };
        GravityEffect = NatFloat.createFromBytes(reader);
        LifeLength = NatFloat.createFromBytes(reader);
        PosOffset = new NatFloat[3]
        {
            NatFloat.createFromBytes(reader),
            NatFloat.createFromBytes(reader),
            NatFloat.createFromBytes(reader)
        };
        Quantity = NatFloat.createFromBytes(reader);
        Size = NatFloat.createFromBytes(reader);
        Velocity = new NatFloat[3]
        {
            NatFloat.createFromBytes(reader),
            NatFloat.createFromBytes(reader),
            NatFloat.createFromBytes(reader)
        };
        ParticleModel = (EnumParticleModel)reader.ReadByte();
        VertexFlags = reader.ReadInt32();
        if (!reader.ReadBoolean())
        {
            OpacityEvolve = EvolvingNatFloat.CreateFromBytes(reader);
        }

        if (!reader.ReadBoolean())
        {
            RedEvolve = EvolvingNatFloat.CreateFromBytes(reader);
        }

        if (!reader.ReadBoolean())
        {
            GreenEvolve = EvolvingNatFloat.CreateFromBytes(reader);
        }

        if (!reader.ReadBoolean())
        {
            BlueEvolve = EvolvingNatFloat.CreateFromBytes(reader);
        }

        SizeEvolve.FromBytes(reader);
        SelfPropelled = reader.ReadBoolean();
        TerrainCollision = reader.ReadBoolean();
        ColorByBlock = reader.ReadBoolean();
        if (reader.ReadBoolean())
        {
            VelocityEvolve = new EvolvingNatFloat[3]
            {
                EvolvingNatFloat.createIdentical(0f),
                EvolvingNatFloat.createIdentical(0f),
                EvolvingNatFloat.createIdentical(0f)
            };
            VelocityEvolve[0].FromBytes(reader);
            VelocityEvolve[1].FromBytes(reader);
            VelocityEvolve[2].FromBytes(reader);
        }

        SecondarySpawnInterval = NatFloat.createFromBytes(reader);
        int num = reader.ReadInt32();
        if (num > 0)
        {
            SecondaryParticles = new AdvancedParticleProperties[num];
            for (int i = 0; i < num; i++)
            {
                SecondaryParticles[i] = createFromBytes(reader, resolver);
            }
        }

        int num2 = reader.ReadInt32();
        if (num2 > 0)
        {
            DeathParticles = new AdvancedParticleProperties[num2];
            for (int j = 0; j < num2; j++)
            {
                DeathParticles[j] = createFromBytes(reader, resolver);
            }
        }

        WindAffectednes = reader.ReadSingle();
        Bounciness = reader.ReadSingle();
    }

    public static AdvancedParticleProperties createFromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        AdvancedParticleProperties advancedParticleProperties = new AdvancedParticleProperties();
        advancedParticleProperties.FromBytes(reader, resolver);
        return advancedParticleProperties;
    }

    public AdvancedParticleProperties Clone()
    {
        AdvancedParticleProperties advancedParticleProperties = new AdvancedParticleProperties();
        using MemoryStream memoryStream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(memoryStream);
        ToBytes(writer);
        memoryStream.Position = 0L;
        advancedParticleProperties.FromBytes(new BinaryReader(memoryStream), null);
        return advancedParticleProperties;
    }

    //
    // Summary:
    //     Begins the advanced particle.
    public void BeginParticle()
    {
        if (WindAffectednes > 0f)
        {
            ParentVelocityWeight = WindAffectednesAtPos * WindAffectednes;
            ParentVelocity = GlobalConstants.CurrentWindSpeedClient;
        }
    }

    //
    // Summary:
    //     prepares the particle for secondary spawning.
    //
    // Parameters:
    //   particleInstance:
    public void PrepareForSecondarySpawn(ParticleBase particleInstance)
    {
        Vec3d position = particleInstance.Position;
        basePos.X = position.X;
        basePos.Y = position.Y;
        basePos.Z = position.Z;
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
