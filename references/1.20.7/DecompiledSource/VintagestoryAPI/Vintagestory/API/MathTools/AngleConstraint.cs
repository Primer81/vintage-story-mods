namespace Vintagestory.API.MathTools;

public class AngleConstraint : Vec2f
{
	/// <summary>
	/// Center point in radians
	/// </summary>
	public float CenterRad => X;

	/// <summary>
	/// Allowed range from that center in radians
	/// </summary>
	public float RangeRad => Y;

	public AngleConstraint(float centerRad, float rangeRad)
		: base(centerRad, rangeRad)
	{
	}
}
