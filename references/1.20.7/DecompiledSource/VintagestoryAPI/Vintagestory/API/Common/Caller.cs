using System.Linq;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public class Caller
{
	public EnumCallerType Type;

	public string[] CallerPrivileges;

	public string CallerRole;

	public int FromChatGroupId;

	public Vec3d Pos;

	private IPlayer player;

	private Entity entity;

	public IPlayer Player
	{
		get
		{
			return player;
		}
		set
		{
			player = value;
			Entity = value.Entity;
			Type = EnumCallerType.Player;
		}
	}

	public Entity Entity
	{
		get
		{
			return entity;
		}
		set
		{
			entity = value;
			Pos = entity?.Pos.XYZ;
		}
	}

	public bool HasPrivilege(string privilege)
	{
		if (Player != null && Player.HasPrivilege(privilege))
		{
			return true;
		}
		if (CallerPrivileges != null && (CallerPrivileges.Contains(privilege) || CallerPrivileges.Contains("*")))
		{
			return true;
		}
		return false;
	}

	public IPlayerRole GetRole(ICoreServerAPI sapi)
	{
		if (Player is IServerPlayer splr)
		{
			return splr.Role;
		}
		return sapi.Permissions.GetRole(CallerRole);
	}

	public string GetName()
	{
		if (player != null)
		{
			return "Player " + player.PlayerName;
		}
		if (Type == EnumCallerType.Console)
		{
			return "Console Admin";
		}
		if (Type == EnumCallerType.Block)
		{
			return "Block @" + Pos;
		}
		if (Type == EnumCallerType.Entity)
		{
			return string.Concat("Entity ", entity.Code, " @", Pos?.ToString());
		}
		return "Unknown caller";
	}
}
