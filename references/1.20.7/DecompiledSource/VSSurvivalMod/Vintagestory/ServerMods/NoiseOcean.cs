using System.Collections.Generic;

namespace Vintagestory.ServerMods;

internal class NoiseOcean : NoiseBase
{
	private float landcover;

	private List<XZ> requireLandAt;

	private float scale;

	public NoiseOcean(long seed, float scale, float landcover)
		: base(seed)
	{
		this.landcover = landcover;
		this.scale = scale;
	}

	public int GetOceanIndexAt(int unscaledXpos, int unscaledZpos)
	{
		int xpos = (int)((float)unscaledXpos / scale);
		int zpos = (int)((float)unscaledZpos / scale);
		InitPositionSeed(xpos, zpos);
		if ((double)NextInt(10000) / 10000.0 < (double)landcover)
		{
			return 0;
		}
		return 255;
	}
}
