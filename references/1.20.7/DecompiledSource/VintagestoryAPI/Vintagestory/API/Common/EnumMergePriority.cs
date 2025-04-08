namespace Vintagestory.API.Common;

public enum EnumMergePriority
{
	/// <summary>
	/// Automatic merge operation, when a player did not specifically request a merge, e.g. with shift + left click, or when collected from the ground
	/// </summary>
	AutoMerge,
	/// <summary>
	/// When using mouse to manually merge item stacks
	/// </summary>
	DirectMerge,
	/// <summary>
	/// Confirmed merge via dialog. Not implemented as of v1.14
	/// </summary>
	ConfirmedMerge
}
