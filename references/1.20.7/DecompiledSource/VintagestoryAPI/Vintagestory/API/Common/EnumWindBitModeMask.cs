namespace Vintagestory.API.Common;

/// <summary>
/// Windmode flags, which can be ORed with existing vertex data to add the specified wind mode (assuming it was 0 previously!)
/// </summary>
public static class EnumWindBitModeMask
{
	/// <summary>
	/// Slightly affected by wind. Wiggle + Height bend based on ground distance.<br />
	/// </summary>
	public const int WeakWind = 33554432;

	/// <summary>
	/// Normally affected by wind. Wiggle + Height bend based on ground distance.
	/// </summary>
	public const int NormalWind = 67108864;

	/// <summary>
	/// Same as normal wind, but with some special behavior for leaves. Wiggle + Height bend based on ground distance.
	/// </summary>
	public const int Leaves = 100663296;

	/// <summary>
	/// Same as weak wind, but no wiggle. Height bend based on ground distance.
	/// </summary>
	public const int Bend = 134217728;

	/// <summary>
	/// Bend behavior for tall plants
	/// </summary> 
	public const int TallBend = 167772160;

	/// <summary>
	/// Vertical wiggle
	/// </summary>
	public const int Water = 201326592;

	/// <summary>
	/// Vertical wiggle
	/// </summary>
	public const int ExtraWeakWind = 234881024;

	public const int Fruit = 268435456;

	public const int WeakWindNoBend = 301989888;

	public const int WeakWindInverseBend = 335544320;

	public const int Seaweed = 369098752;

	public const int FullWaterWave = 402653184;
}
