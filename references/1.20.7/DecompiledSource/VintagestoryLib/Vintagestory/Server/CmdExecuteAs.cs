using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

internal class CmdExecuteAs
{
	private ICoreServerAPI sapi;

	public CmdExecuteAs(ServerMain server)
	{
		sapi = server.api;
		CommandArgumentParsers parsers = sapi.ChatCommands.Parsers;
		sapi.ChatCommands.Create("executeas").RequiresPrivilege(Privilege.controlserver).WithDesc("Execute command with selected player/entity as the caller, but runs under the caller privileges.")
			.WithExamples("<code>/executeas e[type=wolf*] /setblock rock-granite ~ ~1 ~</code> - Place granite above all wolves")
			.WithArgs(parsers.Entities("caller"), parsers.All("command without /"))
			.HandleWith((TextCommandCallingArgs args) => CmdUtil.EntityEach(args, (Entity e) => executeAs(e, args)));
	}

	private TextCommandResult executeAs(Entity entity, TextCommandCallingArgs args)
	{
		string command = args[1] as string;
		TextCommandResult result = TextCommandResult.Deferred;
		string[] callerPrivs = args.Caller.CallerPrivileges ?? args.Caller.Player?.Privileges;
		Caller caller = new Caller
		{
			Entity = entity,
			Type = EnumCallerType.Entity,
			CallerPrivileges = callerPrivs
		};
		if (entity is EntityPlayer eplr)
		{
			caller.Player = eplr.Player;
		}
		sapi.ChatCommands.ExecuteUnparsed(command, new TextCommandCallingArgs
		{
			Caller = caller
		}, delegate(TextCommandResult res)
		{
			result = res;
		});
		return result;
	}
}
