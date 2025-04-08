using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

public class ServerySystemPlayerGroups : ServerSystem
{
	public Dictionary<int, string> DisbandRequests = new Dictionary<int, string>();

	public Dictionary<string, string> InviteRequests = new Dictionary<string, string>();

	public Dictionary<int, PlayerGroup> PlayerGroupsByUid => server.PlayerDataManager.PlayerGroupsById;

	public ServerySystemPlayerGroups(ServerMain server)
		: base(server)
	{
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.ChatCommands.Create("group").WithDescription("Manage a player group").RequiresPrivilege(Privilege.controlplayergroups)
			.BeginSubCommand("create")
			.WithDescription("Creates a new group.")
			.WithExamples("Syntax: /group create [groupname]")
			.RequiresPlayer()
			.WithArgs(parsers.Word("groupName"))
			.HandleWith(CmdCreategroup)
			.EndSubCommand()
			.BeginSubCommand("disband")
			.WithDescription("Disband a group. Only the owner has the privilege to disband.")
			.WithExamples("Syntax: /group disband [groupname]")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalWord("groupName"))
			.HandleWith(CmdDisbandgroup)
			.EndSubCommand()
			.BeginSubCommand("confirmdisband")
			.WithDescription("Confirm disband a group. Only the owner has the privilege to disband.")
			.WithExamples("Syntax: /group confirmdisband [groupname]")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalWord("groupName"))
			.HandleWith(CmdConfirmDisbandgroup)
			.EndSubCommand()
			.BeginSubCommand("joinpolicy")
			.WithDescription("Define how users can join your group")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalWordRange("policy", "inviteonly", "everyone"))
			.HandleWith(CmdJoinPolicy)
			.EndSubCommand()
			.BeginSubCommand("join")
			.WithDescription("Join a group thats open for everyone")
			.RequiresPlayer()
			.WithArgs(parsers.Word("group name"))
			.HandleWith(CmdJoin)
			.EndSubCommand()
			.BeginSubCommand("rename")
			.WithDescription("Rename a group.")
			.WithExamples("Syntax: /group rename [oldname] [newname]", " Syntax in group chat: /group rename [newname]")
			.RequiresPlayer()
			.WithArgs(parsers.Word("groupName"), parsers.OptionalWord("newName"))
			.HandleWith(CmdRenamegroup)
			.EndSubCommand()
			.BeginSubCommand("invite")
			.WithDescription("Invite a player.")
			.WithExamples("Syntax: /group invite [groupname] [playername]")
			.RequiresPlayer()
			.WithArgs(parsers.Word("groupName"), parsers.OptionalWord("playerName"))
			.HandleWith(CmdInvitePlayer)
			.EndSubCommand()
			.BeginSubCommand("acceptinvite")
			.WithAlias("ai")
			.WithDescription("Accept an invitation to a group.")
			.WithExamples("Syntax: /group acceptinvite [groupname/groupid]")
			.WithArgs(parsers.Word("groupName/groupId"))
			.RequiresPlayer()
			.HandleWith(CmdAcceptInvite)
			.EndSubCommand()
			.BeginSubCommand("leave")
			.WithDescription("Leave a group.")
			.WithExamples("Syntax: /group leave [groupname]", "/group leave while in the groups chat room")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalWord("groupName"))
			.HandleWith(CmdLeavegroup)
			.EndSubCommand()
			.BeginSubCommand("list")
			.WithDescription("Lists the group you are in")
			.RequiresPlayer()
			.HandleWith(CmdListgroups)
			.EndSubCommand()
			.BeginSubCommand("info")
			.WithDescription("Show some info on a group.")
			.WithExamples("Syntax: /group info [groupname]")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalWord("groupName"))
			.HandleWith(CmdgroupInfo)
			.EndSubCommand()
			.BeginSubCommand("kick")
			.WithDescription("Kick a player from a group.")
			.WithExamples("Syntax: /group kick [groupname] (playername)", "/group kick (playername) while in the groups chat room")
			.RequiresPlayer()
			.WithArgs(parsers.Word("groupName"), parsers.OptionalWord("playerName"))
			.HandleWith(CmdKickFromgroup)
			.EndSubCommand()
			.BeginSubCommand("op")
			.WithDescription("Grant operator status to a player. Gives that player the ability to kick and invite players.")
			.WithExamples("Syntax: /group op [groupname] (playername)", "/group op (playername) while in the groups chat room")
			.RequiresPlayer()
			.WithArgs(parsers.Word("groupName"), parsers.OptionalWord("playerName"))
			.HandleWith((TextCommandCallingArgs args) => CmdOpPlayer(args, deop: false))
			.EndSubCommand()
			.BeginSubCommand("deop")
			.WithDescription("Revoke operator status from a player.")
			.WithExamples("Syntax: /group deop [groupname] (playername)", "/group deop (playername) while in the groups chat room")
			.RequiresPlayer()
			.WithArgs(parsers.Word("groupName"), parsers.OptionalWord("playerName"))
			.HandleWith((TextCommandCallingArgs args) => CmdOpPlayer(args, deop: true))
			.EndSubCommand();
		server.api.ChatCommands.Create("groupinvite").WithDescription("Enables or disables group invites to be sent to you").RequiresPrivilege(Privilege.chat)
			.RequiresPlayer()
			.WithArgs(parsers.Bool("enable"))
			.HandleWith(CmdNoInvite);
	}

	public override void OnPlayerJoinPost(ServerPlayer player)
	{
		List<int> removedPlayergroups = new List<int>();
		foreach (KeyValuePair<int, PlayerGroupMembership> val in player.serverdata.PlayerGroupMemberShips)
		{
			if (val.Value.Level == EnumPlayerGroupMemberShip.None)
			{
				continue;
			}
			PlayerGroup plrGroup = null;
			server.PlayerDataManager.PlayerGroupsById.TryGetValue(val.Key, out plrGroup);
			if (plrGroup == null)
			{
				removedPlayergroups.Add(val.Key);
				server.SendMessage(player, GlobalConstants.ServerInfoChatGroup, "The player group " + val.Value.GroupName + " you were a member of no longer exists. It probably has been disbanded", EnumChatType.Notification);
				continue;
			}
			server.PlayerDataManager.PlayerGroupsById[val.Key].OnlinePlayers.Add(player);
			if (plrGroup.Name != val.Value.GroupName)
			{
				server.SendMessage(player, GlobalConstants.ServerInfoChatGroup, "The player group " + val.Value.GroupName + " you were a member of has been renamed to " + plrGroup.Name, EnumChatType.Notification);
				val.Value.GroupName = plrGroup.Name;
			}
		}
		foreach (int groupid in removedPlayergroups)
		{
			player.serverdata.PlayerGroupMemberShips.Remove(groupid);
			server.PlayerDataManager.playerDataDirty = true;
		}
		SendPlayerGroups(player);
	}

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		foreach (KeyValuePair<int, PlayerGroupMembership> val in player.serverdata.PlayerGroupMemberShips)
		{
			if (val.Value.Level != 0 && server.PlayerDataManager.PlayerGroupsById.TryGetValue(val.Key, out var group))
			{
				group.OnlinePlayers.Remove(player);
			}
		}
	}

	private TextCommandResult Success(TextCommandCallingArgs args, string message, params string[] msgargs)
	{
		return TextCommandResult.Success(Lang.GetL(args.LanguageCode, message, msgargs));
	}

	private TextCommandResult Error(TextCommandCallingArgs args, string message, params string[] msgargs)
	{
		return TextCommandResult.Error(Lang.GetL(args.LanguageCode, message, msgargs));
	}

	private TextCommandResult CmdCreategroup(TextCommandCallingArgs args)
	{
		string playerUid = args.Caller.Player.PlayerUID;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		string groupName = args[0] as string;
		if (!server.PlayerDataManager.CanCreatePlayerGroup(playerUid))
		{
			return Error(args, "No privilege to create groups.");
		}
		if (server.PlayerDataManager.GetPlayerGroupByName(groupName) != null)
		{
			return Error(args, "This group name already exists, please choose another name");
		}
		if (Regex.IsMatch(groupName, "[^" + GlobalConstants.AllowedChatGroupChars + "]+"))
		{
			return Error(args, "Invalid group name, may only use letters and numbers");
		}
		PlayerGroup group = new PlayerGroup
		{
			Name = groupName,
			OwnerUID = playerUid
		};
		server.PlayerDataManager.AddPlayerGroup(group);
		group.Md5Identifier = GameMath.Md5Hash(group.Uid + playerUid);
		server.PlayerDataManager.PlayerDataByUid[playerUid].JoinGroup(group, EnumPlayerGroupMemberShip.Owner);
		group.OnlinePlayers.Add(player);
		SendPlayerGroup(player, group);
		GotoGroup(player, group.Uid);
		server.PlayerDataManager.playerDataDirty = true;
		server.PlayerDataManager.playerGroupsDirty = true;
		player.SendMessage(group.Uid, Lang.GetL(player.LanguageCode, "Group {0} created by {1}", args[0], player.PlayerName), EnumChatType.CommandSuccess);
		return Success(args, "Group {0} created.", args[0] as string);
	}

	private int GetgroupId(string groupName)
	{
		foreach (PlayerGroup group in server.PlayerDataManager.PlayerGroupsById.Values)
		{
			if (group.Name.Equals(groupName, StringComparison.CurrentCultureIgnoreCase))
			{
				return group.Uid;
			}
		}
		return 0;
	}

	private TextCommandResult CmdDisbandgroup(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		string playerUid = player.PlayerUID;
		int targetgroupid = args.Caller.FromChatGroupId;
		if (!args.Parsers[0].IsMissing)
		{
			targetgroupid = GetgroupId(args[0] as string);
		}
		if (targetgroupid <= 0)
		{
			return Error(args, "Invalid group name");
		}
		if (!HasPlayerPrivilege(player, targetgroupid, EnumPlayerGroupPrivilege.Disband))
		{
			return Error(args, "You must be the owner of the group to disband it.");
		}
		if (DisbandRequests.ContainsKey(targetgroupid))
		{
			return Error(args, "Disband already requested, type /group confirmdisband [groupname] to confirm.");
		}
		DisbandRequests.Add(targetgroupid, playerUid);
		return Success(args, "Really disband group {0}? Type /group confirmdisband [groupname] to confirm.", PlayerGroupsByUid[targetgroupid].Name);
	}

	private TextCommandResult CmdConfirmDisbandgroup(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		string playerUid = player.PlayerUID;
		int targetgroupid = args.Caller.FromChatGroupId;
		if (!args.Parsers[0].IsMissing)
		{
			targetgroupid = GetgroupId(args[0] as string);
		}
		if (targetgroupid <= 0)
		{
			return Error(args, "Invalid group name");
		}
		if (!HasPlayerPrivilege(player, targetgroupid, EnumPlayerGroupPrivilege.Disband))
		{
			return Error(args, "You must be the owner of the group to disband it.");
		}
		if (DisbandRequests.ContainsKey(targetgroupid) && DisbandRequests[targetgroupid] == playerUid)
		{
			PlayerGroup plrgroup = PlayerGroupsByUid[targetgroupid];
			server.PlayerDataManager.RemovePlayerGroup(PlayerGroupsByUid[targetgroupid]);
			server.PlayerDataManager.playerGroupsDirty = true;
			server.PlayerDataManager.playerDataDirty = true;
			foreach (IServerPlayer memberPlayer in plrgroup.OnlinePlayers)
			{
				((ServerPlayer)memberPlayer).serverdata.LeaveGroup(plrgroup);
				SendPlayerGroups(memberPlayer);
				string msg = Lang.GetL(memberPlayer.LanguageCode, "Player group {0} has been disbanded by {1}", plrgroup.Name, player.PlayerName);
				memberPlayer.SendMessage((memberPlayer.ClientId == player.ClientId && args.Caller.FromChatGroupId != targetgroupid) ? args.Caller.FromChatGroupId : GlobalConstants.ServerInfoChatGroup, msg, EnumChatType.Notification);
			}
			return TextCommandResult.Success();
		}
		return Error(args, "Found no disband request to confirm, please use /group disband [groupname] first.");
	}

	private TextCommandResult CmdRenamegroup(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		int targetGroupid = args.Caller.FromChatGroupId;
		string newName;
		if (!args.Parsers[1].IsMissing)
		{
			targetGroupid = GetgroupId(args[0] as string);
			newName = args[1] as string;
		}
		else
		{
			newName = args[0] as string;
		}
		if (targetGroupid <= 0)
		{
			return Error(args, "Invalid group name");
		}
		if (!HasPlayerPrivilege(player, targetGroupid, EnumPlayerGroupPrivilege.Rename))
		{
			return Error(args, "You must be the owner of the group to rename it.");
		}
		if (server.PlayerDataManager.GetPlayerGroupByName(newName) != null)
		{
			return Error(args, "This group name already exists, please choose another name");
		}
		if (Regex.IsMatch(newName, "[^" + GlobalConstants.AllowedChatGroupChars + "]+"))
		{
			return Error(args, "Invalid group name, may only use letters and numbers");
		}
		PlayerGroup plrgroup = PlayerGroupsByUid[targetGroupid];
		string oldname = plrgroup.Name;
		plrgroup.Name = newName;
		server.PlayerDataManager.playerGroupsDirty = true;
		foreach (IServerPlayer memberPlayer in plrgroup.OnlinePlayers)
		{
			SendPlayerGroup(memberPlayer, plrgroup);
			server.Clients[player.ClientId].ServerData.PlayerGroupMemberShips[plrgroup.Uid].GroupName = plrgroup.Name;
			memberPlayer.SendMessage(targetGroupid, Lang.GetL(memberPlayer.LanguageCode, "Player group has been renamed from {0} to {1}", oldname, plrgroup.Name), EnumChatType.Notification);
		}
		return Success(args, "Player group renamed");
	}

	private TextCommandResult CmdJoin(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		string groupname = args[0] as string;
		PlayerGroup targetGroup = PlayerGroupsByUid.FirstOrDefault((KeyValuePair<int, PlayerGroup> g) => g.Value.JoinPolicy == "everyone" && g.Value.Name == groupname).Value;
		if (targetGroup == null)
		{
			PlayerGroupsByUid.TryGetValue(groupname.ToInt(), out targetGroup);
		}
		if (targetGroup == null || targetGroup.JoinPolicy != "everyone")
		{
			return Error(args, "No such group found or the invite policy is invite only");
		}
		int targetGroupid = targetGroup.Uid;
		PlayerGroupMembership membership = ((ServerPlayer)player).serverdata.JoinGroup(targetGroup, EnumPlayerGroupMemberShip.Member);
		server.PlayerDataManager.playerDataDirty = true;
		PlayerGroupsByUid[targetGroupid].OnlinePlayers.Add(player);
		SendPlayerGroup(player, PlayerGroupsByUid[targetGroupid], membership);
		GotoGroup(player, targetGroupid);
		foreach (IServerPlayer onlinePlayer in PlayerGroupsByUid[targetGroupid].OnlinePlayers)
		{
			onlinePlayer.SendMessage(targetGroupid, Lang.Get("Player {0} has joined the group.", player.PlayerName), EnumChatType.Notification);
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdJoinPolicy(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		int targetGroupid = args.Caller.FromChatGroupId;
		if (targetGroupid <= 0)
		{
			return Error(args, "Must write the command inside the chat group you wish to modify");
		}
		if (args.Parsers[0].IsMissing)
		{
			return Success(args, "Join policy of this group is: {0}.", Lang.Get("plrgroup-invitepolicy-" + (PlayerGroupsByUid[targetGroupid].JoinPolicy ?? "inviteonly")));
		}
		string policy = args[0] as string;
		if (!HasPlayerPrivilege(player, targetGroupid, EnumPlayerGroupPrivilege.Rename))
		{
			return Error(args, "You must be the owner of the group to rename it.");
		}
		PlayerGroupsByUid[targetGroupid].JoinPolicy = policy;
		return Success(args, "Join policy {0} set.", policy);
	}

	private TextCommandResult CmdInvitePlayer(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		int targetGroupid = args.Caller.FromChatGroupId;
		string playerName;
		if (!args.Parsers[1].IsMissing)
		{
			targetGroupid = GetgroupId(args[0] as string);
			playerName = args[1] as string;
		}
		else
		{
			playerName = args[0] as string;
		}
		if (targetGroupid <= 0)
		{
			return Error(args, "Invalid group name");
		}
		if (!HasPlayerPrivilege(player, targetGroupid, EnumPlayerGroupPrivilege.Invite))
		{
			return Error(args, "You must be the op or owner of the group to invite players.");
		}
		ConnectedClient invitedClient = server.GetClientByPlayername(playerName);
		if (invitedClient == null || invitedClient.Player == null)
		{
			return Error(args, "Can't invite. Player name {0} does not exist or is not online", playerName);
		}
		if (!server.PlayerDataManager.PlayerDataByUid[invitedClient.ServerData.PlayerUID].AllowInvite)
		{
			return Error(args, "Can't invite. Player name {0} has disabled group invites", playerName);
		}
		if (invitedClient.ServerData.PlayerGroupMemberShips.ContainsKey(targetGroupid))
		{
			return Error(args, "Can't invite. Player name {0} already in this player group!", playerName);
		}
		InviteRequests[targetGroupid + "-" + invitedClient.ServerData.PlayerUID] = invitedClient.ServerData.PlayerUID;
		string cmd = "/group ai " + PlayerGroupsByUid[targetGroupid].Uid;
		string msg = Lang.GetL(invitedClient.Player.LanguageCode, "playergroup-invitemsg", player.PlayerName, PlayerGroupsByUid[targetGroupid].Name, cmd, cmd);
		invitedClient.Player.SendMessage(GlobalConstants.GeneralChatGroup, msg, EnumChatType.GroupInvite);
		return Success(args, "Player name {0} invited.", playerName);
	}

	private TextCommandResult CmdAcceptInvite(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		string playerUid = player.PlayerUID;
		string groupName = args[0] as string;
		if (!int.TryParse(groupName, out var targetGroupid))
		{
			PlayerGroup existingGroup = server.PlayerDataManager.GetPlayerGroupByName(groupName);
			if (existingGroup == null)
			{
				return Error(args, "Invalid param (not a number and no such group name exists), use /group help ai to see available params.");
			}
			targetGroupid = existingGroup.Uid;
		}
		if (InviteRequests.ContainsKey(targetGroupid + "-" + playerUid))
		{
			server.PlayerDataManager.PlayerGroupsById.TryGetValue(targetGroupid, out var targetGroup);
			if (targetGroup == null)
			{
				return Error(args, "Player group no longer exists.");
			}
			ServerPlayerData plrData = ((ServerPlayer)player).serverdata;
			if (plrData.PlayerGroupMemberShips.ContainsKey(targetGroupid))
			{
				player.SendMessage(args.Caller.FromChatGroupId, Lang.GetL(player.LanguageCode, "Can't accept invite, you are already joined this player group"), EnumChatType.CommandError);
			}
			else
			{
				PlayerGroupMembership membership = plrData.JoinGroup(targetGroup, EnumPlayerGroupMemberShip.Member);
				server.PlayerDataManager.playerDataDirty = true;
				PlayerGroupsByUid[targetGroupid].OnlinePlayers.Add(player);
				SendPlayerGroup(player, PlayerGroupsByUid[targetGroupid], membership);
				GotoGroup(player, targetGroupid);
				foreach (IServerPlayer onlinePlayer in PlayerGroupsByUid[targetGroupid].OnlinePlayers)
				{
					onlinePlayer.SendMessage(targetGroupid, Lang.GetL(player.LanguageCode, "Player {0} has joined the group.", player.PlayerName), EnumChatType.Notification);
				}
			}
		}
		else
		{
			player.SendMessage(args.Caller.FromChatGroupId, Lang.GetL(player.LanguageCode, "No invite for this player group found."), EnumChatType.CommandError);
		}
		return TextCommandResult.Success();
	}

	private void GotoGroup(IServerPlayer player, int groupId)
	{
		server.SendPacket(player, new Packet_Server
		{
			Id = 57,
			GotoGroup = new Packet_GotoGroup
			{
				GroupId = groupId
			}
		});
	}

	private TextCommandResult CmdLeavegroup(TextCommandCallingArgs args)
	{
		int targetgroupid = args.Caller.FromChatGroupId;
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		string groupName = args[0] as string;
		if (!args.Parsers[0].IsMissing)
		{
			targetgroupid = GetgroupId(groupName);
		}
		if (targetgroupid <= 0)
		{
			return Error(args, "Invalid group name");
		}
		ServerPlayerData plrData = ((ServerPlayer)player).serverdata;
		if (!plrData.PlayerGroupMemberShips.ContainsKey(targetgroupid))
		{
			return Error(args, "No such group membership found, perhaps you already left this group.");
		}
		if (PlayerGroupsByUid.ContainsKey(targetgroupid))
		{
			PlayerGroup targetGroup = PlayerGroupsByUid[targetgroupid];
			player.SendMessage((args.Caller.FromChatGroupId == targetgroupid) ? GlobalConstants.ServerInfoChatGroup : args.Caller.FromChatGroupId, Lang.GetL(player.LanguageCode, "You have left the group {0}", targetGroup.Name), EnumChatType.CommandSuccess);
			plrData.LeaveGroup(targetGroup);
			server.PlayerDataManager.playerDataDirty = true;
			SendPlayerGroups(player);
			targetGroup.OnlinePlayers.Remove(player);
			server.SendMessageToGroup(args.Caller.FromChatGroupId, Lang.Get("Player {0} has left the group.", player.PlayerName), EnumChatType.Notification);
		}
		else
		{
			plrData.LeaveGroup(targetgroupid);
			player.SendMessage((args.Caller.FromChatGroupId == targetgroupid) ? GlobalConstants.ServerInfoChatGroup : args.Caller.FromChatGroupId, Lang.GetL(player.LanguageCode, "You have left the group."), EnumChatType.CommandSuccess);
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdListgroups(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		Dictionary<int, PlayerGroupMembership> playerGroupMemberShips = ((ServerPlayer)player).serverdata.PlayerGroupMemberShips;
		player.SendMessage(args.Caller.FromChatGroupId, Lang.GetL(player.LanguageCode, "You are in the following groups: "), EnumChatType.Notification);
		foreach (KeyValuePair<int, PlayerGroupMembership> val in playerGroupMemberShips)
		{
			string name = Lang.GetL(player.LanguageCode, "Disbanded group name {0}", val.Value.GroupName);
			if (PlayerGroupsByUid.ContainsKey(val.Key))
			{
				name = PlayerGroupsByUid[val.Key].Name;
			}
			player.SendMessage(args.Caller.FromChatGroupId, name, EnumChatType.Notification);
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdgroupInfo(TextCommandCallingArgs args)
	{
		PlayerGroup group;
		if (!args.Parsers[0].IsMissing)
		{
			group = server.PlayerDataManager.GetPlayerGroupByName(args[0] as string);
			if (group == null)
			{
				return Error(args, "No such group exists.");
			}
		}
		else
		{
			if (GlobalConstants.DefaultChatGroups.Contains(args.Caller.FromChatGroupId))
			{
				return Error(args, "This is a default group.");
			}
			if (!server.PlayerDataManager.PlayerGroupsById.TryGetValue(args.Caller.FromChatGroupId, out group))
			{
				return Error(args, "No such group exists.");
			}
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine(Lang.GetL(args.LanguageCode, "Created: {0}", group.CreatedDate));
		sb.AppendLine(Lang.GetL(args.LanguageCode, "Created by: {0}", server.PlayerDataManager.PlayerDataByUid[group.OwnerUID].LastKnownPlayername));
		sb.Append(Lang.GetL(args.LanguageCode, "Members: "));
		int i = 0;
		foreach (ServerPlayerData plrdata in server.PlayerDataManager.PlayerDataByUid.Values)
		{
			if (plrdata.PlayerGroupMemberships.ContainsKey(group.Uid))
			{
				if (i > 0)
				{
					sb.Append(", ");
				}
				i++;
				sb.Append(plrdata.LastKnownPlayername);
			}
		}
		sb.AppendLine();
		return TextCommandResult.Success(sb.ToString());
	}

	private TextCommandResult CmdKickFromgroup(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		int targetgroupid = args.Caller.FromChatGroupId;
		string playerName;
		if (!args.Parsers[1].IsMissing)
		{
			targetgroupid = GetgroupId(args[0] as string);
			playerName = args[1] as string;
		}
		else
		{
			playerName = args[0] as string;
		}
		if (targetgroupid <= 0)
		{
			return Error(args, "Invalid group name");
		}
		PlayerGroup plrgroup = PlayerGroupsByUid[targetgroupid];
		foreach (ServerPlayerData plrdata in server.PlayerDataManager.PlayerDataByUid.Values)
		{
			if (!string.Equals(playerName, plrdata.LastKnownPlayername, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			foreach (KeyValuePair<int, PlayerGroupMembership> playerGroupMemberShip in plrdata.PlayerGroupMemberShips)
			{
				if (playerGroupMemberShip.Key != targetgroupid)
				{
					continue;
				}
				if (!HasPlayerPrivilege(player, targetgroupid, EnumPlayerGroupPrivilege.Kick, plrdata.PlayerUID))
				{
					return Error(args, "You must be the op or owner to kick this player (and ops can only be kicked by owner).");
				}
				plrdata.LeaveGroup(plrgroup);
				server.PlayerDataManager.playerDataDirty = true;
				if (server.PlayersByUid.TryGetValue(plrdata.PlayerUID, out var targetPlr))
				{
					PlayerGroupsByUid[targetgroupid].OnlinePlayers.Remove(targetPlr);
				}
				server.SendMessageToGroup(args.Caller.FromChatGroupId, Lang.GetL(args.LanguageCode, "Player {0} has been removed from the player group.", plrdata.LastKnownPlayername), EnumChatType.CommandSuccess);
				foreach (ConnectedClient client in server.Clients.Values)
				{
					if (client.WorldData.PlayerUID == plrdata.PlayerUID && client.Player != null)
					{
						SendPlayerGroups(client.Player);
						client.Player.SendMessage(GlobalConstants.InfoLogChatGroup, Lang.GetL(client.Player.LanguageCode, "You've been kicked from player group {0}.", plrgroup.Name), EnumChatType.Notification);
						break;
					}
				}
				return TextCommandResult.Success();
			}
			return Error(args, "This player is not in this group.");
		}
		return Success(args, "No such player name found");
	}

	private TextCommandResult CmdOpPlayer(TextCommandCallingArgs args, bool deop)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		int targetgroupid = args.Caller.FromChatGroupId;
		string playerName;
		if (!args.Parsers[1].IsMissing)
		{
			targetgroupid = GetgroupId(args[0] as string);
			playerName = args[1] as string;
		}
		else
		{
			playerName = args[0] as string;
		}
		if (targetgroupid <= 0)
		{
			return Error(args, "Invalid group name");
		}
		if (!HasPlayerPrivilege(player, targetgroupid, EnumPlayerGroupPrivilege.Op))
		{
			return Error(args, "You must be the owner to op/deop players");
		}
		PlayerGroup plrgroup = PlayerGroupsByUid[targetgroupid];
		foreach (ServerPlayerData plrdata in server.PlayerDataManager.PlayerDataByUid.Values)
		{
			if (!string.Equals(playerName, plrdata.LastKnownPlayername, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			EnumPlayerGroupMemberShip membership = GetGroupMemberShip(plrdata.PlayerUID, targetgroupid).Level;
			if (membership == EnumPlayerGroupMemberShip.None)
			{
				return Error(args, "This player is not in this group, invite him first.");
			}
			if (!deop && (membership == EnumPlayerGroupMemberShip.Op || membership == EnumPlayerGroupMemberShip.Owner))
			{
				return Error(args, "This player is already op in this channel.");
			}
			if (deop && membership != EnumPlayerGroupMemberShip.Op)
			{
				return Error(args, "This player is no op in this channel.");
			}
			plrdata.PlayerGroupMemberShips[targetgroupid].Level = (deop ? EnumPlayerGroupMemberShip.Member : EnumPlayerGroupMemberShip.Op);
			server.PlayerDataManager.playerDataDirty = true;
			foreach (ServerPlayer memberPlayer in plrgroup.OnlinePlayers)
			{
				if (memberPlayer.WorldData.PlayerUID == plrdata.PlayerUID)
				{
					string msg = Lang.GetL(memberPlayer.LanguageCode, "{0} has given you op status. You can now invite and kick group members.", player.PlayerName);
					if (deop)
					{
						msg = Lang.GetL(memberPlayer.LanguageCode, "{0} has removed your op status. You can no longer invite or kick members", player.PlayerName);
					}
					memberPlayer.SendMessage(targetgroupid, msg, EnumChatType.Notification);
				}
				else
				{
					string msg2 = Lang.GetL(memberPlayer.LanguageCode, deop ? "Player {0} has been deopped." : "Player {0} has been opped.", plrdata.LastKnownPlayername);
					memberPlayer.SendMessage((memberPlayer.ClientId == player.ClientId && args.Caller.FromChatGroupId != targetgroupid) ? args.Caller.FromChatGroupId : targetgroupid, msg2, EnumChatType.CommandSuccess);
				}
			}
			break;
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult CmdNoInvite(TextCommandCallingArgs args)
	{
		string playerUid = args.Caller.Player.PlayerUID;
		server.PlayerDataManager.PlayerDataByUid[playerUid].AllowInvite = (bool)args[0];
		return Success(args, server.PlayerDataManager.PlayerDataByUid[playerUid].AllowInvite ? "Ok, Group invites are now enabled" : "Ok, Group invites are now disabled");
	}

	public void SendPlayerGroups(IServerPlayer player)
	{
		List<Packet_PlayerGroup> packets = new List<Packet_PlayerGroup>();
		foreach (KeyValuePair<int, PlayerGroupMembership> val in ((ServerPlayer)player).serverdata.PlayerGroupMemberShips)
		{
			if (val.Value.Level != 0)
			{
				PlayerGroup plrgroup = null;
				server.PlayerDataManager.PlayerGroupsById.TryGetValue(val.Key, out plrgroup);
				if (plrgroup != null)
				{
					packets.Add(GetPlayerGroupPacket(plrgroup, val.Value));
				}
			}
		}
		Packet_PlayerGroups packet = new Packet_PlayerGroups();
		packet.SetGroups(packets.ToArray());
		server.SendPacket(player, new Packet_Server
		{
			Id = 49,
			PlayerGroups = packet
		});
	}

	public void SendPlayerGroup(IServerPlayer player, PlayerGroup playergroup)
	{
		((ServerPlayer)player).serverdata.PlayerGroupMemberShips.TryGetValue(playergroup.Uid, out var membership);
		if (membership != null && membership.Level != 0)
		{
			server.SendPacket(player, new Packet_Server
			{
				Id = 50,
				PlayerGroup = GetPlayerGroupPacket(playergroup, membership)
			});
		}
	}

	public void SendPlayerGroup(IServerPlayer player, PlayerGroup playergroup, PlayerGroupMembership membership)
	{
		if (membership.Level != 0)
		{
			server.SendPacket(player, new Packet_Server
			{
				Id = 50,
				PlayerGroup = GetPlayerGroupPacket(playergroup, membership)
			});
		}
	}

	private Packet_PlayerGroup GetPlayerGroupPacket(PlayerGroup plrgroup, PlayerGroupMembership membership)
	{
		Packet_PlayerGroup pg = new Packet_PlayerGroup
		{
			Membership = (int)membership.Level,
			Name = plrgroup.Name,
			Owneruid = plrgroup.OwnerUID,
			Uid = plrgroup.Uid
		};
		List<Packet_ChatLine> chatlines = new List<Packet_ChatLine>();
		foreach (ChatLine chatline in plrgroup.ChatHistory)
		{
			chatlines.Add(new Packet_ChatLine
			{
				ChatType = (int)chatline.ChatType,
				Groupid = plrgroup.Uid,
				Message = chatline.Message
			});
		}
		pg.SetChathistory(chatlines.ToArray());
		return pg;
	}

	public bool HasPlayerPrivilege(IServerPlayer player, int targetGroupid, EnumPlayerGroupPrivilege priv, string targetPlayerUid = null)
	{
		EnumPlayerGroupMemberShip level = GetGroupMemberShip(player, targetGroupid).Level;
		switch (priv)
		{
		case EnumPlayerGroupPrivilege.Invite:
			if (level != EnumPlayerGroupMemberShip.Op)
			{
				return level == EnumPlayerGroupMemberShip.Owner;
			}
			return true;
		case EnumPlayerGroupPrivilege.Kick:
			if (level != EnumPlayerGroupMemberShip.Op || GetGroupMemberShip(targetPlayerUid, targetGroupid).Level != EnumPlayerGroupMemberShip.Member)
			{
				return level == EnumPlayerGroupMemberShip.Owner;
			}
			return true;
		case EnumPlayerGroupPrivilege.Disband:
			return level == EnumPlayerGroupMemberShip.Owner;
		case EnumPlayerGroupPrivilege.Op:
			return level == EnumPlayerGroupMemberShip.Owner;
		case EnumPlayerGroupPrivilege.Rename:
			return level == EnumPlayerGroupMemberShip.Owner;
		default:
			return false;
		}
	}

	public PlayerGroupMembership GetGroupMemberShip(IServerPlayer player, int targetGroupid)
	{
		return GetGroupMemberShip(player.PlayerUID, targetGroupid);
	}

	public PlayerGroupMembership GetGroupMemberShip(string playerUID, int targetGroupid)
	{
		ServerPlayerData plrData = server.PlayerDataManager.PlayerDataByUid[playerUID];
		if (!plrData.PlayerGroupMemberShips.ContainsKey(targetGroupid))
		{
			return new PlayerGroupMembership
			{
				Level = EnumPlayerGroupMemberShip.None
			};
		}
		return plrData.PlayerGroupMemberShips[targetGroupid];
	}
}
