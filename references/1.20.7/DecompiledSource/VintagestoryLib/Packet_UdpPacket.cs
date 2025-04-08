public class Packet_UdpPacket
{
	public int Id;

	public Packet_EntityPosition EntityPosition;

	public Packet_BulkEntityPosition BulkPositions;

	public Packet_CustomPacket ChannelPaket;

	public Packet_ConnectionPacket ConnectionPacket;

	public int Length;

	public const int IdFieldID = 1;

	public const int EntityPositionFieldID = 2;

	public const int BulkPositionsFieldID = 3;

	public const int ChannelPaketFieldID = 4;

	public const int ConnectionPacketFieldID = 5;

	public const int LengthFieldID = 6;

	public void SetId(int value)
	{
		Id = value;
	}

	public void SetEntityPosition(Packet_EntityPosition value)
	{
		EntityPosition = value;
	}

	public void SetBulkPositions(Packet_BulkEntityPosition value)
	{
		BulkPositions = value;
	}

	public void SetChannelPaket(Packet_CustomPacket value)
	{
		ChannelPaket = value;
	}

	public void SetConnectionPacket(Packet_ConnectionPacket value)
	{
		ConnectionPacket = value;
	}

	public void SetLength(int value)
	{
		Length = value;
	}

	internal void InitializeValues()
	{
	}
}
