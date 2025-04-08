using System;
using System.IO;
using Newtonsoft.Json;
using ProtoBuf;
using Vintagestory.API.Client;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Represents a vector of 3 floats. Go bug Tyron of you need more utility methods in this class.
/// </summary>
[JsonObject(MemberSerialization.OptIn)]
[ProtoContract]
public class Vec3f : IVec3, IEquatable<Vec3f>
{
	/// <summary>
	/// The X-Component of the vector
	/// </summary>
	[JsonProperty]
	[ProtoMember(1)]
	public float X;

	/// <summary>
	/// The Y-Component of the vector
	/// </summary>
	[JsonProperty]
	[ProtoMember(2)]
	public float Y;

	/// <summary>
	/// The Z-Component of the vector
	/// </summary>
	[JsonProperty]
	[ProtoMember(3)]
	public float Z;

	/// <summary>
	/// Create a new instance with x/y/z set to 0
	/// </summary>
	public static Vec3f Zero => new Vec3f();

	public static Vec3f Half => new Vec3f(0.5f, 0.5f, 0.5f);

	public static Vec3f One => new Vec3f(1f, 1f, 1f);

	/// <summary>
	/// Synonum for X
	/// </summary>
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

	/// <summary>
	/// Synonum for Y
	/// </summary>
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

	/// <summary>
	/// Synonum for Z
	/// </summary>
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

	/// <summary>
	/// Returns the n-th coordinate
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Creates a new vector with x/y/z = 0
	/// </summary>
	public Vec3f()
	{
	}

