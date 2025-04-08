using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class MyceliumSystem : ModSystem
{
	private ICoreAPI api;

	public static LCGRandom lcgrnd;

	public static NormalRandom rndn;

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		api.ChatCommands.GetOrCreate("debug").BeginSubCommand("myc").BeginSubCommand("regrow")
			.WithDescription("MyceliumSystem debug cmd")
			.RequiresPrivilege(Privilege.controlserver)
			.HandleWith(OnCmd)
			.EndSubCommand()
			.EndSubCommand();
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		this.api = api;
	}

	private void Event_SaveGameLoaded()
	{
		lcgrnd = new LCGRandom(api.World.Seed);
		rndn = new NormalRandom(api.World.Seed);
	}

	private TextCommandResult OnCmd(TextCommandCallingArgs args)
	{
		BlockPos pos = args.Caller.Entity.Pos.XYZ.AsBlockPos;
		if (!(api.World.BlockAccessor.GetBlockEntity(pos.DownCopy()) is BlockEntityMycelium bemc))
		{
			return TextCommandResult.Success("No mycelium below you");
		}
		bemc.Regrow();
		return TextCommandResult.Success();
	}
}
