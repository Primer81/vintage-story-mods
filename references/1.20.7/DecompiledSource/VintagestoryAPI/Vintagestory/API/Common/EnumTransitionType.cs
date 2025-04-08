namespace Vintagestory.API.Common;

/// <summary>
/// Types of transition for items.
/// </summary>
[DocumentAsJson]
public enum EnumTransitionType
{
	/// <summary>
	/// For food, animals or non-organic materials.
	/// </summary>
	Perish,
	/// <summary>
	/// Can be dried.
	/// </summary>
	Dry,
	/// <summary>
	/// Can be burned.
	/// </summary>
	Burn,
	/// <summary>
	/// Can be cured, for meat.
	/// </summary>
	Cure,
	/// <summary>
	/// Generic 'other' conversion.
	/// </summary>
	Convert,
	/// <summary>
	/// Cheese ripening.
	/// </summary>
	Ripen,
	/// <summary>
	/// Snow/ice melting.
	/// </summary>
	Melt,
	/// <summary>
	/// Glue hardening.
	/// </summary>
	Harden,
	/// <summary>
	/// Used for cooking recipes where the output has no perishableprops, but we still need a non-null TransitionableProperties  (e.g. sulfuric acid in 1.20)
	/// </summary>
	None
}
