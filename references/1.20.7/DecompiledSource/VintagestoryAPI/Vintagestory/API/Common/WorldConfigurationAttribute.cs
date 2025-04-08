using System.Globalization;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class WorldConfigurationAttribute
{
	public EnumDataType DataType;

	public string Category;

	public string Code;

	public double Min;

	public double Max;

	public double Step;

	public bool OnCustomizeScreen = true;

	public string Default;

	public string[] Values;

	public string[] Names;

	public bool OnlyDuringWorldCreate;

	public object TypedDefault => stringToValue(Default);

	public object stringToValue(string text)
	{
		switch (DataType)
		{
		case EnumDataType.Bool:
		{
			bool.TryParse(text, out var on);
			return on;
		}
		case EnumDataType.DoubleInput:
		{
			float.TryParse(text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out var fval);
			return fval;
		}
		case EnumDataType.DropDown:
			return text;
		case EnumDataType.IntInput:
		case EnumDataType.IntRange:
		{
			int.TryParse(text, out var val);
			return val;
		}
		case EnumDataType.String:
			return text;
		default:
			return null;
		}
	}

	public string valueToHumanReadable(string value)
	{
		switch (DataType)
		{
		case EnumDataType.Bool:
			if (!(value.ToLowerInvariant() == "true"))
			{
				return Lang.Get("Off");
			}
			return Lang.Get("On");
		case EnumDataType.DoubleInput:
			return value ?? "";
		case EnumDataType.DropDown:
		{
			int index = Values.IndexOf(value);
			string text;
			if (index < 0)
			{
				text = value;
				if (text == null)
				{
					return "";
				}
			}
			else
			{
				text = Lang.Get("worldconfig-" + Code + "-" + Names[index]);
			}
			return text;
		}
		case EnumDataType.IntInput:
		case EnumDataType.IntRange:
			return value ?? "";
		case EnumDataType.String:
			return value ?? "";
		default:
			return null;
		}
	}
}
