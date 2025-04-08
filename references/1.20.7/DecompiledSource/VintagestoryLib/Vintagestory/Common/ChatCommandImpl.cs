using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.Common;

public class ChatCommandImpl : IChatCommand
{
	private ChatCommandApi _cmdapi;

	private ChatCommandImpl _parent;

	protected bool ignoreAdditonalArguments;

	protected string name;

	protected string[] examples;

	protected List<string> aliases;

	protected List<string> rootAliases;

	protected string privilege;

	protected string description;

	protected string additionalInformation;

	protected OnCommandDelegate handler;

	protected Dictionary<string, IChatCommand> subCommands = new Dictionary<string, IChatCommand>(StringComparer.OrdinalIgnoreCase);

	private ICommandArgumentParser[] _parsers = new ICommandArgumentParser[0];

	public bool Incomplete
	{
		get
		{
			if (name != null)
			{
				return GetPrivilege() == null;
			}
			return true;
		}
	}

	public List<string> Aliases => aliases;

	public List<string> RootAliases => rootAliases;

	public string CommandPrefix => _cmdapi.CommandPrefix;

	public string Name => name;

	public string Description => description;

	public string AdditionalInformation => additionalInformation;

	public string[] Examples => examples;

	public string FullName
	{
		get
		{
			if (_parent != null)
			{
				return _parent.name + " " + name;
			}
			return _cmdapi.CommandPrefix + name;
		}
	}

	public IChatCommand this[string name] => subCommands[name];

	public bool AnyPrivilegeSet => !string.IsNullOrEmpty(GetPrivilege());

	public string CallSyntax
	{
		get
		{
			StringBuilder sb = new StringBuilder();
			if (_parent == null)
			{
				sb.Append(_cmdapi.CommandPrefix);
			}
			else
			{
				sb.Append(_parent.CallSyntax);
			}
			sb.Append(Name);
			sb.Append(" ");
			AddParameterSyntax(sb, "");
			return sb.ToString();
		}
	}

	public string CallSyntaxUnformatted
	{
		get
		{
			StringBuilder sb = new StringBuilder();
			if (_parent == null)
			{
				sb.Append(_cmdapi.CommandPrefix);
			}
			else
			{
				sb.Append(_parent.CallSyntaxUnformatted);
			}
			sb.Append(Name);
			sb.Append(" ");
			AddParameterSyntaxUnformatted(sb, "");
			return sb.ToString();
		}
	}

	public IEnumerable<IChatCommand> Subcommands => subCommands.Values;

	public Dictionary<string, IChatCommand> AllSubcommands => subCommands;

	private event CommandPreconditionDelegate _precond;

	public string GetPrivilege()
	{
		string text = privilege;
		if (text == null)
		{
			ChatCommandImpl parent = _parent;
			if (parent == null)
			{
				return null;
			}
			text = parent.GetPrivilege();
		}
		return text;
	}

	public string GetFullName(string alias, bool isRootAlias = false)
	{
		if (_parent == null || isRootAlias)
		{
			return _cmdapi.CommandPrefix + alias;
		}
		if (alias != name)
		{
			return _cmdapi.CommandPrefix + alias;
		}
		return _parent.name + " " + alias;
	}

	public ChatCommandImpl(ChatCommandApi cmdapi, string name = null, ChatCommandImpl parent = null)
	{
		_cmdapi = cmdapi;
		this.name = name;
		_parent = parent;
	}

	public IChatCommand EndSubCommand()
	{
		if (_parent == null)
		{
			throw new InvalidOperationException("Not inside a subcommand");
		}
		return _parent;
	}

	public IChatCommand HandleWith(OnCommandDelegate handler)
	{
		this.handler = handler;
		return this;
	}

	public IChatCommand RequiresPrivilege(string privilege)
	{
		this.privilege = privilege;
		return this;
	}

	public IChatCommand WithDescription(string description)
	{
		this.description = description;
		return this;
	}

	public IChatCommand WithAdditionalInformation(string text)
	{
		additionalInformation = text;
		return this;
	}

	public IChatCommand WithName(string commandName)
	{
		if (_parent != null)
		{
			throw new InvalidOperationException("This method is not available for subcommands");
		}
		if (_cmdapi.ichatCommands.ContainsKey(commandName))
		{
			throw new InvalidOperationException("Command with such name already exists");
		}
		name = commandName;
		_cmdapi.ichatCommands[commandName] = this;
		return this;
	}

	public IChatCommand WithRootAlias(string commandName)
	{
		string lowerInvariant = commandName.ToLowerInvariant();
		if (rootAliases == null)
		{
			rootAliases = new List<string>();
		}
		rootAliases.Add(lowerInvariant);
		return _cmdapi.ichatCommands[lowerInvariant] = this;
	}

