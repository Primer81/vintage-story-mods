using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server.Network;

namespace Vintagestory.Server;

internal class CmdToggleAllowLan
{
	private ServerMain server;

	public CmdToggleAllowLan(ServerMain server)
	{
		this.server = server;
		server.api.ChatCommands.Create("allowlan").RequiresPrivilege(Privilege.controlserver).WithDescription("Whether or not to allow external LAN connections to the server")
			.WithAdditionalInformation("(this is a temporary runtime setting for non dedicated servers, i.e. single player games)")
			.WithArgs(server.api.ChatCommands.Parsers.OptionalBool("state"))
			.HandleWith(handle);
	}

	private TextCommandResult handle(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("LAN connections are currently " + ((server.MainSockets[1] == null) ? "disabled" : "enabled"));
		}
		if ((bool)args[0])
		{
			if (server.MainSockets[1] == null)
			{
				server.MainSockets[1] = new TcpNetServer();
				server.MainSockets[1].SetIpAndPort(server.CurrentIp, server.CurrentPort);
				server.MainSockets[1].Start();
				server.UdpSockets[1] = new UdpNetServer(server.Clients);
				server.UdpSockets[1].SetIpAndPort(server.CurrentIp, server.CurrentPort);
				server.UdpSockets[1].Start();
				if (server.CurrentIp != null)
				{
					_ = server.CurrentIp;
				}
				else
				{
					RuntimeEnv.GetLocalIpAddress();
				}
				return TextCommandResult.Success(Lang.Get("LAN connections enabled, players in the local network can now connect"));
			}
			return TextCommandResult.Success("LAN connections was already enabled");
		}
		if (server.MainSockets[1] == null)
		{
			return TextCommandResult.Success("LAN connections was already disabled");
		}
		server.MainSockets[1].Dispose();
		server.MainSockets[1] = null;
		server.UdpSockets[1].Dispose();
		server.UdpSockets[1] = null;
		return TextCommandResult.Success("LAN connections disabled");
	}
}
