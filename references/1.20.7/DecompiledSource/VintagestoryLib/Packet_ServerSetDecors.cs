public class Packet_ServerSetDecors
{
	public byte[] SetDecors;

	public const int SetDecorsFieldID = 1;

	public void SetSetDecors(byte[] value)
	{
		SetDecors = value;
	}

	internal void InitializeValues()
	{
	}
}
