using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

internal class CmdWorldConfig
{
	private ServerMain server;

	public CmdWorldConfig(ServerMain server)
	{
		this.server = server;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.ChatCommands.Create("worldconfig").WithAlias("wc").RequiresPrivilege(Privilege.controlserver)
			.WithDescription("Modify the world config")
			.WithArgs(parsers.OptionalWord("key"), parsers.OptionalAll("value"))
			.HandleWith(handle);
	}

	private TextCommandResult handle(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success($"Specify one of the following world configuration settings: {ListConfigs()}");
		}
		string configname = (string)args[0];
		if (configname == "worldWidth" || configname == "worldLength")
		{
			return TextCommandResult.Error($"Changing world size is not supported");
		}
		string currentValue = "";
		bool exists = false;
		WorldConfigurationAttribute attr = null;
		double result2;
		foreach (Mod mod in server.api.modLoader.Mods)
		{
			ModWorldConfiguration config = mod.WorldConfig;
			if (config == null)
			{
				continue;
			}
			if (exists)
			{
				break;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = config.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute attribute in worldConfigAttributes)
			{
				if (!attribute.Code.Equals(configname, StringComparison.InvariantCultureIgnoreCase))
				{
					continue;
				}
				configname = attribute.Code;
				attr = attribute;
				currentValue = "(default:) " + attribute.TypedDefault.ToString();
				if (server.SaveGameData.WorldConfiguration.HasAttribute(configname))
				{
					switch (attr.DataType)
					{
					case EnumDataType.Bool:
						currentValue = server.SaveGameData.WorldConfiguration.GetBool(configname).ToString() ?? "";
						break;
					case EnumDataType.DoubleInput:
						result2 = server.SaveGameData.WorldConfiguration.GetDecimal(configname);
						currentValue = result2.ToString() ?? "";
						break;
					case EnumDataType.String:
					case EnumDataType.DropDown:
						currentValue = server.SaveGameData.WorldConfiguration.GetAsString(configname) ?? "";
						break;
					case EnumDataType.IntInput:
					case EnumDataType.IntRange:
						currentValue = server.SaveGameData.WorldConfiguration.GetInt(configname).ToString() ?? "";
						break;
					}
				}
				exists = true;
				break;
			}
		}
		if (!exists)
		{
			if (args.Parsers[1].IsMissing && server.SaveGameData.WorldConfiguration.HasAttribute(configname))
			{
				return TextCommandResult.Success($"{configname} currently has value: {server.SaveGameData.WorldConfiguration[configname]}");
			}
			return TextCommandResult.Error($"No such config found: {configname}");
		}
		if (args.Parsers[1].IsMissing)
		{
			return TextCommandResult.Success($"{configname} currently has value: {currentValue}");
		}
		string newvalue = (string)args[1];
		string result = null;
		switch (attr.DataType)
		{
		case EnumDataType.Bool:
		{
			bool val3 = newvalue.ToBool();
			server.SaveGameData.WorldConfiguration.SetBool(configname, val3);
			result = $"Ok, value {val3} set. Restart game world or server to apply changes.";
			break;
		}
		case EnumDataType.DoubleInput:
		{
			double val2 = newvalue.ToDouble();
			server.SaveGameData.WorldConfiguration.SetDouble(configname, val2);
			result = $"Ok, value {val2} set. Restart game world or server to apply changes.";
			break;
		}
		case EnumDataType.String:
		case EnumDataType.DropDown:
			server.SaveGameData.WorldConfiguration.SetString(configname, newvalue);
			result = $"Ok, value {newvalue} set. Restart game world or server to apply changes.";
			break;
		case EnumDataType.IntInput:
		case EnumDataType.IntRange:
		{
			int val = newvalue.ToInt();
			server.SaveGameData.WorldConfiguration.SetInt(configname, val);
			result = $"Ok, value {val} set. Restart game world or server to apply changes.";
			break;
		}
		default:
			return TextCommandResult.Error($"Unknown attr datatype.");
		}
		if (attr.Values != null && !attr.Values.Any((string value) => !double.TryParse(value, out var _)) && !double.TryParse(newvalue, out result2))
		{
			result = result + "\n" + $"Values for this config are usually decimals, {newvalue} is not a decimal. Config might not apply correctly.";
		}
		return TextCommandResult.Success(result);
	}

	private string ListConfigs()
	{
		StringBuilder sb = new StringBuilder();
		foreach (Mod mod in server.api.modLoader.Mods)
		{
			ModWorldConfiguration config = mod.WorldConfig;
			if (config == null)
			{
				continue;
			}
			WorldConfigurationAttribute[] worldConfigAttributes = config.WorldConfigAttributes;
			foreach (WorldConfigurationAttribute attribute in worldConfigAttributes)
			{
				if (sb.Length != 0)
				{
					sb.Append(", ");
				}
				sb.Append(attribute.Code);
			}
		}
		return sb.ToString();
	}
}
