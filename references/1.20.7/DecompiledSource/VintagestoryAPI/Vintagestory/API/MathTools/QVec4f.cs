using System;

namespace Vintagestory.API.MathTools;

internal class QVec4f
{
	/// **
	public static float[] Create()
	{
		return new float[4] { 0f, 0f, 0f, 0f };
	}

	/// **
	public static float[] CloneIt(float[] a)
	{
		return new float[4]
		{
			a[0],
			a[1],
			a[2],
			a[3]
		};
	}

	/// **
	public static float[] FromValues(float x, float y, float z, float w)
	{
		return new float[4] { x, y, z, w };
	}

	/// **
	public static float[] Copy(float[] output, float[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		output[3] = a[3];
		return output;
	}

	/// **
	public static float[] Set(float[] output, float x, float y, float z, float w)
	{
		output[0] = x;
		output[1] = y;
		output[2] = z;
		output[3] = w;
		return output;
	}

	/// **
	public static float[] Add(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] + b[0];
		output[1] = a[1] + b[1];
		output[2] = a[2] + b[2];
		output[3] = a[3] + b[3];
		return output;
	}

	/// **
	public static float[] Subtract(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] - b[0];
		output[1] = a[1] - b[1];
		output[2] = a[2] - b[2];
		output[3] = a[3] - b[3];
		return output;
	}

	/// **
	public static float[] Sub(float[] output, float[] a, float[] b)
	{
		return Subtract(output, a, b);
	}

	/// **
	public static float[] Multiply(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] * b[0];
		output[1] = a[1] * b[1];
		output[2] = a[2] * b[2];
		output[3] = a[3] * b[3];
		return output;
	}

	/// **
	public static float[] Mul(float[] output, float[] a, float[] b)
	{
		return Multiply(output, a, b);
	}

	/// **
	public static float[] Divide(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] / b[0];
		output[1] = a[1] / b[1];
		output[2] = a[2] / b[2];
		output[3] = a[3] / b[3];
		return output;
	}

	/// **
	public static float[] Div(float[] output, float[] a, float[] b)
	{
		return Divide(output, a, b);
	}

	/// **
	public static float[] Min(float[] output, float[] a, float[] b)
	{
		output[0] = Math.Min(a[0], b[0]);
		output[1] = Math.Min(a[1], b[1]);
		output[2] = Math.Min(a[2], b[2]);
		output[3] = Math.Min(a[3], b[3]);
		return output;
	}

	/// **
	public static float[] Max(float[] output, float[] a, float[] b)
	{
		output[0] = Math.Max(a[0], b[0]);
		output[1] = Math.Max(a[1], b[1]);
		output[2] = Math.Max(a[2], b[2]);
		output[3] = Math.Max(a[3], b[3]);
		return output;
	}

	/// **
	public static float[] Scale(float[] output, float[] a, float b)
	{
		output[0] = a[0] * b;
		output[1] = a[1] * b;
		output[2] = a[2] * b;
		output[3] = a[3] * b;
		return output;
	}

	/// **
	public static float[] ScaleAndAdd(float[] output, float[] a, float[] b, float scale)
	{
		output[0] = a[0] + b[0] * scale;
		output[1] = a[1] + b[1] * scale;
		output[2] = a[2] + b[2] * scale;
		output[3] = a[3] + b[3] * scale;
		return output;
	}

	/// **
	public static float Distance(float[] a, float[] b)
	{
		float num = b[0] - a[0];
		float y = b[1] - a[1];
		float z = b[2] - a[2];
		float w = b[3] - a[3];
		return GameMath.Sqrt(num * num + y * y + z * z + w * w);
	}

	/// **
	public static float Dist(float[] a, float[] b)
	{
		return Distance(a, b);
	}

	/// **
	public static float SquaredDistance(float[] a, float[] b)
	{
		float num = b[0] - a[0];
		float y = b[1] - a[1];
		float z = b[2] - a[2];
		float w = b[3] - a[3];
		return num * num + y * y + z * z + w * w;
	}

	/// **
	public static float SqrDist(float[] a, float[] b)
	{
		return SquaredDistance(a, b);
	}

	/// **
	public static float Length_(float[] a)
	{
		float num = a[0];
		float y = a[1];
		float z = a[2];
		float w = a[3];
		return GameMath.Sqrt(num * num + y * y + z * z + w * w);
	}

	/// **
	public static float Len(float[] a)
	{
		return Length_(a);
	}

	/// **
	public static float SquaredLength(float[] a)
	{
		float num = a[0];
		float y = a[1];
		float z = a[2];
		float w = a[3];
		return num * num + y * y + z * z + w * w;
	}

	/// **
	public static float SqrLen(float[] a)
	{
		return SquaredLength(a);
	}

	/// **
	public static float[] Negate(float[] output, float[] a)
	{
		output[0] = 0f - a[0];
		output[1] = 0f - a[1];
		output[2] = 0f - a[2];
		output[3] = 0f - a[3];
		return output;
	}

	/// **
	public static float[] Normalize(float[] output, float[] a)
	{
		float num = a[0];
		float y = a[1];
		float z = a[2];
		float w = a[3];
		float len = num * num + y * y + z * z + w * w;
		if (len > 0f)
		{
			len = 1f / GameMath.Sqrt(len);
			output[0] = a[0] * len;
			output[1] = a[1] * len;
			output[2] = a[2] * len;
			output[3] = a[3] * len;
		}
		return output;
	}

	/// **
	public static float Dot(float[] a, float[] b)
	{
		return a[0] * b[0] + a[1] * b[1] + a[2] * b[2] + a[3] * b[3];
	}

	/// **
	public static float[] Lerp(float[] output, float[] a, float[] b, float t)
	{
		float ax = a[0];
		float ay = a[1];
		float az = a[2];
		float aw = a[3];
		output[0] = ax + t * (b[0] - ax);
		output[1] = ay + t * (b[1] - ay);
		output[2] = az + t * (b[2] - az);
		output[3] = aw + t * (b[3] - aw);
		return output;
	}

	/// **
	public static float[] TransformMat4(float[] output, float[] a, float[] m)
	{
		float x = a[0];
		float y = a[1];
		float z = a[2];
		float w = a[3];
		output[0] = m[0] * x + m[4] * y + m[8] * z + m[12] * w;
		output[1] = m[1] * x + m[5] * y + m[9] * z + m[13] * w;
		output[2] = m[2] * x + m[6] * y + m[10] * z + m[14] * w;
		output[3] = m[3] * x + m[7] * y + m[11] * z + m[15] * w;
		return output;
	}

	/// **
	public static float[] transformQuat(float[] output, float[] a, float[] q)
	{
		float x = a[0];
		float y = a[1];
		float z = a[2];
		float qx = q[0];
		float qy = q[1];
		float qz = q[2];
		float qw = q[3];
		float ix = qw * x + qy * z - qz * y;
		float iy = qw * y + qz * x - qx * z;
		float iz = qw * z + qx * y - qy * x;
		float iw = (0f - qx) * x - qy * y - qz * z;
		output[0] = ix * qw + iw * (0f - qx) + iy * (0f - qz) - iz * (0f - qy);
		output[1] = iy * qw + iw * (0f - qy) + iz * (0f - qx) - ix * (0f - qz);
		output[2] = iz * qw + iw * (0f - qz) + ix * (0f - qy) - iy * (0f - qx);
		return output;
	}
}
