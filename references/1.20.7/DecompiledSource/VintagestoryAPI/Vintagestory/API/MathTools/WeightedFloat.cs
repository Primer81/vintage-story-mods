using System.IO;

namespace Vintagestory.API.MathTools;

public class WeightedFloat : WeightedValue<float>
{
	public WeightedFloat()
	{
	}

	public WeightedFloat(float value, float weight)
	{
		Value = value;
		Weight = weight;
	}

	public new static WeightedFloat New(float value, float weight)
	{
		return new WeightedFloat(value, weight);
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Weight);
		writer.Write(Value);
	}

	public void FromBytes(BinaryReader reader)
	{
		Weight = reader.ReadSingle();
		Value = reader.ReadSingle();
	}

	public WeightedFloat Clone()
	{
		return new WeightedFloat
		{
			Weight = Weight,
			Value = Value
		};
	}

	public void SetLerped(WeightedFloat left, WeightedFloat right, float w)
	{
		Value = left.Value * (1f - w) + right.Value * w;
		Weight = left.Weight * (1f - w) + right.Weight * w;
	}
}
