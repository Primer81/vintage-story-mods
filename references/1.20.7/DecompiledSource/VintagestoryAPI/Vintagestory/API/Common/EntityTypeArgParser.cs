using System;
using System.Linq;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.Common;

public class EntityTypeArgParser : ArgumentParserBase
{
	private ICoreAPI api;

	private EntityProperties type;

	public EntityTypeArgParser(string argName, ICoreAPI api, bool isMandatoryArg)
		: base(argName, isMandatoryArg)
	{
		this.api = api;
	}

	public override string GetSyntaxExplanation(string indent)
	{
		return indent + GetSyntax() + " is one of the registered entityTypes";
	}

	public override object GetValue()
	{
		return type;
	}

	public override void PreProcess(TextCommandCallingArgs args)
	{
		base.PreProcess(args);
		type = null;
	}

	public override EnumParseResult TryProcess(TextCommandCallingArgs args, Action<AsyncParseResults> onReady = null)
	{
		string str = args.RawArgs.PopWord();
		if (str == null)
		{
			lastErrorMessage = "Missing";
			return EnumParseResult.Bad;
		}
		type = api.World.GetEntityType(new AssetLocation(str));
		if (type == null)
		{
			lastErrorMessage = "No such entity type exists";
			return EnumParseResult.Bad;
		}
		return EnumParseResult.Good;
	}

	public override string[] GetValidRange(CmdArgs args)
	{
		return api.World.EntityTypes.Select((EntityProperties t) => t.Code.ToShortString()).ToArray();
	}

	public override void SetValue(object data)
	{
		type = (EntityProperties)data;
	}
}
