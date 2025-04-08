using System;
using System.Linq;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class WorldPosition2DArgParser : PositionArgumentParserBase
{
	private Vec2i pos;

	private PositionProviderDelegate mapmiddlePosProvider;

	private ICoreAPI api;

	public WorldPosition2DArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.api = api;
		mapmiddlePosProvider = () => api.World.DefaultSpawnPosition.XYZ;
		argCount = 3;
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
		pos = (Vec2i)data;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		pos = posTo2D(args.Caller.Pos?.Clone());
		base.PreProcess(args);
	}

	private Vec2i posTo2D(Vec3d callerPos)
	{
		if (!(callerPos == null))
		{
			return new Vec2i(callerPos);
		}
		return null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		if (args.RawArgs.Length == 1)
		{
			string maybeplayername = args.RawArgs.PeekWord();
			IPlayer mplr = api.World.AllOnlinePlayers.FirstOrDefault((IPlayer p) => p.PlayerName.Equals(maybeplayername, StringComparison.InvariantCultureIgnoreCase));
			if (mplr != null)
			{
				args.RawArgs.PopWord();
				pos = new Vec2i(mplr.Entity.Pos.XYZ);
				return EnumParseResult.Good;
			}
			char? v = args.RawArgs.PopChar();
			if (v == 'p' || v == 'e' || v == 'l' || v == 's')
			{
				Vec3d pos3d = new Vec3d();
				EnumParseResult result = tryGetPositionBySelector(v.Value, args, ref pos3d, api);
				pos = posTo2D(pos3d);
				return result;
			}
			lastErrorMessage = "World position 2D must be either 2 coordinates or a target selector beginning with p (nearest player), e (nearest entity), l (looked at entity) or s (executing entity)";
			return EnumParseResult.Bad;
		}
		if (args.RawArgs.Length < 2)
		{
			lastErrorMessage = "Need 2 values";
			return EnumParseResult.Good;
		}
		pos = args.RawArgs.PopFlexiblePos2D(args.Caller.Pos, mapmiddlePosProvider());
		if (pos == null)
		{
			lastErrorMessage = Lang.Get("Invalid position, must be 2 numbers");
		}
		if (!(pos == null))
		{
			return EnumParseResult.Good;
		}
		return EnumParseResult.Bad;
	}
}
