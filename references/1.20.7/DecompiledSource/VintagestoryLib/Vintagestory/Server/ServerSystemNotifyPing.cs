using System.Collections.Generic;

namespace Vintagestory.Server;

public class ServerSystemNotifyPing : ServerSystem
{
	private Timer pingtimer = new Timer
	{
		Interval = 1.0,
		MaxDeltaTime = 5.0
	};

	public ServerSystemNotifyPing(ServerMain server)
		: base(server)
	{
		server.RegisterGameTickListener(OnEveryFewSeconds, 5000);
		server.PacketHandlers[2] = HandlePingReply;
		server.PacketHandlingOnConnectingAllowed[2] = true;
	}

	public override void OnServerTick(float dt)
	{
		pingtimer.Update(PingTimerTick);
	}

	private void OnEveryFewSeconds(float t1)
	{
		server.BroadcastPlayerPings();
	}

	private void HandlePingReply(Packet_Client packet, ConnectedClient client)
	{
		client.Ping.OnReceive(server.totalUnpausedTime.ElapsedMilliseconds);
		client.LastPing = (float)client.Ping.RoundtripTimeTotalMilliseconds() / 1000f;
	}

	private void PingTimerTick()
	{
		if (server.exit.GetExit())
		{
			return;
		}
		long currentMs = server.totalUnpausedTime.ElapsedMilliseconds;
		List<int> clientsToKick = new List<int>();
		foreach (KeyValuePair<int, ConnectedClient> i in server.Clients)
		{
			if (!i.Value.Ping.DidReplyOnLastPing)
			{
				if (i.Value.Ping.DidTimeout(currentMs) && !i.Value.IsSinglePlayerClient)
				{
					float seconds = (currentMs - i.Value.Ping.TimeSendMilliSeconds) / 1000;
					ServerMain.Logger.Notification(seconds + "s ping timeout for " + i.Value.PlayerName + ". Kicking player...");
					clientsToKick.Add(i.Key);
				}
			}
			else
			{
				server.SendPacket(i.Key, ServerPackets.Ping());
				i.Value.Ping.OnSend(currentMs);
			}
		}
		foreach (int key in clientsToKick)
		{
			if (server.Clients.TryGetValue(key, out var connectedClient))
			{
				server.DisconnectPlayer(connectedClient);
			}
		}
	}
}
