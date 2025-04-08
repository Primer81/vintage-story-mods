using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.Server;

internal class CmdWorldConfigCreate
{
	private ServerMain server;

	public CmdWorldConfigCreate(ServerMain server)
	{
		this.server = server;
		CommandArgumentParsers parsers = server.api.ChatCommands.Parsers;
		server.api.ChatCommands.Create("worldconfigcreate").RequiresPrivilege(Privilege.controlserver).WithDescription("Add a new world config value")
			.WithArgs(parsers.WordRange("type", "bool", "double", "float", "int", "string"), parsers.Word("key"), parsers.All("value"))
			.HandleWith(handle);
	}

	private TextCommandResult handle(TextCommandCallingArgs args)
	{
		string type = (string)args[0];
		string configname = (string)args[1];
		string newvalue = (string)args[2];
		string result = null;
		switch (type)
		{
		case "bool":
		{
			bool val4 = newvalue.ToBool();
			server.SaveGameData.WorldConfiguration.SetBool(configname, val4);
			result = $"Ok, value {val4} set";
			break;
		}
		case "double":
		{
			double val3 = newvalue.ToDouble();
			server.SaveGameData.WorldConfiguration.SetDouble(configname, val3);
			result = $"Ok, value {val3} set";
			break;
		}
		case "float":
		{
			float val2 = newvalue.ToFloat();
			server.SaveGameData.WorldConfiguration.SetFloat(configname, val2);
			result = $"Ok, value {val2} set";
			break;
		}
		case "string":
			server.SaveGameData.WorldConfiguration.SetString(configname, newvalue);
			result = $"Ok, value {newvalue} set";
			break;
		case "int":
		{
			int val = newvalue.ToInt();
			server.SaveGameData.WorldConfiguration.SetInt(configname, val);
			result = $"Ok, value {val} set";
			break;
		}
		default:
			return TextCommandResult.Error("Invalid or missing datatype");
		}
		return TextCommandResult.Success(result);
	}
}
