public class Packet_FlipItemstacks
{
	public string SourceInventoryId;

	public string TargetInventoryId;

	public int SourceSlot;

	public int TargetSlot;

	public long SourceLastChanged;

	public long TargetLastChanged;

	public int SourceTabIndex;

	public int TargetTabIndex;

	public const int SourceInventoryIdFieldID = 1;

	public const int TargetInventoryIdFieldID = 2;

	public const int SourceSlotFieldID = 3;

	public const int TargetSlotFieldID = 4;

	public const int SourceLastChangedFieldID = 5;

	public const int TargetLastChangedFieldID = 6;

	public const int SourceTabIndexFieldID = 7;

	public const int TargetTabIndexFieldID = 8;

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

	public void SetSourceLastChanged(long value)
	{
		SourceLastChanged = value;
	}

	public void SetTargetLastChanged(long value)
	{
		TargetLastChanged = value;
	}

	public void SetSourceTabIndex(int value)
	{
		SourceTabIndex = value;
	}

	public void SetTargetTabIndex(int value)
	{
		TargetTabIndex = value;
	}

	internal void InitializeValues()
	{
	}
}
