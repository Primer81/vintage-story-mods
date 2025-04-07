#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;

namespace Vintagestory.API.MathTools;

//
// Summary:
//     4x4 Matrix Math
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

    //
    // Summary:
    //     Creates a new identity mat4 0 4 8 12 1 5 9 13 2 6 10 14 3 7 11 15
    //
    // Returns:
    //     {mat4} a new 4x4 matrix
    public static float[] Create()
    {
        return new float[16]
        {
            1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f,
            1f, 0f, 0f, 0f, 0f, 1f
        };
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

    //
    // Summary:
    //     Set a mat4 to the identity matrix with a scale applied
    //
    // Parameters:
    //   output:
    //     {mat4} out the receiving matrix
    //
    //   scale:
    //
    // Returns:
    //     {mat4} out
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
    public static float[] Transpose(float[] output, float[] a)
    {
        if (output == a)
        {
            float num = a[1];
            float num2 = a[2];
            float num3 = a[3];
            float num4 = a[6];
            float num5 = a[7];
            float num6 = a[11];
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
    public static float[] Invert(float[] output, float[] a)
    {
        float num = a[0];
        float num2 = a[1];
        float num3 = a[2];
        float num4 = a[3];
        float num5 = a[4];
        float num6 = a[5];
        float num7 = a[6];
        float num8 = a[7];
        float num9 = a[8];
        float num10 = a[9];
        float num11 = a[10];
        float num12 = a[11];
        float num13 = a[12];
        float num14 = a[13];
        float num15 = a[14];
        float num16 = a[15];
        float num17 = num * num6 - num2 * num5;
        float num18 = num * num7 - num3 * num5;
        float num19 = num * num8 - num4 * num5;
        float num20 = num2 * num7 - num3 * num6;
        float num21 = num2 * num8 - num4 * num6;
        float num22 = num3 * num8 - num4 * num7;
        float num23 = num9 * num14 - num10 * num13;
        float num24 = num9 * num15 - num11 * num13;
        float num25 = num9 * num16 - num12 * num13;
        float num26 = num10 * num15 - num11 * num14;
        float num27 = num10 * num16 - num12 * num14;
        float num28 = num11 * num16 - num12 * num15;
        float num29 = num17 * num28 - num18 * num27 + num19 * num26 + num20 * num25 - num21 * num24 + num22 * num23;
        if (num29 == 0f)
        {
            return null;
        }

        num29 = 1f / num29;
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
    public static float[] Adjoint(float[] output, float[] a)
    {
        float num = a[0];
        float num2 = a[1];
        float num3 = a[2];
        float num4 = a[3];
        float num5 = a[4];
        float num6 = a[5];
        float num7 = a[6];
        float num8 = a[7];
        float num9 = a[8];
        float num10 = a[9];
        float num11 = a[10];
        float num12 = a[11];
        float num13 = a[12];
        float num14 = a[13];
        float num15 = a[14];
        float num16 = a[15];
        output[0] = num6 * (num11 * num16 - num12 * num15) - num10 * (num7 * num16 - num8 * num15) + num14 * (num7 * num12 - num8 * num11);
        output[1] = 0f - (num2 * (num11 * num16 - num12 * num15) - num10 * (num3 * num16 - num4 * num15) + num14 * (num3 * num12 - num4 * num11));
        output[2] = num2 * (num7 * num16 - num8 * num15) - num6 * (num3 * num16 - num4 * num15) + num14 * (num3 * num8 - num4 * num7);
        output[3] = 0f - (num2 * (num7 * num12 - num8 * num11) - num6 * (num3 * num12 - num4 * num11) + num10 * (num3 * num8 - num4 * num7));
        output[4] = 0f - (num5 * (num11 * num16 - num12 * num15) - num9 * (num7 * num16 - num8 * num15) + num13 * (num7 * num12 - num8 * num11));
        output[5] = num * (num11 * num16 - num12 * num15) - num9 * (num3 * num16 - num4 * num15) + num13 * (num3 * num12 - num4 * num11);
        output[6] = 0f - (num * (num7 * num16 - num8 * num15) - num5 * (num3 * num16 - num4 * num15) + num13 * (num3 * num8 - num4 * num7));
        output[7] = num * (num7 * num12 - num8 * num11) - num5 * (num3 * num12 - num4 * num11) + num9 * (num3 * num8 - num4 * num7);
        output[8] = num5 * (num10 * num16 - num12 * num14) - num9 * (num6 * num16 - num8 * num14) + num13 * (num6 * num12 - num8 * num10);
        output[9] = 0f - (num * (num10 * num16 - num12 * num14) - num9 * (num2 * num16 - num4 * num14) + num13 * (num2 * num12 - num4 * num10));
        output[10] = num * (num6 * num16 - num8 * num14) - num5 * (num2 * num16 - num4 * num14) + num13 * (num2 * num8 - num4 * num6);
        output[11] = 0f - (num * (num6 * num12 - num8 * num10) - num5 * (num2 * num12 - num4 * num10) + num9 * (num2 * num8 - num4 * num6));
        output[12] = 0f - (num5 * (num10 * num15 - num11 * num14) - num9 * (num6 * num15 - num7 * num14) + num13 * (num6 * num11 - num7 * num10));
        output[13] = num * (num10 * num15 - num11 * num14) - num9 * (num2 * num15 - num3 * num14) + num13 * (num2 * num11 - num3 * num10);
        output[14] = 0f - (num * (num6 * num15 - num7 * num14) - num5 * (num2 * num15 - num3 * num14) + num13 * (num2 * num7 - num3 * num6));
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
    public static float Determinant(float[] a)
    {
        float num = a[0];
        float num2 = a[1];
        float num3 = a[2];
        float num4 = a[3];
        float num5 = a[4];
        float num6 = a[5];
        float num7 = a[6];
        float num8 = a[7];
        float num9 = a[8];
        float num10 = a[9];
        float num11 = a[10];
        float num12 = a[11];
        float num13 = a[12];
        float num14 = a[13];
        float num15 = a[14];
        float num16 = a[15];
        float num17 = num * num6 - num2 * num5;
        float num18 = num * num7 - num3 * num5;
        float num19 = num * num8 - num4 * num5;
        float num20 = num2 * num7 - num3 * num6;
        float num21 = num2 * num8 - num4 * num6;
        float num22 = num3 * num8 - num4 * num7;
        float num23 = num9 * num14 - num10 * num13;
        float num24 = num9 * num15 - num11 * num13;
        float num25 = num9 * num16 - num12 * num13;
        float num26 = num10 * num15 - num11 * num14;
        float num27 = num10 * num16 - num12 * num14;
        float num28 = num11 * num16 - num12 * num15;
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
    public static float[] Multiply(float[] output, float[] a, float[] b)
    {
        float num = a[0];
        float num2 = a[1];
        float num3 = a[2];
        float num4 = a[3];
        float num5 = a[4];
        float num6 = a[5];
        float num7 = a[6];
        float num8 = a[7];
        float num9 = a[8];
        float num10 = a[9];
        float num11 = a[10];
        float num12 = a[11];
        float num13 = a[12];
        float num14 = a[13];
        float num15 = a[14];
        float num16 = a[15];
        float num17 = b[0];
        float num18 = b[1];
        float num19 = b[2];
        float num20 = b[3];
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
    public static float[] Mul(float[] output, float[] a, float[] b)
    {
        return Multiply(output, a, b);
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
            float num = input[0];
            float num2 = input[1];
            float num3 = input[2];
            float num4 = input[3];
            float num5 = input[4];
            float num6 = input[5];
            float num7 = input[6];
            float num8 = input[7];
            float num9 = input[8];
            float num10 = input[9];
            float num11 = input[10];
            float num12 = input[11];
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
    public static float[] Translate(float[] output, float[] input, float[] translate)
    {
        float num = translate[0];
        float num2 = translate[1];
        float num3 = translate[2];
        if (input == output)
        {
            output[12] = input[0] * num + input[4] * num2 + input[8] * num3 + input[12];
            output[13] = input[1] * num + input[5] * num2 + input[9] * num3 + input[13];
            output[14] = input[2] * num + input[6] * num2 + input[10] * num3 + input[14];
            output[15] = input[3] * num + input[7] * num2 + input[11] * num3 + input[15];
        }
        else
        {
            float num4 = input[0];
            float num5 = input[1];
            float num6 = input[2];
            float num7 = input[3];
            float num8 = input[4];
            float num9 = input[5];
            float num10 = input[6];
            float num11 = input[7];
            float num12 = input[8];
            float num13 = input[9];
            float num14 = input[10];
            float num15 = input[11];
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
    public static float[] Scale(float[] output, float[] a, float[] v)
    {
        float num = v[0];
        float num2 = v[1];
        float num3 = v[2];
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

    public static void SimpleScaleMatrix(Span<float> matrix, float x, float y, float z)
    {
        matrix.Clear();
        matrix[0] = x;
        matrix[5] = y;
        matrix[10] = z;
        matrix[15] = 1f;
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
    //   xScale:
    //
    //   yScale:
    //
    //   zScale:
    //
    // Returns:
    //     {mat4} out
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
    public static float[] Rotate(float[] output, float[] a, float rad, float[] axis)
    {
        float num = axis[0];
        float num2 = axis[1];
        float num3 = axis[2];
        float num4 = GameMath.Sqrt(num * num + num2 * num2 + num3 * num3);
        if (GlMatrixMathf.Abs(num4) < GlMatrixMathf.GLMAT_EPSILON())
        {
            return null;
        }

        num4 = 1f / num4;
        num *= num4;
        num2 *= num4;
        num3 *= num4;
        float num5 = GameMath.Sin(rad);
        float num6 = GameMath.Cos(rad);
        float num7 = 1f - num6;
        float num8 = a[0];
        float num9 = a[1];
        float num10 = a[2];
        float num11 = a[3];
        float num12 = a[4];
        float num13 = a[5];
        float num14 = a[6];
        float num15 = a[7];
        float num16 = a[8];
        float num17 = a[9];
        float num18 = a[10];
        float num19 = a[11];
        float num20 = num * num * num7 + num6;
        float num21 = num2 * num * num7 + num3 * num5;
        float num22 = num3 * num * num7 - num2 * num5;
        float num23 = num * num2 * num7 - num3 * num5;
        float num24 = num2 * num2 * num7 + num6;
        float num25 = num3 * num2 * num7 + num * num5;
        float num26 = num * num3 * num7 + num2 * num5;
        float num27 = num2 * num3 * num7 - num * num5;
        float num28 = num3 * num3 * num7 + num6;
        output[0] = num8 * num20 + num12 * num21 + num16 * num22;
        output[1] = num9 * num20 + num13 * num21 + num17 * num22;
        output[2] = num10 * num20 + num14 * num21 + num18 * num22;
        output[3] = num11 * num20 + num15 * num21 + num19 * num22;
        output[4] = num8 * num23 + num12 * num24 + num16 * num25;
        output[5] = num9 * num23 + num13 * num24 + num17 * num25;
        output[6] = num10 * num23 + num14 * num24 + num18 * num25;
        output[7] = num11 * num23 + num15 * num24 + num19 * num25;
        output[8] = num8 * num26 + num12 * num27 + num16 * num28;
        output[9] = num9 * num26 + num13 * num27 + num17 * num28;
        output[10] = num10 * num26 + num14 * num27 + num18 * num28;
        output[11] = num11 * num26 + num15 * num27 + num19 * num28;
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
    public static float[] RotateX(float[] output, float[] a, float rad)
    {
        float num = GameMath.Sin(rad);
        float num2 = GameMath.Cos(rad);
        float num3 = a[4];
        float num4 = a[5];
        float num5 = a[6];
        float num6 = a[7];
        float num7 = a[8];
        float num8 = a[9];
        float num9 = a[10];
        float num10 = a[11];
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
    public static float[] RotateY(float[] output, float[] a, float rad)
    {
        float num = GameMath.Sin(rad);
        float num2 = GameMath.Cos(rad);
        float num3 = a[0];
        float num4 = a[1];
        float num5 = a[2];
        float num6 = a[3];
        float num7 = a[8];
        float num8 = a[9];
        float num9 = a[10];
        float num10 = a[11];
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
    public static float[] RotateZ(float[] output, float[] a, float rad)
    {
        float num = GameMath.Sin(rad);
        float num2 = GameMath.Cos(rad);
        float num3 = a[0];
        float num4 = a[1];
        float num5 = a[2];
        float num6 = a[3];
        float num7 = a[4];
        float num8 = a[5];
        float num9 = a[6];
        float num10 = a[7];
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
    //     Provides a composite rotation matrix, equivalent to RotateX followed by RotateY
    //     followed by RotateZ
    //     Here we work on a Span (which may be on the stack) for higher performance
    //
    // Parameters:
    //   matrix:
    //
    //   radX:
    //
    //   radY:
    //
    //   radZ:
    public static void RotateXYZ(Span<float> matrix, float radX, float radY, float radZ)
    {
        float num = GameMath.Sin(radX);
        float num2 = GameMath.Cos(radX);
        float num3 = GameMath.Sin(radY);
        float num4 = GameMath.Cos(radY);
        float num5 = GameMath.Sin(radZ);
        float num6 = GameMath.Cos(radZ);
        float num7 = num * num3;
        float num8 = (0f - num2) * num3;
        matrix[0] = num4 * num6;
        matrix[1] = num7 * num6 + num2 * num5;
        matrix[2] = num8 * num6 + num * num5;
        matrix[3] = 0f;
        matrix[4] = (0f - num4) * num5;
        matrix[5] = num2 * num6 - num7 * num5;
        matrix[6] = num * num6 - num8 * num5;
        matrix[7] = 0f;
        matrix[8] = num3;
        matrix[9] = (0f - num) * num4;
        matrix[10] = num2 * num4;
        matrix[11] = 0f;
        matrix[12] = 0f;
        matrix[13] = 0f;
        matrix[14] = 0f;
        matrix[15] = 1f;
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
    public static float[] FromRotationTranslation(float[] output, float[] q, float[] v)
    {
        float num = q[0];
        float num2 = q[1];
        float num3 = q[2];
        float num4 = q[3];
        float num5 = num + num;
        float num6 = num2 + num2;
        float num7 = num3 + num3;
        float num8 = num * num5;
        float num9 = num * num6;
        float num10 = num * num7;
        float num11 = num2 * num6;
        float num12 = num2 * num7;
        float num13 = num3 * num7;
        float num14 = num4 * num5;
        float num15 = num4 * num6;
        float num16 = num4 * num7;
        output[0] = 1f - (num11 + num13);
        output[1] = num9 + num16;
        output[2] = num10 - num15;
        output[3] = 0f;
        output[4] = num9 - num16;
        output[5] = 1f - (num8 + num13);
        output[6] = num12 + num14;
        output[7] = 0f;
        output[8] = num10 + num15;
        output[9] = num12 - num14;
        output[10] = 1f - (num8 + num11);
        output[11] = 0f;
        output[12] = v[0];
        output[13] = v[1];
        output[14] = v[2];
        output[15] = 1f;
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
    public static float[] FromQuat(float[] output, float[] q)
    {
        float num = q[0];
        float num2 = q[1];
        float num3 = q[2];
        float num4 = q[3];
        float num5 = num + num;
        float num6 = num2 + num2;
        float num7 = num3 + num3;
        float num8 = num * num5;
        float num9 = num * num6;
        float num10 = num * num7;
        float num11 = num2 * num6;
        float num12 = num2 * num7;
        float num13 = num3 * num7;
        float num14 = num4 * num5;
        float num15 = num4 * num6;
        float num16 = num4 * num7;
        output[0] = 1f - (num11 + num13);
        output[1] = num9 + num16;
        output[2] = num10 - num15;
        output[3] = 0f;
        output[4] = num9 - num16;
        output[5] = 1f - (num8 + num13);
        output[6] = num12 + num14;
        output[7] = 0f;
        output[8] = num10 + num15;
        output[9] = num12 - num14;
        output[10] = 1f - (num8 + num11);
        output[11] = 0f;
        output[12] = 0f;
        output[13] = 0f;
        output[14] = 0f;
        output[15] = 1f;
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
    public static float[] Frustum(float[] output, float left, float right, float bottom, float top, float near, float far)
    {
        float num = 1f / (right - left);
        float num2 = 1f / (top - bottom);
        float num3 = 1f / (near - far);
        output[0] = near * 2f * num;
        output[1] = 0f;
        output[2] = 0f;
        output[3] = 0f;
        output[4] = 0f;
        output[5] = near * 2f * num2;
        output[6] = 0f;
        output[7] = 0f;
        output[8] = (right + left) * num;
        output[9] = (top + bottom) * num2;
        output[10] = (far + near) * num3;
        output[11] = -1f;
        output[12] = 0f;
        output[13] = 0f;
        output[14] = far * near * 2f * num3;
        output[15] = 0f;
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
    public static float[] Perspective(float[] output, float fovy, float aspect, float near, float far)
    {
        float num = 1f / GameMath.Tan(fovy / 2f);
        float num2 = 1f / (near - far);
        output[0] = num / aspect;
        output[1] = 0f;
        output[2] = 0f;
        output[3] = 0f;
        output[4] = 0f;
        output[5] = num;
        output[6] = 0f;
        output[7] = 0f;
        output[8] = 0f;
        output[9] = 0f;
        output[10] = (far + near) * num2;
        output[11] = -1f;
        output[12] = 0f;
        output[13] = 0f;
        output[14] = 2f * far * near * num2;
        output[15] = 0f;
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
    public static float[] Ortho(float[] output, float left, float right, float bottom, float top, float near, float far)
    {
        float num = 1f / (left - right);
        float num2 = 1f / (bottom - top);
        float num3 = 1f / (near - far);
        output[0] = -2f * num;
        output[1] = 0f;
        output[2] = 0f;
        output[3] = 0f;
        output[4] = 0f;
        output[5] = -2f * num2;
        output[6] = 0f;
        output[7] = 0f;
        output[8] = 0f;
        output[9] = 0f;
        output[10] = 2f * num3;
        output[11] = 0f;
        output[12] = (left + right) * num;
        output[13] = (top + bottom) * num2;
        output[14] = (far + near) * num3;
        output[15] = 1f;
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
    public static float[] LookAt(float[] output, float[] eye, float[] center, float[] up)
    {
        float num = eye[0];
        float num2 = eye[1];
        float num3 = eye[2];
        float num4 = up[0];
        float num5 = up[1];
        float num6 = up[2];
        float num7 = center[0];
        float num8 = center[1];
        float num9 = center[2];
        if (GlMatrixMathf.Abs(num - num7) < GlMatrixMathf.GLMAT_EPSILON() && GlMatrixMathf.Abs(num2 - num8) < GlMatrixMathf.GLMAT_EPSILON() && GlMatrixMathf.Abs(num3 - num9) < GlMatrixMathf.GLMAT_EPSILON())
        {
            return Identity(output);
        }

        float num10 = num - num7;
        float num11 = num2 - num8;
        float num12 = num3 - num9;
        float num13 = 1f / GameMath.Sqrt(num10 * num10 + num11 * num11 + num12 * num12);
        num10 *= num13;
        num11 *= num13;
        num12 *= num13;
        float num14 = num5 * num12 - num6 * num11;
        float num15 = num6 * num10 - num4 * num12;
        float num16 = num4 * num11 - num5 * num10;
        num13 = GameMath.Sqrt(num14 * num14 + num15 * num15 + num16 * num16);
        if (num13 == 0f)
        {
            num14 = 0f;
            num15 = 0f;
            num16 = 0f;
        }
        else
        {
            num13 = 1f / num13;
            num14 *= num13;
            num15 *= num13;
            num16 *= num13;
        }

        float num17 = num11 * num16 - num12 * num15;
        float num18 = num12 * num14 - num10 * num16;
        float num19 = num10 * num15 - num11 * num14;
        num13 = GameMath.Sqrt(num17 * num17 + num18 * num18 + num19 * num19);
        if (num13 == 0f)
        {
            num17 = 0f;
            num18 = 0f;
            num19 = 0f;
        }
        else
        {
            num13 = 1f / num13;
            num17 *= num13;
            num18 *= num13;
            num19 *= num13;
        }

        output[0] = num14;
        output[1] = num17;
        output[2] = num10;
        output[3] = 0f;
        output[4] = num15;
        output[5] = num18;
        output[6] = num11;
        output[7] = 0f;
        output[8] = num16;
        output[9] = num19;
        output[10] = num12;
        output[11] = 0f;
        output[12] = 0f - (num14 * num + num15 * num2 + num16 * num3);
        output[13] = 0f - (num17 * num + num18 * num2 + num19 * num3);
        output[14] = 0f - (num10 * num + num11 * num2 + num12 * num3);
        output[15] = 1f;
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
    public static float[] MulWithVec4(float[] matrix, float[] vec4)
    {
        float[] array = new float[4];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                array[i] += matrix[4 * j + i] * vec4[j];
            }
        }

        return array;
    }

    public static float[] MulWithVec4(float[] matrix, float v1, float v2, float v3, float v4)
    {
        float[] array = new float[4];
        for (int i = 0; i < 4; i++)
        {
            array[i] += matrix[i] * v1;
            array[i] += matrix[4 + i] * v2;
            array[i] += matrix[8 + i] * v3;
            array[i] += matrix[12 + i] * v4;
        }

        return array;
    }

    public static void MulWithVec4(float[] matrix, float[] vec, float[] output)
    {
        MulWithVec4((Span<float>)matrix, vec, output);
    }

    public static void MulWithVec4(Span<float> matrix, float[] vec, float[] output)
    {
        float num = vec[0];
        float num2 = vec[1];
        float num3 = vec[2];
        float num4 = vec[3];
        output[0] = matrix[0] * num + matrix[4] * num2 + matrix[8] * num3 + matrix[12] * num4;
        output[1] = matrix[1] * num + matrix[5] * num2 + matrix[9] * num3 + matrix[13] * num4;
        output[2] = matrix[2] * num + matrix[6] * num2 + matrix[10] * num3 + matrix[14] * num4;
        output[3] = matrix[3] * num + matrix[7] * num2 + matrix[11] * num3 + matrix[15] * num4;
    }

    //
    // Summary:
    //     Used for vec3 representing a direction or normal - as a vec4 this would have
    //     the 4th element set to 0, so that applying a matrix transform with a translation
    //     would have *no* effect
    public static void MulWithVec3(float[] matrix, float[] vec, float[] output)
    {
        MulWithVec3((Span<float>)matrix, vec, output);
    }

    //
    // Summary:
    //     Used for vec3 representing a direction or normal - as a vec4 this would have
    //     the 4th element set to 0, so that applying a matrix transform with a translation
    //     would have *no* effect
    public static void MulWithVec3(Span<float> matrix, float[] vec, float[] output)
    {
        float num = vec[0];
        float num2 = vec[1];
        float num3 = vec[2];
        output[0] = matrix[0] * num + matrix[4] * num2 + matrix[8] * num3;
        output[1] = matrix[1] * num + matrix[5] * num2 + matrix[9] * num3;
        output[2] = matrix[2] * num + matrix[6] * num2 + matrix[10] * num3;
    }

    //
    // Summary:
    //     Used for vec3 representing an x,y,z position - as a vec4 this would have the
    //     4th element set to 1, so that applying a matrix transform with a translation
    //     would have an effect The offset is used to index within the original and output
    //     arrays - e.g. in MeshData.xyz
    public static void MulWithVec3_Position(float[] matrix, float[] vec, float[] output, int offset)
    {
        MulWithVec3_Position((Span<float>)matrix, vec, output, offset);
    }

    public static void MulWithVec3_Position(Span<float> matrix, float[] vec, float[] output, int offset)
    {
        float num = vec[offset];
        float num2 = vec[offset + 1];
        float num3 = vec[offset + 2];
        output[offset] = matrix[0] * num + matrix[4] * num2 + matrix[8] * num3 + matrix[12];
        output[offset + 1] = matrix[1] * num + matrix[5] * num2 + matrix[9] * num3 + matrix[13];
        output[offset + 2] = matrix[2] * num + matrix[6] * num2 + matrix[10] * num3 + matrix[14];
    }

    public static void MulWithVec3_Position_AndScale(float[] matrix, float[] vec, float[] output, int offset, float scaleFactor)
    {
        float num = (vec[offset] - 0.5f) * scaleFactor + 0.5f;
        float num2 = vec[offset + 1] * scaleFactor;
        float num3 = (vec[offset + 2] - 0.5f) * scaleFactor + 0.5f;
        output[offset] = matrix[0] * num + matrix[4] * num2 + matrix[8] * num3 + matrix[12];
        output[offset + 1] = matrix[1] * num + matrix[5] * num2 + matrix[9] * num3 + matrix[13];
        output[offset + 2] = matrix[2] * num + matrix[6] * num2 + matrix[10] * num3 + matrix[14];
    }

    public static void MulWithVec3_Position_AndScaleXY(float[] matrix, float[] vec, float[] output, int offset, float scaleFactor)
    {
        float num = (vec[offset] - 0.5f) * scaleFactor + 0.5f;
        float num2 = vec[offset + 1];
        float num3 = (vec[offset + 2] - 0.5f) * scaleFactor + 0.5f;
        output[offset] = matrix[0] * num + matrix[4] * num2 + matrix[8] * num3 + matrix[12];
        output[offset + 1] = matrix[1] * num + matrix[5] * num2 + matrix[9] * num3 + matrix[13];
        output[offset + 2] = matrix[2] * num + matrix[6] * num2 + matrix[10] * num3 + matrix[14];
    }

    //
    // Summary:
    //     Used for vec3 representing an x,y,z position - as a vec4 this would have the
    //     4th element set to 1, so that applying a matrix transform with a translation
    //     would have an effect The offset is used to index within the original and output
    //     arrays - e.g. in MeshData.xyz The origin is the origin for the rotation
    public static void MulWithVec3_Position_WithOrigin(float[] matrix, float[] vec, float[] output, int offset, Vec3f origin)
    {
        MulWithVec3_Position_WithOrigin((Span<float>)matrix, vec, output, offset, origin);
    }

    public static void MulWithVec3_Position_WithOrigin(Span<float> matrix, float[] vec, float[] output, int offset, Vec3f origin)
    {
        float num = vec[offset] - origin.X;
        float num2 = vec[offset + 1] - origin.Y;
        float num3 = vec[offset + 2] - origin.Z;
        output[offset] = origin.X + matrix[0] * num + matrix[4] * num2 + matrix[8] * num3 + matrix[12];
        output[offset + 1] = origin.Y + matrix[1] * num + matrix[5] * num2 + matrix[9] * num3 + matrix[13];
        output[offset + 2] = origin.Z + matrix[2] * num + matrix[6] * num2 + matrix[10] * num3 + matrix[14];
    }

    //
    // Summary:
    //     Used for vec3 representing an x,y,z position - as a vec4 this would have the
    //     4th element set to 1, so that applying a matrix transform with a translation
    //     would have an effect
    public static void MulWithVec3_Position(float[] matrix, float x, float y, float z, Vec3f output)
    {
        output.X = matrix[0] * x + matrix[4] * y + matrix[8] * z + matrix[12];
        output.Y = matrix[1] * x + matrix[5] * y + matrix[9] * z + matrix[13];
        output.Z = matrix[2] * x + matrix[6] * y + matrix[10] * z + matrix[14];
    }

    //
    // Summary:
    //     Used for Vec3f representing a direction or normal - as a vec4 this would have
    //     the 4th element set to 0, so that applying a matrix transform with a translation
    //     would have *no* effect
    public static void MulWithVec3(float[] matrix, Vec3f vec, Vec3f output)
    {
        output.X = matrix[0] * vec.X + matrix[4] * vec.Y + matrix[8] * vec.Z;
        output.Y = matrix[1] * vec.X + matrix[5] * vec.Y + matrix[9] * vec.Z;
        output.Z = matrix[2] * vec.X + matrix[6] * vec.Y + matrix[10] * vec.Z;
    }

    //
    // Summary:
    //     Used for x,y,z representing a direction or normal - as a vec4 this would have
    //     the 4th element set to 0, so that applying a matrix transform with a translation
    //     would have *no* effect
    public static FastVec3f MulWithVec3(float[] matrix, float x, float y, float z)
    {
        float x2 = matrix[0] * x + matrix[4] * y + matrix[8] * z;
        float y2 = matrix[1] * x + matrix[5] * y + matrix[9] * z;
        float z2 = matrix[2] * x + matrix[6] * y + matrix[10] * z;
        return new FastVec3f(x2, y2, z2);
    }

    public static BlockFacing MulWithVec3_BlockFacing(float[] matrix, Vec3f vec)
    {
        return MulWithVec3_BlockFacing((Span<float>)matrix, vec);
    }

    public static BlockFacing MulWithVec3_BlockFacing(Span<float> matrix, Vec3f vec)
    {
        float num = matrix[0] * vec.X + matrix[4] * vec.Y + matrix[8] * vec.Z;
        float num2 = matrix[1] * vec.X + matrix[5] * vec.Y + matrix[9] * vec.Z;
        float num3 = matrix[2] * vec.X + matrix[6] * vec.Y + matrix[10] * vec.Z;
        return BlockFacing.FromVector(num, num2, num3);
    }

    public static double[] MulWithVec4(float[] matrix, double[] vec4)
    {
        double[] array = new double[4];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                array[i] += (double)matrix[4 * j + i] * vec4[j];
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
    public static void MulWithVec4(float[] matrix, float[] vec4, Vec4f outVal)
    {
        outVal.Set(0f, 0f, 0f, 0f);
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
    public static void MulWithVec4(float[] matrix, Vec4d inVal, Vec4d outVal)
    {
        outVal.Set(0.0, 0.0, 0.0, 0.0);
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                outVal[i] += (double)matrix[4 * j + i] * inVal[j];
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
    public static void MulWithVec4(float[] matrix, Vec4f inVal, Vec4f outVal)
    {
        outVal.Set(0f, 0f, 0f, 0f);
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                outVal[i] += matrix[4 * j + i] * inVal[j];
            }
        }
    }

    public static void ExtractEulerAngles(float[] m, ref float thetaX, ref float thetaY, ref float thetaZ)
    {
        float num = m[8];
        if (Math.Abs(num) == 1f)
        {
            thetaX = num * (float)Math.Atan2(m[1], m[5]);
            thetaY = num * (MathF.PI / 2f);
            thetaZ = 0f;
        }
        else
        {
            thetaX = (float)Math.Atan2(0f - m[9], m[10]);
            thetaY = GameMath.Asin(num);
            thetaZ = (float)Math.Atan2(0f - m[4], m[0]);
        }
    }
}
#if false // Decompilation log
'181' items in cache
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
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
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
