#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using ProtoBuf;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.MathTools;

[ProtoContract]
public class Cuboidi : ICuboid<int, Cuboidi>, IEquatable<Cuboidi>
{
    [ProtoMember(1)]
    public int X1;

    [ProtoMember(2)]
    public int Y1;

    [ProtoMember(3)]
    public int Z1;

    [ProtoMember(4)]
    public int X2;

    [ProtoMember(5)]
    public int Y2;

    [ProtoMember(6)]
    public int Z2;

    public int[] Coordinates => new int[6] { X1, Y1, Z1, X2, Y2, Z2 };

    public int MinX => Math.Min(X1, X2);

    public int MinY => Math.Min(Y1, Y2);

    public int MinZ => Math.Min(Z1, Z2);

    public int MaxX => Math.Max(X1, X2);

    public int MaxY => Math.Max(Y1, Y2);

    public int MaxZ => Math.Max(Z1, Z2);

    public int SizeX => MaxX - MinX;

    public int SizeY => MaxY - MinY;

    public int SizeZ => MaxZ - MinZ;

    public int SizeXYZ => SizeX * SizeY * SizeZ;

    public int SizeXZ => SizeX * SizeZ;

    public Vec3i Start => new Vec3i(X1, Y1, Z1);

    public Vec3i End => new Vec3i(X2, Y2, Z2);

    public Vec3i Center => new Vec3i((X1 + X2) / 2, (Y1 + Y2) / 2, (Z1 + Z2) / 2);

    public int CenterX => (X1 + X2) / 2;

    public int CenterY => (Y1 + Y2) / 2;

    public int CenterZ => (Z1 + Z2) / 2;

    public int Volume => SizeX * SizeY * SizeZ;

    public Cuboidi()
    {
    }

    public Cuboidi(int[] coordinates)
    {
        Set(coordinates[0], coordinates[1], coordinates[2], coordinates[3], coordinates[4], coordinates[5]);
    }

    public Cuboidi(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        Set(x1, y1, z1, x2, y2, z2);
    }

    public Cuboidi(BlockPos startPos, BlockPos endPos)
    {
        Set(startPos.X, startPos.Y, startPos.Z, endPos.X, endPos.Y, endPos.Z);
    }

    public Cuboidi(Vec3i startPos, Vec3i endPos)
    {
        Set(startPos.X, startPos.Y, startPos.Z, endPos.X, endPos.Y, endPos.Z);
    }

    public Cuboidi(BlockPos startPos, int size)
    {
        Set(startPos.X, startPos.Y, startPos.Z, startPos.X + size, startPos.Y + size, startPos.Z + size);
    }

    //
    // Summary:
    //     Sets the minimum and maximum values of the cuboid
    public Cuboidi Set(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        X1 = x1;
        Y1 = y1;
        Z1 = z1;
        X2 = x2;
        Y2 = y2;
        Z2 = z2;
        return this;
    }

    //
    // Summary:
    //     Sets the minimum and maximum values of the cuboid
    public Cuboidi Set(IVec3 min, IVec3 max)
    {
        Set(min.XAsInt, min.YAsInt, min.ZAsInt, max.XAsInt, max.YAsInt, max.ZAsInt);
        return this;
    }

    //
    // Summary:
    //     Adds the given offset to the cuboid
    public Cuboidi Translate(int posX, int posY, int posZ)
    {
        X1 += posX;
        Y1 += posY;
        Z1 += posZ;
        X2 += posX;
        Y2 += posY;
        Z2 += posZ;
        return this;
    }

    //
    // Summary:
    //     Adds the given offset to the cuboid
    public Cuboidi Translate(IVec3 vec)
    {
        Translate(vec.XAsInt, vec.YAsInt, vec.ZAsInt);
        return this;
    }

    //
    // Summary:
    //     Substractes the given offset to the cuboid
    public Cuboidi Sub(int posX, int posY, int posZ)
    {
        X1 -= posX;
        Y1 -= posY;
        Z1 -= posZ;
        X2 -= posX;
        Y2 -= posY;
        Z2 -= posZ;
        return this;
    }

    //
    // Summary:
    //     Divides the given value to the cuboid
    public Cuboidi Div(int value)
    {
        X1 /= value;
        Y1 /= value;
        Z1 /= value;
        X2 /= value;
        Y2 /= value;
        Z2 /= value;
        return this;
    }

    //
    // Summary:
    //     Substractes the given offset to the cuboid
    public Cuboidi Sub(IVec3 vec)
    {
        Sub(vec.XAsInt, vec.YAsInt, vec.ZAsInt);
        return this;
    }

