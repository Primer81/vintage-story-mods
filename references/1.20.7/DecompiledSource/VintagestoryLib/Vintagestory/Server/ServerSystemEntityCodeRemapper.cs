using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class ServerSystemEntityCodeRemapper : ServerSystem
{
	public ServerSystemEntityCodeRemapper(ServerMain server)
		: base(server)
	{
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.ChatCommands.Create("eir").RequiresPrivilege(Privilege.controlserver).WithDescription("Entity code remapper info and fixing tool")
			.BeginSubCommand("list")
			.WithDescription("list")
			.HandleWith(OnCmdList)
			.EndSubCommand()
			.BeginSubCommand("map")
			.WithDescription("map")
			.RequiresPlayer()
			.WithArgs(parsers.Word("new_entity"), parsers.Word("old_entity"), parsers.OptionalWord("force"))
			.HandleWith(OnCmdMap)
			.EndSubCommand()
			.BeginSubCommand("remap")
			.WithAlias("remapq")
			.WithDescription("map")
			.RequiresPlayer()
			.WithArgs(parsers.Word("new_entity"), parsers.Word("old_entity"), parsers.OptionalWord("force"))
			.HandleWith(OnCmdReMap)
			.EndSubCommand();
	}

	private TextCommandResult OnCmdReMap(TextCommandCallingArgs args)
	{
		bool quiet = args.SubCmdCode == "remapq";
		string newEntityCode = args[0] as string;
		string oldEntityCode = args[1] as string;
		bool force = !args.Parsers[2].IsMissing && args[2] as string == "force";
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		Addmapping(server.EntityCodeRemappings, newEntityCode, oldEntityCode, player, args.Caller.FromChatGroupId, remap: true, force, quiet);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdMap(TextCommandCallingArgs args)
	{
		string newEntityCode = args[0] as string;
		string oldEntityCode = args[1] as string;
		bool force = !args.Parsers[2].IsMissing && args[2] as string == "force";
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		Addmapping(server.EntityCodeRemappings, newEntityCode, oldEntityCode, player, args.Caller.FromChatGroupId, remap: false, force, quiet: false);
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdList(TextCommandCallingArgs args)
	{
		Dictionary<string, string> entityCodeRemappings = server.EntityCodeRemappings;
		ServerMain.Logger.Notification("Current entity code remapping (issued by /eir list command)");
		foreach (KeyValuePair<string, string> val in entityCodeRemappings)
		{
			ServerMain.Logger.Notification("  " + val.Key + ": " + val.Value);
		}
		return TextCommandResult.Success("Full mapping printed to console and main log file");
	}

	private void Addmapping(Dictionary<string, string> entityRemaps, string newCode, string oldCode, IServerPlayer player, int groupId, bool remap, bool force, bool quiet)
	{
		if (!force && entityRemaps.TryGetValue(oldCode, out var prevCode))
		{
			player.SendMessage(groupId, "new entity code " + oldCode + " is already mapped to " + prevCode + ", type '/eir " + (remap ? "remap" : "map") + " " + newCode + " " + oldCode + " force' to overwrite", EnumChatType.CommandError);
		}
		else
		{
			entityRemaps[oldCode] = newCode;
			if (!quiet)
			{
				string type = (remap ? "remapped" : "mapped");
				player.SendMessage(groupId, newCode + " is now " + type + " from entity code " + oldCode, EnumChatType.CommandSuccess);
			}
		}
	}
}
