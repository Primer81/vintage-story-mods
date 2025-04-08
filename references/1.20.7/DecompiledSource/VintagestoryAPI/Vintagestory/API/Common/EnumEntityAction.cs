namespace Vintagestory.API.Common;

/// <summary>
/// A players in-world action
/// </summary>
public enum EnumEntityAction
{
	/// <summary>
	/// No action - used when setting preCondition
	/// </summary>
	None = -1,
	/// <summary>
	/// Walk forwards
	/// </summary>
	Forward,
	/// <summary>
	/// Walk backwards
	/// </summary>
	Backward,
	/// <summary>
	/// Walk sideways left
	/// </summary>
	Left,
	/// <summary>
	/// Walk sideways right
	/// </summary>
	Right,
	/// <summary>
	/// Jump
	/// </summary>
	Jump,
	/// <summary>
	/// Sneak
	/// </summary>
	Sneak,
	/// <summary>
	/// Sprint mode
	/// </summary>
	Sprint,
	/// <summary>
	/// Glide
	/// </summary>
	Glide,
	/// <summary>
	/// Sit on the ground
	/// </summary>
	FloorSit,
	/// <summary>
	/// Left mouse down
	/// </summary>
	LeftMouseDown,
	/// <summary>
	/// Right mouse down
	/// </summary>
	RightMouseDown,
	/// <summary>
	/// Fly or swim up
	/// </summary>
	Up,
	/// <summary>
	/// Fly or swim down
	/// </summary>
	Down,
	/// <summary>
	/// Holding down the Ctrl key (which might have been remapped)
	/// </summary>
	CtrlKey,
	/// <summary>
	/// Holding down the Shift key (which might have been remapped)
	/// </summary>
	ShiftKey,
	/// <summary>
	/// Left mouse down
	/// </summary>
	InWorldLeftMouseDown,
	/// <summary>
	/// Right mouse down
	/// </summary>
	InWorldRightMouseDown
}
