using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

public class ServerConsolePlayer : ServerPlayer
{
	private PlayerRole clientGroup;

	public override EnumClientState ConnectionState => EnumClientState.Offline;

	public override string PlayerUID => "console";

	public override IPlayerRole Role => clientGroup;

	public ServerConsolePlayer(ServerMain server, ServerWorldPlayerData worlddata)
		: base(server, worlddata)
	{
		client = server.ServerConsoleClient;
		clientGroup = server.Config.Roles.MaxBy((PlayerRole v) => v.PrivilegeLevel);
	}

	protected override void Init()
	{
	}

	public override void BroadcastPlayerData(bool sendInventory = false)
	{
	}

	public override bool HasPrivilege(string privilegeCode)
	{
		return true;
	}

	public override void Disconnect()
	{
	}

	public override void Disconnect(string message)
	{
	}
}
