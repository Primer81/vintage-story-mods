using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;

namespace Vintagestory.Server.Network;

public class UdpNetServer : UNetServer
{
	private UdpClient udpServer;

	private readonly CancellationTokenSource cts = new CancellationTokenSource();

	public static int MaxUdpPacketSize = 5000;

	private readonly Dictionary<int, IPEndPoint> endPointsReverse = new Dictionary<int, IPEndPoint>();

	private readonly ConcurrentDictionary<int, ConnectedClient> clients;

	private readonly ConcurrentQueue<UdpPacket> serverPacketQueue = new ConcurrentQueue<UdpPacket>();

	private Task udpListenTask;

	private int port { get; set; }

	private string ip { get; set; }

	public override Dictionary<IPEndPoint, int> EndPoints { get; } = new Dictionary<IPEndPoint, int>();


	public UdpNetServer(ConcurrentDictionary<int, ConnectedClient> clients)
	{
		this.clients = clients;
	}

	public override void SetIpAndPort(string ip, int port)
	{
		this.ip = ip;
		this.port = port;
	}

	public override void Start()
	{
		IPAddress ipAddress = (Socket.OSSupportsIPv6 ? IPAddress.IPv6Any : IPAddress.Any);
		bool dualMode = Socket.OSSupportsIPv6;
		if (ip != null)
		{
			ipAddress = IPAddress.Parse(ip);
			dualMode = false;
		}
		udpServer = new UdpClient(ipAddress.AddressFamily);
		if (dualMode)
		{
			udpServer.Client.DualMode = true;
		}
		udpServer.Client.Bind(new IPEndPoint(ipAddress, port));
		udpListenTask = new Task(ListenServer, null, cts.Token, TaskCreationOptions.LongRunning);
		udpListenTask.Start();
	}

	private async void ListenServer(object state)
	{
		while (!cts.IsCancellationRequested)
		{
			try
			{
				UdpReceiveResult result = await udpServer.ReceiveAsync(cts.Token);
				if (result.Buffer.Length > MaxUdpPacketSize)
				{
					continue;
				}
				Packet_UdpPacket packet = new Packet_UdpPacket();
				Packet_UdpPacketSerializer.DeserializeBuffer(result.Buffer, result.Buffer.Length, packet);
				int id = packet.Id;
				if ((id < 1 || id > 7) ? true : false)
				{
					continue;
				}
				UdpPacket udpPacket = default(UdpPacket);
				udpPacket.Packet = packet;
				packet.Length = result.Buffer.Length;
				if (packet.Id == 1)
				{
					udpPacket.EndPoint = result.RemoteEndPoint;
					serverPacketQueue.Enqueue(udpPacket);
					continue;
				}
				int clientId = EndPoints.Get(result.RemoteEndPoint, 0);
				if (clients.TryGetValue(clientId, out var client))
				{
					udpPacket.Player = client.Player;
					serverPacketQueue.Enqueue(udpPacket);
				}
			}
			catch
			{
			}
		}
	}

	public override UdpPacket[] ReadMessage()
	{
		UdpPacket[] packets = null;
		if (serverPacketQueue.Count > 0)
		{
			packets = serverPacketQueue.ToArray();
			serverPacketQueue.Clear();
		}
		return packets;
	}

	public override void Dispose()
	{
		cts.Cancel();
		cts.Dispose();
		udpServer.Dispose();
		udpListenTask.Dispose();
	}

	public override int SendToClient(int clientId, Packet_UdpPacket packet)
	{
		try
		{
			if (endPointsReverse.TryGetValue(clientId, out var ipEndPoint))
			{
				byte[] data = Packet_UdpPacketSerializer.SerializeToBytes(packet);
				udpServer.SendAsync(data, ipEndPoint);
				return data.Length;
			}
		}
		catch
		{
		}
		return 0;
	}

	public override void Remove(IServerPlayer player)
	{
		if (endPointsReverse.TryGetValue(player.ClientId, out var ipEndPoint))
		{
			endPointsReverse.Remove(player.ClientId);
			EndPoints.Remove(ipEndPoint);
		}
	}

	public override void EnqueuePacket(UdpPacket udpPacket)
	{
		serverPacketQueue.Enqueue(udpPacket);
	}

	public override void Add(IPEndPoint endPoint, int clientId)
	{
		EndPoints.Add(endPoint, clientId);
		endPointsReverse.Add(clientId, endPoint);
	}
}
