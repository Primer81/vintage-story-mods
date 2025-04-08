public class Packet_IntString
{
	public int Key_;

	public string Value_;

	public const int Key_FieldID = 1;

	public const int Value_FieldID = 2;

	public void SetKey_(int value)
	{
		Key_ = value;
	}

	public void SetValue_(string value)
	{
		Value_ = value;
	}

	internal void InitializeValues()
	{
	}
}
