#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using Vintagestory.API.Common;

namespace Vintagestory.API.MathTools;

public class AABBIntersectionTest
{
    private BlockFacing hitOnBlockFaceTmp = BlockFacing.DOWN;

    private Vec3d hitPositionTmp = new Vec3d();

    private Vec3d lastExitedBlockFacePos = new Vec3d();

    public IWorldIntersectionSupplier bsTester;

    private Cuboidd tmpCuboidd = new Cuboidd();

    public Vec3d hitPosition = new Vec3d();

    public Ray ray = new Ray();

    public BlockPos pos = new BlockPos();

    public BlockFacing hitOnBlockFace = BlockFacing.DOWN;

    public int hitOnSelectionBox;

    private Block blockIntersected;

    public AABBIntersectionTest(IWorldIntersectionSupplier blockSelectionTester)
    {
        bsTester = blockSelectionTester;
    }

    public void LoadRayAndPos(Line3D line3d)
    {
        ray.origin.Set(line3d.Start[0], line3d.Start[1], line3d.Start[2]);
        ray.dir.Set(line3d.End[0] - line3d.Start[0], line3d.End[1] - line3d.Start[1], line3d.End[2] - line3d.Start[2]);
        pos.SetAndCorrectDimension((int)line3d.Start[0], (int)line3d.Start[1], (int)line3d.Start[2]);
    }

    public void LoadRayAndPos(Ray ray)
    {
        this.ray = ray;
        pos.SetAndCorrectDimension(ray.origin);
    }

    public BlockSelection GetSelectedBlock(Vec3d from, Vec3d to, BlockFilter filter = null)
    {
        LoadRayAndPos(new Line3D
        {
            Start = new double[3] { from.X, from.Y, from.Z },
            End = new double[3] { to.X, to.Y, to.Z }
        });
        float maxDistance = from.DistanceTo(to);
        return GetSelectedBlock(maxDistance, filter);
    }

    public BlockSelection GetSelectedBlock(float maxDistance, BlockFilter filter = null, bool testCollide = false)
    {
        float num = 0f;
        BlockFacing exitingFullBlockFace = GetExitingFullBlockFace(pos, ref lastExitedBlockFacePos);
        if (exitingFullBlockFace == null)
        {
            return null;
        }

        float num2 = (maxDistance + 1f) * (maxDistance + 1f);
        while (!RayIntersectsBlockSelectionBox(pos, filter, testCollide))
        {
            if (num >= num2)
            {
                return null;
            }

            pos.Offset(exitingFullBlockFace);
            exitingFullBlockFace = GetExitingFullBlockFace(pos, ref lastExitedBlockFacePos);
            if (exitingFullBlockFace == null)
            {
                return null;
            }

            num = pos.DistanceSqTo(ray.origin.X - 0.5, ray.origin.Y - 0.5, ray.origin.Z - 0.5);
        }

        if (hitPosition.SquareDistanceTo(ray.origin) > maxDistance * maxDistance)
        {
            return null;
        }

        return new BlockSelection
        {
            Face = hitOnBlockFace,
            Position = pos.CopyAndCorrectDimension(),
            HitPosition = hitPosition.SubCopy(pos.X, pos.InternalY, pos.Z),
            SelectionBoxIndex = hitOnSelectionBox,
            Block = blockIntersected
        };
    }

    public bool RayIntersectsBlockSelectionBox(BlockPos pos, BlockFilter filter, bool testCollide = false)
    {
        Block block = bsTester.blockAccessor.GetBlock(pos, 2);
        Cuboidf[] array;
        if (block.SideSolid.Any)
        {
            array = (testCollide ? block.GetCollisionBoxes(bsTester.blockAccessor, pos) : block.GetSelectionBoxes(bsTester.blockAccessor, pos));
        }
        else
        {
            block = bsTester.GetBlock(pos);
            array = (testCollide ? block.GetCollisionBoxes(bsTester.blockAccessor, pos) : bsTester.GetBlockIntersectionBoxes(pos));
        }

        if (array == null)
        {
            return false;
        }

        if (filter != null && !filter(pos, block))
        {
            return false;
        }

        bool flag = false;
        bool flag2 = false;
        for (int i = 0; i < array.Length; i++)
        {
            tmpCuboidd.Set(array[i]).Translate(pos.X, pos.InternalY, pos.Z);
            if (RayIntersectsWithCuboid(tmpCuboidd, ref hitOnBlockFaceTmp, ref hitPositionTmp))
            {
                bool flag3 = array[i] is DecorSelectionBox;
                if (!flag || !(!flag2 || flag3) || !(hitPosition.SquareDistanceTo(ray.origin) <= hitPositionTmp.SquareDistanceTo(ray.origin)))
                {
                    hitOnSelectionBox = i;
                    flag = true;
                    flag2 = flag3;
                    hitOnBlockFace = hitOnBlockFaceTmp;
                    hitPosition.Set(hitPositionTmp);
                }
            }
        }

        if (flag && array[hitOnSelectionBox] is DecorSelectionBox { PosAdjust: var posAdjust } && posAdjust != null)
        {
            pos.Add(posAdjust);
            block = bsTester.GetBlock(pos);
        }

        if (flag)
        {
            blockIntersected = block;
        }

        return flag;
    }

