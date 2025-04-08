using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

internal class CmdGlobalList
{
	private ServerMain server;

	public CmdGlobalList(ServerMain server)
	{
		this.server = server;
		server.api.commandapi.Create("list").RequiresPrivilege(Privilege.readlists).WithDesc("Show global lists (clients, banned, roles or privileges)")
			.BeginSub("clients")
			.WithAlias("c")
			.WithDesc("Players who are currently online")
			.HandleWith(listClients)
			.EndSub()
			.BeginSub("banned")
			.WithAlias("b")
			.WithDesc("Users who are banned from this server")
			.HandleWith(listBanned)
			.EndSub()
			.BeginSub("roles")
			.WithAlias("r")
			.WithDesc("Available roles")
			.HandleWith(listRoles)
			.EndSub()
			.BeginSub("privileges")
			.WithAlias("p")
			.WithDesc("Available privileges")
			.HandleWith(listPrivileges)
			.EndSub()
			.HandleWith(handleList);
	}

	private TextCommandResult listClients(TextCommandCallingArgs args)
	{
		StringBuilder text = new StringBuilder();
		text.AppendLine(Lang.Get("List of online Players"));
		foreach (KeyValuePair<int, ConnectedClient> i in server.Clients)
		{
			if (i.Value.State != EnumClientState.Connected && i.Value.State != EnumClientState.Playing && i.Value.State != EnumClientState.Queued)
			{
				continue;
			}
			if (i.Value.State == EnumClientState.Queued)
			{
				int q = server.ConnectionQueue.FindIndex((QueuedClient c) => c.Client.Id == i.Value.Id);
				if (q >= 0)
				{
					QueuedClient queueClient = server.ConnectionQueue[q];
					text.AppendLine($"[{i.Key}] {queueClient.Identification.Playername} {i.Value.Socket.RemoteEndPoint()} | Queue position: ({q + 1})");
				}
				else
				{
					ServerMain.Logger.Warning("Client {0} not found in connection queue", i.Value.Id);
				}
			}
			else
			{
				text.AppendLine($"[{i.Key}] {i.Value.PlayerName} {i.Value.Socket.RemoteEndPoint()}");
			}
		}
		return TextCommandResult.Success(text.ToString());
	}

	private TextCommandResult listBanned(TextCommandCallingArgs args)
	{
		StringBuilder text = new StringBuilder();
		text.AppendLine(Lang.Get("List of Banned Users:"));
		foreach (PlayerEntry entry in server.PlayerDataManager.BannedPlayers)
		{
			string reason = entry.Reason;
			if (string.IsNullOrEmpty(reason))
			{
				reason = "";
			}
			if (entry.UntilDate >= DateTime.Now)
			{
				text.AppendLine($"{entry.PlayerName} until {entry.UntilDate}. Reason: {reason}");
			}
		}
		return TextCommandResult.Success(text.ToString());
	}

	private TextCommandResult listRoles(TextCommandCallingArgs args)
	{
		StringBuilder text = new StringBuilder();
		text.AppendLine(Lang.Get("List of roles:"));
		foreach (PlayerRole group in server.Config.Roles)
		{
			text.AppendLine(group.ToString());
		}
		return TextCommandResult.Success(text.ToString());
	}

	private TextCommandResult listPrivileges(TextCommandCallingArgs args)
	{
		StringBuilder text = new StringBuilder();
		text.AppendLine(Lang.Get("Available privileges:"));
		foreach (string privilege in server.AllPrivileges)
		{
			text.AppendLine(privilege.ToString());
		}
		return TextCommandResult.Success(text.ToString());
	}

	private TextCommandResult handleList(TextCommandCallingArgs args)
	{
		return TextCommandResult.Error("Syntax error, requires argument clients|banned|roles|privileges or c|b|r|p");
	}
}
