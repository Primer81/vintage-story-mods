public class Packet_EntityPacket
{
	public long EntityId;

	public int Packetid;

	public byte[] Data;

	public const int EntityIdFieldID = 1;

	public const int PacketidFieldID = 2;

	public const int DataFieldID = 3;

	public void SetEntityId(long value)
	{
		EntityId = value;
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
