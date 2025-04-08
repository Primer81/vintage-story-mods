using System;
using System.Drawing;
using System.Globalization;
using Vintagestory.API.Config;

namespace Vintagestory.API.Common;

public class ColorArgParser : ArgumentParserBase
{
	private Color _value;

	public ColorArgParser(string argName, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
	}

	public override string GetSyntaxExplanation(string indent)
	{
		return indent + GetSyntax() + " can be either a color string like (red, blue, green,.. <a href=\"https://learn.microsoft.com/en-us/dotnet/api/system.drawing.knowncolor?view=net-7.0\">See full list</a>) or a hex value like #F9D0DC";
	}

	public override object GetValue()
	{
		return _value;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		base.PreProcess(args);
		_value = default(Color);
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		string colorString = args.RawArgs.PopWord();
		if (colorString == null)
		{
			lastErrorMessage = Lang.Get("Not a color");
			return EnumParseResult.Bad;
		}
		if (colorString.StartsWith('#'))
		{
			try
			{
				int argb = int.Parse(colorString.Replace("#", ""), NumberStyles.HexNumber);
				_value = Color.FromArgb(argb);
			}
			catch (FormatException)
			{
				lastErrorMessage = Lang.Get("command-waypoint-invalidcolor");
				return EnumParseResult.Bad;
			}
		}
		else
		{
			_value = Color.FromName(colorString);
		}
		return EnumParseResult.Good;
	}

	public override void SetValue(object data)
	{
		_value = (Color)data;
	}
}
