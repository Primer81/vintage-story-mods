using SkiaSharp;
using Vintagestory.API.Util;
using Vintagestory.ServerMods.NoObf;

namespace Vintagestory.ServerMods;

public abstract class NoiseBase
{
	public static bool Debug;

	public static int DebugXCoord;

	public static int DebugZCoord;

	internal long worldSeed;

	internal long mapGenSeed;

	internal long currentSeed;

	public NoiseBase(long worldSeed)
	{
		this.worldSeed = worldSeed;
		currentSeed = mapGenSeed;
		currentSeed = currentSeed * 6364136223846793005L + 1442695040888963407L;
		mapGenSeed = worldSeed;
		mapGenSeed *= worldSeed * 6364136223846793005L + 1442695040888963407L;
		mapGenSeed++;
		mapGenSeed *= worldSeed * 6364136223846793005L + 1442695040888963407L;
		mapGenSeed += 2L;
		mapGenSeed *= worldSeed * 6364136223846793005L + 1442695040888963407L;
		mapGenSeed += 3L;
	}

	public void InitPositionSeed(int xPos, int zPos)
	{
		currentSeed = mapGenSeed;
		currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
		currentSeed += xPos;
		currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
		currentSeed += zPos;
		currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
		currentSeed += xPos;
		currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
		currentSeed += zPos;
	}

	public int NextInt(int max)
	{
		int r = (int)((currentSeed >> 24) % max);
		if (r < 0)
		{
			r += max;
		}
		currentSeed *= currentSeed * 6364136223846793005L + 1442695040888963407L;
		currentSeed += mapGenSeed;
		return r;
	}

	public int NextIntFast(int mask)
	{
		int result = (int)(currentSeed & mask);
		currentSeed = currentSeed * 6364136223846793005L + 1442695040888963407L;
		return result;
	}

	public static void DebugDrawBitmap(DebugDrawMode mode, int[] values, int size, string name)
	{
		DebugDrawBitmap(mode, values, size, size, name);
	}

	public static void DebugDrawBitmap(DebugDrawMode mode, int[] values, int sizeX, int sizeZ, string name)
	{
		if (!Debug)
		{
			return;
		}
		SKBitmap bitmap = new SKBitmap(sizeX, sizeZ);
		for (int i = 0; i < sizeX; i++)
		{
			for (int j = 0; j < sizeZ; j++)
			{
				int num = values[j * sizeX + i];
				if (mode == DebugDrawMode.FirstByteGrayscale)
				{
					int num2 = num & 0xFF;
					num = num2 | (num2 << 8) | (num2 << 16);
				}
				if (mode == DebugDrawMode.LandformRGB)
				{
					LandformVariant[] landFormsByIndex = NoiseLandforms.landforms.LandFormsByIndex;
					if (landFormsByIndex.Length > num)
					{
						num = landFormsByIndex[num].ColorInt;
					}
				}
				if (mode == DebugDrawMode.ProvinceRGB)
				{
					GeologicProvinceVariant[] variants = NoiseGeoProvince.provinces.Variants;
					if (variants.Length > num)
					{
						num = variants[num].ColorInt;
					}
				}
				bitmap.SetPixel(i, j, new SKColor((byte)((uint)(num >> 16) & 0xFFu), (byte)((uint)(num >> 8) & 0xFFu), (byte)((uint)num & 0xFFu)));
			}
		}
		bitmap.Save("map-" + name + ".png");
	}

	public static int[] CutMargins(int[] inInts, int sizeX, int sizeZ, int margin)
	{
		int[] resultInts = new int[(sizeX - 2 * margin) * (sizeZ - 2 * margin)];
		int j = 0;
		for (int i = 0; i < inInts.Length; i++)
		{
			int xpos = i % sizeX;
			int zpos = i / sizeX;
			if (xpos >= margin && xpos < sizeX - margin && zpos >= margin && zpos < sizeZ - margin)
			{
				resultInts[j++] = inInts[i];
			}
		}
		return resultInts;
	}

	public int[] CutRightAndBottom(int[] inInts, int sizeX, int sizeZ, int margin)
	{
		int[] resultInts = new int[(sizeX - margin) * (sizeZ - margin)];
		int j = 0;
		for (int i = 0; i < inInts.Length; i++)
		{
			int num = i % sizeX;
			int zpos = i / sizeX;
			if (num < sizeX - margin && zpos < sizeZ - margin)
			{
				resultInts[j++] = inInts[i];
			}
		}
		return resultInts;
	}
}
