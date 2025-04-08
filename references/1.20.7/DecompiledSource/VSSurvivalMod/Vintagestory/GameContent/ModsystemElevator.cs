using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.CommandAbbr;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModsystemElevator : ModSystem
{
	private ICoreServerAPI sapi;

	public Dictionary<string, ElevatorSystem> Networks = new Dictionary<string, ElevatorSystem>();

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		CommandArgumentParsers parser = sapi.ChatCommands.Parsers;
		sapi.ChatCommands.GetOrCreate("dev").BeginSubCommand("elevator").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("set-entity-net")
			.WithAlias("sen")
			.WithDescription("Set Elevator network code")
			.WithArgs(parser.Entities("entity"), parser.Word("network code"))
			.HandleWith(OnEntityNetworkSet)
			.EndSubCommand()
			.BeginSubCommand("set-block-net")
			.WithAlias("sbn")
			.WithDescription("Set Elevator network code")
			.WithArgs(parser.Word("netowrk code"), parser.WorldPosition("pos"), parser.OptionalInt("offset"))
			.HandleWith(OnsetBlockNetwork)
			.EndSubCommand()
			.EndSubCommand();
	}

	private TextCommandResult OnsetBlockNetwork(TextCommandCallingArgs args)
	{
		string networkCode = args[0] as string;
		Vec3d pos = args.Parsers[1].GetValue() as Vec3d;
		int offset = (args.Parsers[2].IsMissing ? (-1) : ((int)args[2]));
		BEBehaviorElevatorControl beBehavior = sapi.World.BlockAccessor.GetBlock(pos.AsBlockPos).GetBEBehavior<BEBehaviorElevatorControl>(pos.AsBlockPos);
		if (beBehavior == null)
		{
			return TextCommandResult.Success("Target was not a ElevatorControl block");
		}
		beBehavior.NetworkCode = networkCode;
		beBehavior.Offset = offset;
		sapi.ModLoader.GetModSystem<ModsystemElevator>().RegisterControl(networkCode, pos.AsBlockPos, offset);
		return TextCommandResult.Success("Network code set to " + networkCode);
	}

	private TextCommandResult OnEntityNetworkSet(TextCommandCallingArgs args)
	{
		return CmdUtil.EntityEach(args, delegate(Entity e)
		{
			string text = args[1] as string;
			if (!(e is EntityElevator entityElevator))
			{
				return TextCommandResult.Success("Target was not a elevator");
			}
			entityElevator.NetworkCode = text;
			ElevatorSystem elevatorSys = sapi.ModLoader.GetModSystem<ModsystemElevator>().RegisterElevator(text, entityElevator);
			entityElevator.ElevatorSys = elevatorSys;
			return TextCommandResult.Success("Network code set to " + text);
		});
	}

	public void EnsureNetworkExists(string networkCode)
	{
		if (!string.IsNullOrEmpty(networkCode) && !Networks.ContainsKey(networkCode))
		{
			Networks.TryAdd(networkCode, new ElevatorSystem());
		}
	}

	public ElevatorSystem GetElevator(string networkCode)
	{
		EnsureNetworkExists(networkCode);
		return Networks.GetValueOrDefault(networkCode);
	}

	public ElevatorSystem RegisterElevator(string networkCode, EntityElevator elevator)
	{
		if (Networks.TryGetValue(networkCode, out var network))
		{
			network.Entity = elevator;
			return network;
		}
		Networks.TryAdd(networkCode, new ElevatorSystem
		{
			Entity = elevator
		});
		return Networks[networkCode];
	}

	public void CallElevator(string networkCode, BlockPos position, int offset)
	{
		GetElevator(networkCode)?.Entity.CallElevator(position, offset);
	}

	public void RegisterControl(string networkCode, BlockPos pos, int offset)
	{
		ElevatorSystem entityElevator = GetElevator(networkCode);
		if (entityElevator != null && !entityElevator.ControlPositions.Contains(pos.Y + offset))
		{
			entityElevator.ControlPositions.Add(pos.Y + offset);
			entityElevator.ControlPositions.Sort();
		}
	}

	public void ActivateElevator(string networkCode, BlockPos position, int offset)
	{
		GetElevator(networkCode).Entity.ActivateElevator(position, offset);
	}

	public void DeActivateElevator(string networkCode)
	{
		GetElevator(networkCode).Entity.DeActivateElevator();
	}
}
