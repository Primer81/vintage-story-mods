using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class ServerSystemRemapperAssistant : ServerSystem
{
	private Dictionary<string, string[]> remaps = new Dictionary<string, string[]>();

	public ServerSystemRemapperAssistant(ServerMain server)
		: base(server)
	{
		server.api.ChatCommands.Create("fixmapping").RequiresPrivilege(Privilege.controlserver).BeginSubCommand("doremap")
			.WithDescription("Do remap")
			.WithArgs(server.api.ChatCommands.Parsers.Word("code"))
			.HandleWith(OnCmdDoremap)
			.EndSubCommand()
			.BeginSubCommand("ignoreall")
			.WithDescription("Ignore all remappings")
			.HandleWith(OnCmdIgnoreall)
			.EndSubCommand()
			.BeginSubCommand("applyall")
			.WithDescription("Apply all remappings")
			.WithArgs(server.api.ChatCommands.Parsers.OptionalWord("force"))
			.HandleWith(OnCmdApplyall)
			.EndSubCommand();
	}

	private TextCommandResult OnCmdApplyall(TextCommandCallingArgs args)
	{
		int commandsExecuted = 0;
		int setsprocessed = 0;
		bool force = args[0] as string == "force";
		foreach (KeyValuePair<string, string[]> val in remaps)
		{
			string code = val.Key;
			if (!(!server.SaveGameData.RemappingsAppliedByCode.ContainsKey(code) || force))
			{
				continue;
			}
			setsprocessed++;
			string[] value = val.Value;
			for (int i = 0; i < value.Length; i++)
			{
				string command = value[i].Trim();
				if (command.Length != 0)
				{
					server.HandleChatMessage(args.Caller.Player as IServerPlayer, args.Caller.FromChatGroupId, command);
					commandsExecuted++;
				}
			}
			server.SaveGameData.RemappingsAppliedByCode[code] = true;
		}
		if (commandsExecuted == 0)
		{
			return TextCommandResult.Success("No applicable remappings found, seems all good for now!");
		}
		return TextCommandResult.Success($"Okay, {setsprocessed} remapping sets with a total of {commandsExecuted} remappings commands have been executed. You can now restart your game/server");
	}

	private TextCommandResult OnCmdIgnoreall(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<string, string[]> remap in remaps)
		{
			string code = remap.Key;
			if (!server.SaveGameData.RemappingsAppliedByCode.ContainsKey(code))
			{
				server.SaveGameData.RemappingsAppliedByCode[code] = false;
			}
		}
		return TextCommandResult.Success(Lang.Get("Okay, ignoring all new remappings. You can still manually remap them using /fixmapping doremap [code]"));
	}

	private TextCommandResult OnCmdDoremap(TextCommandCallingArgs args)
	{
		string code = args[0] as string;
		if (!remaps.ContainsKey(code))
		{
			return TextCommandResult.Success(Lang.Get("No remapping group found under this code"));
		}
		string[] array = remaps[code];
		int commandsExecuted = 0;
		string[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			string command = array2[i].Trim();
			if (command.Length != 0)
			{
				server.HandleChatMessage(args.Caller.Player as IServerPlayer, args.Caller.FromChatGroupId, command);
				commandsExecuted++;
			}
		}
		server.SaveGameData.RemappingsAppliedByCode[code] = true;
		return TextCommandResult.Success(Lang.Get("Ok, {0} commands executed.", commandsExecuted));
	}

	public override void Dispose()
	{
		BlockSchematic.BlockRemaps = null;
		BlockSchematic.ItemRemaps = null;
	}

	public override void OnFinalizeAssets()
	{
		remaps = server.AssetManager.Get("config/remaps.json").ToObject<Dictionary<string, string[]>>();
		extractRemapsForSchematicImports();
		HashSet<string> remapcodes = new HashSet<string>(remaps.Keys);
		if (server.SaveGameData.IsNewWorld)
		{
			foreach (string code in remapcodes)
			{
				server.SaveGameData.RemappingsAppliedByCode[code] = true;
			}
		}
		else
		{
			foreach (string code2 in server.SaveGameData.RemappingsAppliedByCode.Keys)
			{
				remapcodes.Remove(code2);
			}
			server.requiresRemaps = remapcodes.Count > 0;
		}
		string[] array = server.AssetManager.Get("config/remapentities.json").ToObject<string[]>();
		for (int i = 0; i < array.Length; i++)
		{
			string[] cmdSplit = array[i].Split(" ");
			if (cmdSplit[0].Equals("/eir"))
			{
				server.EntityCodeRemappings.TryAdd(cmdSplit[3], cmdSplit[2]);
			}
		}
	}

	private void extractRemapsForSchematicImports()
	{
		BlockSchematic.BlockRemaps = new Dictionary<string, Dictionary<string, string>>();
		BlockSchematic.ItemRemaps = new Dictionary<string, Dictionary<string, string>>();
		foreach (KeyValuePair<string, string[]> remapping in remaps)
		{
			Dictionary<string, string> blockMapping = new Dictionary<string, string>();
			Dictionary<string, string> itemMapping = new Dictionary<string, string>();
			string[] value = remapping.Value;
			for (int i = 0; i < value.Length; i++)
			{
				string[] cmdSplit = value[i].Split(" ");
				string command = cmdSplit[0];
				if (command.Equals("/bir"))
				{
					blockMapping.TryAdd(cmdSplit[3], cmdSplit[2]);
				}
				else if (command.Equals("/iir"))
				{
					itemMapping.TryAdd(cmdSplit[3], cmdSplit[2]);
				}
			}
			BlockSchematic.BlockRemaps.TryAdd(remapping.Key, blockMapping);
			BlockSchematic.ItemRemaps.TryAdd(remapping.Key, itemMapping);
		}
	}
}
