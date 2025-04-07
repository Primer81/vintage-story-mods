#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.API.MathTools;

//
// Summary:
//     Represents one of the 6 faces of a cube and all it's properties. Uses a right
//     Handed Coordinate System. See also http://www.matrix44.net/cms/notes/opengl-3d-graphics/coordinate-systems-in-opengl
//     In short: North: Negative Z East: Positive X South: Positive Z West: Negative
//     X Up: Positive Y Down: Negative Y
public class BlockFacing
{
    public const int NumberOfFaces = 6;

    public const int indexNORTH = 0;

    public const int indexEAST = 1;

    public const int indexSOUTH = 2;

    public const int indexWEST = 3;

    public const int indexUP = 4;

    public const int indexDOWN = 5;

    //
    // Summary:
    //     All horizontal blockfacing flags combined
    public static readonly byte HorizontalFlags = 15;

    //
    // Summary:
    //     All vertical blockfacing flags combined
    public static readonly byte VerticalFlags = 48;

    //
    // Summary:
    //     Faces towards negative Z
    public static readonly BlockFacing NORTH = new BlockFacing("north", 1, 0, 2, 1, new Vec3i(0, 0, -1), new Vec3f(0.5f, 0.5f, 0f), EnumAxis.Z, new Cuboidf(0f, 0f, 0f, 1f, 1f, 0f));

    //
    // Summary:
    //     Faces towards positive X
    public static readonly BlockFacing EAST = new BlockFacing("east", 2, 1, 3, 0, new Vec3i(1, 0, 0), new Vec3f(1f, 0.5f, 0.5f), EnumAxis.X, new Cuboidf(1f, 0f, 0f, 1f, 1f, 1f));

    //
    // Summary:
    //     Faces towards positive Z
    public static readonly BlockFacing SOUTH = new BlockFacing("south", 4, 2, 0, 3, new Vec3i(0, 0, 1), new Vec3f(0.5f, 0.5f, 1f), EnumAxis.Z, new Cuboidf(0f, 0f, 1f, 1f, 1f, 1f));

    //
    // Summary:
    //     Faces towards negative X
    public static readonly BlockFacing WEST = new BlockFacing("west", 8, 3, 1, 2, new Vec3i(-1, 0, 0), new Vec3f(0f, 0.5f, 0.5f), EnumAxis.X, new Cuboidf(0f, 0f, 0f, 0f, 1f, 1f));

    //
    // Summary:
    //     Faces towards positive Y
    public static readonly BlockFacing UP = new BlockFacing("up", 16, 4, 5, -1, new Vec3i(0, 1, 0), new Vec3f(0.5f, 1f, 0.5f), EnumAxis.Y, new Cuboidf(0f, 1f, 0f, 1f, 1f, 1f));

    //
    // Summary:
    //     Faces towards negative Y
    public static readonly BlockFacing DOWN = new BlockFacing("down", 32, 5, 4, -1, new Vec3i(0, -1, 0), new Vec3f(0.5f, 0f, 0.5f), EnumAxis.Y, new Cuboidf(0f, 0f, 0f, 1f, 0f, 1f));

    //
    // Summary:
    //     All block faces in the order of N, E, S, W, U, D
    public static readonly BlockFacing[] ALLFACES = new BlockFacing[6] { NORTH, EAST, SOUTH, WEST, UP, DOWN };

    //
    // Summary:
    //     All block faces in the order of N, E, S, W, U, D
    public static readonly Vec3i[] ALLNORMALI = new Vec3i[6] { NORTH.normali, EAST.normali, SOUTH.normali, WEST.normali, UP.normali, DOWN.normali };

    //
    // Summary:
    //     Packed ints representing the normal flags, left-shifted by 15 for easy inclusion
    //     in VertexFlags
    public static readonly int[] AllVertexFlagsNormals = new int[6] { NORTH.normalPackedFlags, EAST.normalPackedFlags, SOUTH.normalPackedFlags, WEST.normalPackedFlags, UP.normalPackedFlags, DOWN.normalPackedFlags };

    //
    // Summary:
    //     Array of horizontal faces (N, E, S, W)
    public static readonly BlockFacing[] HORIZONTALS = new BlockFacing[4] { NORTH, EAST, SOUTH, WEST };

