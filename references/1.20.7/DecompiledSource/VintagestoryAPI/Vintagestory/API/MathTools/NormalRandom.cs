using System;

namespace Vintagestory.API.MathTools;

public class NormalRandom : Random, IRandom
{
	public NormalRandom()
	{
	}

	public NormalRandom(int Seed)
		: base(Seed)
	{
	}

	public int NextInt(int max)
	{
		return Next(max);
	}

	public int NextInt()
	{
		return Next();
	}

	public float NextFloat()
	{
		return (float)NextDouble();
	}
}
