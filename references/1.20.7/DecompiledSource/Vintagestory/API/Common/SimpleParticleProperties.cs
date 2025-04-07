#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.IO;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

//
// Summary:
//     A configurable implementation of IParticlePropertiesProvider
public class SimpleParticleProperties : IParticlePropertiesProvider
{
    public static ThreadLocal<Random> randTL = new ThreadLocal<Random>(() => new Random());

    public float MinQuantity;

    public float AddQuantity;

    public float WindAffectednes;

    public Vec3d MinPos;

    public Vec3d AddPos = new Vec3d();

    public Vec3f MinVelocity = new Vec3f();

    public Vec3f AddVelocity = new Vec3f();

    public float LifeLength;

    public float addLifeLength;

    public float MinSize = 1f;

    public float MaxSize = 1f;

    public int Color;

    public bool SelfPropelled;

    public Block ColorByBlock;

    //
    // Summary:
    //     The color map for climate color mapping. Leave null for no coloring by climate
    public string ClimateColorMap;

    //
    // Summary:
    //     The color map for season color mapping. Leave null for no coloring by season
    public string SeasonColorMap;

    protected Vec3d tmpPos = new Vec3d();

    private Vec3f tmpVelo = new Vec3f();

    public static Random rand => randTL.Value;

    public Vec3f ParentVelocity { get; set; }

    public float ParentVelocityWeight { get; set; }

    public float GravityEffect { get; set; }

    public int LightEmission { get; set; }

    public int VertexFlags { get; set; }

    public bool Async { get; set; }

    public float Bounciness { get; set; }

    public bool ShouldDieInAir { get; set; }

    public bool ShouldDieInLiquid { get; set; }

    public bool ShouldSwimOnLiquid { get; set; }

    public bool WithTerrainCollision { get; set; } = true;


    public EvolvingNatFloat OpacityEvolve { get; set; }

    public EvolvingNatFloat RedEvolve { get; set; }

    public EvolvingNatFloat GreenEvolve { get; set; }

    public EvolvingNatFloat BlueEvolve { get; set; }

    public EvolvingNatFloat SizeEvolve { get; set; }

    public bool RandomVelocityChange { get; set; }

    public bool DieInAir => ShouldDieInAir;

    public bool DieInLiquid => ShouldDieInLiquid;

    public bool SwimOnLiquid => ShouldSwimOnLiquid;

    public float Quantity => MinQuantity + (float)rand.NextDouble() * AddQuantity;

    public virtual Vec3d Pos
    {
        get
        {
            tmpPos.Set(MinPos.X + AddPos.X * rand.NextDouble(), MinPos.Y + AddPos.Y * rand.NextDouble(), MinPos.Z + AddPos.Z * rand.NextDouble());
            return tmpPos;
        }
    }

    public float Size => MinSize + (float)rand.NextDouble() * (MaxSize - MinSize);

    float IParticlePropertiesProvider.LifeLength => LifeLength + addLifeLength * (float)rand.NextDouble();

    public EnumParticleModel ParticleModel { get; set; }

    public EvolvingNatFloat[] VelocityEvolve => null;

    bool IParticlePropertiesProvider.SelfPropelled => SelfPropelled;

    public bool TerrainCollision => WithTerrainCollision;

    public IParticlePropertiesProvider[] SecondaryParticles { get; set; }

    public IParticlePropertiesProvider[] DeathParticles { get; set; }

    public float SecondarySpawnInterval => 0f;

    public bool DieOnRainHeightmap { get; set; }

    public bool WindAffected { get; set; }

    public SimpleParticleProperties()
    {
    }

    public void Init(ICoreAPI api)
    {
    }

    public SimpleParticleProperties(float minQuantity, float maxQuantity, int color, Vec3d minPos, Vec3d maxPos, Vec3f minVelocity, Vec3f maxVelocity, float lifeLength = 1f, float gravityEffect = 1f, float minSize = 1f, float maxSize = 1f, EnumParticleModel model = EnumParticleModel.Cube)
    {
        MinQuantity = minQuantity;
        AddQuantity = maxQuantity - minQuantity;
        Color = color;
        MinPos = minPos;
        AddPos = maxPos - minPos;
        MinVelocity = minVelocity;
        AddVelocity = maxVelocity - minVelocity;
        LifeLength = lifeLength;
        GravityEffect = gravityEffect;
        MinSize = minSize;
        MaxSize = maxSize;
        ParticleModel = model;
    }

