namespace Vintagestory.API.MathTools;

/// <summary>
/// Not really a sphere, actually now an AABB centred on x,y,z, but we keep the name for API consistency
/// </summary>
public struct Sphere
{
	public float x;

	public float y;

	public float z;

	public float radius;

	public float radiusY;

	public float radiusZ;

	public const float sqrt3half = 0.8660254f;

	public Sphere(float x1, float y1, float z1, float dx, float dy, float dz)
	{
		x = x1;
		y = y1;
		z = z1;
		radius = 0.8660254f * dx;
		radiusY = 0.8660254f * dy;
		radiusZ = 0.8660254f * dz;
	}

	public static Sphere BoundingSphereForCube(float x, float y, float z, float size)
	{
		Sphere result = default(Sphere);
		result.x = x + size / 2f;
		result.y = y + size / 2f;
		result.z = z + size / 2f;
		result.radius = 0.8660254f * size;
		result.radiusY = 0.8660254f * size;
		result.radiusZ = 0.8660254f * size;
		return result;
	}
}
