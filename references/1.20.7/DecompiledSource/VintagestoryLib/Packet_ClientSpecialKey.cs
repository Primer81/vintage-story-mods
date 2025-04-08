public class Packet_ClientSpecialKey
{
	public int Key_;

	public const int Key_FieldID = 1;

	public void SetKey_(int value)
	{
		Key_ = value;
	}

	internal void InitializeValues()
	{
		Key_ = 0;
	}
}
