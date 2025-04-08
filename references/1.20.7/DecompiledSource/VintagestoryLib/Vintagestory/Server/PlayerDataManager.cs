using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class PlayerDataManager : IPermissionManager, IGroupManager, IPlayerDataManager
{
	private ServerMain server;

	public Dictionary<string, ServerWorldPlayerData> WorldDataByUID = new Dictionary<string, ServerWorldPlayerData>();

	public Dictionary<int, PlayerGroup> PlayerGroupsById;

	public Dictionary<string, ServerPlayerData> PlayerDataByUid;

	public List<PlayerEntry> BannedPlayers;

	public List<PlayerEntry> WhitelistedPlayers;

	public bool playerDataDirty;

	public bool playerGroupsDirty;

	public bool bannedListDirty;

	public bool whiteListDirty;

	Dictionary<int, PlayerGroup> IGroupManager.PlayerGroupsById => PlayerGroupsById;

	Dictionary<string, IServerPlayerData> IPlayerDataManager.PlayerDataByUid
	{
		get
		{
			Dictionary<string, IServerPlayerData> dict = new Dictionary<string, IServerPlayerData>();
			foreach (KeyValuePair<string, ServerPlayerData> val in PlayerDataByUid)
			{
				dict[val.Key] = val.Value;
			}
			return dict;
		}
	}

	public PlayerDataManager(ServerMain server)
	{
		this.server = server;
		server.RegisterGameTickListener(OnCheckRequireSave, 1000);
		server.EventManager.OnGameWorldBeingSaved += OnGameWorldBeingSaved;
		server.EventManager.OnPlayerJoin += EventManager_OnPlayerJoin;
	}

	private void EventManager_OnPlayerJoin(IServerPlayer byPlayer)
	{
		ServerPlayerData plrdata = GetOrCreateServerPlayerData(byPlayer.PlayerUID, byPlayer.PlayerName);
		plrdata.LastJoinDate = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
		if (plrdata.FirstJoinDate == null)
		{
			plrdata.FirstJoinDate = DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToShortTimeString();
		}
	}

	private void OnGameWorldBeingSaved()
	{
		playerDataDirty = true;
		playerGroupsDirty = true;
		bannedListDirty = true;
		whiteListDirty = true;
		OnCheckRequireSave(0f);
	}

	private void OnCheckRequireSave(float dt)
	{
		if (playerDataDirty)
		{
			try
			{
				using (TextWriter textWriter2 = new StreamWriter(Path.Combine(GamePaths.PlayerData, "playerdata.json")))
				{
					textWriter2.Write(JsonConvert.SerializeObject(PlayerDataByUid.Values.ToList(), Formatting.Indented));
					textWriter2.Close();
				}
				playerDataDirty = false;
			}
			catch (Exception e4)
			{
				ServerMain.Logger.Warning("Failed saving player data, will try again. {0}", e4.Message);
			}
		}
		if (playerGroupsDirty)
		{
			try
			{
				using (TextWriter textWriter3 = new StreamWriter(Path.Combine(GamePaths.PlayerData, "playergroups.json")))
				{
					textWriter3.Write(JsonConvert.SerializeObject(PlayerGroupsById.Values.ToList(), Formatting.Indented));
					textWriter3.Close();
				}
				playerGroupsDirty = false;
			}
			catch (Exception e3)
			{
				ServerMain.Logger.Warning("Failed saving player group data, will try again. {0}", e3.Message);
			}
		}
		if (bannedListDirty)
		{
			try
			{
				using (TextWriter textWriter4 = new StreamWriter(Path.Combine(GamePaths.PlayerData, "playersbanned.json")))
				{
					textWriter4.Write(JsonConvert.SerializeObject(BannedPlayers, Formatting.Indented));
					textWriter4.Close();
				}
				bannedListDirty = false;
			}
			catch (Exception e2)
			{
				ServerMain.Logger.Warning("Failed saving player banned list, will try again. {0}", e2.Message);
			}
		}
		if (!whiteListDirty)
		{
			return;
		}
		try
		{
			using (TextWriter textWriter = new StreamWriter(Path.Combine(GamePaths.PlayerData, "playerswhitelisted.json")))
			{
				textWriter.Write(JsonConvert.SerializeObject(WhitelistedPlayers, Formatting.Indented));
				textWriter.Close();
			}
			whiteListDirty = false;
		}
		catch (Exception e)
		{
			ServerMain.Logger.Warning("Failed saving player whitelist, will try again. {0}", e.Message);
		}
	}

	private List<T> LoadList<T>(string name)
	{
		List<T> elems = null;
		try
		{
			string filepath = Path.Combine(GamePaths.PlayerData, name);
			if (File.Exists(filepath))
			{
				using TextReader textReader = new StreamReader(filepath);
				elems = JsonConvert.DeserializeObject<List<T>>(textReader.ReadToEnd());
				textReader.Close();
			}
			if (elems == null)
			{
				elems = new List<T>();
			}
		}
		catch (Exception e)
		{
			ServerMain.Logger.Error("Failed reading file " + name + ". Will stop server now.");
			ServerMain.Logger.Error(e);
			server.Stop("Failed reading playerdata");
		}
		return elems;
	}

	public void Load()
	{
		PlayerGroupsById = new Dictionary<int, PlayerGroup>();
		PlayerDataByUid = new Dictionary<string, ServerPlayerData>();
		BannedPlayers = new List<PlayerEntry>();
		WhitelistedPlayers = new List<PlayerEntry>();
		List<ServerPlayerData> list = LoadList<ServerPlayerData>("playerdata.json");
		List<PlayerGroup> PlayerGroups = LoadList<PlayerGroup>("playergroups.json");
		List<PlayerEntry> PlayerBans = LoadList<PlayerEntry>("playersbanned.json");
		List<PlayerEntry> PlayerWhitelist = LoadList<PlayerEntry>("playerswhitelisted.json");
		foreach (ServerPlayerData plrdata in list)
		{
			PlayerDataByUid[plrdata.PlayerUID] = plrdata;
		}
		foreach (PlayerGroup group in PlayerGroups)
		{
			PlayerGroupsById[group.Uid] = group;
		}
		foreach (PlayerEntry ban in PlayerBans)
		{
			if (ban.UntilDate >= DateTime.Now)
			{
				BannedPlayers.Add(ban);
			}
			else
			{
				bannedListDirty = true;
			}
		}
		foreach (PlayerEntry whitelist in PlayerWhitelist)
		{
			WhitelistedPlayers.Add(whitelist);
		}
	}

	public PlayerGroup PlayerGroupForPrivateMessage(ConnectedClient sender, ConnectedClient receiver)
	{
		string md5 = GameMath.Md5Hash(sender.ServerData.PlayerUID + "-" + receiver.ServerData.PlayerUID);
		foreach (PlayerGroup group2 in PlayerGroupsById.Values)
		{
			if (group2.Md5Identifier == md5)
			{
				return group2;
			}
		}
		PlayerGroup group = new PlayerGroup
		{
			OwnerUID = receiver.ServerData.PlayerUID,
			CreatedDate = DateTime.Today.ToLongDateString(),
			Md5Identifier = md5,
			Name = "PM from " + sender.PlayerName + " to " + receiver.PlayerName,
			CreatedByPrivateMessage = true
		};
		AddPlayerGroup(group);
		return group;
	}

	public bool CanCreatePlayerGroup(string playerUid)
	{
		ServerPlayerData plrdata = GetOrCreateServerPlayerData(playerUid);
		if (plrdata == null)
		{
			return false;
		}
		if (!plrdata.HasPrivilege(Privilege.manageplayergroups, server.Config.RolesByCode))
		{
			return false;
		}
		int channels = 0;
		foreach (PlayerGroupMembership value in plrdata.PlayerGroupMemberShips.Values)
		{
			if (value.Level == EnumPlayerGroupMemberShip.Owner)
			{
				channels++;
			}
		}
		return channels < server.Config.MaxOwnedGroupChannelsPerUser;
	}

	public PlayerGroup GetPlayerGroupByName(string name)
	{
		foreach (PlayerGroup group in PlayerGroupsById.Values)
		{
			if (group.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
			{
				return group;
			}
		}
		return null;
	}

	public void AddPlayerGroup(PlayerGroup group)
	{
		int maxgid = 0;
		foreach (int key in PlayerGroupsById.Keys)
		{
			maxgid = Math.Max(key, maxgid);
		}
		if (maxgid >= server.Config.NextPlayerGroupUid)
		{
			server.Config.NextPlayerGroupUid = maxgid + 1;
			server.ConfigNeedsSaving = true;
		}
		group.Uid = server.Config.NextPlayerGroupUid++;
		server.ConfigNeedsSaving = true;
		PlayerGroupsById[group.Uid] = group;
	}

	public void RemovePlayerGroup(PlayerGroup group)
	{
		PlayerGroupsById.Remove(group.Uid);
	}

	public ServerPlayerData GetOrCreateServerPlayerData(string playerUID, string playerName = null)
	{
		ServerPlayerData plrdata = null;
		PlayerDataByUid.TryGetValue(playerUID, out plrdata);
		string defaultRoleCode = server.Config.DefaultRole.Code;
		if (plrdata == null)
		{
			plrdata = new ServerPlayerData
			{
				AllowInvite = true,
				PlayerUID = playerUID,
				RoleCode = defaultRoleCode,
				LastKnownPlayername = playerName
			};
			PlayerDataByUid[playerUID] = plrdata;
			playerDataDirty = true;
		}
		if (server.GetClientByUID(playerUID)?.FromSocketListener.GetType() == typeof(DummyTcpNetServer))
		{
			plrdata.RoleCode = server.Config.Roles.MaxBy((PlayerRole v) => v.PrivilegeLevel).Code;
		}
		return plrdata;
	}

	public ServerPlayerData GetServerPlayerDataByLastKnownPlayername(string playername)
	{
		foreach (ServerPlayerData plrdata in PlayerDataByUid.Values)
		{
			if (plrdata.LastKnownPlayername != null && plrdata.LastKnownPlayername.Equals(playername, StringComparison.InvariantCultureIgnoreCase))
			{
				return plrdata;
			}
		}
		return null;
	}

	internal void BanPlayer(string playername, string playeruid, string byPlayerName, string reason = "", DateTime? untildate = null)
	{
		PlayerEntry entry = GetPlayerBan(playername, playeruid);
		if (entry == null)
		{
			BannedPlayers.Add(new PlayerEntry
			{
				PlayerName = playername,
				IssuedByPlayerName = byPlayerName,
				PlayerUID = playeruid,
				Reason = reason,
				UntilDate = untildate
			});
			ServerMain.Logger.Audit("{0} was banned by {1} until {2}. Reason: {3}", playername, byPlayerName, untildate, reason);
		}
		else
		{
			entry.Reason = reason;
			entry.UntilDate = untildate;
			ServerMain.Logger.Audit("Existing player ban of {0} updated by {1}. Now until {2}, Reason: {3}", playername, byPlayerName, untildate, reason);
		}
		bannedListDirty = true;
	}

	internal bool UnbanPlayer(string playername, string playeruid, string issuingPlayerName)
	{
		PlayerEntry entry = GetPlayerBan(playername, playeruid);
		if (entry != null)
		{
			BannedPlayers.Remove(entry);
			bannedListDirty = true;
			ServerMain.Logger.Audit("{0} was unbanned by {1}.", playername, issuingPlayerName);
			return true;
		}
		return false;
	}

	public bool UnWhitelistPlayer(string playername, string playeruid)
	{
		PlayerEntry entry = GetPlayerWhitelist(playername, playeruid);
		if (entry != null)
		{
			WhitelistedPlayers.Remove(entry);
			whiteListDirty = true;
			return true;
		}
		return false;
	}

	public void WhitelistPlayer(string playername, string playeruid, string byPlayername, string reason = "", DateTime? untildate = null)
	{
		PlayerEntry entry = GetPlayerWhitelist(playername, playeruid);
		if (entry == null)
		{
			WhitelistedPlayers.Add(new PlayerEntry
			{
				PlayerName = playername,
				IssuedByPlayerName = byPlayername,
				Reason = reason,
				UntilDate = untildate,
				PlayerUID = playeruid
			});
		}
		else
		{
			entry.Reason = reason;
			entry.UntilDate = untildate;
		}
		whiteListDirty = true;
	}

	public PlayerEntry GetPlayerBan(string playername, string playeruid)
	{
		PlayerEntry entry = GetPlayerEntry(BannedPlayers, playeruid, playername);
		if (entry == null)
		{
			return null;
		}
		if (playeruid != null && playeruid != entry.PlayerUID)
		{
			entry.PlayerUID = playeruid;
			bannedListDirty = true;
		}
		return entry;
	}

	public PlayerEntry GetPlayerWhitelist(string playername, string playeruid)
	{
		PlayerEntry entry = GetPlayerEntry(WhitelistedPlayers, playeruid, playername);
		if (entry == null)
		{
			return null;
		}
		if (playeruid != null && playeruid != entry.PlayerUID)
		{
			entry.PlayerUID = playeruid;
			whiteListDirty = true;
		}
		return entry;
	}

	private PlayerEntry GetPlayerEntry(List<PlayerEntry> list, string playeruid, string playername)
	{
		foreach (PlayerEntry entry in list)
		{
			if (entry.PlayerUID == null || playeruid == null)
			{
				if (entry.PlayerName?.ToLowerInvariant() == playername?.ToLowerInvariant())
				{
					return entry;
				}
			}
			else if (entry.PlayerUID == playeruid)
			{
				return entry;
			}
		}
		return null;
	}

	public void SetRole(IServerPlayer player, IPlayerRole role)
	{
		if (!server.Config.RolesByCode.ContainsKey(role.Code))
		{
			throw new ArgumentException("No such role configured '" + role.Code + "'");
		}
		GetOrCreateServerPlayerData(player.PlayerUID).SetRole(role as PlayerRole);
	}

	public void SetRole(IServerPlayer player, string roleCode)
	{
		if (!server.Config.RolesByCode.ContainsKey(roleCode))
		{
			throw new ArgumentException("No such role configured '" + roleCode + "'");
		}
		GetOrCreateServerPlayerData(player.PlayerUID).SetRole(server.Config.RolesByCode[roleCode]);
	}

	public IPlayerRole GetRole(string code)
	{
		return server.Config.RolesByCode[code];
	}

	public void RegisterPrivilege(string code, string shortdescription, bool adminAutoGrant = true)
	{
		server.AllPrivileges.Add(code);
		server.PrivilegeDescriptions[code] = shortdescription;
		if (!adminAutoGrant)
		{
			return;
		}
		foreach (PlayerRole role in server.Config.RolesByCode.Values)
		{
			if (role.AutoGrant)
			{
				role.GrantPrivilege(code);
			}
		}
	}

	public void GrantTemporaryPrivilege(string code)
	{
		server.Config.RuntimePrivileveCodes.Add(code);
	}

	public void DropTemporaryPrivilege(string code)
	{
		server.Config.RuntimePrivileveCodes.Remove(code);
	}

	public bool GrantPrivilege(string playerUID, string code, bool permanent = false)
	{
		ServerPlayerData plrdata = GetOrCreateServerPlayerData(playerUID);
		if (plrdata == null)
		{
			return false;
		}
		if (permanent)
		{
			plrdata.GrantPrivilege(code);
		}
		else
		{
			plrdata.RuntimePrivileges.Add(code);
		}
		return true;
	}

	public bool DenyPrivilege(string playerUID, string code)
	{
		ServerPlayerData plrdata = GetOrCreateServerPlayerData(playerUID);
		if (plrdata == null)
		{
			return false;
		}
		plrdata.DenyPrivilege(code);
		return true;
	}

	public bool RemovePrivilegeDenial(string playerUID, string code)
	{
		ServerPlayerData plrdata = GetOrCreateServerPlayerData(playerUID);
		if (plrdata == null)
		{
			return false;
		}
		plrdata.RemovePrivilegeDenial(code);
		return true;
	}

	public bool RevokePrivilege(string playerUID, string code, bool permanent = false)
	{
		ServerPlayerData plrdata = GetOrCreateServerPlayerData(playerUID);
		if (plrdata == null)
		{
			return false;
		}
		if (permanent)
		{
			plrdata.RevokePrivilege(code);
		}
		else
		{
			plrdata.RuntimePrivileges.Remove(code);
		}
		return true;
	}

	public bool AddPrivilegeToGroup(string groupCode, string privilegeCode)
	{
		PlayerRole group = null;
		server.Config.RolesByCode.TryGetValue(groupCode, out group);
		if (group == null)
		{
			return false;
		}
		group.RuntimePrivileges.Add(privilegeCode);
		return true;
	}

	public bool RemovePrivilegeFromGroup(string groupCode, string privilegeCode)
	{
		PlayerRole group = null;
		server.Config.RolesByCode.TryGetValue(groupCode, out group);
		if (group == null)
		{
			return false;
		}
		group.RuntimePrivileges.Remove(privilegeCode);
		return true;
	}

	public int GetPlayerPermissionLevel(int player)
	{
		return server.Clients[player].ServerData.GetPlayerRole(server).PrivilegeLevel;
	}

	public IServerPlayerData GetPlayerDataByUid(string playerUid)
	{
		ServerPlayerData plrdata = null;
		PlayerDataByUid.TryGetValue(playerUid, out plrdata);
		return plrdata;
	}

	public IServerPlayerData GetPlayerDataByLastKnownName(string name)
	{
		return GetServerPlayerDataByLastKnownPlayername(name);
	}

	public void ResolvePlayerName(string playername, Action<EnumServerResponse, string> onPlayerReceived)
	{
		server.GetOnlineOrOfflinePlayer(playername, onPlayerReceived);
	}

	public void ResolvePlayerUid(string playeruid, Action<EnumServerResponse, string> onPlayerReceived)
	{
		server.GetOnlineOrOfflinePlayerByUid(playeruid, onPlayerReceived);
	}
}
