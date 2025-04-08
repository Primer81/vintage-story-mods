public class Packet_StringList
{
	public string[] Items;

	public int ItemsCount;

	public int ItemsLength;

	public const int ItemsFieldID = 1;

	public string[] GetItems()
	{
		return Items;
	}

	public void SetItems(string[] value, int count, int length)
	{
		Items = value;
		ItemsCount = count;
		ItemsLength = length;
	}

	public void SetItems(string[] value)
	{
		Items = value;
		ItemsCount = value.Length;
		ItemsLength = value.Length;
	}

	public int GetItemsCount()
	{
		return ItemsCount;
	}

	public void ItemsAdd(string value)
	{
		if (ItemsCount >= ItemsLength)
		{
			if ((ItemsLength *= 2) == 0)
			{
				ItemsLength = 1;
			}
			string[] newArray = new string[ItemsLength];
			for (int i = 0; i < ItemsCount; i++)
			{
				newArray[i] = Items[i];
			}
			Items = newArray;
		}
		Items[ItemsCount++] = value;
	}

	internal void InitializeValues()
	{
	}
}
