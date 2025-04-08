using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityCommands : BlockEntity
{
	public string Commands = "";

	public string[] CallingPrivileges;

	public bool Silent;

	public bool executing;

	public virtual void Execute(Caller caller, string Commands)
	{
		if (Commands != null && Api.Side == EnumAppSide.Server && !executing)
		{
			string[] commands = Commands.Split(new string[1] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
			executing = true;
			try
			{
				execCommands(commands, caller);
			}
			catch
			{
				executing = false;
			}
			if (commands.Length != 0 && caller.Player != null)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/toggleswitch"), Pos, 0.0, null, randomizePitch: false, 16f, 0.5f);
			}
		}
	}

	public void FinishExecution()
	{
		executing = false;
	}

	protected virtual void execCommands(IEnumerable<string> commands, Caller caller)
	{
		caller.Type = EnumCallerType.Block;
		caller.Pos = Pos.ToVec3d();
		caller.CallerPrivileges = CallingPrivileges;
		List<string> commandsAfterWait = null;
		int waitMs = 0;
		foreach (string command in commands)
		{
			if (commandsAfterWait == null && command.StartsWithOrdinal("/wait"))
			{
				waitMs = command.Split(' ')[1].ToInt();
				commandsAfterWait = new List<string>();
				continue;
			}
			if (commandsAfterWait != null)
			{
				commandsAfterWait.Add(command);
				continue;
			}
			Api.ChatCommands.ExecuteUnparsed(command, new TextCommandCallingArgs
			{
				Caller = caller
			}, delegate(TextCommandResult result)
			{
				if (!Silent)
				{
					Api.Logger.Notification("{0}: {1}", command, result.StatusMessage);
				}
			});
		}
		if (commandsAfterWait != null)
		{
			Api.Event.RegisterCallback(delegate
			{
				execCommands(commandsAfterWait, caller);
			}, waitMs);
		}
		else
		{
			FinishExecution();
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		Commands = tree.GetString("commands");
		Silent = tree.GetBool("silent");
		CallingPrivileges = (tree["callingPrivileges"] as StringArrayAttribute)?.value;
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetString("commands", Commands);
		tree.SetBool("silent", Silent);
		if (CallingPrivileges != null)
		{
			tree["callingPrivileges"] = new StringArrayAttribute(CallingPrivileges);
		}
	}
}
