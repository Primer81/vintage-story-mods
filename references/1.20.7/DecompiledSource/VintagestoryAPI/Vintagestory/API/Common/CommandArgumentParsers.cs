using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class CommandArgumentParsers
{
	private ICoreAPI api;

	public CommandArgumentParsers(ICoreAPI api)
	{
		this.api = api;
	}

	public UnparsedArg Unparsed(string argname, params string[] validRange)
	{
		return new UnparsedArg(argname, validRange);
	}

	public DirectionArgParser<Vec3i> IntDirection(string argName)
	{
		return new DirectionArgParser<Vec3i>(argName, isMandatoryArg: true);
	}

	public EntitiesArgParser Entities(string argName)
	{
		return new EntitiesArgParser(argName, api, isMandatoryArg: true);
	}

	/// <summary>
	/// Defaults to caller entity
	/// </summary>
	/// <param name="argName"></param>
	/// <returns></returns>
	public EntitiesArgParser OptionalEntities(string argName)
	{
		return new EntitiesArgParser(argName, api, isMandatoryArg: false);
	}

	public EntityTypeArgParser EntityType(string argName)
	{
		return new EntityTypeArgParser(argName, api, isMandatoryArg: true);
	}

	public IntArgParser IntRange(string argName, int min, int max)
	{
		return new IntArgParser(argName, min, max, 0, isMandatoryArg: true);
	}

	public IntArgParser OptionalIntRange(string argName, int min, int max, int defaultValue = 0)
	{
		return new IntArgParser(argName, min, max, defaultValue, isMandatoryArg: false);
	}

	public IntArgParser OptionalInt(string argName, int defaultValue = 0)
	{
		return new IntArgParser(argName, defaultValue, isMandatoryArg: false);
	}

	public IntArgParser Int(string argName)
	{
		return new IntArgParser(argName, 0, isMandatoryArg: true);
	}

	public LongArgParser OptionalLong(string argName, int defaultValue = 0)
	{
		return new LongArgParser(argName, defaultValue, isMandatoryArg: false);
	}

	public LongArgParser Long(string argName)
	{
		return new LongArgParser(argName, 0L, isMandatoryArg: true);
	}

	public BoolArgParser Bool(string argName, string trueAlias = "on")
	{
		return new BoolArgParser(argName, trueAlias, isMandatoryArg: true);
	}

	public BoolArgParser OptionalBool(string argName, string trueAlias = "on")
	{
		return new BoolArgParser(argName, trueAlias, isMandatoryArg: false);
	}

	public DoubleArgParser OptionalDouble(string argName, double defaultvalue = 0.0)
	{
		return new DoubleArgParser(argName, defaultvalue, isMandatoryArg: false);
	}

	public FloatArgParser Float(string argName)
	{
		return new FloatArgParser(argName, 0f, isMandatoryArg: true);
	}

	public FloatArgParser OptionalFloat(string argName, float defaultvalue = 0f)
	{
		return new FloatArgParser(argName, defaultvalue, isMandatoryArg: false);
	}

	public DoubleArgParser Double(string argName)
	{
		return new DoubleArgParser(argName, 0.0, isMandatoryArg: true);
	}

	public DoubleArgParser DoubleRange(string argName, double min, double max)
	{
		return new DoubleArgParser(argName, min, max, isMandatoryArg: true);
	}

	/// <summary>
	/// A currently online player
	/// </summary>
	/// <param name="argName"></param>
	/// <returns></returns>
	public OnlinePlayerArgParser OnlinePlayer(string argName)
	{
		return new OnlinePlayerArgParser(argName, api, isMandatoryArg: true);
	}

	/// <summary>
	/// All selected players
	/// </summary>
	/// <param name="argName"></param>
	/// <returns></returns>
	public PlayersArgParser PlayerUids(string argName)
	{
		return new PlayersArgParser(argName, api, isMandatoryArg: true);
	}

	/// <summary>
	/// All selected players
	/// </summary>
	/// <param name="argName"></param>
	/// <returns></returns>
	public PlayersArgParser OptionalPlayerUids(string argName)
	{
		return new PlayersArgParser(argName, api, isMandatoryArg: false);
	}

	/// <summary>
	/// Parses IPlayerRole, only works on Serverside since it needs the Serverconfig
	/// </summary>
	/// <param name="argName"></param>
	/// <returns></returns>
	public PlayerRoleArgParser PlayerRole(string argName)
	{
		return new PlayerRoleArgParser(argName, api, isMandatoryArg: true);
	}

	/// <summary>
	/// Parses IPlayerRole, only works on Serverside since it needs the Serverconfig
	/// </summary>
	/// <param name="argName"></param>
	/// <returns></returns>
	public PlayerRoleArgParser OptionalPlayerRole(string argName)
	{
		return new PlayerRoleArgParser(argName, api, isMandatoryArg: false);
	}

	public PrivilegeArgParser Privilege(string privilege)
	{
		return new PrivilegeArgParser(privilege, api, isMandatoryArg: true);
	}

	public PrivilegeArgParser OptionalPrivilege(string privilege)
	{
		return new PrivilegeArgParser(privilege, api, isMandatoryArg: false);
	}

	public WordArgParser Word(string argName)
	{
		return new WordArgParser(argName, isMandatoryArg: true);
	}

	public WordArgParser OptionalWord(string argName)
	{
		return new WordArgParser(argName, isMandatoryArg: false);
	}

	public WordRangeArgParser OptionalWordRange(string argName, params string[] words)
	{
		return new WordRangeArgParser(argName, isMandatoryArg: false, words);
	}

	public WordArgParser Word(string argName, string[] wordSuggestions)
	{
		return new WordArgParser(argName, isMandatoryArg: true, wordSuggestions);
	}

	/// <summary>
	/// Parses a string which is either a color name or a hex value as a <see cref="T:System.Drawing.Color" />
	/// </summary>
	/// <param name="argName"></param>
	/// <returns></returns>
	public ColorArgParser Color(string argName)
	{
		return new ColorArgParser(argName, isMandatoryArg: true);
	}

	/// <summary>
	/// Parses a string which is either a color name or a hex value as a <see cref="T:System.Drawing.Color" />
	/// </summary>
	/// <param name="argName"></param>
	/// <returns></returns>
	public ColorArgParser OptionalColor(string argName)
	{
		return new ColorArgParser(argName, isMandatoryArg: false);
	}

	/// <summary>
	/// All remaining arguments together
	/// </summary>
	/// <param name="argName"></param>
	/// <returns></returns>
	public StringArgParser All(string argName)
	{
		return new StringArgParser(argName, isMandatoryArg: true);
	}

	/// <summary>
	/// All remaining arguments together
	/// </summary>
	/// <param name="argName"></param>
	/// <returns></returns>
	public StringArgParser OptionalAll(string argName)
	{
		return new StringArgParser(argName, isMandatoryArg: false);
	}

	public WordRangeArgParser WordRange(string argName, params string[] words)
	{
		return new WordRangeArgParser(argName, isMandatoryArg: true, words);
	}

	public WorldPositionArgParser WorldPosition(string argName)
	{
		return new WorldPositionArgParser(argName, api, isMandatoryArg: true);
	}

	public WorldPosition2DArgParser WorldPosition2D(string argName)
	{
		return new WorldPosition2DArgParser(argName, api, isMandatoryArg: true);
	}

	public Vec3iArgParser Vec3i(string argName)
	{
		return new Vec3iArgParser(argName, api, isMandatoryArg: true);
	}

	public Vec3iArgParser OptionalVec3i(string argName)
	{
		return new Vec3iArgParser(argName, api, isMandatoryArg: true);
	}

	public CollectibleArgParser Item(string argName)
	{
		return new CollectibleArgParser(argName, api, EnumItemClass.Item, isMandatoryArg: true);
	}

	public CollectibleArgParser Block(string argName)
	{
		return new CollectibleArgParser(argName, api, EnumItemClass.Block, isMandatoryArg: true);
	}

	/// <summary>
	/// Defaults to caller position
	/// </summary>
	/// <param name="argName"></param>
	/// <returns></returns>
	public WorldPositionArgParser OptionalWorldPosition(string argName)
	{
		return new WorldPositionArgParser(argName, api, isMandatoryArg: false);
	}

	/// <summary>
	/// Currently only supports time spans (i.e. now + time)
	/// </summary>
	/// <param name="argName"></param>
	/// <returns></returns>
	public DatetimeArgParser DateTime(string argName)
	{
		return new DatetimeArgParser(argName, isMandatoryArg: true);
	}
}
