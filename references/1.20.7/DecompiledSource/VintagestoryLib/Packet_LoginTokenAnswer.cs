public class Packet_LoginTokenAnswer
{
	public string Token;

	public const int TokenFieldID = 1;

	public void SetToken(string value)
	{
		Token = value;
	}

	internal void InitializeValues()
	{
	}
}
