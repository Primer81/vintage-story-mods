using System;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client;

public class TcpNetClient : NetClient
{
	private INetworkConnection tcpConnection;

	private QueueByte incoming;

	private byte[] data;

	private const int dataLength = 16384;

	private int received;

	public override int CurrentlyReceivingBytes => received + incoming.count;

	public TcpNetClient()
	{
		incoming = new QueueByte();
		data = new byte[16384];
	}

	public override void Dispose()
	{
		if (tcpConnection != null)
		{
			tcpConnection.Disconnect();
		}
	}

	public override void Connect(string host, int port, Action<ConnectionResult> OnConnectionResult, Action<Exception> OnDisconnected)
	{
		tcpConnection = new TCPNetworkConnection(host, port, OnConnectionResult, OnDisconnected);
	}

	public override NetIncomingMessage ReadMessage()
	{
		if (tcpConnection == null)
		{
			return null;
		}
		NetIncomingMessage message = TryGetMessageFromIncoming();
		if (message != null)
		{
			return message;
		}
		for (int j = 0; j < 1; j++)
		{
			received = 0;
			tcpConnection.Receive(data, 16384, out received);
			if (received <= 0)
			{
				break;
			}
			for (int i = 0; i < received; i++)
			{
				incoming.Enqueue(data[i]);
			}
		}
		return TryGetMessageFromIncoming();
	}

	private NetIncomingMessage TryGetMessageFromIncoming()
	{
		if (incoming.count >= 4)
		{
			byte[] length = new byte[4];
			incoming.PeekRange(length, 4);
			int num = NetIncomingMessage.ReadInt(length);
			bool compressed = ((uint)num & int.MinValue) > 0;
			int messageLength = num & 0x7FFFFFFF;
			if (incoming.count >= 4 + messageLength)
			{
				incoming.DequeueRange(new byte[4], 4);
				NetIncomingMessage msg = new NetIncomingMessage();
				msg.message = new byte[messageLength];
				msg.messageLength = messageLength;
				msg.originalMessageLength = messageLength;
				incoming.DequeueRange(msg.message, msg.messageLength);
				if (compressed)
				{
					msg.message = Compression.Decompress(msg.message);
					msg.messageLength = msg.message.Length;
				}
				return msg;
			}
		}
		return null;
	}

	public override void Send(byte[] data)
	{
		byte[] packet = new byte[data.Length + 4];
		NetIncomingMessage.WriteInt(packet, data.Length);
		for (int i = 0; i < data.Length; i++)
		{
			packet[i + 4] = data[i];
		}
		tcpConnection.Send(packet, data.Length + 4);
	}
}
