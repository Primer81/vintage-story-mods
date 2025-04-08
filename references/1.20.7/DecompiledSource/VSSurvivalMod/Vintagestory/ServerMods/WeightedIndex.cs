namespace Vintagestory.ServerMods;

public struct WeightedIndex
{
	public int Index;

	public float Weight;

	public WeightedIndex(int index, float weight)
	{
		Index = index;
		Weight = weight;
	}
}
