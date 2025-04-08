public class Packet_CustomPacket
{
	public int ChannelId;

	public int MessageId;

	public byte[] Data;

	public const int ChannelIdFieldID = 1;

	public const int MessageIdFieldID = 2;

	public const int DataFieldID = 3;

	public void SetChannelId(int value)
	{
		ChannelId = value;
	}

	public void SetMessageId(int value)
	{
		MessageId = value;
	}

	public void SetData(byte[] value)
	{
		Data = value;
	}

	internal void InitializeValues()
	{
	}
}
