using System;

namespace Vintagestory.API.MathTools;

public class Quaterniond
{
	/// **
	public static double[] Create()
	{
		return new double[4] { 0.0, 0.0, 0.0, 1.0 };
	}

	/// **
	public static double[] RotationTo(double[] output, double[] a, double[] b)
	{
		double[] tmpvec3 = Vec3Utilsd.Create();
		double[] xUnitVec3 = Vec3Utilsd.FromValues(1.0, 0.0, 0.0);
		double[] yUnitVec3 = Vec3Utilsd.FromValues(0.0, 1.0, 0.0);
		double dot = Vec3Utilsd.Dot(a, b);
		double nines = 999999.0;
		nines /= 1000000.0;
		double epsilon = 1.0;
		epsilon /= 1000000.0;
		if (dot < 0.0 - nines)
		{
			Vec3Utilsd.Cross(tmpvec3, xUnitVec3, a);
			if (Vec3Utilsd.Length_(tmpvec3) < epsilon)
			{
				Vec3Utilsd.Cross(tmpvec3, yUnitVec3, a);
			}
			Vec3Utilsd.Normalize(tmpvec3, tmpvec3);
			SetAxisAngle(output, tmpvec3, 3.1415927410125732);
			return output;
		}
		if (dot > nines)
		{
			output[0] = 0.0;
			output[1] = 0.0;
			output[2] = 0.0;
			output[3] = 1.0;
			return output;
		}
		Vec3Utilsd.Cross(tmpvec3, a, b);
		output[0] = tmpvec3[0];
		output[1] = tmpvec3[1];
		output[2] = tmpvec3[2];
		output[3] = 1.0 + dot;
		return Normalize(output, output);
	}

