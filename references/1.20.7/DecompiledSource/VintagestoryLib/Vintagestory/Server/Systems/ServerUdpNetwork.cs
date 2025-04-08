using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;

namespace Vintagestory.Server.Systems;

public class ServerUdpNetwork : ServerSystem
{
	public readonly Dictionary<string, ConnectedClient> connectingClients = new Dictionary<string, ConnectedClient>();

	public readonly IServerNetworkChannel ServerNetworkChannel;

	public PhysicsManager physicsManager;

	public ServerUdpNetwork(ServerMain server)
		: base(server)
	{
		server.RegisterGameTickListener(ServerTickUdp, 15);
		ServerNetworkChannel = server.api.Network.RegisterChannel("UdpSignals");
		ServerNetworkChannel.RegisterMessageType<AnimationPacket>().RegisterMessageType<BulkAnimationPacket>();
		physicsManager = new PhysicsManager(server.api, this);
		server.PacketHandlers[35] = EnqueueUdpPacket;
	}

	private void EnqueueUdpPacket(Packet_Client packet, ConnectedClient player)
	{
		UdpPacket udpPacket2 = default(UdpPacket);
		udpPacket2.Packet = packet.UdpPacket;
		udpPacket2.Player = player.Player;
		UdpPacket udpPacket = udpPacket2;
		server.UdpSockets[1].EnqueuePacket(udpPacket);
	}

	private void ServerTickUdp(float obj)
	{
		UNetServer[] udpSockets = server.UdpSockets;
		foreach (UNetServer udpSocket in udpSockets)
		{
			UdpPacket[] packets = udpSocket?.ReadMessage();
			if (packets == null)
			{
				continue;
			}
			UdpPacket[] array = packets;
			for (int j = 0; j < array.Length; j++)
			{
				UdpPacket packet = array[j];
				server.TotalReceivedBytesUdp += packet.Packet.Length;
				switch (packet.Packet.Id)
				{
				case 1:
					HandleConnectionRequest(packet, udpSocket);
					break;
				case 2:
					HandlePlayerPosition(packet.Packet.EntityPosition, packet.Player);
					break;
				case 3:
					HandleMountPosition(packet.Packet.EntityPosition, packet.Player);
					break;
				case 6:
					server.HandleCustomUdpPackets(packet.Packet.ChannelPaket, packet.Player);
					break;
				}
			}
		}
	}

