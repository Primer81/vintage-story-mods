using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Vintagestory.API.Common;

public interface IChatCommandApi : IEnumerable<KeyValuePair<string, IChatCommand>>, IEnumerable
{
	IChatCommand this[string name] { get; }

	CommandArgumentParsers Parsers { get; }

	IChatCommand Create();

	IChatCommand Create(string name);

	IChatCommand Get(string name);

	IChatCommand GetOrCreate(string name);

	/// <summary>
	/// Executes a parsed command
	/// </summary>
	/// <param name="name">Name of the command without arguments, without prefix</param>
	/// <param name="args"></param>
	/// <param name="onCommandComplete">Called when the command finished executing</param>
	/// <returns></returns>
	void Execute(string name, TextCommandCallingArgs args, Action<TextCommandResult> onCommandComplete = null);

	/// <summary>
	/// Executes a raw command 
	/// </summary>
	/// <param name="message">Full command line, e.g. /entity spawn chicken-hen 1</param>
	/// <param name="args"></param>
	/// <param name="onCommandComplete">Called when the command finished executing</param>
	/// <returns></returns>
	void ExecuteUnparsed(string message, TextCommandCallingArgs args, Action<TextCommandResult> onCommandComplete = null);

	/// <summary>
	/// Get all commands ordered by name ASC
	/// </summary>
	/// <param name="command"></param>
	/// <returns></returns>
	static Dictionary<string, IChatCommand> GetOrdered(Dictionary<string, IChatCommand> command)
	{
		return command.OrderBy((KeyValuePair<string, IChatCommand> s) => s.Key).ToDictionary((KeyValuePair<string, IChatCommand> i) => i.Key, (KeyValuePair<string, IChatCommand> i) => i.Value);
	}

	/// <summary>
	/// Get all commands from <see cref="T:Vintagestory.API.Common.IChatCommandApi" /> ordered by name ASC
	/// </summary>
	/// <param name="chatCommandApi"></param>
	/// <returns></returns>
	static Dictionary<string, IChatCommand> GetOrdered(IChatCommandApi chatCommandApi)
	{
		return chatCommandApi.OrderBy((KeyValuePair<string, IChatCommand> s) => s.Key).ToDictionary((KeyValuePair<string, IChatCommand> i) => i.Key, (KeyValuePair<string, IChatCommand> i) => i.Value);
	}
}
