using System;
using System.IO;
using ProtoBuf;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common.Entities;

/// <summary>
/// Represents all positional information of an entity, such as coordinates, motion and angles
/// </summary>
[ProtoContract]
public class EntityPos
{
	[ProtoMember(1)]
	protected double x;

	[ProtoMember(2)]
	protected double y;

	[ProtoMember(3)]
	protected double z;

	[ProtoMember(4)]
	public int Dimension;

	[ProtoMember(5)]
	protected float roll;

	[ProtoMember(6)]
	protected float yaw;

	[ProtoMember(7)]
	protected float pitch;

	[ProtoMember(8)]
	protected int stance;

	/// <summary>
	/// The yaw of the agents head
	/// </summary>
	[ProtoMember(9)]
	public float HeadYaw;

	/// <summary>
	/// The pitch of the agents head
	/// </summary>
	[ProtoMember(10)]
	public float HeadPitch;

	[ProtoMember(11)]
	public Vec3d Motion = new Vec3d();

	/// <summary>
	/// The X position of the Entity.
	/// </summary>
	public virtual double X
	{
		get
		{
			return x;
		}
		set
		{
			x = value;
		}
	}

	/// <summary>
	/// The Y position of the Entity.
	/// </summary>
	public virtual double Y
	{
		get
		{
			return y;
		}
		set
		{
			y = value;
		}
	}

	public virtual double InternalY => y + (double)(Dimension * 32768);

	/// <summary>
	/// The Z position of the Entity.
	/// </summary>
	public virtual double Z
	{
		get
		{
			return z;
		}
		set
		{
			z = value;
		}
	}

	public virtual int DimensionYAdjustment => Dimension * 32768;

	/// <summary>
	/// The rotation around the X axis, in radians.
	/// </summary>
	public virtual float Roll
	{
		get
		{
			return roll;
		}
		set
		{
			roll = value;
		}
	}

	/// <summary>
	/// The rotation around the Y axis, in radians.
	/// </summary>
	public virtual float Yaw
	{
		get
		{
			return yaw;
		}
		set
		{
			yaw = value;
		}
	}

	/// <summary>
	/// The rotation around the Z axis, in radians.
	/// </summary>
	public virtual float Pitch
	{
		get
		{
			return pitch;
		}
		set
		{
			pitch = value;
		}
	}

	/// <summary>
	/// Returns the position as BlockPos object
	/// </summary>
	public BlockPos AsBlockPos => new BlockPos((int)x, (int)y, (int)z, Dimension);

	/// <summary>
	/// Returns the position as a Vec3i object
	/// </summary>
	public Vec3i XYZInt => new Vec3i((int)x, (int)InternalY, (int)z);

	/// <summary>
	/// Returns the position as a Vec3d object. Note, dimension aware
	/// </summary>
	public Vec3d XYZ => new Vec3d(x, InternalY, z);

	/// <summary>
	/// Returns the position as a Vec3f object
	/// </summary>
	public Vec3f XYZFloat => new Vec3f((float)x, (float)InternalY, (float)z);

	internal int XInt => (int)x;

	internal int YInt => (int)y;

	internal int ZInt => (int)z;

	/// <summary>
	/// Sets this position to a Vec3d, including setting the dimension
	/// </summary>
	/// <param name="pos">The Vec3d to set to.</param>
	public void SetPosWithDimension(Vec3d pos)
	{
		X = pos.X;
		y = pos.Y % 32768.0;
		z = pos.Z;
		Dimension = (int)pos.Y / 32768;
	}

	/// <summary>
	/// Sets this position to a Vec3d, without dimension information - needed in some situations where no dimension change is intended
	/// </summary>
	/// <param name="pos">The Vec3d to set to.</param>
	public void SetPos(Vec3d pos)
	{
		X = pos.X;
		y = pos.Y;
		z = pos.Z;
	}

	public EntityPos()
	{
	}

	public EntityPos(double x, double y, double z, float heading = 0f, float pitch = 0f, float roll = 0f)
	{
		this.x = x;
		this.y = y;
		this.z = z;
		yaw = heading;
		this.pitch = pitch;
		this.roll = roll;
	}

