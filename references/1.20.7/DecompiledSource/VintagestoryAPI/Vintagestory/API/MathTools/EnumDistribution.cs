namespace Vintagestory.API.MathTools;

/// <summary>
/// The distribution of the random numbers
/// </summary>
[DocumentAsJson]
public enum EnumDistribution
{
	/// <summary>
	/// Select completely random numbers within avg-var until avg+var
	/// </summary>
	UNIFORM = 0,
	/// <summary>
	/// Select random numbers with numbers near avg being the most commonly selected ones, following a triangle curve
	/// </summary>
	TRIANGLE = 1,
	/// <summary>
	/// Select random numbers with numbers near avg being the more commonly selected ones, following a gaussian curve
	/// </summary>
	GAUSSIAN = 2,
	/// <summary>
	/// Select random numbers with numbers near avg being the much more commonly selected ones, following a narrow gaussian curve
	/// </summary>
	NARROWGAUSSIAN = 3,
	/// <summary>
	/// Select random numbers with numbers near avg being the much much more commonly selected ones, following an even narrower gaussian curve
	/// </summary>
	VERYNARROWGAUSSIAN = 10,
	/// <summary>
	/// Select random numbers with numbers near avg being the less commonly selected ones, following an upside down gaussian curve
	/// </summary>
	INVERSEGAUSSIAN = 4,
	/// <summary>
	/// Select random numbers with numbers near avg being the much less commonly selected ones, following an upside down gaussian curve
	/// </summary>
	NARROWINVERSEGAUSSIAN = 5,
	/// <summary>
	/// Select numbers in the form of avg + var, wheras low value of var are preferred
	/// </summary>
	INVEXP = 6,
	/// <summary>
	/// Select numbers in the form of avg + var, wheras low value of var are strongly preferred
	/// </summary>
	STRONGINVEXP = 7,
	/// <summary>
	/// Select numbers in the form of avg + var, wheras low value of var are very strongly preferred
	/// </summary>
	STRONGERINVEXP = 8,
	/// <summary>
	/// Select completely random numbers within avg-var until avg+var only ONCE and then always 0
	/// </summary>
	DIRAC = 9
}
