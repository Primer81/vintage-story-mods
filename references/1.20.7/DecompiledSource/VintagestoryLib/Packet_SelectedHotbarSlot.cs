public class Packet_SelectedHotbarSlot
{
	public int SlotNumber;

	public int ClientId;

	public Packet_ItemStack Itemstack;

	public Packet_ItemStack OffhandStack;

	public const int SlotNumberFieldID = 1;

	public const int ClientIdFieldID = 2;

	public const int ItemstackFieldID = 3;

	public const int OffhandStackFieldID = 4;

	public void SetSlotNumber(int value)
	{
		SlotNumber = value;
	}

	public void SetClientId(int value)
	{
		ClientId = value;
	}

	public void SetItemstack(Packet_ItemStack value)
	{
		Itemstack = value;
	}

	public void SetOffhandStack(Packet_ItemStack value)
	{
		OffhandStack = value;
	}

	internal void InitializeValues()
	{
	}
}
