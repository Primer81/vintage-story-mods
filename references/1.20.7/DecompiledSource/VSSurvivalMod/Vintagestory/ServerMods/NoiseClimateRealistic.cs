using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class NoiseClimateRealistic : NoiseClimatePatchy
{
	private double halfRange;

	private float geologicActivityInv = 10f;

	public float GeologicActivityStrength
	{
		set
		{
			geologicActivityInv = 1f / value;
		}
	}

	public double ZOffset { get; private set; }

	public NoiseClimateRealistic(long seed, double mapsizeZ, int polarEquatorDistance, int spawnMinTemp, int spawnMaxTemp)
		: base(seed + 1)
	{
		halfRange = polarEquatorDistance / TerraGenConfig.climateMapScale / TerraGenConfig.climateMapSubScale;
		int minTempDescaled = Climate.DescaleTemperature(spawnMinTemp);
		int maxTempDescaled = Climate.DescaleTemperature(spawnMaxTemp);
		double rndTemp = minTempDescaled + NextInt(maxTempDescaled - minTempDescaled + 1);
		double zPerDegDescaled = halfRange / 255.0;
		ZOffset = rndTemp * zPerDegDescaled - mapsizeZ / 2.0;
	}

	public override int GetClimateAt(int posX, int posZ)
	{
		InitPositionSeed(posX, posZ);
		return GetRandomClimate(posX, posZ);
	}

	public override int GetLerpedClimateAt(double posX, double posZ)
	{
		int posXInt = (int)posX;
		int posZInt = (int)posZ;
		InitPositionSeed(posXInt, posZInt);
		int leftTop = GetRandomClimate(posX, posZ);
		InitPositionSeed(posXInt + 1, posZInt);
		int rightTop = GetRandomClimate(posX, posZ);
		InitPositionSeed(posXInt, posZInt + 1);
		int leftBottom = GetRandomClimate(posX, posZ);
		InitPositionSeed(posXInt + 1, posZInt + 1);
		int rightBottom = GetRandomClimate(posX, posZ);
		return GameMath.BiSerpRgbColor((float)(posX - (double)posXInt), (float)(posZ - (double)posZInt), leftTop, rightTop, leftBottom, rightBottom);
	}

	public override int GetLerpedClimateAt(double posX, double posZ, int[] climateCache, int sizeX)
	{
		int posXInt = (int)posX;
		int posZInt = (int)posZ;
		return GameMath.BiSerpRgbColor((float)(posX - (double)posXInt), (float)(posZ - (double)posZInt), climateCache[posZInt * sizeX + posXInt], climateCache[posZInt * sizeX + posXInt + 1], climateCache[(posZInt + 1) * sizeX + posXInt], climateCache[(posZInt + 1) * sizeX + posXInt + 1]);
	}

	private int GetRandomClimate(double posX, double posZ)
	{
		int tempRnd = NextInt(51) - 35;
		double P = halfRange;
		double z = posZ + ZOffset;
		int num = GameMath.Clamp((int)((float)((int)(255.0 / P * (P - Math.Abs(Math.Abs(z) % (2.0 * P) - P))) + tempRnd) * tempMul), 0, 255);
		int rain = Math.Min(255, (int)((float)NextInt(256) * rainMul));
		int hereGeologicActivity = (int)Math.Max(0.0, Math.Pow((float)NextInt(256) / 255f, geologicActivityInv) * 255.0);
		return (num << 16) + (rain << 8) + hereGeologicActivity;
	}
}
