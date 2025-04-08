using System;

namespace Vintagestory.API.Common;

public class UnparsedArg : ArgumentParserBase
{
	private string[] validRange;

	public UnparsedArg(string argName, params string[] validRange)
		: base(argName, isMandatoryArg: false)
	{
		this.validRange = validRange;
		argCount = -1;
	}

	public override object GetValue()
	{
		return null;
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return validRange;
	}

	public override void SetValue(object data)
	{
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		return EnumParseResult.Good;
	}
}
