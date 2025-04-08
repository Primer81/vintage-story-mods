using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

internal class CmdHelp
{
	private ServerMain server;

	public CmdHelp(ServerMain server)
	{
		this.server = server;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.commandapi.GetOrCreate("help").RequiresPrivilege(Privilege.chat).WithArgs(parsers.OptionalWord("commandname"), parsers.OptionalWord("subcommand"), parsers.OptionalWord("subsubcommand"))
			.WithDescription("Display list of available server commands")
			.HandleWith(handleHelp);
	}

	private TextCommandResult handleHelp(TextCommandCallingArgs args)
	{
		StringBuilder text = new StringBuilder();
		Dictionary<string, IChatCommand> commands = IChatCommandApi.GetOrdered(server.api.commandapi.AllSubcommands());
		if (args.Parsers[0].IsMissing)
		{
			text.AppendLine("Available commands:");
			WriteCommandsList(text, commands, args.Caller);
			text.Append("\n" + Lang.Get("Type /help [commandname] to see more info about a command"));
			return TextCommandResult.Success(text.ToString());
		}
		string arg = (string)args[0];
		if (!args.Parsers[1].IsMissing)
		{
			bool found = false;
			foreach (KeyValuePair<string, IChatCommand> entry3 in commands)
			{
				if (entry3.Key == arg)
				{
					commands = IChatCommandApi.GetOrdered(entry3.Value.AllSubcommands);
					found = true;
					break;
				}
			}
			if (!found)
			{
				return TextCommandResult.Error(Lang.Get("No such sub-command found") + ": " + arg + " " + (string)args[1]);
			}
			arg = (string)args[1];
			if (!args.Parsers[2].IsMissing)
			{
				found = false;
				foreach (KeyValuePair<string, IChatCommand> entry2 in commands)
				{
					if (entry2.Key == arg)
					{
						commands = IChatCommandApi.GetOrdered(entry2.Value.AllSubcommands);
						found = true;
						break;
					}
				}
				if (!found)
				{
					return TextCommandResult.Error(Lang.Get("No such sub-command found") + ": " + (string)args[0] + arg + " " + (string)args[2]);
				}
				arg = (string)args[2];
			}
		}
		foreach (KeyValuePair<string, IChatCommand> entry in commands)
		{
			if (!(entry.Key == arg))
			{
				continue;
			}
			ChatCommandImpl cm = entry.Value as ChatCommandImpl;
			if (cm.IsAvailableTo(args.Caller))
			{
				Dictionary<string, IChatCommand> subcommands = cm.AllSubcommands;
				if (subcommands.Count > 0)
				{
					text.AppendLine("Available subcommands:");
					WriteCommandsList(text, subcommands, args.Caller, isSubCommand: true);
					text.AppendLine();
					text.AppendLine("Type <code>/help " + cm.CallSyntax.Substring(1) + " &lt;<i>subcommand_name</i>&gt;</code> for help on a specific subcommand");
				}
				else
				{
					text.AppendLine();
					if (cm.Description != null)
					{
						text.AppendLine(cm.Description);
					}
					if (cm.AdditionalInformation != null)
					{
						text.AppendLine(cm.AdditionalInformation);
					}
					text.AppendLine();
					text.AppendLine("Usage: <code>");
					text.Append(cm.GetCallSyntax(entry.Key));
					text.Append("</code>");
					cm.AddSyntaxExplanation(text, "");
					if (cm.Examples != null && cm.Examples.Length != 0)
					{
						text.AppendLine((cm.Examples.Length > 1) ? "Examples:" : "Example:");
						string[] examples = cm.Examples;
						foreach (string ex in examples)
						{
							text.AppendLine(ex);
						}
					}
				}
				return TextCommandResult.Success(text.ToString());
			}
			return TextCommandResult.Error("Insufficient privilege to use this command");
		}
		return TextCommandResult.Error(Lang.Get("No such command found") + ": " + arg);
	}

	private void WriteCommandsList(StringBuilder text, Dictionary<string, IChatCommand> commands, Caller caller, bool isSubCommand = false)
	{
		text.AppendLine();
		foreach (KeyValuePair<string, IChatCommand> val in commands)
		{
			IChatCommand cm = val.Value;
			if (!cm.IsAvailableTo(caller))
			{
				continue;
			}
			string desc = cm.Description;
			if (desc == null)
			{
				desc = " ";
			}
			else
			{
				int i = desc.IndexOf('\n');
				if (i >= 0)
				{
					desc = desc.Substring(0, i);
				}
				desc = Lang.Get(desc);
			}
			text.AppendLine("<code>" + cm.GetCallSyntax(val.Key, !isSubCommand) + "</code> :  " + desc);
		}
	}
}
