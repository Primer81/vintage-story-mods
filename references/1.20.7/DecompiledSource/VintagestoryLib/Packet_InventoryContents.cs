public class Packet_InventoryContents
{
	public int ClientId;

	public string InventoryClass;

	public string InventoryId;

	public Packet_ItemStack[] Itemstacks;

	public int ItemstacksCount;

	public int ItemstacksLength;

	public const int ClientIdFieldID = 1;

	public const int InventoryClassFieldID = 2;

	public const int InventoryIdFieldID = 3;

	public const int ItemstacksFieldID = 4;

	public void SetClientId(int value)
	{
		ClientId = value;
	}

	public void SetInventoryClass(string value)
	{
		InventoryClass = value;
	}

	public void SetInventoryId(string value)
	{
		InventoryId = value;
	}

	public Packet_ItemStack[] GetItemstacks()
	{
		return Itemstacks;
	}

	public void SetItemstacks(Packet_ItemStack[] value, int count, int length)
	{
		Itemstacks = value;
		ItemstacksCount = count;
		ItemstacksLength = length;
	}

	public void SetItemstacks(Packet_ItemStack[] value)
	{
		Itemstacks = value;
		ItemstacksCount = value.Length;
		ItemstacksLength = value.Length;
	}

	public int GetItemstacksCount()
	{
		return ItemstacksCount;
	}

	public void ItemstacksAdd(Packet_ItemStack value)
	{
		if (ItemstacksCount >= ItemstacksLength)
		{
			if ((ItemstacksLength *= 2) == 0)
			{
				ItemstacksLength = 1;
			}
			Packet_ItemStack[] newArray = new Packet_ItemStack[ItemstacksLength];
			for (int i = 0; i < ItemstacksCount; i++)
			{
				newArray[i] = Itemstacks[i];
			}
			Itemstacks = newArray;
		}
		Itemstacks[ItemstacksCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
