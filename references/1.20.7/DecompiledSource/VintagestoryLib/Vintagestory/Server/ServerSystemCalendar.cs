using System;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;

namespace Vintagestory.Server;

internal class ServerSystemCalendar : ServerSystem
{
	private bool serverPause;

	public ServerSystemCalendar(ServerMain server)
		: base(server)
	{
		server.EventManager.OnGameWorldBeingSaved += OnWorldBeingSaved;
		server.EventManager.OnPlayerNowPlaying += EventManager_OnPlayerNowPlaying;
	}

	private void EventManager_OnPlayerNowPlaying(IServerPlayer byPlayer)
	{
		updateGameWorldCalendarRunningState();
	}

	public override void OnBeginModsAndConfigReady()
	{
		ITreeAttribute worldConfig = server.SaveGameData.WorldConfiguration;
		int days = Math.Max(1, worldConfig.GetAsInt("daysPerMonth", 12));
		server.GameWorldCalendar = new GameCalendar(server.AssetManager.Get("textures/environment/sunlight.png"), server.SaveGameData.Seed, 28800 + 86400 * days * 4);
		server.GameWorldCalendar.DaysPerMonth = days;
	}

	public override void OnServerPause()
	{
		serverPause = true;
		updateGameWorldCalendarRunningState();
	}

	public override void OnServerResume()
	{
		serverPause = false;
		updateGameWorldCalendarRunningState();
	}

	public override int GetUpdateInterval()
	{
		return 200;
	}

	public override void OnBeginGameReady(SaveGame savegame)
	{
		if (savegame.TotalGameSeconds < 0)
		{
			ServerMain.Logger.Warning("TotalGameSeconds was negative. Did you accidently set a negative time? This will cause undefined behavior. Clamping back to 0.");
			savegame.TotalGameSeconds = 0L;
		}
		server.GameWorldCalendar.SetTotalSeconds(savegame.TotalGameSeconds, savegame.TotalGameSecondsStart);
		server.GameWorldCalendar.TimeSpeedModifiers = savegame.TimeSpeedModifiers;
		server.GameWorldCalendar.HoursPerDay = savegame.HoursPerDay;
		server.GameWorldCalendar.CalendarSpeedMul = savegame.CalendarSpeedMul;
		server.GameWorldCalendar.Start();
		server.GameWorldCalendar.Tick();
	}

	public void OnWorldBeingSaved()
	{
		if (server.GameWorldCalendar != null)
		{
			server.SaveGameData.TotalGameSeconds = server.GameWorldCalendar.TotalSeconds;
			server.SaveGameData.TimeSpeedModifiers = server.GameWorldCalendar.TimeSpeedModifiers;
			server.SaveGameData.HoursPerDay = server.GameWorldCalendar.HoursPerDay;
			server.SaveGameData.CalendarSpeedMul = server.GameWorldCalendar.CalendarSpeedMul;
		}
	}

	public override void OnPlayerJoin(ServerPlayer player)
	{
		updateGameWorldCalendarRunningState();
		server.SendPacket(player.ClientId, server.GameWorldCalendar.ToPacket());
	}

	public override void OnServerTick(float dt)
	{
		updateGameWorldCalendarRunningState();
		server.GameWorldCalendar.Tick();
		server.SaveGameData.TotalGameSeconds = server.GameWorldCalendar.TotalSeconds;
		if (server.totalUnpausedTime.ElapsedMilliseconds - server.lastUpdateSentToClient > 1000 * MagicNum.CalendarPacketSecondInterval)
		{
			server.BroadcastPacket(server.GameWorldCalendar.ToPacket());
			server.lastUpdateSentToClient = server.totalUnpausedTime.ElapsedMilliseconds;
		}
	}

	private void updateGameWorldCalendarRunningState()
	{
		if (serverPause)
		{
			server.GameWorldCalendar?.Stop();
		}
		else if (server.Config.PassTimeWhenEmpty)
		{
			if (!server.GameWorldCalendar.IsRunning)
			{
				ServerMain.Logger.Notification("Server configured to always pass time, resuming game calendar.");
			}
			server.GameWorldCalendar.Start();
		}
		else if (server.GetPlayingClients() == 0)
		{
			if (server.GameWorldCalendar.IsRunning)
			{
				ServerMain.Logger.Notification("All clients disconnected, pausing game calendar.");
			}
			server.GameWorldCalendar.Stop();
		}
		else
		{
			if (!server.GameWorldCalendar.IsRunning)
			{
				ServerMain.Logger.Notification("A client reconnected, resuming game calendar.");
			}
			server.GameWorldCalendar.Start();
		}
	}
}
