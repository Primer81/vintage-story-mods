using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ChatCommandSyntax : IChatCommand
{
	[ProtoMember(7)]
	public string FullSyntax;

	[ProtoMember(1)]
	public string FullName { get; set; }

	[ProtoMember(2)]
	public string Name { get; set; }

	[ProtoMember(3)]
	public string Description { get; set; }

	[ProtoMember(4)]
	public string AdditionalInformation { get; set; }

	[ProtoMember(5)]
	public string[] Examples { get; set; }

	[ProtoMember(6)]
	public string CallSyntax { get; set; }

	[ProtoMember(8)]
	public string CallSyntaxUnformatted { get; set; }

	[ProtoMember(9)]
	public string FullnameAlias { get; set; }

	[ProtoMember(10)]
	public List<string> Aliases { get; set; }

	[ProtoMember(11)]
	public List<string> RootAliases { get; set; }

	public string CommandPrefix
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IChatCommand this[string name]
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public bool Incomplete
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public IEnumerable<IChatCommand> Subcommands
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public Dictionary<string, IChatCommand> AllSubcommands
	{
		get
		{
			throw new NotImplementedException();
		}
	}

	public string GetFullName(string alias, bool isRootAlias)
	{
		return FullnameAlias;
	}

	public override string ToString()
	{
		return CallSyntaxUnformatted;
	}

	public string GetCallSyntax(string alias, bool isRootAlias = false)
	{
		return CallSyntax;
	}

	public string GetCallSyntaxUnformatted(string alias, bool isRootAlias = false)
	{
		return CallSyntaxUnformatted;
	}

	public void AddParameterSyntax(StringBuilder sb, string indent)
	{
		throw new NotImplementedException();
	}

	public void AddSyntaxExplanation(StringBuilder sb, string indent)
	{
		throw new NotImplementedException();
	}

	public IChatCommand BeginSubCommand(string name)
	{
		throw new NotImplementedException();
	}

	public IChatCommand BeginSubCommands(params string[] name)
	{
		throw new NotImplementedException();
	}

	public IChatCommand EndSubCommand()
	{
		throw new NotImplementedException();
	}

	public void Execute(TextCommandCallingArgs callargs, Action<TextCommandResult> onCommandComplete = null)
	{
		throw new NotImplementedException();
	}

	public string GetFullSyntaxConsole(Caller caller)
	{
		return FullSyntax;
	}

	public IChatCommand HandleWith(OnCommandDelegate handler)
	{
		throw new NotImplementedException();
	}

	public IChatCommand IgnoreAdditionalArgs()
	{
		throw new NotImplementedException();
	}

	public bool IsAvailableTo(Caller caller)
	{
		throw new NotImplementedException();
	}

	public IChatCommand RequiresPlayer()
	{
		throw new NotImplementedException();
	}

	public IChatCommand RequiresPrivilege(string privilege)
	{
		throw new NotImplementedException();
	}

	public void Validate()
	{
		throw new NotImplementedException();
	}

	public IChatCommand WithAdditionalInformation(string detail)
	{
		throw new NotImplementedException();
	}

	public IChatCommand WithAlias(params string[] name)
	{
		throw new NotImplementedException();
	}

	public IChatCommand WithArgs(params ICommandArgumentParser[] args)
	{
		throw new NotImplementedException();
	}

	public IChatCommand WithDescription(string description)
	{
		throw new NotImplementedException();
	}

	public IChatCommand WithExamples(params string[] examaples)
	{
		throw new NotImplementedException();
	}

	public IChatCommand WithName(string name)
	{
		throw new NotImplementedException();
	}

	public IChatCommand WithPreCondition(CommandPreconditionDelegate p)
	{
		throw new NotImplementedException();
	}

	public IChatCommand WithRootAlias(string name)
	{
		throw new NotImplementedException();
	}

	public string GetFullSyntaxHandbook(Caller caller, string indent = "", bool isRootAlias = false)
	{
		return FullSyntax;
	}
}
