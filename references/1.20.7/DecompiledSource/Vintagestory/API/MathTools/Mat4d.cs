#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

namespace Vintagestory.API.MathTools;

//
// Summary:
//     4x4 Matrix Math
public class Mat4d
{
    private class GlMatrixMathd
    {
        public static double Abs(double len)
        {
            if (len < 0.0)
            {
                return 0.0 - len;
            }

            return len;
        }

        public static double GLMAT_EPSILON()
        {
            return 1.0 / 1000000.0;
        }
    }

    //
    // Summary:
    //     Creates a new identity mat4 0 4 8 12 1 5 9 13 2 6 10 14 3 7 11 15
    //
    // Returns:
    //     {mat4} a new 4x4 matrix
    public static double[] Create()
    {
        return new double[16]
        {
            1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0,
            1.0, 0.0, 0.0, 0.0, 0.0, 1.0
        };
    }

    public static float[] ToMat4f(float[] output, double[] input)
    {
        output[0] = (float)input[0];
        output[1] = (float)input[1];
        output[2] = (float)input[2];
        output[3] = (float)input[3];
        output[4] = (float)input[4];
        output[5] = (float)input[5];
        output[6] = (float)input[6];
        output[7] = (float)input[7];
        output[8] = (float)input[8];
        output[9] = (float)input[9];
        output[10] = (float)input[10];
        output[11] = (float)input[11];
        output[12] = (float)input[12];
        output[13] = (float)input[13];
        output[14] = (float)input[14];
        output[15] = (float)input[15];
        return output;
    }