    public bool Contains(Vec3d pos)
    {
        if (pos.X >= (double)MinX && pos.X < (double)MaxX && pos.Y >= (double)MinY && pos.Y < (double)MaxY && pos.Z >= (double)MinZ)
        {
            return pos.Z < (double)MaxZ;
        }

        return false;
    }

    public bool Contains(IVec3 pos)
    {
        if (pos.XAsInt >= MinX && pos.XAsInt < MaxX && pos.YAsInt >= MinY && pos.YAsInt < MaxY && pos.ZAsInt >= MinZ)
        {
            return pos.ZAsInt < MaxZ;
        }

        return false;
    }

    //
    // Summary:
    //     Returns if the given point is inside the cuboid
    public bool Contains(int x, int y, int z)
    {
        if (x >= MinX && x < MaxX && y >= MinY && y < MaxY && z >= MinZ)
        {
            return z < MaxZ;
        }

        return false;
    }

    //
    // Summary:
    //     Returns if the given point is inside the cuboid
    public bool Contains(int x, int z)
    {
        if (x >= MinX && x < MaxX && z >= MinZ)
        {
            return z < MaxZ;
        }

        return false;
    }

    //
    // Summary:
    //     Returns if the given point is inside the cuboid
    public bool Contains(BlockPos pos)
    {
        if (pos.X >= MinX && pos.X < MaxX && pos.InternalY >= MinY && pos.InternalY < MaxY && pos.Z >= MinZ)
        {
            return pos.Z < MaxZ;
        }

        return false;
    }

    //
    // Summary:
    //     Returns if the given point is inside the cuboid
    public bool ContainsOrTouches(int x, int y, int z)
    {
        if (x >= MinX && x <= MaxX && y >= MinY && y <= MaxY && z >= MinZ)
        {
            return z <= MaxZ;
        }

        return false;
    }

    public bool ContainsOrTouches(Cuboidi cuboid)
    {
        if (ContainsOrTouches(cuboid.MinX, cuboid.MinY, cuboid.MinZ))
        {
            return ContainsOrTouches(cuboid.MaxX, cuboid.MaxY, cuboid.MaxZ);
        }

        return false;
    }

    //
    // Summary:
    //     Returns if the given point is inside the cuboid
    public bool ContainsOrTouches(BlockPos pos)
    {
        if (pos.X >= MinX && pos.X <= MaxX && pos.InternalY >= MinY && pos.InternalY <= MaxY && pos.Z >= MinZ)
        {
            return pos.Z <= MaxZ;
        }

        return false;
    }

    //
    // Summary:
    //     Returns if the given point is inside the cuboid
    public bool ContainsOrTouches(IVec3 vec)
    {
        return ContainsOrTouches(vec.XAsInt, vec.YAsInt, vec.ZAsInt);
    }

    //
    // Summary:
    //     Returns if the given entityPos is inside the cuboid
    public bool ContainsOrTouches(EntityPos pos)
    {
        if (pos.X >= (double)MinX && pos.X <= (double)MaxX && pos.Y >= (double)MinY && pos.Y <= (double)MaxY && pos.Z >= (double)MinZ)
        {
            return pos.Z <= (double)MaxZ;
        }

        return false;
    }

    //
    // Summary:
    //     Grows the cuboid so that it includes the given block
    public Cuboidi GrowToInclude(int x, int y, int z)
    {
        X1 = Math.Min(X1, x);
        Y1 = Math.Min(Y1, y);
        Z1 = Math.Min(Z1, z);
        X2 = Math.Max(X2, x);
        Y2 = Math.Max(Y2, y);
        Z2 = Math.Max(Z2, z);
        return this;
    }

    //
    // Summary:
    //     Grows the cuboid so that it includes the given block
    public Cuboidi GrowToInclude(IVec3 vec)
    {
        GrowToInclude(vec.XAsInt, vec.YAsInt, vec.ZAsInt);
        return this;
    }

    public Cuboidi GrowBy(int dx, int dy, int dz)
    {
        X1 -= dx;
        X2 += dx;
        Y1 -= dy;
        Y2 += dy;
        Z1 -= dz;
        Z2 += dz;
        return this;
    }

    //
    // Summary:
    //     Returns the shortest distance between given point and any point inside the cuboid
    public double ShortestDistanceFrom(int x, int y, int z)
    {
        double num = GameMath.Clamp(x, X1, X2);
        double num2 = GameMath.Clamp(y, Y1, Y2);
        double num3 = GameMath.Clamp(z, Z1, Z2);
        return Math.Sqrt(((double)x - num) * ((double)x - num) + ((double)y - num2) * ((double)y - num2) + ((double)z - num3) * ((double)z - num3));
    }