	/// <summary>
	/// Create a new vector with given coordinates
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	public Vec3f(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	/// <summary>
	/// Create a new vector with given coordinates
	/// </summary>
	/// <param name="vec"></param>
	public Vec3f(Vec4f vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
	}

	/// <summary>
	/// Create a new vector with given coordinates
	/// </summary>
	/// <param name="values"></param>
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

	/// <summary>
	/// Returns the length of this vector
	/// </summary>
	/// <returns></returns>
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

	/// <summary>
	/// Returns the dot product with given vector
	/// </summary>
	/// <param name="a"></param>
	/// <returns></returns>
	public float Dot(Vec3f a)
	{
		return X * a.X + Y * a.Y + Z * a.Z;
	}

	public float Dot(FastVec3f a)
	{
		return X * a.X + Y * a.Y + Z * a.Z;
	}

	/// <summary>
	/// Returns the dot product with given vector
	/// </summary>
	/// <param name="a"></param>
	/// <returns></returns>
	public float Dot(Vec3d a)
	{
		return (float)((double)X * a.X + (double)Y * a.Y + (double)Z * a.Z);
	}

	/// <summary>
	/// Returns the dot product with given vector
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
	public double Dot(float[] pos)
	{
		return X * pos[0] + Y * pos[1] + Z * pos[2];
	}

	/// <summary>
	/// Returns the dot product with given vector
	/// </summary>
	/// <param name="pos"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Creates the cross product from a and b and sets own values accordingly
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	public void Cross(Vec3f a, Vec3f b)
	{
		X = a.Y * b.Z - a.Z * b.Y;
		Y = a.Z * b.X - a.X * b.Z;
		Z = a.X * b.Y - a.Y * b.X;
	}

	/// <summary>
	/// Creates the cross product from a and b and sets own values accordingly
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	public void Cross(Vec3f a, Vec4f b)
	{
		X = a.Y * b.Z - a.Z * b.Y;
		Y = a.Z * b.X - a.X * b.Z;
		Z = a.X * b.Y - a.Y * b.X;
	}

	/// <summary>
	/// Adds given x/y/z coordinates to the vector
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public Vec3f Add(float x, float y, float z)
	{
		X += x;
		Y += y;
		Z += z;
		return this;
	}

	/// <summary>
	/// Adds given x/y/z coordinates to the vector
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	public Vec3f Add(Vec3f vec)
	{
		X += vec.X;
		Y += vec.Y;
		Z += vec.Z;
		return this;
	}

	/// <summary>
	/// Adds given x/y/z coordinates to the vector
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	public Vec3f Add(Vec3d vec)
	{
		X += (float)vec.X;
		Y += (float)vec.Y;
		Z += (float)vec.Z;
		return this;
	}

	/// <summary>
	/// Substracts given x/y/z coordinates to the vector
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	public Vec3f Sub(Vec3f vec)
	{
		X -= vec.X;
		Y -= vec.Y;
		Z -= vec.Z;
		return this;
	}

	/// <summary>
	/// Substracts given x/y/z coordinates to the vector
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
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

	/// <summary>
	/// Multiplies each coordinate with given multiplier
	/// </summary>
	/// <param name="multiplier"></param>
	/// <returns></returns>
	public Vec3f Mul(float multiplier)
	{
		X *= multiplier;
		Y *= multiplier;
		Z *= multiplier;
		return this;
	}

	/// <summary>
	/// Creates a copy of the vetor
	/// </summary>
	/// <returns></returns>
	public Vec3f Clone()
	{
		return new Vec3f(X, Y, Z);
	}

	/// <summary>
	/// Turns the vector into a unit vector with length 1, but only if length is non-zero
	/// </summary>
	/// <returns></returns>
	public Vec3f Normalize()
	{
		float length = Length();
		if (length > 0f)
		{
			X /= length;
			Y /= length;
			Z /= length;
		}
		return this;
	}

	/// <summary>
	/// Calculates the square distance the two endpoints
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public double DistanceSq(double x, double y, double z)
	{
		return ((double)X - x) * ((double)X - x) + ((double)Y - y) * ((double)Y - y) + ((double)Z - z) * ((double)Z - z);
	}

	/// <summary>
	/// Calculates the distance the two endpoints
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	public float DistanceTo(Vec3d vec)
	{
		return (float)Math.Sqrt(((double)X - vec.X) * ((double)X - vec.X) + ((double)Y - vec.Y) * ((double)Y - vec.Y) + ((double)Z - vec.Z) * ((double)Z - vec.Z));
	}

	public float DistanceTo(Vec3f vec)
	{
		return (float)Math.Sqrt((X - vec.X) * (X - vec.X) + (Y - vec.Y) * (Y - vec.Y) + (Z - vec.Z) * (Z - vec.Z));
	}

	/// <summary>
	/// Adds given coordinates to a new vectors and returns it. The original calling vector remains unchanged
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public Vec3f AddCopy(float x, float y, float z)
	{
		return new Vec3f(X + x, Y + y, Z + z);
	}

	/// <summary>
	/// Adds both vectors into a new vector. Both source vectors remain unchanged.
	/// </summary>
	/// <param name="vec"></param>
	/// <returns></returns>
	public Vec3f AddCopy(Vec3f vec)
	{
		return new Vec3f(X + vec.X, Y + vec.Y, Z + vec.Z);
	}

	/// <summary>
	/// Substracts val from each coordinate if the coordinate if positive, otherwise it is added. If 0, the value is unchanged. The value must be a positive number
	/// </summary>
	/// <param name="val"></param>
	/// <returns></returns>
	public void ReduceBy(float val)
	{
		X = ((X > 0f) ? Math.Max(0f, X - val) : Math.Min(0f, X + val));
		Y = ((Y > 0f) ? Math.Max(0f, Y - val) : Math.Min(0f, Y + val));
		Z = ((Z > 0f) ? Math.Max(0f, Z - val) : Math.Min(0f, Z + val));
	}

	/// <summary>
	/// Creates a new vectors that is the normalized version of this vector. 
	/// </summary>
	/// <returns></returns>
	public Vec3f NormalizedCopy()
	{
		float length = Length();
		return new Vec3f(X / length, Y / length, Z / length);
	}

	/// <summary>
	/// Creates a new double precision vector with the same coordinates
	/// </summary>
	/// <returns></returns>
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

	/// <summary>
	/// Sets the vector to this coordinates
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	public Vec3f Set(float x, float y, float z)
	{
		X = x;
		Y = y;
		Z = z;
		return this;
	}

	/// <summary>
	/// Sets the vector to the coordinates of given vector
	/// </summary>
	/// <param name="vec"></param>
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

	/// <summary>
	/// Sets the vector to the coordinates of given vector
	/// </summary>
	/// <param name="vec"></param>
	public Vec3f Set(Vec3f vec)
	{
		X = vec.X;
		Y = vec.Y;
		Z = vec.Z;
		return this;
	}

	/// <summary>
	/// Simple string represenation of the x/y/z components
	/// </summary>
	/// <returns></returns>
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
		if (obj is Vec3f other)
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
