namespace Vintagestory.API.MathTools;

/// <summary>
/// An lcg random generator, particularly suitable for worldgen
/// See also https://en.wikipedia.org/wiki/Linear_congruential_generator
/// </summary>
public class LCGRandom : IRandom
{
	public long worldSeed;

	public long mapGenSeed;

	public long currentSeed;

	private const float maxIntF = 2.1474836E+09f;

	private const double maxIntD = 4294967295.0;

	/// <summary>
	/// Square root of int.MaxValue, used for generating floats with approximately 15 bits / 4.5 decimal digits of precision
	/// </summary>
	private const float semiMaxIntF = 46340f;

	/// <summary>
	/// Square root of int.MaxValue, used for generating floats with approximately 15 bits / 4.5 decimal digits of precision
	/// </summary>
	private const int semiMaxInt = 46341;

	/// <summary>
	/// Initialize random with given seed
	/// </summary>
	/// <param name="worldSeed"></param>
	public LCGRandom(long worldSeed)
	{
		SetWorldSeed(worldSeed);
	}

	/// <summary>
	/// Initialize random with no seed. Use SetWorldSeed() to initialize
	/// </summary>
	public LCGRandom()
	{
	}

	/// <summary>
	/// Sets given seed
	/// </summary>
	/// <param name="worldSeed"></param>
	public void SetWorldSeed(long worldSeed)
	{
		this.worldSeed = worldSeed;
		currentSeed = mapGenSeed * 6364136223846793005L + 1442695040888963407L;
		long seed = worldSeed;
		seed *= worldSeed * 6364136223846793005L + 1442695040888963407L;
		seed++;
		seed *= worldSeed * 6364136223846793005L + 1442695040888963407L;
		seed += 2;
		seed *= worldSeed * 6364136223846793005L + 1442695040888963407L;
		seed += 3;
		mapGenSeed = seed;
	}

	/// <summary>
	/// Initializes a position dependent seed, if required
	/// </summary>
	/// <param name="xPos"></param>
	/// <param name="zPos"></param>
	public void InitPositionSeed(int xPos, int zPos)
	{
		long seed = mapGenSeed;
		seed *= seed * 6364136223846793005L + 1442695040888963407L;
		seed += xPos;
		seed *= seed * 6364136223846793005L + 1442695040888963407L;
		seed += zPos;
		seed *= seed * 6364136223846793005L + 1442695040888963407L;
		seed += xPos;
		seed *= seed * 6364136223846793005L + 1442695040888963407L;
		seed += zPos;
		currentSeed = seed;
	}

	/// <summary>
	/// Initializes a position dependent seed, if required
	/// </summary>
	/// <param name="xPos"></param>
	/// <param name="yPos"></param>
	/// <param name="zPos"></param>
	public void InitPositionSeed(int xPos, int yPos, int zPos)
	{
		long seed = mapGenSeed;
		seed *= seed * 6364136223846793005L + 1442695040888963407L;
		seed += xPos;
		seed *= seed * 6364136223846793005L + 1442695040888963407L;
		seed += yPos;
		seed *= seed * 6364136223846793005L + 1442695040888963407L;
		seed += zPos;
		seed *= seed * 6364136223846793005L + 1442695040888963407L;
		seed += xPos;
		seed *= seed * 6364136223846793005L + 1442695040888963407L;
		seed += yPos;
		seed *= seed * 6364136223846793005L + 1442695040888963407L;
		seed += zPos;
		currentSeed = seed;
	}

	/// <summary>
	/// Returns a pseudo random number from 0 - max (excluding max)
	/// </summary>
	/// <param name="max"></param>
	/// <returns></returns>
	public int NextInt(int max)
	{
		long seed = currentSeed;
		int r = (int)((seed >> 24) % max);
		if (r < 0)
		{
			r += max;
		}
		currentSeed = seed * (seed * 6364136223846793005L + 1442695040888963407L) + mapGenSeed;
		return r;
	}

	/// <summary>
	/// Returns a random number from 0 - int.MaxValue (inclusive)
	/// </summary>
	/// <returns></returns>
	public int NextInt()
	{
		long seed = currentSeed;
		int result = (int)(seed >> 24) & 0x7FFFFFFF;
		currentSeed = seed * (seed * 6364136223846793005L + 1442695040888963407L) + mapGenSeed;
		return result;
	}

	/// <summary>
	/// Returns a random number from 0.0F - 1.0F (inclusive)
	/// </summary>
	/// <returns></returns>
	public float NextFloat()
	{
		long seed = currentSeed;
		int num = (int)(seed >> 24) & 0x7FFFFFFF;
		currentSeed = seed * (seed * 6364136223846793005L + 1442695040888963407L) + mapGenSeed;
		return (float)num / 2.1474836E+09f;
	}

	/// <summary>
	/// Returns a random number from -1.0F - 1.0F (inclusive) with a bias towards the zero value (triangle graph, similar to how rolling two 6-sided dice and adding the result is most likely to yield 7)
	/// <br />Precise to better than 15 binary digits / better than 4 significant figures in decimal
	/// </summary>
	/// <returns></returns>
	public float NextFloatMinusToPlusOne()
	{
		long seed = currentSeed;
		int r = (int)(seed >> 24) & 0x7FFFFFFF;
		currentSeed = seed * (seed * 6364136223846793005L + 1442695040888963407L) + mapGenSeed;
		return (float)(r % 46341 - r / 46341) / 46340f;
	}

	/// <summary>
	/// Returns a random number from 0.0 - 1.0 (inclusive)
	/// </summary>
	/// <returns></returns>
	public double NextDouble()
	{
		long seed = currentSeed;
		int num = (int)(seed >> 24);
		currentSeed = seed * (seed * 6364136223846793005L + 1442695040888963407L) + mapGenSeed;
		return (double)(uint)num / 4294967295.0;
	}
}
