public class Packet_NotifySlot
{
	public string InventoryId;

	public int SlotId;

	public const int InventoryIdFieldID = 1;

	public const int SlotIdFieldID = 2;

	public void SetInventoryId(string value)
	{
		InventoryId = value;
	}

	public void SetSlotId(int value)
	{
		SlotId = value;
	}

	internal void InitializeValues()
	{
	}
}