    //
    // Summary:
    //     Array of the normals to the horizontal faces (N, E, S, W)
    public static readonly Vec3i[] HORIZONTAL_NORMALI = new Vec3i[4] { NORTH.normali, EAST.normali, SOUTH.normali, WEST.normali };

    //
    // Summary:
    //     Array of vertical faces (U, D)
    public static readonly BlockFacing[] VERTICALS = new BlockFacing[2] { UP, DOWN };

    //
    // Summary:
    //     Array of horizontal faces in angle order (0°, 90°, 180°, 270°) => (E, N, W, S)
    public static readonly BlockFacing[] HORIZONTALS_ANGLEORDER = new BlockFacing[4] { EAST, NORTH, WEST, SOUTH };

    private int index;

    private byte meshDataIndex;

    private int horizontalAngleIndex;

    private byte flag;

    private int oppositeIndex;

    private Vec3i normali;

    private Vec3f normalf;

    private Vec3d normald;

    private byte normalb;

    private int normalPacked;

    private int normalPackedFlags;

    private Vec3f planeCenter;

    private string code;

    private EnumAxis axis;

    private Cuboidf plane;

    //
    // Summary:
    //     The faces byte flag
    public byte Flag => flag;

    //
    // Summary:
    //     The index of the face (N=0, E=1, S=2, W=3, U=4, D=5)
    public int Index => index;

    //
    // Summary:
    //     Index + 1
    public byte MeshDataIndex => meshDataIndex;

    //
    // Summary:
    //     The angle index of the face (E = 0, N = 1, W = 2, S = 3)
    public int HorizontalAngleIndex => horizontalAngleIndex;

    //
    // Summary:
    //     Returns a normal vector of this face. Classic iterating through these at a position
    //     x,y,z is unlikely to be dimension-aware, use BlockFacing.IterateThruFacingOffsets(pos)
    //     instead.
    public Vec3i Normali => normali;

    //
    // Summary:
    //     Returns a normal vector of this face
    public Vec3f Normalf => normalf;

    public Vec3d Normald => normald;

    //
    // Summary:
    //     Returns a cuboid where either the width, height or length is zero which represents
    //     the min/max of the block 2D plane in 3D space
    public Cuboidf Plane => plane;

    //
    // Summary:
    //     Returns a normal vector of this face encoded in 6 bits/ bit 0: 1 if south or
    //     west bit 1: sign bit bit 2: 1 if up or down bit 3: sign bit bit 4: 1 if north
    //     or south bit 5: sign bit
    public byte NormalByte => normalb;

    //
    // Summary:
    //     Normalized normal vector in format GL_INT_2_10_10_10_REV
    public int NormalPacked => normalPacked;

    //
    // Summary:
    //     Normalized normal vector packed into 3x4=12 bytes total and bit shifted by 15
    //     bits, for use in meshdata flags data
    public int NormalPackedFlags => normalPackedFlags;

    //
    // Summary:
    //     Returns the center position of this face
    public Vec3f PlaneCenter => planeCenter;

    //
    // Summary:
    //     Returns the string north, east, south, west, up or down
    public string Code => code;

    //
    // Summary:
    //     True if this face is N,E,S or W
    public bool IsHorizontal => index <= 3;

    //
    // Summary:
    //     True if this face is U or D
    public bool IsVertical => index >= 4;

    //
    // Summary:
    //     True if this face is N or S
    public bool IsAxisNS
    {
        get
        {
            if (index != 0)
            {
                return index == 2;
            }

            return true;
        }
    }

    //
    // Summary:
    //     True if this face is N or S
    public bool IsAxisWE
    {
        get
        {
            if (index != 1)
            {
                return index == 3;
            }

            return true;
        }
    }

    //
    // Summary:
    //     The normal axis of this vector.
    public EnumAxis Axis => axis;

    //
    // Summary:
    //     Returns the opposing face
    public BlockFacing Opposite => ALLFACES[oppositeIndex];

    public bool Negative
    {
        get
        {
            if (index != 0 && index != 3)
            {
                return index == 5;
            }

            return true;
        }
    }

