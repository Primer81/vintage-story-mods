public class Packet_InvOpenClose
{
	public string InventoryId;

	public int Opened;

	public const int InventoryIdFieldID = 1;

	public const int OpenedFieldID = 2;

	public void SetInventoryId(string value)
	{
		InventoryId = value;
	}

	public void SetOpened(int value)
	{
		Opened = value;
	}

	internal void InitializeValues()
	{
	}
}
