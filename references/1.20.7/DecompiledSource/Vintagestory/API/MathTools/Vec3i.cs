#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ProtoBuf;

namespace Vintagestory.API.MathTools;

//
// Summary:
//     Represents a vector of 3 ints. Go bug Tyron if you need more utility methods
//     in this class.
[ProtoContract]
[JsonObject(/*Could not decode attribute arguments.*/)]
public class Vec3i : IEquatable<Vec3i>, IVec3
{
    [ProtoMember(1)]
    [JsonProperty]
    public int X;

    [ProtoMember(2)]
    [JsonProperty]
    public int Y;

    [ProtoMember(3)]
    [JsonProperty]
    public int Z;

    //
    // Summary:
    //     List of offset of all direct and indirect neighbours of coordinate 0/0/0
    public static readonly Vec3i[] DirectAndIndirectNeighbours;

    public bool IsZero
    {
        get
        {
            if (X == 0 && Y == 0)
            {
                return Z == 0;
            }

            return false;
        }
    }

    //
    // Summary:
    //     Returns the n-th coordinate
    //
    // Parameters:
    //   index:
    public int this[int index]
    {
        get
        {
            return index switch
            {
                1 => Y,
                0 => X,
                _ => Z,
            };
        }
        set
        {
            switch (index)
            {
                case 0:
                    X = value;
                    break;
                case 1:
                    Y = value;
                    break;
                default:
                    Z = value;
                    break;
            }
        }
    }

    public BlockPos AsBlockPos => new BlockPos(X, Y, Z);

    int IVec3.XAsInt => X;

    int IVec3.YAsInt => Y;

    int IVec3.ZAsInt => Z;

    double IVec3.XAsDouble => X;

    double IVec3.YAsDouble => Y;

    double IVec3.ZAsDouble => Z;

    float IVec3.XAsFloat => X;

    float IVec3.YAsFloat => Y;

    float IVec3.ZAsFloat => Z;

    public static Vec3i Zero => new Vec3i(0, 0, 0);

    public Vec2i XZ => new Vec2i(X, Z);

    public Vec3i AsVec3i => new Vec3i(X, Y, Z);