    //
    // Summary:
    //     Returns the shortest distance between given point and any point inside the cuboid
    public double ShortestDistanceFrom(IVec3 vec)
    {
        return ShortestDistanceFrom(vec.XAsInt, vec.YAsInt, vec.ZAsInt);
    }

    //
    // Summary:
    //     Returns the shortest distance to any point between this and given cuboid
    public double ShortestDistanceFrom(Cuboidi cuboid)
    {
        double num = cuboid.X1 - GameMath.Clamp(cuboid.X1, X1, X2);
        double num2 = cuboid.Y1 - GameMath.Clamp(cuboid.Y1, Y1, Y2);
        double num3 = cuboid.Z1 - GameMath.Clamp(cuboid.Z1, Z1, Z2);
        double num4 = cuboid.X2 - GameMath.Clamp(cuboid.X2, X1, X2);
        double num5 = cuboid.Y2 - GameMath.Clamp(cuboid.Y2, Y1, Y2);
        double num6 = cuboid.Z2 - GameMath.Clamp(cuboid.Z2, Z1, Z2);
        return Math.Sqrt(Math.Min(num * num, num4 * num4) + Math.Min(num2 * num2, num5 * num5) + Math.Min(num3 * num3, num6 * num6));
    }

    //
    // Summary:
    //     Returns a new x coordinate that's ensured to be outside this cuboid. Used for
    //     collision detection.
    public double pushOutX(Cuboidi from, int x, ref EnumPushDirection direction)
    {
        direction = EnumPushDirection.None;
        if (from.Y2 > Y1 && from.Y1 < Y2 && from.Z2 > Z1 && from.Z1 < Z2)
        {
            if ((double)x > 0.0 && from.X2 <= X1 && X1 - from.X2 < x)
            {
                direction = EnumPushDirection.Positive;
                x = X1 - from.X2;
            }
            else if ((double)x < 0.0 && from.X1 >= X2 && X2 - from.X1 > x)
            {
                direction = EnumPushDirection.Negative;
                x = X2 - from.X1;
            }
        }

        return x;
    }

    //
    // Summary:
    //     Returns a new y coordinate that's ensured to be outside this cuboid. Used for
    //     collision detection.
    public double pushOutY(Cuboidi from, int y, ref EnumPushDirection direction)
    {
        direction = EnumPushDirection.None;
        if (from.X2 > X1 && from.X1 < X2 && from.Z2 > Z1 && from.Z1 < Z2)
        {
            if ((double)y > 0.0 && from.Y2 <= Y1 && Y1 - from.Y2 < y)
            {
                direction = EnumPushDirection.Positive;
                y = Y1 - from.Y2;
            }
            else if ((double)y < 0.0 && from.Y1 >= Y2 && Y2 - from.Y1 > y)
            {
                direction = EnumPushDirection.Negative;
                y = Y2 - from.Y1;
            }
        }

        return y;
    }

    //
    // Summary:
    //     Returns a new z coordinate that's ensured to be outside this cuboid. Used for
    //     collision detection.
    public double pushOutZ(Cuboidi from, int z, ref EnumPushDirection direction)
    {
        direction = EnumPushDirection.None;
        if (from.X2 > X1 && from.X1 < X2 && from.Y2 > Y1 && from.Y1 < Y2)
        {
            if ((double)z > 0.0 && from.Z2 <= Z1 && Z1 - from.Z2 < z)
            {
                direction = EnumPushDirection.Positive;
                z = Z1 - from.Z2;
            }
            else if ((double)z < 0.0 && from.Z1 >= Z2 && Z2 - from.Z1 > z)
            {
                direction = EnumPushDirection.Negative;
                z = Z2 - from.Z1;
            }
        }

        return z;
    }

