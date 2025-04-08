using System;
using System.Collections.Generic;
using System.Linq;

namespace Vintagestory.API.MathTools;

public static class ShapeUtil
{
	private static Vec3f[][] cubicShellNormalizedVectors;

	public static int MaxShells;

	static ShapeUtil()
	{
		MaxShells = 38;
		cubicShellNormalizedVectors = new Vec3f[MaxShells][];
		int[] ab = new int[2];
		for (int r = 1; r < MaxShells; r++)
		{
			cubicShellNormalizedVectors[r] = new Vec3f[(2 * r + 1) * (2 * r + 1) * 6];
			int i = 0;
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing facing in aLLFACES)
			{
				ab[0] = -r;
				while (ab[0] <= r)
				{
					ab[1] = -r;
					while (ab[1] <= r)
					{
						Vec3f pos = new Vec3f(facing.Normali.X * r, facing.Normali.Y * r, facing.Normali.Z * r);
						int j = 0;
						if (pos.X == 0f)
						{
							pos.X = ab[j++];
						}
						if (pos.Y == 0f)
						{
							pos.Y = ab[j++];
						}
						if (j < 2 && pos.Z == 0f)
						{
							pos.Z = ab[j++];
						}
						cubicShellNormalizedVectors[r][i++] = pos.Normalize();
						ab[1]++;
					}
					ab[0]++;
				}
			}
		}
	}

	public static Vec3f[] GetCachedCubicShellNormalizedVectors(int radius)
	{
		return cubicShellNormalizedVectors[radius];
	}

	public static Vec3i[] GenCubicShellVectors(int r)
	{
		int[] ab = new int[2];
		Vec3i[] vectors = new Vec3i[(2 * r + 1) * (2 * r + 1) * 6];
		int i = 0;
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			ab[0] = -r;
			while (ab[0] <= r)
			{
				ab[1] = -r;
				while (ab[1] <= r)
				{
					Vec3i pos = new Vec3i(facing.Normali.X * r, facing.Normali.Y * r, facing.Normali.Z * r);
					int j = 0;
					if (pos.X == 0)
					{
						pos.X = ab[j++];
					}
					if (pos.Y == 0)
					{
						pos.Y = ab[j++];
					}
					if (j < 2 && pos.Z == 0)
					{
						pos.Z = ab[j++];
					}
					vectors[i++] = pos;
					ab[1]++;
				}
				ab[0]++;
			}
		}
		return vectors;
	}

	/// <summary>
	/// Returns an array of vectors for each point in a square, sorted by manhatten distance to center, exluding the center point
	/// </summary>
	/// <param name="halflength"></param>
	/// <returns></returns>
	public static Vec2i[] GetSquarePointsSortedByMDist(int halflength)
	{
		if (halflength == 0)
		{
			return new Vec2i[0];
		}
		Vec2i[] result = new Vec2i[(2 * halflength + 1) * (2 * halflength + 1) - 1];
		int i = 0;
		for (int x = -halflength; x <= halflength; x++)
		{
			for (int y = -halflength; y <= halflength; y++)
			{
				if (x != 0 || y != 0)
				{
					result[i++] = new Vec2i(x, y);
				}
			}
		}
		return result.OrderBy((Vec2i vec) => vec.ManhattenDistance(0, 0)).ToArray();
	}

	/// <summary>
	/// Returns a square outline of given radius (only for odd lengths)
	/// </summary>
	/// <param name="halflength"></param>
	/// <returns></returns>
	public static Vec2i[] GetHollowSquarePoints(int halflength)
	{
		if (halflength == 0)
		{
			return new Vec2i[0];
		}
		int radius = halflength * 2 + 1;
		Vec2i[] result = new Vec2i[radius * 4 - 4];
		int j = 0;
		for (int i = 0; i < radius * 4 - 1; i++)
		{
			int x = i % radius - halflength;
			int y = i % radius - halflength;
			int quadrant = i / radius;
			switch (quadrant)
			{
			case 0:
				y = -halflength;
				break;
			case 1:
				x = halflength;
				break;
			case 2:
				y = halflength;
				x = -x;
				break;
			case 3:
				x = -halflength;
				y = -y;
				break;
			}
			result[j++] = new Vec2i(x, y);
			if ((i + 1) / radius > quadrant)
			{
				i++;
			}
		}
		return result;
	}

	public static Vec2i[] GetOctagonPoints(int x, int y, int r)
	{
		if (r == 0)
		{
			return new Vec2i[1]
			{
				new Vec2i(x, y)
			};
		}
		List<Vec2i> points = new List<Vec2i>();
		int th = 9;
		int S = 2 * r;
		int a = Math.Min(S, th);
		int b = (int)Math.Ceiling((double)Math.Max(0, S - th) / 2.0);
		int a2 = a / 2;
		for (int j = 0; j < a; j++)
		{
			points.Add(new Vec2i(x + j - a2, y - r));
			points.Add(new Vec2i(x - j + a2, y + r));
			points.Add(new Vec2i(x - r, y - j + a2));
			points.Add(new Vec2i(x + r, y + j - a2));
		}
		for (int i = 0; i < b; i++)
		{
			points.Add(new Vec2i(x + a2 + i, y - r + i));
			points.Add(new Vec2i(x - r + i, y + a2 + i));
			points.Add(new Vec2i(x - r + i, y - a2 - i));
			points.Add(new Vec2i(x + a2 + i, y + r - i));
		}
		return Enumerable.ToArray(points);
	}

	public static void LoadOctagonIndices(ICollection<long> list, int x, int y, int r, int mapSizeX)
	{
		if (r == 0)
		{
			list.Add(MapUtil.Index2dL(x, y, mapSizeX));
			return;
		}
		int S = 2 * r;
		int a = Math.Min(S, 9);
		int b = (int)((double)Math.Max(0, S - 9) / Math.Sqrt(2.0));
		int a2 = a / 2;
		for (int j = 0; j < a; j++)
		{
			list.Add(MapUtil.Index2dL(x + j - a2, y - r, mapSizeX));
			list.Add(MapUtil.Index2dL(x - j + a2, y + r, mapSizeX));
			list.Add(MapUtil.Index2dL(x - r, y - j + a2, mapSizeX));
			list.Add(MapUtil.Index2dL(x + r, y + j - a2, mapSizeX));
		}
		for (int i = 0; i < b; i++)
		{
			list.Add(MapUtil.Index2dL(x + a2 + i, y - r + i, mapSizeX));
			list.Add(MapUtil.Index2dL(x - r + i, y + a2 + i, mapSizeX));
			list.Add(MapUtil.Index2dL(x - r + i, y - a2 - i, mapSizeX));
			list.Add(MapUtil.Index2dL(x + a2 + i, y + r - i, mapSizeX));
		}
	}

	public static Vec2i[] GetPointsOfCircle(int xm, int ym, int r)
	{
		List<Vec2i> points = new List<Vec2i>();
		int x = -r;
		int y = 0;
		int err = 2 - 2 * r;
		do
		{
			points.Add(new Vec2i(xm - x, ym + y));
			points.Add(new Vec2i(xm - y, ym - x));
			points.Add(new Vec2i(xm + x, ym - y));
			points.Add(new Vec2i(xm + y, ym + x));
			r = err;
			if (r <= y)
			{
				err += ++y * 2 + 1;
			}
			if (r > x || err > y)
			{
				err += ++x * 2 + 1;
			}
		}
		while (x < 0);
		return Enumerable.ToArray(points);
	}
}