	/// <summary>
	/// Adds given position offset
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns>Returns itself</returns>
	public EntityPos Add(double x, double y, double z)
	{
		X += x;
		this.y += y;
		this.z += z;
		return this;
	}

	/// <summary>
	/// Adds given position offset
	/// </summary>
	/// <param name="vec"></param>
	/// <returns>Returns itself</returns>
	public EntityPos Add(Vec3f vec)
	{
		X += vec.X;
		y += vec.Y;
		z += vec.Z;
		return this;
	}

	/// <summary>
	/// Sets the entity position.
	/// </summary>
	public EntityPos SetPos(int x, int y, int z)
	{
		X = x;
		this.y = y;
		this.z = z;
		return this;
	}

	/// <summary>
	/// Sets the entity position.
	/// </summary>
	public EntityPos SetPos(BlockPos pos)
	{
		X = pos.X;
		y = pos.Y;
		z = pos.Z;
		return this;
	}

	/// <summary>
	/// Sets the entity position.
	/// </summary>
	public EntityPos SetPos(double x, double y, double z)
	{
		X = x;
		this.y = y;
		this.z = z;
		return this;
	}

	/// <summary>
	/// Sets the entity position.
	/// </summary>
	public EntityPos SetPos(EntityPos pos)
	{
		X = pos.x;
		y = pos.y;
		z = pos.z;
		return this;
	}

	/// <summary>
	/// Sets the entity angles.
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
	public EntityPos SetAngles(EntityPos pos)
	{
		Roll = pos.roll;
		yaw = pos.yaw;
		pitch = pos.pitch;
		HeadPitch = pos.HeadPitch;
		HeadYaw = pos.HeadYaw;
		return this;
	}

	/// <summary>
	/// Sets the entity position.
	/// </summary>
	public EntityPos SetAngles(float roll, float yaw, float pitch)
	{
		Roll = roll;
		this.yaw = yaw;
		this.pitch = pitch;
		return this;
	}

	/// <summary>
	/// Sets the Yaw of this entity.
	/// </summary>
	/// <param name="yaw"></param>
	/// <returns></returns>
	public EntityPos SetYaw(float yaw)
	{
		Yaw = yaw;
		return this;
	}

	/// <summary>
	/// Returns true if the entity is within given distance of the other entity
	/// </summary>
	/// <param name="position"></param>
	/// <param name="squareDistance"></param>
	/// <returns></returns>
	public bool InRangeOf(EntityPos position, int squareDistance)
	{
		double num = x - position.x;
		double dy = InternalY - position.InternalY;
		double dz = z - position.z;
		return num * num + dy * dy + dz * dz <= (double)squareDistance;
	}

	/// <summary>
	/// Returns true if the entity is within given distance of given position
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="squareDistance"></param>
	/// <returns></returns>
	public bool InRangeOf(int x, int y, int z, float squareDistance)
	{
		double num = this.x - (double)x;
		double dy = InternalY - (double)y;
		double dz = this.z - (double)z;
		return num * num + dy * dy + dz * dz <= (double)squareDistance;
	}

	/// <summary>
	/// Returns true if the entity is within given distance of given position
	/// </summary>
	/// <param name="x"></param>
	/// <param name="z"></param>
	/// <param name="squareDistance"></param>
	/// <returns></returns>
	public bool InHorizontalRangeOf(int x, int z, float squareDistance)
	{
		double num = this.x - (double)x;
		double dz = this.z - (double)z;
		return num * num + dz * dz <= (double)squareDistance;
	}

	/// <summary>
	/// Returns true if the entity is within given distance of given position
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="squareDistance"></param>
	/// <returns></returns>
	public bool InRangeOf(double x, double y, double z, float squareDistance)
	{
		double num = this.x - x;
		double dy = InternalY - y;
		double dz = this.z - z;
		return num * num + dy * dy + dz * dz <= (double)squareDistance;
	}

	/// <summary>
	/// Returns true if the entity is within given distance of given block position
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="squareDistance"></param>
	/// <returns></returns>
	public bool InRangeOf(BlockPos pos, float squareDistance)
	{
		double num = x - (double)pos.X;
		double dy = InternalY - (double)pos.InternalY;
		double dz = z - (double)pos.Z;
		return num * num + dy * dy + dz * dz <= (double)squareDistance;
	}

