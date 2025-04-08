using System;
using System.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public class PlayerRoleArgParser : ArgumentParserBase
{
	private readonly ICoreServerAPI _api;

	private IPlayerRole _value;

	public PlayerRoleArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		_api = api as ICoreServerAPI;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		base.PreProcess(args);
		_value = null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		string roleString = args.RawArgs.PopWord();
		if (roleString == null)
		{
			lastErrorMessage = Lang.Get("Argument is missing");
			return EnumParseResult.Bad;
		}
		_value = _api.Server.Config.Roles.Find((IPlayerRole grp) => grp.Code.Equals(roleString, StringComparison.InvariantCultureIgnoreCase));
		if (_value == null)
		{
			lastErrorMessage = Lang.Get("No such role found: " + string.Join(", ", _api.Server.Config.Roles.Select((IPlayerRole role) => role.Code)));
		}
		if (_value == null)
		{
			return EnumParseResult.Bad;
		}
		return EnumParseResult.Good;
	}

	public override object GetValue()
	{
		return _value;
	}

	public override void SetValue(object data)
	{
		_value = (IPlayerRole)data;
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return _api.Server.Config.Roles.Select((IPlayerRole role) => role.Code).ToArray();
	}
}
