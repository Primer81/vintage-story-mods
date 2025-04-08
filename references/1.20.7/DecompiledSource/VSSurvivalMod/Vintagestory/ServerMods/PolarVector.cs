namespace Vintagestory.ServerMods;

public class PolarVector
{
	public float angle;

	public float length;

	public PolarVector(float angle, float length)
	{
		this.angle = angle;
		this.length = length;
	}

	public override string ToString()
	{
		return "angle " + angle + ", length " + length;
	}
}
