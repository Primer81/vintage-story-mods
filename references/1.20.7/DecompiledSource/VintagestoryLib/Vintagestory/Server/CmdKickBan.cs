using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

internal class CmdKickBan
{
	private ServerMain server;

	public CmdKickBan(ServerMain server)
	{
		this.server = server;
		IChatCommandApi cmdapi = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		cmdapi.Create("kick").RequiresPrivilege(Privilege.kick).WithDescription("Kicks a player from the server")
			.WithArgs(parsers.PlayerUids("player name"), parsers.OptionalAll("kick reason"))
			.HandleWith((TextCommandCallingArgs args) => CmdPlayer.Each(args, (PlayerUidName plr, TextCommandCallingArgs args) => Kick(args.Caller, plr, (string)args[1])))
			.Validate();
		cmdapi.Create("ban").RequiresPrivilege(Privilege.ban).WithDescription("Ban a player from the server")
			.WithArgs(parsers.PlayerUids("player name"), parsers.DateTime("duration"), parsers.All("reason"))
			.HandleWith((TextCommandCallingArgs cargs) => CmdPlayer.Each(cargs, (PlayerUidName plr, TextCommandCallingArgs args) => Ban(args.Caller, plr, (DateTime)args[1], (string)args[2])))
			.Validate();
		cmdapi.Create("unban").RequiresPrivilege(Privilege.ban).WithDescription("Remove a player ban")
			.WithArgs(parsers.PlayerUids("player name"))
			.HandleWith((TextCommandCallingArgs cargs) => CmdPlayer.Each(cargs, (PlayerUidName plr, TextCommandCallingArgs args) => UnBan(args.Caller, plr)))
			.Validate();
		cmdapi.Create("hardban").RequiresPrivilege(Privilege.ban).WithDescription("Ban a player forever without reason")
			.WithArgs(parsers.PlayerUids("player name"))
			.HandleWith((TextCommandCallingArgs cargs) => CmdPlayer.Each(cargs, (PlayerUidName plr, TextCommandCallingArgs args) => Ban(args.Caller, plr, DateTime.Now.AddYears(1000), "hard ban")))
			.Validate();
	}

	private TextCommandResult UnBan(Caller caller, PlayerUidName plr)
	{
		if (server.PlayerDataManager.UnbanPlayer(plr.Name, plr.Uid, caller.GetName()))
		{
			return TextCommandResult.Success(Lang.Get("Player is now unbanned"));
		}
		return TextCommandResult.Error(Lang.Get("Player was not banned"));
	}

	private TextCommandResult Ban(Caller caller, PlayerUidName targetPlayer, DateTime untilDate, string reason)
	{
		TextCommandResult result = CanKickOrBanTarget(caller, targetPlayer.Name);
		if (result.Status == EnumCommandStatus.Error)
		{
			return result;
		}
		server.PlayerDataManager.BanPlayer(targetPlayer.Name, targetPlayer.Uid, caller.GetName(), reason, untilDate);
		ConnectedClient targetClient = server.GetClientByUID(targetPlayer.Uid);
		if (targetClient != null)
		{
			server.DisconnectPlayer(targetClient, Lang.Get("cmdban-playerwasbanned", targetPlayer.Name, caller.GetName(), (reason.Length > 0) ? (", reason: " + reason) : ""), Lang.Get("cmdban-youvebeenbanned", caller.GetName(), (reason.Length > 0) ? (", reason: " + reason) : ""));
		}
		return TextCommandResult.Success(Lang.Get("cmdban-playerisnowbanned", untilDate));
	}

	private TextCommandResult Kick(Caller caller, PlayerUidName puidn, string reason = "")
	{
		IPlayer targetPlayer = server.AllOnlinePlayers.FirstOrDefault((IPlayer plr) => plr.PlayerUID == puidn.Uid);
		if (targetPlayer == null)
		{
			return TextCommandResult.Error("No such user online");
		}
		if (!server.Clients.TryGetValue(targetPlayer.ClientId, out var targetClient))
		{
			return TextCommandResult.Error(Lang.Get("No player with connectionid '{0}' exists", targetPlayer.ClientId));
		}
		TextCommandResult result = CanKickOrBanTarget(caller, targetPlayer.PlayerName);
		if (result.Status == EnumCommandStatus.Error)
		{
			return result;
		}
		string targetName = targetClient.PlayerName;
		string sourceName = caller.GetName();
		if (reason == null)
		{
			reason = "";
		}
		string hisMsg = ((reason.Length == 0) ? Lang.Get("You've been kicked by {0}", sourceName) : Lang.Get("You've been kicked by {0}, reason: {1}", sourceName, reason));
		string othersMsg = ((reason.Length == 0) ? Lang.Get("{0} has been kicked by {1}", targetName, sourceName) : Lang.Get("{0} has been kicked by {1}, reason: {2}", targetName, sourceName, reason));
		server.DisconnectPlayer(targetClient, othersMsg, hisMsg);
		ServerMain.Logger.Audit(string.Format("{0} kicks {1}. Reason: {2}", sourceName, targetName, (reason.Length == 0) ? "none given" : reason));
		return TextCommandResult.Success(othersMsg);
	}

	protected TextCommandResult CanKickOrBanTarget(Caller caller, string targetPlayerName)
	{
		ServerPlayerData plrdata = server.PlayerDataManager.GetServerPlayerDataByLastKnownPlayername(targetPlayerName);
		if (plrdata == null)
		{
			return TextCommandResult.Success();
		}
		PlayerRole targetRole = plrdata.GetPlayerRole(server);
		if (targetRole == null)
		{
			return TextCommandResult.Success();
		}
		IPlayerRole callerRole = caller.GetRole(server.api);
		if (targetRole.IsSuperior(callerRole) || (targetRole.EqualLevel(callerRole) && !caller.HasPrivilege(Privilege.root)))
		{
			return TextCommandResult.Error(Lang.Get("Can't kick or ban a player with a superior or equal group level"));
		}
		return TextCommandResult.Success();
	}
}
