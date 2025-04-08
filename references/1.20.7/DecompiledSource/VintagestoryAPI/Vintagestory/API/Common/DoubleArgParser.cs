using System;
using Vintagestory.API.Config;

namespace Vintagestory.API.Common;

public class DoubleArgParser : ArgumentParserBase
{
	private double min;

	private double max;

	private double value;

	private double defaultvalue;

	public DoubleArgParser(string argName, double min, double max, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.min = min;
		this.max = max;
	}

	public override string GetSyntaxExplanation(string indent)
	{
		return indent + Lang.Get("{0} is a decimal number, for example 0.5", GetSyntax());
	}

	public DoubleArgParser(string argName, double defaultvalue, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.defaultvalue = defaultvalue;
		min = double.MinValue;
		max = double.MaxValue;
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return new string[2]
		{
			double.MinValue.ToString() ?? "",
			double.MaxValue.ToString() ?? ""
		};
	}

	public override object GetValue()
	{
		return value;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		value = defaultvalue;
		base.PreProcess(args);
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		if (args.RawArgs.Length == 0)
		{
			lastErrorMessage = Lang.Get("Missing");
			return EnumParseResult.Bad;
		}
		double? val = args.RawArgs.PopDouble();
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
		value = (double)data;
	}
}
