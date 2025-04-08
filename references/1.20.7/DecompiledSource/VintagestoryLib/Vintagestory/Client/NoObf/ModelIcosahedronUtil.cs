using System;
using Vintagestory.API.Client;

namespace Vintagestory.Client.NoObf;

public class ModelIcosahedronUtil
{
	public static uint white = uint.MaxValue;

	public static double X = 0.525731086730957;

	public static double Z = 0.8506507873535156;

	public static double[][] vdata = new double[12][]
	{
		new double[3]
		{
			0.0 - X,
			0.0,
			Z
		},
		new double[3] { X, 0.0, Z },
		new double[3]
		{
			0.0 - X,
			0.0,
			0.0 - Z
		},
		new double[3]
		{
			X,
			0.0,
			0.0 - Z
		},
		new double[3] { 0.0, Z, X },
		new double[3]
		{
			0.0,
			Z,
			0.0 - X
		},
		new double[3]
		{
			0.0,
			0.0 - Z,
			X
		},
		new double[3]
		{
			0.0,
			0.0 - Z,
			0.0 - X
		},
		new double[3] { Z, X, 0.0 },
		new double[3]
		{
			0.0 - Z,
			X,
			0.0
		},
		new double[3]
		{
			Z,
			0.0 - X,
			0.0
		},
		new double[3]
		{
			0.0 - Z,
			0.0 - X,
			0.0
		}
	};

	public static int[][] tindx = new int[20][]
	{
		new int[3] { 0, 4, 1 },
		new int[3] { 0, 9, 4 },
		new int[3] { 9, 5, 4 },
		new int[3] { 4, 5, 8 },
		new int[3] { 4, 8, 1 },
		new int[3] { 8, 10, 1 },
		new int[3] { 8, 3, 10 },
		new int[3] { 5, 3, 8 },
		new int[3] { 5, 2, 3 },
		new int[3] { 2, 7, 3 },
		new int[3] { 7, 10, 3 },
		new int[3] { 7, 6, 10 },
		new int[3] { 7, 11, 6 },
		new int[3] { 11, 0, 6 },
		new int[3] { 0, 1, 6 },
		new int[3] { 6, 1, 10 },
		new int[3] { 9, 0, 11 },
		new int[3] { 9, 11, 2 },
		new int[3] { 9, 2, 5 },
		new int[3] { 7, 2, 11 }
	};

	public static MeshData genIcosahedron(int depth, float radius)
	{
		MeshData modeldata = new MeshData(10, 10);
		int index = 0;
		for (int i = 0; i < tindx.Length; i++)
		{
			subdivide(modeldata, ref index, vdata[tindx[i][0]], vdata[tindx[i][1]], vdata[tindx[i][2]], depth, radius);
		}
		return modeldata;
	}

	private static void subdivide(MeshData modeldata, ref int index, double[] vA0, double[] vB1, double[] vC2, int depth, float radius)
	{
		double[] vAB = new double[3];
		double[] vBC = new double[3];
		double[] vCA = new double[3];
		if (depth == 0)
		{
			addTriangle(modeldata, ref index, vA0, vB1, vC2, radius);
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			vAB[i] = (vA0[i] + vB1[i]) / 2.0;
			vBC[i] = (vB1[i] + vC2[i]) / 2.0;
			vCA[i] = (vC2[i] + vA0[i]) / 2.0;
		}
		double modAB = mod(vAB);
		double modBC = mod(vBC);
		double modCA = mod(vCA);
		for (int i = 0; i < 3; i++)
		{
			vAB[i] /= modAB;
			vBC[i] /= modBC;
			vCA[i] /= modCA;
		}
		subdivide(modeldata, ref index, vA0, vAB, vCA, depth - 1, radius);
		subdivide(modeldata, ref index, vB1, vBC, vAB, depth - 1, radius);
		subdivide(modeldata, ref index, vC2, vCA, vBC, depth - 1, radius);
		subdivide(modeldata, ref index, vAB, vBC, vCA, depth - 1, radius);
	}

	public static double mod(double[] v)
	{
		return Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);
	}

	private static double[] calcTextureMap(double[] vtx)
	{
		double[] ret = new double[3];
		ret[0] = Math.Sqrt(vtx[0] * vtx[0] + vtx[1] * vtx[1] + vtx[2] * vtx[2]);
		ret[1] = Math.Acos(vtx[2] / ret[0]);
		ret[2] = Math.Atan2(vtx[1], vtx[0]);
		ret[1] += Math.PI;
		ret[1] /= Math.PI * 2.0;
		ret[2] += Math.PI;
		ret[2] /= Math.PI * 2.0;
		return ret;
	}

	private static void addTriangle(MeshData modeldata, ref int index, double[] v1, double[] v2, double[] v3, float radius)
	{
		double[] spherical = calcTextureMap(v1);
		modeldata.AddVertex((float)((double)radius * v1[0]), (float)((double)radius * v1[1]), (float)((double)radius * v1[2]), (float)spherical[1], (float)spherical[2], (int)white);
		modeldata.AddIndex(index++);
		spherical = calcTextureMap(v2);
		modeldata.AddVertex((float)((double)radius * v2[0]), (float)((double)radius * v2[1]), (float)((double)radius * v2[2]), (float)spherical[1], (float)spherical[2], (int)white);
		modeldata.AddIndex(index++);
		spherical = calcTextureMap(v3);
		modeldata.AddVertex((float)((double)radius * v3[0]), (float)((double)radius * v3[1]), (float)((double)radius * v3[2]), (float)spherical[1], (float)spherical[2], (int)white);
		modeldata.AddIndex(index++);
	}
}
