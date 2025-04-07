#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using ProtoBuf;
using Vintagestory.API.Common;

namespace Vintagestory.API.MathTools;

//
// Summary:
//     A useful data structure when operating with block postions.
//     Valuable Hint: Make use of Copy() or the XXXCopy() variants where needed. A common
//     pitfall is writing code like: BlockPos abovePos = pos.Up(); - with this code
//     abovePos and pos will reference to the same object!
[ProtoContract]
[JsonObject(/*Could not decode attribute arguments.*/)]
public class BlockPos : IEquatable<BlockPos>, IVec3
{
    [ProtoMember(1)]
    [JsonProperty]
    public int X;

    [ProtoMember(3)]
    [JsonProperty]
    public int Z;

    public int Y;

    public int dimension;

    public const int DimensionBoundary = 32768;

    [ProtoMember(2)]
    [JsonProperty]
    public int InternalY
    {
        get
        {
            return Y + dimension * 32768;
        }
        set
        {
            Y = value % 32768;
            dimension = value / 32768;
        }
    }

    //
    // Summary:
    //     0 = x, 1 = y, 2 = z
    //
    // Parameters:
    //   i:
    public int this[int i]
    {
        get
        {
            return i switch
            {
                2 => Z,
                1 => Y,
                0 => X,
                _ => dimension,
            };
        }
        set
        {
            switch (i)
            {
                case 0:
                    X = value;
                    break;
                case 1:
                    Y = value;
                    break;
                case 2:
                    Z = value;
                    break;
                default:
                    dimension = value;
                    break;
            }
        }
    }

    int IVec3.XAsInt => X;

    int IVec3.YAsInt => Y;

    int IVec3.ZAsInt => Z;

    double IVec3.XAsDouble => X;

    double IVec3.YAsDouble => Y;

    double IVec3.ZAsDouble => Z;

    float IVec3.XAsFloat => X;

    float IVec3.YAsFloat => Y;

    float IVec3.ZAsFloat => Z;

    [JsonIgnore]
    public Vec3i AsVec3i => new Vec3i(X, Y, Z);

    [Obsolete("Not dimension-aware. Use new BlockPos(dimensionId) where possible")]
    public BlockPos()
    {
    }

    public BlockPos(int dim)
    {
        dimension = dim;
    }

    //
    // Summary:
    //     The new BlockPos takes its dimension from the supplied y value, if the y value
    //     is higher than the DimensionBoundary (32768 blocks). This constructor is therefore
    //     dimension-aware, so long as the y parameter was originally based on .InternalY,
    //     including for example a Vec3d created originally from .InternalY
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public BlockPos(int x, int y, int z)
    {
        X = x;
        Y = y % 32768;
        Z = z;
        dimension = y / 32768;
    }

    public BlockPos(int x, int y, int z, int dim)
    {
        X = x;
        Y = y;
        Z = z;
        dimension = dim;
    }

    [Obsolete("Not dimension-aware. Use overload with a dimension parameter instead")]
    public BlockPos(Vec3i vec)
    {
        X = vec.X;
        Y = vec.Y;
        Z = vec.Z;
    }

    public BlockPos(Vec3i vec, int dim)
    {
        X = vec.X;
        Y = vec.Y;
        Z = vec.Z;
        dimension = dim;
    }

    //
    // Summary:
    //     Note - for backwards compatibility, this is *not* dimension-aware; explicitly
    //     set the dimension in the resulting BlockPos if you need to
    public BlockPos(Vec4i vec)
    {
        X = vec.X;
        Y = vec.Y;
        Z = vec.Z;
    }

    //
    // Summary:
    //     Move the position vertically up
    //
    // Parameters:
    //   dy:
    public BlockPos Up(int dy = 1)
    {
        Y += dy;
        return this;
    }

    //
    // Summary:
    //     Move the position vertically down
    //
    // Parameters:
    //   dy:
    public BlockPos Down(int dy = 1)
    {
        Y -= dy;
        return this;
    }