	/// **
	public static double[] SetAxes(double[] output, double[] view, double[] right, double[] up)
	{
		double[] matr = Mat3d.Create();
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
	public static double[] CloneIt(double[] a)
	{
		return QVec4d.CloneIt(a);
	}

	/// **
	public static double[] FromValues(double x, double y, double z, double w)
	{
		return QVec4d.FromValues(x, y, z, w);
	}

	/// **
	public static double[] Copy(double[] output, double[] a)
	{
		return QVec4d.Copy(output, a);
	}

	/// **
	public static double[] Set(double[] output, double x, double y, double z, double w)
	{
		return QVec4d.Set(output, x, y, z, w);
	}

	/// **
	public static double[] Identity_(double[] output)
	{
		output[0] = 0.0;
		output[1] = 0.0;
		output[2] = 0.0;
		output[3] = 1.0;
		return output;
	}

	/// **
	public static double[] SetAxisAngle(double[] output, double[] axis, double rad)
	{
		rad /= 2.0;
		double s = GameMath.Sin(rad);
		output[0] = s * axis[0];
		output[1] = s * axis[1];
		output[2] = s * axis[2];
		output[3] = GameMath.Cos(rad);
		return output;
	}

	/// **
	public static double[] Add(double[] output, double[] a, double[] b)
	{
		return QVec4d.Add(output, a, b);
	}

	/// **
	public static double[] Multiply(double[] output, double[] a, double[] b)
	{
		double ax = a[0];
		double ay = a[1];
		double az = a[2];
		double aw = a[3];
		double bx = b[0];
		double by = b[1];
		double bz = b[2];
		double bw = b[3];
		output[0] = ax * bw + aw * bx + ay * bz - az * by;
		output[1] = ay * bw + aw * by + az * bx - ax * bz;
		output[2] = az * bw + aw * bz + ax * by - ay * bx;
		output[3] = aw * bw - ax * bx - ay * by - az * bz;
		return output;
	}

	/// **
	public static double[] Scale(double[] output, double[] a, double b)
	{
		return QVec4d.Scale(output, a, b);
	}

	/// **
	public static double[] RotateX(double[] output, double[] a, double rad)
	{
		rad /= 2.0;
		double ax = a[0];
		double ay = a[1];
		double az = a[2];
		double aw = a[3];
		double bx = GameMath.Sin(rad);
		double bw = GameMath.Cos(rad);
		output[0] = ax * bw + aw * bx;
		output[1] = ay * bw + az * bx;
		output[2] = az * bw - ay * bx;
		output[3] = aw * bw - ax * bx;
		return output;
	}

	/// **
	public static double[] RotateY(double[] output, double[] a, double rad)
	{
		rad /= 2.0;
		double ax = a[0];
		double ay = a[1];
		double az = a[2];
		double aw = a[3];
		double by = GameMath.Sin(rad);
		double bw = GameMath.Cos(rad);
		output[0] = ax * bw - az * by;
		output[1] = ay * bw + aw * by;
		output[2] = az * bw + ax * by;
		output[3] = aw * bw - ay * by;
		return output;
	}

	/// **
	public static double[] RotateZ(double[] output, double[] a, double rad)
	{
		rad /= 2.0;
		double ax = a[0];
		double ay = a[1];
		double az = a[2];
		double aw = a[3];
		double bz = GameMath.Sin(rad);
		double bw = GameMath.Cos(rad);
		output[0] = ax * bw + ay * bz;
		output[1] = ay * bw - ax * bz;
		output[2] = az * bw + aw * bz;
		output[3] = aw * bw - az * bz;
		return output;
	}

	/// **
	public static double[] CalculateW(double[] output, double[] a)
	{
		double x = a[0];
		double y = a[1];
		double z = a[2];
		output[0] = x;
		output[1] = y;
		output[2] = z;
		double one = 1.0;
		output[3] = 0f - GameMath.Sqrt(Math.Abs(one - x * x - y * y - z * z));
		return output;
	}

	/// **
	public static double Dot(double[] a, double[] b)
	{
		return QVec4d.Dot(a, b);
	}

	public static float[] ToEulerAngles(double[] quat)
	{
		float[] angles = new float[3];
		double sinr_cosp = 2.0 * (quat[3] * quat[0] + quat[1] * quat[2]);
		double cosr_cosp = 1.0 - 2.0 * (quat[0] * quat[0] + quat[1] * quat[1]);
		angles[2] = (float)Math.Atan2(sinr_cosp, cosr_cosp);
		double sinp = 2.0 * (quat[3] * quat[1] - quat[2] * quat[0]);
		if (Math.Abs(sinp) >= 1.0)
		{
			angles[1] = (float)Math.PI / 2f * (float)Math.Sign(sinp);
		}
		else
		{
			angles[1] = (float)Math.Asin(sinp);
		}
		double siny_cosp = 2.0 * (quat[3] * quat[2] + quat[0] * quat[1]);
		double cosy_cosp = 1.0 - 2.0 * (quat[1] * quat[1] + quat[2] * quat[2]);
		angles[0] = (float)Math.Atan2(siny_cosp, cosy_cosp);
		return angles;
	}

	/// **
	public static double[] Lerp(double[] output, double[] a, double[] b, double t)
	{
		return QVec4d.Lerp(output, a, b, t);
	}

	/// **
	public static double[] Slerp(double[] output, double[] a, double[] b, double t)
	{
		double ax = a[0];
		double ay = a[1];
		double az = a[2];
		double aw = a[3];
		double bx = b[0];
		double by = b[1];
		double bz = b[2];
		double bw = b[3];
		double cosom = ax * bx + ay * by + az * bz + aw * bw;
		if (cosom < 0.0)
		{
			cosom = 0.0 - cosom;
			bx = 0.0 - bx;
			by = 0.0 - by;
			bz = 0.0 - bz;
			bw = 0.0 - bw;
		}
		double one = 1.0;
		double epsilon = one / 1000000.0;
		double scale0;
		double scale1;
		if (one - cosom > epsilon)
		{
			double omega = GameMath.Acos(cosom);
			double sinom = GameMath.Sin(omega);
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
	public double[] Invert(double[] output, double[] a)
	{
		double a2 = a[0];
		double a3 = a[1];
		double a4 = a[2];
		double a5 = a[3];
		double dot = a2 * a2 + a3 * a3 + a4 * a4 + a5 * a5;
		double one = 1.0;
		double invDot = ((dot != 0.0) ? (one / dot) : 0.0);
		output[0] = (0.0 - a2) * invDot;
		output[1] = (0.0 - a3) * invDot;
		output[2] = (0.0 - a4) * invDot;
		output[3] = a5 * invDot;
		return output;
	}

	/// **
	public double[] Conjugate(double[] output, double[] a)
	{
		output[0] = 0.0 - a[0];
		output[1] = 0.0 - a[1];
		output[2] = 0.0 - a[2];
		output[3] = a[3];
		return output;
	}

	/// **
	public static double Length_(double[] a)
	{
		return QVec4d.Length_(a);
	}

	/// **
	public static double SquaredLength(double[] a)
	{
		return QVec4d.SquaredLength(a);
	}

	/// **
	public static double[] Normalize(double[] output, double[] a)
	{
		return QVec4d.Normalize(output, a);
	}

	/// **
	public static double[] FromMat3(double[] output, double[] m)
	{
		double fTrace = m[0] + m[4] + m[8];
		double zero = 0.0;
		double one = 1.0;
		double half = one / 2.0;
		if (fTrace > zero)
		{
			double fRoot = GameMath.Sqrt(fTrace + one);
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
			double fRoot = GameMath.Sqrt(m[i * 3 + i] - m[j * 3 + j] - m[k * 3 + k] + one);
			output[i] = half * fRoot;
			fRoot = half / fRoot;
			output[3] = (m[k * 3 + j] - m[j * 3 + k]) * fRoot;
			output[j] = (m[j * 3 + i] + m[i * 3 + j]) * fRoot;
			output[k] = (m[k * 3 + i] + m[i * 3 + k]) * fRoot;
		}
		return output;
	}
}
