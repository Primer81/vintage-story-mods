using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents one of the 6 faces of a cube and all it's properties. Uses a right Handed Coordinate System. See also http://www.matrix44.net/cms/notes/opengl-3d-graphics/coordinate-systems-in-opengl
/// In short:
/// North: Negative Z
/// East: Positive X
/// South: Positive Z
/// West: Negative X
/// Up: Positive Y
/// Down: Negative Y
/// </summary>
public class BlockFacing
{
	public const int NumberOfFaces = 6;

	public const int indexNORTH = 0;

	public const int indexEAST = 1;

	public const int indexSOUTH = 2;

	public const int indexWEST = 3;

	public const int indexUP = 4;

	public const int indexDOWN = 5;

	/// <summary>
	/// All horizontal blockfacing flags combined
	/// </summary>
	public static readonly byte HorizontalFlags = 15;

	/// <summary>
	/// All vertical blockfacing flags combined
	/// </summary>
	public static readonly byte VerticalFlags = 48;

	/// <summary>
	/// Faces towards negative Z
	/// </summary>
	public static readonly BlockFacing NORTH = new BlockFacing("north", 1, 0, 2, 1, new Vec3i(0, 0, -1), new Vec3f(0.5f, 0.5f, 0f), EnumAxis.Z, new Cuboidf(0f, 0f, 0f, 1f, 1f, 0f));

	/// <summary>
	/// Faces towards positive X
	/// </summary>
	public static readonly BlockFacing EAST = new BlockFacing("east", 2, 1, 3, 0, new Vec3i(1, 0, 0), new Vec3f(1f, 0.5f, 0.5f), EnumAxis.X, new Cuboidf(1f, 0f, 0f, 1f, 1f, 1f));

	/// <summary>
	/// Faces towards positive Z
	/// </summary>
	public static readonly BlockFacing SOUTH = new BlockFacing("south", 4, 2, 0, 3, new Vec3i(0, 0, 1), new Vec3f(0.5f, 0.5f, 1f), EnumAxis.Z, new Cuboidf(0f, 0f, 1f, 1f, 1f, 1f));

	/// <summary>
	/// Faces towards negative X
	/// </summary>
	public static readonly BlockFacing WEST = new BlockFacing("west", 8, 3, 1, 2, new Vec3i(-1, 0, 0), new Vec3f(0f, 0.5f, 0.5f), EnumAxis.X, new Cuboidf(0f, 0f, 0f, 0f, 1f, 1f));

	/// <summary>
	/// Faces towards positive Y
	/// </summary>
	public static readonly BlockFacing UP = new BlockFacing("up", 16, 4, 5, -1, new Vec3i(0, 1, 0), new Vec3f(0.5f, 1f, 0.5f), EnumAxis.Y, new Cuboidf(0f, 1f, 0f, 1f, 1f, 1f));

	/// <summary>
	/// Faces towards negative Y
	/// </summary>
	public static readonly BlockFacing DOWN = new BlockFacing("down", 32, 5, 4, -1, new Vec3i(0, -1, 0), new Vec3f(0.5f, 0f, 0.5f), EnumAxis.Y, new Cuboidf(0f, 0f, 0f, 1f, 0f, 1f));

	/// <summary>
	/// All block faces in the order of N, E, S, W, U, D
	/// </summary>
	public static readonly BlockFacing[] ALLFACES = new BlockFacing[6] { NORTH, EAST, SOUTH, WEST, UP, DOWN };

	/// <summary>
	/// All block faces in the order of N, E, S, W, U, D
	/// </summary>
	public static readonly Vec3i[] ALLNORMALI = new Vec3i[6] { NORTH.normali, EAST.normali, SOUTH.normali, WEST.normali, UP.normali, DOWN.normali };

	/// <summary>
	/// Packed ints representing the normal flags, left-shifted by 15 for easy inclusion in VertexFlags
	/// </summary>
	public static readonly int[] AllVertexFlagsNormals = new int[6] { NORTH.normalPackedFlags, EAST.normalPackedFlags, SOUTH.normalPackedFlags, WEST.normalPackedFlags, UP.normalPackedFlags, DOWN.normalPackedFlags };

