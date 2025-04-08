public class Packet_ConnectionPacket
{
	public string LoginToken;

	public const int LoginTokenFieldID = 1;

	public void SetLoginToken(string value)
	{
		LoginToken = value;
	}

	internal void InitializeValues()
	{
	}
}
