using System;

namespace Vintagestory.API.MathTools;

internal class QVec4d
{
	/// **
	public static double[] Create()
	{
		return new double[4] { 0.0, 0.0, 0.0, 0.0 };
	}

	/// **
	public static double[] CloneIt(double[] a)
	{
		return new double[4]
		{
			a[0],
			a[1],
			a[2],
			a[3]
		};
	}

	/// **
	public static double[] FromValues(double x, double y, double z, double w)
	{
		return new double[4] { x, y, z, w };
	}

	/// **
	public static double[] Copy(double[] output, double[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		output[3] = a[3];
		return output;
	}

	/// **
	public static double[] Set(double[] output, double x, double y, double z, double w)
	{
		output[0] = x;
		output[1] = y;
		output[2] = z;
		output[3] = w;
		return output;
	}

	/// **
	public static double[] Add(double[] output, double[] a, double[] b)
	{
		output[0] = a[0] + b[0];
		output[1] = a[1] + b[1];
		output[2] = a[2] + b[2];
		output[3] = a[3] + b[3];
		return output;
	}

	/// **
	public static double[] Subtract(double[] output, double[] a, double[] b)
	{
		output[0] = a[0] - b[0];
		output[1] = a[1] - b[1];
		output[2] = a[2] - b[2];
		output[3] = a[3] - b[3];
		return output;
	}

	/// **
	public static double[] Multiply(double[] output, double[] a, double[] b)
	{
		output[0] = a[0] * b[0];
		output[1] = a[1] * b[1];
		output[2] = a[2] * b[2];
		output[3] = a[3] * b[3];
		return output;
	}

	/// **
	public static double[] Divide(double[] output, double[] a, double[] b)
	{
		output[0] = a[0] / b[0];
		output[1] = a[1] / b[1];
		output[2] = a[2] / b[2];
		output[3] = a[3] / b[3];
		return output;
	}

	/// **
	public static double[] Min(double[] output, double[] a, double[] b)
	{
		output[0] = Math.Min(a[0], b[0]);
		output[1] = Math.Min(a[1], b[1]);
		output[2] = Math.Min(a[2], b[2]);
		output[3] = Math.Min(a[3], b[3]);
		return output;
	}

	/// **
	public static double[] Max(double[] output, double[] a, double[] b)
	{
		output[0] = Math.Max(a[0], b[0]);
		output[1] = Math.Max(a[1], b[1]);
		output[2] = Math.Max(a[2], b[2]);
		output[3] = Math.Max(a[3], b[3]);
		return output;
	}

	/// **
	public static double[] Scale(double[] output, double[] a, double b)
	{
		output[0] = a[0] * b;
		output[1] = a[1] * b;
		output[2] = a[2] * b;
		output[3] = a[3] * b;
		return output;
	}

	/// **
	public static double[] ScaleAndAdd(double[] output, double[] a, double[] b, double scale)
	{
		output[0] = a[0] + b[0] * scale;
		output[1] = a[1] + b[1] * scale;
		output[2] = a[2] + b[2] * scale;
		output[3] = a[3] + b[3] * scale;
		return output;
	}

	/// **
	public static double Distance(double[] a, double[] b)
	{
		double num = b[0] - a[0];
		double y = b[1] - a[1];
		double z = b[2] - a[2];
		double w = b[3] - a[3];
		return GameMath.Sqrt(num * num + y * y + z * z + w * w);
	}

	/// **
	public static double SquaredDistance(double[] a, double[] b)
	{
		double num = b[0] - a[0];
		double y = b[1] - a[1];
		double z = b[2] - a[2];
		double w = b[3] - a[3];
		return num * num + y * y + z * z + w * w;
	}

	/// **
	public static double Length_(double[] a)
	{
		double num = a[0];
		double y = a[1];
		double z = a[2];
		double w = a[3];
		return GameMath.Sqrt(num * num + y * y + z * z + w * w);
	}

	/// **
	public static double SquaredLength(double[] a)
	{
		double num = a[0];
		double y = a[1];
		double z = a[2];
		double w = a[3];
		return num * num + y * y + z * z + w * w;
	}

	/// **
	public static double[] Negate(double[] output, double[] a)
	{
		output[0] = 0.0 - a[0];
		output[1] = 0.0 - a[1];
		output[2] = 0.0 - a[2];
		output[3] = 0.0 - a[3];
		return output;
	}

	/// **
	public static double[] Normalize(double[] output, double[] a)
	{
		double num = a[0];
		double y = a[1];
		double z = a[2];
		double w = a[3];
		double len = num * num + y * y + z * z + w * w;
		if (len > 0.0)
		{
			len = 1.0 / (double)GameMath.Sqrt(len);
			output[0] = a[0] * len;
			output[1] = a[1] * len;
			output[2] = a[2] * len;
			output[3] = a[3] * len;
		}
		return output;
	}

	/// **
	public static double Dot(double[] a, double[] b)
	{
		return a[0] * b[0] + a[1] * b[1] + a[2] * b[2] + a[3] * b[3];
	}

	/// **
	public static double[] Lerp(double[] output, double[] a, double[] b, double t)
	{
		double ax = a[0];
		double ay = a[1];
		double az = a[2];
		double aw = a[3];
		output[0] = ax + t * (b[0] - ax);
		output[1] = ay + t * (b[1] - ay);
		output[2] = az + t * (b[2] - az);
		output[3] = aw + t * (b[3] - aw);
		return output;
	}

	/// **
	public static double[] TransformMat4(double[] output, double[] a, double[] m)
	{
		double x = a[0];
		double y = a[1];
		double z = a[2];
		double w = a[3];
		output[0] = m[0] * x + m[4] * y + m[8] * z + m[12] * w;
		output[1] = m[1] * x + m[5] * y + m[9] * z + m[13] * w;
		output[2] = m[2] * x + m[6] * y + m[10] * z + m[14] * w;
		output[3] = m[3] * x + m[7] * y + m[11] * z + m[15] * w;
		return output;
	}

	/// **
	public static double[] transformQuat(double[] output, double[] a, double[] q)
	{
		double x = a[0];
		double y = a[1];
		double z = a[2];
		double qx = q[0];
		double qy = q[1];
		double qz = q[2];
		double qw = q[3];
		double ix = qw * x + qy * z - qz * y;
		double iy = qw * y + qz * x - qx * z;
		double iz = qw * z + qx * y - qy * x;
		double iw = (0.0 - qx) * x - qy * y - qz * z;
		output[0] = ix * qw + iw * (0.0 - qx) + iy * (0.0 - qz) - iz * (0.0 - qy);
		output[1] = iy * qw + iw * (0.0 - qy) + iz * (0.0 - qx) - ix * (0.0 - qz);
		output[2] = iz * qw + iw * (0.0 - qz) + ix * (0.0 - qy) - iy * (0.0 - qx);
		return output;
	}
}
