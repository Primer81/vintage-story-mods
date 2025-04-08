namespace Vintagestory.GameContent;

public class BlockEntityStove : BlockEntityFirepit
{
	public override bool BurnsAllFuell => false;

	public override float HeatModifier => 1.1f;

	public override float BurnDurationModifier => 1.2f;

	public override string DialogTitle => "Stove";
}
