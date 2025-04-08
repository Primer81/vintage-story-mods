using System;

namespace Vintagestory.Server;

public abstract class ServerSystem
{
	internal ServerMain server;

	public long millisecondsSinceStart;

	public long millisecondsSinceStartSeperateThread;

	internal string FrameprofilerName;

	public virtual int GetUpdateInterval()
	{
		return 1;
	}

	public ServerSystem(ServerMain server)
	{
		this.server = server;
		FrameprofilerName = "ss-tick-" + GetType();
	}

	public virtual void OnServerTick(float dt)
	{
	}

	public virtual void OnSeparateThreadTick()
	{
	}

	[Obsolete("Use OnSeparateThreadTick() instead, dt is likely arbitrary here and no longer used.")]
	public virtual void OnSeperateThreadTick(float dt)
	{
	}

	public virtual void OnRestart()
	{
	}

	public virtual void OnBeginInitialization()
	{
	}

	public virtual void OnBeginConfiguration()
	{
	}

	public virtual void OnLoadAssets()
	{
	}

	public virtual void OnFinalizeAssets()
	{
	}

	public virtual void OnBeginModsAndConfigReady()
	{
	}

	public virtual void OnBeginGameReady(SaveGame savegame)
	{
	}

	public virtual void OnBeginWorldReady()
	{
	}

	public virtual void OnSeperateThreadShutDown()
	{
	}

	public virtual void OnBeginRunGame()
	{
	}

	public virtual void OnBeginShutdown()
	{
	}

	public virtual void OnPlayerJoin(ServerPlayer player)
	{
	}

	public virtual void OnPlayerSwitchGameMode(ServerPlayer player)
	{
	}

	public virtual void OnPlayerDisconnect(ServerPlayer player)
	{
	}

	public virtual void OnServerPause()
	{
	}

	public virtual void OnServerResume()
	{
	}

	public virtual void OnPlayerJoinPost(ServerPlayer player)
	{
	}

	public virtual void Dispose()
	{
	}
}
