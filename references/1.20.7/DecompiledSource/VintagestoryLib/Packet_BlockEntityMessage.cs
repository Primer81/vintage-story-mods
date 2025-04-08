public class Packet_BlockEntityMessage
{
	public int X;

	public int Y;

	public int Z;

	public int PacketId;

	public byte[] Data;

	public const int XFieldID = 1;

	public const int YFieldID = 2;

	public const int ZFieldID = 3;

	public const int PacketIdFieldID = 4;

	public const int DataFieldID = 5;

	public void SetX(int value)
	{
		X = value;
	}

	public void SetY(int value)
	{
		Y = value;
	}

	public void SetZ(int value)
	{
		Z = value;
	}

	public void SetPacketId(int value)
	{
		PacketId = value;
	}

	public void SetData(byte[] value)
	{
		Data = value;
	}

	internal void InitializeValues()
	{
	}
}
