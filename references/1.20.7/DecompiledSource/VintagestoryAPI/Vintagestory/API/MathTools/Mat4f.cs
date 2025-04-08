using System;

namespace Vintagestory.API.MathTools;

/// <summary>
/// 4x4 Matrix Math
/// </summary>
public class Mat4f
{
	private class GlMatrixMathf
	{
		public static float Abs(float len)
		{
			if (len < 0f)
			{
				return 0f - len;
			}
			return len;
		}

		public static float GLMAT_EPSILON()
		{
			return 1f / 1000000f;
		}
	}

	/// <summary>
	/// Creates a new identity mat4
	/// 0 4 8  12
	/// 1 5 9  13
	/// 2 6 10 14
	/// 3 7 11 15
	/// </summary>
	/// <returns>{mat4} a new 4x4 matrix</returns>
	public static float[] Create()
	{
		return new float[16]
		{
			1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f,
			1f, 0f, 0f, 0f, 0f, 1f
		};
	}

	/// <summary>
	/// Creates a new mat4 initialized with values from an existing matrix
	/// </summary>
	/// <param name="a">a matrix to clone</param>
	/// <returns>{mat4} a new 4x4 matrix</returns>
	public static float[] CloneIt(float[] a)
	{
		return new float[16]
		{
			a[0],
			a[1],
			a[2],
			a[3],
			a[4],
			a[5],
			a[6],
			a[7],
			a[8],
			a[9],
			a[10],
			a[11],
			a[12],
			a[13],
			a[14],
			a[15]
		};
	}

