#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.MathTools;

//
// Summary:
//     Represents a three dimensional axis-aligned cuboid using two 3d coordinates.
//     Used for collision and selection withes.
public class Cuboidd : ICuboid<double, Cuboidd>, IEquatable<Cuboidd>
{
    private const double epsilon = 1.6E-05;

    public double X1;

    public double Y1;

    public double Z1;

    public double X2;

    public double Y2;

    public double Z2;

    //
    // Summary:
    //     MaxX-MinX
    public double Width => MaxX - MinX;

    //
    // Summary:
    //     MaxY-MinY
    public double Height => MaxY - MinY;

    //
    // Summary:
    //     MaxZ-MinZ
    public double Length => MaxZ - MinZ;

    public double MinX => Math.Min(X1, X2);

    public double MinY => Math.Min(Y1, Y2);

    public double MinZ => Math.Min(Z1, Z2);

    public double MaxX => Math.Max(X1, X2);

    public double MaxY => Math.Max(Y1, Y2);

    public double MaxZ => Math.Max(Z1, Z2);

    public Vec3d Start => new Vec3d(MinX, MinY, MinZ);

    public Vec3d End => new Vec3d(MaxX, MaxY, MaxZ);

    public Cuboidd()
    {
    }

    public Cuboidd(double x1, double y1, double z1, double x2, double y2, double z2)
    {
        Set(x1, y1, z1, x2, y2, z2);
    }

    public Cuboidd(Vec3d start, Vec3d end)
    {
        X1 = start.X;
        Y1 = start.Y;
        Z1 = start.Z;
        X2 = end.X;
        Y2 = end.Y;
        Z2 = end.Z;
    }

    //
    // Summary:
    //     Sets the minimum and maximum values of the cuboid
    public Cuboidd Set(double x1, double y1, double z1, double x2, double y2, double z2)
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
    public Cuboidd Set(IVec3 min, IVec3 max)
    {
        Set(min.XAsDouble, min.YAsDouble, min.ZAsDouble, max.XAsDouble, max.YAsDouble, max.ZAsDouble);
        return this;
    }

    //
    // Summary:
    //     Sets the minimum and maximum values of the cuboid
    public Cuboidd Set(Cuboidf selectionBox)
    {
        X1 = selectionBox.X1;
        Y1 = selectionBox.Y1;
        Z1 = selectionBox.Z1;
        X2 = selectionBox.X2;
        Y2 = selectionBox.Y2;
        Z2 = selectionBox.Z2;
        return this;
    }

    public void Set(Cuboidd other)
    {
        X1 = other.X1;
        Y1 = other.Y1;
        Z1 = other.Z1;
        X2 = other.X2;
        Y2 = other.Y2;
        Z2 = other.Z2;
    }

    //
    // Summary:
    //     Sets the cuboid to the selectionBox, translated by vec
    public Cuboidd SetAndTranslate(Cuboidf selectionBox, Vec3d vec)
    {
        X1 = (double)selectionBox.X1 + vec.X;
        Y1 = (double)selectionBox.Y1 + vec.Y;
        Z1 = (double)selectionBox.Z1 + vec.Z;
        X2 = (double)selectionBox.X2 + vec.X;
        Y2 = (double)selectionBox.Y2 + vec.Y;
        Z2 = (double)selectionBox.Z2 + vec.Z;
        return this;
    }

    //
    // Summary:
    //     Sets the cuboid to the selectionBox, translated by (dX, dY, dZ)
    public Cuboidd SetAndTranslate(Cuboidf selectionBox, double dX, double dY, double dZ)
    {
        X1 = (double)selectionBox.X1 + dX;
        Y1 = (double)selectionBox.Y1 + dY;
        Z1 = (double)selectionBox.Z1 + dZ;
        X2 = (double)selectionBox.X2 + dX;
        Y2 = (double)selectionBox.Y2 + dY;
        Z2 = (double)selectionBox.Z2 + dZ;
        return this;
    }

