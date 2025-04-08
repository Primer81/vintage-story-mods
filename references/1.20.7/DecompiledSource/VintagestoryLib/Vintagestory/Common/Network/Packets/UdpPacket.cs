using System.Net;
using Vintagestory.Server;

namespace Vintagestory.Common.Network.Packets;

public struct UdpPacket
{
	public Packet_UdpPacket Packet;

	public ServerPlayer Player;

	public IPEndPoint EndPoint;
}
