using System;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class WordRangeArgParser : ArgumentParserBase
{
	private string[] words;

	protected string word;

	public WordRangeArgParser(string argName, bool isMandatoryArg, params string[] words)
		: base(argName, isMandatoryArg)
	{
		this.words = words;
	}

	public override string GetSyntax()
	{
		string options = string.Join("/", words);
		if (!isMandatoryArg)
		{
			return "<i>[" + options + "]</i>";
		}
		return "<i>&lt;" + options + "&gt;</i>";
	}

	public override string GetSyntaxUnformatted()
	{
		string options = string.Join("/", words);
		if (!isMandatoryArg)
		{
			return "[" + options + "]";
		}
		return "&lt;" + options + "&gt;";
	}

	public override string GetSyntaxExplanation(string indent)
	{
		return indent + GetSyntax() + (isMandatoryArg ? " is the " : " is (optionally) the ") + argName;
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return words;
	}

	public override object GetValue()
	{
		return word;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		base.PreProcess(args);
		word = null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		word = args.RawArgs.PopWord();
		if (!words.Contains(word))
		{
			word = null;
			lastErrorMessage = Lang.Get("Invalid word, not in word range") + " [" + string.Join(", ", words) + "]";
			return EnumParseResult.Bad;
		}
		return EnumParseResult.Good;
	}

	public override void SetValue(object data)
	{
		word = (string)data;
	}
}
