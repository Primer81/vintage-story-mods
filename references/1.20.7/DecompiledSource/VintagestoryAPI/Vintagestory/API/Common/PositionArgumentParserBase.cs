using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public abstract class PositionArgumentParserBase : ArgumentParserBase
{
	protected PositionArgumentParserBase(string argName, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
	}

	protected EnumParseResult tryGetPositionBySelector(char v, TextCommandCallingArgs args, ref Vec3d pos, ICoreAPI api)
	{
		string subargsstr = args.RawArgs.PopWord();
		Dictionary<string, string> dictionary = parseSubArgs(subargsstr);
		Vec3d sourcePos = args.Caller.Pos;
		Entity callingEntity = args.Caller.Entity;
		float? range = null;
		if (dictionary.TryGetValue("range", out var strrange))
		{
			range = strrange.ToFloat();
		}
		AssetLocation type = null;
		if (dictionary.TryGetValue("type", out var typestr))
		{
			type = new AssetLocation(typestr);
		}
		dictionary.TryGetValue("name", out var name);
		bool? alive = null;
		if (dictionary.TryGetValue("alive", out var stralive))
		{
			alive = stralive.ToBool();
		}
		if (range.HasValue && sourcePos == null)
		{
			lastErrorMessage = "Can't use range argument without source position";
			return EnumParseResult.Bad;
		}
		switch (v)
		{
		case 'p':
		{
			IPlayer nearestPlr = null;
			IPlayer[] allOnlinePlayers = api.World.AllOnlinePlayers;
			foreach (IPlayer plr in allOnlinePlayers)
			{
				if ((range.HasValue && plr.Entity.Pos.DistanceTo(sourcePos) > (double?)range) || (name != null && !WildcardUtil.Match(name, plr.PlayerName)) || (alive.HasValue && plr.Entity.Alive != alive))
				{
					continue;
				}
				if (nearestPlr == null)
				{
					nearestPlr = plr;
					continue;
				}
				if (sourcePos == null)
				{
					lastErrorMessage = "Two matching players found. Can't get nearest player without source position";
					return EnumParseResult.Bad;
				}
				if (nearestPlr.Entity.Pos.DistanceTo(sourcePos) > plr.Entity.Pos.DistanceTo(sourcePos))
				{
					nearestPlr = plr;
				}
			}
			pos = nearestPlr?.Entity.Pos.XYZ;
			return EnumParseResult.Good;
		}
		case 'e':
		{
			ICollection<Entity> entities = ((api.Side != EnumAppSide.Server) ? (api as ICoreClientAPI).World.LoadedEntities.Values : (api as ICoreServerAPI).World.LoadedEntities.Values);
			Entity nearestEntity = null;
			foreach (Entity e in entities)
			{
				if ((range.HasValue && e.Pos.DistanceTo(sourcePos) > (double?)range) || (type != null && !WildcardUtil.Match(type, e.Code)) || (alive.HasValue && e.Alive != alive) || (name != null && !WildcardUtil.Match(name, e.GetName())))
				{
					continue;
				}
				if (nearestEntity == null)
				{
					nearestEntity = e;
					continue;
				}
				if (sourcePos == null)
				{
					lastErrorMessage = "Two matching entities found. Can't get nearest entity without source position";
					return EnumParseResult.Bad;
				}
				if (nearestEntity.Pos.DistanceTo(sourcePos) > e.Pos.DistanceTo(sourcePos))
				{
					nearestEntity = e;
				}
			}
			pos = nearestEntity?.Pos.XYZ;
			return EnumParseResult.Good;
		}
		case 'l':
			if (!(callingEntity is EntityPlayer eplr))
			{
				lastErrorMessage = "Can't use 'l' without source player";
				return EnumParseResult.Bad;
			}
			if (eplr.Player.CurrentEntitySelection == null && eplr.Player.CurrentBlockSelection == null)
			{
				lastErrorMessage = "Not looking at an entity or block";
				return EnumParseResult.Bad;
			}
			pos = eplr.Player.CurrentEntitySelection?.Entity.Pos.XYZ ?? eplr.Player.CurrentBlockSelection.Position.ToVec3d();
			return EnumParseResult.Good;
		case 's':
			pos = callingEntity.Pos.XYZ;
			return EnumParseResult.Good;
		default:
			lastErrorMessage = "Wrong selector, needs to be p,e,l or s";
			return EnumParseResult.Bad;
		}
	}
}
