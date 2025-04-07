#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.IO;
using Newtonsoft.Json;
using ProtoBuf;
using Vintagestory.API.Client;

namespace Vintagestory.API.MathTools;

//
// Summary:
//     Represents a vector of 3 floats. Go bug Tyron of you need more utility methods
//     in this class.
[JsonObject(/*Could not decode attribute arguments.*/)]
[ProtoContract]
public class Vec3f : IVec3, IEquatable<Vec3f>
{
    //
    // Summary:
    //     The X-Component of the vector
    [JsonProperty]
    [ProtoMember(1)]
    public float X;

    //
    // Summary:
    //     The Y-Component of the vector
    [JsonProperty]
    [ProtoMember(2)]
    public float Y;

    //
    // Summary:
    //     The Z-Component of the vector
    [JsonProperty]
    [ProtoMember(3)]
    public float Z;

    //
    // Summary:
    //     Create a new instance with x/y/z set to 0
    public static Vec3f Zero => new Vec3f();

    public static Vec3f Half => new Vec3f(0.5f, 0.5f, 0.5f);

    public static Vec3f One => new Vec3f(1f, 1f, 1f);

    //
    // Summary:
    //     Synonum for X
    public float R
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
    public float G
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
    public float B
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

    public bool IsZero
    {
        get
        {
            if (X == 0f && Y == 0f)
            {
                return Z == 0f;
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
    public float this[int index]
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

    int IVec3.XAsInt => (int)X;

    int IVec3.YAsInt => (int)Y;

    int IVec3.ZAsInt => (int)Z;

    double IVec3.XAsDouble => X;

    double IVec3.YAsDouble => Y;

    double IVec3.ZAsDouble => Z;

    float IVec3.XAsFloat => X;

    float IVec3.YAsFloat => Y;

    float IVec3.ZAsFloat => Z;

    public Vec3i AsVec3i => new Vec3i((int)X, (int)Y, (int)Z);

    //
    // Summary:
    //     Creates a new vector with x/y/z = 0
    public Vec3f()
    {
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
    public Vec3f(float x, float y, float z)
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
    //   vec:
    public Vec3f(Vec4f vec)
    {
        X = vec.X;
        Y = vec.Y;
        Z = vec.Z;
    }

    //
    // Summary:
    //     Create a new vector with given coordinates
    //
    // Parameters:
    //   values:
    public Vec3f(float[] values)
    {
        X = values[0];
        Y = values[1];
        Z = values[2];
    }

    public Vec3f(Vec3i vec3i)
    {
        X = vec3i.X;
        Y = vec3i.Y;
        Z = vec3i.Z;
    }

    //
    // Summary:
    //     Returns the length of this vector
    public float Length()
    {
        return GameMath.RootSumOfSquares(X, Y, Z);
    }

    public void Negate()
    {
        X = 0f - X;
        Y = 0f - Y;
        Z = 0f - Z;
    }

    public Vec3f RotatedCopy(float yaw)
    {
        Matrixf matrixf = new Matrixf();
        matrixf.RotateYDeg(yaw);
        return matrixf.TransformVector(new Vec4f(X, Y, Z, 0f)).XYZ;
    }

    //
    // Summary:
    //     Returns the dot product with given vector
    //
    // Parameters:
    //   a:
    public float Dot(Vec3f a)
    {
        return X * a.X + Y * a.Y + Z * a.Z;
    }

    public float Dot(FastVec3f a)
    {
        return X * a.X + Y * a.Y + Z * a.Z;
    }

    //
    // Summary:
    //     Returns the dot product with given vector
    //
    // Parameters:
    //   a:
    public float Dot(Vec3d a)
    {
        return (float)((double)X * a.X + (double)Y * a.Y + (double)Z * a.Z);
    }

    //
    // Summary:
    //     Returns the dot product with given vector
    //
    // Parameters:
    //   pos:
    public double Dot(float[] pos)
    {
        return X * pos[0] + Y * pos[1] + Z * pos[2];
    }

    //
    // Summary:
    //     Returns the dot product with given vector
    //
    // Parameters:
    //   pos:
    public double Dot(double[] pos)
    {
        return (float)((double)X * pos[0] + (double)Y * pos[1] + (double)Z * pos[2]);
    }

    public Vec3f Cross(Vec3f vec)
    {
        return new Vec3f
        {
            X = Y * vec.Z - Z * vec.Y,
            Y = Z * vec.X - X * vec.Z,
            Z = X * vec.Y - Y * vec.X
        };
    }

    public double[] ToDoubleArray()
    {
        return new double[3] { X, Y, Z };
    }

    //
    // Summary:
    //     Creates the cross product from a and b and sets own values accordingly
    //
    // Parameters:
    //   a:
    //
    //   b:
    public void Cross(Vec3f a, Vec3f b)
    {
        X = a.Y * b.Z - a.Z * b.Y;
        Y = a.Z * b.X - a.X * b.Z;
        Z = a.X * b.Y - a.Y * b.X;
    }

    //
    // Summary:
    //     Creates the cross product from a and b and sets own values accordingly
    //
    // Parameters:
    //   a:
    //
    //   b:
    public void Cross(Vec3f a, Vec4f b)
    {
        X = a.Y * b.Z - a.Z * b.Y;
        Y = a.Z * b.X - a.X * b.Z;
        Z = a.X * b.Y - a.Y * b.X;
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
    public Vec3f Add(float x, float y, float z)
    {
        X += x;
        Y += y;
        Z += z;
        return this;
    }

    //
    // Summary:
    //     Adds given x/y/z coordinates to the vector
    //
    // Parameters:
    //   vec:
    public Vec3f Add(Vec3f vec)
    {
        X += vec.X;
        Y += vec.Y;
        Z += vec.Z;
        return this;
    }

    //
    // Summary:
    //     Adds given x/y/z coordinates to the vector
    //
    // Parameters:
    //   vec:
    public Vec3f Add(Vec3d vec)
    {
        X += (float)vec.X;
        Y += (float)vec.Y;
        Z += (float)vec.Z;
        return this;
    }

    //
    // Summary:
    //     Substracts given x/y/z coordinates to the vector
    //
    // Parameters:
    //   vec:
    public Vec3f Sub(Vec3f vec)
    {
        X -= vec.X;
        Y -= vec.Y;
        Z -= vec.Z;
        return this;
    }

    //
    // Summary:
    //     Substracts given x/y/z coordinates to the vector
    //
    // Parameters:
    //   vec:
    public Vec3f Sub(Vec3d vec)
    {
        X -= (float)vec.X;
        Y -= (float)vec.Y;
        Z -= (float)vec.Z;
        return this;
    }

    public Vec3f Sub(Vec3i vec)
    {
        X -= vec.X;
        Y -= vec.Y;
        Z -= vec.Z;
        return this;
    }

    //
    // Summary:
    //     Multiplies each coordinate with given multiplier
    //
    // Parameters:
    //   multiplier:
    public Vec3f Mul(float multiplier)
    {
        X *= multiplier;
        Y *= multiplier;
        Z *= multiplier;
        return this;
    }

    //
    // Summary:
    //     Creates a copy of the vetor
    public Vec3f Clone()
    {
        return new Vec3f(X, Y, Z);
    }

    //
    // Summary:
    //     Turns the vector into a unit vector with length 1, but only if length is non-zero
    public Vec3f Normalize()
    {
        float num = Length();
        if (num > 0f)
        {
            X /= num;
            Y /= num;
            Z /= num;
        }

        return this;
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
    public float DistanceTo(Vec3d vec)
    {
        return (float)Math.Sqrt(((double)X - vec.X) * ((double)X - vec.X) + ((double)Y - vec.Y) * ((double)Y - vec.Y) + ((double)Z - vec.Z) * ((double)Z - vec.Z));
    }

    public float DistanceTo(Vec3f vec)
    {
        return (float)Math.Sqrt((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z));
    }

    //
    // Summary:
    //     Adds given coordinates to a new vectors and returns it. The original calling
    //     vector remains unchanged
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public Vec3f AddCopy(float x, float y, float z)
    {
        return new Vec3f(X + x, Y + y, Z + z);
    }

    //
    // Summary:
    //     Adds both vectors into a new vector. Both source vectors remain unchanged.
    //
    // Parameters:
    //   vec:
    public Vec3f AddCopy(Vec3f vec)
    {
        return new Vec3f(X + vec.X, Y + vec.Y, Z + vec.Z);
    }

    //
    // Summary:
    //     Substracts val from each coordinate if the coordinate if positive, otherwise
    //     it is added. If 0, the value is unchanged. The value must be a positive number
    //
    //
    // Parameters:
    //   val:
    public void ReduceBy(float val)
    {
        X = ((X > 0f) ? Math.Max(0f, X - val) : Math.Min(0f, X + val));
        Y = ((Y > 0f) ? Math.Max(0f, Y - val) : Math.Min(0f, Y + val));
        Z = ((Z > 0f) ? Math.Max(0f, Z - val) : Math.Min(0f, Z + val));
    }

    //
    // Summary:
    //     Creates a new vectors that is the normalized version of this vector.
    public Vec3f NormalizedCopy()
    {
        float num = Length();
        return new Vec3f(X / num, Y / num, Z / num);
    }

    //
    // Summary:
    //     Creates a new double precision vector with the same coordinates
    public Vec3d ToVec3d()
    {
        return new Vec3d(X, Y, Z);
    }

    public static Vec3f operator -(Vec3f left, Vec3f right)
    {
        return new Vec3f(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    public static Vec3f operator +(Vec3f left, Vec3f right)
    {
        return new Vec3f(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    public static Vec3f operator -(Vec3f left, float right)
    {
        return new Vec3f(left.X - right, left.Y - right, left.Z - right);
    }

    public static Vec3f operator -(float left, Vec3f right)
    {
        return new Vec3f(left - right.X, left - right.Y, left - right.Z);
    }

    public static Vec3f operator +(Vec3f left, float right)
    {
        return new Vec3f(left.X + right, left.Y + right, left.Z + right);
    }

    public static Vec3f operator *(Vec3f left, float right)
    {
        return new Vec3f(left.X * right, left.Y * right, left.Z * right);
    }

    public static Vec3f operator *(float left, Vec3f right)
    {
        return new Vec3f(left * right.X, left * right.Y, left * right.Z);
    }

    public static float operator *(Vec3f left, Vec3f right)
    {
        return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
    }

    public static Vec3f operator /(Vec3f left, float right)
    {
        return new Vec3f(left.X / right, left.Y / right, left.Z / right);
    }

    public static bool operator ==(Vec3f left, Vec3f right)
    {
        return left?.Equals(right) ?? ((object)right == null);
    }

    public static bool operator !=(Vec3f left, Vec3f right)
    {
        return !(left == right);
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
    public Vec3f Set(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
        return this;
    }

    //
    // Summary:
    //     Sets the vector to the coordinates of given vector
    //
    // Parameters:
    //   vec:
    public Vec3f Set(Vec3d vec)
    {
        X = (float)vec.X;
        Y = (float)vec.Y;
        Z = (float)vec.Z;
        return this;
    }

    public Vec3f Set(float[] vec)
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
    public Vec3f Set(Vec3f vec)
    {
        X = vec.X;
        Y = vec.Y;
        Z = vec.Z;
        return this;
    }

    //
    // Summary:
    //     Simple string represenation of the x/y/z components
    public override string ToString()
    {
        return "x=" + X + ", y=" + Y + ", z=" + Z;
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
    }

    public static Vec3f CreateFromBytes(BinaryReader reader)
    {
        return new Vec3f(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    public Vec4f ToVec4f(float w)
    {
        return new Vec4f(X, Y, Z, w);
    }

    public bool Equals(Vec3f other, double epsilon)
    {
        if ((double)Math.Abs(X - other.X) < epsilon && (double)Math.Abs(Y - other.Y) < epsilon)
        {
            return (double)Math.Abs(Z - other.Z) < epsilon;
        }

        return false;
    }

    public bool Equals(Vec3f other)
    {
        if (other != null && X == other.X && Y == other.Y)
        {
            return Z == other.Z;
        }

        return false;
    }

    public override bool Equals(object obj)
    {
        if (obj is Vec3f vec3f)
        {
            if (vec3f != null && X == vec3f.X && Y == vec3f.Y)
            {
                return Z == vec3f.Z;
            }

            return false;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return ((391 + X.GetHashCode()) * 23 + Y.GetHashCode()) * 23 + Z.GetHashCode();
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