	/// <summary>
	/// Returns true if the entity is within given distance of given position
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="squareDistance"></param>
	/// <returns></returns>
	public bool InRangeOf(Vec3f pos, float squareDistance)
	{
		double num = x - (double)pos.X;
		double dy = InternalY - (double)pos.Y;
		double dz = z - (double)pos.Z;
		return num * num + dy * dy + dz * dz <= (double)squareDistance;
	}

	/// <summary>
	/// Returns true if the entity is within given distance of given position
	/// </summary>
	/// <param name="position"></param>
	/// <param name="horRangeSq"></param>
	/// <param name="vertRange"></param>
	/// <returns></returns>
	public bool InRangeOf(Vec3d position, float horRangeSq, float vertRange)
	{
		double num = x - position.X;
		double dz = z - position.Z;
		if (num * num + dz * dz > (double)horRangeSq)
		{
			return false;
		}
		return Math.Abs(InternalY - position.Y) <= (double)vertRange;
	}

	/// <summary>
	/// Returns the squared distance of the entity to this position
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public float SquareDistanceTo(float x, float y, float z)
	{
		double num = this.x - (double)x;
		double dy = InternalY - (double)y;
		double dz = this.z - (double)z;
		return (float)(num * num + dy * dy + dz * dz);
	}

	/// <summary>
	/// Returns the squared distance of the entity to this position. Note: dimension aware, this requires the parameter y coordinate also to be based on InternalY as it should be (like EntityPos.XYZ)
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public float SquareDistanceTo(double x, double y, double z)
	{
		double num = this.x - x;
		double dy = InternalY - y;
		double dz = this.z - z;
		return (float)(num * num + dy * dy + dz * dz);
	}

	/// <summary>
	/// Returns the squared distance of the entity to this position. Note: dimension aware, this requires the parameter Vec3d pos.Y coordinate also to be based on InternalY as it should be (like EntityPos.XYZ)
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
	public double SquareDistanceTo(Vec3d pos)
	{
		double num = x - pos.X;
		double dy = InternalY - pos.Y;
		double dz = z - pos.Z;
		return num * num + dy * dy + dz * dz;
	}

	/// <summary>
	/// Returns the horizontal squared distance of the entity to this position
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
	public double SquareHorDistanceTo(Vec3d pos)
	{
		double num = x - pos.X;
		double dz = z - pos.Z;
		return num * num + dz * dz;
	}

	public double DistanceTo(Vec3d pos)
	{
		double num = x - pos.X;
		double dy = InternalY - pos.Y;
		double dz = z - pos.Z;
		return GameMath.Sqrt(num * num + dy * dy + dz * dz);
	}

	public double DistanceTo(EntityPos pos)
	{
		double num = x - pos.x;
		double dy = InternalY - pos.InternalY;
		double dz = z - pos.z;
		return GameMath.Sqrt(num * num + dy * dy + dz * dz);
	}

	public double HorDistanceTo(Vec3d pos)
	{
		double num = x - pos.X;
		double dz = z - pos.Z;
		return GameMath.Sqrt(num * num + dz * dz);
	}

	public double HorDistanceTo(EntityPos pos)
	{
		double num = x - pos.x;
		double dz = z - pos.z;
		return GameMath.Sqrt(num * num + dz * dz);
	}

	/// <summary>
	/// Returns the squared distance of the entity to this position
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
	public float SquareDistanceTo(EntityPos pos)
	{
		double num = x - pos.x;
		double dy = InternalY - pos.InternalY;
		double dz = z - pos.z;
		return (float)(num * num + dy * dy + dz * dz);
	}

	/// <summary>
	/// Creates a full copy
	/// </summary>
	/// <returns></returns>
	public EntityPos Copy()
	{
		return new EntityPos
		{
			X = x,
			y = y,
			z = z,
			yaw = yaw,
			pitch = pitch,
			roll = roll,
			HeadYaw = HeadYaw,
			HeadPitch = HeadPitch,
			Motion = new Vec3d(Motion.X, Motion.Y, Motion.Z),
			Dimension = Dimension
		};
	}

