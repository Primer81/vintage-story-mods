using System.Threading;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class DummyTcpNetServer : NetServer
{
	internal DummyNetwork network;

	private DummyNetConnection connectedClient;

	private bool receivedAnyMessage;

	public override string Name => "Dummy connection";

	public override string LocalEndpoint => "127.0.0.1";

	public DummyTcpNetServer()
	{
		connectedClient = new DummyNetConnection();
	}

	public override void Start()
	{
	}

	public override NetIncomingMessage ReadMessage()
	{
		NetIncomingMessage msg = null;
		Monitor.Enter(network.ServerReceiveBufferLock);
		if (network.ServerReceiveBuffer.Count > 0)
		{
			if (!receivedAnyMessage)
			{
				receivedAnyMessage = true;
				msg = new NetIncomingMessage();
				msg.Type = NetworkMessageType.Connect;
				msg.SenderConnection = connectedClient;
			}
			else
			{
				msg = new NetIncomingMessage();
				DummyNetworkPacket b = network.ServerReceiveBuffer.Dequeue() as DummyNetworkPacket;
				msg.message = b.Data;
				msg.messageLength = b.Length;
				msg.SenderConnection = connectedClient;
			}
		}
		Monitor.Exit(network.ServerReceiveBufferLock);
		return msg;
	}

	public void SetNetwork(DummyNetwork dummyNetwork)
	{
		network = dummyNetwork;
		connectedClient.network = network;
	}

	public override void SetIpAndPort(string ip, int port)
	{
	}

	public override void Dispose()
	{
	}
}
