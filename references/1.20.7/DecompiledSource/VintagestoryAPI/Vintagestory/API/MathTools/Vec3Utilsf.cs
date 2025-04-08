using System;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Don't use this class unless you need it to interoperate with Mat4d
/// </summary>
public class Vec3Utilsf
{
	/// Creates a new, empty vec3
	/// Returns {vec3} a new 3D vector.
	public static float[] Create()
	{
		return new float[3] { 0f, 0f, 0f };
	}

	/// <summary>
	/// Creates a new vec3 initialized with values from an existing vector. Returns {vec3} a new 3D vector
	/// </summary>
	/// <param name="a"></param>
	/// <returns></returns>
	public static float[] CloneIt(float[] a)
	{
		return new float[3]
		{
			a[0],
			a[1],
			a[2]
		};
	}

	/// <summary>
	/// Creates a new vec3 initialized with the given values. Returns {vec3} a new 3D vector
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public static float[] FromValues(float x, float y, float z)
	{
		return new float[3] { x, y, z };
	}

	/// <summary>
	/// Copy the values from one vec3 to another. Returns {vec3} out
	/// </summary>
	/// <param name="output">the receiving vector</param>
	/// <param name="a">the source vector</param>
	/// <returns></returns>
	public static float[] Copy(float[] output, float[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		return output;
	}

	/// <summary>
	/// Set the components of a vec3 to the given values
	/// </summary>
	/// <param name="output"></param>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public static float[] Set(float[] output, float x, float y, float z)
	{
		output[0] = x;
		output[1] = y;
		output[2] = z;
		return output;
	}

	/// <summary>
	/// Adds two vec3's
	/// </summary>
	/// <param name="output">the receiving vector</param>
	/// <param name="a">the first operand</param>
	/// <param name="b">the second operand</param>
	/// <returns></returns>
	public static float[] Add(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] + b[0];
		output[1] = a[1] + b[1];
		output[2] = a[2] + b[2];
		return output;
	}

