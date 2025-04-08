public class Packet_InventoryUpdate
{
	public int ClientId;

	public string InventoryId;

	public int SlotId;

	public Packet_ItemStack ItemStack;

	public const int ClientIdFieldID = 1;

	public const int InventoryIdFieldID = 2;

	public const int SlotIdFieldID = 3;

	public const int ItemStackFieldID = 4;

	public void SetClientId(int value)
	{
		ClientId = value;
	}

	public void SetInventoryId(string value)
	{
		InventoryId = value;
	}

	public void SetSlotId(int value)
	{
		SlotId = value;
	}

	public void SetItemStack(Packet_ItemStack value)
	{
		ItemStack = value;
	}

	internal void InitializeValues()
	{
	}
}
