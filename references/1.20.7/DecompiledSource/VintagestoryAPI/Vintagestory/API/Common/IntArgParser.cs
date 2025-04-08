using System;
using Vintagestory.API.Config;

namespace Vintagestory.API.Common;

public class IntArgParser : ArgumentParserBase
{
	private int min;

	private int max;

	private int value;

	private int defaultValue;

	public IntArgParser(string argName, int min, int max, int defaultValue, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.defaultValue = defaultValue;
		this.min = min;
		this.max = max;
	}

	public override string GetSyntaxExplanation(string indent)
	{
		return indent + GetSyntax() + " is an integer number";
	}

	public IntArgParser(string argName, int defaultValue, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.defaultValue = defaultValue;
		min = int.MinValue;
		max = int.MaxValue;
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return new string[2]
		{
			int.MinValue.ToString() ?? "",
			int.MaxValue.ToString() ?? ""
		};
	}

	public override object GetValue()
	{
		return value;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		value = defaultValue;
		base.PreProcess(args);
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		int? val = args.RawArgs.PopInt();
		if (!val.HasValue)
		{
			lastErrorMessage = Lang.Get("Not a number");
			return EnumParseResult.Bad;
		}
		if (val < min || val > max)
		{
			lastErrorMessage = Lang.Get("Number out of range");
			return EnumParseResult.Bad;
		}
		value = val.Value;
		return EnumParseResult.Good;
	}

	public override void SetValue(object data)
	{
		value = (int)data;
	}
}
