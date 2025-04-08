public class Packet_ClientLeave
{
	public int Reason;

	public const int ReasonFieldID = 1;

	public void SetReason(int value)
	{
		Reason = value;
	}

	internal void InitializeValues()
	{
		Reason = 0;
	}
}
