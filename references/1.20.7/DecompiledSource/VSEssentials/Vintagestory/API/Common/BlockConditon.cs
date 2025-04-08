namespace Vintagestory.API.Common;

public class BlockConditon : GoapCondition
{
	private ActionConsumable<Block> matcher;

	public BlockConditon(ActionConsumable<Block> matcher)
	{
		this.matcher = matcher;
	}

	public bool Satisfies(Block block)
	{
		return matcher(block);
	}
}
