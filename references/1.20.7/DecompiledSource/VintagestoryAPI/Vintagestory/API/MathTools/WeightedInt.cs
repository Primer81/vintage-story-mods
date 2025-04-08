using System.IO;

namespace Vintagestory.API.MathTools;

public class WeightedInt : WeightedValue<int>
{
	public WeightedInt()
	{
	}

	public WeightedInt(int value, float weight)
	{
		Value = value;
		Weight = weight;
	}

	public new static WeightedInt New(int value, float weight)
	{
		return new WeightedInt(value, weight);
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Weight);
		writer.Write(Value);
	}

	public void FromBytes(BinaryReader reader)
	{
		Weight = reader.ReadSingle();
		Value = reader.ReadInt32();
	}
}
