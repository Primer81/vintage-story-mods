using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Vintagestory.Server.Network;

public class TcpNetConnection : NetConnection
{
	public static HashSet<string> blockedIps = new HashSet<string>();

	public static bool TemporaryIpBlockList = false;

	public const int ClientSocketBufferSize = 4096;

	public static int MaxPacketClientId = DetermineMaxPacketId();

	public const int clientIdentificationPacketId = 8;

	public const int pingPacketId = 18;

	public Socket TcpSocket;

	public string Address;

	public IPEndPoint IpEndpoint;

	public bool Connected;

	private bool _disposed;

	private Memory<byte> dataRcvBuf = new byte[4096];

	private CancellationTokenSource cts;

	public int MaxPacketSize = 5000;

	public event OnReceivedMessageDelegate ReceivedMessage;

	public event TcpConnectionDelegate Disconnected;

	public void SetLengthLimit(bool isCreative)
	{
		MaxPacketSize = (isCreative ? int.MaxValue : 1000000);
	}

	public void StartReceiving()
	{
		cts = new CancellationTokenSource();
		Task.Run((Func<Task?>)ReceiveData);
	}

	private async Task ReceiveData()
	{
		try
		{
			FastMemoryStream receivedBytes = null;
			while (TcpSocket.Connected && !cts.Token.IsCancellationRequested)
			{
				int nBytesRec;
				try
				{
					nBytesRec = await TcpSocket.ReceiveAsync(dataRcvBuf, cts.Token);
				}
				catch
				{
					InvokeDisconnected();
					break;
				}
				if (nBytesRec <= 0)
				{
					InvokeDisconnected();
					break;
				}
				if ((base.client == null || base.client.IsNewClient) && nBytesRec > 4 && (receivedBytes == null || receivedBytes.Position == 0L))
				{
					int peekPacketId = dataRcvBuf.Span[4];
					if (peekPacketId != 8 && peekPacketId != 18)
					{
						DisconnectForBadPacket("Client " + Address + " disconnected, invalid packet received");
						break;
					}
				}
				if (receivedBytes == null)
				{
					receivedBytes = new FastMemoryStream(512);
				}
				receivedBytes.Write(dataRcvBuf.Span.Slice(0, nBytesRec));
				while (receivedBytes.Position >= 4)
				{
					byte[] receivedBytesArray = receivedBytes.GetBuffer();
					int packetLength = NetIncomingMessage.ReadInt(receivedBytesArray);
					bool compressed = packetLength < 0;
					packetLength &= 0x7FFFFFFF;
					if (packetLength > MaxPacketSize)
					{
						DisconnectForBadPacket($"Client {Address} disconnected, too large packet of {packetLength} bytes received");
						return;
					}
					if (packetLength == 0)
					{
						receivedBytes.RemoveFromStart(4);
						continue;
					}
					if (receivedBytes.Position < 4 + packetLength)
					{
						break;
					}
					byte[] packet;
					if (compressed)
					{
						packet = Compression.Decompress(receivedBytesArray, 4, packetLength);
					}
					else
					{
						packet = new byte[packetLength];
						for (int i = 0; i < packetLength; i++)
						{
							packet[i] = receivedBytesArray[4 + i];
						}
					}
					receivedBytes.RemoveFromStart(4 + packetLength);
					int packetId = ProtocolParser.PeekPacketId(packet);
					if (packetId <= 0 || packetId >= (MaxPacketClientId + 1) * 8)
					{
						DisconnectForBadPacket("Client " + Address + " disconnected, send packet with invalid client packet id: " + packetId);
						return;
					}
					this.ReceivedMessage(packet, this);
				}
			}
		}
		catch
		{
			InvokeDisconnected();
		}
	}

	private void DisconnectForBadPacket(string msg)
	{
		if (TemporaryIpBlockList)
		{
			blockedIps.Add(((IPEndPoint)TcpSocket.RemoteEndPoint).Address.ToString());
		}
		InvokeDisconnected();
		ServerMain.Logger.Notification(msg);
	}

