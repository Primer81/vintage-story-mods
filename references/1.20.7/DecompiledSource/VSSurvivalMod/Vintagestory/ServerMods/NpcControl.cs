using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Vintagestory.ServerMods;

public class NpcControl : ModSystem
{
	private ICoreServerAPI sapi;

	private Dictionary<string, long> currentEntityIdByPlayerUid = new Dictionary<string, long>();

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		sapi = api;
		CreateCommands();
		api.Event.OnPlayerInteractEntity += Event_OnPlayerInteractEntity;
	}

	private void CreateCommands()
	{
		CommandArgumentParsers parsers = sapi.ChatCommands.Parsers;
		sapi.ChatCommands.Create("npc").WithDescription("npc commands").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("list")
			.WithDescription("list")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcList)
			.EndSubCommand()
			.BeginSubCommand("enqueue")
			.WithAlias("enq")
			.WithDescription("Enqueue a command")
			.BeginSubCommand("tp")
			.WithDescription("tp")
			.RequiresPlayer()
			.WithArgs(parsers.WorldPosition("position"))
			.HandleWith(OnCmdNpcTp)
			.EndSubCommand()
			.BeginSubCommand("goto")
			.WithDescription("Add a goto command that will move then entity from its current position to the new one using specified animation and speed")
			.RequiresPlayer()
			.WithArgs(parsers.WorldPosition("position"), parsers.Word("animcode"), parsers.OptionalFloat("speed", 0.02f), parsers.OptionalFloat("animspeed", 1f))
			.HandleWith((TextCommandCallingArgs args) => OnCmdNpcEnqGoto(args, astar: false))
			.EndSubCommand()
			.BeginSubCommand("playanim")
			.WithDescription("Add a play animation command")
			.RequiresPlayer()
			.WithArgs(parsers.Word("animcode"), parsers.OptionalFloat("animspeed", 1f))
			.HandleWith(OnCmdNpcEnqPlayanim)
			.EndSubCommand()
			.BeginSubCommand("lookat")
			.WithDescription("Make the npc look at a specific direction in radians")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalFloat("yaw"))
			.HandleWith(OnCmdNpcEnqLookat)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("upd")
			.BeginSubCommand("goto")
			.WithDescription("Update a specific goto command in the command list")
			.RequiresPlayer()
			.WithArgs(parsers.Int("id"), parsers.WordRange("type", "gs", "as"), parsers.Float("speed"))
			.HandleWith(OnCmdNpcGotoUpd)
			.EndSubCommand()
			.BeginSubCommand("lookat")
			.WithDescription("Update a specific lookat command in the command list")
			.RequiresPlayer()
			.WithArgs(parsers.Int("id"), parsers.Float("yaw"))
			.HandleWith(OnCmdNpcLookatUpd)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("start")
			.WithDescription("Start executing the command list")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcStart)
			.EndSubCommand()
			.BeginSubCommand("stop")
			.WithDescription("Stop executing the command list")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcStop)
			.EndSubCommand()
			.BeginSubCommand("clear")
			.WithDescription("Clear all commands in the command list")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcClear)
			.EndSubCommand()
			.BeginSubCommand("exec")
			.WithDescription("Execute a command directly without adding it to the command list")
			.RequiresPlayer()
			.BeginSubCommand("tp")
			.WithDescription("tp")
			.RequiresPlayer()
			.WithArgs(parsers.WorldPosition("position"))
			.HandleWith(OnCmdNpcTp)
			.EndSubCommand()
			.BeginSubCommand("goto")
			.WithDescription("Execute a goto command that will move then entity from its current position to the new one using specified animation and speed")
			.RequiresPlayer()
			.WithArgs(parsers.WorldPosition("position"), parsers.Word("animcode"), parsers.OptionalFloat("speed", 0.02f), parsers.OptionalFloat("animspeed", 1f))
			.HandleWith((TextCommandCallingArgs args) => OnCmdNpcEnqGoto(args, astar: false))
			.EndSubCommand()
			.BeginSubCommand("navigate")
			.WithAlias("nav")
			.WithDescription("Execute a navigate command that will move then entity from its current position using A* pathfinding, to the new one using specified animation and speed")
			.RequiresPlayer()
			.WithArgs(parsers.WorldPosition("position"), parsers.Word("animcode"), parsers.OptionalFloat("speed", 0.02f), parsers.OptionalFloat("animspeed", 1f))
			.HandleWith((TextCommandCallingArgs args) => OnCmdNpcEnqGoto(args, astar: true))
			.EndSubCommand()
			.BeginSubCommand("playanim")
			.WithDescription("Execute a play animation command")
			.RequiresPlayer()
			.WithArgs(parsers.Word("animcode"), parsers.OptionalFloat("animspeed", 1f))
			.HandleWith(OnCmdNpcEnqPlayanim)
			.EndSubCommand()
			.BeginSubCommand("lookat")
			.WithDescription("Make the npc look at a specific yaw [radians] now")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalFloat("yaw"))
			.HandleWith(OnCmdNpcEnqLookat)
			.EndSubCommand()
			.EndSubCommand()
			.BeginSubCommand("loop")
			.WithDescription("Enable looping of all commands in the command list")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalBool("active"))
			.HandleWith(OnCmdNpcLoop)
			.EndSubCommand()
			.BeginSubCommand("remove")
			.WithDescription("Remove a specific command from the list. To see a list with index use /npc list")
			.RequiresPlayer()
			.WithArgs(parsers.Int("id"))
			.HandleWith(OnCmdNpcRemove)
			.EndSubCommand()
			.BeginSubCommand("setname")
			.WithDescription("Set the name of the npc")
			.RequiresPlayer()
			.WithArgs(parsers.Word("name"))
			.HandleWith(OnCmdNpcSetName)
			.EndSubCommand()
			.BeginSubCommand("copyskin")
			.WithDescription("Apply your own skin to the npc if it is skin able")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcCopySkin)
			.EndSubCommand();
		sapi.ChatCommands.Create("npcs").RequiresPrivilege(Privilege.controlserver).WithDescription("Npcs control")
			.BeginSubCommand("startall")
			.WithAlias("startallrandom")
			.WithDescription("Start all loaded npcs")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcsStartAll)
			.EndSubCommand()
			.BeginSubCommand("stopall")
			.WithDescription("Stop all loaded npcs")
			.RequiresPlayer()
			.HandleWith(OnCmdNpcsStopall)
			.EndSubCommand()
			.BeginSubCommand("loopall")
			.WithDescription("Set all loaded npcs loop mode")
			.RequiresPlayer()
			.WithArgs(parsers.OptionalBool("loop_mode"))
			.HandleWith(OnCmdNpcsLoopall)
			.EndSubCommand();
	}

	private TextCommandResult OnCmdNpcsLoopall(TextCommandCallingArgs args)
	{
		bool loop = (bool)args[0];
		foreach (KeyValuePair<long, Entity> loadedEntity in sapi.World.LoadedEntities)
		{
			if (loadedEntity.Value is EntityAnimalBot npc)
			{
				npc.LoopCommands = loop;
			}
		}
		return TextCommandResult.Success("Command list looping is now " + (loop ? "on" : "off"));
	}

	private TextCommandResult OnCmdNpcsStopall(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<long, Entity> loadedEntity in sapi.World.LoadedEntities)
		{
			if (loadedEntity.Value is EntityAnimalBot npc)
			{
				npc.StopExecuteCommands();
			}
		}
		return TextCommandResult.Success("Command lists stopped");
	}

	private TextCommandResult OnCmdNpcsStartAll(TextCommandCallingArgs args)
	{
		foreach (KeyValuePair<long, Entity> val in sapi.World.LoadedEntities)
		{
			Entity value = val.Value;
			EntityAnimalBot npc = value as EntityAnimalBot;
			if (npc == null)
			{
				continue;
			}
			if (args.Command.Name == "startallrandom")
			{
				sapi.Event.RegisterCallback(delegate
				{
					npc.StartExecuteCommands();
				}, (int)(sapi.World.Rand.NextDouble() * 200.0));
			}
			else
			{
				npc.StartExecuteCommands();
			}
		}
		return TextCommandResult.Success("Command lists started");
	}

	private TextCommandResult OnCmdNpcCopySkin(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		EntityBehaviorExtraSkinnable behavior = player.Entity.GetBehavior<EntityBehaviorExtraSkinnable>();
		EntityBehaviorExtraSkinnable botskin = entityNpc.GetBehavior<EntityBehaviorExtraSkinnable>();
		if (behavior == null)
		{
			TextCommandResult.Success("Can't copy, player is not skinnable");
		}
		if (botskin == null)
		{
			TextCommandResult.Success("Can't copy, bot is not skinnable");
		}
		foreach (AppliedSkinnablePartVariant val in behavior.AppliedSkinParts)
		{
			botskin.selectSkinPart(val.PartCode, val.Code, retesselateShape: false);
		}
		entityNpc.WatchedAttributes.MarkPathDirty("skinConfig");
		return TextCommandResult.Success("SkinConfig set.");
	}

	private TextCommandResult OnCmdNpcSetName(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		entityNpc.GetBehavior<EntityBehaviorNameTag>()?.SetName(args[0] as string);
		return TextCommandResult.Success("Name set.");
	}

	private TextCommandResult OnCmdNpcLoop(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		entityNpc.LoopCommands = (args.Parsers[0].IsMissing ? (!entityNpc.LoopCommands) : ((bool)args[0]));
		return TextCommandResult.Success("Command list looping is now " + (entityNpc.LoopCommands ? "on" : "off"));
	}

	private TextCommandResult OnCmdNpcRemove(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		int index = (int)args[0];
		if (index >= 0 && index < entityNpc.Commands.Count)
		{
			entityNpc.Commands.RemoveAt(index);
			return TextCommandResult.Success("Ok, removed given command");
		}
		return TextCommandResult.Success("Index out of range or command list empty");
	}

	private TextCommandResult OnCmdNpcEnqLookat(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		float yaw = (float)args[0];
		if (args.Command.FullName.Contains("exec"))
		{
			entityNpc.ExecutingCommands.Enqueue(new NpcLookatCommand(entityNpc, yaw));
			entityNpc.StartExecuteCommands(enqueue: false);
			return TextCommandResult.Success("Started executing. " + entityNpc.ExecutingCommands.Count + " commands in queue");
		}
		entityNpc.Commands.Add(new NpcLookatCommand(entityNpc, yaw));
		return TextCommandResult.Success("Command enqueued");
	}

	private TextCommandResult OnCmdNpcEnqPlayanim(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		string animCode = args[0] as string;
		float animSpeed = (float)args[1];
		if (args.Command.FullName.Contains("exec"))
		{
			entityNpc.ExecutingCommands.Enqueue(new NpcPlayAnimationCommand(entityNpc, animCode, animSpeed));
			entityNpc.StartExecuteCommands(enqueue: false);
			return TextCommandResult.Success("Started executing. " + entityNpc.ExecutingCommands.Count + " commands in queue");
		}
		entityNpc.Commands.Add(new NpcPlayAnimationCommand(entityNpc, animCode, animSpeed));
		return TextCommandResult.Success("Command enqueued");
	}

	private TextCommandResult OnCmdNpcEnqGoto(TextCommandCallingArgs args, bool astar)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		Vec3d target = (Vec3d)args.Parsers[0].GetValue();
		string animcode = args[1] as string;
		float speed = (float)args[2];
		float animspeed = (float)args[3];
		if (args.Command.FullName.Contains("exec"))
		{
			entityNpc.ExecutingCommands.Enqueue(new NpcGotoCommand(entityNpc, target, astar, animcode, speed, animspeed));
			entityNpc.StartExecuteCommands(enqueue: false);
			return TextCommandResult.Success("Started executing. " + entityNpc.ExecutingCommands.Count + " commands in queue");
		}
		entityNpc.Commands.Add(new NpcGotoCommand(entityNpc, target, astar, animcode, speed, animspeed));
		return TextCommandResult.Success("Command enqueued");
	}

	private TextCommandResult OnCmdNpcTp(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		Vec3d target = (Vec3d)args.Parsers[0].GetValue();
		if (args.Command.FullName.Contains("exec"))
		{
			entityNpc.ExecutingCommands.Enqueue(new NpcTeleportCommand(entityNpc, target));
			entityNpc.StartExecuteCommands(enqueue: false);
			return TextCommandResult.Success("Started executing. " + entityNpc.ExecutingCommands.Count + " commands in queue");
		}
		entityNpc.Commands.Add(new NpcTeleportCommand(entityNpc, target));
		return TextCommandResult.Success("Command enqueued");
	}

	private void Event_OnPlayerInteractEntity(Entity entity, IPlayer byPlayer, ItemSlot slot, Vec3d hitPosition, int mode, ref EnumHandling handling)
	{
		if (entity is EntityAnimalBot && mode == 1)
		{
			currentEntityIdByPlayerUid[byPlayer.PlayerUID] = entity.EntityId;
			(byPlayer as IServerPlayer).SendMessage(GlobalConstants.CurrentChatGroup, "Ok, npc selected", EnumChatType.Notification);
		}
	}

	private TextCommandResult OnCmdNpcExec(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var _, out var msg))
		{
			return msg;
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdNpcClear(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		entityNpc.Commands.Clear();
		if (entityNpc.ExecutingCommands.Count > 0)
		{
			entityNpc.ExecutingCommands.Peek().Stop();
		}
		entityNpc.ExecutingCommands.Clear();
		return TextCommandResult.Success("Command list cleared");
	}

	private TextCommandResult OnCmdNpcStart(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		entityNpc.StartExecuteCommands();
		return TextCommandResult.Success("Started command execution");
	}

	private TextCommandResult OnCmdNpcStop(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		entityNpc.StopExecuteCommands();
		return TextCommandResult.Success("Stopped command execution");
	}

	private TextCommandResult OnCmdNpcGotoUpd(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		int idx = (int)args[0];
		if (idx < 0 || idx > entityNpc.Commands.Count)
		{
			return TextCommandResult.Success("Index out of range");
		}
		if (!(entityNpc.Commands[idx] is NpcGotoCommand cmd))
		{
			return TextCommandResult.Success("fail");
		}
		string type = (string)args[1];
		float speed = (float)args[2];
		if (!(type == "gs"))
		{
			if (type == "as")
			{
				cmd.AnimSpeed = speed;
				return TextCommandResult.Success("Ok animation speed updated to " + cmd.AnimSpeed);
			}
			return TextCommandResult.Success();
		}
		cmd.GotoSpeed = speed;
		return TextCommandResult.Success("Ok goto speed updated to " + cmd.GotoSpeed);
	}

	private TextCommandResult OnCmdNpcLookatUpd(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		int idx = (int)args[0];
		if (idx < 0 || idx > entityNpc.Commands.Count)
		{
			return TextCommandResult.Success("Index out of range");
		}
		if (!(entityNpc.Commands[idx] is NpcLookatCommand cmd))
		{
			return TextCommandResult.Success("fail");
		}
		cmd.yaw = (float)args[1];
		return TextCommandResult.Success("Yaw " + cmd.yaw + " set");
	}

	private TextCommandResult OnCmdNpcList(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (!TryGetCurrentEntity(player, out var entityNpc, out var msg))
		{
			return msg;
		}
		StringBuilder sb = new StringBuilder();
		int i = 0;
		foreach (INpcCommand val in entityNpc.Commands)
		{
			sb.AppendLine(i + ": " + val);
			i++;
		}
		return TextCommandResult.Success(sb.ToString());
	}

	private bool TryGetCurrentEntity(IPlayer player, out EntityAnimalBot entityNpc, out TextCommandResult msg)
	{
		if (!currentEntityIdByPlayerUid.TryGetValue(player.PlayerUID, out var entityid) || entityid == 0L)
		{
			msg = TextCommandResult.Success("Select a npc first");
			entityNpc = null;
			return false;
		}
		sapi.World.LoadedEntities.TryGetValue(entityid, out var entity);
		entityNpc = entity as EntityAnimalBot;
		if (entityNpc == null)
		{
			msg = TextCommandResult.Success("No such npc with this id found");
			return false;
		}
		msg = null;
		return true;
	}
}
