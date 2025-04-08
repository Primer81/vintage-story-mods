namespace Vintagestory.API.MathTools;

public class Mat3f
{
	/// **
	public static float[] Create()
	{
		return new float[9] { 1f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 1f };
	}

	/// **
	public static float[] FromMat4(float[] output, float[] a)
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
	public static float[] CloneIt(float[] a)
	{
		return new float[9]
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
	public static float[] Copy(float[] output, float[] a)
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
	public static float[] Identity_(float[] output)
	{
		output[0] = 1f;
		output[1] = 0f;
		output[2] = 0f;
		output[3] = 0f;
		output[4] = 1f;
		output[5] = 0f;
		output[6] = 0f;
		output[7] = 0f;
		output[8] = 1f;
		return output;
	}

	/// **
	public static float[] Transpose(float[] output, float[] a)
	{
		if (output == a)
		{
			float a2 = a[1];
			float a3 = a[2];
			float a4 = a[5];
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
	public static float[] Invert(float[] output, float[] a)
	{
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float a6 = a[4];
		float a7 = a[5];
		float a8 = a[6];
		float a9 = a[7];
		float a10 = a[8];
		float b = a10 * a6 - a7 * a9;
		float b2 = (0f - a10) * a5 + a7 * a8;
		float b3 = a9 * a5 - a6 * a8;
		float det = a2 * b + a3 * b2 + a4 * b3;
		if (det == 0f)
		{
			return null;
		}
		det = 1f / det;
		output[0] = b * det;
		output[1] = ((0f - a10) * a3 + a4 * a9) * det;
		output[2] = (a7 * a3 - a4 * a6) * det;
		output[3] = b2 * det;
		output[4] = (a10 * a2 - a4 * a8) * det;
		output[5] = ((0f - a7) * a2 + a4 * a5) * det;
		output[6] = b3 * det;
		output[7] = ((0f - a9) * a2 + a3 * a8) * det;
		output[8] = (a6 * a2 - a3 * a5) * det;
		return output;
	}

	/// **
	public static float[] Adjoint(float[] output, float[] a)
	{
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float a6 = a[4];
		float a7 = a[5];
		float a8 = a[6];
		float a9 = a[7];
		float a10 = a[8];
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
	public static float Determinant(float[] a)
	{
		float num = a[0];
		float a2 = a[1];
		float a3 = a[2];
		float a4 = a[3];
		float a5 = a[4];
		float a6 = a[5];
		float a7 = a[6];
		float a8 = a[7];
		float a9 = a[8];
		return num * (a9 * a5 - a6 * a8) + a2 * ((0f - a9) * a4 + a6 * a7) + a3 * (a8 * a4 - a5 * a7);
	}

	/// **
	public static float[] Multiply(float[] output, float[] a, float[] b)
	{
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float a6 = a[4];
		float a7 = a[5];
		float a8 = a[6];
		float a9 = a[7];
		float a10 = a[8];
		float b2 = b[0];
		float b3 = b[1];
		float b4 = b[2];
		float b5 = b[3];
		float b6 = b[4];
		float b7 = b[5];
		float b8 = b[6];
		float b9 = b[7];
		float b10 = b[8];
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
	public static float[] Mul(float[] output, float[] a, float[] b)
	{
		return Multiply(output, a, b);
	}

	/// **
	public static float[] Translate(float[] output, float[] a, float[] v)
	{
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float a6 = a[4];
		float a7 = a[5];
		float a8 = a[6];
		float a9 = a[7];
		float a10 = a[8];
		float x = v[0];
		float y = v[1];
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
	public static float[] Rotate(float[] output, float[] a, float rad)
	{
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float a6 = a[4];
		float a7 = a[5];
		float a8 = a[6];
		float a9 = a[7];
		float a10 = a[8];
		float s = GameMath.Sin(rad);
		float c = GameMath.Cos(rad);
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
	public static float[] Scale(float[] output, float[] a, float[] v)
	{
		float x = v[0];
		float y = v[1];
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
	public static float[] FromMat2d(float[] output, float[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = 0f;
		output[3] = a[2];
		output[4] = a[3];
		output[5] = 0f;
		output[6] = a[4];
		output[7] = a[5];
		output[8] = 1f;
		return output;
	}

	/// **
	public static float[] FromQuat(float[] output, float[] q)
	{
		float num = q[0];
		float y = q[1];
		float z = q[2];
		float w = q[3];
		float x2 = num + num;
		float y2 = y + y;
		float z2 = z + z;
		float xx = num * x2;
		float xy = num * y2;
		float xz = num * z2;
		float yy = y * y2;
		float yz = y * z2;
		float zz = z * z2;
		float wx = w * x2;
		float wy = w * y2;
		float wz = w * z2;
		output[0] = 1f - (yy + zz);
		output[3] = xy + wz;
		output[6] = xz - wy;
		output[1] = xy - wz;
		output[4] = 1f - (xx + zz);
		output[7] = yz + wx;
		output[2] = xz + wy;
		output[5] = yz - wx;
		output[8] = 1f - (xx + yy);
		return output;
	}

	/// **
	public static float[] NormalFromMat4(float[] output, float[] a)
	{
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float a6 = a[4];
		float a7 = a[5];
		float a8 = a[6];
		float a9 = a[7];
		float num = a[8];
		float a10 = a[9];
		float a11 = a[10];
		float a12 = a[11];
		float a13 = a[12];
		float a14 = a[13];
		float a15 = a[14];
		float a16 = a[15];
		float b0 = a2 * a7 - a3 * a6;
		float b = a2 * a8 - a4 * a6;
		float b2 = a2 * a9 - a5 * a6;
		float b3 = a3 * a8 - a4 * a7;
		float b4 = a3 * a9 - a5 * a7;
		float b5 = a4 * a9 - a5 * a8;
		float b6 = num * a14 - a10 * a13;
		float b7 = num * a15 - a11 * a13;
		float b8 = num * a16 - a12 * a13;
		float b9 = a10 * a15 - a11 * a14;
		float b10 = a10 * a16 - a12 * a14;
		float b11 = a11 * a16 - a12 * a15;
		float det = b0 * b11 - b * b10 + b2 * b9 + b3 * b8 - b4 * b7 + b5 * b6;
		if (det == 0f)
		{
			return null;
		}
		det = 1f / det;
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