	public IChatCommand BeginSub(string name)
	{
		return BeginSubCommand(name);
	}

	public IChatCommand EndSub()
	{
		return EndSubCommand();
	}

	public IChatCommand BeginSubCommand(string name)
	{
		name = name.ToLowerInvariant();
		if (subCommands.TryGetValue(name, out var command))
		{
			return command;
		}
		return subCommands[name] = new ChatCommandImpl(_cmdapi, name, this);
	}

	public IChatCommand BeginSubCommands(params string[] names)
	{
		names[0] = names[0].ToLowerInvariant();
		IChatCommand chatCommand2;
		if (!subCommands.ContainsKey(names[0]))
		{
			IChatCommand chatCommand = new ChatCommandImpl(_cmdapi, names[0], this);
			chatCommand2 = chatCommand;
		}
		else
		{
			chatCommand2 = subCommands[names[0]];
		}
		ChatCommandImpl cmd = chatCommand2 as ChatCommandImpl;
		ChatCommandImpl chatCommandImpl = cmd;
		if (chatCommandImpl.aliases == null)
		{
			chatCommandImpl.aliases = new List<string>();
		}
		string[] array = names;
		foreach (string name2 in array)
		{
			subCommands[name2.ToLowerInvariant()] = cmd;
		}
		array = names[1..];
		foreach (string name in array)
		{
			cmd.Aliases.Add(name.ToLowerInvariant());
		}
		return cmd;
	}

	public IChatCommand WithSubCommand(string name, string desc, OnCommandDelegate handler, params ICommandArgumentParser[] parsers)
	{
		BeginSubCommand(name).WithName(name).WithDescription(desc).WithArgs(parsers)
			.HandleWith(handler)
			.EndSubCommand();
		return this;
	}

	public IChatCommand WithArgs(params ICommandArgumentParser[] parsers)
	{
		_parsers = parsers;
		return this;
	}

	public void Execute(TextCommandCallingArgs callargs, Action<TextCommandResult> onCommandComplete = null)
	{
		callargs.Command = this;
		if (this._precond != null)
		{
			Delegate[] invocationList = this._precond.GetInvocationList();
			for (int j = 0; j < invocationList.Length; j++)
			{
				TextCommandResult res = ((CommandPreconditionDelegate)invocationList[j])(callargs);
				if (res.Status == EnumCommandStatus.Error)
				{
					if (onCommandComplete != null)
					{
						onCommandComplete(res);
					}
					return;
				}
			}
		}
		Dictionary<int, AsyncParseResults> asyncParseResults = null;
		int deferredCount = 0;
		bool allParsed = false;
		ICommandArgumentParser parserResultDependedOnSubsequent = null;
		for (int i = 0; i < _parsers.Length; i++)
		{
			int index = i;
			ICommandArgumentParser parser = _parsers[i];
			parser.PreProcess(callargs);
			if (parser.IsMissing)
			{
				if (parser.IsMandatoryArg)
				{
					onCommandComplete?.Invoke(TextCommandResult.Error(Lang.Get("command-missingarg", i + 1, Lang.Get(parser.ArgumentName)), "missingarg"));
					return;
				}
				continue;
			}
			EnumParseResult status = parser.TryProcess(callargs, delegate(AsyncParseResults data)
			{
				int num = deferredCount;
				deferredCount = num - 1;
				if (asyncParseResults == null)
				{
					asyncParseResults = new Dictionary<int, AsyncParseResults>();
				}
				asyncParseResults[index] = data;
				if (deferredCount == 0 && allParsed)
				{
					CallHandler(callargs, onCommandComplete, asyncParseResults);
				}
			});
			if (status == EnumParseResult.Good)
			{
				continue;
			}
			if (status == EnumParseResult.Deferred)
			{
				int j = deferredCount;
				deferredCount = j + 1;
			}
			if (parserResultDependedOnSubsequent != null)
			{
				onCommandComplete?.Invoke(TextCommandResult.Error(Lang.Get("command-argumenterror1", parserResultDependedOnSubsequent.ArgumentName, Lang.Get(parserResultDependedOnSubsequent.LastErrorMessage ?? "unknown error")), "wrongarg"));
			}
			switch (status)
			{
			case EnumParseResult.Bad:
				onCommandComplete?.Invoke(TextCommandResult.Error(Lang.Get("command-argumenterror2", i + 1, parser.ArgumentName, Lang.Get(parser.LastErrorMessage ?? "unknown error")), "wrongarg"));
				return;
			case EnumParseResult.DependsOnSubsequent:
				if (parserResultDependedOnSubsequent != null)
				{
					return;
				}
				parserResultDependedOnSubsequent = parser;
				break;
			}
		}
		callargs.Parsers.AddRange(_parsers);
		allParsed = true;
		if (deferredCount == 0)
		{
			CallHandler(callargs, onCommandComplete, asyncParseResults);
		}
		else
		{
			onCommandComplete?.Invoke(TextCommandResult.Deferred);
		}
	}

