using System;
using System.Collections.Concurrent;
using System.Threading;
using Vintagestory.Common;
using Vintagestory.Server.Network;

namespace Vintagestory.Server;

public class TcpNetServer : NetServer
{
	protected ServerNetManager server;

	private ConcurrentQueue<NetIncomingMessage> messages;

	private int Port;

	private string Ip;

	public CancellationTokenSource cts = new CancellationTokenSource();

	private bool disposed;

	public override string Name => "TCP";

	public override string LocalEndpoint => Ip;

	public TcpNetServer()
	{
		messages = new ConcurrentQueue<NetIncomingMessage>();
		server = new ServerNetManager(cts.Token);
	}

	public override void Start()
	{
		server.StartServer(Port, Ip);
		server.Connected += ServerConnected;
		server.ReceivedMessage += ServerReceivedMessage;
		server.Disconnected += ServerDisconnected;
	}

	private void ServerConnected(TcpNetConnection tcpConnection)
	{
		NetIncomingMessage msg = new NetIncomingMessage();
		msg.Type = NetworkMessageType.Connect;
		msg.SenderConnection = tcpConnection;
		messages.Enqueue(msg);
	}

	private void ServerDisconnected(TcpNetConnection tcpConnection)
	{
		NetIncomingMessage msg = new NetIncomingMessage();
		msg.Type = NetworkMessageType.Disconnect;
		msg.SenderConnection = tcpConnection;
		messages.Enqueue(msg);
	}

	private void ServerReceivedMessage(byte[] data, TcpNetConnection tcpConnection)
	{
		NetIncomingMessage msg = new NetIncomingMessage();
		msg.Type = NetworkMessageType.Data;
		msg.message = data;
		msg.messageLength = data.Length;
		msg.SenderConnection = tcpConnection;
		messages.Enqueue(msg);
	}

	public override NetIncomingMessage ReadMessage()
	{
		if (messages.TryDequeue(out var msg))
		{
			return msg;
		}
		return null;
	}

	public override void SetIpAndPort(string ip, int port)
	{
		Ip = ip;
		Port = port;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposed)
		{
			if (disposing)
			{
				cts.Cancel();
				cts.Dispose();
				server.Dispose();
				messages.Clear();
			}
			disposed = true;
		}
	}

	public override void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
