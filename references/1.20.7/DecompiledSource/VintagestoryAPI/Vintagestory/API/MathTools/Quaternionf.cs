using System;

namespace Vintagestory.API.MathTools;

public class Quaternionf
{
	/// **
	public static float[] Create()
	{
		return new float[4] { 0f, 0f, 0f, 1f };
	}

	/// **
	public static float[] RotationTo(float[] output, float[] a, float[] b)
	{
		float[] tmpvec3 = Vec3Utilsf.Create();
		float[] xUnitVec3 = Vec3Utilsf.FromValues(1f, 0f, 0f);
		float[] yUnitVec3 = Vec3Utilsf.FromValues(0f, 1f, 0f);
		float dot = Vec3Utilsf.Dot(a, b);
		float nines = 999999f;
		nines /= 1000000f;
		float epsilon = 1f;
		epsilon /= 1000000f;
		if (dot < 0f - nines)
		{
			Vec3Utilsf.Cross(tmpvec3, xUnitVec3, a);
			if (Vec3Utilsf.Length_(tmpvec3) < epsilon)
			{
				Vec3Utilsf.Cross(tmpvec3, yUnitVec3, a);
			}
			Vec3Utilsf.Normalize(tmpvec3, tmpvec3);
			SetAxisAngle(output, tmpvec3, (float)Math.PI);
			return output;
		}
		if (dot > nines)
		{
			output[0] = 0f;
			output[1] = 0f;
			output[2] = 0f;
			output[3] = 1f;
			return output;
		}
		Vec3Utilsf.Cross(tmpvec3, a, b);
		output[0] = tmpvec3[0];
		output[1] = tmpvec3[1];
		output[2] = tmpvec3[2];
		output[3] = 1f + dot;
		return Normalize(output, output);
	}

	/// **
	public static float[] SetAxes(float[] output, float[] view, float[] right, float[] up)
	{
		float[] matr = Mat3f.Create();
		matr[0] = right[0];
		matr[3] = right[1];
		matr[6] = right[2];
		matr[1] = up[0];
		matr[4] = up[1];
		matr[7] = up[2];
		matr[2] = view[0];
		matr[5] = view[1];
		matr[8] = view[2];
		return Normalize(output, FromMat3(output, matr));
	}

	/// **
	public static float[] CloneIt(float[] a)
	{
		return QVec4f.CloneIt(a);
	}

	/// **
	public static float[] FromValues(float x, float y, float z, float w)
	{
		return QVec4f.FromValues(x, y, z, w);
	}

	/// **
	public static float[] Copy(float[] output, float[] a)
	{
		return QVec4f.Copy(output, a);
	}

	/// **
	public static float[] Set(float[] output, float x, float y, float z, float w)
	{
		return QVec4f.Set(output, x, y, z, w);
	}

	/// **
	public static float[] Identity_(float[] output)
	{
		output[0] = 0f;
		output[1] = 0f;
		output[2] = 0f;
		output[3] = 1f;
		return output;
	}

	/// **
	public static float[] SetAxisAngle(float[] output, float[] axis, float rad)
	{
		rad /= 2f;
		float s = GameMath.Sin(rad);
		output[0] = s * axis[0];
		output[1] = s * axis[1];
		output[2] = s * axis[2];
		output[3] = GameMath.Cos(rad);
		return output;
	}

	/// **
	public static float[] Add(float[] output, float[] a, float[] b)
	{
		return QVec4f.Add(output, a, b);
	}

	/// **
	public static float[] Multiply(float[] output, float[] a, float[] b)
	{
		float ax = a[0];
		float ay = a[1];
		float az = a[2];
		float aw = a[3];
		float bx = b[0];
		float by = b[1];
		float bz = b[2];
		float bw = b[3];
		output[0] = ax * bw + aw * bx + ay * bz - az * by;
		output[1] = ay * bw + aw * by + az * bx - ax * bz;
		output[2] = az * bw + aw * bz + ax * by - ay * bx;
		output[3] = aw * bw - ax * bx - ay * by - az * bz;
		return output;
	}

	/// **
	public static float[] Mul(float[] output, float[] a, float[] b)
	{
		return Multiply(output, a, b);
	}

	/// **
	public static float[] Scale(float[] output, float[] a, float b)
	{
		return QVec4f.Scale(output, a, b);
	}

	/// **
	public static float[] RotateX(float[] output, float[] a, float rad)
	{
		rad /= 2f;
		float ax = a[0];
		float ay = a[1];
		float az = a[2];
		float aw = a[3];
		float bx = GameMath.Sin(rad);
		float bw = GameMath.Cos(rad);
		output[0] = ax * bw + aw * bx;
		output[1] = ay * bw + az * bx;
		output[2] = az * bw - ay * bx;
		output[3] = aw * bw - ax * bx;
		return output;
	}

	/// **
	public static float[] RotateY(float[] output, float[] a, float rad)
	{
		rad /= 2f;
		float ax = a[0];
		float ay = a[1];
		float az = a[2];
		float aw = a[3];
		float by = GameMath.Sin(rad);
		float bw = GameMath.Cos(rad);
		output[0] = ax * bw - az * by;
		output[1] = ay * bw + aw * by;
		output[2] = az * bw + ax * by;
		output[3] = aw * bw - ay * by;
		return output;
	}

	/// **
	public static float[] RotateZ(float[] output, float[] a, float rad)
	{
		rad /= 2f;
		float ax = a[0];
		float ay = a[1];
		float az = a[2];
		float aw = a[3];
		float bz = GameMath.Sin(rad);
		float bw = GameMath.Cos(rad);
		output[0] = ax * bw + ay * bz;
		output[1] = ay * bw - ax * bz;
		output[2] = az * bw + aw * bz;
		output[3] = aw * bw - az * bz;
		return output;
	}

