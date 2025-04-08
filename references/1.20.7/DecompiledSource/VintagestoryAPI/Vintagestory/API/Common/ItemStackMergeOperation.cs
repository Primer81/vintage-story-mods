namespace Vintagestory.API.Common;

public class ItemStackMergeOperation : ItemStackMoveOperation
{
	/// <summary>
	/// The slot that the item is attempting transfer to.
	/// </summary>
	public ItemSlot SinkSlot;

	/// <summary>
	/// The slot that the item is being transferred from
	/// </summary>
	public ItemSlot SourceSlot;

	public ItemStackMergeOperation(IWorldAccessor world, EnumMouseButton mouseButton, EnumModifierKey modifiers, EnumMergePriority currentPriority, int requestedQuantity)
		: base(world, mouseButton, modifiers, currentPriority, requestedQuantity)
	{
	}
}
