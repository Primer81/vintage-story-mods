using System;
using System.Collections.Generic;

namespace Vintagestory.Server;

internal class ServerSystemClientAwareness : ServerSystem
{
	private Dictionary<int, ClientStatistics> clients = new Dictionary<int, ClientStatistics>();

	public ServerSystemClientAwareness(ServerMain server)
		: base(server)
	{
		server.clientAwarenessEvents = new Dictionary<EnumClientAwarenessEvent, List<Action<ClientStatistics>>>();
		server.clientAwarenessEvents[EnumClientAwarenessEvent.ChunkTransition] = new List<Action<ClientStatistics>>();
	}

	public override int GetUpdateInterval()
	{
		return 100;
	}

	public override void OnServerTick(float dt)
	{
		foreach (ClientStatistics clientStats in clients.Values)
		{
			EnumClientAwarenessEvent? clientEvent = clientStats.DetectChanges();
			if (!clientEvent.HasValue)
			{
				continue;
			}
			foreach (Action<ClientStatistics> item in server.clientAwarenessEvents[clientEvent.Value])
			{
				item(clientStats);
			}
		}
	}

	public void TriggerEvent(EnumClientAwarenessEvent clientEvent, int clientId)
	{
		if (!clients.TryGetValue(clientId, out var clientStats) || !server.clientAwarenessEvents.TryGetValue(clientEvent, out var actions))
		{
			return;
		}
		foreach (Action<ClientStatistics> item in actions)
		{
			item(clientStats);
		}
	}

	public override void OnPlayerJoin(ServerPlayer player)
	{
		clients[player.ClientId] = new ClientStatistics
		{
			client = player.client,
			lastChunkX = (int)player.Entity.ServerPos.X / MagicNum.ServerChunkSize,
			lastChunkY = (int)player.Entity.ServerPos.Y / MagicNum.ServerChunkSize,
			lastChunkZ = (int)player.Entity.ServerPos.Z / MagicNum.ServerChunkSize
		};
	}

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		clients.Remove(player.ClientId);
	}
}
