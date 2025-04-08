using System.Net;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Common;

public class DummyNetConnection : NetConnection
{
	internal DummyNetwork network;

	private IPEndPoint dummyEndPoint = new IPEndPoint(new IPAddress(new byte[4] { 127, 0, 0, 1 }), 0);

	public override EnumSendResult Send(byte[] data, bool compressed = false)
	{
		Monitor.Enter(network.ClientReceiveBufferLock);
		DummyNetworkPacket packet = new DummyNetworkPacket();
		packet.Data = data;
		packet.Length = data.Length;
		network.ClientReceiveBuffer.Enqueue(packet);
		Monitor.Exit(network.ClientReceiveBufferLock);
		return EnumSendResult.Ok;
	}

	public override EnumSendResult HiPerformanceSend(BoxedPacket box, ILogger Logger, bool compressionAllowed)
	{
		int len = (box.LengthSent = box.Length);
		byte[] dataCopy = box.Clone(0);
		Monitor.Enter(network.ClientReceiveBufferLock);
		DummyNetworkPacket packet = new DummyNetworkPacket();
		packet.Data = dataCopy;
		packet.Length = len;
		network.ClientReceiveBuffer.Enqueue(packet);
		Monitor.Exit(network.ClientReceiveBufferLock);
		return EnumSendResult.Ok;
	}

	public override IPEndPoint RemoteEndPoint()
	{
		return dummyEndPoint;
	}

	public override bool EqualsConnection(NetConnection connection)
	{
		return true;
	}

	public override void Close()
	{
	}

	public override void Shutdown()
	{
	}

	internal static bool SendServerAssetsPacketDirectly(Packet_Server packet)
	{
		return ClientSystemStartup.ReceiveAssetsPacketDirectly(packet);
	}

	internal static bool SendServerPacketDirectly(Packet_Server packet)
	{
		return ClientSystemStartup.ReceiveServerPacketDirectly(packet);
	}
}
