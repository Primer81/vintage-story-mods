namespace Vintagestory.API.MathTools;

public interface IRandom
{
	/// <summary>
	/// Returns 0..max-1
	/// </summary>
	/// <param name="max"></param>
	/// <returns></returns>
	int NextInt(int max);

	int NextInt();

	double NextDouble();

	float NextFloat();
}
