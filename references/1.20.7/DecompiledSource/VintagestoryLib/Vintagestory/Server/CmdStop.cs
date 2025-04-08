using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

internal class CmdStop
{
	private ServerMain server;

	public CmdStop(ServerMain server)
	{
		this.server = server;
		server.api.commandapi.Create("stop").WithArgs(server.api.commandapi.Parsers.OptionalInt("exit code")).RequiresPrivilege(Privilege.controlserver)
			.HandleWith(handleStop);
	}

	private TextCommandResult handleStop(TextCommandCallingArgs args)
	{
		server.BroadcastMessageToAllGroups(Lang.Get("{0} commenced a server shutdown", args.Caller.GetName()), EnumChatType.AllGroups);
		ServerMain.Logger.Event($"{args.Caller.GetName()} shuts down server.");
		server.ExitCode = (int)args[0];
		server.Stop("Shutdown via server command");
		return TextCommandResult.Success("Shut down command executed");
	}
}
