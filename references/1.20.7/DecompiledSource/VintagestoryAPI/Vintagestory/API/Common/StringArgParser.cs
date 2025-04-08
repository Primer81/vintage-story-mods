using System;

namespace Vintagestory.API.Common;

public class StringArgParser : ArgumentParserBase
{
	private string value;

	public StringArgParser(string argName, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		argCount = -1;
	}

	public override object GetValue()
	{
		if (!isMandatoryArg && base.IsMissing)
		{
			return null;
		}
		return value;
	}

	public override void SetValue(object data)
	{
		value = (string)data;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		base.PreProcess(args);
		value = null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		value = args.RawArgs.PopAll();
		return EnumParseResult.Good;
	}
}