	private void CallHandler(TextCommandCallingArgs callargs, Action<TextCommandResult> onCommandComplete = null, Dictionary<int, AsyncParseResults> asyncParseResults = null)
	{
		if (asyncParseResults != null)
		{
			foreach (KeyValuePair<int, AsyncParseResults> val2 in asyncParseResults)
			{
				int index = val2.Key;
				if (val2.Value.Status == EnumParseResultStatus.Error)
				{
					onCommandComplete?.Invoke(TextCommandResult.Error(Lang.Get("Error in argument {0} ({1}): {2}", index + 1, Lang.Get(_parsers[index].ArgumentName), Lang.Get(_parsers[index].LastErrorMessage)), "wrongarg"));
					return;
				}
				callargs.Parsers[index].SetValue(val2.Value.Data);
			}
		}
		string subcmd = callargs.RawArgs.PeekWord()?.ToLowerInvariant();
		if (subcmd != null && subCommands.ContainsKey(subcmd))
		{
			callargs.SubCmdCode = callargs.RawArgs.PopWord();
			subCommands[subcmd].Execute(callargs, onCommandComplete);
		}
		else if (!callargs.Caller.HasPrivilege(GetPrivilege()))
		{
			onCommandComplete?.Invoke(new TextCommandResult
			{
				Status = EnumCommandStatus.Error,
				ErrorCode = "noprivilege",
				StatusMessage = Lang.Get("Sorry, you don't have the privilege to use this command")
			});
		}
		else if (handler == null)
		{
			if (subCommands.Count > 0)
			{
				List<string> subcchat = new List<string>();
				foreach (string val in subCommands.Keys)
				{
					subcchat.Add(string.Format("<a href=\"chattype://{0}\">{1}</a>", callargs.Command.FullName + " " + val, val));
				}
				onCommandComplete?.Invoke(TextCommandResult.Error("Choose a subcommand: " + string.Join(", ", subcchat), "selectsubcommand"));
			}
			else
			{
				onCommandComplete?.Invoke(TextCommandResult.Error("Insufficently set up command - no handlers or subcommands set up", "incompletecommandsetup"));
			}
		}
		else if (callargs.RawArgs.Length > 0 && callargs.ArgCount >= 0 && !ignoreAdditonalArguments)
		{
			if (_parent == null)
			{
				if (subCommands.Count > 0)
				{
					onCommandComplete?.Invoke(TextCommandResult.Error(Lang.Get("Command {0}, unrecognised subcommand: {1}", _cmdapi.CommandPrefix + name, subcmd), "wrongargcount"));
				}
				else
				{
					onCommandComplete?.Invoke(TextCommandResult.Error(Lang.Get("Command {0}, too many arguments", _cmdapi.CommandPrefix + name), "wrongargcount"));
				}
			}
			else
			{
				onCommandComplete?.Invoke(TextCommandResult.Error(Lang.Get("Subcommand {0}, too many arguments", name), "wrongargcount"));
			}
		}
		else
		{
			TextCommandResult results = handler(callargs);
			onCommandComplete?.Invoke(results);
		}
	}

	public IChatCommand WithPreCondition(CommandPreconditionDelegate precond)
	{
		_precond += precond;
		return this;
	}

	public IChatCommand WithAlias(params string[] names)
	{
		if (aliases == null)
		{
			aliases = new List<string>();
		}
		for (int i = 0; i < names.Length; i++)
		{
			string lowerInvariant = names[i].ToLowerInvariant();
			if (_parent == null)
			{
				_cmdapi.ichatCommands[lowerInvariant] = this;
			}
			else
			{
				_parent.subCommands[lowerInvariant] = this;
			}
			aliases.Add(lowerInvariant);
		}
		return this;
	}

	public IChatCommand GroupWith(params string[] name)
	{
		WithAlias(name);
		return this;
	}

	public IChatCommand WithExamples(params string[] examples)
	{
		this.examples = examples;
		return this;
	}

	public IChatCommand RequiresPlayer()
	{
		_precond += (TextCommandCallingArgs args) => (args.Caller.Player == null) ? TextCommandResult.Error("Caller must be player") : TextCommandResult.Success();
		return this;
	}

