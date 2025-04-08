public class Packet_CreateItemstack
{
	public string TargetInventoryId;

	public int TargetSlot;

	public long TargetLastChanged;

	public Packet_ItemStack Itemstack;

	public const int TargetInventoryIdFieldID = 1;

	public const int TargetSlotFieldID = 2;

	public const int TargetLastChangedFieldID = 3;

	public const int ItemstackFieldID = 4;

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

	public void SetItemstack(Packet_ItemStack value)
	{
		Itemstack = value;
	}

	internal void InitializeValues()
	{
	}
}
