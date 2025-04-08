using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class CubeFaceVertices
{
	public static Vec3iAndFacingFlags[][] blockFaceVerticesCentered;

	public static FastVec3f[][] blockFaceVertices;

	public static Vec3iAndFacingFlags[][] blockFaceVerticesCenteredDiv2;

	public static FastVec3f[][] blockFaceVerticesDiv2;

	static CubeFaceVertices()
	{
		Vec3iAndFacingFlags.Initialize(34);
		Init(0.5f, 0.5f, out blockFaceVerticesCentered, out blockFaceVertices);
		Init(1f, 0.5f, out blockFaceVerticesCenteredDiv2, out blockFaceVerticesDiv2);
	}

	public static void Init(float horMul, float vertMul, out Vec3iAndFacingFlags[][] bfVerticesCentered, out FastVec3f[][] bfVertices)
	{
		bfVerticesCentered = new Vec3iAndFacingFlags[6][];
		for (int j = 0; j < 6; j++)
		{
			bfVerticesCentered[j] = new Vec3iAndFacingFlags[9];
		}
		int l = BlockFacing.NORTH.Flag;
		int e = BlockFacing.EAST.Flag;
		int s = BlockFacing.SOUTH.Flag;
		int w = BlockFacing.WEST.Flag;
		int u = BlockFacing.UP.Flag;
		int d = BlockFacing.DOWN.Flag;
		int es = e | s;
		int sw = s | w;
		int ne = l | e;
		int nw = l | w;
		int sd = s | d;
		int nd = l | d;
		int su = s | u;
		int nu = l | u;
		int wd = w | d;
		int ed = e | d;
		int wu = w | u;
		int eu = e | u;
		bfVerticesCentered[4][8] = new Vec3iAndFacingFlags(0, 1, 0, d, u);
		bfVerticesCentered[4][0] = new Vec3iAndFacingFlags(0, 1, 1, s, l, ne, nw);
		bfVerticesCentered[4][1] = new Vec3iAndFacingFlags(0, 1, -1, l, s, es, sw);
		bfVerticesCentered[4][2] = new Vec3iAndFacingFlags(1, 1, 0, e, w, sw, nw);
		bfVerticesCentered[4][3] = new Vec3iAndFacingFlags(-1, 1, 0, w, e, es, ne);
		bfVerticesCentered[4][4] = new Vec3iAndFacingFlags(1, 1, 1, es, nw);
		bfVerticesCentered[4][5] = new Vec3iAndFacingFlags(-1, 1, 1, sw, ne);
		bfVerticesCentered[4][6] = new Vec3iAndFacingFlags(1, 1, -1, ne, sw);
		bfVerticesCentered[4][7] = new Vec3iAndFacingFlags(-1, 1, -1, nw, es);
		bfVerticesCentered[1][8] = new Vec3iAndFacingFlags(1, 0, 0, w, e);
		bfVerticesCentered[1][0] = new Vec3iAndFacingFlags(1, 1, 0, u, d, nd, sd);
		bfVerticesCentered[1][1] = new Vec3iAndFacingFlags(1, -1, 0, d, u, nu, su);
		bfVerticesCentered[1][2] = new Vec3iAndFacingFlags(1, 0, 1, s, l, nu, nd);
		bfVerticesCentered[1][3] = new Vec3iAndFacingFlags(1, 0, -1, l, s, su, sd);
		bfVerticesCentered[1][4] = new Vec3iAndFacingFlags(1, 1, 1, su, nd);
		bfVerticesCentered[1][5] = new Vec3iAndFacingFlags(1, 1, -1, nu, sd);
		bfVerticesCentered[1][6] = new Vec3iAndFacingFlags(1, -1, 1, sd, nu);
		bfVerticesCentered[1][7] = new Vec3iAndFacingFlags(1, -1, -1, nd, su);
		bfVerticesCentered[5][8] = new Vec3iAndFacingFlags(0, -1, 0, u, d);
		bfVerticesCentered[5][0] = new Vec3iAndFacingFlags(0, -1, -1, l, s, es, sw);
		bfVerticesCentered[5][1] = new Vec3iAndFacingFlags(0, -1, 1, s, l, ne, nw);
		bfVerticesCentered[5][2] = new Vec3iAndFacingFlags(1, -1, 0, e, w, nw, sw);
		bfVerticesCentered[5][3] = new Vec3iAndFacingFlags(-1, -1, 0, w, e, ne, es);
		bfVerticesCentered[5][4] = new Vec3iAndFacingFlags(1, -1, -1, ne, sw);
		bfVerticesCentered[5][5] = new Vec3iAndFacingFlags(-1, -1, -1, nw, es);
		bfVerticesCentered[5][6] = new Vec3iAndFacingFlags(1, -1, 1, es, nw);
		bfVerticesCentered[5][7] = new Vec3iAndFacingFlags(-1, -1, 1, sw, ne);
		bfVerticesCentered[3][8] = new Vec3iAndFacingFlags(-1, 0, 0, e, w);
		bfVerticesCentered[3][0] = new Vec3iAndFacingFlags(-1, 1, 0, u, d, sd, nd);
		bfVerticesCentered[3][1] = new Vec3iAndFacingFlags(-1, -1, 0, d, u, su, nu);
		bfVerticesCentered[3][2] = new Vec3iAndFacingFlags(-1, 0, -1, l, s, su, sd);
		bfVerticesCentered[3][3] = new Vec3iAndFacingFlags(-1, 0, 1, s, l, nu, nd);
		bfVerticesCentered[3][4] = new Vec3iAndFacingFlags(-1, 1, -1, nu, sd);
		bfVerticesCentered[3][5] = new Vec3iAndFacingFlags(-1, 1, 1, su, nd);
		bfVerticesCentered[3][6] = new Vec3iAndFacingFlags(-1, -1, -1, nd, su);
		bfVerticesCentered[3][7] = new Vec3iAndFacingFlags(-1, -1, 1, sd, nu);
		bfVerticesCentered[2][8] = new Vec3iAndFacingFlags(0, 0, 1, l, s);
		bfVerticesCentered[2][0] = new Vec3iAndFacingFlags(0, 1, 1, u, d, ed, wd);
		bfVerticesCentered[2][1] = new Vec3iAndFacingFlags(0, -1, 1, d, u, eu, wu);
		bfVerticesCentered[2][2] = new Vec3iAndFacingFlags(-1, 0, 1, w, e, eu, ed);
		bfVerticesCentered[2][3] = new Vec3iAndFacingFlags(1, 0, 1, e, w, wu, wd);
		bfVerticesCentered[2][4] = new Vec3iAndFacingFlags(-1, 1, 1, wu, ed);
		bfVerticesCentered[2][5] = new Vec3iAndFacingFlags(1, 1, 1, eu, wd);
		bfVerticesCentered[2][6] = new Vec3iAndFacingFlags(-1, -1, 1, wd, eu);
		bfVerticesCentered[2][7] = new Vec3iAndFacingFlags(1, -1, 1, ed, wu);
		bfVerticesCentered[0][8] = new Vec3iAndFacingFlags(0, 0, -1, s, l);
		bfVerticesCentered[0][0] = new Vec3iAndFacingFlags(0, 1, -1, u, d, wd, ed);
		bfVerticesCentered[0][1] = new Vec3iAndFacingFlags(0, -1, -1, d, u, wu, eu);
		bfVerticesCentered[0][2] = new Vec3iAndFacingFlags(1, 0, -1, e, w, wu, wd);
		bfVerticesCentered[0][3] = new Vec3iAndFacingFlags(-1, 0, -1, w, e, eu, ed);
		bfVerticesCentered[0][4] = new Vec3iAndFacingFlags(1, 1, -1, eu, wd);
		bfVerticesCentered[0][5] = new Vec3iAndFacingFlags(-1, 1, -1, wu, ed);
		bfVerticesCentered[0][6] = new Vec3iAndFacingFlags(1, -1, -1, ed, wu);
		bfVerticesCentered[0][7] = new Vec3iAndFacingFlags(-1, -1, -1, wd, eu);
		bfVertices = new FastVec3f[6][];
		for (int i = 0; i < bfVerticesCentered.Length; i++)
		{
			bfVertices[i] = new FastVec3f[bfVerticesCentered[i].Length];
			for (int k = 0; k < bfVerticesCentered[i].Length; k++)
			{
				Vec3iAndFacingFlags v = bfVerticesCentered[i][k];
				bfVertices[i][k] = new FastVec3f((float)(v.X + 1) * horMul, (float)(v.Y + 1) * vertMul, (float)(v.Z + 1) * horMul);
			}
		}
	}
}
