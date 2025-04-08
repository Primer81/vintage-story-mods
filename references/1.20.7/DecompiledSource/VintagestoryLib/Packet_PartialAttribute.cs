public class Packet_PartialAttribute
{
	public string Path;

	public byte[] Data;

	public const int PathFieldID = 1;

	public const int DataFieldID = 2;

	public void SetPath(string value)
	{
		Path = value;
	}

	public void SetData(byte[] value)
	{
		Data = value;
	}

	internal void InitializeValues()
	{
	}
}
