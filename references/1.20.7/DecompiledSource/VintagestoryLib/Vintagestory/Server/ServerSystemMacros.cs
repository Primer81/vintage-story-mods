using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

public class ServerSystemMacros : ServerSystem
{
	private Dictionary<string, ServerCommandMacro> wipMacroByPlayer = new Dictionary<string, ServerCommandMacro>();

	private Dictionary<string, ServerCommandMacro> ServerCommmandMacros = new Dictionary<string, ServerCommandMacro>();

	public override void OnPlayerDisconnect(ServerPlayer player)
	{
		wipMacroByPlayer.Remove(player.PlayerUID);
	}

	public override void OnBeginConfiguration()
	{
		LoadMacros();
	}

	public ServerSystemMacros(ServerMain server)
		: base(server)
	{
		IChatCommandApi chatCommands = server.api.ChatCommands;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		chatCommands.Create("macro").WithDesc("Manage server side macros").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("addcmd")
			.WithDesc("Append a command")
			.WithArgs(parsers.All("Command to add. {{param0}}, {{param1}}, etc. can be used as placeholders for command arguments."))
			.HandleWith((TextCommandCallingArgs args) => addCmd(args, clear: false))
			.EndSubCommand()
			.BeginSubCommand("setcmd")
			.WithDesc("Set command (clears any previously set commands). {{param0}}, {{param1}}, etc. can be used as placeholders for command arguments.")
			.WithArgs(parsers.All("Command to set"))
			.HandleWith((TextCommandCallingArgs args) => addCmd(args, clear: true))
			.EndSubCommand()
			.BeginSubCommand("desc")
			.WithDesc("Set command description")
			.WithArgs(parsers.All("Description to set"))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				getWipMacro(args.Caller, createIfNotExists: true).Description = (string)args[0];
				return TextCommandResult.Success(Lang.Get("Ok, description set"));
			})
			.EndSubCommand()
			.BeginSubCommand("priv")
			.WithDesc("Set command privilege")
			.WithArgs(parsers.Word("Required privilege to run command", Privilege.AllCodes().Append("or custom privelges")))
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				getWipMacro(args.Caller, createIfNotExists: true).Privilege = (string)args[0];
				return TextCommandResult.Success(Lang.Get("Ok, privilege set"));
			})
			.EndSubCommand()
			.BeginSubCommand("discard")
			.WithDesc("Discard wip macro")
			.HandleWith(delegate(TextCommandCallingArgs args)
			{
				wipMacroByPlayer.Remove(args.Caller.Player?.PlayerUID ?? "_console");
				return TextCommandResult.Success("wip macro discarded");
			})
			.EndSubCommand()
			.BeginSubCommand("save")
			.WithDesc("Save wip macro")
			.WithArgs(parsers.Word("name of the macro"))
			.HandleWith(saveMacro)
			.EndSubCommand()
			.BeginSubCommand("delete")
			.WithDesc("Delete a macro")
			.WithArgs(parsers.Word("macro name"))
			.HandleWith(deleteMacro)
			.EndSubCommand()
			.BeginSubCommand("list")
			.WithDesc("List current macros")
			.HandleWith(listMacros)
			.EndSubCommand()
			.BeginSubCommand("show")
			.WithDesc("Show given info on macro")
			.WithArgs(parsers.Word("macro name"))
			.HandleWith(showMacro)
			.EndSubCommand()
			.BeginSubCommand("showwip")
			.WithDesc("Show info on current wip macro")
			.HandleWith(showWipMacro)
			.EndSubCommand();
	}

	private TextCommandResult saveMacro(TextCommandCallingArgs args)
	{
		string macroname = (string)args[0];
		if (server.api.commandapi.Get(macroname) != null)
		{
			return TextCommandResult.Error(Lang.Get("Command /{0} is already taken, please choose another name", macroname), "commandnameused");
		}
		ServerCommandMacro macro = getWipMacro(args.Caller, createIfNotExists: false);
		if (macro == null || macro.Commands.Length == 0)
		{
			return TextCommandResult.Error(Lang.Get("No commands defined for this macro. Add at least 1 command first."), "nocommandsdefined");
		}
		if (macro.Privilege == null)
		{
			return TextCommandResult.Error(Lang.Get("No privilege defined for this macro. Set privilege with /macro priv."), "noprivdefined");
		}
		macro.CreatedByPlayerUid = args.Caller.Player?.PlayerUID ?? "console";
		ServerCommmandMacros[macroname] = macro;
		RegisterMacro(macro);
		SaveMacros();
		wipMacroByPlayer.Remove(args.Caller.Player?.PlayerUID ?? "_console");
		return TextCommandResult.Success(Lang.Get("Ok, command created. You can use it now."));
	}

	private TextCommandResult showWipMacro(TextCommandCallingArgs args)
	{
		ServerCommandMacro macro = getWipMacro(args.Caller, createIfNotExists: false);
		if (macro != null)
		{
			return TextCommandResult.Success(Lang.Get("Name: {0}\nDescription: {1}\nRequired privilege: {2}\nCommands: {3}", macro.Name, macro.Syntax, macro.Description, macro.Privilege, macro.Commands));
		}
		return TextCommandResult.Error(Lang.Get("No macro in wip"), "nomacroinwip");
	}

	private TextCommandResult showMacro(TextCommandCallingArgs args)
	{
		string name = (string)args[0];
		if (ServerCommmandMacros.TryGetValue(name, out var macro))
		{
			return TextCommandResult.Success(Lang.Get("Name: {0}\nDescription: {1}\nRequired privilege: {2}\nCommands: {3}", macro.Name, macro.Syntax, macro.Description, macro.Privilege, macro.Commands));
		}
		return TextCommandResult.Error(Lang.Get("No such macro found"), "notfound");
	}

	private TextCommandResult listMacros(TextCommandCallingArgs args)
	{
		if (ServerCommmandMacros.Count > 0)
		{
			StringBuilder macrosList = new StringBuilder();
			foreach (ServerCommandMacro macro in ServerCommmandMacros.Values)
			{
				macrosList.AppendLine("  /" + macro.Name + " " + macro.Syntax + " - " + macro.Description);
			}
			return TextCommandResult.Success(Lang.Get("{0}Type /macro show [name] to see more info about a particular macro", macrosList.ToString()));
		}
		return TextCommandResult.Error("No macros defined on this server", "nomacros");
	}

	private TextCommandResult deleteMacro(TextCommandCallingArgs args)
	{
		string name = (string)args[0];
		if (ServerCommmandMacros.TryGetValue(name, out var macro))
		{
			ServerCommmandMacros.Remove(macro.Name);
			server.api.commandapi.UnregisterCommand(macro.Name);
			SaveMacros();
			return TextCommandResult.Success("Ok, macro deleted");
		}
		return TextCommandResult.Error("No such macro found", "nosuchmacro");
	}

	private TextCommandResult addCmd(TextCommandCallingArgs args, bool clear)
	{
		ServerCommandMacro macro = getWipMacro(args.Caller, createIfNotExists: true);
		if (clear)
		{
			macro.Commands = "";
		}
		macro.Commands += (string)args[0];
		macro.Commands += "\n";
		return TextCommandResult.Success(Lang.Get("Ok, command added."));
	}

	private ServerCommandMacro getWipMacro(Caller caller, bool createIfNotExists)
	{
		string key = caller.Player?.PlayerUID ?? "_console";
		if (wipMacroByPlayer.TryGetValue(key, out var macro))
		{
			return macro;
		}
		if (createIfNotExists)
		{
			macro = new ServerCommandMacro();
			return wipMacroByPlayer[key] = macro;
		}
		return null;
	}

	private void OnMacro(string name, TextCommandCallingArgs args, Action<TextCommandResult> onCommandComplete = null)
	{
		if (!ServerCommmandMacros.ContainsKey(name))
		{
			onCommandComplete(TextCommandResult.Error("No such macro found", "nosuchmacro"));
		}
		ServerCommandMacro macro = ServerCommmandMacros[name];
		string[] commands = macro.Commands.Split('\n');
		int success = 0;
		for (int i = 0; i < commands.Length; i++)
		{
			int index = i;
			string message = commands[i];
			for (int j = 0; j < args.RawArgs.Length; j++)
			{
				message = message.Replace("{param" + (j + 1) + "}", args.RawArgs[j]);
			}
			message = Regex.Replace(message, "{param\\d+}", "");
			if (message.Length == 0)
			{
				continue;
			}
			string[] ss = message.Split(new char[1] { ' ' });
			string command = ss[0].Replace("/", "");
			string argument = ((message.IndexOf(' ') < 0) ? "" : message.Substring(message.IndexOf(' ') + 1));
			server.api.ChatCommands.Execute(command, new TextCommandCallingArgs
			{
				Caller = args.Caller,
				RawArgs = new CmdArgs(argument)
			}, delegate(TextCommandResult result)
			{
				if (result.Status == EnumCommandStatus.Success)
				{
					int num = success;
					success = num + 1;
				}
				if (index == command.Length - 1)
				{
					onCommandComplete(TextCommandResult.Success(Lang.Get("Macro executed. {0}/{1} commands successful.", success, commands.Length)));
				}
			});
		}
	}

	public void LoadMacros()
	{
		string filename = "servermacros.json";
		if (!File.Exists(Path.Combine(GamePaths.Config, filename)))
		{
			return;
		}
		try
		{
			List<ServerCommandMacro> macros = null;
			using (TextReader textReader = new StreamReader(Path.Combine(GamePaths.Config, filename)))
			{
				macros = JsonConvert.DeserializeObject<List<ServerCommandMacro>>(textReader.ReadToEnd());
				textReader.Close();
			}
			foreach (ServerCommandMacro macro in macros)
			{
				ServerCommmandMacros[macro.Name] = macro;
				RegisterMacro(macro);
			}
			ServerMain.Logger.Notification("{0} Macros loaded", macros.Count);
		}
		catch (Exception e)
		{
			ServerMain.Logger.Error("Failed loading {0}:", filename);
			ServerMain.Logger.Error(e);
		}
	}

	private void RegisterMacro(ServerCommandMacro macro)
	{
		server.api.ChatCommands.Create(macro.Name).WithDesc(macro.Description).HandleWith(delegate(TextCommandCallingArgs args)
		{
			OnMacro(macro.Name, args);
			return TextCommandResult.Deferred;
		})
			.RequiresPrivilege(macro.Privilege);
	}

	public void SaveMacros()
	{
		StreamWriter streamWriter = new StreamWriter(Path.Combine(GamePaths.Config, "servermacros.json"));
		streamWriter.Write(JsonConvert.SerializeObject(ServerCommmandMacros.Values, Formatting.Indented));
		streamWriter.Close();
	}
}
