using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class DirectionArgParser<T> : ArgumentParserBase where T : IVec3, new()
{
	private IVec3 value;

	public DirectionArgParser(string argName, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
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
		base.PreProcess(args);
		value = null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		return EnumParseResult.Good;
	}

	public override void SetValue(object data)
	{
		value = (Vec3d)data;
	}
}
