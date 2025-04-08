namespace Vintagestory.API.Common.Entities;

/// <summary>
/// Allows you to control spawn limits based on a set of entity codes using a wildcard.
/// </summary>
[DocumentAsJson]
public class QuantityByGroup
{
	/// <summary>
	/// <!--<jsonoptional>Required</jsonoptional>-->
	/// The maximum quantity for all entities that match the <see cref="F:Vintagestory.API.Common.Entities.QuantityByGroup.Code" /> wildcard.
	/// </summary>
	[DocumentAsJson]
	public int MaxQuantity;

	/// <summary>
	/// <!--<jsonoptional>Required</jsonoptional>-->
	/// A wildcard asset location which can group many entities together.
	/// </summary>
	[DocumentAsJson]
	public AssetLocation Code;
}