    private BlockFacing(string code, byte flag, int index, int oppositeIndex, int horizontalAngleIndex, Vec3i facingVector, Vec3f planeCenter, EnumAxis axis, Cuboidf plane)
    {
        this.index = index;
        meshDataIndex = (byte)(index + 1);
        this.horizontalAngleIndex = horizontalAngleIndex;
        this.flag = flag;
        this.code = code;
        this.oppositeIndex = oppositeIndex;
        normali = facingVector;
        normalf = new Vec3f(facingVector.X, facingVector.Y, facingVector.Z);
        normald = new Vec3d(facingVector.X, facingVector.Y, facingVector.Z);
        this.plane = plane;
        normalPacked = NormalUtil.PackNormal(normalf.X, normalf.Y, normalf.Z);
        normalb = (byte)(((axis == EnumAxis.Z) ? 1u : 0u) | (((facingVector.Z < 0) ? 1u : 0u) << 1) | (((axis == EnumAxis.Y) ? 1u : 0u) << 2) | (((facingVector.Y < 0) ? 1u : 0u) << 3) | (((axis == EnumAxis.X) ? 1u : 0u) << 4) | (((facingVector.X < 0) ? 1u : 0u) << 5));
        normalPackedFlags = VertexFlags.PackNormal(normalf);
        this.planeCenter = planeCenter;
        this.axis = axis;
    }

    [Obsolete("Use Opposite property instead")]
    public BlockFacing GetOpposite()
    {
        return ALLFACES[oppositeIndex];
    }

    //
    // Summary:
    //     Returns the face if current face would be horizontally counter-clockwise rotated,
    //     only works for horizontal faces
    public BlockFacing GetCCW()
    {
        return HORIZONTALS_ANGLEORDER[(horizontalAngleIndex + 1) % 4];
    }

    //
    // Summary:
    //     Returns the face if current face would be horizontally clockwise rotated, only
    //     works for horizontal faces
    public BlockFacing GetCW()
    {
        return HORIZONTALS_ANGLEORDER[GameMath.Mod(horizontalAngleIndex - 1, 4)];
    }

    //
    // Summary:
    //     Gets the Horizontal BlockFacing by applying the given angel If used on a UP or
    //     DOWN BlockFacing it will return it's current BlockFacing
    //
    // Parameters:
    //   angle:
    public BlockFacing GetHorizontalRotated(int angle)
    {
        if (horizontalAngleIndex < 0)
        {
            return this;
        }

        int num = GameMath.Mod(angle / 90 + index, 4);
        return HORIZONTALS[num];
    }

    //
    // Summary:
    //     Applies a 3d rotation on the face and returns the face thats closest to the rotated
    //     face
    //
    // Parameters:
    //   radX:
    //
    //   radY:
    //
    //   radZ:
    public BlockFacing FaceWhenRotatedBy(float radX, float radY, float radZ)
    {
        float[] array = Mat4f.Create();
        Mat4f.RotateX(array, array, radX);
        Mat4f.RotateY(array, array, radY);
        Mat4f.RotateZ(array, array, radZ);
        float[] vec = new float[4] { Normalf.X, Normalf.Y, Normalf.Z, 1f };
        vec = Mat4f.MulWithVec4(array, vec);
        float num = MathF.PI;
        BlockFacing result = null;
        for (int i = 0; i < ALLFACES.Length; i++)
        {
            BlockFacing blockFacing = ALLFACES[i];
            float num2 = (float)Math.Acos(blockFacing.Normalf.Dot(vec));
            if (num2 < num)
            {
                num = num2;
                result = blockFacing;
            }
        }

        return result;
    }

    //
    // Summary:
    //     Rotates the face by given angle and returns the interpolated brightness of this
    //     face.
    //
    // Parameters:
    //   radX:
    //
    //   radY:
    //
    //   radZ:
    //
    //   BlockSideBrightnessByFacing:
    //     Array of brightness values between 0 and 1 per face. In index order (N, E, S,
    //     W, U, D)
    public float GetFaceBrightness(float radX, float radY, float radZ, float[] BlockSideBrightnessByFacing)
    {
        float[] array = Mat4f.Create();
        Mat4f.RotateX(array, array, radX);
        Mat4f.RotateY(array, array, radY);
        Mat4f.RotateZ(array, array, radZ);
        FastVec3f a = Mat4f.MulWithVec3(array, Normalf.X, Normalf.Y, Normalf.Z);
        float num = 0f;
        for (int i = 0; i < ALLFACES.Length; i++)
        {
            BlockFacing blockFacing = ALLFACES[i];
            float num2 = (float)Math.Acos(blockFacing.Normalf.Dot(a));
            if (!(num2 >= MathF.PI / 2f))
            {
                num += (1f - num2 / (MathF.PI / 2f)) * BlockSideBrightnessByFacing[blockFacing.Index];
            }
        }

        return num;
    }