	public override void OnBeginInitialization()
	{
		physicsManager.Init();
	}

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		KeyValuePair<string, ConnectedClient> cc = connectingClients.FirstOrDefault((KeyValuePair<string, ConnectedClient> c) => c.Value.Player?.Equals(player) ?? false);
		if (cc.Key != null)
		{
			connectingClients.Remove(cc.Key);
		}
		if (player.client.IsSinglePlayerClient)
		{
			server.UdpSockets[0].Remove(player);
		}
		else
		{
			server.UdpSockets[1].Remove(player);
		}
		server.api.Logger.Notification("UDP: client disconnected " + player.PlayerName);
	}

	private void HandleConnectionRequest(UdpPacket udpPacket, UNetServer uNetServer)
	{
		try
		{
			Packet_ConnectionPacket packet = udpPacket.Packet.ConnectionPacket;
			if (packet == null)
			{
				return;
			}
			connectingClients.TryGetValue(packet?.LoginToken, out var client);
			if (client == null || uNetServer.EndPoints.ContainsKey(udpPacket.EndPoint))
			{
				return;
			}
			connectingClients.Remove(packet.LoginToken);
			uNetServer.Add(udpPacket.EndPoint, client.Id);
			client.ServerDidReceiveUdp = true;
			server.api.Logger.Notification($"UDP: Client {client.Id} connected via: {udpPacket.EndPoint}");
			Packet_Server didRecive = new Packet_Server
			{
				Id = 81
			};
			server.SendPacket(client.Id, didRecive);
			string clientLoginToken = client.LoginToken;
			if (client.IsSinglePlayerClient)
			{
				return;
			}
			Task.Run(async delegate
			{
				Packet_UdpPacket con = new Packet_UdpPacket
				{
					Id = 1,
					ConnectionPacket = new Packet_ConnectionPacket
					{
						LoginToken = clientLoginToken
					}
				};
				for (int i = 0; i < 20; i++)
				{
					int bytesSend = server.UdpSockets[1].SendToClient(client.Id, con);
					server.UpdateUdpStatsAndBenchmark(con, bytesSend);
					await Task.Delay(500);
					if (client.State == EnumClientState.Offline || client.FallBackToTcp)
					{
						break;
					}
				}
			});
		}
		catch (Exception e)
		{
			server.api.Logger.Warning($"Error when connecting UDP client from {udpPacket.EndPoint}");
			server.api.Logger.Warning(e);
		}
	}

	public void HandlePlayerPosition(Packet_EntityPosition packet, ServerPlayer player)
	{
		if (packet == null)
		{
			return;
		}
		EntityPlayer entity = player.Entity;
		int version = entity.WatchedAttributes.GetInt("positionVersionNumber");
		if (packet.PositionVersion < version)
		{
			return;
		}
		player.LastReceivedClientPosition = server.ElapsedMilliseconds;
		int currentTick = entity.Attributes.GetInt("tick");
		currentTick++;
		entity.Attributes.SetInt("tick", currentTick);
		entity.ServerPos.SetFromPacket(packet, entity);
		entity.Pos.SetFromPacket(packet, entity);
		foreach (EntityBehavior behavior in entity.SidedProperties.Behaviors)
		{
			if (behavior is IRemotePhysics remote)
			{
				remote.OnReceivedClientPos(version);
				break;
			}
		}
		Packet_EntityPosition entityPositionPacket = ServerPackets.getEntityPositionPacket(entity.ServerPos, entity, currentTick);
		entityPositionPacket.BodyYaw = CollectibleNet.SerializeFloatPrecise(entity.BodyYawServer);
		Packet_UdpPacket packetBytesUdp = new Packet_UdpPacket
		{
			Id = 5,
			EntityPosition = entityPositionPacket
		};
		entity.IsTeleport = false;
		AnimationPacket animationPacket = new AnimationPacket(entity);
		bool animationsDirty = entity.AnimManager?.AnimationsDirty ?? false;
		IPlayer[] allOnlinePlayers = server.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer sp = (IServerPlayer)allOnlinePlayers[i];
			if (sp != player && server.Clients[sp.ClientId].TrackedEntities.TryGetValue(entity.EntityId, out var _))
			{
				server.SendPacket(sp.ClientId, packetBytesUdp);
				if (animationsDirty)
				{
					ServerNetworkChannel.SendPacket(animationPacket, sp);
				}
			}
		}
		if (animationsDirty)
		{
			entity.AnimManager.AnimationsDirty = false;
		}
	}

	public void HandleMountPosition(Packet_EntityPosition packet, ServerPlayer player)
	{
		if (packet == null)
		{
			return;
		}
		Entity entity = server.api.World.GetEntityById(packet.EntityId);
		IMountable mount = entity?.GetInterface<IMountable>();
		if (mount == null || !mount.IsMountedBy(player.Entity))
		{
			return;
		}
		int version = entity.WatchedAttributes.GetInt("positionVersionNumber");
		if (packet.PositionVersion < version)
		{
			return;
		}
		int currentTick = entity.Attributes.GetInt("tick");
		currentTick++;
		entity.Attributes.SetInt("tick", currentTick);
		entity.ServerPos.SetFromPacket(packet, entity);
		entity.Pos.SetFromPacket(packet, entity);
		foreach (EntityBehavior behavior in entity.SidedProperties.Behaviors)
		{
			if (behavior is IRemotePhysics remote)
			{
				remote.OnReceivedClientPos(version);
				break;
			}
		}
		Packet_EntityPosition entityPositionPacket = ServerPackets.getEntityPositionPacket(entity.ServerPos, entity, currentTick);
		Packet_UdpPacket packetBytesUdp = new Packet_UdpPacket
		{
			Id = 5,
			EntityPosition = entityPositionPacket
		};
		entity.IsTeleport = false;
		IPlayer[] allOnlinePlayers = server.AllOnlinePlayers;
		bool value;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer sp2 = (IServerPlayer)allOnlinePlayers[i];
			if (server.Clients[sp2.ClientId].TrackedEntities.TryGetValue(entity.EntityId, out value))
			{
				server.SendPacket(sp2.ClientId, packetBytesUdp);
			}
		}
		IAnimationManager animManager = entity.AnimManager;
		if (animManager == null || !animManager.AnimationsDirty)
		{
			return;
		}
		AnimationPacket animationPacket = new AnimationPacket(entity);
		allOnlinePlayers = server.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer sp = (IServerPlayer)allOnlinePlayers[i];
			if (server.Clients[sp.ClientId].TrackedEntities.TryGetValue(entity.EntityId, out value))
			{
				ServerNetworkChannel.SendPacket(animationPacket, sp);
			}
		}
		entity.AnimManager.AnimationsDirty = false;
	}

	public void SendBulkPositionPacket(Packet_BulkEntityPosition bulkPositionPacket, ServerPlayer clientPlayer)
	{
		Packet_UdpPacket packet = new Packet_UdpPacket
		{
			Id = 4,
			BulkPositions = bulkPositionPacket
		};
		server.SendPacket(clientPlayer.client, packet);
	}
}
