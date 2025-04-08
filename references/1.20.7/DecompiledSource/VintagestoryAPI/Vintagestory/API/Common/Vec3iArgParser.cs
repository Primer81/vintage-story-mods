using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class Vec3iArgParser : ArgumentParserBase
{
	private Vec3i _vector;

	public Vec3iArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		argCount = 3;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		base.PreProcess(args);
		_vector = null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		int? x = args.RawArgs.PopInt();
		int? y = args.RawArgs.PopInt();
		int? z = args.RawArgs.PopInt();
		if (x.HasValue && y.HasValue && z.HasValue)
		{
			_vector = new Vec3i(x.Value, y.Value, z.Value);
		}
		if (!(_vector == null))
		{
			return EnumParseResult.Good;
		}
		return EnumParseResult.Bad;
	}

	public override object GetValue()
	{
		return _vector;
	}

	public override void SetValue(object data)
	{
		_vector = (Vec3i)data;
	}
}
