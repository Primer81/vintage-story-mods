using System;
using System.Collections.Generic;
using System.Net;
using Vintagestory.API.Server;
using Vintagestory.Common.Network.Packets;

namespace Vintagestory.Common;

public abstract class UNetServer : IDisposable
{
	public abstract Dictionary<IPEndPoint, int> EndPoints { get; }

	public abstract void SetIpAndPort(string ip, int port);

	public abstract void Start();

	public abstract UdpPacket[] ReadMessage();

	public abstract void Dispose();

	public abstract int SendToClient(int clientId, Packet_UdpPacket packet);

	public abstract void Add(IPEndPoint endPoint, int clientId);

	public abstract void Remove(IServerPlayer player);

	public abstract void EnqueuePacket(UdpPacket udpPacket);
}
