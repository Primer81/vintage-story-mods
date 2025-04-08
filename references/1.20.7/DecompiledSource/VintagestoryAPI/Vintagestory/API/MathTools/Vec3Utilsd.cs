using System;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Don't use this class unless you need it to interoperate with Mat4d
/// </summary>
public class Vec3Utilsd
{
	/// Creates a new, empty vec3
	/// Returns {vec3} a new 3D vector.
	public static double[] Create()
	{
		return new double[3] { 0.0, 0.0, 0.0 };
	}

	/// <summary>
	/// Creates a new vec3 initialized with values from an existing vector. Returns {vec3} a new 3D vector
	/// </summary>
	/// <param name="a">vector to clone</param>
	/// <returns></returns>
	public static double[] CloneIt(double[] a)
	{
		return new double[3]
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
	public static double[] FromValues(double x, double y, double z)
	{
		return new double[3] { x, y, z };
	}

	/// <summary>
	/// Copy the values from one vec3 to another. Returns {vec3} out
	/// </summary>
	/// <param name="output">the receiving vector</param>
	/// <param name="a">the source vector</param>
	/// <returns></returns>
	public static double[] Copy(double[] output, double[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		return output;
	}

	/// <summary>
	/// Set the components of a vec3 to the given values. Returns {vec3} out
	/// </summary>
	/// <param name="output">the receiving vector</param>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public static double[] Set(double[] output, double x, double y, double z)
	{
		output[0] = x;
		output[1] = y;
		output[2] = z;
		return output;
	}

	/// <summary>
	/// Adds two vec3's. returns {vec3} out
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static double[] Add(double[] output, double[] a, double[] b)
	{
		output[0] = a[0] + b[0];
		output[1] = a[1] + b[1];
		output[2] = a[2] + b[2];
		return output;
	}

	/// <summary>
	/// Subtracts vector b from vector a. Returns {vec3} out
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a">the first operand</param>
	/// <param name="b">the second operand</param>
	/// <returns></returns>
	public static double[] Substract(double[] output, double[] a, double[] b)
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
	public static double[] Multiply(double[] output, double[] a, double[] b)
	{
		output[0] = a[0] * b[0];
		output[1] = a[1] * b[1];
		output[2] = a[2] * b[2];
		return output;
	}

	/// <summary>
	/// Divides two vec3's
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static double[] Divide(double[] output, double[] a, double[] b)
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
	public static double[] Min(double[] output, double[] a, double[] b)
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
	public static double[] Max(double[] output, double[] a, double[] b)
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
	public static double[] Scale(double[] output, double[] a, double b)
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
	public static double[] ScaleAndAdd(double[] output, double[] a, double[] b, double scale)
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
	public static double Distance(double[] a, double[] b)
	{
		double num = b[0] - a[0];
		double y = b[1] - a[1];
		double z = b[2] - a[2];
		return GameMath.Sqrt(num * num + y * y + z * z);
	}

	/// <summary>
	/// Calculates the squared euclidian distance between two vec3's. Returns {Number} squared distance between a and b
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static double SquaredDistance(double[] a, double[] b)
	{
		double num = b[0] - a[0];
		double y = b[1] - a[1];
		double z = b[2] - a[2];
		return num * num + y * y + z * z;
	}

	/// <summary>
	/// Calculates the length of a vec3
	/// </summary>
	/// <param name="a"></param>
	/// <returns></returns>
	public static double Length_(double[] a)
	{
		double num = a[0];
		double y = a[1];
		double z = a[2];
		return GameMath.Sqrt(num * num + y * y + z * z);
	}

	/// <summary>
	/// Calculates the squared length of a vec3. Returns {Number} squared length of a
	/// </summary>
	/// <param name="a"></param>
	/// <returns></returns>
	public static double SquaredLength(double[] a)
	{
		double num = a[0];
		double y = a[1];
		double z = a[2];
		return num * num + y * y + z * z;
	}

	/// <summary>
	/// SquaredLength()
	/// </summary>
	/// <param name="a"></param>
	/// <returns></returns>
	public static double SqrLen(double[] a)
	{
		return SquaredLength(a);
	}

	/// <summary>
	/// Negates the components of a vec3
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <returns></returns>
	public static double[] Negate(double[] output, double[] a)
	{
		output[0] = 0.0 - a[0];
		output[1] = 0.0 - a[1];
		output[2] = 0.0 - a[2];
		return output;
	}

	/// <summary>
	/// Normalize a vec3
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <returns></returns>
	public static double[] Normalize(double[] output, double[] a)
	{
		double num = a[0];
		double y = a[1];
		double z = a[2];
		double len = num * num + y * y + z * z;
		if (len > 0.0)
		{
			len = 1.0 / (double)GameMath.Sqrt(len);
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
	public static double Dot(double[] a, double[] b)
	{
		return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
	}

	/// <summary>
	/// Computes the cross product of two vec3's
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	public static double[] Cross(double[] output, double[] a, double[] b)
	{
		double ax = a[0];
		double ay = a[1];
		double az = a[2];
		double bx = b[0];
		double by = b[1];
		double bz = b[2];
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
	public static double[] Lerp(double[] output, double[] a, double[] b, double t)
	{
		double ax = a[0];
		double ay = a[1];
		double az = a[2];
		output[0] = ax + t * (b[0] - ax);
		output[1] = ay + t * (b[1] - ay);
		output[2] = az + t * (b[2] - az);
		return output;
	}

	/// <summary>
	/// Transforms the vec3 with a mat4. 4th vector component is implicitly '1'
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a"></param>
	/// <param name="m"></param>
	/// <returns></returns>
	public static double[] TransformMat4(double[] output, double[] a, double[] m)
	{
		double x = a[0];
		double y = a[1];
		double z = a[2];
		output[0] = m[0] * x + m[4] * y + m[8] * z + m[12];
		output[1] = m[1] * x + m[5] * y + m[9] * z + m[13];
		output[2] = m[2] * x + m[6] * y + m[10] * z + m[14];
		return output;
	}

	/// <summary>
	/// Transforms the vec3 with a mat3.
	/// </summary>
	/// <param name="output"></param>
	/// <param name="a">the vector to transform</param>
	/// <param name="m">the 3x3 matrix to transform with</param>
	/// <returns></returns>
	public static double[] TransformMat3(double[] output, double[] a, double[] m)
	{
		double x = a[0];
		double y = a[1];
		double z = a[2];
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
	public static double[] TransformQuat(double[] output, double[] a, double[] q)
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
