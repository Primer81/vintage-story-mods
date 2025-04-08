namespace Vintagestory.API.MathTools;

public class Mat3d
{
	/// **
	public static double[] Create()
	{
		return new double[9] { 1.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0 };
	}

	/// **
	public static double[] FromMat4(double[] output, double[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		output[3] = a[4];
		output[4] = a[5];
		output[5] = a[6];
		output[6] = a[8];
		output[7] = a[9];
		output[8] = a[10];
		return output;
	}

	/// **
	public static double[] CloneIt(double[] a)
	{
		return new double[9]
		{
			a[0],
			a[1],
			a[2],
			a[3],
			a[4],
			a[5],
			a[6],
			a[7],
			a[8]
		};
	}

	/// **
	public static double[] Copy(double[] output, double[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		output[3] = a[3];
		output[4] = a[4];
		output[5] = a[5];
		output[6] = a[6];
		output[7] = a[7];
		output[8] = a[8];
		return output;
	}

	/// **
	public static double[] Identity_(double[] output)
	{
		output[0] = 1.0;
		output[1] = 0.0;
		output[2] = 0.0;
		output[3] = 0.0;
		output[4] = 1.0;
		output[5] = 0.0;
		output[6] = 0.0;
		output[7] = 0.0;
		output[8] = 1.0;
		return output;
	}

	/// **
	public static double[] Transpose(double[] output, double[] a)
	{
		if (output == a)
		{
			double a2 = a[1];
			double a3 = a[2];
			double a4 = a[5];
			output[1] = a[3];
			output[2] = a[6];
			output[3] = a2;
			output[5] = a[7];
			output[6] = a3;
			output[7] = a4;
		}
		else
		{
			output[0] = a[0];
			output[1] = a[3];
			output[2] = a[6];
			output[3] = a[1];
			output[4] = a[4];
			output[5] = a[7];
			output[6] = a[2];
			output[7] = a[5];
			output[8] = a[8];
		}
		return output;
	}

	/// **
	public static double[] Invert(double[] output, double[] a)
	{
		double a2 = a[0];
		double a3 = a[1];
		double a4 = a[2];
		double a5 = a[3];
		double a6 = a[4];
		double a7 = a[5];
		double a8 = a[6];
		double a9 = a[7];
		double a10 = a[8];
		double b = a10 * a6 - a7 * a9;
		double b2 = (0.0 - a10) * a5 + a7 * a8;
		double b3 = a9 * a5 - a6 * a8;
		double det = a2 * b + a3 * b2 + a4 * b3;
		if (det == 0.0)
		{
			return null;
		}
		det = 1.0 / det;
		output[0] = b * det;
		output[1] = ((0.0 - a10) * a3 + a4 * a9) * det;
		output[2] = (a7 * a3 - a4 * a6) * det;
		output[3] = b2 * det;
		output[4] = (a10 * a2 - a4 * a8) * det;
		output[5] = ((0.0 - a7) * a2 + a4 * a5) * det;
		output[6] = b3 * det;
		output[7] = ((0.0 - a9) * a2 + a3 * a8) * det;
		output[8] = (a6 * a2 - a3 * a5) * det;
		return output;
	}

	/// **
	public static double[] Adjoint(double[] output, double[] a)
	{
		double a2 = a[0];
		double a3 = a[1];
		double a4 = a[2];
		double a5 = a[3];
		double a6 = a[4];
		double a7 = a[5];
		double a8 = a[6];
		double a9 = a[7];
		double a10 = a[8];
		output[0] = a6 * a10 - a7 * a9;
		output[1] = a4 * a9 - a3 * a10;
		output[2] = a3 * a7 - a4 * a6;
		output[3] = a7 * a8 - a5 * a10;
		output[4] = a2 * a10 - a4 * a8;
		output[5] = a4 * a5 - a2 * a7;
		output[6] = a5 * a9 - a6 * a8;
		output[7] = a3 * a8 - a2 * a9;
		output[8] = a2 * a6 - a3 * a5;
		return output;
	}

	/// **
	public static double Determinant(double[] a)
	{
		double num = a[0];
		double a2 = a[1];
		double a3 = a[2];
		double a4 = a[3];
		double a5 = a[4];
		double a6 = a[5];
		double a7 = a[6];
		double a8 = a[7];
		double a9 = a[8];
		return num * (a9 * a5 - a6 * a8) + a2 * ((0.0 - a9) * a4 + a6 * a7) + a3 * (a8 * a4 - a5 * a7);
	}

	/// **
	public static double[] Multiply(double[] output, double[] a, double[] b)
	{
		double a2 = a[0];
		double a3 = a[1];
		double a4 = a[2];
		double a5 = a[3];
		double a6 = a[4];
		double a7 = a[5];
		double a8 = a[6];
		double a9 = a[7];
		double a10 = a[8];
		double b2 = b[0];
		double b3 = b[1];
		double b4 = b[2];
		double b5 = b[3];
		double b6 = b[4];
		double b7 = b[5];
		double b8 = b[6];
		double b9 = b[7];
		double b10 = b[8];
		output[0] = b2 * a2 + b3 * a5 + b4 * a8;
		output[1] = b2 * a3 + b3 * a6 + b4 * a9;
		output[2] = b2 * a4 + b3 * a7 + b4 * a10;
		output[3] = b5 * a2 + b6 * a5 + b7 * a8;
		output[4] = b5 * a3 + b6 * a6 + b7 * a9;
		output[5] = b5 * a4 + b6 * a7 + b7 * a10;
		output[6] = b8 * a2 + b9 * a5 + b10 * a8;
		output[7] = b8 * a3 + b9 * a6 + b10 * a9;
		output[8] = b8 * a4 + b9 * a7 + b10 * a10;
		return output;
	}

	/// **
	public static double[] Mul(double[] output, double[] a, double[] b)
	{
		return Multiply(output, a, b);
	}

	/// **
	public static double[] Translate(double[] output, double[] a, double[] v)
	{
		double a2 = a[0];
		double a3 = a[1];
		double a4 = a[2];
		double a5 = a[3];
		double a6 = a[4];
		double a7 = a[5];
		double a8 = a[6];
		double a9 = a[7];
		double a10 = a[8];
		double x = v[0];
		double y = v[1];
		output[0] = a2;
		output[1] = a3;
		output[2] = a4;
		output[3] = a5;
		output[4] = a6;
		output[5] = a7;
		output[6] = x * a2 + y * a5 + a8;
		output[7] = x * a3 + y * a6 + a9;
		output[8] = x * a4 + y * a7 + a10;
		return output;
	}

	/// **
	public static double[] Rotate(double[] output, double[] a, double rad)
	{
		double a2 = a[0];
		double a3 = a[1];
		double a4 = a[2];
		double a5 = a[3];
		double a6 = a[4];
		double a7 = a[5];
		double a8 = a[6];
		double a9 = a[7];
		double a10 = a[8];
		double s = GameMath.Sin(rad);
		double c = GameMath.Cos(rad);
		output[0] = c * a2 + s * a5;
		output[1] = c * a3 + s * a6;
		output[2] = c * a4 + s * a7;
		output[3] = c * a5 - s * a2;
		output[4] = c * a6 - s * a3;
		output[5] = c * a7 - s * a4;
		output[6] = a8;
		output[7] = a9;
		output[8] = a10;
		return output;
	}

	/// **
	public static double[] Scale(double[] output, double[] a, double[] v)
	{
		double x = v[0];
		double y = v[1];
		output[0] = x * a[0];
		output[1] = x * a[1];
		output[2] = x * a[2];
		output[3] = y * a[3];
		output[4] = y * a[4];
		output[5] = y * a[5];
		output[6] = a[6];
		output[7] = a[7];
		output[8] = a[8];
		return output;
	}

	/// **
	public static double[] FromMat2d(double[] output, double[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = 0.0;
		output[3] = a[2];
		output[4] = a[3];
		output[5] = 0.0;
		output[6] = a[4];
		output[7] = a[5];
		output[8] = 1.0;
		return output;
	}

	/// **
	public static double[] FromQuat(double[] output, double[] q)
	{
		double num = q[0];
		double y = q[1];
		double z = q[2];
		double w = q[3];
		double x2 = num + num;
		double y2 = y + y;
		double z2 = z + z;
		double xx = num * x2;
		double xy = num * y2;
		double xz = num * z2;
		double yy = y * y2;
		double yz = y * z2;
		double zz = z * z2;
		double wx = w * x2;
		double wy = w * y2;
		double wz = w * z2;
		output[0] = 1.0 - (yy + zz);
		output[3] = xy + wz;
		output[6] = xz - wy;
		output[1] = xy - wz;
		output[4] = 1.0 - (xx + zz);
		output[7] = yz + wx;
		output[2] = xz + wy;
		output[5] = yz - wx;
		output[8] = 1.0 - (xx + yy);
		return output;
	}

	/// **
	public static double[] NormalFromMat4(double[] output, double[] a)
	{
		double a2 = a[0];
		double a3 = a[1];
		double a4 = a[2];
		double a5 = a[3];
		double a6 = a[4];
		double a7 = a[5];
		double a8 = a[6];
		double a9 = a[7];
		double num = a[8];
		double a10 = a[9];
		double a11 = a[10];
		double a12 = a[11];
		double a13 = a[12];
		double a14 = a[13];
		double a15 = a[14];
		double a16 = a[15];
		double b0 = a2 * a7 - a3 * a6;
		double b = a2 * a8 - a4 * a6;
		double b2 = a2 * a9 - a5 * a6;
		double b3 = a3 * a8 - a4 * a7;
		double b4 = a3 * a9 - a5 * a7;
		double b5 = a4 * a9 - a5 * a8;
		double b6 = num * a14 - a10 * a13;
		double b7 = num * a15 - a11 * a13;
		double b8 = num * a16 - a12 * a13;
		double b9 = a10 * a15 - a11 * a14;
		double b10 = a10 * a16 - a12 * a14;
		double b11 = a11 * a16 - a12 * a15;
		double det = b0 * b11 - b * b10 + b2 * b9 + b3 * b8 - b4 * b7 + b5 * b6;
		if (det == 0.0)
		{
			return null;
		}
		det = 1.0 / det;
		output[0] = (a7 * b11 - a8 * b10 + a9 * b9) * det;
		output[1] = (a8 * b8 - a6 * b11 - a9 * b7) * det;
		output[2] = (a6 * b10 - a7 * b8 + a9 * b6) * det;
		output[3] = (a4 * b10 - a3 * b11 - a5 * b9) * det;
		output[4] = (a2 * b11 - a4 * b8 + a5 * b7) * det;
		output[5] = (a3 * b8 - a2 * b10 - a5 * b6) * det;
		output[6] = (a14 * b5 - a15 * b4 + a16 * b3) * det;
		output[7] = (a15 * b2 - a13 * b5 - a16 * b) * det;
		output[8] = (a13 * b4 - a14 * b2 + a16 * b0) * det;
		return output;
	}

	/// **
	private void f()
	{
	}
}
