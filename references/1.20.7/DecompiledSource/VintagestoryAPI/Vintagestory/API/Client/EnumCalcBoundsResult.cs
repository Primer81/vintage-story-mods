namespace Vintagestory.API.Client;

public enum EnumCalcBoundsResult
{
	/// <summary>
	/// Can continue on the same line
	/// </summary>
	Continue,
	/// <summary>
	/// Element was split between current and next line
	/// </summary>
	Multiline,
	/// <summary>
	/// Element was put on next line
	/// </summary>
	Nextline
}
