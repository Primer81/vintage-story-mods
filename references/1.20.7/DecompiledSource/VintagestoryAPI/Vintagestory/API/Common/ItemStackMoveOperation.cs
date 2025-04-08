using System;

namespace Vintagestory.API.Common;

public class ItemStackMoveOperation
{
	/// <summary>
	/// The world that the move operation is being performed.
	/// </summary>
	public IWorldAccessor World;

	/// <summary>
	/// The acting player within the world.
	/// </summary>
	public IPlayer ActingPlayer;

	/// <summary>
	/// The mouse button the ActingPlayer has pressed.
	/// </summary>
	public EnumMouseButton MouseButton;

	/// <summary>
	/// Any modifiers that the ActingPlayer is using for the operation (Ctrl, shift, alt)
	/// </summary>
	public EnumModifierKey Modifiers;

	/// <summary>
	/// The current Priority for merging slots.
	/// </summary>
	public EnumMergePriority CurrentPriority;

	/// <summary>
	/// The required Priority (can be null)
	/// </summary>
	public EnumMergePriority? RequiredPriority;

	/// <summary>
	/// The confirmation message code for this operation.
	/// </summary>
	public string ConfirmationMessageCode;

	/// <summary>
	/// The amount requested.
	/// </summary>
	public int RequestedQuantity;

	/// <summary>
	/// The amount moveable.
	/// </summary>
	public int MovableQuantity;

	/// <summary>
	/// The amount moved.
	/// </summary>
	public int MovedQuantity;

	public int WheelDir;

	/// <summary>
	/// The amount not moved.
	/// </summary>
	public int NotMovedQuantity => Math.Max(0, RequestedQuantity - MovedQuantity);

	/// <summary>
	/// Checks if the Shift Key is held down.
	/// </summary>
	public bool ShiftDown => (Modifiers & EnumModifierKey.SHIFT) > (EnumModifierKey)0;

	/// <summary>
	/// Checks if the Ctrl key is held down.
	/// </summary>
	public bool CtrlDown => (Modifiers & EnumModifierKey.CTRL) > (EnumModifierKey)0;

	/// <summary>
	/// Checks if the Alt key is held down.
	/// </summary>
	public bool AltDown => (Modifiers & EnumModifierKey.ALT) > (EnumModifierKey)0;

	public ItemStackMoveOperation(IWorldAccessor world, EnumMouseButton mouseButton, EnumModifierKey modifiers, EnumMergePriority currentPriority, int requestedQuantity = 0)
	{
		World = world;
		MouseButton = mouseButton;
		Modifiers = modifiers;
		CurrentPriority = currentPriority;
		RequestedQuantity = requestedQuantity;
	}

	/// <summary>
	/// Converts this MoveOperation to a Merge Operation.
	/// </summary>
	/// <param name="SinkSlot">The slot to put items.</param>
	/// <param name="SourceSlot">The slot to take items.</param>
	public ItemStackMergeOperation ToMergeOperation(ItemSlot SinkSlot, ItemSlot SourceSlot)
	{
		return new ItemStackMergeOperation(World, MouseButton, Modifiers, CurrentPriority, RequestedQuantity)
		{
			SinkSlot = SinkSlot,
			SourceSlot = SourceSlot,
			ActingPlayer = ActingPlayer
		};
	}
}