	/// <summary>
	/// Array of horizontal faces (N, E, S, W)
	/// </summary>
	public static readonly BlockFacing[] HORIZONTALS = new BlockFacing[4] { NORTH, EAST, SOUTH, WEST };

	/// <summary>
	/// Array of the normals to the horizontal faces (N, E, S, W)
	/// </summary>
	public static readonly Vec3i[] HORIZONTAL_NORMALI = new Vec3i[4] { NORTH.normali, EAST.normali, SOUTH.normali, WEST.normali };

	/// <summary>
	/// Array of vertical faces (U, D)
	/// </summary>
	public static readonly BlockFacing[] VERTICALS = new BlockFacing[2] { UP, DOWN };

	/// <summary>
	/// Array of horizontal faces in angle order (0°, 90°, 180°, 270°) =&gt; (E, N, W, S)
	/// </summary>
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

	/// <summary>
	/// The faces byte flag
	/// </summary>
	public byte Flag => flag;

	/// <summary>
	/// The index of the face (N=0, E=1, S=2, W=3, U=4, D=5)
	/// </summary>
	public int Index => index;

	/// <summary>
	/// Index + 1
	/// </summary>
	public byte MeshDataIndex => meshDataIndex;

	/// <summary>
	/// The angle index of the face (E = 0, N = 1, W = 2, S = 3)
	/// </summary>
	public int HorizontalAngleIndex => horizontalAngleIndex;

	/// <summary>
	/// Returns a normal vector of this face.  Classic iterating through these at a position x,y,z is unlikely to be dimension-aware, use BlockFacing.IterateThruFacingOffsets(pos) instead.
	/// </summary>
	public Vec3i Normali => normali;

	/// <summary>
	/// Returns a normal vector of this face
	/// </summary>
	public Vec3f Normalf => normalf;

	public Vec3d Normald => normald;

	/// <summary>
	/// Returns a cuboid where either the width, height or length is zero which represents the min/max of the block 2D plane in 3D space
	/// </summary>
	public Cuboidf Plane => plane;

	/// <summary>
	/// Returns a normal vector of this face encoded in 6 bits/
	/// bit 0: 1 if south or west
	/// bit 1: sign bit
	/// bit 2: 1 if up or down
	/// bit 3: sign bit
	/// bit 4: 1 if north or south
	/// bit 5: sign bit
	/// </summary>
	public byte NormalByte => normalb;

	/// <summary>
	/// Normalized normal vector in format GL_INT_2_10_10_10_REV
	/// </summary>
	public int NormalPacked => normalPacked;

	/// <summary>
	/// Normalized normal vector packed into 3x4=12 bytes total and bit shifted by 15 bits, for use in meshdata flags data
	/// </summary>
	public int NormalPackedFlags => normalPackedFlags;

	/// <summary>
	/// Returns the center position of this face
	/// </summary>
	public Vec3f PlaneCenter => planeCenter;

	/// <summary>
	/// Returns the string north, east, south, west, up or down
	/// </summary>
	public string Code => code;

	/// <summary>
	/// True if this face is N,E,S or W
	/// </summary>
	public bool IsHorizontal => index <= 3;

	/// <summary>
	/// True if this face is U or D
	/// </summary>
	public bool IsVertical => index >= 4;

	/// <summary>
	/// True if this face is N or S
	/// </summary>
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

	/// <summary>
	/// True if this face is N or S
	/// </summary>
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

	/// <summary>
	/// The normal axis of this vector.
	/// </summary>
	public EnumAxis Axis => axis;

	/// <summary>
	/// Returns the opposing face
	/// </summary>
	/// <returns></returns>
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

	/// <summary>
	/// Returns the face if current face would be horizontally counter-clockwise rotated, only works for horizontal faces
	/// </summary>
	/// <returns></returns>
	public BlockFacing GetCCW()
	{
		return HORIZONTALS_ANGLEORDER[(horizontalAngleIndex + 1) % 4];
	}

	/// <summary>
	/// Returns the face if current face would be horizontally clockwise rotated, only works for horizontal faces
	/// </summary>
	/// <returns></returns>
	public BlockFacing GetCW()
	{
		return HORIZONTALS_ANGLEORDER[GameMath.Mod(horizontalAngleIndex - 1, 4)];
	}

