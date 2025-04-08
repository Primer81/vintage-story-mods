namespace Vintagestory.API.Common;

/// <summary>
/// Used in blockAccessor.GetLightLevel() to determine what kind of light level you want
/// </summary>
[DocumentAsJson]
public enum EnumLightLevelType
{
	/// <summary>
	/// Will get you just the block light
	/// </summary>
	OnlyBlockLight,
	/// <summary>
	/// Will get you just the sun light unaffected by the day/night cycle
	/// </summary>
	OnlySunLight,
	/// <summary>
	/// Will get you max(onlysunlight, onlyblocklight)
	/// </summary>
	MaxLight,
	/// <summary>
	/// Will get you max(sunlight * sunbrightness, blocklight)
	/// </summary>
	MaxTimeOfDayLight,
	/// <summary>
	/// Will get you sunlight * sunbrightness
	/// </summary>
	TimeOfDaySunLight,
	/// <summary>
	/// Will get you sunbrightness
	/// </summary>
	Sunbrightness
}
