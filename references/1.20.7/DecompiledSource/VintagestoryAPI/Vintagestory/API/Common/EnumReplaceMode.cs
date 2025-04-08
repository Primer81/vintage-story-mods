namespace Vintagestory.API.Common;

public enum EnumReplaceMode
{
	/// <summary>
	/// Replace if new block replaceable value &gt; old block replaceable value
	/// </summary>
	Replaceable,
	/// <summary>
	/// Replace always, no matter what blocks were there previously
	/// </summary>
	ReplaceAll,
	/// <summary>
	/// Replace always, no matter what blocks were there previously, but skip air blocks in the schematic
	/// </summary>
	ReplaceAllNoAir,
	/// <summary>
	/// Replace only air blocks
	/// </summary>
	ReplaceOnlyAir
}
