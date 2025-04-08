using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class PlayerPacketMonitor : ServerSystem
{
	private class MonitorClient
	{
		public int Id = -1;

		public int[] PacketsReceivedById = new int[100];

		public int PacketsReceived;

		public int BlocksSet;

		public int MessagesSent;

		public Punishment SetBlockPunishment;

		public Punishment MessagePunishment;

		public bool SetBlockPunished()
		{
			if (SetBlockPunishment == null)
			{
				return false;
			}
			return SetBlockPunishment.Active();
		}

		public bool MessagePunished()
		{
			if (MessagePunishment == null)
			{
				return false;
			}
			return MessagePunishment.Active();
		}
	}

	private class Punishment
	{
		private DateTime punishmentStartDate;

		private bool permanent;

		private TimeSpan duration;

		public Punishment(TimeSpan duration)
		{
			punishmentStartDate = DateTime.UtcNow;
			this.duration = duration;
			permanent = false;
		}

		public Punishment()
		{
			punishmentStartDate = DateTime.UtcNow;
			duration = TimeSpan.MinValue;
			permanent = true;
		}

		public bool Active()
		{
			if (permanent)
			{
				return true;
			}
			if (DateTime.UtcNow.Subtract(punishmentStartDate).CompareTo(duration) == -1)
			{
				return true;
			}
			return false;
		}
	}

	public class ServerMonitorConfig
	{
		public Dictionary<int, int> PacketLimits = new Dictionary<int, int>();

		public int MaxPackets;

		public int MaxBlocks;

		public int MaxMessages;

		public int MessageBanTime;

		public int TimeIntervall;

		public ServerMonitorConfig()
		{
			MaxPackets = 1000;
			MaxBlocks = 100;
			MaxMessages = 10;
			MessageBanTime = 60;
			TimeIntervall = 3;
		}
	}

	private ServerMonitorConfig config = new ServerMonitorConfig();

	private Dictionary<int, MonitorClient> monitorClients;

	private string filename = "servermonitor.json";

	public PlayerPacketMonitor(ServerMain server)
		: base(server)
	{
		LoadConfig();
		monitorClients = new Dictionary<int, MonitorClient>();
	}

	public override int GetUpdateInterval()
	{
		return 3000;
	}

	public bool RemoveMonitorClient(int clientid)
	{
		return monitorClients.Remove(clientid);
	}

	public override void OnServerTick(float dt)
	{
		foreach (KeyValuePair<int, MonitorClient> i in monitorClients)
		{
			i.Value.BlocksSet = 0;
			i.Value.MessagesSent = 0;
			i.Value.PacketsReceived = 0;
			i.Value.PacketsReceivedById = new int[100];
		}
	}

	private bool HaveOverflow(MonitorClient monitor, int packetid)
	{
		if (!config.PacketLimits.ContainsKey(packetid) || monitor.PacketsReceivedById[packetid] <= config.PacketLimits[packetid])
		{
			return monitor.PacketsReceived > config.MaxPackets;
		}
		return true;
	}

	private string OverflowReason(MonitorClient monitor, int lastpacketid, int mostsendpacketId)
	{
		if (config.PacketLimits.ContainsKey(lastpacketid) && monitor.PacketsReceivedById[lastpacketid] > config.PacketLimits[lastpacketid])
		{
			return "Packet with id " + lastpacketid + " was sent more often than max allowed of " + monitor.PacketsReceivedById[lastpacketid];
		}
		if (monitor.PacketsReceived > config.MaxPackets)
		{
			return "Total sum of packet exceeded max allowed of " + config.MaxPackets + ", mostly packet id " + mostsendpacketId + " (" + monitor.PacketsReceivedById[mostsendpacketId] + " times)";
		}
		return "unknown";
	}

	public bool CheckPacket(int clientId, Packet_Client packet)
	{
		if (!monitorClients.ContainsKey(clientId))
		{
			monitorClients.Add(clientId, new MonitorClient
			{
				Id = clientId
			});
		}
		MonitorClient monitorClient = monitorClients[clientId];
		monitorClient.PacketsReceived++;
		monitorClient.PacketsReceivedById[packet.Id]++;
		ConnectedClient client = server.Clients[clientId];
		if (HaveOverflow(monitorClient, packet.Id))
		{
			_ = client.PlayerName;
			string message = Lang.Get("Automatically kicked by packet monitor, reason: {0}", "Packet overflow");
			server.DisconnectPlayer(client, message);
			int packetId = monitorClient.PacketsReceivedById.ToList().IndexOf(monitorClient.PacketsReceivedById.Max());
			ServerMain.Logger.Notification(OverflowReason(monitorClient, packet.Id, packetId));
			return false;
		}
		switch (packet.Id)
		{
		case 3:
			return true;
		case 4:
			if (monitorClients[clientId].MessagePunished())
			{
				server.SendMessage(client.Player, packet.Chatline.Groupid, Lang.Get("Spam protection in place, message not sent"), EnumChatType.Notification);
				return false;
			}
			if (monitorClients[clientId].MessagesSent < config.MaxMessages)
			{
				monitorClients[clientId].MessagesSent++;
				return true;
			}
			return ActionMessage(client.Player, packet.Chatline.Groupid);
		default:
			return true;
		}
	}

	private bool ActionMessage(IServerPlayer player, int groupid)
	{
		monitorClients[player.ClientId].MessagePunishment = new Punishment(new TimeSpan(0, 0, config.MessageBanTime));
		string msg = Lang.Get("You've sent too many message at once, you've been muted for {0} seconds", config.MessageBanTime);
		player.SendMessage(groupid, msg, EnumChatType.Notification);
		return false;
	}

	private void LoadConfig()
	{
		if (!File.Exists(filename))
		{
			ServerMain.Logger.Notification("servermonitor.json not found, creating new one");
			SaveConfig();
		}
		else
		{
			using TextReader textReader = new StreamReader(filename);
			config = JsonConvert.DeserializeObject<ServerMonitorConfig>(textReader.ReadToEnd());
			textReader.Close();
			SaveConfig();
		}
		ServerMain.Logger.Notification("servermonitor.json now loaded");
	}

	public void SaveConfig()
	{
		using TextWriter textWriter = new StreamWriter(filename);
		textWriter.Write(JsonConvert.SerializeObject(config));
		textWriter.Close();
	}
}
