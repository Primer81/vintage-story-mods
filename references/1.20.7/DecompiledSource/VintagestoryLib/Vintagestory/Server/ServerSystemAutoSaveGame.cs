using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class ServerSystemAutoSaveGame : ServerSystem
{
	private long milliSecondsSinceSave;

	public ServerSystemAutoSaveGame(ServerMain server)
		: base(server)
	{
		server.api.ChatCommands.Create("autosavenow").RequiresPrivilege(Privilege.controlserver).HandleWith(delegate
		{
			doAutoSave();
			return TextCommandResult.Success("Autosave completed");
		});
	}

	private void onCmdAutoSave(IServerPlayer player, int groupId, CmdArgs args)
	{
		doAutoSave();
		player.SendMessage(groupId, "Autosave completed", EnumChatType.CommandSuccess);
	}

	public override int GetUpdateInterval()
	{
		return 500;
	}

	public override void OnServerTick(float dt)
	{
		if (MagicNum.ServerAutoSave > 0 && (millisecondsSinceStart - milliSecondsSinceSave) / 1000 > MagicNum.ServerAutoSave && server.RunPhase == EnumServerRunPhase.RunGame && server.readyToAutoSave)
		{
			doAutoSave();
		}
	}

	private void doAutoSave()
	{
		if (server.chunkThread.runOffThreadSaveNow)
		{
			ServerMain.Logger.Warning("Call to autosave, but server is already saving. May indicate a disk i/o bottleneck. Reduce autosave interval or improve file i/o. Will ignore this autosave call.");
			return;
		}
		ServerMain.FrameProfiler.Mark("autosave - preparing for autosave");
		if (!Monitor.TryEnter(server.suspendLock, 5000))
		{
			return;
		}
		try
		{
			ServerMain.FrameProfiler.Mark("autosave - obtaining lock");
			if (!server.Suspend(newSuspendState: true, 3000))
			{
				ServerMain.Logger.Notification("Unable to autosave, was not able to pause the server");
				server.Suspend(newSuspendState: false);
				return;
			}
			if (!server.Saving)
			{
				server.Saving = true;
				ServerMain.FrameProfiler.Mark("autosave - pausing server");
				ServerMain.Logger.Notification("Autosaving game world. Notifying mods, then systems of save...");
				server.SendMessageToGroup(GlobalConstants.ServerInfoChatGroup, "Saving game world....", EnumChatType.Notification);
				ServerMain.FrameProfiler.Mark("autosave - notifying players");
				server.EventManager.TriggerGameWorldBeingSaved();
				server.Saving = false;
				milliSecondsSinceSave = millisecondsSinceStart;
			}
			server.Suspend(newSuspendState: false);
			ServerMain.FrameProfiler.Mark("autosave");
		}
		finally
		{
			Monitor.Exit(server.suspendLock);
		}
	}
}