	/// <summary>
	/// Copy the values from one mat4 to another
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="a">{mat4} a the source matrix</param>
	/// <returns>{mat4} out</returns>
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
		output[9] = a[9];
		output[10] = a[10];
		output[11] = a[11];
		output[12] = a[12];
		output[13] = a[13];
		output[14] = a[14];
		output[15] = a[15];
		return output;
	}

	/// <summary>
	/// Set a mat4 to the identity matrix
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <returns>{mat4} out</returns>
	public static float[] Identity(float[] output)
	{
		output[0] = 1f;
		output[1] = 0f;
		output[2] = 0f;
		output[3] = 0f;
		output[4] = 0f;
		output[5] = 1f;
		output[6] = 0f;
		output[7] = 0f;
		output[8] = 0f;
		output[9] = 0f;
		output[10] = 1f;
		output[11] = 0f;
		output[12] = 0f;
		output[13] = 0f;
		output[14] = 0f;
		output[15] = 1f;
		return output;
	}

	/// <summary>
	/// Set a mat4 to the identity matrix with a scale applied
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="scale"></param>
	/// <returns>{mat4} out</returns>
	public static float[] Identity_Scaled(float[] output, float scale)
	{
		output[0] = scale;
		output[1] = 0f;
		output[2] = 0f;
		output[3] = 0f;
		output[4] = 0f;
		output[5] = scale;
		output[6] = 0f;
		output[7] = 0f;
		output[8] = 0f;
		output[9] = 0f;
		output[10] = scale;
		output[11] = 0f;
		output[12] = 0f;
		output[13] = 0f;
		output[14] = 0f;
		output[15] = 1f;
		return output;
	}

	/// <summary>
	/// Transpose the values of a mat4
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="a">{mat4} a the source matrix</param>
	/// <returns>{mat4} out</returns>
	public static float[] Transpose(float[] output, float[] a)
	{
		if (output == a)
		{
			float a2 = a[1];
			float a3 = a[2];
			float a4 = a[3];
			float a5 = a[6];
			float a6 = a[7];
			float a7 = a[11];
			output[1] = a[4];
			output[2] = a[8];
			output[3] = a[12];
			output[4] = a2;
			output[6] = a[9];
			output[7] = a[13];
			output[8] = a3;
			output[9] = a5;
			output[11] = a[14];
			output[12] = a4;
			output[13] = a6;
			output[14] = a7;
		}
		else
		{
			output[0] = a[0];
			output[1] = a[4];
			output[2] = a[8];
			output[3] = a[12];
			output[4] = a[1];
			output[5] = a[5];
			output[6] = a[9];
			output[7] = a[13];
			output[8] = a[2];
			output[9] = a[6];
			output[10] = a[10];
			output[11] = a[14];
			output[12] = a[3];
			output[13] = a[7];
			output[14] = a[11];
			output[15] = a[15];
		}
		return output;
	}

	/// <summary>
	/// Inverts a mat4
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="a">{mat4} a the source matrix</param>
	/// <returns>{mat4} out</returns>
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
		float a11 = a[9];
		float a12 = a[10];
		float a13 = a[11];
		float a14 = a[12];
		float a15 = a[13];
		float a16 = a[14];
		float a17 = a[15];
		float b0 = a2 * a7 - a3 * a6;
		float b = a2 * a8 - a4 * a6;
		float b2 = a2 * a9 - a5 * a6;
		float b3 = a3 * a8 - a4 * a7;
		float b4 = a3 * a9 - a5 * a7;
		float b5 = a4 * a9 - a5 * a8;
		float b6 = a10 * a15 - a11 * a14;
		float b7 = a10 * a16 - a12 * a14;
		float b8 = a10 * a17 - a13 * a14;
		float b9 = a11 * a16 - a12 * a15;
		float b10 = a11 * a17 - a13 * a15;
		float b11 = a12 * a17 - a13 * a16;
		float det = b0 * b11 - b * b10 + b2 * b9 + b3 * b8 - b4 * b7 + b5 * b6;
		if (det == 0f)
		{
			return null;
		}
		det = 1f / det;
		output[0] = (a7 * b11 - a8 * b10 + a9 * b9) * det;
		output[1] = (a4 * b10 - a3 * b11 - a5 * b9) * det;
		output[2] = (a15 * b5 - a16 * b4 + a17 * b3) * det;
		output[3] = (a12 * b4 - a11 * b5 - a13 * b3) * det;
		output[4] = (a8 * b8 - a6 * b11 - a9 * b7) * det;
		output[5] = (a2 * b11 - a4 * b8 + a5 * b7) * det;
		output[6] = (a16 * b2 - a14 * b5 - a17 * b) * det;
		output[7] = (a10 * b5 - a12 * b2 + a13 * b) * det;
		output[8] = (a6 * b10 - a7 * b8 + a9 * b6) * det;
		output[9] = (a3 * b8 - a2 * b10 - a5 * b6) * det;
		output[10] = (a14 * b4 - a15 * b2 + a17 * b0) * det;
		output[11] = (a11 * b2 - a10 * b4 - a13 * b0) * det;
		output[12] = (a7 * b7 - a6 * b9 - a8 * b6) * det;
		output[13] = (a2 * b9 - a3 * b7 + a4 * b6) * det;
		output[14] = (a15 * b - a14 * b3 - a16 * b0) * det;
		output[15] = (a10 * b3 - a11 * b + a12 * b0) * det;
		return output;
	}

	/// <summary>
	/// Calculates the adjugate of a mat4   
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="a">{mat4} a the source matrix</param>
	/// <returns>{mat4} out</returns>
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
		float a11 = a[9];
		float a12 = a[10];
		float a13 = a[11];
		float a14 = a[12];
		float a15 = a[13];
		float a16 = a[14];
		float a17 = a[15];
		output[0] = a7 * (a12 * a17 - a13 * a16) - a11 * (a8 * a17 - a9 * a16) + a15 * (a8 * a13 - a9 * a12);
		output[1] = 0f - (a3 * (a12 * a17 - a13 * a16) - a11 * (a4 * a17 - a5 * a16) + a15 * (a4 * a13 - a5 * a12));
		output[2] = a3 * (a8 * a17 - a9 * a16) - a7 * (a4 * a17 - a5 * a16) + a15 * (a4 * a9 - a5 * a8);
		output[3] = 0f - (a3 * (a8 * a13 - a9 * a12) - a7 * (a4 * a13 - a5 * a12) + a11 * (a4 * a9 - a5 * a8));
		output[4] = 0f - (a6 * (a12 * a17 - a13 * a16) - a10 * (a8 * a17 - a9 * a16) + a14 * (a8 * a13 - a9 * a12));
		output[5] = a2 * (a12 * a17 - a13 * a16) - a10 * (a4 * a17 - a5 * a16) + a14 * (a4 * a13 - a5 * a12);
		output[6] = 0f - (a2 * (a8 * a17 - a9 * a16) - a6 * (a4 * a17 - a5 * a16) + a14 * (a4 * a9 - a5 * a8));
		output[7] = a2 * (a8 * a13 - a9 * a12) - a6 * (a4 * a13 - a5 * a12) + a10 * (a4 * a9 - a5 * a8);
		output[8] = a6 * (a11 * a17 - a13 * a15) - a10 * (a7 * a17 - a9 * a15) + a14 * (a7 * a13 - a9 * a11);
		output[9] = 0f - (a2 * (a11 * a17 - a13 * a15) - a10 * (a3 * a17 - a5 * a15) + a14 * (a3 * a13 - a5 * a11));
		output[10] = a2 * (a7 * a17 - a9 * a15) - a6 * (a3 * a17 - a5 * a15) + a14 * (a3 * a9 - a5 * a7);
		output[11] = 0f - (a2 * (a7 * a13 - a9 * a11) - a6 * (a3 * a13 - a5 * a11) + a10 * (a3 * a9 - a5 * a7));
		output[12] = 0f - (a6 * (a11 * a16 - a12 * a15) - a10 * (a7 * a16 - a8 * a15) + a14 * (a7 * a12 - a8 * a11));
		output[13] = a2 * (a11 * a16 - a12 * a15) - a10 * (a3 * a16 - a4 * a15) + a14 * (a3 * a12 - a4 * a11);
		output[14] = 0f - (a2 * (a7 * a16 - a8 * a15) - a6 * (a3 * a16 - a4 * a15) + a14 * (a3 * a8 - a4 * a7));
		output[15] = a2 * (a7 * a12 - a8 * a11) - a6 * (a3 * a12 - a4 * a11) + a10 * (a3 * a8 - a4 * a7);
		return output;
	}

	/// <summary>
	/// Calculates the determinant of a mat4
	/// </summary>
	/// <param name="a">{mat4} a the source matrix</param>
	/// <returns>{Number} determinant of a</returns>
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
		float a10 = a[9];
		float a11 = a[10];
		float a12 = a[11];
		float a13 = a[12];
		float a14 = a[13];
		float a15 = a[14];
		float a16 = a[15];
		float b0 = num * a6 - a2 * a5;
		float b = num * a7 - a3 * a5;
		float b2 = num * a8 - a4 * a5;
		float b3 = a2 * a7 - a3 * a6;
		float b4 = a2 * a8 - a4 * a6;
		float b5 = a3 * a8 - a4 * a7;
		float b6 = a9 * a14 - a10 * a13;
		float b7 = a9 * a15 - a11 * a13;
		float b8 = a9 * a16 - a12 * a13;
		float b9 = a10 * a15 - a11 * a14;
		float b10 = a10 * a16 - a12 * a14;
		float b11 = a11 * a16 - a12 * a15;
		return b0 * b11 - b * b10 + b2 * b9 + b3 * b8 - b4 * b7 + b5 * b6;
	}

	/// <summary>
	/// Multiplies two mat4's
	///
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="a">{mat4} a the first operand</param>
	/// <param name="b">{mat4} b the second operand</param>
	/// <returns>{mat4} out</returns>
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
		float a11 = a[9];
		float a12 = a[10];
		float a13 = a[11];
		float a14 = a[12];
		float a15 = a[13];
		float a16 = a[14];
		float a17 = a[15];
		float b2 = b[0];
		float b3 = b[1];
		float b4 = b[2];
		float b5 = b[3];
		output[0] = b2 * a2 + b3 * a6 + b4 * a10 + b5 * a14;
		output[1] = b2 * a3 + b3 * a7 + b4 * a11 + b5 * a15;
		output[2] = b2 * a4 + b3 * a8 + b4 * a12 + b5 * a16;
		output[3] = b2 * a5 + b3 * a9 + b4 * a13 + b5 * a17;
		b2 = b[4];
		b3 = b[5];
		b4 = b[6];
		b5 = b[7];
		output[4] = b2 * a2 + b3 * a6 + b4 * a10 + b5 * a14;
		output[5] = b2 * a3 + b3 * a7 + b4 * a11 + b5 * a15;
		output[6] = b2 * a4 + b3 * a8 + b4 * a12 + b5 * a16;
		output[7] = b2 * a5 + b3 * a9 + b4 * a13 + b5 * a17;
		b2 = b[8];
		b3 = b[9];
		b4 = b[10];
		b5 = b[11];
		output[8] = b2 * a2 + b3 * a6 + b4 * a10 + b5 * a14;
		output[9] = b2 * a3 + b3 * a7 + b4 * a11 + b5 * a15;
		output[10] = b2 * a4 + b3 * a8 + b4 * a12 + b5 * a16;
		output[11] = b2 * a5 + b3 * a9 + b4 * a13 + b5 * a17;
		b2 = b[12];
		b3 = b[13];
		b4 = b[14];
		b5 = b[15];
		output[12] = b2 * a2 + b3 * a6 + b4 * a10 + b5 * a14;
		output[13] = b2 * a3 + b3 * a7 + b4 * a11 + b5 * a15;
		output[14] = b2 * a4 + b3 * a8 + b4 * a12 + b5 * a16;
		output[15] = b2 * a5 + b3 * a9 + b4 * a13 + b5 * a17;
		return output;
	}

	/// <summary>
	/// mat4.multiply
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
	/// Translate a mat4 by the given vector
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="input">{mat4} a the matrix to translate</param>
	/// <param name="x">{vec3} v vector to translate by</param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns>{mat4} out</returns>
	public static float[] Translate(float[] output, float[] input, float x, float y, float z)
	{
		if (input == output)
		{
			output[12] = input[0] * x + input[4] * y + input[8] * z + input[12];
			output[13] = input[1] * x + input[5] * y + input[9] * z + input[13];
			output[14] = input[2] * x + input[6] * y + input[10] * z + input[14];
			output[15] = input[3] * x + input[7] * y + input[11] * z + input[15];
		}
		else
		{
			float a0 = input[0];
			float a = input[1];
			float a2 = input[2];
			float a3 = input[3];
			float a4 = input[4];
			float a5 = input[5];
			float a6 = input[6];
			float a7 = input[7];
			float a8 = input[8];
			float a9 = input[9];
			float a10 = input[10];
			float a11 = input[11];
			output[0] = a0;
			output[1] = a;
			output[2] = a2;
			output[3] = a3;
			output[4] = a4;
			output[5] = a5;
			output[6] = a6;
			output[7] = a7;
			output[8] = a8;
			output[9] = a9;
			output[10] = a10;
			output[11] = a11;
			output[12] = a0 * x + a4 * y + a8 * z + input[12];
			output[13] = a * x + a5 * y + a9 * z + input[13];
			output[14] = a2 * x + a6 * y + a10 * z + input[14];
			output[15] = a3 * x + a7 * y + a11 * z + input[15];
		}
		return output;
	}

	/// <summary>
	/// Translate a mat4 by the given vector
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="input">{mat4} a the matrix to translate</param>
	/// <param name="translate">{vec3} v vector to translate by</param>
	/// <returns>{mat4} out</returns>
	public static float[] Translate(float[] output, float[] input, float[] translate)
	{
		float x = translate[0];
		float y = translate[1];
		float z = translate[2];
		if (input == output)
		{
			output[12] = input[0] * x + input[4] * y + input[8] * z + input[12];
			output[13] = input[1] * x + input[5] * y + input[9] * z + input[13];
			output[14] = input[2] * x + input[6] * y + input[10] * z + input[14];
			output[15] = input[3] * x + input[7] * y + input[11] * z + input[15];
		}
		else
		{
			float a0 = input[0];
			float a = input[1];
			float a2 = input[2];
			float a3 = input[3];
			float a4 = input[4];
			float a5 = input[5];
			float a6 = input[6];
			float a7 = input[7];
			float a8 = input[8];
			float a9 = input[9];
			float a10 = input[10];
			float a11 = input[11];
			output[0] = a0;
			output[1] = a;
			output[2] = a2;
			output[3] = a3;
			output[4] = a4;
			output[5] = a5;
			output[6] = a6;
			output[7] = a7;
			output[8] = a8;
			output[9] = a9;
			output[10] = a10;
			output[11] = a11;
			output[12] = a0 * x + a4 * y + a8 * z + input[12];
			output[13] = a * x + a5 * y + a9 * z + input[13];
			output[14] = a2 * x + a6 * y + a10 * z + input[14];
			output[15] = a3 * x + a7 * y + a11 * z + input[15];
		}
		return output;
	}

	/// <summary>
	/// Scales the mat4 by the dimensions in the given vec3
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="a">{mat4} a the matrix to scale</param>
	/// <param name="v">{vec3} v the vec3 to scale the matrix by</param>
	/// <returns>{mat4} out</returns>
	public static float[] Scale(float[] output, float[] a, float[] v)
	{
		float x = v[0];
		float y = v[1];
		float z = v[2];
		output[0] = a[0] * x;
		output[1] = a[1] * x;
		output[2] = a[2] * x;
		output[3] = a[3] * x;
		output[4] = a[4] * y;
		output[5] = a[5] * y;
		output[6] = a[6] * y;
		output[7] = a[7] * y;
		output[8] = a[8] * z;
		output[9] = a[9] * z;
		output[10] = a[10] * z;
		output[11] = a[11] * z;
		output[12] = a[12];
		output[13] = a[13];
		output[14] = a[14];
		output[15] = a[15];
		return output;
	}

	public static void SimpleScaleMatrix(Span<float> matrix, float x, float y, float z)
	{
		matrix.Clear();
		matrix[0] = x;
		matrix[5] = y;
		matrix[10] = z;
		matrix[15] = 1f;
	}

	/// <summary>
	/// Scales the mat4 by the dimensions in the given vec3
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="a">{mat4} a the matrix to scale</param>
	/// <param name="xScale"></param>
	/// <param name="yScale"></param>
	/// <param name="zScale"></param>
	/// <returns>{mat4} out</returns>
	public static float[] Scale(float[] output, float[] a, float xScale, float yScale, float zScale)
	{
		output[0] = a[0] * xScale;
		output[1] = a[1] * xScale;
		output[2] = a[2] * xScale;
		output[3] = a[3] * xScale;
		output[4] = a[4] * yScale;
		output[5] = a[5] * yScale;
		output[6] = a[6] * yScale;
		output[7] = a[7] * yScale;
		output[8] = a[8] * zScale;
		output[9] = a[9] * zScale;
		output[10] = a[10] * zScale;
		output[11] = a[11] * zScale;
		output[12] = a[12];
		output[13] = a[13];
		output[14] = a[14];
		output[15] = a[15];
		return output;
	}

	/// <summary>
	/// Rotates a mat4 by the given angle
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="a">{mat4} a the matrix to rotate</param>
	/// <param name="rad">{Number} rad the angle to rotate the matrix by</param>
	/// <param name="axis">{vec3} axis the axis to rotate around</param>
	/// <returns>{mat4} out</returns>
	public static float[] Rotate(float[] output, float[] a, float rad, float[] axis)
	{
		float x = axis[0];
		float y = axis[1];
		float z = axis[2];
		float len = GameMath.Sqrt(x * x + y * y + z * z);
		if (GlMatrixMathf.Abs(len) < GlMatrixMathf.GLMAT_EPSILON())
		{
			return null;
		}
		len = 1f / len;
		x *= len;
		y *= len;
		z *= len;
		float s = GameMath.Sin(rad);
		float c = GameMath.Cos(rad);
		float t = 1f - c;
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float a6 = a[4];
		float a7 = a[5];
		float a8 = a[6];
		float a9 = a[7];
		float a10 = a[8];
		float a11 = a[9];
		float a12 = a[10];
		float a13 = a[11];
		float b0 = x * x * t + c;
		float b = y * x * t + z * s;
		float b2 = z * x * t - y * s;
		float b3 = x * y * t - z * s;
		float b4 = y * y * t + c;
		float b5 = z * y * t + x * s;
		float b6 = x * z * t + y * s;
		float b7 = y * z * t - x * s;
		float b8 = z * z * t + c;
		output[0] = a2 * b0 + a6 * b + a10 * b2;
		output[1] = a3 * b0 + a7 * b + a11 * b2;
		output[2] = a4 * b0 + a8 * b + a12 * b2;
		output[3] = a5 * b0 + a9 * b + a13 * b2;
		output[4] = a2 * b3 + a6 * b4 + a10 * b5;
		output[5] = a3 * b3 + a7 * b4 + a11 * b5;
		output[6] = a4 * b3 + a8 * b4 + a12 * b5;
		output[7] = a5 * b3 + a9 * b4 + a13 * b5;
		output[8] = a2 * b6 + a6 * b7 + a10 * b8;
		output[9] = a3 * b6 + a7 * b7 + a11 * b8;
		output[10] = a4 * b6 + a8 * b7 + a12 * b8;
		output[11] = a5 * b6 + a9 * b7 + a13 * b8;
		if (a != output)
		{
			output[12] = a[12];
			output[13] = a[13];
			output[14] = a[14];
			output[15] = a[15];
		}
		return output;
	}

	/// <summary>
	/// Rotates a matrix by the given angle around the X axis
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="a">{mat4} a the matrix to rotate</param>
	/// <param name="rad">{Number} rad the angle to rotate the matrix by</param>
	/// <returns>{mat4} out</returns>
	public static float[] RotateX(float[] output, float[] a, float rad)
	{
		float s = GameMath.Sin(rad);
		float c = GameMath.Cos(rad);
		float a2 = a[4];
		float a3 = a[5];
		float a4 = a[6];
		float a5 = a[7];
		float a6 = a[8];
		float a7 = a[9];
		float a8 = a[10];
		float a9 = a[11];
		if (a != output)
		{
			output[0] = a[0];
			output[1] = a[1];
			output[2] = a[2];
			output[3] = a[3];
			output[12] = a[12];
			output[13] = a[13];
			output[14] = a[14];
			output[15] = a[15];
		}
		output[4] = a2 * c + a6 * s;
		output[5] = a3 * c + a7 * s;
		output[6] = a4 * c + a8 * s;
		output[7] = a5 * c + a9 * s;
		output[8] = a6 * c - a2 * s;
		output[9] = a7 * c - a3 * s;
		output[10] = a8 * c - a4 * s;
		output[11] = a9 * c - a5 * s;
		return output;
	}

	/// <summary>
	/// Rotates a matrix by the given angle around the Y axis
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="a">{mat4} a the matrix to rotate</param>
	/// <param name="rad">{Number} rad the angle to rotate the matrix by</param>
	/// <returns>{mat4} out</returns>
	public static float[] RotateY(float[] output, float[] a, float rad)
	{
		float s = GameMath.Sin(rad);
		float c = GameMath.Cos(rad);
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float a6 = a[8];
		float a7 = a[9];
		float a8 = a[10];
		float a9 = a[11];
		if (a != output)
		{
			output[4] = a[4];
			output[5] = a[5];
			output[6] = a[6];
			output[7] = a[7];
			output[12] = a[12];
			output[13] = a[13];
			output[14] = a[14];
			output[15] = a[15];
		}
		output[0] = a2 * c - a6 * s;
		output[1] = a3 * c - a7 * s;
		output[2] = a4 * c - a8 * s;
		output[3] = a5 * c - a9 * s;
		output[8] = a2 * s + a6 * c;
		output[9] = a3 * s + a7 * c;
		output[10] = a4 * s + a8 * c;
		output[11] = a5 * s + a9 * c;
		return output;
	}

	/// <summary>
	/// Rotates a matrix by the given angle around the Z axis
	/// </summary>
	/// <param name="output">{mat4} out the receiving matrix</param>
	/// <param name="a">{mat4} a the matrix to rotate</param>
	/// <param name="rad">{Number} rad the angle to rotate the matrix by</param>
	/// <returns>{mat4} out</returns>
	public static float[] RotateZ(float[] output, float[] a, float rad)
	{
		float s = GameMath.Sin(rad);
		float c = GameMath.Cos(rad);
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float a6 = a[4];
		float a7 = a[5];
		float a8 = a[6];
		float a9 = a[7];
		if (a != output)
		{
			output[8] = a[8];
			output[9] = a[9];
			output[10] = a[10];
			output[11] = a[11];
			output[12] = a[12];
			output[13] = a[13];
			output[14] = a[14];
			output[15] = a[15];
		}
		output[0] = a2 * c + a6 * s;
		output[1] = a3 * c + a7 * s;
		output[2] = a4 * c + a8 * s;
		output[3] = a5 * c + a9 * s;
		output[4] = a6 * c - a2 * s;
		output[5] = a7 * c - a3 * s;
		output[6] = a8 * c - a4 * s;
		output[7] = a9 * c - a5 * s;
		return output;
	}

	/// <summary>
	/// Provides a composite rotation matrix, equivalent to RotateX followed by RotateY followed by RotateZ
	/// <br />Here we work on a Span (which may be on the stack) for higher performance
	/// </summary>
	/// <param name="matrix"></param>
	/// <param name="radX"></param>
	/// <param name="radY"></param>
	/// <param name="radZ"></param>
	public static void RotateXYZ(Span<float> matrix, float radX, float radY, float radZ)
	{
		float sx = GameMath.Sin(radX);
		float cx = GameMath.Cos(radX);
		float sy = GameMath.Sin(radY);
		float cy = GameMath.Cos(radY);
		float sz = GameMath.Sin(radZ);
		float cz = GameMath.Cos(radZ);
		float a = sx * sy;
		float a2 = (0f - cx) * sy;
		matrix[0] = cy * cz;
		matrix[1] = a * cz + cx * sz;
		matrix[2] = a2 * cz + sx * sz;
		matrix[3] = 0f;
		matrix[4] = (0f - cy) * sz;
		matrix[5] = cx * cz - a * sz;
		matrix[6] = sx * cz - a2 * sz;
		matrix[7] = 0f;
		matrix[8] = sy;
		matrix[9] = (0f - sx) * cy;
		matrix[10] = cx * cy;
		matrix[11] = 0f;
		matrix[12] = 0f;
		matrix[13] = 0f;
		matrix[14] = 0f;
		matrix[15] = 1f;
	}

	/// <summary>
	/// Creates a matrix from a quaternion rotation and vector translation
	/// This is equivalent to (but much faster than):
	///     mat4.identity(dest);
	///     mat4.translate(dest, vec);
	///     var quatMat = mat4.create();
	///     quat4.toMat4(quat, quatMat);
	///     mat4.multiply(dest, quatMat);
	/// </summary>
	/// <param name="output">{mat4} out mat4 receiving operation result</param>
	/// <param name="q">{quat4} q Rotation quaternion</param>
	/// <param name="v">{vec3} v Translation vector</param>
	/// <returns>{mat4} out</returns>
	public static float[] FromRotationTranslation(float[] output, float[] q, float[] v)
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
		output[1] = xy + wz;
		output[2] = xz - wy;
		output[3] = 0f;
		output[4] = xy - wz;
		output[5] = 1f - (xx + zz);
		output[6] = yz + wx;
		output[7] = 0f;
		output[8] = xz + wy;
		output[9] = yz - wx;
		output[10] = 1f - (xx + yy);
		output[11] = 0f;
		output[12] = v[0];
		output[13] = v[1];
		output[14] = v[2];
		output[15] = 1f;
		return output;
	}

	/// <summary>
	/// Calculates a 4x4 matrix from the given quaternion
	/// </summary>
	/// <param name="output">{mat4} out mat4 receiving operation result</param>
	/// <param name="q">{quat} q Quaternion to create matrix from</param>
	/// <returns>{mat4} out</returns>
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
		output[1] = xy + wz;
		output[2] = xz - wy;
		output[3] = 0f;
		output[4] = xy - wz;
		output[5] = 1f - (xx + zz);
		output[6] = yz + wx;
		output[7] = 0f;
		output[8] = xz + wy;
		output[9] = yz - wx;
		output[10] = 1f - (xx + yy);
		output[11] = 0f;
		output[12] = 0f;
		output[13] = 0f;
		output[14] = 0f;
		output[15] = 1f;
		return output;
	}

	/// <summary>
	/// Generates a frustum matrix with the given bounds
	/// </summary>
	/// <param name="output">{mat4} out mat4 frustum matrix will be written into</param>
	/// <param name="left">{Number} left Left bound of the frustum</param>
	/// <param name="right">{Number} right Right bound of the frustum</param>
	/// <param name="bottom">{Number} bottom Bottom bound of the frustum</param>
	/// <param name="top">{Number} top Top bound of the frustum</param>
	/// <param name="near">{Number} near Near bound of the frustum</param>
	/// <param name="far">{Number} far Far bound of the frustum</param>
	/// <returns>{mat4} out</returns>
	public static float[] Frustum(float[] output, float left, float right, float bottom, float top, float near, float far)
	{
		float rl = 1f / (right - left);
		float tb = 1f / (top - bottom);
		float nf = 1f / (near - far);
		output[0] = near * 2f * rl;
		output[1] = 0f;
		output[2] = 0f;
		output[3] = 0f;
		output[4] = 0f;
		output[5] = near * 2f * tb;
		output[6] = 0f;
		output[7] = 0f;
		output[8] = (right + left) * rl;
		output[9] = (top + bottom) * tb;
		output[10] = (far + near) * nf;
		output[11] = -1f;
		output[12] = 0f;
		output[13] = 0f;
		output[14] = far * near * 2f * nf;
		output[15] = 0f;
		return output;
	}

	/// <summary>
	/// Generates a perspective projection matrix with the given bounds
	/// </summary>
	/// <param name="output">{mat4} out mat4 frustum matrix will be written into</param>
	/// <param name="fovy">{number} fovy Vertical field of view in radians</param>
	/// <param name="aspect">{number} aspect Aspect ratio. typically viewport width/height</param>
	/// <param name="near">{number} near Near bound of the frustum</param>
	/// <param name="far">{number} far Far bound of the frustum</param>
	/// <returns>{mat4} out</returns>
	public static float[] Perspective(float[] output, float fovy, float aspect, float near, float far)
	{
		float f = 1f / GameMath.Tan(fovy / 2f);
		float nf = 1f / (near - far);
		output[0] = f / aspect;
		output[1] = 0f;
		output[2] = 0f;
		output[3] = 0f;
		output[4] = 0f;
		output[5] = f;
		output[6] = 0f;
		output[7] = 0f;
		output[8] = 0f;
		output[9] = 0f;
		output[10] = (far + near) * nf;
		output[11] = -1f;
		output[12] = 0f;
		output[13] = 0f;
		output[14] = 2f * far * near * nf;
		output[15] = 0f;
		return output;
	}

	/// <summary>
	/// Generates a orthogonal projection matrix with the given bounds
	/// </summary>
	/// <param name="output">{mat4} out mat4 frustum matrix will be written into</param>
	/// <param name="left">{number} left Left bound of the frustum</param>
	/// <param name="right">{number} right Right bound of the frustum</param>
	/// <param name="bottom">{number} bottom Bottom bound of the frustum</param>
	/// <param name="top">{number} top Top bound of the frustum</param>
	/// <param name="near">{number} near Near bound of the frustum</param>
	/// <param name="far">{number} far Far bound of the frustum</param>
	/// <returns>{mat4} out</returns>
	public static float[] Ortho(float[] output, float left, float right, float bottom, float top, float near, float far)
	{
		float lr = 1f / (left - right);
		float bt = 1f / (bottom - top);
		float nf = 1f / (near - far);
		output[0] = -2f * lr;
		output[1] = 0f;
		output[2] = 0f;
		output[3] = 0f;
		output[4] = 0f;
		output[5] = -2f * bt;
		output[6] = 0f;
		output[7] = 0f;
		output[8] = 0f;
		output[9] = 0f;
		output[10] = 2f * nf;
		output[11] = 0f;
		output[12] = (left + right) * lr;
		output[13] = (top + bottom) * bt;
		output[14] = (far + near) * nf;
		output[15] = 1f;
		return output;
	}

	/// <summary>
	/// Generates a look-at matrix with the given eye position, focal point, and up axis
	/// </summary>
	/// <param name="output">{mat4} out mat4 frustum matrix will be written into</param>
	/// <param name="eye">{vec3} eye Position of the viewer</param>
	/// <param name="center">{vec3} center Point the viewer is looking at</param>
	/// <param name="up">{vec3} up vec3 pointing up</param>
	/// <returns>{mat4} out</returns>
	public static float[] LookAt(float[] output, float[] eye, float[] center, float[] up)
	{
		float eyex = eye[0];
		float eyey = eye[1];
		float eyez = eye[2];
		float upx = up[0];
		float upy = up[1];
		float upz = up[2];
		float centerx = center[0];
		float centery = center[1];
		float centerz = center[2];
		if (GlMatrixMathf.Abs(eyex - centerx) < GlMatrixMathf.GLMAT_EPSILON() && GlMatrixMathf.Abs(eyey - centery) < GlMatrixMathf.GLMAT_EPSILON() && GlMatrixMathf.Abs(eyez - centerz) < GlMatrixMathf.GLMAT_EPSILON())
		{
			return Identity(output);
		}
		float z0 = eyex - centerx;
		float z1 = eyey - centery;
		float z2 = eyez - centerz;
		float len = 1f / GameMath.Sqrt(z0 * z0 + z1 * z1 + z2 * z2);
		z0 *= len;
		z1 *= len;
		z2 *= len;
		float x0 = upy * z2 - upz * z1;
		float x1 = upz * z0 - upx * z2;
		float x2 = upx * z1 - upy * z0;
		len = GameMath.Sqrt(x0 * x0 + x1 * x1 + x2 * x2);
		if (len == 0f)
		{
			x0 = 0f;
			x1 = 0f;
			x2 = 0f;
		}
		else
		{
			len = 1f / len;
			x0 *= len;
			x1 *= len;
			x2 *= len;
		}
		float y0 = z1 * x2 - z2 * x1;
		float y1 = z2 * x0 - z0 * x2;
		float y2 = z0 * x1 - z1 * x0;
		len = GameMath.Sqrt(y0 * y0 + y1 * y1 + y2 * y2);
		if (len == 0f)
		{
			y0 = 0f;
			y1 = 0f;
			y2 = 0f;
		}
		else
		{
			len = 1f / len;
			y0 *= len;
			y1 *= len;
			y2 *= len;
		}
		output[0] = x0;
		output[1] = y0;
		output[2] = z0;
		output[3] = 0f;
		output[4] = x1;
		output[5] = y1;
		output[6] = z1;
		output[7] = 0f;
		output[8] = x2;
		output[9] = y2;
		output[10] = z2;
		output[11] = 0f;
		output[12] = 0f - (x0 * eyex + x1 * eyey + x2 * eyez);
		output[13] = 0f - (y0 * eyex + y1 * eyey + y2 * eyez);
		output[14] = 0f - (z0 * eyex + z1 * eyey + z2 * eyez);
		output[15] = 1f;
		return output;
	}

	/// <summary>
	/// Multiply the matrix with a vec4. Reference: http://mathinsight.org/matrix_vector_multiplication
	/// Returns a new vec4 vector
	/// </summary>
	/// <param name="matrix"></param>
	/// <param name="vec4"></param>
	/// <returns></returns>
	public static float[] MulWithVec4(float[] matrix, float[] vec4)
	{
		float[] output = new float[4];
		for (int row = 0; row < 4; row++)
		{
			for (int col = 0; col < 4; col++)
			{
				output[row] += matrix[4 * col + row] * vec4[col];
			}
		}
		return output;
	}

	public static float[] MulWithVec4(float[] matrix, float v1, float v2, float v3, float v4)
	{
		float[] output = new float[4];
		for (int row = 0; row < 4; row++)
		{
			output[row] += matrix[row] * v1;
			output[row] += matrix[4 + row] * v2;
			output[row] += matrix[8 + row] * v3;
			output[row] += matrix[12 + row] * v4;
		}
		return output;
	}

	public static void MulWithVec4(float[] matrix, float[] vec, float[] output)
	{
		MulWithVec4((Span<float>)matrix, vec, output);
	}

	public static void MulWithVec4(Span<float> matrix, float[] vec, float[] output)
	{
		float vx = vec[0];
		float vy = vec[1];
		float vz = vec[2];
		float va = vec[3];
		output[0] = matrix[0] * vx + matrix[4] * vy + matrix[8] * vz + matrix[12] * va;
		output[1] = matrix[1] * vx + matrix[5] * vy + matrix[9] * vz + matrix[13] * va;
		output[2] = matrix[2] * vx + matrix[6] * vy + matrix[10] * vz + matrix[14] * va;
		output[3] = matrix[3] * vx + matrix[7] * vy + matrix[11] * vz + matrix[15] * va;
	}

	/// <summary>
	/// Used for vec3 representing a direction or normal - as a vec4 this would have the 4th element set to 0, so that applying a matrix transform with a translation would have *no* effect
	/// </summary>
	public static void MulWithVec3(float[] matrix, float[] vec, float[] output)
	{
		MulWithVec3((Span<float>)matrix, vec, output);
	}

	/// <summary>
	/// Used for vec3 representing a direction or normal - as a vec4 this would have the 4th element set to 0, so that applying a matrix transform with a translation would have *no* effect
	/// </summary>
	public static void MulWithVec3(Span<float> matrix, float[] vec, float[] output)
	{
		float x = vec[0];
		float y = vec[1];
		float z = vec[2];
		output[0] = matrix[0] * x + matrix[4] * y + matrix[8] * z;
		output[1] = matrix[1] * x + matrix[5] * y + matrix[9] * z;
		output[2] = matrix[2] * x + matrix[6] * y + matrix[10] * z;
	}

	/// <summary>
	/// Used for vec3 representing an x,y,z position - as a vec4 this would have the 4th element set to 1, so that applying a matrix transform with a translation would have an effect
	/// The offset is used to index within the original and output arrays - e.g. in MeshData.xyz
	/// </summary>
	public static void MulWithVec3_Position(float[] matrix, float[] vec, float[] output, int offset)
	{
		MulWithVec3_Position((Span<float>)matrix, vec, output, offset);
	}

	public static void MulWithVec3_Position(Span<float> matrix, float[] vec, float[] output, int offset)
	{
		float x = vec[offset];
		float y = vec[offset + 1];
		float z = vec[offset + 2];
		output[offset] = matrix[0] * x + matrix[4] * y + matrix[8] * z + matrix[12];
		output[offset + 1] = matrix[1] * x + matrix[5] * y + matrix[9] * z + matrix[13];
		output[offset + 2] = matrix[2] * x + matrix[6] * y + matrix[10] * z + matrix[14];
	}

	public static void MulWithVec3_Position_AndScale(float[] matrix, float[] vec, float[] output, int offset, float scaleFactor)
	{
		float x = (vec[offset] - 0.5f) * scaleFactor + 0.5f;
		float y = vec[offset + 1] * scaleFactor;
		float z = (vec[offset + 2] - 0.5f) * scaleFactor + 0.5f;
		output[offset] = matrix[0] * x + matrix[4] * y + matrix[8] * z + matrix[12];
		output[offset + 1] = matrix[1] * x + matrix[5] * y + matrix[9] * z + matrix[13];
		output[offset + 2] = matrix[2] * x + matrix[6] * y + matrix[10] * z + matrix[14];
	}

	public static void MulWithVec3_Position_AndScaleXY(float[] matrix, float[] vec, float[] output, int offset, float scaleFactor)
	{
		float x = (vec[offset] - 0.5f) * scaleFactor + 0.5f;
		float y = vec[offset + 1];
		float z = (vec[offset + 2] - 0.5f) * scaleFactor + 0.5f;
		output[offset] = matrix[0] * x + matrix[4] * y + matrix[8] * z + matrix[12];
		output[offset + 1] = matrix[1] * x + matrix[5] * y + matrix[9] * z + matrix[13];
		output[offset + 2] = matrix[2] * x + matrix[6] * y + matrix[10] * z + matrix[14];
	}

	/// <summary>
	/// Used for vec3 representing an x,y,z position - as a vec4 this would have the 4th element set to 1, so that applying a matrix transform with a translation would have an effect
	/// The offset is used to index within the original and output arrays - e.g. in MeshData.xyz
	/// The origin is the origin for the rotation
	/// </summary>
	public static void MulWithVec3_Position_WithOrigin(float[] matrix, float[] vec, float[] output, int offset, Vec3f origin)
	{
		MulWithVec3_Position_WithOrigin((Span<float>)matrix, vec, output, offset, origin);
	}

	public static void MulWithVec3_Position_WithOrigin(Span<float> matrix, float[] vec, float[] output, int offset, Vec3f origin)
	{
		float vx = vec[offset] - origin.X;
		float vy = vec[offset + 1] - origin.Y;
		float vz = vec[offset + 2] - origin.Z;
		output[offset] = origin.X + matrix[0] * vx + matrix[4] * vy + matrix[8] * vz + matrix[12];
		output[offset + 1] = origin.Y + matrix[1] * vx + matrix[5] * vy + matrix[9] * vz + matrix[13];
		output[offset + 2] = origin.Z + matrix[2] * vx + matrix[6] * vy + matrix[10] * vz + matrix[14];
	}

	/// <summary>
	/// Used for vec3 representing an x,y,z position - as a vec4 this would have the 4th element set to 1, so that applying a matrix transform with a translation would have an effect
	/// </summary>
	public static void MulWithVec3_Position(float[] matrix, float x, float y, float z, Vec3f output)
	{
		output.X = matrix[0] * x + matrix[4] * y + matrix[8] * z + matrix[12];
		output.Y = matrix[1] * x + matrix[5] * y + matrix[9] * z + matrix[13];
		output.Z = matrix[2] * x + matrix[6] * y + matrix[10] * z + matrix[14];
	}

	/// <summary>
	/// Used for Vec3f representing a direction or normal - as a vec4 this would have the 4th element set to 0, so that applying a matrix transform with a translation would have *no* effect
	/// </summary>
	public static void MulWithVec3(float[] matrix, Vec3f vec, Vec3f output)
	{
		output.X = matrix[0] * vec.X + matrix[4] * vec.Y + matrix[8] * vec.Z;
		output.Y = matrix[1] * vec.X + matrix[5] * vec.Y + matrix[9] * vec.Z;
		output.Z = matrix[2] * vec.X + matrix[6] * vec.Y + matrix[10] * vec.Z;
	}

	/// <summary>
	/// Used for x,y,z representing a direction or normal - as a vec4 this would have the 4th element set to 0, so that applying a matrix transform with a translation would have *no* effect
	/// </summary>
	public static FastVec3f MulWithVec3(float[] matrix, float x, float y, float z)
	{
		float x2 = matrix[0] * x + matrix[4] * y + matrix[8] * z;
		float yOut = matrix[1] * x + matrix[5] * y + matrix[9] * z;
		float zOut = matrix[2] * x + matrix[6] * y + matrix[10] * z;
		return new FastVec3f(x2, yOut, zOut);
	}

	public static BlockFacing MulWithVec3_BlockFacing(float[] matrix, Vec3f vec)
	{
		return MulWithVec3_BlockFacing((Span<float>)matrix, vec);
	}

	public static BlockFacing MulWithVec3_BlockFacing(Span<float> matrix, Vec3f vec)
	{
		float num = matrix[0] * vec.X + matrix[4] * vec.Y + matrix[8] * vec.Z;
		float y = matrix[1] * vec.X + matrix[5] * vec.Y + matrix[9] * vec.Z;
		float z = matrix[2] * vec.X + matrix[6] * vec.Y + matrix[10] * vec.Z;
		return BlockFacing.FromVector(num, y, z);
	}

	public static double[] MulWithVec4(float[] matrix, double[] vec4)
	{
		double[] output = new double[4];
		for (int row = 0; row < 4; row++)
		{
			for (int col = 0; col < 4; col++)
			{
				output[row] += (double)matrix[4 * col + row] * vec4[col];
			}
		}
		return output;
	}

	/// <summary>
	/// Multiply the matrix with a vec4. Reference: http://mathinsight.org/matrix_vector_multiplication
	/// </summary>
	/// <param name="matrix"></param>
	/// <param name="vec4"></param>
	/// <param name="outVal"></param>
	/// <returns></returns>
	public static void MulWithVec4(float[] matrix, float[] vec4, Vec4f outVal)
	{
		outVal.Set(0f, 0f, 0f, 0f);
		for (int row = 0; row < 4; row++)
		{
			for (int col = 0; col < 4; col++)
			{
				outVal[row] += matrix[4 * col + row] * vec4[col];
			}
		}
	}

	/// <summary>
	/// Multiply the matrix with a vec4. Reference: http://mathinsight.org/matrix_vector_multiplication
	/// </summary>
	/// <param name="matrix"></param>
	/// <param name="inVal"></param>
	/// <param name="outVal"></param>
	public static void MulWithVec4(float[] matrix, Vec4d inVal, Vec4d outVal)
	{
		outVal.Set(0.0, 0.0, 0.0, 0.0);
		for (int row = 0; row < 4; row++)
		{
			for (int col = 0; col < 4; col++)
			{
				outVal[row] += (double)matrix[4 * col + row] * inVal[col];
			}
		}
	}

	/// <summary>
	/// Multiply the matrix with a vec4. Reference: http://mathinsight.org/matrix_vector_multiplication
	///
	/// </summary>
	/// <param name="matrix"></param>
	/// <param name="inVal"></param>
	/// <param name="outVal"></param>
	/// <returns></returns>
	public static void MulWithVec4(float[] matrix, Vec4f inVal, Vec4f outVal)
	{
		outVal.Set(0f, 0f, 0f, 0f);
		for (int row = 0; row < 4; row++)
		{
			for (int col = 0; col < 4; col++)
			{
				outVal[row] += matrix[4 * col + row] * inVal[col];
			}
		}
	}

	public static void ExtractEulerAngles(float[] m, ref float thetaX, ref float thetaY, ref float thetaZ)
	{
		float sinY = m[8];
		if (Math.Abs(sinY) == 1f)
		{
			thetaX = sinY * (float)Math.Atan2(m[1], m[5]);
			thetaY = sinY * ((float)Math.PI / 2f);
			thetaZ = 0f;
		}
		else
		{
			thetaX = (float)Math.Atan2(0f - m[9], m[10]);
			thetaY = GameMath.Asin(sinY);
			thetaZ = (float)Math.Atan2(0f - m[4], m[0]);
		}
	}
}