	/// <summary>
	/// Same as AheadCopy(1) - AheadCopy(0)
	/// </summary>
	/// <returns></returns>
	public Vec3f GetViewVector()
	{
		return GetViewVector(pitch, yaw);
	}

	/// <summary>
	/// Same as AheadCopy(1) - AheadCopy(0)
	/// </summary>
	/// <returns></returns>
	public static Vec3f GetViewVector(float pitch, float yaw)
	{
		float cosPitch = GameMath.Cos(pitch);
		float sinPitch = GameMath.Sin(pitch);
		float cosYaw = GameMath.Cos(yaw);
		float sinYaw = GameMath.Sin(yaw);
		return new Vec3f((0f - cosPitch) * sinYaw, sinPitch, (0f - cosPitch) * cosYaw);
	}

	/// <summary>
	/// Returns a new entity position that is in front of the position the entity is currently looking at
	/// </summary>
	/// <param name="offset"></param>
	/// <returns></returns>
	public EntityPos AheadCopy(double offset)
	{
		float cosPitch = GameMath.Cos(pitch);
		float sinPitch = GameMath.Sin(pitch);
		float cosYaw = GameMath.Cos(yaw);
		float sinYaw = GameMath.Sin(yaw);
		return new EntityPos(x - (double)(cosPitch * sinYaw) * offset, y + (double)sinPitch * offset, z - (double)(cosPitch * cosYaw) * offset, yaw, pitch, roll)
		{
			Dimension = Dimension
		};
	}

	/// <summary>
	/// Returns a new entity position that is in front of the position the entity is currently looking at using only the entities yaw, meaning the resulting coordinate will be always at the same y position.
	/// </summary>
	/// <param name="offset"></param>
	/// <returns></returns>
	public EntityPos HorizontalAheadCopy(double offset)
	{
		float cosYaw = GameMath.Cos(yaw);
		float sinYaw = GameMath.Sin(yaw);
		return new EntityPos(x + (double)sinYaw * offset, y, z + (double)cosYaw * offset, yaw, pitch, roll)
		{
			Dimension = Dimension
		};
	}

	/// <summary>
	/// Returns a new entity position that is behind of the position the entity is currently looking at
	/// </summary>
	/// <param name="offset"></param>
	/// <returns></returns>
	public EntityPos BehindCopy(double offset)
	{
		float cosYaw = GameMath.Cos(0f - yaw);
		float sinYaw = GameMath.Sin(0f - yaw);
		return new EntityPos(x + (double)sinYaw * offset, y, z + (double)cosYaw * offset, yaw, pitch, roll)
		{
			Dimension = Dimension
		};
	}

	/// <summary>
	/// Makes a "basiclly equals" check on the position, motions and angles using a small tolerance of epsilon=0.0001f
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="epsilon"></param>
	/// <returns></returns>
	public bool BasicallySameAs(EntityPos pos, double epsilon = 0.0001)
	{
		double epsilonSquared = epsilon * epsilon;
		if (GameMath.SumOfSquares(x - pos.x, y - pos.y, z - pos.z) >= epsilonSquared)
		{
			return false;
		}
		if (GameMath.Square(roll - pos.roll) < epsilonSquared && GameMath.Square(yaw - pos.yaw) < epsilonSquared && GameMath.Square(pitch - pos.pitch) < epsilonSquared)
		{
			return GameMath.SumOfSquares(Motion.X - pos.Motion.X, Motion.Y - pos.Motion.Y, Motion.Z - pos.Motion.Z) < epsilonSquared;
		}
		return false;
	}

	/// <summary>
	/// Makes a "basiclly equals" check on the position, motions and angles using a small tolerance of epsilon=0.0001f. Ignores motion
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="epsilon"></param>
	/// <returns></returns>
	public bool BasicallySameAsIgnoreMotion(EntityPos pos, double epsilon = 0.0001)
	{
		double epsilonSquared = epsilon * epsilon;
		if (GameMath.Square(x - pos.x) >= epsilonSquared || GameMath.Square(y - pos.y) >= epsilonSquared || GameMath.Square(z - pos.z) >= epsilonSquared)
		{
			return false;
		}
		if (GameMath.Square(roll - pos.roll) < epsilonSquared && GameMath.Square(yaw - pos.yaw) < epsilonSquared)
		{
			return GameMath.Square(pitch - pos.pitch) < epsilonSquared;
		}
		return false;
	}

