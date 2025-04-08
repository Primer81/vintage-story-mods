using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class LakeBedLayerProperties
{
	public LakeBedBlockCodeByMin[] BlockCodeByMin;

	public int GetSuitable(float temp, float rainRel, float yRel, LCGRandom rand, int rockBlockId)
	{
		for (int i = 0; i < BlockCodeByMin.Length; i++)
		{
			if (BlockCodeByMin[i].Suitable(temp, rainRel, yRel, rand))
			{
				return BlockCodeByMin[i].GetBlockForMotherRock(rockBlockId);
			}
		}
		return 0;
	}
}
