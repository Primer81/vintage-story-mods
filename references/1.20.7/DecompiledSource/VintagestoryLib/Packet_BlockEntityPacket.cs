public class Packet_BlockEntityPacket
{
	public int X;

	public int Y;

	public int Z;

	public int Packetid;

	public byte[] Data;

	public const int XFieldID = 1;

	public const int YFieldID = 2;

	public const int ZFieldID = 3;

	public const int PacketidFieldID = 4;

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

	public void SetPacketid(int value)
	{
		Packetid = value;
	}

	public void SetData(byte[] value)
	{
		Data = value;
	}

	internal void InitializeValues()
	{
	}
}