	public override EnumSendResult Send(byte[] data, bool compressedFlag)
	{
		try
		{
			int length = data.Length;
			byte[] dataWithLength = new byte[length + 4];
			NetIncomingMessage.WriteInt(dataWithLength, length | (int)((compressedFlag ? 1u : 0u) << 31));
			for (int i = 0; i < length; i++)
			{
				dataWithLength[4 + i] = data[i];
			}
			TcpSocket.SendAsync(dataWithLength, SocketFlags.None, cts.Token);
			return EnumSendResult.Ok;
		}
		catch
		{
			InvokeDisconnected();
			return EnumSendResult.Disconnected;
		}
	}

	public EnumSendResult SendPreparedBytes(byte[] dataWithLength, int length, bool compressedFlag)
	{
		try
		{
			NetIncomingMessage.WriteInt(dataWithLength, length | (int)((compressedFlag ? 1u : 0u) << 31));
			TcpSocket.SendAsync(dataWithLength, SocketFlags.None, cts.Token);
			return EnumSendResult.Ok;
		}
		catch
		{
			InvokeDisconnected();
			return EnumSendResult.Disconnected;
		}
	}

	public override string ToString()
	{
		if (Address != null)
		{
			return Address;
		}
		return base.ToString();
	}

	public TcpNetConnection(Socket tcpSocket)
	{
		TcpSocket = tcpSocket;
		if (tcpSocket.RemoteEndPoint is IPEndPoint enpoint)
		{
			IpEndpoint = enpoint;
			Address = enpoint.Address.ToString();
		}
		else
		{
			IpEndpoint = new IPEndPoint(0L, 0);
			Address = "0.0.0.0";
		}
	}

	public override IPEndPoint RemoteEndPoint()
	{
		return IpEndpoint;
	}

	public override EnumSendResult HiPerformanceSend(BoxedPacket box, ILogger Logger, bool compressionAllowed)
	{
		int len = box.Length;
		bool compressed = false;
		byte[] packet;
		if (len > 1460 && compressionAllowed)
		{
			packet = Compression.CompressOffset4(box.buffer, len);
			len = packet.Length - 4;
			compressed = true;
		}
		else
		{
			packet = box.Clone(4);
		}
		try
		{
			EnumSendResult result = SendPreparedBytes(packet, len, compressed);
			box.LengthSent = len;
			return result;
		}
		catch (Exception e)
		{
			Logger.Error("Network exception:");
			Logger.Error(e);
			return EnumSendResult.Error;
		}
	}

	public override bool EqualsConnection(NetConnection connection)
	{
		return TcpSocket == ((TcpNetConnection)connection).TcpSocket;
	}

	public override void Shutdown()
	{
		if (TcpSocket == null)
		{
			return;
		}
		try
		{
			TcpSocket.Shutdown(SocketShutdown.Both);
		}
		catch
		{
		}
	}

	public override void Close()
	{
		try
		{
			cts?.Cancel();
		}
		catch
		{
		}
		try
		{
			TcpSocket?.Close();
		}
		catch
		{
		}
		Dispose();
	}

	internal void InvokeDisconnected()
	{
		if (!_disposed)
		{
			try
			{
				cts.Cancel();
			}
			catch
			{
			}
			try
			{
				TcpSocket.Close();
			}
			catch
			{
			}
			if (this.Disconnected != null && TcpSocket != null && Connected)
			{
				this.Disconnected(this);
				Connected = false;
			}
			Dispose();
		}
	}

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			TcpSocket.Dispose();
			cts.Dispose();
			TcpSocket = null;
			cts = null;
		}
	}

	public static int DetermineMaxPacketId()
	{
		MemberInfo[] members = typeof(Packet_ClientIdEnum).GetMembers();
		int maxid = 0;
		MemberInfo[] array = members;
		foreach (MemberInfo member in array)
		{
			if (member.MemberType == MemberTypes.Field && member is FieldInfo f && f.FieldType.Name == "Int32" && f.GetValue(f) is int id && id > maxid)
			{
				maxid = id;
			}
		}
		return maxid;
	}
}