    public Vec3f GetVelocity(Vec3d pos)
    {
        tmpVelo.Set(MinVelocity.X + AddVelocity.X * (float)rand.NextDouble(), MinVelocity.Y + AddVelocity.Y * (float)rand.NextDouble(), MinVelocity.Z + AddVelocity.Z * (float)rand.NextDouble());
        return tmpVelo;
    }

    public int GetRgbaColor(ICoreClientAPI capi)
    {
        if (ColorByBlock != null)
        {
            return ColorByBlock.GetRandomColor(capi, new ItemStack(ColorByBlock));
        }

        if (SeasonColorMap != null || ClimateColorMap != null)
        {
            return capi.World.ApplyColorMapOnRgba(ClimateColorMap, SeasonColorMap, Color, (int)MinPos.X, (int)MinPos.Y, (int)MinPos.Z);
        }

        return Color;
    }

    public bool UseLighting()
    {
        return true;
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write(MinQuantity);
        writer.Write(AddQuantity);
        MinPos.ToBytes(writer);
        AddPos.ToBytes(writer);
        MinVelocity.ToBytes(writer);
        AddVelocity.ToBytes(writer);
        writer.Write(LifeLength);
        writer.Write(GravityEffect);
        writer.Write(MinSize);
        writer.Write(MaxSize);
        writer.Write(Color);
        writer.Write(VertexFlags);
        writer.Write((int)ParticleModel);
        writer.Write(ShouldDieInAir);
        writer.Write(ShouldDieInLiquid);
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

        writer.Write(SizeEvolve == null);
        if (SizeEvolve != null)
        {
            SizeEvolve.ToBytes(writer);
        }

        writer.Write(SelfPropelled);
        writer.Write(ColorByBlock == null);
        if (ColorByBlock != null)
        {
            writer.Write(ColorByBlock.BlockId);
        }

        writer.Write(ClimateColorMap == null);
        if (ClimateColorMap != null)
        {
            writer.Write(ClimateColorMap);
        }

        writer.Write(SeasonColorMap == null);
        if (SeasonColorMap != null)
        {
            writer.Write(SeasonColorMap);
        }

        writer.Write(Bounciness);
        writer.Write(Async);
    }

    public void FromBytes(BinaryReader reader, IWorldAccessor resolver)
    {
        MinQuantity = reader.ReadSingle();
        AddQuantity = reader.ReadSingle();
        MinPos = Vec3d.CreateFromBytes(reader);
        AddPos = Vec3d.CreateFromBytes(reader);
        MinVelocity = Vec3f.CreateFromBytes(reader);
        AddVelocity = Vec3f.CreateFromBytes(reader);
        LifeLength = reader.ReadSingle();
        GravityEffect = reader.ReadSingle();
        MinSize = reader.ReadSingle();
        MaxSize = reader.ReadSingle();
        Color = reader.ReadInt32();
        VertexFlags = reader.ReadInt32();
        ParticleModel = (EnumParticleModel)reader.ReadInt32();
        ShouldDieInAir = reader.ReadBoolean();
        ShouldDieInLiquid = reader.ReadBoolean();
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

        if (!reader.ReadBoolean())
        {
            SizeEvolve = EvolvingNatFloat.CreateFromBytes(reader);
        }

        SelfPropelled = reader.ReadBoolean();
        if (!reader.ReadBoolean())
        {
            ColorByBlock = resolver.Blocks[reader.ReadInt32()];
        }

        if (!reader.ReadBoolean())
        {
            ClimateColorMap = reader.ReadString();
        }

        if (!reader.ReadBoolean())
        {
            SeasonColorMap = reader.ReadString();
        }

        Bounciness = reader.ReadSingle();
        Async = reader.ReadBoolean();
    }

    public void BeginParticle()
    {
        if (WindAffectednes > 0f)
        {
            ParentVelocityWeight = WindAffectednes;
            ParentVelocity = GlobalConstants.CurrentWindSpeedClient;
        }
    }

    public void PrepareForSecondarySpawn(ParticleBase particleInstance)
    {
        Vec3d position = particleInstance.Position;
        MinPos.X = position.X;
        MinPos.Y = position.Y;
        MinPos.Z = position.Z;
    }

    public SimpleParticleProperties Clone(IWorldAccessor worldForResovle)
    {
        SimpleParticleProperties simpleParticleProperties = new SimpleParticleProperties();
        using MemoryStream memoryStream = new MemoryStream();
        using (BinaryWriter writer = new BinaryWriter(memoryStream))
        {
            ToBytes(writer);
        }

        memoryStream.Position = 0L;
        using BinaryReader reader = new BinaryReader(memoryStream);
        simpleParticleProperties.FromBytes(reader, worldForResovle);
        return simpleParticleProperties;
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
