public class Packet_CrushingProperties
{
	public byte[] CrushedStack;

	public int HardnessTier;

	public Packet_NatFloat Quantity;

	public const int CrushedStackFieldID = 1;

	public const int HardnessTierFieldID = 2;

	public const int QuantityFieldID = 3;

	public void SetCrushedStack(byte[] value)
	{
		CrushedStack = value;
	}

	public void SetHardnessTier(int value)
	{
		HardnessTier = value;
	}

	public void SetQuantity(Packet_NatFloat value)
	{
		Quantity = value;
	}

	internal void InitializeValues()
	{
	}
}
