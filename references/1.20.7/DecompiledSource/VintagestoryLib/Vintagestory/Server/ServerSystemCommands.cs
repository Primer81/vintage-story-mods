namespace Vintagestory.Server;

internal class ServerSystemCommands : ServerSystem
{
	public ServerSystemCommands(ServerMain server)
		: base(server)
	{
	}

	public override void OnBeginConfiguration()
	{
		new CmdKickBan(server);
		new CmdAnnounce(server);
		new CmdTp(server);
		new CmdLand(server);
		new CmdGlobalList(server);
		new CmdHelp(server);
		new CmdServerConfig(server);
		new CmdWorldConfig(server);
		new CmdWorldConfigCreate(server);
		new CmdEntity(server);
		new CmdGive(server);
		new CmdDebug(server);
		new CmdStop(server);
		new CmdStats(server);
		new CmdInfo(server);
		new CmdChunk(server);
		new CmdModDBUtil(server);
		if (!server.IsDedicatedServer)
		{
			new CmdToggleAllowLan(server);
		}
		new CmdSetBlock(server);
		new CmdExecuteAs(server);
		new CmdActivate(server);
		new CmdTime(server);
	}
}
