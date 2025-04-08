public class Packet_ItemStack
{
	public int ItemClass;

	public int ItemId;

	public int StackSize;

	public byte[] Attributes;

	public const int ItemClassFieldID = 1;

	public const int ItemIdFieldID = 2;

	public const int StackSizeFieldID = 3;

	public const int AttributesFieldID = 4;

	public void SetItemClass(int value)
	{
		ItemClass = value;
	}

	public void SetItemId(int value)
	{
		ItemId = value;
	}

	public void SetStackSize(int value)
	{
		StackSize = value;
	}

	public void SetAttributes(byte[] value)
	{
		Attributes = value;
	}

	internal void InitializeValues()
	{
		ItemClass = 0;
	}
}
