using System;
using System.Collections.Generic;
using Vintagestory.API.Config;

namespace Vintagestory.API.Common;

public class DatetimeArgParser : ArgumentParserBase
{
	private DateTime datetime;

	private List<string> timeUnits = new List<string> { "minute", "hour", "day", "week", "year" };

	public DatetimeArgParser(string argName, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
	}

	public override string GetSyntaxExplanation(string indent)
	{
		return indent + GetSyntax() + " is a number and time, for example 1 day, where time can be any of [" + string.Join(",", timeUnits) + "]";
	}

	public override object GetValue()
	{
		return datetime;
	}

	public override void SetValue(object data)
	{
		datetime = (DateTime)data;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		base.PreProcess(args);
		datetime = default(DateTime);
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		int quantity = args.RawArgs.PopInt(0).Value;
		string timeunit = args.RawArgs.PopWord();
		if (quantity <= 0)
		{
			lastErrorMessage = Lang.Get("cmdban-invalidtimespan");
			return EnumParseResult.Bad;
		}
		switch (timeunit)
		{
		case "minute":
			datetime = DateTime.Now.AddMinutes(quantity);
			break;
		case "hour":
			datetime = DateTime.Now.AddHours(quantity);
			break;
		case "day":
			datetime = DateTime.Now.AddDays(quantity);
			break;
		case "week":
			datetime = DateTime.Now.AddDays(quantity * 7);
			break;
		case "year":
			datetime = DateTime.Now.AddYears(quantity);
			break;
		default:
			lastErrorMessage = Lang.Get("cmdban-invalidtimeunit", string.Join(", ", timeUnits));
			return EnumParseResult.Bad;
		}
		return EnumParseResult.Good;
	}
}
