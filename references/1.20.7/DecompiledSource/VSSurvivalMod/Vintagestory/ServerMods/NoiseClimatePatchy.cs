using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class NoiseClimatePatchy : NoiseClimate
{
	public NoiseClimatePatchy(long seed)
		: base(seed)
	{
	}

	public override int GetClimateAt(int posX, int posZ)
	{
		InitPositionSeed(posX, posZ);
		return GetRandomClimate();
	}

	public override int GetLerpedClimateAt(double posX, double posZ)
	{
		int posXInt = (int)posX;
		int posZInt = (int)posZ;
		InitPositionSeed(posXInt, posZInt);
		int leftTop = GetRandomClimate();
		InitPositionSeed(posXInt + 1, posZInt);
		int rightTop = GetRandomClimate();
		InitPositionSeed(posXInt, posZInt + 1);
		int leftBottom = GetRandomClimate();
		InitPositionSeed(posXInt + 1, posZInt + 1);
		int rightBottom = GetRandomClimate();
		return GameMath.BiSerpRgbColor((float)(posX - (double)posXInt), (float)(posZ - (double)posZInt), leftTop, rightTop, leftBottom, rightBottom);
	}

	public override int GetLerpedClimateAt(double posX, double posZ, int[] climateCache, int sizeX)
	{
		int posXInt = (int)posX;
		int posZInt = (int)posZ;
		return GameMath.BiSerpRgbColor((float)(posX - (double)posXInt), (float)(posZ - (double)posZInt), climateCache[posZInt * sizeX + posXInt], climateCache[posZInt * sizeX + posXInt + 1], climateCache[(posZInt + 1) * sizeX + posXInt], climateCache[(posZInt + 1) * sizeX + posXInt + 1]);
	}

	protected int gaussRnd3(int maxint)
	{
		return Math.Min(255, (NextInt(maxint) + NextInt(maxint) + NextInt(maxint)) / 3);
	}

	protected int gaussRnd2(int maxint)
	{
		return Math.Min(255, (NextInt(maxint) + NextInt(maxint)) / 2);
	}

	protected virtual int GetRandomClimate()
	{
		int rnd = NextIntFast(127);
		int geologicActivity = Math.Max(0, NextInt(256) - 128) * 2;
		int temperature;
		int rain;
		if (rnd < 20)
		{
			temperature = Math.Min(255, (int)((float)gaussRnd3(60) * tempMul));
			rain = Math.Min(255, (int)((float)gaussRnd3(130) * rainMul));
			return (temperature << 16) + (rain << 8) + geologicActivity;
		}
		if (rnd < 40)
		{
			temperature = Math.Min(255, (int)((float)(220 + gaussRnd3(75)) * tempMul));
			rain = Math.Min(255, (int)((float)gaussRnd3(20) * rainMul));
			return (temperature << 16) + (rain << 8) + geologicActivity;
		}
		if (rnd < 50)
		{
			temperature = Math.Min(255, (int)((float)(220 + gaussRnd3(75)) * tempMul));
			rain = Math.Min(255, (int)((float)(220 + NextInt(35)) * rainMul));
			return (temperature << 16) + (rain << 8) + geologicActivity;
		}
		if (rnd < 55)
		{
			temperature = Math.Min(255, (int)((float)(120 + NextInt(60)) * tempMul));
			rain = Math.Min(255, (int)((float)(200 + NextInt(50)) * rainMul));
			return (temperature << 16) + (rain << 8) + geologicActivity;
		}
		temperature = Math.Min(255, (int)((float)(100 + gaussRnd2(165)) * tempMul));
		rain = Math.Min(255, (int)((float)gaussRnd3(210 - (150 - temperature)) * rainMul));
		return (temperature << 16) + (rain << 8) + geologicActivity;
	}
}
