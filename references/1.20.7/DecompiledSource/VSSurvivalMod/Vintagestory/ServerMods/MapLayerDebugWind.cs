using System;

namespace Vintagestory.ServerMods;

internal class MapLayerDebugWind : MapLayerBase
{
	private NoiseWind windmap;

	public MapLayerDebugWind(long seed)
		: base(seed)
	{
		windmap = new NoiseWind(seed);
	}

	public override int[] GenLayer(int xCoord, int zCoord, int sizeX, int sizeZ)
	{
		int[] result = new int[sizeX * sizeZ];
		int drawLatticeSize = 16;
		float windZoom = 128f;
		for (int x = 0; x < sizeX + drawLatticeSize; x += drawLatticeSize)
		{
			for (int z = 0; z < sizeZ + drawLatticeSize; z += drawLatticeSize)
			{
				PolarVector vec = windmap.getWindAt(((float)xCoord + (float)x) / windZoom, ((float)zCoord + (float)z) / windZoom);
				int dx = (int)((double)vec.length * Math.Cos(vec.angle));
				int dz = (int)((double)vec.length * Math.Sin(vec.angle));
				plotLine(result, sizeX, x, z, x + dx, z + dz);
				if (x < sizeX && z < sizeZ)
				{
					result[z * sizeX + x] = 16711680;
				}
			}
		}
		return result;
	}

	private void plotLine(int[] map, int sizeX, int x0, int y0, int x1, int y1)
	{
		int dx = Math.Abs(x1 - x0);
		int sx = ((x0 < x1) ? 1 : (-1));
		int dy = -Math.Abs(y1 - y0);
		int sy = ((y0 < y1) ? 1 : (-1));
		int err = dx + dy;
		while (true)
		{
			if (x0 >= 0 && x0 < sizeX && y0 >= 0 && y0 < sizeX)
			{
				map[y0 * sizeX + x0] = 7895160;
			}
			if (x0 != x1 || y0 != y1)
			{
				int num = 2 * err;
				if (num >= dy)
				{
					err += dy;
					x0 += sx;
				}
				if (num <= dx)
				{
					err += dx;
					y0 += sy;
				}
				continue;
			}
			break;
		}
	}
}
