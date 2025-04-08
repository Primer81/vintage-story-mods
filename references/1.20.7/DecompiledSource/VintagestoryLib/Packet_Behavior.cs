public class Packet_Behavior
{
	public string Code;

	public string Attributes;

	public int ClientSideOptional;

	public const int CodeFieldID = 1;

	public const int AttributesFieldID = 2;

	public const int ClientSideOptionalFieldID = 3;

	public void SetCode(string value)
	{
		Code = value;
	}

	public void SetAttributes(string value)
	{
		Attributes = value;
	}

	public void SetClientSideOptional(int value)
	{
		ClientSideOptional = value;
	}

	internal void InitializeValues()
	{
	}
}
