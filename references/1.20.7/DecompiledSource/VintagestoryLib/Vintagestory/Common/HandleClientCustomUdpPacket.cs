using Vintagestory.API.Server;

namespace Vintagestory.Common;

public delegate void HandleClientCustomUdpPacket(Packet_CustomPacket udpPacket, IServerPlayer player);