    //
    // Summary:
    //     Not dimension aware (but existing dimension in this BlockPos will be preserved)
    //     - use SetAndCorrectDimension() for dimension awareness
    //
    // Parameters:
    //   origin:
    public BlockPos Set(Vec3d origin)
    {
        X = (int)origin.X;
        Y = (int)origin.Y;
        Z = (int)origin.Z;
        return this;
    }

    public BlockPos Set(Vec3i pos)
    {
        X = pos.X;
        Y = pos.Y;
        Z = pos.Z;
        return this;
    }

    public BlockPos Set(FastVec3i pos)
    {
        X = pos.X;
        Y = pos.Y;
        Z = pos.Z;
        return this;
    }

    //
    // Summary:
    //     Dimension aware version of Set() - use this if the Vec3d has the dimension embedded
    //     in the Y coordinate (e.g. Y == 65536+ for dimension 2)
    //
    // Parameters:
    //   origin:
    public BlockPos SetAndCorrectDimension(Vec3d origin)
    {
        X = (int)origin.X;
        Y = (int)origin.Y % 32768;
        Z = (int)origin.Z;
        dimension = (int)origin.Y / 32768;
        return this;
    }

    //
    // Summary:
    //     Dimension aware version of Set() - use this if there is a dimension embedded
    //     in the y coordinate (e.g. y == 65536+ for dimension 2)
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public BlockPos SetAndCorrectDimension(int x, int y, int z)
    {
        X = x;
        Y = y % 32768;
        Z = z;
        dimension = y / 32768;
        return this;
    }

    //
    // Summary:
    //     Sets XYZ to new values - not dimension aware (but existing dimension will be
    //     preserved) - use SetAndCorrectDimension() for dimension awareness
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public BlockPos Set(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
        return this;
    }

    public BlockPos Set(float x, float y, float z)
    {
        X = (int)x;
        Y = (int)y;
        Z = (int)z;
        return this;
    }

    public BlockPos Set(BlockPos blockPos)
    {
        X = blockPos.X;
        Y = blockPos.Y;
        Z = blockPos.Z;
        return this;
    }

    public BlockPos Set(BlockPos blockPos, int dim)
    {
        X = blockPos.X;
        Y = blockPos.Y;
        Z = blockPos.Z;
        dimension = dim;
        return this;
    }

    public BlockPos SetDimension(int dim)
    {
        dimension = dim;
        return this;
    }

    //
    // Summary:
    //     Sets this BlockPos to the x,y,z values given, and returns a boolean stating if
    //     the existing values were already equal to x,y,z
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    //
    // Returns:
    //     Returns true if the BlockPos already held these exact x, y, z values (the .Set
    //     operation has not changed anything)
    //     Returns false if the .Set operation caused a change to the BlockPos
    public bool SetAndEquals(int x, int y, int z)
    {
        if (X == x && Z == z && Y == y)
        {
            return true;
        }

        X = x;
        Y = y;
        Z = z;
        return false;
    }

