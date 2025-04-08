using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

public class NetworkAPI : IServerNetworkAPI, INetworkAPI
{
	internal ServerMain server;

	private OrderedDictionary<int, NetworkChannel> channels = new OrderedDictionary<int, NetworkChannel>();

	private OrderedDictionary<int, UdpNetworkChannel> channelsUdp = new OrderedDictionary<int, UdpNetworkChannel>();

	private int nextFreeChannelId;

	private int nextFreeUdpChannelId;

	public NetworkAPI(ServerMain server)
	{
		this.server = server;
		server.PacketHandlers[23] = HandleCustomPacket;
		server.HandleCustomUdpPackets = HandleCustomUdpPacket;
	}

	private void HandleCustomUdpPacket(Packet_CustomPacket packet, IServerPlayer player)
	{
		if (channelsUdp.TryGetValue(packet.ChannelId, out var channel))
		{
			channel.OnPacket(packet, player);
		}
	}

	public void SendChannelsPacket(IServerPlayer player)
	{
		Packet_NetworkChannels p = new Packet_NetworkChannels();
		p.SetChannelIds(channels.Keys.ToArray());
		p.SetChannelNames(channels.Values.Select((NetworkChannel ch) => ch.ChannelName).ToArray());
		p.SetChannelUdpIds(channelsUdp.Keys.ToArray());
		p.SetChannelUdpNames(channelsUdp.Values.Select((UdpNetworkChannel ch) => ch.ChannelName).ToArray());
		server.SendPacket(player.ClientId, new Packet_Server
		{
			Id = 56,
			NetworkChannels = p
		});
	}

	private void HandleCustomPacket(Packet_Client packet, ConnectedClient client)
	{
		Packet_CustomPacket p = packet.CustomPacket;
		if (channels.TryGetValue(p.ChannelId, out var channel))
		{
			channel.OnPacket(p, client.Player);
		}
	}

	public IServerNetworkChannel RegisterChannel(string channelName)
	{
		nextFreeChannelId++;
		channels[nextFreeChannelId] = new NetworkChannel(this, nextFreeChannelId, channelName);
		return channels[nextFreeChannelId];
	}

	public IServerNetworkChannel RegisterUdpChannel(string channelName)
	{
		nextFreeUdpChannelId++;
		channelsUdp[nextFreeUdpChannelId] = new UdpNetworkChannel(this, nextFreeUdpChannelId, channelName);
		return channelsUdp[nextFreeUdpChannelId];
	}

	public void BroadcastArbitraryPacket(byte[] data, params IServerPlayer[] exceptPlayers)
	{
		server.BroadcastArbitraryPacket(data, exceptPlayers);
	}

	public void BroadcastBlockEntityPacket(BlockPos pos, int packetId, byte[] data = null)
	{
		server.BroadcastBlockEntityPacket(pos.X, pos.InternalY, pos.Z, packetId, data);
	}

	public void BroadcastBlockEntityPacket(BlockPos pos, int packetId, byte[] data = null, params IServerPlayer[] skipPlayers)
	{
		server.BroadcastBlockEntityPacket(pos.X, pos.InternalY, pos.Z, packetId, data, skipPlayers);
	}

	public void SendArbitraryPacket(byte[] data, params IServerPlayer[] players)
	{
		server.SendArbitraryPacket(data, players);
	}

	public void SendBlockEntityPacket(IServerPlayer player, BlockPos pos, int packetId, byte[] data = null)
	{
		server.SendBlockEntityMessagePacket(player, pos.X, pos.InternalY, pos.Z, packetId, data);
	}

	public void SendEntityPacket(IServerPlayer player, long entityid, int packetId, byte[] data = null)
	{
		server.SendEntityPacket(player, entityid, packetId, data);
	}

	public void BroadcastEntityPacket(long entityid, int packetId, byte[] data = null)
	{
		server.BroadcastEntityPacket(entityid, packetId, data);
	}

	INetworkChannel INetworkAPI.RegisterChannel(string channelName)
	{
		return RegisterChannel(channelName);
	}

	INetworkChannel INetworkAPI.RegisterUdpChannel(string channelName)
	{
		return RegisterUdpChannel(channelName);
	}

	public INetworkChannel GetChannel(string channelName)
	{
		return channels.FirstOrDefault((KeyValuePair<int, NetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
	}

	IServerNetworkChannel IServerNetworkAPI.GetChannel(string channelName)
	{
		return channels.FirstOrDefault((KeyValuePair<int, NetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
	}

	public INetworkChannel GetUdpChannel(string channelName)
	{
		return channelsUdp.FirstOrDefault((KeyValuePair<int, UdpNetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
	}

	IServerNetworkChannel IServerNetworkAPI.GetUdpChannel(string channelName)
	{
		return channelsUdp.FirstOrDefault((KeyValuePair<int, UdpNetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
	}

	public void SendBlockEntityPacket<T>(IServerPlayer player, BlockPos pos, int packetId, T data = default(T))
	{
		server.SendBlockEntityMessagePacket(player, pos.X, pos.InternalY, pos.Z, packetId, SerializerUtil.Serialize(data));
	}

	public void BroadcastBlockEntityPacket<T>(BlockPos pos, int packetId, T data = default(T))
	{
		server.BroadcastBlockEntityPacket(pos.X, pos.InternalY, pos.Z, packetId, SerializerUtil.Serialize(data));
	}
}