    //
    // Summary:
    //     Creates a new mat4 initialized with values from an existing matrix
    //
    // Parameters:
    //   a:
    //     a matrix to clone
    //
    // Returns:
    //     {mat4} a new 4x4 matrix
    public static double[] CloneIt(double[] a)
    {
        return new double[16]
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

    //
    // Summary:
    //     Copy the values from one mat4 to another
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   a:
    //     {mat4} a the source matrix
    //
    // Returns:
    //     {mat4} out
    public static double[] Copy(double[] output, double[] a)
    {
        for (int i = 0; i < output.Length; i += 4)
        {
            output[i] = a[i];
            output[i + 1] = a[i + 1];
            output[i + 2] = a[i + 2];
            output[i + 3] = a[i + 3];
        }

        return output;
    }

    //
    // Summary:
    //     Set a mat4 to the identity matrix
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    // Returns:
    //     {mat4} out
    public static double[] Identity(double[] output)
    {
        output[0] = 1.0;
        output[1] = 0.0;
        output[2] = 0.0;
        output[3] = 0.0;
        output[4] = 0.0;
        output[5] = 1.0;
        output[6] = 0.0;
        output[7] = 0.0;
        output[8] = 0.0;
        output[9] = 0.0;
        output[10] = 1.0;
        output[11] = 0.0;
        output[12] = 0.0;
        output[13] = 0.0;
        output[14] = 0.0;
        output[15] = 1.0;
        return output;
    }

    //
    // Summary:
    //     Transpose the values of a mat4
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   a:
    //     {mat4} a the source matrix
    //
    // Returns:
    //     {mat4} out
    public static double[] Transpose(double[] output, double[] a)
    {
        if (output == a)
        {
            double num = a[1];
            double num2 = a[2];
            double num3 = a[3];
            double num4 = a[6];
            double num5 = a[7];
            double num6 = a[11];
            output[1] = a[4];
            output[2] = a[8];
            output[3] = a[12];
            output[4] = num;
            output[6] = a[9];
            output[7] = a[13];
            output[8] = num2;
            output[9] = num4;
            output[11] = a[14];
            output[12] = num3;
            output[13] = num5;
            output[14] = num6;
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

    //
    // Summary:
    //     Inverts a mat4
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   a:
    //     {mat4} a the source matrix
    //
    // Returns:
    //     {mat4} out
    public static double[] Invert(double[] output, double[] a)
    {
        double num = a[0];
        double num2 = a[1];
        double num3 = a[2];
        double num4 = a[3];
        double num5 = a[4];
        double num6 = a[5];
        double num7 = a[6];
        double num8 = a[7];
        double num9 = a[8];
        double num10 = a[9];
        double num11 = a[10];
        double num12 = a[11];
        double num13 = a[12];
        double num14 = a[13];
        double num15 = a[14];
        double num16 = a[15];
        double num17 = num * num6 - num2 * num5;
        double num18 = num * num7 - num3 * num5;
        double num19 = num * num8 - num4 * num5;
        double num20 = num2 * num7 - num3 * num6;
        double num21 = num2 * num8 - num4 * num6;
        double num22 = num3 * num8 - num4 * num7;
        double num23 = num9 * num14 - num10 * num13;
        double num24 = num9 * num15 - num11 * num13;
        double num25 = num9 * num16 - num12 * num13;
        double num26 = num10 * num15 - num11 * num14;
        double num27 = num10 * num16 - num12 * num14;
        double num28 = num11 * num16 - num12 * num15;
        double num29 = num17 * num28 - num18 * num27 + num19 * num26 + num20 * num25 - num21 * num24 + num22 * num23;
        if (num29 == 0.0)
        {
            return null;
        }

        num29 = 1.0 / num29;
        output[0] = (num6 * num28 - num7 * num27 + num8 * num26) * num29;
        output[1] = (num3 * num27 - num2 * num28 - num4 * num26) * num29;
        output[2] = (num14 * num22 - num15 * num21 + num16 * num20) * num29;
        output[3] = (num11 * num21 - num10 * num22 - num12 * num20) * num29;
        output[4] = (num7 * num25 - num5 * num28 - num8 * num24) * num29;
        output[5] = (num * num28 - num3 * num25 + num4 * num24) * num29;
        output[6] = (num15 * num19 - num13 * num22 - num16 * num18) * num29;
        output[7] = (num9 * num22 - num11 * num19 + num12 * num18) * num29;
        output[8] = (num5 * num27 - num6 * num25 + num8 * num23) * num29;
        output[9] = (num2 * num25 - num * num27 - num4 * num23) * num29;
        output[10] = (num13 * num21 - num14 * num19 + num16 * num17) * num29;
        output[11] = (num10 * num19 - num9 * num21 - num12 * num17) * num29;
        output[12] = (num6 * num24 - num5 * num26 - num7 * num23) * num29;
        output[13] = (num * num26 - num2 * num24 + num3 * num23) * num29;
        output[14] = (num14 * num18 - num13 * num20 - num15 * num17) * num29;
        output[15] = (num9 * num20 - num10 * num18 + num11 * num17) * num29;
        return output;
    }

    //
    // Summary:
    //     Calculates the adjugate of a mat4
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   a:
    //     {mat4} a the source matrix
    //
    // Returns:
    //     {mat4} out
    public static double[] Adjoint(double[] output, double[] a)
    {
        double num = a[0];
        double num2 = a[1];
        double num3 = a[2];
        double num4 = a[3];
        double num5 = a[4];
        double num6 = a[5];
        double num7 = a[6];
        double num8 = a[7];
        double num9 = a[8];
        double num10 = a[9];
        double num11 = a[10];
        double num12 = a[11];
        double num13 = a[12];
        double num14 = a[13];
        double num15 = a[14];
        double num16 = a[15];
        output[0] = num6 * (num11 * num16 - num12 * num15) - num10 * (num7 * num16 - num8 * num15) + num14 * (num7 * num12 - num8 * num11);
        output[1] = 0.0 - (num2 * (num11 * num16 - num12 * num15) - num10 * (num3 * num16 - num4 * num15) + num14 * (num3 * num12 - num4 * num11));
        output[2] = num2 * (num7 * num16 - num8 * num15) - num6 * (num3 * num16 - num4 * num15) + num14 * (num3 * num8 - num4 * num7);
        output[3] = 0.0 - (num2 * (num7 * num12 - num8 * num11) - num6 * (num3 * num12 - num4 * num11) + num10 * (num3 * num8 - num4 * num7));
        output[4] = 0.0 - (num5 * (num11 * num16 - num12 * num15) - num9 * (num7 * num16 - num8 * num15) + num13 * (num7 * num12 - num8 * num11));
        output[5] = num * (num11 * num16 - num12 * num15) - num9 * (num3 * num16 - num4 * num15) + num13 * (num3 * num12 - num4 * num11);
        output[6] = 0.0 - (num * (num7 * num16 - num8 * num15) - num5 * (num3 * num16 - num4 * num15) + num13 * (num3 * num8 - num4 * num7));
        output[7] = num * (num7 * num12 - num8 * num11) - num5 * (num3 * num12 - num4 * num11) + num9 * (num3 * num8 - num4 * num7);
        output[8] = num5 * (num10 * num16 - num12 * num14) - num9 * (num6 * num16 - num8 * num14) + num13 * (num6 * num12 - num8 * num10);
        output[9] = 0.0 - (num * (num10 * num16 - num12 * num14) - num9 * (num2 * num16 - num4 * num14) + num13 * (num2 * num12 - num4 * num10));
        output[10] = num * (num6 * num16 - num8 * num14) - num5 * (num2 * num16 - num4 * num14) + num13 * (num2 * num8 - num4 * num6);
        output[11] = 0.0 - (num * (num6 * num12 - num8 * num10) - num5 * (num2 * num12 - num4 * num10) + num9 * (num2 * num8 - num4 * num6));
        output[12] = 0.0 - (num5 * (num10 * num15 - num11 * num14) - num9 * (num6 * num15 - num7 * num14) + num13 * (num6 * num11 - num7 * num10));
        output[13] = num * (num10 * num15 - num11 * num14) - num9 * (num2 * num15 - num3 * num14) + num13 * (num2 * num11 - num3 * num10);
        output[14] = 0.0 - (num * (num6 * num15 - num7 * num14) - num5 * (num2 * num15 - num3 * num14) + num13 * (num2 * num7 - num3 * num6));
        output[15] = num * (num6 * num11 - num7 * num10) - num5 * (num2 * num11 - num3 * num10) + num9 * (num2 * num7 - num3 * num6);
        return output;
    }

    //
    // Summary:
    //     Calculates the determinant of a mat4
    //
    // Parameters:
    //   a:
    //     {mat4} a the source matrix
    //
    // Returns:
    //     {Number} determinant of a
    public static double Determinant(double[] a)
    {
        double num = a[0];
        double num2 = a[1];
        double num3 = a[2];
        double num4 = a[3];
        double num5 = a[4];
        double num6 = a[5];
        double num7 = a[6];
        double num8 = a[7];
        double num9 = a[8];
        double num10 = a[9];
        double num11 = a[10];
        double num12 = a[11];
        double num13 = a[12];
        double num14 = a[13];
        double num15 = a[14];
        double num16 = a[15];
        double num17 = num * num6 - num2 * num5;
        double num18 = num * num7 - num3 * num5;
        double num19 = num * num8 - num4 * num5;
        double num20 = num2 * num7 - num3 * num6;
        double num21 = num2 * num8 - num4 * num6;
        double num22 = num3 * num8 - num4 * num7;
        double num23 = num9 * num14 - num10 * num13;
        double num24 = num9 * num15 - num11 * num13;
        double num25 = num9 * num16 - num12 * num13;
        double num26 = num10 * num15 - num11 * num14;
        double num27 = num10 * num16 - num12 * num14;
        double num28 = num11 * num16 - num12 * num15;
        return num17 * num28 - num18 * num27 + num19 * num26 + num20 * num25 - num21 * num24 + num22 * num23;
    }

    //
    // Summary:
    //     Multiplies two mat4's
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   a:
    //     {mat4} a the first operand
    //
    //   b:
    //     {mat4} b the second operand
    //
    // Returns:
    //     {mat4} out
    public static double[] Multiply(double[] output, double[] a, double[] b)
    {
        double num = a[0];
        double num2 = a[1];
        double num3 = a[2];
        double num4 = a[3];
        double num5 = a[4];
        double num6 = a[5];
        double num7 = a[6];
        double num8 = a[7];
        double num9 = a[8];
        double num10 = a[9];
        double num11 = a[10];
        double num12 = a[11];
        double num13 = a[12];
        double num14 = a[13];
        double num15 = a[14];
        double num16 = a[15];
        double num17 = b[0];
        double num18 = b[1];
        double num19 = b[2];
        double num20 = b[3];
        output[0] = num17 * num + num18 * num5 + num19 * num9 + num20 * num13;
        output[1] = num17 * num2 + num18 * num6 + num19 * num10 + num20 * num14;
        output[2] = num17 * num3 + num18 * num7 + num19 * num11 + num20 * num15;
        output[3] = num17 * num4 + num18 * num8 + num19 * num12 + num20 * num16;
        num17 = b[4];
        num18 = b[5];
        num19 = b[6];
        num20 = b[7];
        output[4] = num17 * num + num18 * num5 + num19 * num9 + num20 * num13;
        output[5] = num17 * num2 + num18 * num6 + num19 * num10 + num20 * num14;
        output[6] = num17 * num3 + num18 * num7 + num19 * num11 + num20 * num15;
        output[7] = num17 * num4 + num18 * num8 + num19 * num12 + num20 * num16;
        num17 = b[8];
        num18 = b[9];
        num19 = b[10];
        num20 = b[11];
        output[8] = num17 * num + num18 * num5 + num19 * num9 + num20 * num13;
        output[9] = num17 * num2 + num18 * num6 + num19 * num10 + num20 * num14;
        output[10] = num17 * num3 + num18 * num7 + num19 * num11 + num20 * num15;
        output[11] = num17 * num4 + num18 * num8 + num19 * num12 + num20 * num16;
        num17 = b[12];
        num18 = b[13];
        num19 = b[14];
        num20 = b[15];
        output[12] = num17 * num + num18 * num5 + num19 * num9 + num20 * num13;
        output[13] = num17 * num2 + num18 * num6 + num19 * num10 + num20 * num14;
        output[14] = num17 * num3 + num18 * num7 + num19 * num11 + num20 * num15;
        output[15] = num17 * num4 + num18 * num8 + num19 * num12 + num20 * num16;
        return output;
    }

    //
    // Summary:
    //     Multiplies two mat4's
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   a:
    //     {mat4} a the first operand
    //
    //   b:
    //     {mat4} b the second operand
    //
    // Returns:
    //     {mat4} out
    public static double[] Multiply(double[] output, float[] a, double[] b)
    {
        double num = a[0];
        double num2 = a[1];
        double num3 = a[2];
        double num4 = a[3];
        double num5 = a[4];
        double num6 = a[5];
        double num7 = a[6];
        double num8 = a[7];
        double num9 = a[8];
        double num10 = a[9];
        double num11 = a[10];
        double num12 = a[11];
        double num13 = a[12];
        double num14 = a[13];
        double num15 = a[14];
        double num16 = a[15];
        double num17 = b[0];
        double num18 = b[1];
        double num19 = b[2];
        double num20 = b[3];
        output[0] = num17 * num + num18 * num5 + num19 * num9 + num20 * num13;
        output[1] = num17 * num2 + num18 * num6 + num19 * num10 + num20 * num14;
        output[2] = num17 * num3 + num18 * num7 + num19 * num11 + num20 * num15;
        output[3] = num17 * num4 + num18 * num8 + num19 * num12 + num20 * num16;
        num17 = b[4];
        num18 = b[5];
        num19 = b[6];
        num20 = b[7];
        output[4] = num17 * num + num18 * num5 + num19 * num9 + num20 * num13;
        output[5] = num17 * num2 + num18 * num6 + num19 * num10 + num20 * num14;
        output[6] = num17 * num3 + num18 * num7 + num19 * num11 + num20 * num15;
        output[7] = num17 * num4 + num18 * num8 + num19 * num12 + num20 * num16;
        num17 = b[8];
        num18 = b[9];
        num19 = b[10];
        num20 = b[11];
        output[8] = num17 * num + num18 * num5 + num19 * num9 + num20 * num13;
        output[9] = num17 * num2 + num18 * num6 + num19 * num10 + num20 * num14;
        output[10] = num17 * num3 + num18 * num7 + num19 * num11 + num20 * num15;
        output[11] = num17 * num4 + num18 * num8 + num19 * num12 + num20 * num16;
        num17 = b[12];
        num18 = b[13];
        num19 = b[14];
        num20 = b[15];
        output[12] = num17 * num + num18 * num5 + num19 * num9 + num20 * num13;
        output[13] = num17 * num2 + num18 * num6 + num19 * num10 + num20 * num14;
        output[14] = num17 * num3 + num18 * num7 + num19 * num11 + num20 * num15;
        output[15] = num17 * num4 + num18 * num8 + num19 * num12 + num20 * num16;
        return output;
    }

    //
    // Summary:
    //     mat4.multiply
    //
    // Parameters:
    //   output:
    //
    //   a:
    //
    //   b:
    public static double[] Mul(double[] output, double[] a, double[] b)
    {
        return Multiply(output, a, b);
    }

    //
    // Summary:
    //     mat4.multiply
    //
    // Parameters:
    //   output:
    //
    //   a:
    //
    //   b:
    public static double[] Mul(double[] output, float[] a, double[] b)
    {
        return Multiply(output, a, b);
    }

    //
    // Summary:
    //     If we have a translation-only matrix - one with no rotation or scaling - return
    //     true. If the matrix includes some scaling or rotation components, return false.
    //
    //     The identity matrix returns true here because there is no scaling or rotation,
    //     even though the translation is zero in that special case.
    //
    // Parameters:
    //   matrix:
    //
    // Returns:
    //     true if a simple translation matrix was found, otherwise false
    public static bool IsTranslationOnly(double[] matrix)
    {
        if ((float)(matrix[1] + 1.0) != 1f || (float)(matrix[6] + 1.0) != 1f)
        {
            return false;
        }

        if ((float)(matrix[2] + 1.0) != 1f || (float)(matrix[4] + 1.0) != 1f)
        {
            return false;
        }

        if ((float)matrix[0] != 1f || (float)matrix[5] != 1f || (float)matrix[10] != 1f)
        {
            return false;
        }

        if ((float)(matrix[8] + 1.0) != 1f || (float)(matrix[9] + 1.0) != 1f)
        {
            return false;
        }

        return true;
    }

    //
    // Summary:
    //     Translate a mat4 by the given vector
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   input:
    //     {mat4} a the matrix to translate
    //
    //   x:
    //     {vec3} v vector to translate by
    //
    //   y:
    //
    //   z:
    //
    // Returns:
    //     {mat4} out
    public static double[] Translate(double[] output, double[] input, double x, double y, double z)
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
            double num = input[0];
            double num2 = input[1];
            double num3 = input[2];
            double num4 = input[3];
            double num5 = input[4];
            double num6 = input[5];
            double num7 = input[6];
            double num8 = input[7];
            double num9 = input[8];
            double num10 = input[9];
            double num11 = input[10];
            double num12 = input[11];
            output[0] = num;
            output[1] = num2;
            output[2] = num3;
            output[3] = num4;
            output[4] = num5;
            output[5] = num6;
            output[6] = num7;
            output[7] = num8;
            output[8] = num9;
            output[9] = num10;
            output[10] = num11;
            output[11] = num12;
            output[12] = num * x + num5 * y + num9 * z + input[12];
            output[13] = num2 * x + num6 * y + num10 * z + input[13];
            output[14] = num3 * x + num7 * y + num11 * z + input[14];
            output[15] = num4 * x + num8 * y + num12 * z + input[15];
        }

        return output;
    }

    //
    // Summary:
    //     Translate a mat4 by the given vector
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   input:
    //     {mat4} a the matrix to translate
    //
    //   translate:
    //     {vec3} v vector to translate by
    //
    // Returns:
    //     {mat4} out
    public static double[] Translate(double[] output, double[] input, double[] translate)
    {
        double num = translate[0];
        double num2 = translate[1];
        double num3 = translate[2];
        if (input == output)
        {
            output[12] = input[0] * num + input[4] * num2 + input[8] * num3 + input[12];
            output[13] = input[1] * num + input[5] * num2 + input[9] * num3 + input[13];
            output[14] = input[2] * num + input[6] * num2 + input[10] * num3 + input[14];
            output[15] = input[3] * num + input[7] * num2 + input[11] * num3 + input[15];
        }
        else
        {
            double num4 = input[0];
            double num5 = input[1];
            double num6 = input[2];
            double num7 = input[3];
            double num8 = input[4];
            double num9 = input[5];
            double num10 = input[6];
            double num11 = input[7];
            double num12 = input[8];
            double num13 = input[9];
            double num14 = input[10];
            double num15 = input[11];
            output[0] = num4;
            output[1] = num5;
            output[2] = num6;
            output[3] = num7;
            output[4] = num8;
            output[5] = num9;
            output[6] = num10;
            output[7] = num11;
            output[8] = num12;
            output[9] = num13;
            output[10] = num14;
            output[11] = num15;
            output[12] = num4 * num + num8 * num2 + num12 * num3 + input[12];
            output[13] = num5 * num + num9 * num2 + num13 * num3 + input[13];
            output[14] = num6 * num + num10 * num2 + num14 * num3 + input[14];
            output[15] = num7 * num + num11 * num2 + num15 * num3 + input[15];
        }

        return output;
    }

    //
    // Summary:
    //     Scales the mat4 by the dimensions in the given vec3
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   a:
    //     {mat4} a the matrix to scale
    //
    //   v:
    //     {vec3} v the vec3 to scale the matrix by
    //
    // Returns:
    //     {mat4} out
    public static double[] Scale(double[] output, double[] a, double[] v)
    {
        double num = v[0];
        double num2 = v[1];
        double num3 = v[2];
        output[0] = a[0] * num;
        output[1] = a[1] * num;
        output[2] = a[2] * num;
        output[3] = a[3] * num;
        output[4] = a[4] * num2;
        output[5] = a[5] * num2;
        output[6] = a[6] * num2;
        output[7] = a[7] * num2;
        output[8] = a[8] * num3;
        output[9] = a[9] * num3;
        output[10] = a[10] * num3;
        output[11] = a[11] * num3;
        output[12] = a[12];
        output[13] = a[13];
        output[14] = a[14];
        output[15] = a[15];
        return output;
    }

    public static void Scale(double[] matrix, double x, double y, double z)
    {
        matrix[0] *= x;
        matrix[1] *= x;
        matrix[2] *= x;
        matrix[3] *= x;
        matrix[4] *= y;
        matrix[5] *= y;
        matrix[6] *= y;
        matrix[7] *= y;
        matrix[8] *= z;
        matrix[9] *= z;
        matrix[10] *= z;
        matrix[11] *= z;
    }

    //
    // Summary:
    //     Rotates a mat4 by the given angle
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   a:
    //     {mat4} a the matrix to rotate
    //
    //   rad:
    //     {Number} rad the angle to rotate the matrix by
    //
    //   axis:
    //     {vec3} axis the axis to rotate around
    //
    // Returns:
    //     {mat4} out
    public static double[] Rotate(double[] output, double[] a, double rad, double[] axis)
    {
        double x = axis[0];
        double y = axis[1];
        double z = axis[2];
        return Rotate(output, a, rad, x, y, z);
    }

    //
    // Summary:
    //     Rotates a mat4 by the given angle
    //
    // Parameters:
    //   output:
    //
    //   a:
    //
    //   rad:
    //
    //   x:
    //
    //   y:
    //
    //   z:
    public static double[] Rotate(double[] output, double[] a, double rad, double x, double y, double z)
    {
        double num = GameMath.Sqrt(x * x + y * y + z * z);
        if (GlMatrixMathd.Abs(num) < GlMatrixMathd.GLMAT_EPSILON())
        {
            return null;
        }

        num = 1.0 / num;
        x *= num;
        y *= num;
        z *= num;
        double num2 = GameMath.Sin(rad);
        double num3 = GameMath.Cos(rad);
        double num4 = 1.0 - num3;
        double num5 = a[0];
        double num6 = a[1];
        double num7 = a[2];
        double num8 = a[3];
        double num9 = a[4];
        double num10 = a[5];
        double num11 = a[6];
        double num12 = a[7];
        double num13 = a[8];
        double num14 = a[9];
        double num15 = a[10];
        double num16 = a[11];
        double num17 = x * x * num4 + num3;
        double num18 = y * x * num4 + z * num2;
        double num19 = z * x * num4 - y * num2;
        double num20 = x * y * num4 - z * num2;
        double num21 = y * y * num4 + num3;
        double num22 = z * y * num4 + x * num2;
        double num23 = x * z * num4 + y * num2;
        double num24 = y * z * num4 - x * num2;
        double num25 = z * z * num4 + num3;
        output[0] = num5 * num17 + num9 * num18 + num13 * num19;
        output[1] = num6 * num17 + num10 * num18 + num14 * num19;
        output[2] = num7 * num17 + num11 * num18 + num15 * num19;
        output[3] = num8 * num17 + num12 * num18 + num16 * num19;
        output[4] = num5 * num20 + num9 * num21 + num13 * num22;
        output[5] = num6 * num20 + num10 * num21 + num14 * num22;
        output[6] = num7 * num20 + num11 * num21 + num15 * num22;
        output[7] = num8 * num20 + num12 * num21 + num16 * num22;
        output[8] = num5 * num23 + num9 * num24 + num13 * num25;
        output[9] = num6 * num23 + num10 * num24 + num14 * num25;
        output[10] = num7 * num23 + num11 * num24 + num15 * num25;
        output[11] = num8 * num23 + num12 * num24 + num16 * num25;
        if (a != output)
        {
            output[12] = a[12];
            output[13] = a[13];
            output[14] = a[14];
            output[15] = a[15];
        }

        return output;
    }

    //
    // Summary:
    //     Rotates a matrix by the given angle around the X axis
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   a:
    //     {mat4} a the matrix to rotate
    //
    //   rad:
    //     {Number} rad the angle to rotate the matrix by
    //
    // Returns:
    //     {mat4} out
    public static double[] RotateX(double[] output, double[] a, double rad)
    {
        double num = GameMath.Sin(rad);
        double num2 = GameMath.Cos(rad);
        double num3 = a[4];
        double num4 = a[5];
        double num5 = a[6];
        double num6 = a[7];
        double num7 = a[8];
        double num8 = a[9];
        double num9 = a[10];
        double num10 = a[11];
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

        output[4] = num3 * num2 + num7 * num;
        output[5] = num4 * num2 + num8 * num;
        output[6] = num5 * num2 + num9 * num;
        output[7] = num6 * num2 + num10 * num;
        output[8] = num7 * num2 - num3 * num;
        output[9] = num8 * num2 - num4 * num;
        output[10] = num9 * num2 - num5 * num;
        output[11] = num10 * num2 - num6 * num;
        return output;
    }

    //
    // Summary:
    //     Rotates a matrix by the given angle around the Y axis
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   a:
    //     {mat4} a the matrix to rotate
    //
    //   rad:
    //     {Number} rad the angle to rotate the matrix by
    //
    // Returns:
    //     {mat4} out
    public static double[] RotateY(double[] output, double[] a, double rad)
    {
        double num = GameMath.Sin(rad);
        double num2 = GameMath.Cos(rad);
        double num3 = a[0];
        double num4 = a[1];
        double num5 = a[2];
        double num6 = a[3];
        double num7 = a[8];
        double num8 = a[9];
        double num9 = a[10];
        double num10 = a[11];
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

        output[0] = num3 * num2 - num7 * num;
        output[1] = num4 * num2 - num8 * num;
        output[2] = num5 * num2 - num9 * num;
        output[3] = num6 * num2 - num10 * num;
        output[8] = num3 * num + num7 * num2;
        output[9] = num4 * num + num8 * num2;
        output[10] = num5 * num + num9 * num2;
        output[11] = num6 * num + num10 * num2;
        return output;
    }

    //
    // Summary:
    //     Rotates a matrix by the given angle around the Z axis
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   a:
    //     {mat4} a the matrix to rotate
    //
    //   rad:
    //     {Number} rad the angle to rotate the matrix by
    //
    // Returns:
    //     {mat4} out
    public static double[] RotateZ(double[] output, double[] a, double rad)
    {
        double num = GameMath.Sin(rad);
        double num2 = GameMath.Cos(rad);
        double num3 = a[0];
        double num4 = a[1];
        double num5 = a[2];
        double num6 = a[3];
        double num7 = a[4];
        double num8 = a[5];
        double num9 = a[6];
        double num10 = a[7];
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

        output[0] = num3 * num2 + num7 * num;
        output[1] = num4 * num2 + num8 * num;
        output[2] = num5 * num2 + num9 * num;
        output[3] = num6 * num2 + num10 * num;
        output[4] = num7 * num2 - num3 * num;
        output[5] = num8 * num2 - num4 * num;
        output[6] = num9 * num2 - num5 * num;
        output[7] = num10 * num2 - num6 * num;
        return output;
    }

    //
    // Summary:
    //     Creates a matrix from a quaternion rotation and vector translation This is equivalent
    //     to (but much faster than): mat4.identity(dest); mat4.translate(dest, vec); var
    //     quatMat = mat4.create(); quat4.toMat4(quat, quatMat); mat4.multiply(dest, quatMat);
    //
    //
    // Parameters:
    //   output:
    //     {mat4} out mat4 receiving operation result
    //
    //   q:
    //     {quat4} q Rotation quaternion
    //
    //   v:
    //     {vec3} v Translation vector
    //
    // Returns:
    //     {mat4} out
    public static double[] FromRotationTranslation(double[] output, double[] q, double[] v)
    {
        double num = q[0];
        double num2 = q[1];
        double num3 = q[2];
        double num4 = q[3];
        double num5 = num + num;
        double num6 = num2 + num2;
        double num7 = num3 + num3;
        double num8 = num * num5;
        double num9 = num * num6;
        double num10 = num * num7;
        double num11 = num2 * num6;
        double num12 = num2 * num7;
        double num13 = num3 * num7;
        double num14 = num4 * num5;
        double num15 = num4 * num6;
        double num16 = num4 * num7;
        output[0] = 1.0 - (num11 + num13);
        output[1] = num9 + num16;
        output[2] = num10 - num15;
        output[3] = 0.0;
        output[4] = num9 - num16;
        output[5] = 1.0 - (num8 + num13);
        output[6] = num12 + num14;
        output[7] = 0.0;
        output[8] = num10 + num15;
        output[9] = num12 - num14;
        output[10] = 1.0 - (num8 + num11);
        output[11] = 0.0;
        output[12] = v[0];
        output[13] = v[1];
        output[14] = v[2];
        output[15] = 1.0;
        return output;
    }

    //
    // Summary:
    //     Calculates a 4x4 matrix from the given quaternion
    //
    // Parameters:
    //   output:
    //     {mat4} out mat4 receiving operation result
    //
    //   q:
    //     {quat} q Quaternion to create matrix from
    //
    // Returns:
    //     {mat4} out
    public static double[] FromQuat(double[] output, double[] q)
    {
        double num = q[0];
        double num2 = q[1];
        double num3 = q[2];
        double num4 = q[3];
        double num5 = num + num;
        double num6 = num2 + num2;
        double num7 = num3 + num3;
        double num8 = num * num5;
        double num9 = num * num6;
        double num10 = num * num7;
        double num11 = num2 * num6;
        double num12 = num2 * num7;
        double num13 = num3 * num7;
        double num14 = num4 * num5;
        double num15 = num4 * num6;
        double num16 = num4 * num7;
        output[0] = 1.0 - (num11 + num13);
        output[1] = num9 + num16;
        output[2] = num10 - num15;
        output[3] = 0.0;
        output[4] = num9 - num16;
        output[5] = 1.0 - (num8 + num13);
        output[6] = num12 + num14;
        output[7] = 0.0;
        output[8] = num10 + num15;
        output[9] = num12 - num14;
        output[10] = 1.0 - (num8 + num11);
        output[11] = 0.0;
        output[12] = 0.0;
        output[13] = 0.0;
        output[14] = 0.0;
        output[15] = 1.0;
        return output;
    }

    //
    // Summary:
    //     Generates a frustum matrix with the given bounds
    //
    // Parameters:
    //   output:
    //     {mat4} out mat4 frustum matrix will be written into
    //
    //   left:
    //     {Number} left Left bound of the frustum
    //
    //   right:
    //     {Number} right Right bound of the frustum
    //
    //   bottom:
    //     {Number} bottom Bottom bound of the frustum
    //
    //   top:
    //     {Number} top Top bound of the frustum
    //
    //   near:
    //     {Number} near Near bound of the frustum
    //
    //   far:
    //     {Number} far Far bound of the frustum
    //
    // Returns:
    //     {mat4} out
    public static double[] Frustum(double[] output, double left, double right, double bottom, double top, double near, double far)
    {
        double num = 1.0 / (right - left);
        double num2 = 1.0 / (top - bottom);
        double num3 = 1.0 / (near - far);
        output[0] = near * 2.0 * num;
        output[1] = 0.0;
        output[2] = 0.0;
        output[3] = 0.0;
        output[4] = 0.0;
        output[5] = near * 2.0 * num2;
        output[6] = 0.0;
        output[7] = 0.0;
        output[8] = (right + left) * num;
        output[9] = (top + bottom) * num2;
        output[10] = (far + near) * num3;
        output[11] = -1.0;
        output[12] = 0.0;
        output[13] = 0.0;
        output[14] = far * near * 2.0 * num3;
        output[15] = 0.0;
        return output;
    }

    //
    // Summary:
    //     Generates a perspective projection matrix with the given bounds
    //
    // Parameters:
    //   output:
    //     {mat4} out mat4 frustum matrix will be written into
    //
    //   fovy:
    //     {number} fovy Vertical field of view in radians
    //
    //   aspect:
    //     {number} aspect Aspect ratio. typically viewport width/height
    //
    //   near:
    //     {number} near Near bound of the frustum
    //
    //   far:
    //     {number} far Far bound of the frustum
    //
    // Returns:
    //     {mat4} out
    public static double[] Perspective(double[] output, double fovy, double aspect, double near, double far)
    {
        double num = 1.0 / GameMath.Tan(fovy / 2.0);
        double num2 = 1.0 / (near - far);
        output[0] = num / aspect;
        output[1] = 0.0;
        output[2] = 0.0;
        output[3] = 0.0;
        output[4] = 0.0;
        output[5] = num;
        output[6] = 0.0;
        output[7] = 0.0;
        output[8] = 0.0;
        output[9] = 0.0;
        output[10] = (far + near) * num2;
        output[11] = -1.0;
        output[12] = 0.0;
        output[13] = 0.0;
        output[14] = 2.0 * far * near * num2;
        output[15] = 0.0;
        return output;
    }

    //
    // Summary:
    //     Generates a orthogonal projection matrix with the given bounds
    //
    // Parameters:
    //   output:
    //     {mat4} out mat4 frustum matrix will be written into
    //
    //   left:
    //     {number} left Left bound of the frustum
    //
    //   right:
    //     {number} right Right bound of the frustum
    //
    //   bottom:
    //     {number} bottom Bottom bound of the frustum
    //
    //   top:
    //     {number} top Top bound of the frustum
    //
    //   near:
    //     {number} near Near bound of the frustum
    //
    //   far:
    //     {number} far Far bound of the frustum
    //
    // Returns:
    //     {mat4} out
    public static double[] Ortho(double[] output, double left, double right, double bottom, double top, double near, double far)
    {
        double num = 1.0 / (left - right);
        double num2 = 1.0 / (bottom - top);
        double num3 = 1.0 / (near - far);
        output[0] = -2.0 * num;
        output[1] = 0.0;
        output[2] = 0.0;
        output[3] = 0.0;
        output[4] = 0.0;
        output[5] = -2.0 * num2;
        output[6] = 0.0;
        output[7] = 0.0;
        output[8] = 0.0;
        output[9] = 0.0;
        output[10] = 2.0 * num3;
        output[11] = 0.0;
        output[12] = (left + right) * num;
        output[13] = (top + bottom) * num2;
        output[14] = (far + near) * num3;
        output[15] = 1.0;
        return output;
    }

    //
    // Summary:
    //     Generates a look-at matrix with the given eye position, focal point, and up axis
    //
    //
    // Parameters:
    //   output:
    //     {mat4} out mat4 frustum matrix will be written into
    //
    //   eye:
    //     {vec3} eye Position of the viewer
    //
    //   center:
    //     {vec3} center Point the viewer is looking at
    //
    //   up:
    //     {vec3} up vec3 pointing up
    //
    // Returns:
    //     {mat4} out
    public static double[] LookAt(double[] output, double[] eye, double[] center, double[] up)
    {
        double num = eye[0];
        double num2 = eye[1];
        double num3 = eye[2];
        double num4 = up[0];
        double num5 = up[1];
        double num6 = up[2];
        double num7 = center[0];
        double num8 = center[1];
        double num9 = center[2];
        if (GlMatrixMathd.Abs(num - num7) < GlMatrixMathd.GLMAT_EPSILON() && GlMatrixMathd.Abs(num2 - num8) < GlMatrixMathd.GLMAT_EPSILON() && GlMatrixMathd.Abs(num3 - num9) < GlMatrixMathd.GLMAT_EPSILON())
        {
            return Identity(output);
        }

        double num10 = num - num7;
        double num11 = num2 - num8;
        double num12 = num3 - num9;
        double num13 = 1f / GameMath.Sqrt(num10 * num10 + num11 * num11 + num12 * num12);
        num10 *= num13;
        num11 *= num13;
        num12 *= num13;
        double num14 = num5 * num12 - num6 * num11;
        double num15 = num6 * num10 - num4 * num12;
        double num16 = num4 * num11 - num5 * num10;
        num13 = GameMath.Sqrt(num14 * num14 + num15 * num15 + num16 * num16);
        if (num13 == 0.0)
        {
            num14 = 0.0;
            num15 = 0.0;
            num16 = 0.0;
        }
        else
        {
            num13 = 1.0 / num13;
            num14 *= num13;
            num15 *= num13;
            num16 *= num13;
        }

        double num17 = num11 * num16 - num12 * num15;
        double num18 = num12 * num14 - num10 * num16;
        double num19 = num10 * num15 - num11 * num14;
        num13 = GameMath.Sqrt(num17 * num17 + num18 * num18 + num19 * num19);
        if (num13 == 0.0)
        {
            num17 = 0.0;
            num18 = 0.0;
            num19 = 0.0;
        }
        else
        {
            num13 = 1.0 / num13;
            num17 *= num13;
            num18 *= num13;
            num19 *= num13;
        }

        output[0] = num14;
        output[1] = num17;
        output[2] = num10;
        output[3] = 0.0;
        output[4] = num15;
        output[5] = num18;
        output[6] = num11;
        output[7] = 0.0;
        output[8] = num16;
        output[9] = num19;
        output[10] = num12;
        output[11] = 0.0;
        output[12] = 0.0 - (num14 * num + num15 * num2 + num16 * num3);
        output[13] = 0.0 - (num17 * num + num18 * num2 + num19 * num3);
        output[14] = 0.0 - (num10 * num + num11 * num2 + num12 * num3);
        output[15] = 1.0;
        return output;
    }

    //
    // Summary:
    //     Multiply the matrix with a vec4. Reference: http://mathinsight.org/matrix_vector_multiplication
    //     Returns a new vec4 vector
    //
    // Parameters:
    //   matrix:
    //
    //   vec4:
    public static double[] MulWithVec4(double[] matrix, double[] vec4)
    {
        double[] array = new double[4];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                array[i] += matrix[4 * j + i] * vec4[j];
            }
        }

        return array;
    }

    //
    // Summary:
    //     Multiply the matrix with a vec4. Reference: http://mathinsight.org/matrix_vector_multiplication
    //
    //
    // Parameters:
    //   matrix:
    //
    //   vec4:
    //
    //   outVal:
    public static void MulWithVec4(double[] matrix, double[] vec4, Vec4d outVal)
    {
        outVal.Set(0.0, 0.0, 0.0, 0.0);
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                outVal[i] += matrix[4 * j + i] * vec4[j];
            }
        }
    }

    //
    // Summary:
    //     Multiply the matrix with a vec4. Reference: http://mathinsight.org/matrix_vector_multiplication
    //
    //
    // Parameters:
    //   matrix:
    //
    //   inVal:
    //
    //   outVal:
    public static void MulWithVec4(double[] matrix, Vec4d inVal, Vec4d outVal)
    {
        outVal.Set(0.0, 0.0, 0.0, 0.0);
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                outVal[i] += matrix[4 * j + i] * inVal[j];
            }
        }
    }
}
#if false // Decompilation log
'182' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Newtonsoft.Json.dll'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
