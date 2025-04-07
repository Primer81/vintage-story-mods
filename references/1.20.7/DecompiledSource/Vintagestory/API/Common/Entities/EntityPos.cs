#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.IO;
using ProtoBuf;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

//
// Summary:
//     Represents all positional information of an entity, such as coordinates, motion
//     and angles
[ProtoContract]
public class EntityPos
{
    [ProtoMember(1)]
    protected double x;

    [ProtoMember(2)]
    protected double y;

    [ProtoMember(3)]
    protected double z;

    [ProtoMember(4)]
    public int Dimension;

    [ProtoMember(5)]
    protected float roll;

    [ProtoMember(6)]
    protected float yaw;

    [ProtoMember(7)]
    protected float pitch;

    [ProtoMember(8)]
    protected int stance;

    //
    // Summary:
    //     The yaw of the agents head
    [ProtoMember(9)]
    public float HeadYaw;

    //
    // Summary:
    //     The pitch of the agents head
    [ProtoMember(10)]
    public float HeadPitch;

    [ProtoMember(11)]
    public Vec3d Motion = new Vec3d();

    //
    // Summary:
    //     The X position of the Entity.
    public virtual double X
    {
        get
        {
            return x;
        }
        set
        {
            x = value;
        }
    }

    //
    // Summary:
    //     The Y position of the Entity.
    public virtual double Y
    {
        get
        {
            return y;
        }
        set
        {
            y = value;
        }
    }

    public virtual double InternalY => y + (double)(Dimension * 32768);

    //
    // Summary:
    //     The Z position of the Entity.
    public virtual double Z
    {
        get
        {
            return z;
        }
        set
        {
            z = value;
        }
    }

    public virtual int DimensionYAdjustment => Dimension * 32768;

    //
    // Summary:
    //     The rotation around the X axis, in radians.
    public virtual float Roll
    {
        get
        {
            return roll;
        }
        set
        {
            roll = value;
        }
    }

    //
    // Summary:
    //     The rotation around the Y axis, in radians.
    public virtual float Yaw
    {
        get
        {
            return yaw;
        }
        set
        {
            yaw = value;
        }
    }

    //
    // Summary:
    //     The rotation around the Z axis, in radians.
    public virtual float Pitch
    {
        get
        {
            return pitch;
        }
        set
        {
            pitch = value;
        }
    }

    //
    // Summary:
    //     Returns the position as BlockPos object
    public BlockPos AsBlockPos => new BlockPos((int)x, (int)y, (int)z, Dimension);

    //
    // Summary:
    //     Returns the position as a Vec3i object
    public Vec3i XYZInt => new Vec3i((int)x, (int)InternalY, (int)z);

    //
    // Summary:
    //     Returns the position as a Vec3d object. Note, dimension aware
    public Vec3d XYZ => new Vec3d(x, InternalY, z);

    //
    // Summary:
    //     Returns the position as a Vec3f object
    public Vec3f XYZFloat => new Vec3f((float)x, (float)InternalY, (float)z);

    internal int XInt => (int)x;

    internal int YInt => (int)y;

    internal int ZInt => (int)z;

    //
    // Summary:
    //     Sets this position to a Vec3d, including setting the dimension
    //
    // Parameters:
    //   pos:
    //     The Vec3d to set to.
    public void SetPosWithDimension(Vec3d pos)
    {
        X = pos.X;
        y = pos.Y % 32768.0;
        z = pos.Z;
        Dimension = (int)pos.Y / 32768;
    }

    //
    // Summary:
    //     Sets this position to a Vec3d, without dimension information - needed in some
    //     situations where no dimension change is intended
    //
    // Parameters:
    //   pos:
    //     The Vec3d to set to.
    public void SetPos(Vec3d pos)
    {
        X = pos.X;
        y = pos.Y;
        z = pos.Z;
    }

    public EntityPos()
    {
    }

