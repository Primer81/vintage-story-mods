using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class PrivilegeArgParser : ArgumentParserBase
{
	private string Value;

	private ICoreServerAPI _api;

	public PrivilegeArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		if (api is ICoreServerAPI serverApi)
		{
			_api = serverApi;
		}
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		base.PreProcess(args);
		Value = null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		string privilegeString = args.RawArgs.PopWord();
		if (privilegeString == null)
		{
			lastErrorMessage = Lang.Get("Argument is missing");
			return EnumParseResult.Bad;
		}
		if (_api != null)
		{
			HashSet<string> privileges = new HashSet<string>();
			_api.Server.Config.Roles.ForEach(delegate(IPlayerRole r)
			{
				privileges.AddRange(r.Privileges);
			});
			Value = privileges.First((string privilege) => privilege.Equals(privilegeString, StringComparison.InvariantCultureIgnoreCase));
		}
		else
		{
			Value = Privilege.AllCodes().First((string privilege) => privilege.Equals(privilegeString, StringComparison.InvariantCultureIgnoreCase));
		}
		if (Value == null)
		{
			lastErrorMessage = Lang.Get("No such privilege found: " + string.Join(", ", Privilege.AllCodes()));
		}
		if (Value == null)
		{
			return EnumParseResult.Bad;
		}
		return EnumParseResult.Good;
	}

	public override object GetValue()
	{
		return Value;
	}

	public override void SetValue(object data)
	{
		Value = (string)data;
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return Privilege.AllCodes();
	}
}
