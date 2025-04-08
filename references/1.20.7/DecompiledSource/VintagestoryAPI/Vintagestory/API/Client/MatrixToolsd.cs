using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class MatrixToolsd
{
	public static Vec3d Project(Vec3d pos, double[] projection, double[] view, int viewportWidth, int viewportHeight)
	{
		double[] array = new double[16];
		Mat4d.Mul(array, projection, view);
		double[] outpos = Mat4d.MulWithVec4(array, new double[4] { pos.X, pos.Y, pos.Z, 1.0 });
		return new Vec3d((outpos[0] / outpos[3] + 1.0) * (double)(viewportWidth / 2), (outpos[1] / outpos[3] + 1.0) * (double)(viewportHeight / 2), outpos[2]);
	}

	public static void MatFollowPlayer(double[] m)
	{
		m[12] = 0.0;
		m[13] = 0.0;
		m[14] = 0.0;
	}

	public static void LoadPlayerFacingMatrix(double[] m)
	{
		double d = (m[0] = GameMath.Sqrt(m[0] * m[0] + m[1] * m[1] + m[2] * m[2]));
		m[1] = 0.0;
		m[2] = 0.0;
		m[3] = 0.0;
		m[4] = 0.0;
		m[5] = d;
		m[6] = 0.0;
		m[7] = 0.0;
		m[8] = 0.0;
		m[9] = 0.0;
		m[10] = d;
		m[11] = 0.0;
		m[12] = m[12];
		m[13] = m[13];
		m[14] = m[14];
		m[15] = 1.0;
		Mat4d.RotateX(m, m, 3.1415927410125732);
	}

	public static void MatFacePlayer(double[] m)
	{
		double d = (m[0] = GameMath.Sqrt(m[0] * m[0] + m[1] * m[1] + m[2] * m[2]));
		m[1] = 0.0;
		m[2] = 0.0;
		m[3] = 0.0;
		m[4] = 0.0;
		m[5] = d;
		m[6] = 0.0;
		m[7] = 0.0;
		m[8] = 0.0;
		m[9] = 0.0;
		m[10] = d;
		m[11] = 0.0;
		m[12] = m[12];
		m[13] = m[13];
		m[14] = m[14];
		m[15] = 1.0;
		Mat4d.RotateX(m, m, 3.1415927410125732);
	}
}
