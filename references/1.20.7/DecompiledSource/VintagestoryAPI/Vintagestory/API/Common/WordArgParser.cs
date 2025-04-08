using System;

namespace Vintagestory.API.Common;

public class WordArgParser : ArgumentParserBase
{
	private string word;

	private string[] suggestions;

	public WordArgParser(string argName, bool isMandatoryArg, string[] suggestions = null)
		: base(argName, isMandatoryArg)
	{
		this.suggestions = suggestions;
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return suggestions;
	}

	public override object GetValue()
	{
		if (!isMandatoryArg && base.IsMissing)
		{
			return null;
		}
		return word;
	}

	public override void SetValue(object data)
	{
		word = (string)data;
	}

	public override string GetSyntaxExplanation(string indent)
	{
		if (suggestions != null)
		{
			return indent + GetSyntax() + " here are some suggestions: " + string.Join(", ", suggestions);
		}
		return indent + GetSyntax() + " is a string without spaces";
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		base.PreProcess(args);
		word = null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		word = args.RawArgs.PopWord();
		if (word == null)
		{
			lastErrorMessage = "Argument is missing";
			return EnumParseResult.Bad;
		}
		return EnumParseResult.Good;
	}
}