    public void RemoveRoundingErrors()
    {
        double num = X1 * 16.0;
        double num2 = Z1 * 16.0;
        double num3 = X2 * 16.0;
        double num4 = Z2 * 16.0;
        if (Math.Ceiling(num) - num < 1.6E-05)
        {
            X1 = Math.Ceiling(num) / 16.0;
        }

        if (Math.Ceiling(num2) - num2 < 1.6E-05)
        {
            Z1 = Math.Ceiling(num2) / 16.0;
        }

        if (num3 - Math.Floor(num3) < 1.6E-05)
        {
            X2 = Math.Floor(num3) / 16.0;
        }

        if (num4 - Math.Floor(num4) < 1.6E-05)
        {
            Z2 = Math.Floor(num4) / 16.0;
        }
    }

    //
    // Summary:
    //     Adds the given offset to the cuboid
    public Cuboidd Translate(IVec3 vec)
    {
        Translate(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble);
        return this;
    }

    //
    // Summary:
    //     Adds the given offset to the cuboid
    public Cuboidd Translate(double posX, double posY, double posZ)
    {
        X1 += posX;
        Y1 += posY;
        Z1 += posZ;
        X2 += posX;
        Y2 += posY;
        Z2 += posZ;
        return this;
    }

    public Cuboidd GrowBy(double dx, double dy, double dz)
    {
        X1 -= dx;
        Y1 -= dy;
        Z1 -= dz;
        X2 += dx;
        Y2 += dy;
        Z2 += dz;
        return this;
    }

    //
    // Summary:
    //     Returns if the given point is inside the cuboid
    public bool ContainsOrTouches(double x, double y, double z)
    {
        if (x >= X1 && x <= X2 && y >= Y1 && y <= Y2 && z >= Z1)
        {
            return z <= Z2;
        }

        return false;
    }

    //
    // Summary:
    //     Returns if the given point is inside the cuboid
    public bool Contains(double x, double y, double z)
    {
        if (x > X1 && x < X2 && y > Y1 && y < Y2 && z > Z1)
        {
            return z < Z2;
        }

        return false;
    }

    //
    // Summary:
    //     Returns if the given point is inside the cuboid
    public bool ContainsOrTouches(IVec3 vec)
    {
        return ContainsOrTouches(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble);
    }

    //
    // Summary:
    //     Grows the cuboid so that it includes the given block
    public Cuboidd GrowToInclude(int x, int y, int z)
    {
        X1 = Math.Min(X1, x);
        Y1 = Math.Min(Y1, y);
        Z1 = Math.Min(Z1, z);
        X2 = Math.Max(X2, x + 1);
        Y2 = Math.Max(Y2, y + 1);
        Z2 = Math.Max(Z2, z + 1);
        return this;
    }

    //
    // Summary:
    //     Grows the cuboid so that it includes the given block
    public Cuboidd GrowToInclude(IVec3 vec)
    {
        GrowToInclude(vec.XAsInt, vec.YAsInt, vec.ZAsInt);
        return this;
    }

    //
    // Summary:
    //     Returns the shortest distance between given point and any point inside the cuboid
    public double ShortestDistanceFrom(double x, double y, double z)
    {
        double num = x - GameMath.Clamp(x, X1, X2);
        double num2 = y - GameMath.Clamp(y, Y1, Y2);
        double num3 = z - GameMath.Clamp(z, Z1, Z2);
        return Math.Sqrt(num * num + num2 * num2 + num3 * num3);
    }

    public Cuboidi ToCuboidi()
    {
        return new Cuboidi((int)X1, (int)Y1, (int)Z1, (int)X2, (int)Y2, (int)Z2);
    }

    //
    // Summary:
    //     Returns the shortest distance between given point and any point inside the cuboid
    public double ShortestVerticalDistanceFrom(double y)
    {
        return y - GameMath.Clamp(y, Y1, Y2);
    }

    //
    // Summary:
    //     Returns the shortest vertical distance to any point between this and given cuboid
    public double ShortestVerticalDistanceFrom(Cuboidd cuboid)
    {
        double val = cuboid.Y1 - GameMath.Clamp(cuboid.Y1, Y1, Y2);
        double val2 = cuboid.Y2 - GameMath.Clamp(cuboid.Y2, Y1, Y2);
        return Math.Min(val, val2);
    }