    static Vec3i()
    {
        List<Vec3i> list = new List<Vec3i>();
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                for (int k = -1; k <= 1; k++)
                {
                    if (i != 0 || j != 0 || k != 0)
                    {
                        list.Add(new Vec3i(i, j, k));
                    }
                }
            }
        }

        DirectAndIndirectNeighbours = list.ToArray();
    }

    public Vec3i()
    {
    }

    public Vec3i(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public Vec3i(BlockPos pos)
    {
        X = pos.X;
        Y = pos.Y;
        Z = pos.Z;
    }

    public Vec3i Add(int x, int y, int z)
    {
        X += x;
        Y += y;
        Z += z;
        return this;
    }

    public Vec3i AddCopy(int x, int y, int z)
    {
        return new Vec3i(X + x, Y + y, Z + z);
    }

    public Vec3i Add(int x, int y, int z, Vec3i intoVec)
    {
        intoVec.X = X + x;
        intoVec.Y = Y + y;
        intoVec.Z = Z + z;
        return this;
    }

    public Vec3i Add(BlockFacing towardsFace, int length = 1)
    {
        X += towardsFace.Normali.X * length;
        Y += towardsFace.Normali.Y * length;
        Z += towardsFace.Normali.Z * length;
        return this;
    }

    public int ManhattenDistanceTo(Vec3i vec)
    {
        return Math.Abs(X - vec.X) + Math.Abs(Y - vec.Y) + Math.Abs(Z - vec.Z);
    }

    public long SquareDistanceTo(Vec3i vec)
    {
        long num = X - vec.X;
        long num2 = Y - vec.Y;
        long num3 = Z - vec.Z;
        return num * num + num2 * num2 + num3 * num3;
    }

    public long SquareDistanceTo(int x, int y, int z)
    {
        long num = X - x;
        long num2 = Y - y;
        long num3 = Z - z;
        return num * num + num2 * num2 + num3 * num3;
    }

    public double DistanceTo(Vec3i vec)
    {
        long num = X - vec.X;
        long num2 = Y - vec.Y;
        long num3 = Z - vec.Z;
        return Math.Sqrt(num * num + num2 * num2 + num3 * num3);
    }

    //
    // Summary:
    //     Substracts val from each coordinate if the coordinate if positive, otherwise
    //     it is added. If 0, the value is unchanged. The value must be a positive number
    //
    //
    // Parameters:
    //   val:
    public void Reduce(int val = 1)
    {
        X = ((X > 0) ? Math.Max(0, X - val) : Math.Min(0, X + val));
        Y = ((Y > 0) ? Math.Max(0, Y - val) : Math.Min(0, Y + val));
        Z = ((Z > 0) ? Math.Max(0, Z - val) : Math.Min(0, Z + val));
    }

    public void ReduceX(int val = 1)
    {
        X = ((X > 0) ? Math.Max(0, X - val) : Math.Min(0, X + val));
    }

    public void ReduceY(int val = 1)
    {
        Y = ((Y > 0) ? Math.Max(0, Y - val) : Math.Min(0, Y + val));
    }

    public void ReduceZ(int val = 1)
    {
        Z = ((Z > 0) ? Math.Max(0, Z - val) : Math.Min(0, Z + val));
    }

    public Vec3i Set(int positionX, int positionY, int positionZ)
    {
        X = positionX;
        Y = positionY;
        Z = positionZ;
        return this;
    }

    public Vec3i Set(Vec3i fromPos)
    {
        X = fromPos.X;
        Y = fromPos.Y;
        Z = fromPos.Z;
        return this;
    }

    internal void Offset(BlockFacing face)
    {
        X += face.Normali.X;
        Y += face.Normali.Y;
        Z += face.Normali.Z;
    }

    public Vec3i Clone()
    {
        return (Vec3i)MemberwiseClone();
    }

    public override bool Equals(object obj)
    {
        if (obj is Vec3i)
        {
            Vec3i vec3i = (Vec3i)obj;
            if (X == vec3i.X && Y == vec3i.Y)
            {
                return Z == vec3i.Z;
            }

            return false;
        }

        return false;
    }

    public bool Equals(Vec3i other)
    {
        if (other != null && X == other.X && Y == other.Y)
        {
            return Z == other.Z;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return ((391 + X) * 23 + Y) * 23 + Z;
    }

    public override string ToString()
    {
        return "X=" + X + ",Y=" + Y + ",Z=" + Z;
    }

    internal float[] ToFloats()
    {
        return new float[3] { X, Y, Z };
    }

    public Vec3i AddCopy(BlockFacing facing)
    {
        return new Vec3i(X + facing.Normali.X, Y + facing.Normali.Y, Z + facing.Normali.Z);
    }

    public BlockPos ToBlockPos()
    {
        return new BlockPos
        {
            X = X,
            Y = Y,
            Z = Z
        };
    }

    public bool Equals(int x, int y, int z)
    {
        if (X == x && Y == y)
        {
            return Z == z;
        }

        return false;
    }

    public static Vec3i operator *(Vec3i left, int right)
    {
        return new Vec3i(left.X * right, left.Y * right, left.Z * right);
    }

    public static Vec3i operator *(int left, Vec3i right)
    {
        return new Vec3i(left * right.X, left * right.Y, left * right.Z);
    }

    public static Vec3i operator /(Vec3i left, int right)
    {
        return new Vec3i(left.X / right, left.Y / right, left.Z / right);
    }

    public static Vec3i operator +(Vec3i left, Vec3i right)
    {
        return new Vec3i(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    public static Vec3i operator -(Vec3i left, Vec3i right)
    {
        return new Vec3i(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    public static Vec3i operator -(Vec3i vec)
    {
        return new Vec3i(-vec.X, -vec.Y, -vec.Z);
    }

    public static bool operator ==(Vec3i left, Vec3i right)
    {
        return left?.Equals(right) ?? ((object)right == null);
    }

    public static bool operator !=(Vec3i left, Vec3i right)
    {
        return !(left == right);
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
