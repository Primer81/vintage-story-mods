using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.Server;

namespace Vintagestory.Client.NoObf;

public class NetworkAPI : ClientSystem, IClientNetworkAPI, INetworkAPI
{
	private Dictionary<int, NetworkChannel> clientchannels = new Dictionary<int, NetworkChannel>();

	private Dictionary<int, UdpNetworkChannel> clientchannelsUdp = new Dictionary<int, UdpNetworkChannel>();

	private Dictionary<int, NetworkChannel> channels = new Dictionary<int, NetworkChannel>();

	private Dictionary<int, UdpNetworkChannel> channelsUdp = new Dictionary<int, UdpNetworkChannel>();

	private int nextFreeChannelId;

	private int nextFreeUdpChannelId;

	private bool serverChannelsReceived;

	private Queue<Packet_Server> earlyPackets = new Queue<Packet_Server>();

	private Queue<Packet_CustomPacket> earlyUdpPackets = new Queue<Packet_CustomPacket>();

	public override string Name => "networkapi";

	public NetworkAPI(ClientMain game)
		: base(game)
	{
		game.PacketHandlers[55] = HandleCustomPacket;
		game.PacketHandlers[56] = HandleChannelsPacket;
		game.HandleCustomUdpPackets = HandleCustomUdpPacket;
	}

	public void SendPlayerNowReady()
	{
		if (!game.clientPlayingFired)
		{
			game.SendPacketClient(new Packet_Client
			{
				Id = 29
			});
			game.clientPlayingFired = true;
		}
	}

	public EnumChannelState GetChannelState(string channelName)
	{
		if (clientchannels.Values.FirstOrDefault((NetworkChannel c) => c.channelName == channelName) == null)
		{
			return EnumChannelState.NotFound;
		}
		if (!serverChannelsReceived)
		{
			return EnumChannelState.Registered;
		}
		if (clientchannels.Values.FirstOrDefault((NetworkChannel c) => c.channelName == channelName) != null)
		{
			return EnumChannelState.Connected;
		}
		return EnumChannelState.NotConnected;
	}

	private void HandleChannelsPacket(Packet_Server packet)
	{
		Dictionary<int, NetworkChannel> matchedchannels = new Dictionary<int, NetworkChannel>();
		Dictionary<int, UdpNetworkChannel> matchedchannelsUdp = new Dictionary<int, UdpNetworkChannel>();
		Packet_NetworkChannels serverPacket = packet.NetworkChannels;
		List<NetworkChannel> clientChannels = new List<NetworkChannel>(clientchannels.Values);
		List<UdpNetworkChannel> clientUdpChannels = new List<UdpNetworkChannel>(clientchannelsUdp.Values);
		int j;
		for (j = 0; j < serverPacket.ChannelNamesCount; j++)
		{
			NetworkChannel channel2 = clientChannels.FirstOrDefault((NetworkChannel ch) => ch.ChannelName == serverPacket.ChannelNames[j]);
			if (channel2 != null)
			{
				clientChannels.Remove(channel2);
				channel2.channelId = serverPacket.ChannelIds[j];
				channel2.Connected = true;
				matchedchannels[serverPacket.ChannelIds[j]] = channel2;
			}
			else
			{
				game.Logger.Warning("Improperly configured mod. Server sends me channel name {0}, but no client side mod registered it.", serverPacket.ChannelNames[j]);
			}
		}
		if (clientChannels.Count > 0)
		{
			game.Logger.Warning("Client registered {0} network channels ({1}) the server does not know about, may cause issues.", clientChannels.Count, string.Join(", ", clientChannels.Select((NetworkChannel ch) => ch.channelName)));
			foreach (NetworkChannel item in clientChannels)
			{
				item.Connected = false;
				item.channelId = 0;
			}
		}
		int i;
		for (i = 0; i < serverPacket.ChannelUdpNamesCount; i++)
		{
			UdpNetworkChannel channel = clientUdpChannels.FirstOrDefault((UdpNetworkChannel ch) => ch.ChannelName == serverPacket.ChannelUdpNames[i]);
			if (channel != null)
			{
				clientUdpChannels.Remove(channel);
				channel.channelId = serverPacket.ChannelUdpIds[i];
				channel.Connected = true;
				matchedchannelsUdp[serverPacket.ChannelUdpIds[i]] = channel;
			}
			else
			{
				game.Logger.Warning("Improperly configured mod. Server sends me udp channel name {0}, but no client side mod registered it.", serverPacket.ChannelUdpNames[i]);
			}
		}
		if (clientUdpChannels.Count > 0)
		{
			game.Logger.Warning("Client registered {0} network udp channels ({1}) the server does not know about, may cause issues.", clientUdpChannels.Count, string.Join(", ", clientUdpChannels.Select((UdpNetworkChannel ch) => ch.channelName)));
			foreach (UdpNetworkChannel item2 in clientUdpChannels)
			{
				item2.Connected = false;
				item2.channelId = 0;
			}
		}
		channels = matchedchannels;
		channelsUdp = matchedchannelsUdp;
		serverChannelsReceived = true;
		while (earlyPackets.Count > 0)
		{
			HandleCustomPacket(earlyPackets.Dequeue());
		}
		while (earlyUdpPackets.Count > 0)
		{
			HandleCustomUdpPacket(earlyUdpPackets.Dequeue());
		}
	}

