using System;
using System.IO;
using Newtonsoft.Json;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents a vector of 3 doubles. Go bug Tyron of you need more utility methods in this class.
/// </summary>
[ProtoContract]
public class Vec3d : IVec3, IEquatable<Vec3d>
{
	[ProtoMember(1)]
	public double X;

	[ProtoMember(2)]
	public double Y;

	[ProtoMember(3)]
	public double Z;

	[JsonIgnore]
	public BlockPos AsBlockPos => new BlockPos((int)X, (int)Y, (int)Z);

	[JsonIgnore]
	public int XInt => (int)X;

	[JsonIgnore]
	public int YInt => (int)Y;

	[JsonIgnore]
	public int ZInt => (int)Z;

	/// <summary>
	/// Create a new instance with x/y/z set to 0
	/// </summary>
	public static Vec3d Zero => new Vec3d();

	/// <summary>
	/// Returns the n-th coordinate
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public double this[int index]
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

	float IVec3.XAsFloat => (float)X;

	float IVec3.YAsFloat => (float)Y;

	float IVec3.ZAsFloat => (float)Z;

	public Vec3i AsVec3i => new Vec3i((int)X, (int)Y, (int)Z);

	public Vec3d()
	{
	}

	public Vec3d(double x, double y, double z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	/// <summary>
	/// Create a new vector with given coordinates
	/// </summary>
	/// <param name="vec"></param>
	public Vec3d(Vec4d vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	public Vec3d(EntityPos vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	public double Dot(Vec3d a)
	{
		return X * a.X + Y * a.Y + Z * a.Z;
	}

	public Vec3d Cross(Vec3d vec)
	{
		return new Vec3d
		{
			X = Y * vec.Z - Z * vec.Y,
			Y = Z * vec.X - X * vec.Z,
			Z = X * vec.Y - Y * vec.X
		};
	}

	public void Cross(Vec3d a, Vec3d b)
	{
		X = a.Y * b.Z - a.Z * b.Y;
		Y = a.Z * b.X - a.X * b.Z;
		Z = a.X * b.Y - a.Y * b.X;
	}

	public void Cross(Vec3d a, Vec4d b)
	{
		X = a.Y * b.Z - a.Z * b.Y;
		Y = a.Z * b.X - a.X * b.Z;
		Z = a.X * b.Y - a.Y * b.X;
	}

	public void Negate()
	{
		X = 0.0 - X;
		Y = 0.0 - Y;
		Z = 0.0 - Z;
	}

	public void Cross(Vec4d a, Vec4d b)
	{
		X = a.Y * b.Z - a.Z * b.Y;
		Y = a.Z * b.X - a.X * b.Z;
		Z = a.X * b.Y - a.Y * b.X;
	}

	public Vec3d Add(Vec3d a)
	{
		X += a.X;
		Y += a.Y;
		Z += a.Z;
		return this;
	}

	public Vec3d Add(BlockPos a)
	{
		X += a.X;
		Y += a.InternalY;
		Z += a.Z;
		return this;
	}

	public Vec3d Add(Vec3f a)
	{
		X += a.X;
		Y += a.Y;
		Z += a.Z;
		return this;
	}

	public Vec3d AddCopy(Vec3f a)
	{
		return new Vec3d(X + (double)a.X, Y + (double)a.Y, Z + (double)a.Z);
	}

	public Vec3d AddCopy(Vec3d a)
	{
		return new Vec3d(X + a.X, Y + a.Y, Z + a.Z);
	}

	public Vec3d AddCopy(float x, float y, float z)
	{
		return new Vec3d(X + (double)x, Y + (double)y, Z + (double)z);
	}

	public Vec3d AddCopy(double x, double y, double z)
	{
		return new Vec3d(X + x, Y + y, Z + z);
	}

	public Vec3d AddCopy(BlockFacing facing)
	{
		return new Vec3d(X + (double)facing.Normalf.X, Y + (double)facing.Normalf.Y, Z + (double)facing.Normalf.Z);
	}

	public Vec3d AddCopy(BlockPos pos)
	{
		return new Vec3d(X + (double)pos.X, Y + (double)pos.InternalY, Z + (double)pos.Z);
	}

	public Vec3d Mul(double val)
	{
		X *= val;
		Y *= val;
		Z *= val;
		return this;
	}

	public Vec3d Mul(double x, double y, double z)
	{
		X *= x;
		Y *= y;
		Z *= z;
		return this;
	}

	public Vec3d Add(double x, double y, double z)
	{
		X += x;
		Y += y;
		Z += z;
		return this;
	}

	public Vec3d Sub(double x, double y, double z)
	{
		X -= x;
		Y -= y;
		Z -= z;
		return this;
	}

	public Vec3d SubCopy(double x, double y, double z)
	{
		return new Vec3d(X - x, Y - y, Z - z);
	}

	public Vec3d SubCopy(Vec3d sub)
	{
		return new Vec3d(X - sub.X, Y - sub.Y, Z - sub.Z);
	}

	public float[] ToFloatArray()
	{
		return new float[3]
		{
			(float)X,
			(float)Y,
			(float)Z
		};
	}

	public double[] ToDoubleArray()
	{
		return new double[3] { X, Y, Z };
	}

	public double Length()
	{
		return Math.Sqrt(X * X + Y * Y + Z * Z);
	}

	public double HorLength()
	{
		return Math.Sqrt(X * X + Z * Z);
	}

	public double LengthSq()
	{
		return X * X + Y * Y + Z * Z;
	}

	public Vec3d Normalize()
	{
		double length = Length();
		if (length > 0.0)
		{
			X /= length;
			Y /= length;
			Z /= length;
		}
		return this;
	}

	public Vec3d Clone()
	{
		return new Vec3d(X, Y, Z);
	}

	public Vec3d Sub(Vec3d vec)
	{
		X -= vec.X;
		Y -= vec.Y;
		Z -= vec.Z;
		return this;
	}

	public Vec3d Add(double value)
	{
		X += value;
		Y += value;
		Z += value;
		return this;
	}

	public static Vec3d Add(Vec3d a, Vec3d b)
	{
		return new Vec3d(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
	}

	public static Vec3d Sub(Vec3d a, Vec3d b)
	{
		return new Vec3d(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
	}

	public Vec3d Sub(BlockPos pos)
	{
		X -= pos.X;
		Y -= pos.InternalY;
		Z -= pos.Z;
		return this;
	}

	/// <summary>
	/// Note: adjusts the calling Vec3d, does not make a copy
	/// </summary>
	public Vec3d Scale(double f)
	{
		X *= f;
		Y *= f;
		Z *= f;
		return this;
	}

	/// <summary>
	/// Note: adjusts the calling Vec3d, does not make a copy
	/// </summary>
	public Vec3d Offset(Vec3d b)
	{
		X += b.X;
		Y += b.Y;
		Z += b.Z;
		return this;
	}

	/// <summary>
	/// Note: adjusts the calling Vec3d, does not make a copy
	/// </summary>
	public Vec3d Offset(double x, double y, double z)
	{
		X += x;
		Y += y;
		Z += z;
		return this;
	}

	public Vec3d OffsetCopy(float x, float y, float z)
	{
		return new Vec3d(X + (double)x, Y + (double)y, Z + (double)z);
	}

	public Vec3d OffsetCopy(double x, double y, double z)
	{
		return new Vec3d(X + x, Y + y, Z + z);
	}

	public Vec3d Set(Vec3i pos)
	{
		X = pos.X;
		Y = pos.Y;
		Z = pos.Z;
		return this;
	}

	public Vec3d Set(Vec3f pos)
	{
		X = pos.X;
		Y = pos.Y;
		Z = pos.Z;
		return this;
	}

	public Vec3d Set(double x, double y, double z)
	{
		X = x;
		Y = y;
		Z = z;
		return this;
	}

	public Vec3d Set(Vec3d pos)
	{
		X = pos.X;
		Y = pos.Y;
		Z = pos.Z;
		return this;
	}

	public Vec3d Set(BlockPos pos)
	{
		X = pos.X;
		Y = pos.Y;
		Z = pos.Z;
		return this;
	}

	public Vec3d Set(EntityPos pos)
	{
		X = pos.X;
		Y = pos.Y;
		Z = pos.Z;
		return this;
	}

	/// <summary>
	/// Include dimension info. We don't always want this, but sometimes we do
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
	public Vec3d SetWithDimension(EntityPos pos)
	{
		X = pos.X;
		Y = pos.InternalY;
		Z = pos.Z;
		return this;
	}

	public float SquareDistanceTo(float x, float y, float z)
	{
		double num = X - (double)x;
		double dy = Y - (double)y;
		double dz = Z - (double)z;
		return (float)(num * num + dy * dy + dz * dz);
	}

	public float SquareDistanceTo(double x, double y, double z)
	{
		double num = X - x;
		double dy = Y - y;
		double dz = Z - z;
		return (float)(num * num + dy * dy + dz * dz);
	}

	public float SquareDistanceTo(Vec3d pos)
	{
		double num = X - pos.X;
		double dy = Y - pos.Y;
		double dz = Z - pos.Z;
		return (float)(num * num + dy * dy + dz * dz);
	}

	public float SquareDistanceTo(EntityPos pos)
	{
		double num = X - pos.X;
		double dy = Y - pos.Y;
		double dz = Z - pos.Z;
		return (float)(num * num + dy * dy + dz * dz);
	}

	public float DistanceTo(Vec3d pos)
	{
		double num = X - pos.X;
		double dy = Y - pos.Y;
		double dz = Z - pos.Z;
		return (float)Math.Sqrt(num * num + dy * dy + dz * dz);
	}

	public float DistanceTo(double x, double y, double z)
	{
		double num = X - x;
		double dy = Y - y;
		double dz = Z - z;
		return (float)Math.Sqrt(num * num + dy * dy + dz * dz);
	}

	public float HorizontalSquareDistanceTo(Vec3d pos)
	{
		double num = X - pos.X;
		double dz = Z - pos.Z;
		return (float)(num * num + dz * dz);
	}

	public float HorizontalSquareDistanceTo(double x, double z)
	{
		double num = X - x;
		double dz = Z - z;
		return (float)(num * num + dz * dz);
	}

	public static Vec3d operator -(Vec3d left, Vec3d right)
	{
		return new Vec3d(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
	}

	public static Vec3d operator +(Vec3d left, Vec3d right)
	{
		return new Vec3d(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
	}

	public static Vec3d operator +(Vec3d left, Vec3i right)
	{
		return new Vec3d(left.X + (double)right.X, left.Y + (double)right.Y, left.Z + (double)right.Z);
	}

	public static Vec3d operator -(Vec3d left, float right)
	{
		return new Vec3d(left.X - (double)right, left.Y - (double)right, left.Z - (double)right);
	}

	public static Vec3d operator -(float left, Vec3d right)
	{
		return new Vec3d((double)left - right.X, (double)left - right.Y, (double)left - right.Z);
	}

	public static Vec3d operator +(Vec3d left, float right)
	{
		return new Vec3d(left.X + (double)right, left.Y + (double)right, left.Z + (double)right);
	}

	public static Vec3d operator *(Vec3d left, float right)
	{
		return new Vec3d(left.X * (double)right, left.Y * (double)right, left.Z * (double)right);
	}

	public static Vec3d operator *(float left, Vec3d right)
	{
		return new Vec3d((double)left * right.X, (double)left * right.Y, (double)left * right.Z);
	}

	public static Vec3d operator *(Vec3d left, double right)
	{
		return new Vec3d(left.X * right, left.Y * right, left.Z * right);
	}

	public static Vec3d operator *(double left, Vec3d right)
	{
		return new Vec3d(left * right.X, left * right.Y, left * right.Z);
	}

	public static double operator *(Vec3d left, Vec3d right)
	{
		return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
	}

	public static Vec3d operator /(Vec3d left, float right)
	{
		return new Vec3d(left.X / (double)right, left.Y / (double)right, left.Z / (double)right);
	}

	public static Vec3d operator /(Vec3d left, double right)
	{
		return new Vec3d(left.X / right, left.Y / right, left.Z / right);
	}

	public static bool operator ==(Vec3d left, Vec3d right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(Vec3d left, Vec3d right)
	{
		return !(left == right);
	}

	public Vec3f ToVec3f()
	{
		return new Vec3f((float)X, (float)Y, (float)Z);
	}

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

	public static Vec3d CreateFromBytes(BinaryReader reader)
	{
		return new Vec3d(reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());
	}

	public Vec3d RotatedCopy(float yaw)
	{
		Matrixf matrixf = new Matrixf();
		matrixf.RotateY(yaw);
		return matrixf.TransformVector(new Vec4d(X, Y, Z, 0.0)).XYZ;
	}

	public Vec3d AheadCopy(double offset, float Pitch, float Yaw)
	{
		float cosPitch = GameMath.Cos(Pitch);
		float sinPitch = GameMath.Sin(Pitch);
		float cosYaw = GameMath.Cos(Yaw);
		float sinYaw = GameMath.Sin(Yaw);
		return new Vec3d(X - (double)(cosPitch * sinYaw) * offset, Y + (double)sinPitch * offset, Z - (double)(cosPitch * cosYaw) * offset);
	}

	public Vec3d AheadCopy(double offset, double Pitch, double Yaw)
	{
		double cosPitch = Math.Cos(Pitch);
		double sinPitch = Math.Sin(Pitch);
		double cosYaw = Math.Cos(Yaw);
		double sinYaw = Math.Sin(Yaw);
		return new Vec3d(X - cosPitch * sinYaw * offset, Y + sinPitch * offset, Z - cosPitch * cosYaw * offset);
	}

	public Vec3d Ahead(double offset, float Pitch, float Yaw)
	{
		float cosPitch = GameMath.Cos(Pitch);
		float sinPitch = GameMath.Sin(Pitch);
		float cosYaw = GameMath.Cos(Yaw);
		float sinYaw = GameMath.Sin(Yaw);
		X -= (double)(cosPitch * sinYaw) * offset;
		Y += (double)sinPitch * offset;
		Z -= (double)(cosPitch * cosYaw) * offset;
		return this;
	}

	public Vec3d Ahead(double offset, double Pitch, double Yaw)
	{
		double cosPitch = Math.Cos(Pitch);
		double sinPitch = Math.Sin(Pitch);
		double cosYaw = Math.Cos(Yaw);
		double sinYaw = Math.Sin(Yaw);
		X -= cosPitch * sinYaw * offset;
		Y += sinPitch * offset;
		Z -= cosPitch * cosYaw * offset;
		return this;
	}

	public bool Equals(Vec3d other, double epsilon)
	{
		if (Math.Abs(X - other.X) < epsilon && Math.Abs(Y - other.Y) < epsilon)
		{
			return Math.Abs(Z - other.Z) < epsilon;
		}
		return false;
	}

	public bool Equals(Vec3d other)
	{
		if (other != null && X == other.X && Y == other.Y)
		{
			return Z == other.Z;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is Vec3d other)
		{
			if (other != null && X == other.X && Y == other.Y)
			{
				return Z == other.Z;
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