	/// <summary>
	/// Gets the Horizontal BlockFacing by applying the given angel
	/// If used on a UP or DOWN BlockFacing it will return it's current BlockFacing
	/// </summary>
	/// <param name="angle"></param>
	/// <returns></returns>
	public BlockFacing GetHorizontalRotated(int angle)
	{
		if (horizontalAngleIndex < 0)
		{
			return this;
		}
		int indexRot = GameMath.Mod(angle / 90 + index, 4);
		return HORIZONTALS[indexRot];
	}

	/// <summary>
	/// Applies a 3d rotation on the face and returns the face thats closest to the rotated face
	/// </summary>
	/// <param name="radX"></param>
	/// <param name="radY"></param>
	/// <param name="radZ"></param>
	/// <returns></returns>
	public BlockFacing FaceWhenRotatedBy(float radX, float radY, float radZ)
	{
		float[] matrix = Mat4f.Create();
		Mat4f.RotateX(matrix, matrix, radX);
		Mat4f.RotateY(matrix, matrix, radY);
		Mat4f.RotateZ(matrix, matrix, radZ);
		float[] pos = new float[4] { Normalf.X, Normalf.Y, Normalf.Z, 1f };
		pos = Mat4f.MulWithVec4(matrix, pos);
		float smallestAngle = (float)Math.PI;
		BlockFacing facing = null;
		for (int i = 0; i < ALLFACES.Length; i++)
		{
			BlockFacing f = ALLFACES[i];
			float angle = (float)Math.Acos(f.Normalf.Dot(pos));
			if (angle < smallestAngle)
			{
				smallestAngle = angle;
				facing = f;
			}
		}
		return facing;
	}

	/// <summary>
	/// Rotates the face by given angle and returns the interpolated brightness of this face.
	/// </summary>
	/// <param name="radX"></param>
	/// <param name="radY"></param>
	/// <param name="radZ"></param>
	/// <param name="BlockSideBrightnessByFacing">Array of brightness values between 0 and 1 per face. In index order (N, E, S, W, U, D)</param>
	/// <returns></returns>
	public float GetFaceBrightness(float radX, float radY, float radZ, float[] BlockSideBrightnessByFacing)
	{
		float[] array = Mat4f.Create();
		Mat4f.RotateX(array, array, radX);
		Mat4f.RotateY(array, array, radY);
		Mat4f.RotateZ(array, array, radZ);
		FastVec3f pos = Mat4f.MulWithVec3(array, Normalf.X, Normalf.Y, Normalf.Z);
		float brightness = 0f;
		for (int i = 0; i < ALLFACES.Length; i++)
		{
			BlockFacing f = ALLFACES[i];
			float angle = (float)Math.Acos(f.Normalf.Dot(pos));
			if (!(angle >= (float)Math.PI / 2f))
			{
				brightness += (1f - angle / ((float)Math.PI / 2f)) * BlockSideBrightnessByFacing[f.Index];
			}
		}
		return brightness;
	}

	/// <summary>
	/// Project pos onto the block face
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
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

	/// <summary>
	/// In 1.20+ this is the recommended technique for examining blocks on all sides of a BlockPos position, as it is dimension-aware<br />
	/// Successive calls to this when looping through the standard six BlockFacings will set pos to the relevant facing offset from the original position<br />
	/// NOTE: this modifies the fields of the pos parameter, which is better for heap usage than creating a new BlockPos object for each iteration
	/// <br />If necessary to restore the original blockPos value, call FinishIteratingAllFaces(pos)
	/// </summary>
	/// <param name="pos"></param>
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

	/// <summary>
	/// Restores the original value of pos, if we are certain we looped through ALLFACES using IterateThruFacingOffsets
	/// Note: if for any reason control might have exited the loop early, this cannot sensibly be used
	/// </summary>
	/// <param name="pos"></param>
	public static void FinishIteratingAllFaces(BlockPos pos)
	{
		pos.Y++;
	}