	/// <summary>
	/// Subtracts vector b from vector a
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static float[] Substract(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] - b[0];
		output[1] = a[1] - b[1];
		output[2] = a[2] - b[2];
		return output;
	}

	/// <summary>
	/// Multiplies two vec3's
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static float[] Multiply(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] * b[0];
		output[1] = a[1] * b[1];
		output[2] = a[2] * b[2];
		return output;
	}

	/// <summary>
	/// Alias of Mul()
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static float[] Mul(float[] output, float[] a, float[] b)
	{
		return Multiply(output, a, b);
	}

	/// <summary>
	/// Divides two vec3's
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static float[] Divide(float[] output, float[] a, float[] b)
	{
		output[0] = a[0] / b[0];
		output[1] = a[1] / b[1];
		output[2] = a[2] / b[2];
		return output;
	}

	/// <summary>
	/// Returns the minimum of two vec3's
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static float[] Min(float[] output, float[] a, float[] b)
	{
		output[0] = Math.Min(a[0], b[0]);
		output[1] = Math.Min(a[1], b[1]);
		output[2] = Math.Min(a[2], b[2]);
		return output;
	}

	/// <summary>
	/// Returns the maximum of two vec3's
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static float[] Max(float[] output, float[] a, float[] b)
	{
		output[0] = Math.Max(a[0], b[0]);
		output[1] = Math.Max(a[1], b[1]);
		output[2] = Math.Max(a[2], b[2]);
		return output;
	}

	/// <summary>
	/// Scales a vec3 by a scalar number
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static float[] Scale(float[] output, float[] a, float b)
	{
		output[0] = a[0] * b;
		output[1] = a[1] * b;
		output[2] = a[2] * b;
		return output;
	}

	/// <summary>
	/// Adds two vec3's after scaling the second operand by a scalar value
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <param name="scale"></param>
	/// <returns></returns>
	public static float[] ScaleAndAdd(float[] output, float[] a, float[] b, float scale)
	{
		output[0] = a[0] + b[0] * scale;
		output[1] = a[1] + b[1] * scale;
		output[2] = a[2] + b[2] * scale;
		return output;
	}

	/// <summary>
	/// Calculates the euclidian distance between two vec3's. Returns {Number} distance between a and b
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static float Distance(float[] a, float[] b)
	{
		float num = b[0] - a[0];
		float y = b[1] - a[1];
		float z = b[2] - a[2];
		return GameMath.Sqrt(num * num + y * y + z * z);
	}

	/// <summary>
	/// Calculates the squared euclidian distance between two vec3's. Returns {Number} squared distance between a and b
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static float SquaredDistance(float[] a, float[] b)
	{
		float num = b[0] - a[0];
		float y = b[1] - a[1];
		float z = b[2] - a[2];
		return num * num + y * y + z * z;
	}

	/// <summary>
	/// Calculates the length of a vec3
	/// </summary>
	/// <param name="a"></param>
	/// <returns></returns>
	public static float Length_(float[] a)
	{
		float num = a[0];
		float y = a[1];
		float z = a[2];
		return GameMath.Sqrt(num * num + y * y + z * z);
	}

	/// <summary>
	/// Calculates the squared length of a vec3. Returns {Number} squared length of a
	/// </summary>
	/// <param name="a"></param>
	/// <returns></returns>
	public static float SquaredLength(float[] a)
	{
		float num = a[0];
		float y = a[1];
		float z = a[2];
		return num * num + y * y + z * z;
	}

	/// <summary>
	/// Negates the components of a vec3
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <returns></returns>
	public static float[] Negate(float[] output, float[] a)
	{
		output[0] = 0f - a[0];
		output[1] = 0f - a[1];
		output[2] = 0f - a[2];
		return output;
	}

	/// <summary>
	/// Normalize a vec3
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <returns></returns>
	public static float[] Normalize(float[] output, float[] a)
	{
		float num = a[0];
		float y = a[1];
		float z = a[2];
		float len = num * num + y * y + z * z;
		if (len > 0f)
		{
			len = 1f / GameMath.Sqrt(len);
			output[0] = a[0] * len;
			output[1] = a[1] * len;
			output[2] = a[2] * len;
		}
		return output;
	}

	/// <summary>
	/// Calculates the dot product of two vec3's. Returns {Number} dot product of a and b
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static float Dot(float[] a, float[] b)
	{
		return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
	}

	/// <summary>
	/// Computes the cross product of two vec3's. Returns {vec3} out
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static float[] Cross(float[] output, float[] a, float[] b)
	{
		float ax = a[0];
		float ay = a[1];
		float az = a[2];
		float bx = b[0];
		float by = b[1];
		float bz = b[2];
		output[0] = ay * bz - az * by;
		output[1] = az * bx - ax * bz;
		output[2] = ax * by - ay * bx;
		return output;
	}

	/// <summary>
	/// Performs a linear interpolation between two vec3's
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <param name="t"></param>
	/// <returns></returns>
	public static float[] Lerp(float[] output, float[] a, float[] b, float t)
	{
		float ax = a[0];
		float ay = a[1];
		float az = a[2];
		output[0] = ax + t * (b[0] - ax);
		output[1] = ay + t * (b[1] - ay);
		output[2] = az + t * (b[2] - az);
		return output;
	}

	/// <summary>
	/// Transforms the vec3 with a mat4. 4th vector component is implicitly '1'. Returns {vec3} out
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="m"></param>
	/// <returns></returns>
	public static float[] TransformMat4(float[] output, float[] a, float[] m)
	{
		float x = a[0];
		float y = a[1];
		float z = a[2];
		output[0] = m[0] * x + m[4] * y + m[8] * z + m[12];
		output[1] = m[1] * x + m[5] * y + m[9] * z + m[13];
		output[2] = m[2] * x + m[6] * y + m[10] * z + m[14];
		return output;
	}

	/// <summary>
	/// Transforms the vec3 with a mat3. Returns {vec3} out
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="m"></param>
	/// <returns></returns>
	public static float[] TransformMat3(float[] output, float[] a, float[] m)
	{
		float x = a[0];
		float y = a[1];
		float z = a[2];
		output[0] = x * m[0] + y * m[3] + z * m[6];
		output[1] = x * m[1] + y * m[4] + z * m[7];
		output[2] = x * m[2] + y * m[5] + z * m[8];
		return output;
	}

	/// <summary>
	/// Transforms the vec3 with a quat
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="q"></param>
	/// <returns></returns>
	public static float[] TransformQuat(float[] output, float[] a, float[] q)
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
