#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.IO;

namespace Vintagestory.API.MathTools;

//
// Summary:
//     Represents a vector of 3 ints, similar to a Vec3i or a BlockPos but this is a
//     struct
public struct FastVec3i
{
    //
    // Summary:
    //     The X-Component of the vector
    public int X;

    //
    // Summary:
    //     The Y-Component of the vector
    public int Y;

    //
    // Summary:
    //     The Z-Component of the vector
    public int Z;

    //
    // Summary:
    //     Synonum for X
    public int R
    {
        get
        {
            return X;
        }
        set
        {
            X = value;
        }
    }

    //
    // Summary:
    //     Synonum for Y
    public int G
    {
        get
        {
            return Y;
        }
        set
        {
            Y = value;
        }
    }

    //
    // Summary:
    //     Synonum for Z
    public int B
    {
        get
        {
            return Z;
        }
        set
        {
            Z = value;
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

    //
    // Summary:
    //     Create a new vector with given coordinates
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public FastVec3i(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    //
    // Summary:
    //     Create a new vector with given coordinates
    //
    // Parameters:
    //   pos:
    public FastVec3i(BlockPos pos)
    {
        X = pos.X;
        Y = pos.Y;
        Z = pos.Z;
    }

    //
    // Summary:
    //     Create a new vector with given coordinates
    //
    // Parameters:
    //   values:
    public FastVec3i(int[] values)
    {
        X = values[0];
        Y = values[1];
        Z = values[2];
    }

    public FastVec3i(Vec3i vec3i)
    {
        X = vec3i.X;
        Y = vec3i.Y;
        Z = vec3i.Z;
    }

    public bool Equals(Vec3i other)
    {
        if (other != null && X == other.X && Y == other.Y)
        {
            return Z == other.Z;
        }

        return false;
    }

    public bool Equals(BlockPos other)
    {
        if (other != null && X == other.X && Y == other.Y)
        {
            return Z == other.Z;
        }

        return false;
    }

    public bool Equals(FastVec3i other)
    {
        if (X == other.X && Y == other.Y)
        {
            return Z == other.Z;
        }

        return false;
    }

    //
    // Summary:
    //     Returns the length of this vector
    public float Length()
    {
        return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
    }

    public void Negate()
    {
        X = -X;
        Y = -Y;
        Z = -Z;
    }

    //
    // Summary:
    //     Adds given x/y/z coordinates to the vector
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public FastVec3i Add(int x, int y, int z)
    {
        X += x;
        Y += y;
        Z += z;
        return this;
    }

    //
    // Summary:
    //     Multiplies each coordinate with given multiplier
    //
    // Parameters:
    //   multiplier:
    public FastVec3i Mul(int multiplier)
    {
        X *= multiplier;
        Y *= multiplier;
        Z *= multiplier;
        return this;
    }

    //
    // Summary:
    //     Creates a copy of the vetor
    public FastVec3i Clone()
    {
        return (FastVec3i)MemberwiseClone();
    }

    //
    // Summary:
    //     Calculates the distance the two endpoints
    //
    // Parameters:
    //   vec:
    public float Distance(FastVec3i vec)
    {
        return (float)Math.Sqrt((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z));
    }

    //
    // Summary:
    //     Calculates the square distance the two endpoints
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public double DistanceSq(double x, double y, double z)
    {
        return ((double)X - x) * ((double)X - x) + ((double)Y - y) * ((double)Y - y) + ((double)Z - z) * ((double)Z - z);
    }

    //
    // Summary:
    //     Calculates the distance the two endpoints
    //
    // Parameters:
    //   vec:
    public float Distance(Vec3d vec)
    {
        return (float)Math.Sqrt(((double)X - vec.X) * ((double)X - vec.X) + ((double)Y - vec.Y) * ((double)Y - vec.Y) + ((double)Z - vec.Z) * ((double)Z - vec.Z));
    }

    //
    // Summary:
    //     Adds both vectors into a new vector. Both source vectors remain unchanged.
    //
    // Parameters:
    //   vec:
    public FastVec3i AddCopy(FastVec3i vec)
    {
        return new FastVec3i(X + vec.X, Y + vec.Y, Z + vec.Z);
    }

    //
    // Summary:
    //     Creates a new double precision vector with the same coordinates
    public Vec3d ToVec3d()
    {
        return new Vec3d(X, Y, Z);
    }

    //
    // Summary:
    //     Sets the vector to this coordinates
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public FastVec3i Set(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
        return this;
    }

    public FastVec3i Set(BlockPos pos)
    {
        X = pos.X;
        Y = pos.Y;
        Z = pos.Z;
        return this;
    }

    //
    // Summary:
    //     Sets the vector to the coordinates of given vector
    //
    // Parameters:
    //   vec:
    public FastVec3i Set(Vec3d vec)
    {
        X = (int)vec.X;
        Y = (int)vec.Y;
        Z = (int)vec.Z;
        return this;
    }

    public FastVec3i Set(int[] vec)
    {
        X = vec[0];
        Y = vec[1];
        Z = vec[2];
        return this;
    }

    //
    // Summary:
    //     Sets the vector to the coordinates of given vector
    //
    // Parameters:
    //   vec:
    public void Set(FastVec3i vec)
    {
        X = vec.X;
        Y = vec.Y;
        Z = vec.Z;
    }

    public override int GetHashCode()
    {
        return ((391 + X) * 23 + Y) * 23 + Z;
    }

    //
    // Summary:
    //     Simple string represenation of the x/y/z components
    public override string ToString()
    {
        return "x=" + X + ", y=" + Y + ", z=" + Z;
    }

    public void Write(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
    }

    public static FastVec3i CreateFromBytes(BinaryReader reader)
    {
        return new FastVec3i(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
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
