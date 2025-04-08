namespace Vintagestory.API.Common;

/// <summary>
/// The type of smelting for the collectible. This effects how the object is smelted.
/// </summary>
[DocumentAsJson]
public enum EnumSmeltType
{
	/// <summary>
	/// Currently has no special behavior.
	/// </summary>
	Smelt,
	/// <summary>
	/// Currently has no special behavior.
	/// </summary>
	Cook,
	/// <summary>
	/// This collectible must be baked in a clay oven. Note that you will likely want to use <see cref="T:Vintagestory.API.Common.BakingProperties" /> in the item's attributes.
	/// </summary>
	Bake,
	/// <summary>
	/// Currently has no special behavior.
	/// </summary>
	Convert,
	/// <summary>
	/// This collectible must be fired in a kiln.
	/// </summary>
	Fire
}
