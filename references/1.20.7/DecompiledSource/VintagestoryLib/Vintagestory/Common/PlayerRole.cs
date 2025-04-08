using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Serialization;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class PlayerRole : IPlayerRole, IComparable<IPlayerRole>
{
	public string Code { get; set; }

	public int PrivilegeLevel { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public PlayerSpawnPos DefaultSpawn { get; set; }

	public PlayerSpawnPos ForcedSpawn { get; set; }

	public List<string> Privileges { get; set; }

	public HashSet<string> RuntimePrivileges { get; set; }

	public EnumGameMode DefaultGameMode { get; set; }

	public Color Color { get; set; }

	public int LandClaimAllowance { get; set; }

	public Vec3i LandClaimMinSize { get; set; } = new Vec3i(5, 5, 5);


	public int LandClaimMaxAreas { get; set; } = 3;


	public bool AutoGrant { get; set; }

	public PlayerRole()
	{
		PrivilegeLevel = 0;
		Privileges = new List<string>();
		RuntimePrivileges = new HashSet<string>();
		Color = Color.White;
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		if (Privileges == null)
		{
			Privileges = new List<string>();
		}
		if (RuntimePrivileges == null)
		{
			RuntimePrivileges = new HashSet<string>();
		}
	}

	public override string ToString()
	{
		return $"{Code}:{Name}:{PrivilegeLevel}:{PrivilegesString(Privileges)}:{Color.ToString()}";
	}

	public int CompareTo(IPlayerRole other)
	{
		return PrivilegeLevel.CompareTo(other.PrivilegeLevel);
	}

	public bool IsSuperior(IPlayerRole clientGroup)
	{
		if (clientGroup == null)
		{
			return true;
		}
		return PrivilegeLevel > clientGroup.PrivilegeLevel;
	}

	public bool EqualLevel(IPlayerRole clientGroup)
	{
		return PrivilegeLevel == clientGroup.PrivilegeLevel;
	}

	public void GrantPrivilege(params string[] privileges)
	{
		foreach (string priv in privileges)
		{
			if (!Privileges.Contains(priv))
			{
				Privileges.Add(priv);
			}
		}
	}

	public void RevokePrivilege(string privilege)
	{
		Privileges.Remove(privilege);
	}

	public static string PrivilegesString(List<string> privileges)
	{
		string privilegesString = "";
		if (privileges.Count > 0)
		{
			privilegesString = privileges[0].ToString();
			for (int i = 1; i < privileges.Count; i++)
			{
				privilegesString = privilegesString + "," + privileges[i].ToString();
			}
		}
		return privilegesString;
	}
}
