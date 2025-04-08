using System;
using Vintagestory.API.Config;

namespace Vintagestory.API.Common;

public class LongArgParser : ArgumentParserBase
{
	private long min;

	private long max;

	private long value;

	private long defaultValue;

	public LongArgParser(string argName, long min, long max, long defaultValue, bool isMandatoryArg)
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

	public LongArgParser(string argName, long defaultValue, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.defaultValue = defaultValue;
		min = long.MinValue;
		max = long.MaxValue;
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return new string[2]
		{
			long.MinValue.ToString() ?? "",
			long.MaxValue.ToString() ?? ""
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
		long? val = args.RawArgs.PopLong();
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
		value = (long)data;
	}
}
