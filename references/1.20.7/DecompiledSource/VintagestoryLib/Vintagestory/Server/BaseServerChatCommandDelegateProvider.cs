using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public abstract class BaseServerChatCommandDelegateProvider : LegacyServerChatCommand
{
	protected ServerMain server;

	protected bool ConfigNeedsSaving
	{
		get
		{
			return server.ConfigNeedsSaving;
		}
		set
		{
			server.ConfigNeedsSaving = value;
		}
	}

	protected ServerConfig Config => server.Config;

	protected ServerWorldMap Servermap => server.WorldMap;

	public string syntax { get; set; }

	protected void ServerEventLog(string p)
	{
		ServerMain.Logger.Event(p);
	}

	protected void ErrorSyntax(string command, IServerPlayer player, int groupId)
	{
		player.SendMessage(groupId, "Syntax: " + syntax, EnumChatType.CommandError);
	}

	protected bool PlayerHasPrivilege(int player, string privilege)
	{
		return server.PlayerHasPrivilege(player, privilege);
	}

	public BaseServerChatCommandDelegateProvider(ServerMain server)
	{
		this.server = server;
	}

	public ServerChatCommandDelegate GetDelegate()
	{
		return Handle;
	}

	public ConnectedClient GetClient(IServerPlayer player)
	{
		if (player is ServerConsolePlayer)
		{
			return server.ServerConsoleClient;
		}
		return server.Clients[player.ClientId];
	}

	protected void Success(IServerPlayer player, int groupId, string message)
	{
		player.SendMessage(groupId, message, EnumChatType.CommandSuccess);
	}

	protected void Error(IServerPlayer player, int groupId, string message)
	{
		player.SendMessage(groupId, message, EnumChatType.CommandError);
	}

	protected bool CanKickOrBanTarget(int groupId, IServerPlayer issuingPlayer, string targetPlayerName)
	{
		ServerPlayerData plrdata = server.PlayerDataManager.GetServerPlayerDataByLastKnownPlayername(targetPlayerName);
		if (plrdata == null)
		{
			return true;
		}
		PlayerRole hisGroup = plrdata.GetPlayerRole(server);
		if (hisGroup == null)
		{
			return true;
		}
		PlayerRole ownGroup = Config.RolesByCode[GetClient(issuingPlayer).ServerData.RoleCode];
		if (hisGroup.IsSuperior(ownGroup) || (hisGroup.EqualLevel(ownGroup) && !issuingPlayer.HasPrivilege(Privilege.root)))
		{
			Error(issuingPlayer, groupId, Lang.Get("Can't kick or ban a player with a superior or equal group level"));
			return false;
		}
		return true;
	}

	public abstract void Handle(IServerPlayer player, int groupId, CmdArgs args);
}
