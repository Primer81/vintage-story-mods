namespace Vintagestory.API.Common;

/// <summary>
/// The type of collectible in an itemstack.
/// </summary>
[DocumentAsJson]
public enum EnumItemClass
{
	/// <summary>
	/// This itemstack holds a block.
	/// </summary>
	Block,
	/// <summary>
	/// This itemstack holds an item.
	/// </summary>
	Item
}