    //
    // Summary:
    //     Project pos onto the block face
    //
    // Parameters:
    //   pos:
    public Vec2f ToAB(Vec3f pos)
    {
        return axis switch
        {
            EnumAxis.X => new Vec2f(pos.Z, pos.Y),
            EnumAxis.Y => new Vec2f(pos.X, pos.Z),
            EnumAxis.Z => new Vec2f(pos.X, pos.Y),
            _ => null,
        };
    }

    //
    // Summary:
    //     In 1.20+ this is the recommended technique for examining blocks on all sides
    //     of a BlockPos position, as it is dimension-aware
    //     Successive calls to this when looping through the standard six BlockFacings will
    //     set pos to the relevant facing offset from the original position
    //     NOTE: this modifies the fields of the pos parameter, which is better for heap
    //     usage than creating a new BlockPos object for each iteration
    //     If necessary to restore the original blockPos value, call FinishIteratingAllFaces(pos)
    //
    //
    // Parameters:
    //   pos:
    public void IterateThruFacingOffsets(BlockPos pos)
    {
        switch (index)
        {
            case 0:
                pos.Z--;
                break;
            case 1:
                pos.Z++;
                pos.X++;
                break;
            case 2:
                pos.X--;
                pos.Z++;
                break;
            case 3:
                pos.Z--;
                pos.X--;
                break;
            case 4:
                pos.X++;
                pos.Y++;
                break;
            case 5:
                pos.Y -= 2;
                break;
        }
    }

    //
    // Summary:
    //     Restores the original value of pos, if we are certain we looped through ALLFACES
    //     using IterateThruFacingOffsets Note: if for any reason control might have exited
    //     the loop early, this cannot sensibly be used
    //
    // Parameters:
    //   pos:
    public static void FinishIteratingAllFaces(BlockPos pos)
    {
        pos.Y++;
    }

    //
    // Summary:
    //     Rotates the face by given angle and returns the interpolated brightness of this
    //     face.
    //
    // Parameters:
    //   matrix:
    //
    //   BlockSideBrightnessByFacing:
    //     Array of brightness values between 0 and 1 per face. In index order (N, E, S,
    //     W, U, D)
    public float GetFaceBrightness(double[] matrix, float[] BlockSideBrightnessByFacing)
    {
        double[] vec = new double[4] { Normalf.X, Normalf.Y, Normalf.Z, 1.0 };
        matrix[12] = 0.0;
        matrix[13] = 0.0;
        matrix[14] = 0.0;
        vec = Mat4d.MulWithVec4(matrix, vec);
        float num = GameMath.Sqrt(vec[0] * vec[0] + vec[1] * vec[1] + vec[2] * vec[2]);
        vec[0] /= num;
        vec[1] /= num;
        vec[2] /= num;
        float num2 = 0f;
        for (int i = 0; i < ALLFACES.Length; i++)
        {
            BlockFacing blockFacing = ALLFACES[i];
            float num3 = (float)Math.Acos(blockFacing.Normalf.Dot(vec));
            if (!(num3 >= MathF.PI / 2f))
            {
                num2 += (1f - num3 / (MathF.PI / 2f)) * BlockSideBrightnessByFacing[blockFacing.Index];
            }
        }

        return num2;
    }

    public bool IsAdjacent(BlockFacing facing)
    {
        if (IsVertical)
        {
            return facing.IsHorizontal;
        }

        if ((!IsHorizontal || !facing.IsVertical) && (axis != 0 || facing.axis != EnumAxis.Z))
        {
            if (axis == EnumAxis.Z)
            {
                return facing.axis == EnumAxis.X;
            }

            return false;
        }

        return true;
    }

    public override string ToString()
    {
        return code;
    }

