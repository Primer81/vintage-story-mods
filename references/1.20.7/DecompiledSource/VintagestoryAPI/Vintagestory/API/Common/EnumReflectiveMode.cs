namespace Vintagestory.API.Common;

/// <summary>
/// On the graphics card we have only one reflective bit, but we can store the mode in the wind data bits
/// </summary>
public enum EnumReflectiveMode
{
	/// <summary>
	/// Not reflective
	/// </summary>
	None,
	/// <summary>
	/// Sun-Position independent reflectivity
	/// </summary>
	Weak,
	/// <summary>
	/// Sun-Position dependent weak reflectivity
	/// </summary>
	Medium,
	/// <summary>
	/// Sun-Position dependent weak reflectivity
	/// </summary>
	Strong,
	/// <summary>
	/// Many small sparkles
	/// </summary>
	Sparkly,
	Mild
}
