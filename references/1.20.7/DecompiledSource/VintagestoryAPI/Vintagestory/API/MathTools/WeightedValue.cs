namespace Vintagestory.API.MathTools;

public class WeightedValue<T>
{
	public T Value;

	public float Weight;

	public WeightedValue()
	{
	}

	public WeightedValue(T value, float weight)
	{
		Value = value;
		Weight = weight;
	}

	public static WeightedValue<T> New(T value, float weight)
	{
		return new WeightedValue<T>(value, weight);
	}

	public void Set(T value, float weight = 1f)
	{
		Value = value;
		Weight = weight;
	}
}