    //
    // Summary:
    //     Returns the face if code is 'north', 'east', 'south', 'west', 'north', 'up' or
    //     'down'. Otherwise null.
    //
    // Parameters:
    //   code:
    public static BlockFacing FromCode(string code)
    {
        code = code?.ToLowerInvariant();
        return code switch
        {
            "north" => NORTH,
            "south" => SOUTH,
            "east" => EAST,
            "west" => WEST,
            "up" => UP,
            "down" => DOWN,
            _ => null,
        };
    }

    public static BlockFacing FromFirstLetter(char code)
    {
        return FromFirstLetter(code.ToString() ?? "");
    }

    //
    // Summary:
    //     Returns the face if code is 'n', 'e', 's', 'w', 'n', 'u' or 'd'. Otherwise null.
    //
    //
    // Parameters:
    //   code:
    public static BlockFacing FromFirstLetter(string code)
    {
        if (code.Length < 1)
        {
            return null;
        }

        return char.ToLowerInvariant(code[0]) switch
        {
            'n' => NORTH,
            's' => SOUTH,
            'e' => EAST,
            'w' => WEST,
            'u' => UP,
            'd' => DOWN,
            _ => null,
        };
    }

    public static BlockFacing FromNormal(Vec3f vec)
    {
        float num = MathF.PI;
        BlockFacing result = null;
        for (int i = 0; i < ALLFACES.Length; i++)
        {
            BlockFacing blockFacing = ALLFACES[i];
            float num2 = (float)Math.Acos(blockFacing.Normalf.Dot(vec));
            if (num2 < num)
            {
                num = num2;
                result = blockFacing;
            }
        }

        return result;
    }

    public static BlockFacing FromNormal(Vec3i vec)
    {
        for (int i = 0; i < ALLFACES.Length; i++)
        {
            BlockFacing blockFacing = ALLFACES[i];
            if (blockFacing.normali.Equals(vec))
            {
                return blockFacing;
            }
        }

        return null;
    }

    public static BlockFacing FromVector(double x, double y, double z)
    {
        float num = MathF.PI;
        BlockFacing result = null;
        double num2 = GameMath.Sqrt(x * x + y * y + z * z);
        x /= num2;
        y /= num2;
        z /= num2;
        for (int i = 0; i < ALLFACES.Length; i++)
        {
            BlockFacing blockFacing = ALLFACES[i];
            float num3 = (float)Math.Acos((double)blockFacing.Normalf.X * x + (double)blockFacing.Normalf.Y * y + (double)blockFacing.Normalf.Z * z);
            if (num3 < num)
            {
                num = num3;
                result = blockFacing;
            }
        }

        return result;
    }

    public static BlockFacing FromFlag(int flag)
    {
        return flag switch
        {
            1 => NORTH,
            4 => SOUTH,
            2 => EAST,
            8 => WEST,
            16 => UP,
            32 => DOWN,
            _ => null,
        };
    }

    //
    // Summary:
    //     Returns the closest horizontal face from given angle (0 degree = east). Uses
    //     HORIZONTALS_ANGLEORDER
    //
    // Parameters:
    //   radians:
    public static BlockFacing HorizontalFromAngle(float radians)
    {
        int num = GameMath.Mod((int)Math.Round(radians * (180f / MathF.PI) / 90f), 4);
        return HORIZONTALS_ANGLEORDER[num];
    }

    //
    // Summary:
    //     Returns the closest horizontal face from given angle (0 degree = north for yaw!).
    //     Uses HORIZONTALS_ANGLEORDER
    //
    // Parameters:
    //   radians:
    public static BlockFacing HorizontalFromYaw(float radians)
    {
        int num = GameMath.Mod((int)Math.Round(radians * (180f / MathF.PI) / 90f) - 1, 4);
        return HORIZONTALS_ANGLEORDER[num];
    }

    //
    // Summary:
    //     Returns true if given byte flags contain given face
    //
    // Parameters:
    //   flag:
    //
    //   facing:
    public static bool FlagContains(byte flag, BlockFacing facing)
    {
        return (flag & facing.flag) > 0;
    }

    //
    // Summary:
    //     Returns true if given byte flags contains a horizontal face
    //
    // Parameters:
    //   flag:
    public static bool FlagContainsHorizontals(byte flag)
    {
        return (flag & HorizontalFlags) > 0;
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
