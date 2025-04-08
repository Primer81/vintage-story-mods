using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

internal class CmdStats
{
	private ServerMain server;

	public CmdStats(ServerMain server)
	{
		this.server = server;
		server.api.commandapi.Create("stats").RequiresPrivilege(Privilege.controlserver).WithArgs(server.api.commandapi.Parsers.OptionalWord("compact"))
			.HandleWith(handleStats);
	}

	private TextCommandResult handleStats(TextCommandCallingArgs args)
	{
		string ending = (((string)args[0] == "compact") ? ";" : "\n");
		return TextCommandResult.Success(genStats(server, ending));
	}

	public static string genStats(ServerMain server, string ending)
	{
		StringBuilder sb = new StringBuilder();
		long totalsecondsup = server.totalUpTime.ElapsedMilliseconds / 1000;
		long secondsup = server.totalUpTime.ElapsedMilliseconds / 1000;
		int minutesup = 0;
		int hoursup = 0;
		int daysup = 0;
		if (secondsup > 60)
		{
			minutesup = (int)(secondsup / 60);
			secondsup -= 60 * minutesup;
		}
		if (minutesup > 60)
		{
			hoursup = minutesup / 60;
			minutesup -= 60 * hoursup;
		}
		if (hoursup > 24)
		{
			daysup = hoursup / 24;
			hoursup -= 24 * daysup;
		}
		int clientCount = ((server.Clients != null) ? server.Clients.Values.Count((ConnectedClient x) => x.State != EnumClientState.Queued) : 0);
		if (clientCount > 0)
		{
			server.lastDisconnectTotalMs = server.totalUpTime.ElapsedMilliseconds;
		}
		int lastonlinesec = Math.Max(0, (int)(totalsecondsup - server.lastDisconnectTotalMs / 1000));
		sb.Append("Version: 1.20.7");
		sb.Append(ending);
		sb.Append($"Uptime: {daysup} days, {hoursup} hours, {minutesup} minutes, {secondsup} seconds");
		sb.Append(ending);
		sb.Append($"Players last online: {lastonlinesec} seconds ago");
		sb.Append(ending);
		sb.Append("Players online: " + clientCount + " / " + server.Config.MaxClients);
		if (clientCount > 0 && clientCount < 20)
		{
			sb.Append(" (");
			int i = 0;
			foreach (ConnectedClient client in server.Clients.Values)
			{
				if (client.State != EnumClientState.Connecting && client.State != EnumClientState.Queued)
				{
					if (i++ > 0)
					{
						sb.Append(", ");
					}
					sb.Append(client.PlayerName);
				}
			}
			sb.Append(")");
		}
		sb.Append(ending);
		if (server.Config.MaxClientsInQueue > 0)
		{
			sb.Append("Players in queue: " + server.ConnectionQueue.Count + " / " + server.Config.MaxClientsInQueue);
		}
		int activeCount = 0;
		foreach (Entity value in server.LoadedEntities.Values)
		{
			if (value.State != EnumEntityState.Inactive)
			{
				activeCount++;
			}
		}
		sb.Append(ending);
		string managed = decimal.Round((decimal)((float)GC.GetTotalMemory(forceFullCollection: false) / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);
		string total = decimal.Round((decimal)((float)Process.GetCurrentProcess().WorkingSet64 / 1024f / 1024f), 2).ToString("#.#", GlobalConstants.DefaultCultureInfo);
		sb.Append("Memory usage Managed/Total: " + managed + "Mb / " + total + " Mb");
		sb.Append(ending);
		StatsCollection prevColl = server.StatsCollector[GameMath.Mod(server.StatsCollectorIndex - 1, server.StatsCollector.Length)];
		double seconds = 2.0;
		if (prevColl.ticksTotal > 0)
		{
			sb.Append("Last 2s Average Tick Time: " + decimal.Round((decimal)prevColl.tickTimeTotal / (decimal)prevColl.ticksTotal, 2) + " ms");
			sb.Append(ending);
			sb.Append("Last 2s Ticks/s: " + decimal.Round((decimal)((double)prevColl.ticksTotal / seconds), 2));
			sb.Append(ending);
			sb.Append("Last 10 ticks (ms): " + string.Join(", ", prevColl.tickTimes));
		}
		sb.Append(ending);
		sb.Append("Loaded chunks: " + server.loadedChunks.Count);
		sb.Append(ending);
		sb.Append("Loaded entities: " + server.LoadedEntities.Count + " (" + activeCount + " active)");
		sb.Append(ending);
		sb.Append("Network TCP: " + decimal.Round((decimal)((double)prevColl.statTotalPackets / seconds), 2) + " Packets/s or " + decimal.Round((decimal)((double)prevColl.statTotalPacketsLength / seconds / 1024.0), 2, MidpointRounding.AwayFromZero) + " Kb/s");
		sb.Append(ending);
		sb.Append("Network UDP: " + decimal.Round((decimal)((double)prevColl.statTotalUdpPackets / seconds), 2) + " Packets/s or " + decimal.Round((decimal)((double)prevColl.statTotalUdpPacketsLength / seconds / 1024.0), 2, MidpointRounding.AwayFromZero) + " Kb/s");
		return sb.ToString();
	}
}