	/// **
	public static float[] CalculateW(float[] output, float[] a)
	{
		float x = a[0];
		float y = a[1];
		float z = a[2];
		output[0] = x;
		output[1] = y;
		output[2] = z;
		float one = 1f;
		output[3] = 0f - GameMath.Sqrt(Math.Abs(one - x * x - y * y - z * z));
		return output;
	}

	/// **
	public static float Dot(float[] a, float[] b)
	{
		return QVec4f.Dot(a, b);
	}

	public static float[] ToEulerAngles(float[] quat)
	{
		float[] angles = new float[3];
		float sinr_cosp = 2f * (quat[3] * quat[0] + quat[1] * quat[2]);
		float cosr_cosp = 1f - 2f * (quat[0] * quat[0] + quat[1] * quat[1]);
		angles[2] = (float)Math.Atan2(sinr_cosp, cosr_cosp);
		float sinp = 2f * (quat[3] * quat[1] - quat[2] * quat[0]);
		if (Math.Abs(sinp) >= 1f)
		{
			angles[1] = (float)Math.PI / 2f * (float)Math.Sign(sinp);
		}
		else
		{
			angles[1] = (float)Math.Asin(sinp);
		}
		float siny_cosp = 2f * (quat[3] * quat[2] + quat[0] * quat[1]);
		float cosy_cosp = 1f - 2f * (quat[1] * quat[1] + quat[2] * quat[2]);
		angles[0] = (float)Math.Atan2(siny_cosp, cosy_cosp);
		return angles;
	}

	/// **
	public static float[] Lerp(float[] output, float[] a, float[] b, float t)
	{
		return QVec4f.Lerp(output, a, b, t);
	}

	/// **
	public static float[] Slerp(float[] output, float[] a, float[] b, float t)
	{
		float ax = a[0];
		float ay = a[1];
		float az = a[2];
		float aw = a[3];
		float bx = b[0];
		float by = b[1];
		float bz = b[2];
		float bw = b[3];
		float cosom = ax * bx + ay * by + az * bz + aw * bw;
		if (cosom < 0f)
		{
			cosom = 0f - cosom;
			bx = 0f - bx;
			by = 0f - by;
			bz = 0f - bz;
			bw = 0f - bw;
		}
		float one = 1f;
		float epsilon = one / 1000000f;
		float scale0;
		float scale1;
		if (one - cosom > epsilon)
		{
			float omega = GameMath.Acos(cosom);
			float sinom = GameMath.Sin(omega);
			scale0 = GameMath.Sin((one - t) * omega) / sinom;
			scale1 = GameMath.Sin(t * omega) / sinom;
		}
		else
		{
			scale0 = one - t;
			scale1 = t;
		}
		output[0] = scale0 * ax + scale1 * bx;
		output[1] = scale0 * ay + scale1 * by;
		output[2] = scale0 * az + scale1 * bz;
		output[3] = scale0 * aw + scale1 * bw;
		return output;
	}

	/// **
	public float[] Invert(float[] output, float[] a)
	{
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float dot = a2 * a2 + a3 * a3 + a4 * a4 + a5 * a5;
		float one = 1f;
		float invDot = ((dot != 0f) ? (one / dot) : 0f);
		output[0] = (0f - a2) * invDot;
		output[1] = (0f - a3) * invDot;
		output[2] = (0f - a4) * invDot;
		output[3] = a5 * invDot;
		return output;
	}

	/// **
	public float[] Conjugate(float[] output, float[] a)
	{
		output[0] = 0f - a[0];
		output[1] = 0f - a[1];
		output[2] = 0f - a[2];
		output[3] = a[3];
		return output;
	}

	/// **
	public static float Length_(float[] a)
	{
		return QVec4f.Length_(a);
	}

	/// **
	public static float Len(float[] a)
	{
		return Length_(a);
	}

	/// **
	public static float SquaredLength(float[] a)
	{
		return QVec4f.SquaredLength(a);
	}

	/// **
	public static float SqrLen(float[] a)
	{
		return SquaredLength(a);
	}

	/// **
	public static float[] Normalize(float[] output, float[] a)
	{
		return QVec4f.Normalize(output, a);
	}

	/// **
	public static float[] FromMat3(float[] output, float[] m)
	{
		float fTrace = m[0] + m[4] + m[8];
		float zero = 0f;
		float one = 1f;
		float half = one / 2f;
		if (fTrace > zero)
		{
			float fRoot = GameMath.Sqrt(fTrace + one);
			output[3] = half * fRoot;
			fRoot = half / fRoot;
			output[0] = (m[7] - m[5]) * fRoot;
			output[1] = (m[2] - m[6]) * fRoot;
			output[2] = (m[3] - m[1]) * fRoot;
		}
		else
		{
			int i = 0;
			if (m[4] > m[0])
			{
				i = 1;
			}
			if (m[8] > m[i * 3 + i])
			{
				i = 2;
			}
			int j = (i + 1) % 3;
			int k = (i + 2) % 3;
			float fRoot = GameMath.Sqrt(m[i * 3 + i] - m[j * 3 + j] - m[k * 3 + k] + one);
			output[i] = half * fRoot;
			fRoot = half / fRoot;
			output[3] = (m[k * 3 + j] - m[j * 3 + k]) * fRoot;
			output[j] = (m[j * 3 + i] + m[i * 3 + j]) * fRoot;
			output[k] = (m[k * 3 + i] + m[i * 3 + k]) * fRoot;
		}
		return output;
	}
}
