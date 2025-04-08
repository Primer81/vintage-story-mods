using System;
using System.IO;
using ProtoBuf;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class UdpNetworkChannel : NetworkChannel
{
	internal Action<Packet_CustomPacket, IServerPlayer>[] handlersUdp = new Action<Packet_CustomPacket, IServerPlayer>[256];

	public UdpNetworkChannel(NetworkAPI api, int channelId, string channelName)
		: base(api, channelId, channelName)
	{
	}

	public new void OnPacket(Packet_CustomPacket packet, IServerPlayer player)
	{
		if (packet.MessageId < handlersUdp.Length)
		{
			handlersUdp[packet.MessageId]?.Invoke(packet, player);
		}
	}

	public override IServerNetworkChannel SetMessageHandler<T>(NetworkClientMessageHandler<T> handler)
	{
		int messageId = 0;
		if (!messageTypes.TryGetValue(typeof(T), out messageId))
		{
			throw new Exception("No such message type " + typeof(T)?.ToString() + " registered. Did you forgot to call RegisterMessageType?");
		}
		if (typeof(T).IsArray)
		{
			throw new ArgumentException("Please do not use array messages, they seem to cause serialization problems in rare cases. Pack that array into its own class.");
		}
		Serializer.PrepareSerializer<T>();
		handlersUdp[messageId] = delegate(Packet_CustomPacket p, IServerPlayer player)
		{
			T packet;
			using (FastMemoryStream source = new FastMemoryStream(p.Data, p.Data.Length))
			{
				packet = Serializer.Deserialize<T>(source);
			}
			handler(player, packet);
		};
		return this;
	}

	public override void BroadcastPacket<T>(T message, params IServerPlayer[] exceptPlayers)
	{
		if (!messageTypes.TryGetValue(typeof(T), out var messageId))
		{
			throw new Exception("No such message type " + typeof(T)?.ToString() + " registered. Did you forgot to call RegisterMessageType?");
		}
		using FastMemoryStream ms = new FastMemoryStream();
		Serializer.Serialize((Stream)ms, message);
		Packet_CustomPacket udpChannelPacket = new Packet_CustomPacket
		{
			ChannelId = channelId,
			MessageId = messageId,
			Data = ms.ToArray()
		};
		Packet_UdpPacket udpPacket = new Packet_UdpPacket
		{
			Id = 6,
			ChannelPaket = udpChannelPacket
		};
		api.server.BroadcastArbitraryUdpPacket(udpPacket, exceptPlayers);
	}

	public override void SendPacket<T>(T message, params IServerPlayer[] players)
	{
		if (players == null || players.Length == 0)
		{
			throw new ArgumentNullException("No players supplied to send the packet to");
		}
		if (!messageTypes.TryGetValue(typeof(T), out var messageId))
		{
			throw new Exception("No such message type " + typeof(T)?.ToString() + " registered. Did you forgot to call RegisterMessageType?");
		}
		using FastMemoryStream ms = new FastMemoryStream();
		Serializer.Serialize((Stream)ms, message);
		Packet_CustomPacket udpChannelPacket = new Packet_CustomPacket
		{
			ChannelId = channelId,
			MessageId = messageId,
			Data = ms.ToArray()
		};
		Packet_UdpPacket udpPacket = new Packet_UdpPacket
		{
			Id = 6,
			ChannelPaket = udpChannelPacket
		};
		api.server.SendArbitraryUdpPacket(udpPacket, players);
	}
}