    //
    // Summary:
    //     Returns the shortest distance to any point between this and given cuboid
    public double ShortestVerticalDistanceFrom(Cuboidf cuboid, EntityPos offset)
    {
        double num = offset.Y + (double)cuboid.Y1;
        double num2 = offset.Y + (double)cuboid.Y2;
        double val = num - GameMath.Clamp(num, Y1, Y2);
        if (num <= Y1 && num2 >= Y2)
        {
            val = 0.0;
        }

        double val2 = num2 - GameMath.Clamp(num2, Y1, Y2);
        return Math.Min(val, val2);
    }

    //
    // Summary:
    //     Returns the shortest distance to any point between this and given cuboid
    public double ShortestDistanceFrom(Cuboidd cuboid)
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
    //     Returns the shortest distance to any point between this and given cuboid
    public double ShortestDistanceFrom(Cuboidf cuboid, BlockPos offset)
    {
        double num = (float)offset.X + cuboid.X1;
        double num2 = (float)offset.Y + cuboid.Y1;
        double num3 = (float)offset.Z + cuboid.Z1;
        double num4 = (float)offset.X + cuboid.X2;
        double num5 = (float)offset.Y + cuboid.Y2;
        double num6 = (float)offset.Z + cuboid.Z2;
        double num7 = num - GameMath.Clamp(num, X1, X2);
        double num8 = num2 - GameMath.Clamp(num2, Y1, Y2);
        double num9 = num3 - GameMath.Clamp(num3, Z1, Z2);
        if (num <= X1 && num4 >= X2)
        {
            num7 = 0.0;
        }

        if (num2 <= Y1 && num5 >= Y2)
        {
            num8 = 0.0;
        }

        if (num3 <= Z1 && num6 >= Z2)
        {
            num9 = 0.0;
        }

        double num10 = num4 - GameMath.Clamp(num4, X1, X2);
        double num11 = num5 - GameMath.Clamp(num5, Y1, Y2);
        double num12 = num6 - GameMath.Clamp(num6, Z1, Z2);
        return Math.Sqrt(Math.Min(num7 * num7, num10 * num10) + Math.Min(num8 * num8, num11 * num11) + Math.Min(num9 * num9, num12 * num12));
    }

    //
    // Summary:
    //     Returns the shortest horizontal distance to any point between this and given
    //     cuboid
    public double ShortestHorizontalDistanceFrom(Cuboidf cuboid, BlockPos offset)
    {
        double num = (double)((float)offset.X + cuboid.X1) - GameMath.Clamp((float)offset.X + cuboid.X1, X1, X2);
        double num2 = (double)((float)offset.Z + cuboid.Z1) - GameMath.Clamp((float)offset.Z + cuboid.Z1, Z1, Z2);
        double num3 = (double)((float)offset.X + cuboid.X2) - GameMath.Clamp((float)offset.X + cuboid.X2, X1, X2);
        double num4 = (double)((float)offset.Z + cuboid.Z2) - GameMath.Clamp((float)offset.Z + cuboid.Z2, Z1, Z2);
        return Math.Sqrt(Math.Min(num * num, num3 * num3) + Math.Min(num2 * num2, num4 * num4));
    }

    //
    // Summary:
    //     Returns the shortest horizontal distance to any point between this and given
    //     coordinate
    //
    // Parameters:
    //   x:
    //
    //   z:
    public double ShortestHorizontalDistanceFrom(double x, double z)
    {
        double num = x - GameMath.Clamp(x, X1, X2);
        double num2 = z - GameMath.Clamp(z, Z1, Z2);
        return Math.Sqrt(num * num + num2 * num2);
    }

    //
    // Summary:
    //     Returns the shortest distance between given point and any point inside the cuboid
    public double ShortestDistanceFrom(IVec3 vec)
    {
        return ShortestDistanceFrom(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble);
    }

