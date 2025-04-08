namespace Vintagestory.API.Common;

public enum EnumWindBitMode
{
	/// <summary>
	/// Not affected by wind
	/// </summary>
	NoWind,
	/// <summary>
	/// Slightly affected by wind. Wiggle + Height bend based on ground distance.
	/// </summary>
	WeakWind,
	/// <summary>
	/// Normally affected by wind. Wiggle + Height bend based on ground distance.
	/// </summary>
	NormalWind,
	/// <summary>
	/// Same as normal wind, but with some special behavior for leaves. Wiggle + Height bend based on ground distance.
	/// </summary>
	Leaves,
	/// <summary>
	/// Same as normal wind, but no wiggle. Weak height bend based on ground distance.
	/// </summary>
	Bend,
	/// <summary>
	/// Bend behavior for tall plants
	/// </summary>
	TallBend,
	/// <summary>
	/// Vertical wiggle
	/// </summary>
	Water,
	ExtraWeakWind,
	Fruit,
	WeakWindNoBend,
	WeakWindInverseBend,
	WaterPlant
}
