using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ClutterBookshelfUtil : ModSystem
{
	private ICoreAPI api;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		api.ChatCommands.GetOrCreate("dev").RequiresPrivilege(Privilege.controlserver).BeginSub("bookshelfvariant")
			.WithDesc("Set book shelf variant")
			.WithArgs(parsers.WorldPosition("block position"), parsers.Word("index or 'random' or 'dec'/'inc' to dec/increment by 1"))
			.HandleWith((TextCommandCallingArgs args) => setBookshelfVariant(args, 1))
			.EndSub()
			.BeginSub("bookshelfvariant2")
			.WithDesc("Set book shelf variant, other side on double sided ones")
			.WithArgs(parsers.WorldPosition("block position"), parsers.Word("index or 'random' or 'dec'/'inc' to dec/increment by 1"))
			.HandleWith((TextCommandCallingArgs args) => setBookshelfVariant(args, 2))
			.EndSub();
	}

	private TextCommandResult setBookshelfVariant(TextCommandCallingArgs args, int type)
	{
		Vec3d pos = args[0] as Vec3d;
		string arg1 = args[1] as string;
		int index = arg1.ToInt(-1);
		BEBehaviorClutterBookshelf beh = api.World.BlockAccessor.GetBlockEntity(pos.AsBlockPos)?.GetBehavior<BEBehaviorClutterBookshelf>();
		if (beh == null)
		{
			return TextCommandResult.Error("Not looking at a bookshelf");
		}
		BookShelfVariantGroup vgroup = (beh.Block as BlockClutterBookshelf).variantGroupsByCode[beh.Variant];
		if (arg1 == "inc" || arg1 == "dec")
		{
			index = GameMath.Mod(vgroup.typesByCode.IndexOfKey(beh.Type) + ((arg1 == "inc") ? 1 : (-1)), vgroup.typesByCode.Count);
		}
		else
		{
			if (index < 0)
			{
				index = api.World.Rand.Next(vgroup.typesByCode.Count);
			}
			if (vgroup.typesByCode.Count <= index)
			{
				return TextCommandResult.Error("Wrong index");
			}
		}
		if (type == 1)
		{
			beh.Type = vgroup.typesByCode.GetKeyAtIndex(index);
		}
		else
		{
			beh.Type2 = vgroup.typesByCode.GetKeyAtIndex(index);
		}
		beh.loadMesh();
		beh.Blockentity.MarkDirty(redrawOnClient: true);
		return TextCommandResult.Success("type " + ((type == 1) ? beh.Type : beh.Type2) + " set.");
	}
}