    //
    // Summary:
    //     Returns a new x coordinate that's ensured to be outside this cuboid. Used for
    //     collision detection.
    public double pushOutX(Cuboidd from, double motx, ref EnumPushDirection direction)
    {
        direction = EnumPushDirection.None;
        if (from.Z2 > Z1 && from.Z1 < Z2 && from.Y2 > Y1 && from.Y1 < Y2)
        {
            if (motx > 0.0 && from.X2 <= X1 && X1 - from.X2 < motx)
            {
                direction = EnumPushDirection.Positive;
                motx = X1 - from.X2;
            }
            else if (motx < 0.0 && from.X1 >= X2 && X2 - from.X1 > motx)
            {
                direction = EnumPushDirection.Negative;
                motx = X2 - from.X1;
            }
        }

        return motx;
    }

    //
    // Summary:
    //     Returns a new y coordinate that's ensured to be outside this cuboid. Used for
    //     collision detection.
    public double pushOutY(Cuboidd from, double moty, ref EnumPushDirection direction)
    {
        direction = EnumPushDirection.None;
        if (from.X2 > X1 && from.X1 < X2 && from.Z2 > Z1 && from.Z1 < Z2)
        {
            if (moty > 0.0 && from.Y2 <= Y1 && Y1 - from.Y2 < moty)
            {
                direction = EnumPushDirection.Positive;
                moty = Y1 - from.Y2;
            }
            else if (moty < 0.0 && from.Y1 >= Y2 && Y2 - from.Y1 > moty)
            {
                direction = EnumPushDirection.Negative;
                moty = Y2 - from.Y1;
            }
        }

        return moty;
    }

    //
    // Summary:
    //     Returns a new z coordinate that's ensured to be outside this cuboid. Used for
    //     collision detection.
    public double pushOutZ(Cuboidd from, double motz, ref EnumPushDirection direction)
    {
        direction = EnumPushDirection.None;
        if (from.X2 > X1 && from.X1 < X2 && from.Y2 > Y1 && from.Y1 < Y2)
        {
            if (motz > 0.0 && from.Z2 <= Z1 && Z1 - from.Z2 < motz)
            {
                direction = EnumPushDirection.Positive;
                motz = Z1 - from.Z2;
            }
            else if (motz < 0.0 && from.Z1 >= Z2 && Z2 - from.Z1 > motz)
            {
                direction = EnumPushDirection.Negative;
                motz = Z2 - from.Z1;
            }
        }

        return motz;
    }

