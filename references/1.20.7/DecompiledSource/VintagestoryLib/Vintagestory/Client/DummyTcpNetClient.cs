using System;
using System.Threading;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class DummyTcpNetClient : NetClient
{
	internal DummyNetwork network;

	public override int CurrentlyReceivingBytes => 0;

	public override void Connect(string ip, int port, Action<ConnectionResult> OnConnectionResult, Action<Exception> OnDisconnected)
	{
	}

	public override NetIncomingMessage ReadMessage()
	{
		NetIncomingMessage msg = null;
		Monitor.Enter(network.ClientReceiveBufferLock);
		if (network.ClientReceiveBuffer.Count > 0)
		{
			msg = new NetIncomingMessage();
			DummyNetworkPacket packet = network.ClientReceiveBuffer.Dequeue() as DummyNetworkPacket;
			msg.message = packet.Data;
			msg.messageLength = packet.Length;
		}
		Monitor.Exit(network.ClientReceiveBufferLock);
		return msg;
	}

	public override void Send(byte[] data)
	{
		Monitor.Enter(network.ServerReceiveBufferLock);
		DummyNetworkPacket b = new DummyNetworkPacket();
		b.Data = data;
		b.Length = data.Length;
		network.ServerReceiveBuffer.Enqueue(b);
		Monitor.Exit(network.ServerReceiveBufferLock);
	}

	public void SetNetwork(DummyNetwork network_)
	{
		network = network_;
	}
}