	/// <summary>
	/// Rotates the face by given angle and returns the interpolated brightness of this face.
	/// </summary>
	/// <param name="matrix"></param>
	/// <param name="BlockSideBrightnessByFacing">Array of brightness values between 0 and 1 per face. In index order (N, E, S, W, U, D)</param>
	/// <returns></returns>
	public float GetFaceBrightness(double[] matrix, float[] BlockSideBrightnessByFacing)
	{
		double[] pos = new double[4] { Normalf.X, Normalf.Y, Normalf.Z, 1.0 };
		matrix[12] = 0.0;
		matrix[13] = 0.0;
		matrix[14] = 0.0;
		pos = Mat4d.MulWithVec4(matrix, pos);
		float len = GameMath.Sqrt(pos[0] * pos[0] + pos[1] * pos[1] + pos[2] * pos[2]);
		pos[0] /= len;
		pos[1] /= len;
		pos[2] /= len;
		float brightness = 0f;
		for (int i = 0; i < ALLFACES.Length; i++)
		{
			BlockFacing f = ALLFACES[i];
			float angle = (float)Math.Acos(f.Normalf.Dot(pos));
			if (!(angle >= (float)Math.PI / 2f))
			{
				brightness += (1f - angle / ((float)Math.PI / 2f)) * BlockSideBrightnessByFacing[f.Index];
			}
		}
		return brightness;
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

	/// <summary>
	/// Returns the face if code is 'north', 'east', 'south', 'west', 'north', 'up' or 'down'. Otherwise null.
	/// </summary>
	/// <param name="code"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Returns the face if code is 'n', 'e', 's', 'w', 'n', 'u' or 'd'. Otherwise null.
	/// </summary>
	/// <param name="code"></param>
	/// <returns></returns>
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
		float smallestAngle = (float)Math.PI;
		BlockFacing facing = null;
		for (int i = 0; i < ALLFACES.Length; i++)
		{
			BlockFacing f = ALLFACES[i];
			float angle = (float)Math.Acos(f.Normalf.Dot(vec));
			if (angle < smallestAngle)
			{
				smallestAngle = angle;
				facing = f;
			}
		}
		return facing;
	}

	public static BlockFacing FromNormal(Vec3i vec)
	{
		for (int i = 0; i < ALLFACES.Length; i++)
		{
			BlockFacing f = ALLFACES[i];
			if (f.normali.Equals(vec))
			{
				return f;
			}
		}
		return null;
	}

	public static BlockFacing FromVector(double x, double y, double z)
	{
		float smallestAngle = (float)Math.PI;
		BlockFacing facing = null;
		double len = GameMath.Sqrt(x * x + y * y + z * z);
		x /= len;
		y /= len;
		z /= len;
		for (int i = 0; i < ALLFACES.Length; i++)
		{
			BlockFacing f = ALLFACES[i];
			float angle = (float)Math.Acos((double)f.Normalf.X * x + (double)f.Normalf.Y * y + (double)f.Normalf.Z * z);
			if (angle < smallestAngle)
			{
				smallestAngle = angle;
				facing = f;
			}
		}
		return facing;
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

	/// <summary>
	/// Returns the closest horizontal face from given angle (0 degree = east). Uses HORIZONTALS_ANGLEORDER
	/// </summary>
	/// <param name="radians"></param>
	/// <returns></returns>
	public static BlockFacing HorizontalFromAngle(float radians)
	{
		int index = GameMath.Mod((int)Math.Round(radians * (180f / (float)Math.PI) / 90f), 4);
		return HORIZONTALS_ANGLEORDER[index];
	}

	/// <summary>
	/// Returns the closest horizontal face from given angle (0 degree = north for yaw!). Uses HORIZONTALS_ANGLEORDER
	/// </summary>
	/// <param name="radians"></param>
	/// <returns></returns>
	public static BlockFacing HorizontalFromYaw(float radians)
	{
		int index = GameMath.Mod((int)Math.Round(radians * (180f / (float)Math.PI) / 90f) - 1, 4);
		return HORIZONTALS_ANGLEORDER[index];
	}

	/// <summary>
	/// Returns true if given byte flags contain given face
	/// </summary>
	/// <param name="flag"></param>
	/// <param name="facing"></param>
	/// <returns></returns>
	public static bool FlagContains(byte flag, BlockFacing facing)
	{
		return (flag & facing.flag) > 0;
	}

	/// <summary>
	/// Returns true if given byte flags contains a horizontal face
	/// </summary>
	/// <param name="flag"></param>
	/// <returns></returns>
	public static bool FlagContainsHorizontals(byte flag)
	{
		return (flag & HorizontalFlags) > 0;
	}
}
