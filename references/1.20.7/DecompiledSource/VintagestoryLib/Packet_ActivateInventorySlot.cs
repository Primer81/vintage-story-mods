public class Packet_ActivateInventorySlot
{
	public int MouseButton;

	public int Modifiers;

	public string TargetInventoryId;

	public int TargetSlot;

	public long TargetLastChanged;

	public int TabIndex;

	public int Priority;

	public int Dir;

	public const int MouseButtonFieldID = 1;

	public const int ModifiersFieldID = 4;

	public const int TargetInventoryIdFieldID = 2;

	public const int TargetSlotFieldID = 3;

	public const int TargetLastChangedFieldID = 5;

	public const int TabIndexFieldID = 6;

	public const int PriorityFieldID = 7;

	public const int DirFieldID = 8;

	public void SetMouseButton(int value)
	{
		MouseButton = value;
	}

	public void SetModifiers(int value)
	{
		Modifiers = value;
	}

	public void SetTargetInventoryId(string value)
	{
		TargetInventoryId = value;
	}

	public void SetTargetSlot(int value)
	{
		TargetSlot = value;
	}

	public void SetTargetLastChanged(long value)
	{
		TargetLastChanged = value;
	}

	public void SetTabIndex(int value)
	{
		TabIndex = value;
	}

	public void SetPriority(int value)
	{
		Priority = value;
	}

	public void SetDir(int value)
	{
		Dir = value;
	}

	internal void InitializeValues()
	{
	}
}
