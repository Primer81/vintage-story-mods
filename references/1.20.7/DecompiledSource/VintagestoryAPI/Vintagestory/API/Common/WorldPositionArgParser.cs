using System;
using System.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class WorldPositionArgParser : PositionArgumentParserBase
{
	private Vec3d pos;

	private PositionProviderDelegate mapmiddlePosProvider;

	private ICoreAPI api;

	public WorldPositionArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.api = api;
		mapmiddlePosProvider = () => new Vec3d(api.World.DefaultSpawnPosition.X, 0.0, api.World.DefaultSpawnPosition.Z);
		argCount = 3;
	}

	public override string GetSyntaxExplanation(string indent)
	{
		return indent + GetSyntax() + " is a world position.  A world position can be either 3 coordinates, or a target selector.\n" + indent + "&nbsp;&nbsp;3 coordinates are specified as in the following examples:\n" + indent + "&nbsp;&nbsp;&nbsp;&nbsp;<code>100 150 -180</code> means 100 blocks East and 180 blocks North from the map center, with height 150 blocks\n" + indent + "&nbsp;&nbsp;&nbsp;&nbsp;<code>~-5 ~0 ~4</code> means 5 blocks West and 4 blocks South from the caller's position\n" + indent + "&nbsp;&nbsp;&nbsp;&nbsp;<code>=512100 =150 =511880</code> means the absolute x,y,z position specified (at default settings this is near the map center)\n\n" + indent + "&nbsp;&nbsp;A target selector is either a player's name (meaning that player's current position), or one of: <code>s[]</code> for self, <code>l[]</code> for looked-at entity or block, <code>p[]</code> for players, <code>e[]</code> for entities.\n" + indent + "One or more filters can be specified inside the brackets.  For p[] or e[], the target will be the nearest player or entity which passes all the filters.\n" + indent + "Filters include name, type, class, alive, range.  For example, <code>e[type=gazelle,range=3,alive=true]</code>.  The filters minx/miny/minz/maxx/maxy/maxz can also be used to specify a volume to search, coordinates are relative to the command caller's position.\n";
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return null;
	}

	public override object GetValue()
	{
		return pos;
	}

	public override void SetValue(object data)
	{
		pos = (Vec3d)data;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		pos = args.Caller.Pos?.Clone();
		base.PreProcess(args);
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		string maybeplayername = args.RawArgs.PeekWord();
		IPlayer mplr = api.World.AllOnlinePlayers.FirstOrDefault((IPlayer p) => p.PlayerName.Equals(maybeplayername, StringComparison.InvariantCultureIgnoreCase));
		if (mplr != null)
		{
			args.RawArgs.PopWord();
			pos = mplr.Entity.Pos.XYZ;
			return EnumParseResult.Good;
		}
		char? v = args.RawArgs.PeekChar();
		if (v == 'p' || v == 'e' || v == 'l' || v == 's')
		{
			args.RawArgs.PopChar();
			return tryGetPositionBySelector(v.Value, args, ref pos, api);
		}
		if (args.RawArgs.Length < 3)
		{
			lastErrorMessage = "World position must be either 3 coordinates or a target selector beginning with p (nearest player), e (nearest entity), l (looked at entity/block) or s (executing entity)";
			return EnumParseResult.Bad;
		}
		pos = args.RawArgs.PopFlexiblePos(args.Caller.Pos, mapmiddlePosProvider());
		if (pos == null)
		{
			lastErrorMessage = Lang.Get("Invalid position, must be 3 numbers");
		}
		if (!(pos == null))
		{
			return EnumParseResult.Good;
		}
		return EnumParseResult.Bad;
	}
}
