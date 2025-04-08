public class Packet_ServerDisconnectPlayer
{
	public string DisconnectReason;

	public const int DisconnectReasonFieldID = 1;

	public void SetDisconnectReason(string value)
	{
		DisconnectReason = value;
	}

	internal void InitializeValues()
	{
	}
}
