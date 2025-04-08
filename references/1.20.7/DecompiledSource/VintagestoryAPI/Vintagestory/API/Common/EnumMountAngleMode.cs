namespace Vintagestory.API.Common;

public enum EnumMountAngleMode
{
	/// <summary>
	/// Don't affect the mounted entity angles
	/// </summary>
	Unaffected,
	/// <summary>
	/// Turn the player but allow him to still change its yaw
	/// </summary>
	PushYaw,
	/// <summary>
	/// Turn the player in all directions but allow him to still change its angles
	/// </summary>
	Push,
	/// <summary>
	/// Fixate the mounted entity yaw to the mount
	/// </summary>
	FixateYaw,
	/// <summary>
	/// Fixate all entity angles to the mount
	/// </summary>
	Fixate
}
