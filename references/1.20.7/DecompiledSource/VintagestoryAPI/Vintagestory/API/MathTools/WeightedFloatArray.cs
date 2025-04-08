using System.IO;

namespace Vintagestory.API.MathTools;

public class WeightedFloatArray : WeightedValue<float[]>
{
	public WeightedFloatArray()
	{
	}

	public WeightedFloatArray(float[] value, float weight)
	{
		Value = value;
		Weight = weight;
	}

	public new static WeightedFloatArray New(float[] value, float weight)
	{
		return new WeightedFloatArray(value, weight);
	}

	public WeightedFloatArray Clone()
	{
		return new WeightedFloatArray
		{
			Weight = Weight,
			Value = (float[])Value.Clone()
		};
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Weight);
		writer.Write(Value.Length);
		for (int i = 0; i < Value.Length; i++)
		{
			writer.Write(Value[i]);
		}
	}

	public void FromBytes(BinaryReader reader)
	{
		Weight = reader.ReadSingle();
		Value = new float[reader.ReadInt32()];
		for (int i = 0; i < Value.Length; i++)
		{
			Value[i] = reader.ReadSingle();
		}
	}

	public void SetLerped(WeightedFloatArray left, WeightedFloatArray right, float w)
	{
		Weight = left.Weight * w + right.Weight * (1f - w);
		for (int i = 0; i < Value.Length; i++)
		{
			Value[i] = left.Value[i] * w + right.Value[i] * (1f - w);
		}
	}
}