    public EntityPos(double x, double y, double z, float heading = 0f, float pitch = 0f, float roll = 0f)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        yaw = heading;
        this.pitch = pitch;
        this.roll = roll;
    }

    //
    // Summary:
    //     Adds given position offset
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    // Returns:
    //     Returns itself
    public EntityPos Add(double x, double y, double z)
    {
        X += x;
        this.y += y;
        this.z += z;
        return this;
    }

    //
    // Summary:
    //     Adds given position offset
    //
    // Parameters:
    //   vec:
    //
    // Returns:
    //     Returns itself
    public EntityPos Add(Vec3f vec)
    {
        X += vec.X;
        y += vec.Y;
        z += vec.Z;
        return this;
    }

    //
    // Summary:
    //     Sets the entity position.
    public EntityPos SetPos(int x, int y, int z)
    {
        X = x;
        this.y = y;
        this.z = z;
        return this;
    }

    //
    // Summary:
    //     Sets the entity position.
    public EntityPos SetPos(BlockPos pos)
    {
        X = pos.X;
        y = pos.Y;
        z = pos.Z;
        return this;
    }

    //
    // Summary:
    //     Sets the entity position.
    public EntityPos SetPos(double x, double y, double z)
    {
        X = x;
        this.y = y;
        this.z = z;
        return this;
    }

    //
    // Summary:
    //     Sets the entity position.
    public EntityPos SetPos(EntityPos pos)
    {
        X = pos.x;
        y = pos.y;
        z = pos.z;
        return this;
    }

    //
    // Summary:
    //     Sets the entity angles.
    //
    // Parameters:
    //   pos:
    public EntityPos SetAngles(EntityPos pos)
    {
        Roll = pos.roll;
        yaw = pos.yaw;
        pitch = pos.pitch;
        HeadPitch = pos.HeadPitch;
        HeadYaw = pos.HeadYaw;
        return this;
    }

    //
    // Summary:
    //     Sets the entity position.
    public EntityPos SetAngles(float roll, float yaw, float pitch)
    {
        Roll = roll;
        this.yaw = yaw;
        this.pitch = pitch;
        return this;
    }

    //
    // Summary:
    //     Sets the Yaw of this entity.
    //
    // Parameters:
    //   yaw:
    public EntityPos SetYaw(float yaw)
    {
        Yaw = yaw;
        return this;
    }

    //
    // Summary:
    //     Returns true if the entity is within given distance of the other entity
    //
    // Parameters:
    //   position:
    //
    //   squareDistance:
    public bool InRangeOf(EntityPos position, int squareDistance)
    {
        double num = x - position.x;
        double num2 = InternalY - position.InternalY;
        double num3 = z - position.z;
        return num * num + num2 * num2 + num3 * num3 <= (double)squareDistance;
    }

    //
    // Summary:
    //     Returns true if the entity is within given distance of given position
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   squareDistance:
    public bool InRangeOf(int x, int y, int z, float squareDistance)
    {
        double num = this.x - (double)x;
        double num2 = InternalY - (double)y;
        double num3 = this.z - (double)z;
        return num * num + num2 * num2 + num3 * num3 <= (double)squareDistance;
    }

    //
    // Summary:
    //     Returns true if the entity is within given distance of given position
    //
    // Parameters:
    //   x:
    //
    //   z:
    //
    //   squareDistance:
    public bool InHorizontalRangeOf(int x, int z, float squareDistance)
    {
        double num = this.x - (double)x;
        double num2 = this.z - (double)z;
        return num * num + num2 * num2 <= (double)squareDistance;
    }

    //
    // Summary:
    //     Returns true if the entity is within given distance of given position
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    //   squareDistance:
    public bool InRangeOf(double x, double y, double z, float squareDistance)
    {
        double num = this.x - x;
        double num2 = InternalY - y;
        double num3 = this.z - z;
        return num * num + num2 * num2 + num3 * num3 <= (double)squareDistance;
    }

    //
    // Summary:
    //     Returns true if the entity is within given distance of given block position
    //
    // Parameters:
    //   pos:
    //
    //   squareDistance:
    public bool InRangeOf(BlockPos pos, float squareDistance)
    {
        double num = x - (double)pos.X;
        double num2 = InternalY - (double)pos.InternalY;
        double num3 = z - (double)pos.Z;
        return num * num + num2 * num2 + num3 * num3 <= (double)squareDistance;
    }

    //
    // Summary:
    //     Returns true if the entity is within given distance of given position
    //
    // Parameters:
    //   pos:
    //
    //   squareDistance:
    public bool InRangeOf(Vec3f pos, float squareDistance)
    {
        double num = x - (double)pos.X;
        double num2 = InternalY - (double)pos.Y;
        double num3 = z - (double)pos.Z;
        return num * num + num2 * num2 + num3 * num3 <= (double)squareDistance;
    }

    //
    // Summary:
    //     Returns true if the entity is within given distance of given position
    //
    // Parameters:
    //   position:
    //
    //   horRangeSq:
    //
    //   vertRange:
    public bool InRangeOf(Vec3d position, float horRangeSq, float vertRange)
    {
        double num = x - position.X;
        double num2 = z - position.Z;
        if (num * num + num2 * num2 > (double)horRangeSq)
        {
            return false;
        }

        return Math.Abs(InternalY - position.Y) <= (double)vertRange;
    }

    //
    // Summary:
    //     Returns the squared distance of the entity to this position
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public float SquareDistanceTo(float x, float y, float z)
    {
        double num = this.x - (double)x;
        double num2 = InternalY - (double)y;
        double num3 = this.z - (double)z;
        return (float)(num * num + num2 * num2 + num3 * num3);
    }

    //
    // Summary:
    //     Returns the squared distance of the entity to this position. Note: dimension
    //     aware, this requires the parameter y coordinate also to be based on InternalY
    //     as it should be (like EntityPos.XYZ)
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public float SquareDistanceTo(double x, double y, double z)
    {
        double num = this.x - x;
        double num2 = InternalY - y;
        double num3 = this.z - z;
        return (float)(num * num + num2 * num2 + num3 * num3);
    }

    //
    // Summary:
    //     Returns the squared distance of the entity to this position. Note: dimension
    //     aware, this requires the parameter Vec3d pos.Y coordinate also to be based on
    //     InternalY as it should be (like EntityPos.XYZ)
    //
    // Parameters:
    //   pos:
    public double SquareDistanceTo(Vec3d pos)
    {
        double num = x - pos.X;
        double num2 = InternalY - pos.Y;
        double num3 = z - pos.Z;
        return num * num + num2 * num2 + num3 * num3;
    }

    //
    // Summary:
    //     Returns the horizontal squared distance of the entity to this position
    //
    // Parameters:
    //   pos:
    public double SquareHorDistanceTo(Vec3d pos)
    {
        double num = x - pos.X;
        double num2 = z - pos.Z;
        return num * num + num2 * num2;
    }

    public double DistanceTo(Vec3d pos)
    {
        double num = x - pos.X;
        double num2 = InternalY - pos.Y;
        double num3 = z - pos.Z;
        return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3);
    }

    public double DistanceTo(EntityPos pos)
    {
        double num = x - pos.x;
        double num2 = InternalY - pos.InternalY;
        double num3 = z - pos.z;
        return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3);
    }

    public double HorDistanceTo(Vec3d pos)
    {
        double num = x - pos.X;
        double num2 = z - pos.Z;
        return GameMath.Sqrt(num * num + num2 * num2);
    }

    public double HorDistanceTo(EntityPos pos)
    {
        double num = x - pos.x;
        double num2 = z - pos.z;
        return GameMath.Sqrt(num * num + num2 * num2);
    }

    //
    // Summary:
    //     Returns the squared distance of the entity to this position
    //
    // Parameters:
    //   pos:
    public float SquareDistanceTo(EntityPos pos)
    {
        double num = x - pos.x;
        double num2 = InternalY - pos.InternalY;
        double num3 = z - pos.z;
        return (float)(num * num + num2 * num2 + num3 * num3);
    }

    //
    // Summary:
    //     Creates a full copy
    public EntityPos Copy()
    {
        return new EntityPos
        {
            X = x,
            y = y,
            z = z,
            yaw = yaw,
            pitch = pitch,
            roll = roll,
            HeadYaw = HeadYaw,
            HeadPitch = HeadPitch,
            Motion = new Vec3d(Motion.X, Motion.Y, Motion.Z),
            Dimension = Dimension
        };
    }

    //
    // Summary:
    //     Same as AheadCopy(1) - AheadCopy(0)
    public Vec3f GetViewVector()
    {
        return GetViewVector(pitch, yaw);
    }

    //
    // Summary:
    //     Same as AheadCopy(1) - AheadCopy(0)
    public static Vec3f GetViewVector(float pitch, float yaw)
    {
        float num = GameMath.Cos(pitch);
        float num2 = GameMath.Sin(pitch);
        float num3 = GameMath.Cos(yaw);
        float num4 = GameMath.Sin(yaw);
        return new Vec3f((0f - num) * num4, num2, (0f - num) * num3);
    }

    //
    // Summary:
    //     Returns a new entity position that is in front of the position the entity is
    //     currently looking at
    //
    // Parameters:
    //   offset:
    public EntityPos AheadCopy(double offset)
    {
        float num = GameMath.Cos(pitch);
        float num2 = GameMath.Sin(pitch);
        float num3 = GameMath.Cos(yaw);
        float num4 = GameMath.Sin(yaw);
        return new EntityPos(x - (double)(num * num4) * offset, y + (double)num2 * offset, z - (double)(num * num3) * offset, yaw, pitch, roll)
        {
            Dimension = Dimension
        };
    }

    //
    // Summary:
    //     Returns a new entity position that is in front of the position the entity is
    //     currently looking at using only the entities yaw, meaning the resulting coordinate
    //     will be always at the same y position.
    //
    // Parameters:
    //   offset:
    public EntityPos HorizontalAheadCopy(double offset)
    {
        float num = GameMath.Cos(yaw);
        float num2 = GameMath.Sin(yaw);
        return new EntityPos(x + (double)num2 * offset, y, z + (double)num * offset, yaw, pitch, roll)
        {
            Dimension = Dimension
        };
    }

    //
    // Summary:
    //     Returns a new entity position that is behind of the position the entity is currently
    //     looking at
    //
    // Parameters:
    //   offset:
    public EntityPos BehindCopy(double offset)
    {
        float num = GameMath.Cos(0f - yaw);
        float num2 = GameMath.Sin(0f - yaw);
        return new EntityPos(x + (double)num2 * offset, y, z + (double)num * offset, yaw, pitch, roll)
        {
            Dimension = Dimension
        };
    }

    //
    // Summary:
    //     Makes a "basiclly equals" check on the position, motions and angles using a small
    //     tolerance of epsilon=0.0001f
    //
    // Parameters:
    //   pos:
    //
    //   epsilon:
    public bool BasicallySameAs(EntityPos pos, double epsilon = 0.0001)
    {
        double num = epsilon * epsilon;
        if (GameMath.SumOfSquares(x - pos.x, y - pos.y, z - pos.z) >= num)
        {
            return false;
        }

        if (GameMath.Square(roll - pos.roll) < num && GameMath.Square(yaw - pos.yaw) < num && GameMath.Square(pitch - pos.pitch) < num)
        {
            return GameMath.SumOfSquares(Motion.X - pos.Motion.X, Motion.Y - pos.Motion.Y, Motion.Z - pos.Motion.Z) < num;
        }

        return false;
    }

    //
    // Summary:
    //     Makes a "basiclly equals" check on the position, motions and angles using a small
    //     tolerance of epsilon=0.0001f. Ignores motion
    //
    // Parameters:
    //   pos:
    //
    //   epsilon:
    public bool BasicallySameAsIgnoreMotion(EntityPos pos, double epsilon = 0.0001)
    {
        double num = epsilon * epsilon;
        if (GameMath.Square(x - pos.x) >= num || GameMath.Square(y - pos.y) >= num || GameMath.Square(z - pos.z) >= num)
        {
            return false;
        }

        if (GameMath.Square(roll - pos.roll) < num && GameMath.Square(yaw - pos.yaw) < num)
        {
            return GameMath.Square(pitch - pos.pitch) < num;
        }

        return false;
    }

    //
    // Summary:
    //     Makes a "basiclly equals" check on position and motions using a small tolerance
    //     of epsilon=0.0001f. Ignores the entities angles.
    //
    // Parameters:
    //   pos:
    //
    //   epsilon:
    public bool BasicallySameAsIgnoreAngles(EntityPos pos, double epsilon = 0.0001)
    {
        double num = epsilon * epsilon;
        if (GameMath.SumOfSquares(x - pos.x, y - pos.y, z - pos.z) < num)
        {
            return GameMath.SumOfSquares(Motion.X - pos.Motion.X, Motion.Y - pos.Motion.Y, Motion.Z - pos.Motion.Z) < num;
        }

        return false;
    }

    //
    // Summary:
    //     Loads the position and angles from given entity position.
    //
    // Parameters:
    //   pos:
    //
    // Returns:
    //     Returns itself
    public EntityPos SetFrom(EntityPos pos)
    {
        X = pos.x;
        y = pos.y;
        z = pos.z;
        Dimension = pos.Dimension;
        roll = pos.roll;
        yaw = pos.yaw;
        pitch = pos.pitch;
        Motion.Set(pos.Motion);
        HeadYaw = pos.HeadYaw;
        HeadPitch = pos.HeadPitch;
        return this;
    }

    //
    // Summary:
    //     Loads the position from given position.
    //
    // Parameters:
    //   pos:
    //
    // Returns:
    //     Returns itself
    public EntityPos SetFrom(Vec3d pos)
    {
        X = pos.X;
        y = pos.Y;
        z = pos.Z;
        return this;
    }

    public override string ToString()
    {
        return "XYZ: " + X + "/" + Y + "/" + Z + ", YPR " + Yaw + "/" + Pitch + "/" + Roll + ", Dim " + Dimension;
    }

    public string OnlyPosToString()
    {
        return X.ToString("#.##", GlobalConstants.DefaultCultureInfo) + ", " + Y.ToString("#.##", GlobalConstants.DefaultCultureInfo) + ", " + Z.ToString("#.##", GlobalConstants.DefaultCultureInfo);
    }

    public string OnlyAnglesToString()
    {
        return roll.ToString("#.##", GlobalConstants.DefaultCultureInfo) + ", " + yaw.ToString("#.##", GlobalConstants.DefaultCultureInfo) + pitch.ToString("#.##", GlobalConstants.DefaultCultureInfo);
    }

    //
    // Summary:
    //     Serializes all positional information. Does not write HeadYaw and HeadPitch.
    //
    //
    // Parameters:
    //   writer:
    public void ToBytes(BinaryWriter writer)
    {
        writer.Write(x);
        writer.Write(InternalY);
        writer.Write(z);
        writer.Write(roll);
        writer.Write(yaw);
        writer.Write(pitch);
        writer.Write(stance);
        writer.Write(Motion.X);
        writer.Write(Motion.Y);
        writer.Write(Motion.Z);
    }

    //
    // Summary:
    //     Deserializes all positional information. Does not read HeadYaw and HeadPitch
    //
    //
    // Parameters:
    //   reader:
    public void FromBytes(BinaryReader reader)
    {
        x = reader.ReadDouble();
        y = reader.ReadDouble();
        Dimension = (int)y / 32768;
        y -= Dimension * 32768;
        z = reader.ReadDouble();
        roll = reader.ReadSingle();
        yaw = reader.ReadSingle();
        pitch = reader.ReadSingle();
        stance = reader.ReadInt32();
        Motion.X = reader.ReadDouble();
        Motion.Y = reader.ReadDouble();
        Motion.Z = reader.ReadDouble();
    }

    public bool AnyNaN()
    {
        if (double.IsNaN(x + y + z))
        {
            return true;
        }

        if (float.IsNaN(roll + yaw + pitch))
        {
            return true;
        }

        if (double.IsNaN(Motion.X + Motion.Y + Motion.Z))
        {
            return true;
        }

        if (Math.Abs(x) + Math.Abs(y) + Math.Abs(z) > 268435456.0)
        {
            return true;
        }

        return false;
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
