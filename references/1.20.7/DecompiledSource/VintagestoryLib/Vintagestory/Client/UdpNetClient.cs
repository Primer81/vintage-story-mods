using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class UdpNetClient : UNetClient
{
	protected internal UdpClient udpClient;

	private readonly CancellationTokenSource cts = new CancellationTokenSource();

	private ConcurrentQueue<Packet_UdpPacket> clientPacketQueue = new ConcurrentQueue<Packet_UdpPacket>();

	private Task udpListenTask;

	private bool disposed;

	public override event UdpConnectionRequest DidReceiveUdpConnectionRequest;

	public override void Connect(string ip, int port)
	{
		udpClient = new UdpClient(ip, port);
		udpListenTask = new Task(ListenClient, null, cts.Token, TaskCreationOptions.LongRunning);
		udpClient.Connect(ip, port);
		udpListenTask.Start();
	}

	private async void ListenClient(object state)
	{
		while (!cts.IsCancellationRequested)
		{
			try
			{
				UdpReceiveResult result = await udpClient.ReceiveAsync(cts.Token);
				Packet_UdpPacket packet = new Packet_UdpPacket();
				Packet_UdpPacketSerializer.Deserialize(new CitoMemoryStream(result.Buffer, result.Buffer.Length), packet);
				if (packet.Id > 0)
				{
					if (packet.Id == 1)
					{
						DidReceiveUdpConnectionRequest?.Invoke();
						continue;
					}
					packet.Length = result.Buffer.Length;
					clientPacketQueue.Enqueue(packet);
				}
			}
			catch
			{
			}
		}
	}

	public override IEnumerable<Packet_UdpPacket> ReadMessage()
	{
		Packet_UdpPacket[] packets = null;
		if (clientPacketQueue.Count > 0)
		{
			packets = clientPacketQueue.ToArray();
			clientPacketQueue.Clear();
		}
		return packets;
	}

	public override void EnqueuePacket(Packet_UdpPacket udpPacket)
	{
		clientPacketQueue.Enqueue(udpPacket);
	}

	public override void Send(Packet_UdpPacket packet)
	{
		if (disposed)
		{
			return;
		}
		try
		{
			byte[] data = Packet_UdpPacketSerializer.SerializeToBytes(packet);
			udpClient.Send(data);
		}
		catch
		{
		}
	}

	public override void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			udpClient?.Dispose();
			cts?.Cancel();
			cts?.Dispose();
			udpListenTask?.Dispose();
		}
	}
}
