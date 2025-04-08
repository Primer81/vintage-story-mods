public class Packet_Recipes
{
	public string Code;

	public int Quantity;

	public byte[] Data;

	public const int CodeFieldID = 1;

	public const int QuantityFieldID = 2;

	public const int DataFieldID = 3;

	public void SetCode(string value)
	{
		Code = value;
	}

	public void SetQuantity(int value)
	{
		Quantity = value;
	}

	public void SetData(byte[] value)
	{
		Data = value;
	}

	internal void InitializeValues()
	{
	}
}
