using System;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.ModDb;

namespace Vintagestory.Server;

internal class CmdModDBUtil
{
	private ServerMain server;

	private ModDbUtil modDbUtil;

	public CmdModDBUtil(ServerMain server)
	{
		this.server = server;
		modDbUtil = new ModDbUtil(server.api, server.Config.ModDbUrl, GamePaths.DataPathMods);
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.commandapi.Create("moddb").RequiresPrivilege(Privilege.controlserver).WithDescription("ModDB utility. To install and remove mods.")
			.WithPreCondition(OnPrecondition)
			.BeginSubCommand("install")
			.WithDescription("Install the specified mod")
			.WithArgs(parsers.Word("modid"), parsers.OptionalWord("forGameVersion"))
			.HandleWith((TextCommandCallingArgs args) => handleTwoArgs(args, modDbUtil.onInstallCommand))
			.EndSubCommand()
			.BeginSubCommand("remove")
			.WithDescription("Uninstall the specified mod")
			.WithArgs(parsers.Word("modid"))
			.HandleWith((TextCommandCallingArgs args) => handleOneArg(args, modDbUtil.onRemoveCommand))
			.EndSubCommand()
			.BeginSubCommand("list")
			.WithDescription("List all installed mods")
			.HandleWith(handleList)
			.EndSubCommand()
			.BeginSubCommand("search")
			.WithDescription("Search for a mod, filtered for the current game version only")
			.WithArgs(parsers.Word("modid"))
			.HandleWith((TextCommandCallingArgs args) => handleOneArg(args, modDbUtil.onSearchCommand))
			.EndSubCommand()
			.BeginSubCommand("searchcompatible")
			.WithAlias("searchc")
			.WithDescription("Search for a mod, filtered for game versions compatible with the current version")
			.WithArgs(parsers.Word("modid"))
			.HandleWith((TextCommandCallingArgs args) => handleOneArg(args, modDbUtil.onSearchCompatibleCommand))
			.EndSubCommand()
			.BeginSubCommand("searchfor")
			.WithDescription("Search for a mod, filtered for the specified game version only")
			.WithArgs(parsers.Word("version"), parsers.Word("modid"))
			.HandleWith((TextCommandCallingArgs args) => handleTwoArgs(args, modDbUtil.onSearchforCommand))
			.EndSubCommand()
			.BeginSubCommand("searchforc")
			.WithDescription("Search for a mod, filtered for game versions compatible with the specified version")
			.WithArgs(parsers.Word("version"), parsers.Word("modid"))
			.HandleWith((TextCommandCallingArgs args) => handleTwoArgs(args, modDbUtil.onSearchforAndCompatibleCommand))
			.EndSubCommand()
			.Validate();
	}

	private TextCommandResult OnPrecondition(TextCommandCallingArgs args)
	{
		if (!server.Config.HostedMode || (server.Config.HostedMode && server.Config.HostedModeAllowMods))
		{
			return TextCommandResult.Success();
		}
		return TextCommandResult.Error("Command not available. Disabled probably by the host.");
	}

	private TextCommandResult handleOneArg(TextCommandCallingArgs args, Action<string, Action<string>> modDbCommand)
	{
		string result = modDbUtil.preConsoleCommand();
		if (result != null)
		{
			return TextCommandResult.Error(result);
		}
		modDbCommand((string)args[0], delegate(string response)
		{
			server.SendMessage(args.Caller, response, EnumChatType.CommandSuccess);
		});
		return TextCommandResult.Deferred;
	}

	private TextCommandResult handleTwoArgs(TextCommandCallingArgs args, Action<string, string, Action<string>> modDbCommand)
	{
		string result = modDbUtil.preConsoleCommand();
		if (result != null)
		{
			return TextCommandResult.Error(result);
		}
		modDbCommand((string)args[0], (string)args[1], delegate(string response)
		{
			server.SendMessage(args.Caller, response, EnumChatType.CommandSuccess);
		});
		return TextCommandResult.Deferred;
	}

	private TextCommandResult handleList(TextCommandCallingArgs args)
	{
		string result = modDbUtil.preConsoleCommand();
		if (result != null)
		{
			return TextCommandResult.Error(result);
		}
		modDbUtil.onListCommand(delegate(string response)
		{
			server.SendMessage(args.Caller, response, EnumChatType.CommandSuccess);
		});
		return TextCommandResult.Deferred;
	}
}
