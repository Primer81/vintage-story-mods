namespace Vintagestory.API.MathTools;

/// <summary>
/// 2x2 Matrix
/// </summary>
public class Mat22
{
	/// <summary>
	/// Creates a new identity mat2
	/// Returns a new 2x2 matrix
	/// </summary>
	/// <returns></returns>
	public static float[] Create()
	{
		return new float[4] { 1f, 0f, 0f, 1f };
	}

	/// <summary>
	/// Creates a new mat2 initialized with values from an existing matrix
	/// Returns a new 2x2 matrix
	/// </summary>
	/// <param name="a">matrix to clone</param>
	/// <returns></returns>
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

	/// <summary>
	/// Copy the values from one mat2 to another
	/// Returns output
	/// </summary>
	/// <param name="output">the receiving matrix</param>
	/// <param name="a">the source matrix</param>
	/// <returns></returns>
	public static float[] Copy(float[] output, float[] a)
	{
		output[0] = a[0];
		output[1] = a[1];
		output[2] = a[2];
		output[3] = a[3];
		return output;
	}

	/// <summary>
	/// Set a mat2 to the identity matrix
	/// Returns output
	/// </summary>
	/// <param name="output">the receiving matrix</param>
	/// <returns></returns>
	public static float[] Identity_(float[] output)
	{
		output[0] = 1f;
		output[1] = 0f;
		output[2] = 0f;
		output[3] = 1f;
		return output;
	}

	/// <summary>
	/// Transpose the values of a mat2
	/// Returns output
	/// </summary>
	/// <param name="output">the receiving matrix</param>
	/// <param name="a">the source matrix</param>
	/// <returns></returns>
	public static float[] Transpose(float[] output, float[] a)
	{
		output[0] = a[0];
		output[1] = a[2];
		output[2] = a[1];
		output[3] = a[3];
		return output;
	}

	/// <summary>
	/// Inverts a mat2
	/// Returns output
	/// </summary>
	/// <param name="output">the receiving matrix</param>
	/// <param name="a">the source matrix</param>
	/// <returns></returns>
	public static float[] Invert(float[] output, float[] a)
	{
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float det = a2 * a5 - a4 * a3;
		if (det == 0f)
		{
			return null;
		}
		det = 1f / det;
		output[0] = a5 * det;
		output[1] = (0f - a3) * det;
		output[2] = (0f - a4) * det;
		output[3] = a2 * det;
		return output;
	}

	/// <summary>
	/// Calculates the adjugate of a mat2
	/// Returns output
	/// </summary>
	/// <param name="output">the receiving matrix</param>
	/// <param name="a">the source matrix</param>
	/// <returns></returns>
	public static float[] Adjoint(float[] output, float[] a)
	{
		float a2 = a[0];
		output[0] = a[3];
		output[1] = 0f - a[1];
		output[2] = 0f - a[2];
		output[3] = a2;
		return output;
	}

	/// <summary>
	/// Calculates the determinant of a mat2
	/// Returns determinant of a
	/// </summary>
	/// <param name="a">the source matrix</param>
	/// <returns></returns>
	public static float Determinant(float[] a)
	{
		return a[0] * a[3] - a[2] * a[1];
	}

	/// <summary>
	/// Multiplies two mat2's
	/// Returns output
	/// </summary>
	/// <param name="output">the receiving matrix</param>
	/// <param name="a">the first operand</param>
	/// <param name="b">the second operand</param>
	/// <returns></returns>
	public static float[] Multiply(float[] output, float[] a, float[] b)
	{
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float b2 = b[0];
		float b3 = b[1];
		float b4 = b[2];
		float b5 = b[3];
		output[0] = a2 * b2 + a3 * b4;
		output[1] = a2 * b3 + a3 * b5;
		output[2] = a4 * b2 + a5 * b4;
		output[3] = a4 * b3 + a5 * b5;
		return output;
	}

	/// <summary>
	/// Alias for {@link mat2.multiply}
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
	/// Rotates a mat2 by the given angle
	/// Returns output
	/// </summary>
	/// <param name="output">the receiving matrix</param>
	/// <param name="a">the matrix to rotate</param>
	/// <param name="rad">the angle to rotate the matrix by</param>
	/// <returns></returns>
	public static float[] Rotate(float[] output, float[] a, float rad)
	{
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float s = GameMath.Sin(rad);
		float c = GameMath.Cos(rad);
		output[0] = a2 * c + a3 * s;
		output[1] = a2 * (0f - s) + a3 * c;
		output[2] = a4 * c + a5 * s;
		output[3] = a4 * (0f - s) + a5 * c;
		return output;
	}

	/// <summary>
	/// Scales the mat2 by the dimensions in the given vec2
	/// Returns output
	/// </summary>
	/// <param name="output">the receiving matrix</param>
	/// <param name="a">the matrix to rotate</param>
	/// <param name="v">the vec2 to scale the matrix by</param>
	/// <returns></returns>
	public static float[] Scale(float[] output, float[] a, float[] v)
	{
		float a2 = a[0];
		float a3 = a[1];
		float a4 = a[2];
		float a5 = a[3];
		float v2 = v[0];
		float v3 = v[1];
		output[0] = a2 * v2;
		output[1] = a3 * v3;
		output[2] = a4 * v2;
		output[3] = a5 * v3;
		return output;
	}
}
