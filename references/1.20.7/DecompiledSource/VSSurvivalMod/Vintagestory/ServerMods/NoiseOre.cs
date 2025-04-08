using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class NoiseOre : NoiseBase
{
	public NoiseOre(long worldSeed)
		: base(worldSeed)
	{
	}

	public int GetOreAt(int posX, int posZ)
	{
		InitPositionSeed(posX, posZ);
		return GetRandomOre();
	}

	public int GetLerpedOreValueAt(double posX, double posZ, int[] oreCache, int sizeX, float contrastMul, float sub)
	{
		int posXInt = (int)posX;
		int posZInt = (int)posZ;
		byte val = GameMath.BiSerpByte((float)(posX - (double)posXInt), (float)(posZ - (double)posZInt), 0, oreCache[posZInt * sizeX + posXInt], oreCache[posZInt * sizeX + posXInt + 1], oreCache[(posZInt + 1) * sizeX + posXInt], oreCache[(posZInt + 1) * sizeX + posXInt + 1]);
		val = (byte)GameMath.Clamp(((float)(int)val - 128f) * contrastMul + 128f - sub, 0f, 255f);
		int richness = Math.Max(oreCache[(posZInt + 1) * sizeX + posXInt + 1] & 0xFF0000, Math.Max(oreCache[(posZInt + 1) * sizeX + posXInt] & 0xFF0000, Math.Max(oreCache[posZInt * sizeX + posXInt] & 0xFF0000, oreCache[posZInt * sizeX + posXInt + 1] & 0xFF0000)));
		int hypercommon = Math.Max(oreCache[(posZInt + 1) * sizeX + posXInt + 1] & 0xFF00, Math.Max(oreCache[(posZInt + 1) * sizeX + posXInt] & 0xFF00, Math.Max(oreCache[posZInt * sizeX + posXInt] & 0xFF00, oreCache[posZInt * sizeX + posXInt + 1] & 0xFF00)));
		if (val != 0)
		{
			return val | richness | hypercommon;
		}
		return 0;
	}

	private int GetRandomOre()
	{
		int rnd = NextInt(1024);
		int quality = ((NextInt(2) > 0) ? 1 : ((NextInt(50) <= 15) ? 2 : 0)) << 10;
		if (rnd < 1)
		{
			return quality | 0x100 | 0xFF;
		}
		if (rnd < 30)
		{
			return quality | 0xFF;
		}
		if (rnd < 105)
		{
			return quality | (75 + NextInt(100));
		}
		if (rnd < 190)
		{
			return quality | (20 + NextInt(20));
		}
		return 0;
	}
}
