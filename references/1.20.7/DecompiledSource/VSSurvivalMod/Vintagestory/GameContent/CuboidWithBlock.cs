namespace Vintagestory.GameContent;

public class CuboidWithBlock : CuboidWithMaterial
{
	public int BlockId;

	public CuboidWithBlock(CuboidWithMaterial cwm, int blockId)
	{
		BlockId = blockId;
		Material = cwm.Material;
		Set(cwm.X1, cwm.Y1, cwm.Z1, cwm.X2, cwm.Y2, cwm.Z2);
	}
}
