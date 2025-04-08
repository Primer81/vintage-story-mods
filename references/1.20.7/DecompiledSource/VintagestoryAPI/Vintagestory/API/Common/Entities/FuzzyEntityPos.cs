namespace Vintagestory.API.Common.Entities;

public class FuzzyEntityPos : EntityPos
{
	public float Radius;

	public int UsesLeft;

	public FuzzyEntityPos(double x, double y, double z, float heading = 0f, float pitch = 0f, float roll = 0f)
		: base(x, y, z, heading, pitch, roll)
	{
	}
}
