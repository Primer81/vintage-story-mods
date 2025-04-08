namespace Vintagestory.API.Common;

/// <summary>
/// The type of climate values you wish to receive
/// </summary>
[DocumentAsJson]
public enum EnumGetClimateMode
{
	/// <summary>
	/// The values generate during world generation, these are loosely considered as yearly averages
	/// </summary>
	WorldGenValues,
	/// <summary>
	/// The values at the current calendar time
	/// </summary>
	NowValues,
	/// <summary>
	/// The values at the supplied calendar time, supplied as additional arg
	/// </summary>
	ForSuppliedDateValues,
	/// <summary>
	/// The values at the supplied calendar time, ignoring rainfall etc.  Calling IBlockAccessor.GetClimateAt with this mode will never return a null ClimateCondition value, if it would be null it returns a ClimateCondition with a default 4 degrees temperature value
	/// </summary>
	ForSuppliedDate_TemperatureOnly,
	/// <summary>
	/// The values at the supplied calendar time, ignoring forest cover etc.  Calling IBlockAccessor.GetClimateAt with this mode will never return a null ClimateCondition value, if it would be null it returns a ClimateCondition with a default 4 degrees temperature value and no rain
	/// </summary>
	ForSuppliedDate_TemperatureRainfallOnly
}
