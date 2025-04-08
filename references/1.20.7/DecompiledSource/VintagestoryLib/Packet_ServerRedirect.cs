public class Packet_ServerRedirect
{
	public string Name;

	public string Host;

	public const int NameFieldID = 1;

	public const int HostFieldID = 2;

	public void SetName(string value)
	{
		Name = value;
	}

	public void SetHost(string value)
	{
		Host = value;
	}

	internal void InitializeValues()
	{
	}
}