	/// <summary>
	/// Makes a "basiclly equals" check on position and motions using a small tolerance of epsilon=0.0001f. Ignores the entities angles.
	/// </summary>
	/// <param name="pos"></param>
	/// <param name="epsilon"></param>
	/// <returns></returns>
	public bool BasicallySameAsIgnoreAngles(EntityPos pos, double epsilon = 0.0001)
	{
		double epsilonSquared = epsilon * epsilon;
		if (GameMath.SumOfSquares(x - pos.x, y - pos.y, z - pos.z) < epsilonSquared)
		{
			return GameMath.SumOfSquares(Motion.X - pos.Motion.X, Motion.Y - pos.Motion.Y, Motion.Z - pos.Motion.Z) < epsilonSquared;
		}
		return false;
	}

	/// <summary>
	/// Loads the position and angles from given entity position.
	/// </summary>
	/// <param name="pos"></param>
	/// <returns>Returns itself</returns>
	public EntityPos SetFrom(EntityPos pos)
	{
		X = pos.x;
		y = pos.y;
		z = pos.z;
		Dimension = pos.Dimension;
		roll = pos.roll;
		yaw = pos.yaw;
		pitch = pos.pitch;
		Motion.Set(pos.Motion);
		HeadYaw = pos.HeadYaw;
		HeadPitch = pos.HeadPitch;
		return this;
	}

	/// <summary>
	/// Loads the position from given position.
	/// </summary>
	/// <param name="pos"></param>
	/// <returns>Returns itself</returns>
	public EntityPos SetFrom(Vec3d pos)
	{
		X = pos.X;
		y = pos.Y;
		z = pos.Z;
		return this;
	}

	public override string ToString()
	{
		return "XYZ: " + X + "/" + Y + "/" + Z + ", YPR " + Yaw + "/" + Pitch + "/" + Roll + ", Dim " + Dimension;
	}

	public string OnlyPosToString()
	{
		return X.ToString("#.##", GlobalConstants.DefaultCultureInfo) + ", " + Y.ToString("#.##", GlobalConstants.DefaultCultureInfo) + ", " + Z.ToString("#.##", GlobalConstants.DefaultCultureInfo);
	}

	public string OnlyAnglesToString()
	{
		return roll.ToString("#.##", GlobalConstants.DefaultCultureInfo) + ", " + yaw.ToString("#.##", GlobalConstants.DefaultCultureInfo) + pitch.ToString("#.##", GlobalConstants.DefaultCultureInfo);
	}

	/// <summary>
	/// Serializes all positional information. Does not write HeadYaw and HeadPitch.
	/// </summary>
	/// <param name="writer"></param>
	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(x);
		writer.Write(InternalY);
		writer.Write(z);
		writer.Write(roll);
		writer.Write(yaw);
		writer.Write(pitch);
		writer.Write(stance);
		writer.Write(Motion.X);
		writer.Write(Motion.Y);
		writer.Write(Motion.Z);
	}

	/// <summary>
	/// Deserializes all positional information. Does not read HeadYaw and HeadPitch
	/// </summary>
	/// <param name="reader"></param>
	public void FromBytes(BinaryReader reader)
	{
		x = reader.ReadDouble();
		y = reader.ReadDouble();
		Dimension = (int)y / 32768;
		y -= Dimension * 32768;
		z = reader.ReadDouble();
		roll = reader.ReadSingle();
		yaw = reader.ReadSingle();
		pitch = reader.ReadSingle();
		stance = reader.ReadInt32();
		Motion.X = reader.ReadDouble();
		Motion.Y = reader.ReadDouble();
		Motion.Z = reader.ReadDouble();
	}

	public bool AnyNaN()
	{
		if (double.IsNaN(x + y + z))
		{
			return true;
		}
		if (float.IsNaN(roll + yaw + pitch))
		{
			return true;
		}
		if (double.IsNaN(Motion.X + Motion.Y + Motion.Z))
		{
			return true;
		}
		if (Math.Abs(x) + Math.Abs(y) + Math.Abs(z) > 268435456.0)
		{
			return true;
		}
		return false;
	}
}
