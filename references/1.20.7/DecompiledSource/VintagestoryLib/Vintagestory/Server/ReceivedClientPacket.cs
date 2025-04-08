namespace Vintagestory.Server;

public class ReceivedClientPacket
{
	internal readonly ConnectedClient client;

	internal readonly Packet_Client packet;

	internal readonly string disconnectReason;

	internal readonly ReceivedClientPacketType type;

	public ReceivedClientPacket(ConnectedClient client)
	{
		type = ReceivedClientPacketType.NewConnection;
		this.client = client;
	}

	public ReceivedClientPacket(ConnectedClient client, Packet_Client packet)
	{
		type = ReceivedClientPacketType.PacketReceived;
		this.client = client;
		this.packet = packet;
	}

	public ReceivedClientPacket(ConnectedClient client, string reason)
	{
		type = ReceivedClientPacketType.Disconnect;
		this.client = client;
		disconnectReason = reason;
	}
}
