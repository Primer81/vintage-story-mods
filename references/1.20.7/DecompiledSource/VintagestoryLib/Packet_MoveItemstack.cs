public class Packet_MoveItemstack
{
	public string SourceInventoryId;

	public string TargetInventoryId;

	public int SourceSlot;

	public int TargetSlot;

	public int Quantity;

	public long SourceLastChanged;

	public long TargetLastChanged;

	public int MouseButton;

	public int Modifiers;

	public int Priority;

	public int TabIndex;

	public const int SourceInventoryIdFieldID = 1;

	public const int TargetInventoryIdFieldID = 2;

	public const int SourceSlotFieldID = 3;

	public const int TargetSlotFieldID = 4;

	public const int QuantityFieldID = 5;

	public const int SourceLastChangedFieldID = 6;

	public const int TargetLastChangedFieldID = 7;

	public const int MouseButtonFieldID = 8;

	public const int ModifiersFieldID = 9;

	public const int PriorityFieldID = 10;

	public const int TabIndexFieldID = 11;

	public void SetSourceInventoryId(string value)
	{
		SourceInventoryId = value;
	}

	public void SetTargetInventoryId(string value)
	{
		TargetInventoryId = value;
	}

	public void SetSourceSlot(int value)
	{
		SourceSlot = value;
	}

	public void SetTargetSlot(int value)
	{
		TargetSlot = value;
	}

	public void SetQuantity(int value)
	{
		Quantity = value;
	}

	public void SetSourceLastChanged(long value)
	{
		SourceLastChanged = value;
	}

	public void SetTargetLastChanged(long value)
	{
		TargetLastChanged = value;
	}

	public void SetMouseButton(int value)
	{
		MouseButton = value;
	}

	public void SetModifiers(int value)
	{
		Modifiers = value;
	}

	public void SetPriority(int value)
	{
		Priority = value;
	}

	public void SetTabIndex(int value)
	{
		TabIndex = value;
	}

	internal void InitializeValues()
	{
	}
}