    public void ToBytes(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);
        writer.Write(Z);
        writer.Write(dimension);
    }

    //
    // Summary:
    //     Convert a block position to coordinates relative to the world spawn position.
    //     Note this is dimension unaware
    //
    // Parameters:
    //   api:
    public Vec3i ToLocalPosition(ICoreAPI api)
    {
        return new Vec3i(X - api.World.DefaultSpawnPosition.XInt, Y, Z - api.World.DefaultSpawnPosition.ZInt);
    }

    public BlockPos West()
    {
        X--;
        return this;
    }

    public static BlockPos CreateFromBytes(BinaryReader reader)
    {
        return new BlockPos(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
    }

    public BlockPos North()
    {
        Z--;
        return this;
    }

    public BlockPos East()
    {
        X++;
        return this;
    }

    public BlockPos South()
    {
        Z++;
        return this;
    }

    //
    // Summary:
    //     Returns the direction moved from the other blockPos, to get to this BlockPos
    //
    //
    // Parameters:
    //   other:
    public BlockFacing FacingFrom(BlockPos other)
    {
        int num = other.X - X;
        int num2 = other.Y - Y;
        int num3 = other.Z - Z;
        if (num * num >= num3 * num3)
        {
            if (num * num >= num2 * num2)
            {
                if (num <= 0)
                {
                    return BlockFacing.EAST;
                }

                return BlockFacing.WEST;
            }
        }
        else if (num3 * num3 >= num2 * num2)
        {
            if (num3 <= 0)
            {
                return BlockFacing.SOUTH;
            }

            return BlockFacing.NORTH;
        }

        if (num2 <= 0)
        {
            return BlockFacing.UP;
        }

        return BlockFacing.DOWN;
    }

    //
    // Summary:
    //     Creates a copy of this blocks position with the x-position adjusted by -length
    //
    //
    // Parameters:
    //   length:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos WestCopy(int length = 1)
    {
        return new BlockPos(X - length, Y, Z, dimension);
    }

    //
    // Summary:
    //     Creates a copy of this blocks position with the z-position adjusted by +length
    //
    //
    // Parameters:
    //   length:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos SouthCopy(int length = 1)
    {
        return new BlockPos(X, Y, Z + length, dimension);
    }

    //
    // Summary:
    //     Creates a copy of this blocks position with the x-position adjusted by +length
    //
    //
    // Parameters:
    //   length:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos EastCopy(int length = 1)
    {
        return new BlockPos(X + length, Y, Z, dimension);
    }

    //
    // Summary:
    //     Creates a copy of this blocks position with the z-position adjusted by -length
    //
    //
    // Parameters:
    //   length:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos NorthCopy(int length = 1)
    {
        return new BlockPos(X, Y, Z - length, dimension);
    }

    //
    // Summary:
    //     Creates a copy of this blocks position with the y-position adjusted by -length
    //
    //
    // Parameters:
    //   length:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos DownCopy(int length = 1)
    {
        return new BlockPos(X, Y - length, Z, dimension);
    }

    //
    // Summary:
    //     Creates a copy of this blocks position with the y-position adjusted by +length
    //
    //
    // Parameters:
    //   length:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos UpCopy(int length = 1)
    {
        return new BlockPos(X, Y + length, Z, dimension);
    }

    //
    // Summary:
    //     Creates a copy of this blocks position
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual BlockPos Copy()
    {
        return new BlockPos(X, Y, Z, dimension);
    }

    //
    // Summary:
    //     Creates a copy of this blocks position, obtaining the correct dimension value
    //     from the Y value
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public virtual BlockPos CopyAndCorrectDimension()
    {
        return new BlockPos(X, Y % 32768, Z, dimension + Y / 32768);
    }

    //
    // Summary:
    //     Offsets the position by given xyz
    //
    // Parameters:
    //   dx:
    //
    //   dy:
    //
    //   dz:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos Add(float dx, float dy, float dz)
    {
        X += (int)dx;
        Y += (int)dy;
        Z += (int)dz;
        return this;
    }

    //
    // Summary:
    //     Offsets the position by given xyz
    //
    // Parameters:
    //   dx:
    //
    //   dy:
    //
    //   dz:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos Add(int dx, int dy, int dz)
    {
        X += dx;
        Y += dy;
        Z += dz;
        return this;
    }

    //
    // Summary:
    //     Offsets the position by given xyz vector
    //
    // Parameters:
    //   vector:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos Add(Vec3i vector)
    {
        X += vector.X;
        Y += vector.Y;
        Z += vector.Z;
        return this;
    }

    //
    // Summary:
    //     Offsets the position by given xyz vector
    //
    // Parameters:
    //   vector:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos Add(FastVec3i vector)
    {
        X += vector.X;
        Y += vector.Y;
        Z += vector.Z;
        return this;
    }

    //
    // Summary:
    //     Offsets the position by given xyz vector
    //
    // Parameters:
    //   pos:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos Add(BlockPos pos)
    {
        X += pos.X;
        Y += pos.Y;
        Z += pos.Z;
        return this;
    }

    //
    // Summary:
    //     Offsets the position into the direction of given block face
    //
    // Parameters:
    //   facing:
    //
    //   length:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos Add(BlockFacing facing, int length = 1)
    {
        Vec3i normali = facing.Normali;
        X += normali.X * length;
        Y += normali.Y * length;
        Z += normali.Z * length;
        return this;
    }

    //
    // Summary:
    //     Offsets the position into the direction of given block face
    //
    // Parameters:
    //   facing:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos Offset(BlockFacing facing)
    {
        Vec3i normali = facing.Normali;
        X += normali.X;
        Y += normali.Y;
        Z += normali.Z;
        return this;
    }

    //
    // Summary:
    //     Creates a copy of this blocks position and offsets it by given xyz
    //
    // Parameters:
    //   dx:
    //
    //   dy:
    //
    //   dz:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos AddCopy(float dx, float dy, float dz)
    {
        return new BlockPos((int)((float)X + dx), (int)((float)Y + dy), (int)((float)Z + dz), dimension);
    }

    //
    // Summary:
    //     Creates a copy of this blocks position and offsets it by given xyz
    //
    // Parameters:
    //   dx:
    //
    //   dy:
    //
    //   dz:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos AddCopy(int dx, int dy, int dz)
    {
        return new BlockPos(X + dx, Y + dy, Z + dz, dimension);
    }

    //
    // Summary:
    //     Creates a copy of this blocks position and offsets it by given xyz
    //
    // Parameters:
    //   xyz:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos AddCopy(int xyz)
    {
        return new BlockPos(X + xyz, Y + xyz, Z + xyz, dimension);
    }

    //
    // Summary:
    //     Creates a copy of this blocks position and offsets it by given xyz
    //
    // Parameters:
    //   vector:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos AddCopy(Vec3i vector)
    {
        return new BlockPos(X + vector.X, Y + vector.Y, Z + vector.Z, dimension);
    }

    //
    // Summary:
    //     Creates a copy of this blocks position and offsets it in the direction of given
    //     block face
    //
    // Parameters:
    //   facing:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos AddCopy(BlockFacing facing)
    {
        return new BlockPos(X + facing.Normali.X, Y + facing.Normali.Y, Z + facing.Normali.Z, dimension);
    }

    //
    // Summary:
    //     Creates a copy of this blocks position and offsets it in the direction of given
    //     block face
    //
    // Parameters:
    //   facing:
    //
    //   length:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos AddCopy(BlockFacing facing, int length)
    {
        return new BlockPos(X + facing.Normali.X * length, Y + facing.Normali.Y * length, Z + facing.Normali.Z * length, dimension);
    }

    //
    // Summary:
    //     Substract a position => you'll have the manhatten distance
    //
    // Parameters:
    //   pos:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos Sub(BlockPos pos)
    {
        X -= pos.X;
        Y -= pos.Y;
        Z -= pos.Z;
        return this;
    }

    //
    // Summary:
    //     Substract a position => you'll have the manhatten distance
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos Sub(int x, int y, int z)
    {
        X -= x;
        Y -= y;
        Z -= z;
        return this;
    }

    //
    // Summary:
    //     Substract a position => you'll have the manhatten distance.
    //     If used within a non-zero dimension the resulting BlockPos will be dimensionless
    //     as it's a distance or offset between two positions
    //
    // Parameters:
    //   pos:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos SubCopy(BlockPos pos)
    {
        return new BlockPos(X - pos.X, InternalY - pos.InternalY, Z - pos.Z, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos SubCopy(int x, int y, int z)
    {
        return new BlockPos(X - x, Y - y, Z - z, dimension);
    }

    //
    // Summary:
    //     Creates a copy of this blocks position and divides it by given factor
    //
    // Parameters:
    //   factor:
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BlockPos DivCopy(int factor)
    {
        return new BlockPos(X / factor, Y / factor, Z / factor, dimension);
    }

    //
    // Summary:
    //     Returns the Euclidean distance to between this and given position. Note if dimensions
    //     are different returns maximum value (i.e. infinite)
    //
    // Parameters:
    //   pos:
    public float DistanceTo(BlockPos pos)
    {
        if (pos.dimension != dimension)
        {
            return float.MaxValue;
        }

        double num = pos.X - X;
        double num2 = pos.Y - Y;
        double num3 = pos.Z - Z;
        return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3);
    }

    //
    // Summary:
    //     Returns the Euclidean distance to between this and given position. Note this
    //     is dimension unaware
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public float DistanceTo(double x, double y, double z)
    {
        double num = x - (double)X;
        double num2 = y - (double)Y;
        double num3 = z - (double)Z;
        return GameMath.Sqrt(num * num + num2 * num2 + num3 * num3);
    }

    //
    // Summary:
    //     Returns the squared Euclidean distance to between this and given position. Dimension
    //     aware
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public float DistanceSqTo(double x, double y, double z)
    {
        double num = x - (double)X;
        double num2 = y - (double)InternalY;
        double num3 = z - (double)Z;
        return (float)(num * num + num2 * num2 + num3 * num3);
    }

    //
    // Summary:
    //     Returns the squared Euclidean distance between the nearer edge of this blockpos
    //     (assumed 1 x 0.75 x 1 cube) and given position The 0.75 offset is because the
    //     "heat source" is likely to be above the base position of this block: it's approximate
    //     Note this is dimension unaware
    public double DistanceSqToNearerEdge(double x, double y, double z)
    {
        double num = x - (double)X;
        double num2 = y - (double)Y - 0.75;
        double num3 = z - (double)Z;
        if (num > 0.0)
        {
            num = ((num <= 1.0) ? 0.0 : (num - 1.0));
        }

        if (num3 > 0.0)
        {
            num3 = ((num3 <= 1.0) ? 0.0 : (num3 - 1.0));
        }

        return num * num + num2 * num2 + num3 * num3;
    }

    //
    // Summary:
    //     Returns the squared Euclidean horizontal distance to between this and given position
    //     Note this is dimension unaware
    //
    // Parameters:
    //   x:
    //
    //   z:
    public float HorDistanceSqTo(double x, double z)
    {
        double num = x - (double)X;
        double num2 = z - (double)Z;
        return (float)(num * num + num2 * num2);
    }

    //
    // Summary:
    //     Returns the manhatten distance to given position
    //
    // Parameters:
    //   pos:
    public int HorizontalManhattenDistance(BlockPos pos)
    {
        if (pos.dimension != dimension)
        {
            return int.MaxValue;
        }

        return Math.Abs(X - pos.X) + Math.Abs(Z - pos.Z);
    }

    //
    // Summary:
    //     Returns the manhatten distance to given position
    //
    // Parameters:
    //   pos:
    public int ManhattenDistance(BlockPos pos)
    {
        if (pos.dimension != dimension)
        {
            return int.MaxValue;
        }

        return Math.Abs(X - pos.X) + Math.Abs(Y - pos.Y) + Math.Abs(Z - pos.Z);
    }

    //
    // Summary:
    //     Returns the manhatten distance to given position Note this is dimension unaware
    //
    //
    // Parameters:
    //   x:
    //
    //   y:
    //
    //   z:
    public int ManhattenDistance(int x, int y, int z)
    {
        return Math.Abs(X - x) + Math.Abs(Y - y) + Math.Abs(Z - z);
    }

    //
    // Summary:
    //     Returns true if the specified x,z is within a box the specified range around
    //     this position Note this is dimension unaware
    public bool InRangeHorizontally(int x, int z, int range)
    {
        if (Math.Abs(X - x) <= range)
        {
            return Math.Abs(Z - z) <= range;
        }

        return false;
    }

    //
    // Summary:
    //     Creates a new instance of a Vec3d initialized with this position Note this is
    //     dimension unaware
    public Vec3d ToVec3d()
    {
        return new Vec3d(X, InternalY, Z);
    }

    //
    // Summary:
    //     Creates a new instance of a Vec3i initialized with this position Note this is
    //     dimension unaware
    public Vec3i ToVec3i()
    {
        return new Vec3i(X, InternalY, Z);
    }

    public Vec3f ToVec3f()
    {
        return new Vec3f(X, InternalY, Z);
    }

    public override string ToString()
    {
        return X + ", " + Y + ", " + Z + ((dimension > 0) ? (" : " + dimension) : "");
    }

    public override bool Equals(object obj)
    {
        if (obj is BlockPos blockPos && X == blockPos.X && Y == blockPos.Y && Z == blockPos.Z)
        {
            return dimension == blockPos.dimension;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return ((391 + X) * 23 + Y) * 23 + Z + dimension * 269023;
    }

    public bool Equals(BlockPos other)
    {
        if (other != null && X == other.X && Y == other.Y && Z == other.Z)
        {
            return dimension == other.dimension;
        }

        return false;
    }

    public bool Equals(int x, int y, int z)
    {
        if (X == x && Y == y)
        {
            return Z == z;
        }

        return false;
    }

    public static BlockPos operator +(BlockPos left, BlockPos right)
    {
        return new BlockPos(left.X + right.X, left.Y + right.Y, left.Z + right.Z, left.dimension);
    }

    public static BlockPos operator -(BlockPos left, BlockPos right)
    {
        return new BlockPos(left.X - right.X, left.Y - right.Y, left.Z - right.Z, left.dimension);
    }

    public static BlockPos operator +(BlockPos left, int right)
    {
        return new BlockPos(left.X + right, left.Y + right, left.Z + right, left.dimension);
    }

    public static BlockPos operator -(BlockPos left, int right)
    {
        return new BlockPos(left.X - right, left.Y - right, left.Z - right, left.dimension);
    }

    public static BlockPos operator *(BlockPos left, int right)
    {
        return new BlockPos(left.X * right, left.Y * right, left.Z * right, left.dimension);
    }

    public static BlockPos operator *(int left, BlockPos right)
    {
        return new BlockPos(left * right.X, left * right.Y, left * right.Z, right.dimension);
    }

    public static BlockPos operator /(BlockPos left, int right)
    {
        return new BlockPos(left.X / right, left.Y / right, left.Z / right, left.dimension);
    }

    public static bool operator ==(BlockPos left, BlockPos right)
    {
        return left?.Equals(right) ?? ((object)right == null);
    }

    public static bool operator !=(BlockPos left, BlockPos right)
    {
        return !(left == right);
    }

    public static void Walk(BlockPos startPos, BlockPos untilPos, Vec3i mapSizeForClamp, Action<int, int, int> onpos)
    {
        int num = GameMath.Clamp(Math.Min(startPos.X, untilPos.X), 0, mapSizeForClamp.X);
        int num2 = GameMath.Clamp(Math.Min(startPos.Y, untilPos.Y), 0, mapSizeForClamp.Y);
        int num3 = GameMath.Clamp(Math.Min(startPos.Z, untilPos.Z), 0, mapSizeForClamp.Z);
        int num4 = GameMath.Clamp(Math.Max(startPos.X, untilPos.X), 0, mapSizeForClamp.X);
        int num5 = GameMath.Clamp(Math.Max(startPos.Y, untilPos.Y), 0, mapSizeForClamp.Y);
        int num6 = GameMath.Clamp(Math.Max(startPos.Z, untilPos.Z), 0, mapSizeForClamp.Z);
        for (int i = num; i < num4; i++)
        {
            for (int j = num2; j < num5; j++)
            {
                for (int k = num3; k < num6; k++)
                {
                    onpos(i, j, k);
                }
            }
        }
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
