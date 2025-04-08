namespace Vintagestory.API.Common;

public struct SolarSphericalCoords
{
	public float ZenithAngle;

	public float AzimuthAngle;

	public SolarSphericalCoords(float zenithAngle, float azimuthAngle)
	{
		ZenithAngle = zenithAngle;
		AzimuthAngle = azimuthAngle;
	}
}