	private void HandleCustomPacket(Packet_Server packet)
	{
		if (!serverChannelsReceived)
		{
			earlyPackets.Enqueue(packet);
			return;
		}
		Packet_CustomPacket p = packet.CustomPacket;
		if (channels.TryGetValue(p.ChannelId, out var channel))
		{
			channel.OnPacket(p);
		}
	}

	private void HandleCustomUdpPacket(Packet_CustomPacket packet)
	{
		UdpNetworkChannel channel;
		if (!serverChannelsReceived)
		{
			earlyUdpPackets.Enqueue(packet);
		}
		else if (channelsUdp.TryGetValue(packet.ChannelId, out channel))
		{
			channel.OnPacket(packet);
		}
	}

	public IClientNetworkChannel RegisterChannel(string channelName)
	{
		if (serverChannelsReceived)
		{
			throw new Exception("Cannot register network channels at this point. Server already sent his channel list. Make sure to register your network channel early enough, i.e. in StartClientSide().");
		}
		nextFreeChannelId++;
		clientchannels[nextFreeChannelId] = new NetworkChannel(this, nextFreeChannelId, channelName);
		return clientchannels[nextFreeChannelId];
	}

	public IClientNetworkChannel RegisterUdpChannel(string channelName)
	{
		if (serverChannelsReceived)
		{
			throw new Exception("Cannot register network udp channels at this point. Server already sent his udp channel list. Make sure to register your network channel early enough, i.e. in StartClientSide().");
		}
		nextFreeUdpChannelId++;
		clientchannelsUdp[nextFreeUdpChannelId] = new UdpNetworkChannel(this, nextFreeUdpChannelId, channelName);
		return clientchannelsUdp[nextFreeUdpChannelId];
	}

	public void SendArbitraryPacket(byte[] data)
	{
		game.SendArbitraryPacket(data);
	}

	public void SendBlockEntityPacket(int x, int y, int z, int packetId, byte[] data = null)
	{
		game.SendBlockEntityPacket(x, y, z, packetId, data);
	}

	public void SendBlockEntityPacket(BlockPos pos, int packetId, byte[] data = null)
	{
		game.SendBlockEntityPacket(pos.X, pos.InternalY, pos.Z, packetId, data);
	}

	public void SendBlockEntityPacket(int x, int y, int z, object packetClient)
	{
		Packet_Client packet = packetClient as Packet_Client;
		byte[] data = game.Serialize(packet);
		game.SendBlockEntityPacket(x, y, z, packet.Id, data);
	}

	public void SendPacketClient(object packetClient)
	{
		game.SendPacketClient(packetClient as Packet_Client);
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}

