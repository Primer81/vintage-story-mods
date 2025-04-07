#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class Matrixf
{
    public float[] Values;

    public double[] ValuesAsDouble
    {
        get
        {
            double[] array = new double[16];
            for (int i = 0; i < 16; i++)
            {
                array[i] = Values[i];
            }

            return array;
        }
    }

    public Matrixf()
    {
        Values = Mat4f.Create();
    }

    public Matrixf(float[] values)
    {
        Values = Mat4f.Create();
        Set(values);
    }

    public static Matrixf Create()
    {
        return new Matrixf();
    }

    public Matrixf Identity()
    {
        Mat4f.Identity(Values);
        return this;
    }

    public Matrixf Set(float[] values)
    {
        Values[0] = values[0];
        Values[1] = values[1];
        Values[2] = values[2];
        Values[3] = values[3];
        Values[4] = values[4];
        Values[5] = values[5];
        Values[6] = values[6];
        Values[7] = values[7];
        Values[8] = values[8];
        Values[9] = values[9];
        Values[10] = values[10];
        Values[11] = values[11];
        Values[12] = values[12];
        Values[13] = values[13];
        Values[14] = values[14];
        Values[15] = values[15];
        return this;
    }

    public Matrixf Set(double[] values)
    {
        Values[0] = (float)values[0];
        Values[1] = (float)values[1];
        Values[2] = (float)values[2];
        Values[3] = (float)values[3];
        Values[4] = (float)values[4];
        Values[5] = (float)values[5];
        Values[6] = (float)values[6];
        Values[7] = (float)values[7];
        Values[8] = (float)values[8];
        Values[9] = (float)values[9];
        Values[10] = (float)values[10];
        Values[11] = (float)values[11];
        Values[12] = (float)values[12];
        Values[13] = (float)values[13];
        Values[14] = (float)values[14];
        Values[15] = (float)values[15];
        return this;
    }

    public Matrixf Translate(double x, double y, double z)
    {
        Mat4f.Translate(Values, Values, (float)x, (float)y, (float)z);
        return this;
    }

    public Matrixf Translate(Vec3f vec)
    {
        Mat4f.Translate(Values, Values, vec.X, vec.Y, vec.Z);
        return this;
    }

    public Matrixf Translate(float x, float y, float z)
    {
        Mat4f.Translate(Values, Values, x, y, z);
        return this;
    }

    public Matrixf Scale(float x, float y, float z)
    {
        Mat4f.Scale(Values, Values, x, y, z);
        return this;
    }

    public Matrixf RotateDeg(Vec3f degrees)
    {
        Mat4f.RotateX(Values, Values, degrees.X * (MathF.PI / 180f));
        Mat4f.RotateY(Values, Values, degrees.Y * (MathF.PI / 180f));
        Mat4f.RotateZ(Values, Values, degrees.Z * (MathF.PI / 180f));
        return this;
    }

    public Matrixf Rotate(Vec3f radians)
    {
        Mat4f.RotateX(Values, Values, radians.X);
        Mat4f.RotateY(Values, Values, radians.Y);
        Mat4f.RotateZ(Values, Values, radians.Z);
        return this;
    }

    public Matrixf Rotate(float radX, float radY, float radZ)
    {
        Mat4f.RotateX(Values, Values, radX);
        Mat4f.RotateY(Values, Values, radY);
        Mat4f.RotateZ(Values, Values, radZ);
        return this;
    }

    public Matrixf RotateX(float radX)
    {
        Mat4f.RotateX(Values, Values, radX);
        return this;
    }

    public Matrixf RotateY(float radY)
    {
        Mat4f.RotateY(Values, Values, radY);
        return this;
    }

    public Matrixf RotateZ(float radZ)
    {
        Mat4f.RotateZ(Values, Values, radZ);
        return this;
    }

    public Matrixf RotateXDeg(float degX)
    {
        Mat4f.RotateX(Values, Values, degX * (MathF.PI / 180f));
        return this;
    }

    public Matrixf RotateYDeg(float degY)
    {
        Mat4f.RotateY(Values, Values, degY * (MathF.PI / 180f));
        return this;
    }

    public Matrixf RotateZDeg(float degZ)
    {
        Mat4f.RotateZ(Values, Values, degZ * (MathF.PI / 180f));
        return this;
    }

    //
    // Summary:
    //     Vectors with w==0 are called vectors and with w==1 are called points
    //
    // Parameters:
    //   vec:
    public Vec4f TransformVector(Vec4f vec)
    {
        Vec4f vec4f = new Vec4f();
        Mat4f.MulWithVec4(Values, vec, vec4f);
        return vec4f;
    }

    //
    // Summary:
    //     Vectors with w==0 are called vectors and with w==1 are called points
    //
    // Parameters:
    //   vec:
    public Vec4d TransformVector(Vec4d vec)
    {
        Vec4d vec4d = new Vec4d();
        Mat4f.MulWithVec4(Values, vec, vec4d);
        return vec4d;
    }

    public Matrixf Mul(float[] matrix)
    {
        Mat4f.Mul(Values, Values, matrix);
        return this;
    }

    public Matrixf Mul(Matrixf matrix)
    {
        Mat4f.Mul(Values, Values, matrix.Values);
        return this;
    }

    public Matrixf ReverseMul(float[] matrix)
    {
        Mat4f.Mul(Values, matrix, Values);
        return this;
    }

    public Matrixf FollowPlayer()
    {
        Values[12] = 0f;
        Values[13] = 0f;
        Values[14] = 0f;
        return this;
    }

    public Matrixf FollowPlayerXZ()
    {
        Values[12] = 0f;
        Values[14] = 0f;
        return this;
    }

    public Matrixf Invert()
    {
        Mat4f.Invert(Values, Values);
        return this;
    }

    public Matrixf Clone()
    {
        return new Matrixf
        {
            Values = (float[])Values.Clone()
        };
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
