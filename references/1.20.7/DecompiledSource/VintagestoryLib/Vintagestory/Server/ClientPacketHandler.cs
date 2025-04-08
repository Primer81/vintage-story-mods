namespace Vintagestory.Server;

public delegate void ClientPacketHandler<Packet_Client, IServerPlayer>(Packet_Client packet, IServerPlayer player);