	public void SendHandInteraction(int mouseButton, BlockSelection blockSelection, EntitySelection entitySelection, EnumHandInteract beforeUseType, int handInteract, bool firstEvent, EnumItemUseCancelReason cancelReason)
	{
		game.SendHandInteraction(mouseButton, blockSelection, entitySelection, beforeUseType, (EnumHandInteractNw)handInteract, firstEvent, cancelReason);
	}

	public void SendEntityPacket(long entityid, int packetId, byte[] data = null)
	{
		game.SendEntityPacket(entityid, packetId, data);
	}

	public void SendPlayerPositionPacket()
	{
		if (double.IsNaN(game.EntityPlayer.Pos.X))
		{
			throw new ArgumentException("Position is not a number");
		}
		if (double.IsNaN(game.EntityPlayer.Pos.Motion.X))
		{
			throw new ArgumentException("Motion is not a number");
		}
		Packet_EntityPosition packet = ServerPackets.getEntityPositionPacket(game.EntityPlayer.Pos, game.EntityPlayer, 0);
		Packet_UdpPacket packetUdpClient = new Packet_UdpPacket
		{
			Id = 2,
			EntityPosition = packet
		};
		if (game.FallBackToTcp)
		{
			Packet_Client packetClient = new Packet_Client
			{
				Id = 35,
				UdpPacket = packetUdpClient
			};
			game.SendPacketClient(packetClient);
		}
		else
		{
			game.UdpNetClient.Send(packetUdpClient);
		}
	}

	public void SendPlayerMountPositionPacket(Entity mount)
	{
		if (double.IsNaN(mount.Pos.X))
		{
			throw new ArgumentException("Mount Position is not a number");
		}
		if (double.IsNaN(mount.Pos.Motion.X))
		{
			throw new ArgumentException("Mount Motion is not a number");
		}
		Packet_EntityPosition packet = ServerPackets.getEntityPositionPacket(mount.Pos, mount, 0);
		Packet_UdpPacket packetUdpClient = new Packet_UdpPacket
		{
			Id = 3,
			EntityPosition = packet
		};
		if (game.FallBackToTcp)
		{
			Packet_Client packetClient = new Packet_Client
			{
				Id = 35,
				UdpPacket = packetUdpClient
			};
			game.SendPacketClient(packetClient);
		}
		else
		{
			game.UdpNetClient.Send(packetUdpClient);
		}
	}

	public void SendEntityPacket(long entityid, object packetClient)
	{
		Packet_Client packet = packetClient as Packet_Client;
		byte[] data = game.Serialize(packet);
		game.SendEntityPacket(entityid, packet.Id, data);
	}

	public void SendEntityPacketWithOffset(long entityid, int packetIdOffset, object packetClient)
	{
		Packet_Client packet = packetClient as Packet_Client;
		byte[] data = game.Serialize(packet);
		game.SendEntityPacket(entityid, packet.Id + packetIdOffset, data);
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
		return (serverChannelsReceived ? channels : clientchannels).FirstOrDefault((KeyValuePair<int, NetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
	}

	IClientNetworkChannel IClientNetworkAPI.GetChannel(string channelName)
	{
		return (serverChannelsReceived ? channels : clientchannels).FirstOrDefault((KeyValuePair<int, NetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
	}

	public INetworkChannel GetUdpChannel(string channelName)
	{
		return (serverChannelsReceived ? channelsUdp : clientchannelsUdp).FirstOrDefault((KeyValuePair<int, UdpNetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
	}

	IClientNetworkChannel IClientNetworkAPI.GetUdpChannel(string channelName)
	{
		return (serverChannelsReceived ? channelsUdp : clientchannelsUdp).FirstOrDefault((KeyValuePair<int, UdpNetworkChannel> pair) => pair.Value.ChannelName == channelName).Value;
	}

	public void SendBlockEntityPacket<T>(BlockPos pos, int packetId, T data = default(T))
	{
		game.SendBlockEntityPacket(pos.X, pos.InternalY, pos.Z, packetId, SerializerUtil.Serialize(data));
	}
}
