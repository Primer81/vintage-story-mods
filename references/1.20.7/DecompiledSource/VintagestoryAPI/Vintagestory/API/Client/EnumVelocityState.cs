namespace Vintagestory.API.Client;

public enum EnumVelocityState
{
	/// <summary>
	/// Currently falling
	/// </summary>
	Moving,
	/// <summary>
	/// Is now outside the world (x/y/z below -30 or x/z above mapsize + 30)
	/// </summary>
	OutsideWorld,
	/// <summary>
	/// Was falling and has now collided with the terrain
	/// </summary>
	Collided
}
