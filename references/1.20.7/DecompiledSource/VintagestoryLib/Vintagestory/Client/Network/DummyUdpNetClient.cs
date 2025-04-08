using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Vintagestory.Common;

namespace Vintagestory.Client.Network;

public class DummyUdpNetClient : UNetClient
{
	private DummyNetwork network;

	public override event UdpConnectionRequest DidReceiveUdpConnectionRequest;

	public override void Connect(string ip, int port)
	{
	}

	public override IEnumerable<Packet_UdpPacket> ReadMessage()
	{
		Monitor.Enter(network.ClientReceiveBufferLock);
		Packet_UdpPacket[] udpPacket = null;
		if (network.ClientReceiveBuffer.Count > 0)
		{
			udpPacket = network.ClientReceiveBuffer.Select((object p) => (Packet_UdpPacket)p).ToArray();
			network.ClientReceiveBuffer.Clear();
		}
		Monitor.Exit(network.ClientReceiveBufferLock);
		return udpPacket;
	}

	public override void Send(Packet_UdpPacket packet)
	{
		Monitor.Enter(network.ServerReceiveBufferLock);
		network.ServerReceiveBuffer.Enqueue(packet);
		Monitor.Exit(network.ServerReceiveBufferLock);
	}

	public override void EnqueuePacket(Packet_UdpPacket udpPacket)
	{
		throw new NotImplementedException();
	}

	public void SetNetwork(DummyNetwork network_)
	{
		network = network_;
	}
}
