using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;

namespace Vintagestory.Server;

[JsonObject(MemberSerialization.OptIn)]
public class ServerPlayerData : IComparable<ServerPlayerData>, IServerPlayerData
{
	[JsonProperty]
	public string PlayerUID;

	[JsonProperty]
	public string RoleCode;

	[JsonProperty]
	internal HashSet<string> PermaPrivileges;

	[JsonProperty]
	internal HashSet<string> DeniedPrivileges;

	[JsonProperty]
	public Dictionary<int, PlayerGroupMembership> PlayerGroupMemberShips;

	[JsonProperty]
	public bool AllowInvite;

	[JsonProperty]
	public string LastKnownPlayername;

	[JsonProperty]
	public Dictionary<string, string> CustomPlayerData;

	[JsonProperty]
	public int ExtraLandClaimAllowance;

	[JsonProperty]
	public int ExtraLandClaimAreas;

	internal HashSet<string> RuntimePrivileges;

	[JsonProperty]
	public string FirstJoinDate { get; set; }

	[JsonProperty]
	public string LastJoinDate { get; set; }

	[JsonProperty]
	public string LastCharacterSelectionDate { get; set; }

	public Dictionary<int, PlayerGroupMembership> PlayerGroupMemberships => PlayerGroupMemberShips;

	Dictionary<string, string> IServerPlayerData.CustomPlayerData => CustomPlayerData;

	int IServerPlayerData.ExtraLandClaimAllowance
	{
		get
		{
			return ExtraLandClaimAllowance;
		}
		set
		{
			ExtraLandClaimAllowance = value;
		}
	}

	int IServerPlayerData.ExtraLandClaimAreas
	{
		get
		{
			return ExtraLandClaimAreas;
		}
		set
		{
			ExtraLandClaimAreas = value;
		}
	}

	string IServerPlayerData.PlayerUID => PlayerUID;

	string IServerPlayerData.RoleCode => RoleCode;

	HashSet<string> IServerPlayerData.PermaPrivileges => PermaPrivileges;

	HashSet<string> IServerPlayerData.DeniedPrivileges => DeniedPrivileges;

	Dictionary<int, PlayerGroupMembership> IServerPlayerData.PlayerGroupMemberships => PlayerGroupMemberShips;

	bool IServerPlayerData.AllowInvite => AllowInvite;

	string IServerPlayerData.LastKnownPlayername => LastKnownPlayername;

	public ServerPlayerData()
	{
		PlayerUID = "";
		RoleCode = "";
		PermaPrivileges = new HashSet<string>();
		DeniedPrivileges = new HashSet<string>();
		RuntimePrivileges = new HashSet<string>();
		PlayerGroupMemberShips = new Dictionary<int, PlayerGroupMembership>();
		CustomPlayerData = new Dictionary<string, string>();
	}

	public override string ToString()
	{
		return $"{PlayerUID}:{RoleCode}";
	}

	public int CompareTo(ServerPlayerData other)
	{
		return RoleCode.CompareOrdinal(other.RoleCode);
	}

	public void SetRole(PlayerRole newGroup)
	{
		RoleCode = newGroup.Code;
	}

	public void GrantPrivilege(string privilege)
	{
		PermaPrivileges.Add(privilege);
		DeniedPrivileges.Remove(privilege);
	}

	public void DenyPrivilege(string privilege)
	{
		PermaPrivileges.Remove(privilege);
		DeniedPrivileges.Add(privilege);
	}

	public void RevokePrivilege(string privilege)
	{
		PermaPrivileges.Remove(privilege);
		DeniedPrivileges.Add(privilege);
	}

	internal void RemovePrivilegeDenial(string code)
	{
		DeniedPrivileges.Remove(code);
	}

	public PlayerGroupMembership JoinGroup(PlayerGroup group, EnumPlayerGroupMemberShip level)
	{
		Dictionary<int, PlayerGroupMembership> playerGroupMemberShips = PlayerGroupMemberShips;
		int uid = group.Uid;
		PlayerGroupMembership obj = new PlayerGroupMembership
		{
			GroupName = group.Name,
			GroupUid = group.Uid,
			Level = level
		};
		PlayerGroupMembership result = obj;
		playerGroupMemberShips[uid] = obj;
		return result;
	}

	public HashSet<string> GetAllPrivilegeCodes(ServerConfig serverConfig)
	{
		HashSet<string> codes = new HashSet<string>();
		codes.AddRange(PermaPrivileges);
		if (serverConfig.RolesByCode.TryGetValue(RoleCode, out var role))
		{
			codes.AddRange(role.Privileges);
			codes.AddRange(role.RuntimePrivileges);
		}
		foreach (string val in DeniedPrivileges)
		{
			codes.Remove(val);
		}
		codes.AddRange(RuntimePrivileges);
		return codes;
	}

	public void LeaveGroup(PlayerGroup group)
	{
		PlayerGroupMemberShips.Remove(group.Uid);
	}

	public void LeaveGroup(int groupid)
	{
		PlayerGroupMemberShips.Remove(groupid);
	}

	public bool HasPrivilege(string privilege, Dictionary<string, PlayerRole> rolesByCode)
	{
		if (privilege == null)
		{
			return true;
		}
		if (RuntimePrivileges.Contains(privilege))
		{
			return true;
		}
		if (DeniedPrivileges.Contains(privilege))
		{
			return false;
		}
		if (PermaPrivileges.Contains(privilege))
		{
			return true;
		}
		rolesByCode.TryGetValue(RoleCode, out var role);
		if (role == null)
		{
			return false;
		}
		if (role.Privileges.Contains(privilege))
		{
			return true;
		}
		if (role.RuntimePrivileges.Contains(privilege))
		{
			return true;
		}
		return false;
	}

	public PlayerRole GetPlayerRole(ServerMain server)
	{
		server.Config.RolesByCode.TryGetValue(RoleCode, out var role);
		if (role == null)
		{
			ServerMain.Logger.Warning("Player " + LastKnownPlayername + " has role " + RoleCode + " but no such role exists! Assigning to default group");
			RoleCode = server.Config.DefaultRoleCode;
			return server.Config.DefaultRole;
		}
		return role;
	}
}
