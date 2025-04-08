using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public class EntitiesArgParser : ArgumentParserBase
{
	private Entity[] entities;

	private ICoreAPI api;

	public EntitiesArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.api = api;
	}

	public override string GetSyntaxExplanation(string indent)
	{
		return indent + GetSyntax() + " is either a player name, or else one of the following selection codes:\n" + indent + "  s[] for self\n" + indent + "  l[] for the entity currently looked at\n" + indent + "  p[] for all players\n" + indent + "  e[] for all entities.\n" + indent + "  Inside the square brackets, one or more filters can be added, to be more selective.  Filters include name, type, class, alive, range.  For example, <code>e[type=gazelle,range=3,alive=true]</code>.  The filters minx/miny/minz/maxx/maxy/maxz can also be used to specify a volume to search, coordinates are relative to the command caller's position.\n" + indent + "  This argument may be omitted if the remainder of the command makes sense, in which case it will be interpreted as self.";
	}

	public override object GetValue()
	{
		return entities;
	}

	public override void SetValue(object data)
	{
		entities = (Entity[])data;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		entities = null;
		base.PreProcess(args);
		if (base.IsMissing)
		{
			entities = new Entity[1] { args.Caller.Entity };
		}
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		string maybeplayername = args.RawArgs.PeekWord();
		IPlayer mplr = api.World.AllOnlinePlayers.FirstOrDefault((IPlayer p) => p.PlayerName.Equals(maybeplayername, StringComparison.InvariantCultureIgnoreCase));
		if (mplr != null)
		{
			args.RawArgs.PopWord();
			this.entities = new Entity[1] { mplr.Entity };
			return EnumParseResult.Good;
		}
		string text = maybeplayername;
		char v = ((text != null && text.Length > 0) ? maybeplayername[0] : ' ');
		if (v != 'p' && v != 'e' && v != 'l' && v != 's')
		{
			lastErrorMessage = Lang.Get("Not a player name and not a selector p, e, l or s: {0}'", maybeplayername);
			this.entities = new Entity[1] { args.Caller.Entity };
			return EnumParseResult.DependsOnSubsequent;
		}
		v = args.RawArgs.PopChar().GetValueOrDefault(' ');
		Dictionary<string, string> subargs;
		if (args.RawArgs.PeekChar() == '[')
		{
			string errorMsg;
			string subargsstr = args.RawArgs.PopCodeBlock('[', ']', out errorMsg);
			if (errorMsg != null)
			{
				lastErrorMessage = errorMsg;
				return EnumParseResult.Bad;
			}
			subargs = parseSubArgs(subargsstr);
		}
		else
		{
			if (args.RawArgs.PeekChar() != ' ')
			{
				lastErrorMessage = "Invalid selector, needs to be p,e,l,s followed by [";
				return EnumParseResult.Bad;
			}
			args.RawArgs.PopWord();
			subargs = new Dictionary<string, string>();
		}
		Vec3d sourcePos = args.Caller.Pos;
		Entity callingEntity = args.Caller.Entity;
		float? range = null;
		if (subargs.TryGetValue("range", out var strrange))
		{
			range = strrange.ToFloat();
			subargs.Remove("range");
		}
		AssetLocation type = null;
		if (subargs.TryGetValue("type", out var typestr))
		{
			type = new AssetLocation(typestr);
			subargs.Remove("type");
		}
		string classstr = null;
		if (subargs.TryGetValue("class", out classstr))
		{
			classstr = classstr.ToLowerInvariant();
			subargs.Remove("class");
		}
		string name = null;
		if (subargs.TryGetValue("name", out name))
		{
			subargs.Remove("name");
		}
		bool? alive = null;
		if (subargs.TryGetValue("alive", out var stralive))
		{
			alive = stralive.ToBool();
			subargs.Remove("alive");
		}
		long? id = null;
		if (subargs.TryGetValue("id", out var strid))
		{
			id = strid.ToLong(0L);
			subargs.Remove("id");
		}
		Cuboidi box = null;
		if (sourcePos != null)
		{
			bool hasBox = false;
			string[] codes = new string[6] { "minx", "miny", "minz", "maxx", "maxy", "maxz" };
			int[] values = new int[6];
			for (int i = 0; i < codes.Length; i++)
			{
				if (subargs.TryGetValue(codes[i], out var val))
				{
					values[i] = val.ToInt() + i / 3;
					subargs.Remove(codes[i]);
					hasBox = true;
				}
			}
			if (hasBox)
			{
				BlockPos center = sourcePos.AsBlockPos;
				box = new Cuboidi(values).Translate(center.X, center.Y, center.Z);
			}
		}
		if (subargs.Count > 0)
		{
			lastErrorMessage = "Unknown selector '" + string.Join(", ", subargs.Keys) + "'";
			return EnumParseResult.Bad;
		}
		List<Entity> foundEntities = new List<Entity>();
		if (range.HasValue && sourcePos == null)
		{
			lastErrorMessage = "Can't use range argument without source pos";
			return EnumParseResult.Bad;
		}
		switch (v)
		{
		case 'p':
		{
			IPlayer[] allOnlinePlayers = api.World.AllOnlinePlayers;
			foreach (IPlayer plr in allOnlinePlayers)
			{
				if (entityMatches(plr.Entity, sourcePos, type, classstr, range, box, name, alive, id))
				{
					foundEntities.Add(plr.Entity);
				}
			}
			this.entities = foundEntities.ToArray();
			return EnumParseResult.Good;
		}
		case 'e':
			if (!range.HasValue)
			{
				ICollection<Entity> entities = ((api.Side != EnumAppSide.Server) ? (api as ICoreClientAPI).World.LoadedEntities.Values : (api as ICoreServerAPI).World.LoadedEntities.Values);
				foreach (Entity e2 in entities)
				{
					if (entityMatches(e2, sourcePos, type, classstr, range, box, name, alive, id))
					{
						foundEntities.Add(e2);
					}
				}
				this.entities = foundEntities.ToArray();
			}
			else
			{
				float r = range.Value;
				this.entities = api.World.GetEntitiesAround(sourcePos, r, r, (Entity e) => entityMatches(e, sourcePos, type, classstr, range, box, name, alive, id));
			}
			return EnumParseResult.Good;
		case 'l':
		{
			if (!(callingEntity is EntityPlayer eplr))
			{
				lastErrorMessage = "Can't use 'l' without source player";
				return EnumParseResult.Bad;
			}
			if (eplr.Player.CurrentEntitySelection == null)
			{
				lastErrorMessage = "Not looking at an entity";
				return EnumParseResult.Bad;
			}
			Entity lookedAtEntity = eplr.Player.CurrentEntitySelection.Entity;
			if (entityMatches(lookedAtEntity, sourcePos, type, classstr, range, box, name, alive, id))
			{
				this.entities = new Entity[1] { lookedAtEntity };
			}
			else
			{
				this.entities = new Entity[0];
			}
			return EnumParseResult.Good;
		}
		case 's':
			if (entityMatches(callingEntity, sourcePos, type, classstr, range, box, name, alive, id))
			{
				this.entities = new Entity[1] { callingEntity };
			}
			else
			{
				this.entities = new Entity[0];
			}
			return EnumParseResult.Good;
		default:
			lastErrorMessage = "Wrong selector, needs to be a player name or p,e,l or s";
			return EnumParseResult.Bad;
		}
	}

	private bool entityMatches(Entity e, Vec3d sourcePos, AssetLocation type, string classstr, float? range, Cuboidi box, string name, bool? alive, long? id)
	{
		if (id.HasValue && e.EntityId != id)
		{
			return false;
		}
		if (range.HasValue && e.SidedPos.DistanceTo(sourcePos) > (double?)range)
		{
			return false;
		}
		if (box != null && !box.ContainsOrTouches(e.SidedPos))
		{
			return false;
		}
		if (classstr != null && classstr != e.Class.ToLowerInvariant())
		{
			return false;
		}
		if (type != null && !WildcardUtil.Match(type, e.Code))
		{
			return false;
		}
		if (alive.HasValue && e.Alive != alive)
		{
			return false;
		}
		if (name != null && !WildcardUtil.Match(name, e.GetName()))
		{
			return false;
		}
		return true;
	}
}
