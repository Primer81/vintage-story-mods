using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class CmdTimeswitch
{
	private ICoreServerAPI sapi;

	public CmdTimeswitch(ICoreServerAPI api)
	{
		sapi = api;
		IChatCommandApi chatCommands = api.ChatCommands;
		CommandArgumentParsers parsers = api.ChatCommands.Parsers;
		chatCommands.Create("timeswitch").WithDescription("Timeswitch and dimensions switching commands").RequiresPrivilege(Privilege.chat)
			.BeginSubCommand("toggle")
			.WithDescription("Toggle timeswitch state for the calling player")
			.RequiresPlayer()
			.HandleWith(ToggleState)
			.EndSubCommand()
			.BeginSubCommand("start")
			.WithDescription("Start the system (to be used by a proximity trigger")
			.HandleWith(Start)
			.EndSubCommand()
			.BeginSubCommand("setpos")
			.WithDescription("Set the chunk column used for timeswitching")
			.WithArgs(parsers.WorldPosition("column position"))
			.HandleWith(SetPos)
			.EndSubCommand()
			.BeginSubCommand("copy")
			.WithDescription("Copy blocks from normal dimension to timeswitch dimension")
			.WithAdditionalInformation("(Destructive of the timeswitch dimension! Use argument 'confirm' to confirm)")
			.WithArgs(parsers.OptionalWord("confirmation"))
			.HandleWith(CopyBlocks)
			.EndSubCommand()
			.BeginSubCommand("relight")
			.WithDescription("Relight the alternate dimension")
			.HandleWith(Relight)
			.EndSubCommand();
	}

	private TextCommandResult ToggleState(TextCommandCallingArgs args)
	{
		if (!(args.Caller.Player is IServerPlayer serverPlayer))
		{
			return TextCommandResult.Error("The toggle command must be called by a currently active player");
		}
		sapi.ModLoader.GetModSystem<Timeswitch>().ActivateTimeswitchServer(serverPlayer, raiseToWorldSurface: false, out var _);
		return TextCommandResult.Success();
	}

	private TextCommandResult CopyBlocks(TextCommandCallingArgs args)
	{
		if ((args.Parsers[0].IsMissing ? "" : (args[0] as string)) != "confirm")
		{
			return TextCommandResult.Error("The copy command will destroy existing blocks in the timeswitch dimension. To confirm, type: /timeswitch copy confirm");
		}
		sapi.ModLoader.GetModSystem<Timeswitch>().CopyBlocksToAltDimension(player: args.Caller.Player as IServerPlayer, sourceblockAccess: sapi.World.BlockAccessor);
		return TextCommandResult.Success();
	}

	private TextCommandResult Relight(TextCommandCallingArgs args)
	{
		sapi.ModLoader.GetModSystem<Timeswitch>().RelightCommand(player: args.Caller.Player as IServerPlayer, sourceblockAccess: sapi.World.BlockAccessor);
		return TextCommandResult.Success();
	}

	private TextCommandResult SetPos(TextCommandCallingArgs args)
	{
		BlockPos pos = (args[0] as Vec3d).AsBlockPos;
		sapi.ModLoader.GetModSystem<Timeswitch>().SetPos(pos);
		return TextCommandResult.Success();
	}

	private TextCommandResult Start(TextCommandCallingArgs args)
	{
		Timeswitch modSystem = sapi.ModLoader.GetModSystem<Timeswitch>();
		IServerPlayer serverPlayer = args.Caller.Player as IServerPlayer;
		modSystem.OnStartCommand(serverPlayer);
		return TextCommandResult.Success();
	}
}