    //
    // Summary:
    //     Performs a 3-dimensional rotation on the cuboid and returns a new axis-aligned
    //     cuboid resulting from this rotation. Not sure it it makes any sense to use this
    //     for other rotations than 90 degree intervals.
    public Cuboidd RotatedCopy(double degX, double degY, double degZ, Vec3d origin)
    {
        double rad = degX * 0.01745329238474369;
        double rad2 = degY * 0.01745329238474369;
        double rad3 = degZ * 0.01745329238474369;
        double[] array = Mat4d.Create();
        Mat4d.RotateX(array, array, rad);
        Mat4d.RotateY(array, array, rad2);
        Mat4d.RotateZ(array, array, rad3);
        (new double[4])[3] = 1.0;
        double[] vec = new double[4]
        {
            X1 - origin.X,
            Y1 - origin.Y,
            Z1 - origin.Z,
            1.0
        };
        double[] vec2 = new double[4]
        {
            X2 - origin.X,
            Y2 - origin.Y,
            Z2 - origin.Z,
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

        return new Cuboidd(vec[0] + origin.X, vec[1] + origin.Y, vec[2] + origin.Z, vec2[0] + origin.X, vec2[1] + origin.Y, vec2[2] + origin.Z);
    }

    //
    // Summary:
    //     Performs a 3-dimensional rotation on the cuboid and returns a new axis-aligned
    //     cuboid resulting from this rotation. Not sure it makes any sense to use this
    //     for other rotations than 90 degree intervals.
    public Cuboidd RotatedCopy(IVec3 vec, Vec3d origin)
    {
        return RotatedCopy(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble, origin);
    }

    public Cuboidd Offset(double dx, double dy, double dz)
    {
        X1 += dx;
        Y1 += dy;
        Z1 += dz;
        X2 += dx;
        Y2 += dy;
        Z2 += dz;
        return this;
    }

    //
    // Summary:
    //     Returns a new cuboid offseted by given position
    public Cuboidd OffsetCopy(double x, double y, double z)
    {
        return new Cuboidd(X1 + x, Y1 + y, Z1 + z, X2 + x, Y2 + y, Z2 + z);
    }

    //
    // Summary:
    //     Returns a new cuboid offseted by given position
    public Cuboidd OffsetCopy(IVec3 vec)
    {
        return OffsetCopy(vec.XAsDouble, vec.YAsDouble, vec.ZAsDouble);
    }

    //
    // Summary:
    //     If the given cuboid intersects with this cuboid
    public bool Intersects(Cuboidd other)
    {
        if (X2 > other.X1 && X1 < other.X2 && Y2 > other.Y1 && Y1 < other.Y2 && Z2 > other.Z1 && Z1 < other.Z2)
        {
            return true;
        }

        return false;
    }

    //
    // Summary:
    //     If the given cuboid intersects with this cuboid
    public bool Intersects(Cuboidf other)
    {
        if (X2 > (double)other.X1 && X1 < (double)other.X2 && Y2 > (double)other.Y1 && Y1 < (double)other.Y2 && Z2 > (double)other.Z1 && Z1 < (double)other.Z2)
        {
            return true;
        }

        return false;
    }

    //
    // Summary:
    //     If the given cuboid intersects with this cuboid
    public bool Intersects(Cuboidf other, Vec3d offset)
    {
        if (X2 > (double)other.X1 + offset.X && X1 < (double)other.X2 + offset.X && Z2 > (double)other.Z1 + offset.Z && Z1 < (double)other.Z2 + offset.Z && Y2 > (double)other.Y1 + offset.Y && Y1 < Math.Round((double)other.Y2 + offset.Y, 5))
        {
            return true;
        }

        return false;
    }

    public bool Intersects(Cuboidf other, double offsetx, double offsety, double offsetz)
    {
        if (X2 > (double)other.X1 + offsetx && X1 < (double)other.X2 + offsetx && Z2 > (double)other.Z1 + offsetz && Z1 < (double)other.Z2 + offsetz && Y2 > (double)other.Y1 + offsety && Y1 < Math.Round((double)other.Y2 + offsety, 5))
        {
            return true;
        }

        return false;
    }

    //
    // Summary:
    //     If the given cuboid intersects with this cuboid
    public bool IntersectsOrTouches(Cuboidd other)
    {
        if (X2 >= other.X1 && X1 <= other.X2 && Y2 >= other.Y1 && Y1 <= other.Y2 && Z2 >= other.Z1 && Z1 <= other.Z2)
        {
            return true;
        }

        return false;
    }

    //
    // Summary:
    //     If the given cuboid intersects with this cuboid
    public bool IntersectsOrTouches(Cuboidf other, Vec3d offset)
    {
        if (X2 >= (double)other.X1 + offset.X && X1 <= (double)other.X2 + offset.X && Z2 >= (double)other.Z1 + offset.Z && Z1 <= (double)other.Z2 + offset.Z && Y2 >= (double)other.Y1 + offset.Y && Y1 <= Math.Round((double)other.Y2 + offset.Y, 5))
        {
            return true;
        }

        return false;
    }

    //
    // Summary:
    //     If the given cuboid intersects with this cuboid
    public bool IntersectsOrTouches(Cuboidf other, double offsetX, double offsetY, double offsetZ)
    {
        return !(X2 < (double)other.X1 + offsetX) && !(X1 > (double)other.X2 + offsetX) && !(Y2 < (double)other.Y1 + offsetY) && !(Y1 > (double)other.Y2 + offsetY) && !(Z2 < (double)other.Z1 + offsetZ) && !(Z1 > (double)other.Z2 + offsetZ);
    }

    public Cuboidf ToFloat()
    {
        return new Cuboidf((float)X1, (float)Y1, (float)Z1, (float)X2, (float)Y2, (float)Z2);
    }

    //
    // Summary:
    //     Creates a copy of the cuboid
    public Cuboidd Clone()
    {
        return (Cuboidd)MemberwiseClone();
    }

    public bool Equals(Cuboidd other)
    {
        if (other.X1 == X1 && other.Y1 == Y1 && other.Z1 == Z1 && other.X2 == X2 && other.Y2 == Y2)
        {
            return other.Z2 == Z2;
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
