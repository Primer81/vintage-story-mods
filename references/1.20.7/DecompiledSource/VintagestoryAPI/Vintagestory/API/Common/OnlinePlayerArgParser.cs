using System;
using System.Linq;
using Vintagestory.API.Config;

namespace Vintagestory.API.Common;

public class OnlinePlayerArgParser : ArgumentParserBase
{
	protected ICoreAPI api;

	protected IPlayer player;

	public OnlinePlayerArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.api = api;
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return api.World.AllOnlinePlayers.Select((IPlayer p) => p.PlayerName).ToArray();
	}

	public override object GetValue()
	{
		return player;
	}

	public override void SetValue(object data)
	{
		player = (IPlayer)data;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		base.PreProcess(args);
		player = null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		string playername = args.RawArgs.PopWord();
		if (playername == null)
		{
			lastErrorMessage = Lang.Get("Argument is missing");
			return EnumParseResult.Bad;
		}
		player = api.World.AllOnlinePlayers.FirstOrDefault((IPlayer p) => p.PlayerName == playername);
		if (player == null)
		{
			lastErrorMessage = Lang.Get("No such player online");
		}
		if (player == null)
		{
			return EnumParseResult.Bad;
		}
		return EnumParseResult.Good;
	}
}
