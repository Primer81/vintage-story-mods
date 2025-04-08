using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class ConditionalPatternConfig
{
	public EnumChanceFunction WeightFunction;

	public float? MinRain;

	public float? MaxRain;

	public float RainRange = 1f;

	public float? MinTemp;

	public float? MaxTemp;

	public float TempRange = 1f;

	public float Weight = 1f;

	public float getWeight(float rainfall, float temperature)
	{
		float hereweight = Weight;
		switch (WeightFunction)
		{
		case EnumChanceFunction.TestRainTemp:
			if (MinRain.HasValue)
			{
				hereweight *= GameMath.Clamp(rainfall - MinRain.Value, 0f, RainRange) / RainRange;
			}
			if (MinTemp.HasValue)
			{
				hereweight *= GameMath.Clamp(temperature - MinTemp.Value, 0f, TempRange) / TempRange;
			}
			if (MaxRain.HasValue)
			{
				hereweight *= GameMath.Clamp(MaxRain.Value - rainfall, 0f, RainRange) / RainRange;
			}
			if (MaxTemp.HasValue)
			{
				hereweight *= GameMath.Clamp(MaxTemp.Value - temperature, 0f, TempRange) / TempRange;
			}
			break;
		case EnumChanceFunction.AvoidHotAndDry:
		{
			float tmprel = (TempRange + 20f) / 60f;
			float mul = rainfall * (1f - tmprel);
			hereweight *= mul;
			break;
		}
		}
		return hereweight;
	}
}