	public void Validate()
	{
		if (_parent != null)
		{
			throw new Exception("Validate not called from the root command, likely missing EndSub()");
		}
		ValidateRecursive();
	}

	private void ValidateRecursive()
	{
		if (string.IsNullOrEmpty(description))
		{
			throw new Exception("Command " + CallSyntax + ": Description not set");
		}
		if (string.IsNullOrEmpty(name))
		{
			throw new Exception("Command " + CallSyntax + ": Name not set");
		}
		if (!AnyPrivilegeSet)
		{
			throw new Exception("Command " + CallSyntax + ": Privilege not set for subcommand or any parent command");
		}
		if (subCommands.Count == 0 && handler == null)
		{
			throw new Exception("Command " + CallSyntax + ": No handler or subcommands defined");
		}
		foreach (KeyValuePair<string, IChatCommand> subCommand in subCommands)
		{
			(subCommand.Value as ChatCommandImpl).ValidateRecursive();
		}
	}

	public bool IsAvailableTo(Caller caller)
	{
		return caller.HasPrivilege(GetPrivilege());
	}

	public IChatCommand IgnoreAdditionalArgs()
	{
		ignoreAdditonalArguments = true;
		return this;
	}

	public string GetFullSyntaxHandbook(Caller caller, string indent = "", bool isRootAlias = false)
	{
		StringBuilder text = new StringBuilder();
		Dictionary<string, IChatCommand> subcommands = IChatCommandApi.GetOrdered(AllSubcommands);
		if (handler != null && (isRootAlias || _parent == null))
		{
			if (RootAliases != null)
			{
				foreach (string alias in RootAliases)
				{
					text.AppendLine(indent + $"<a href=\"chattype://{GetCallSyntaxUnformatted(alias, isRootAlias: true)}\">{GetCallSyntax(alias, isRootAlias: true)}</a>");
				}
			}
			text.AppendLine(indent + $"<a href=\"chattype://{CallSyntaxUnformatted}\">{CallSyntax}</a>");
		}
		if (Description != null)
		{
			AddVerticalSpace(text);
			text.AppendLine(indent + Description);
		}
		if (AdditionalInformation != null)
		{
			AddVerticalSpace(text);
			text.AppendLine(indent + AdditionalInformation);
		}
		AddSyntaxExplanation(text, indent);
		if (Examples != null && Examples.Length != 0)
		{
			AddVerticalSpace(text);
			text.AppendLine(indent + ((Examples.Length > 1) ? "Examples:" : "Example:"));
			string[] array = Examples;
			foreach (string ex in array)
			{
				text.AppendLine(indent + ex);
			}
		}
		if (subcommands.Count > 0 && !isRootAlias)
		{
			AddVerticalSpace(text);
			WriteCommandsListHandbook(text, subcommands, caller, indent);
		}
		AddVerticalSpace(text);
		return text.ToString();
	}

	public string GetFullSyntaxConsole(Caller caller)
	{
		StringBuilder text = new StringBuilder();
		Dictionary<string, IChatCommand> subcommands = AllSubcommands;
		if (subcommands.Count > 0)
		{
			text.AppendLine("Available subcommands:");
			WriteCommandsList(text, subcommands, caller, isSubCommand: true);
			text.AppendLine();
			text.AppendLine("Type <code>/help " + CallSyntax.Substring(1) + " &lt;<i>subcommand_name</i>&gt;</code> for help on a specific subcommand");
		}
		else
		{
			text.AppendLine();
			if (Description != null)
			{
				text.AppendLine(Description);
			}
			if (AdditionalInformation != null)
			{
				text.AppendLine(AdditionalInformation);
			}
			text.AppendLine();
			text.AppendLine("Usage: <code>");
			text.Append(CallSyntax);
			text.Append("</code>");
			AddSyntaxExplanation(text, "");
			if (Examples != null && Examples.Length != 0)
			{
				text.AppendLine((Examples.Length > 1) ? "Examples:" : "Example:");
				string[] array = Examples;
				foreach (string ex in array)
				{
					text.AppendLine(ex);
				}
			}
		}
		return text.ToString();
	}