    public bool RayIntersectsWithCuboid(Cuboidd selectionBox)
    {
        if (selectionBox == null)
        {
            return false;
        }

        return RayIntersectsWithCuboid(tmpCuboidd, ref hitOnBlockFace, ref hitPosition);
    }

    public bool RayIntersectsWithCuboid(Cuboidf selectionBox, double posX, double posY, double posZ)
    {
        if (selectionBox == null)
        {
            return false;
        }

        tmpCuboidd.Set(selectionBox).Translate(posX, posY, posZ);
        return RayIntersectsWithCuboid(tmpCuboidd, ref hitOnBlockFace, ref hitPosition);
    }

    public bool RayIntersectsWithCuboid(Cuboidd selectionBox, ref BlockFacing hitOnBlockFace, ref Vec3d hitPosition)
    {
        if (selectionBox == null)
        {
            return false;
        }

        double num = selectionBox.X2 - selectionBox.X1;
        double num2 = selectionBox.Y2 - selectionBox.Y1;
        double num3 = selectionBox.Z2 - selectionBox.Z1;
        for (int i = 0; i < 6; i++)
        {
            BlockFacing blockFacing = BlockFacing.ALLFACES[i];
            Vec3i normali = blockFacing.Normali;
            double num4 = (double)normali.X * ray.dir.X + (double)normali.Y * ray.dir.Y + (double)normali.Z * ray.dir.Z;
            if (!(num4 < -1E-05))
            {
                continue;
            }

            Vec3d vec3d = blockFacing.PlaneCenter.ToVec3d().Mul(num, num2, num3).Add(selectionBox.X1, selectionBox.Y1, selectionBox.Z1);
            Vec3d vec3d2 = Vec3d.Sub(vec3d, ray.origin);
            double num5 = (vec3d2.X * (double)normali.X + vec3d2.Y * (double)normali.Y + vec3d2.Z * (double)normali.Z) / num4;
            if (num5 >= 0.0)
            {
                hitPosition = new Vec3d(ray.origin.X + ray.dir.X * num5, ray.origin.Y + ray.dir.Y * num5, ray.origin.Z + ray.dir.Z * num5);
                lastExitedBlockFacePos = Vec3d.Sub(hitPosition, vec3d);
                if (Math.Abs(lastExitedBlockFacePos.X) <= num / 2.0 && Math.Abs(lastExitedBlockFacePos.Y) <= num2 / 2.0 && Math.Abs(lastExitedBlockFacePos.Z) <= num3 / 2.0)
                {
                    hitOnBlockFace = blockFacing;
                    return true;
                }
            }
        }

        return false;
    }

    public static bool RayInteresectWithCuboidSlabMethod(Cuboidd b, Ray r)
    {
        double val = (b.X1 - r.dir.X) / r.dir.X;
        double val2 = (b.X2 - r.dir.X) / r.dir.X;
        double val3 = Math.Min(val, val2);
        double val4 = Math.Max(val, val2);
        double val5 = (b.Y1 - r.dir.Y) / r.dir.Y;
        double val6 = (b.Y2 - r.dir.Y) / r.dir.Y;
        val3 = Math.Max(val3, Math.Min(val5, val6));
        double val7 = Math.Min(val4, Math.Max(val5, val6));
        double val8 = (b.Z1 - r.dir.Z) / r.dir.Z;
        double val9 = (b.Z2 - r.dir.Z) / r.dir.Z;
        val3 = Math.Max(val3, Math.Min(val8, val9));
        return Math.Min(val7, Math.Max(val8, val9)) >= val3;
    }

    private BlockFacing GetExitingFullBlockFace(BlockPos pos, ref Vec3d exitPos)
    {
        for (int i = 0; i < 6; i++)
        {
            BlockFacing blockFacing = BlockFacing.ALLFACES[i];
            Vec3i normali = blockFacing.Normali;
            double num = (double)normali.X * ray.dir.X + (double)normali.Y * ray.dir.Y + (double)normali.Z * ray.dir.Z;
            if (!(num > 1E-05))
            {
                continue;
            }

            Vec3d vec3d = pos.ToVec3d().Add(blockFacing.PlaneCenter);
            Vec3d vec3d2 = Vec3d.Sub(vec3d, ray.origin);
            double num2 = (vec3d2.X * (double)normali.X + vec3d2.Y * (double)normali.Y + vec3d2.Z * (double)normali.Z) / num;
            if (num2 >= 0.0)
            {
                Vec3d a = new Vec3d(ray.origin.X + ray.dir.X * num2, ray.origin.Y + ray.dir.Y * num2, ray.origin.Z + ray.dir.Z * num2);
                exitPos = Vec3d.Sub(a, vec3d);
                if (Math.Abs(exitPos.X) <= 0.5 && Math.Abs(exitPos.Y) <= 0.5 && Math.Abs(exitPos.Z) <= 0.5)
                {
                    return blockFacing;
                }
            }
        }

        return null;
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
