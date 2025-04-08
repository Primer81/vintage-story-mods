using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class Unproject
{
	private double[] finalMatrix;

	private double[] inp;

	private double[] out_;

	public Unproject()
	{
		finalMatrix = Mat4d.Create();
		inp = new double[4];
		out_ = new double[4];
	}

	public bool UnProject(int winX, int winY, int winZ, double[] model, double[] proj, double[] view, double[] objPos)
	{
		inp[0] = winX;
		inp[1] = winY;
		inp[2] = winZ;
		inp[3] = 1.0;
		Mat4d.Multiply(finalMatrix, proj, model);
		Mat4d.Invert(finalMatrix, finalMatrix);
		inp[0] = (inp[0] - view[0]) / view[2];
		inp[1] = (inp[1] - view[1]) / view[3];
		inp[0] = inp[0] * 2.0 - 1.0;
		inp[1] = inp[1] * 2.0 - 1.0;
		inp[2] = inp[2] * 2.0 - 1.0;
		MultMatrixVec(finalMatrix, inp, out_);
		if (out_[3] == 0.0)
		{
			return false;
		}
		out_[0] /= out_[3];
		out_[1] /= out_[3];
		out_[2] /= out_[3];
		objPos[0] = out_[0];
		objPos[1] = out_[1];
		objPos[2] = out_[2];
		return true;
	}

	private void MultMatrixVec(double[] matrix, double[] inp__, double[] out__)
	{
		for (int i = 0; i < 4; i++)
		{
			out__[i] = inp__[0] * matrix[i] + inp__[1] * matrix[4 + i] + inp__[2] * matrix[8 + i] + inp__[3] * matrix[12 + i];
		}
	}
}