	public static void WriteCommandsListHandbook(StringBuilder text, Dictionary<string, IChatCommand> commands, Caller caller, string indent = "")
	{
		text.AppendLine();
		foreach (ChatCommandImpl cm in commands.Values.Distinct(ChatCommandComparer.Comparer).Cast<ChatCommandImpl>())
		{
			if (caller != null && !cm.IsAvailableTo(caller))
			{
				continue;
			}
			if (cm.AllSubcommands.Count > 0 && cm.handler == null)
			{
				if (cm.RootAliases != null)
				{
					foreach (string alias2 in cm.RootAliases)
					{
						text.AppendLine(indent + "<strong>" + cm.GetCallSyntax(alias2, isRootAlias: true) + "</strong>");
					}
				}
				if (cm.Aliases != null)
				{
					foreach (string alias in cm.Aliases)
					{
						text.AppendLine(indent + "<strong>" + cm.GetCallSyntax(alias) + "</strong>");
					}
				}
				text.AppendLine(indent + "<strong>" + cm.CallSyntax + "</strong> ");
			}
			else
			{
				if (cm.RootAliases != null)
				{
					foreach (string alias4 in cm.RootAliases)
					{
						text.AppendLine(indent + $"<a href=\"chattype://{cm.GetCallSyntaxUnformatted(alias4, isRootAlias: true)}\">{cm.GetCallSyntax(alias4, isRootAlias: true).TrimEnd()}</a>");
					}
				}
				if (cm.Aliases != null)
				{
					foreach (string alias3 in cm.Aliases)
					{
						text.AppendLine(indent + $"<a href=\"chattype://{cm.GetCallSyntaxUnformatted(alias3)}\">{cm.GetCallSyntax(alias3).TrimEnd()}</a>");
					}
				}
				text.AppendLine(indent + $"<a href=\"chattype://{cm.CallSyntaxUnformatted}\">{cm.CallSyntax}</a>");
			}
			text.Append(cm.GetFullSyntaxHandbook(caller, indent + "   "));
		}
	}

	public static void WriteCommandsList(StringBuilder text, Dictionary<string, IChatCommand> commands, Caller caller, bool isSubCommand = false)
	{
		foreach (KeyValuePair<string, IChatCommand> val in commands)
		{
			IChatCommand cm = val.Value;
			if (caller != null && !cm.IsAvailableTo(caller))
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
			text.AppendLine("<code>" + cm.GetCallSyntax(val.Key, !isSubCommand).TrimEnd() + "</code> :  " + desc);
		}
	}

	public string GetCallSyntax(string name, bool isRootAlias = false)
	{
		StringBuilder sb = new StringBuilder();
		if (isRootAlias)
		{
			sb.Append(_cmdapi.CommandPrefix);
		}
		else
		{
			sb.Append((_parent == null) ? _cmdapi.CommandPrefix : _parent.CallSyntax);
		}
		sb.Append(name);
		sb.Append(" ");
		AddParameterSyntax(sb, "");
		return sb.ToString();
	}

	public string GetCallSyntaxUnformatted(string name, bool isRootAlias = false)
	{
		StringBuilder sb = new StringBuilder();
		if (isRootAlias)
		{
			sb.Append(_cmdapi.CommandPrefix);
		}
		else
		{
			sb.Append((_parent == null) ? _cmdapi.CommandPrefix : _parent.CallSyntaxUnformatted);
		}
		sb.Append(name);
		sb.Append(" ");
		AddParameterSyntaxUnformatted(sb, "");
		return sb.ToString();
	}

	public void AddParameterSyntax(StringBuilder sb, string indent)
	{
		ICommandArgumentParser[] parsers = _parsers;
		for (int i = 0; i < parsers.Length; i++)
		{
			ArgumentParserBase p = (ArgumentParserBase)parsers[i];
			sb.Append(p.GetSyntax());
			sb.Append(" ");
		}
	}

	public void AddParameterSyntaxUnformatted(StringBuilder sb, string indent)
	{
		ICommandArgumentParser[] parsers = _parsers;
		for (int i = 0; i < parsers.Length; i++)
		{
			ArgumentParserBase p = (ArgumentParserBase)parsers[i];
			sb.Append(p.GetSyntaxUnformatted());
			sb.Append(" ");
		}
	}

	public void AddSyntaxExplanation(StringBuilder sb, string indent)
	{
		if (_parsers.Length == 0)
		{
			return;
		}
		bool first = true;
		sb.Append("<font scale=\"80%\">");
		ICommandArgumentParser[] parsers = _parsers;
		for (int i = 0; i < parsers.Length; i++)
		{
			string explanation = ((ArgumentParserBase)parsers[i]).GetSyntaxExplanation(indent);
			if (explanation != null)
			{
				if (first)
				{
					sb.AppendLine();
					first = false;
				}
				sb.AppendLine(explanation);
			}
		}
		sb.Append("</font>");
	}

	private void AddVerticalSpace(StringBuilder text)
	{
		if (text.Length != 0)
		{
			text.Append("\n");
		}
	}
}