    //
    // Summary:
    //     Performs a 3-dimensional rotation on the cuboid and returns a new axis-aligned
    //     cuboid resulting from this rotation. Not sure it it makes any sense to use this
    //     for other rotations than 90 degree intervals.
    public Cuboidi RotatedCopy(int degX, int degY, int degZ, Vec3d origin)
    {
        double rad = (float)degX * (MathF.PI / 180f);
        double rad2 = (float)degY * (MathF.PI / 180f);
        double rad3 = (float)degZ * (MathF.PI / 180f);
        double[] array = Mat4d.Create();
        Mat4d.RotateX(array, array, rad);
        Mat4d.RotateY(array, array, rad2);
        Mat4d.RotateZ(array, array, rad3);
        double[] vec = new double[4]
        {
            (double)X1 - origin.X,
            (double)Y1 - origin.Y,
            (double)Z1 - origin.Z,
            1.0
        };
        double[] vec2 = new double[4]
        {
            (double)X2 - origin.X,
            (double)Y2 - origin.Y,
            (double)Z2 - origin.Z,
            1.0
        };
        vec = Mat4d.MulWithVec4(array, vec);
        vec2 = Mat4d.MulWithVec4(array, vec2);
        if (vec2[0] < vec[0])
        {
            double num = vec2[0];
            vec2[0] = vec[0];
            vec[0] = num;
        }

        if (vec2[1] < vec[1])
        {
            double num = vec2[1];
            vec2[1] = vec[1];
            vec[1] = num;
        }

        if (vec2[2] < vec[2])
        {
            double num = vec2[2];
            vec2[2] = vec[2];
            vec[2] = num;
        }

        return new Cuboidi((int)(Math.Round(vec[0]) + origin.X), (int)(Math.Round(vec[1]) + origin.Y), (int)(Math.Round(vec[2]) + origin.Z), (int)(Math.Round(vec2[0]) + origin.X), (int)(Math.Round(vec2[1]) + origin.Y), (int)Math.Round(vec2[2] + origin.Z));
    }

    //
    // Summary:
    //     Performs a 3-dimensional rotation on the cuboid and returns a new axis-aligned
    //     cuboid resulting from this rotation. Not sure it it makes any sense to use this
    //     for other rotations than 90 degree intervals.
    public Cuboidi RotatedCopy(IVec3 vec, Vec3d origin)
    {
        return RotatedCopy(vec.XAsInt, vec.YAsInt, vec.ZAsInt, origin);
    }

    //
    // Summary:
    //     Returns a new cuboid offseted by given position
    public Cuboidi OffsetCopy(int x, int y, int z)
    {
        return new Cuboidi(X1 + x, Y1 + y, Z1 + z, X2 + x, Y2 + y, Z2 + z);
    }

    //
    // Summary:
    //     Returns a new cuboid offseted by given position
    public Cuboidi OffsetCopy(IVec3 vec)
    {
        return OffsetCopy(vec.XAsInt, vec.YAsInt, vec.ZAsInt);
    }

    //
    // Summary:
    //     If the given cuboid intersects with this cubiod
    public bool Intersects(Cuboidi with)
    {
        if (with.MaxX <= MinX || with.MinX >= MaxX)
        {
            return false;
        }

        if (with.MaxY <= MinY || with.MinY >= MaxY)
        {
            return false;
        }

        if (with.MaxZ > MinZ)
        {
            return with.MinZ < MaxZ;
        }

        return false;
    }

    //
    // Summary:
    //     Ignores the y-axis
    //
    // Parameters:
    //   with:
    public bool Intersects(HorRectanglei with)
    {
        if (with.MaxX <= MinX || with.MinX >= MaxX)
        {
            return false;
        }

        if (with.MaxZ > MinZ)
        {
            return with.MinZ < MaxZ;
        }

        return false;
    }

    //
    // Summary:
    //     If the given cuboid intersects with or is adjacent to this cubiod
    public bool IntersectsOrTouches(Cuboidi with)
    {
        if (with.MaxX < MinX || with.MinX > MaxX)
        {
            return false;
        }

        if (with.MaxY < MinY || with.MinY > MaxY)
        {
            return false;
        }

        if (with.MaxZ >= MinZ)
        {
            return with.MinZ <= MaxZ;
        }

        return false;
    }

    //
    // Summary:
    //     Creates a copy of the cuboid
    public Cuboidi Clone()
    {
        return new Cuboidi(Start, End);
    }

    public bool Equals(Cuboidi other)
    {
        if (other.X1 == X1 && other.Y1 == Y1 && other.Z1 == Z1 && other.X2 == X2 && other.Y2 == Y2)
        {
            return other.Z2 == Z2;
        }

        return false;
    }

    public override bool Equals(object obj)
    {
        if (obj is Cuboidi other)
        {
            return Equals(other);
        }

        return false;
    }

    //
    // Summary:
    //     Returns true if supplied cuboid is directly adjacent to this one
    //
    // Parameters:
    //   cuboidi:
    internal bool IsAdjacent(Cuboidi cuboidi)
    {
        bool num = Intersects(cuboidi);
        bool flag = IntersectsOrTouches(cuboidi);
        return !num && flag;
    }

    public override string ToString()
    {
        return $"X1={X1},Y1={Y1},Z1={Z1},X2={X2},Y2={Y2},Z2={Z2}";
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (((((927660019 * -1521134295 + X1.GetHashCode()) * -1521134295 + Y1.GetHashCode()) * -1521134295 + Z1.GetHashCode()) * -1521134295 + X2.GetHashCode()) * -1521134295 + Y2.GetHashCode()) * -1521134295 + Z2.GetHashCode();
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
