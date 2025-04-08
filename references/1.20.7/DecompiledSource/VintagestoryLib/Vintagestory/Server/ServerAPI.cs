using System.Linq;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

internal class ServerAPI : ServerAPIComponentBase, IServerAPI
{
	public ILogger Logger => ServerMain.Logger;

	public bool IsDedicated => server.IsDedicatedServer;

	public int TotalWorldPlayTime => server.SaveGameData.TotalSecondsPlayed;

	public string ServerIp
	{
		get
		{
			if (!server.IsDedicatedServer)
			{
				return server.MainSockets[0].LocalEndpoint;
			}
			return server.MainSockets[1].LocalEndpoint;
		}
	}

	public long TotalReceivedBytes => server.TotalReceivedBytes;

	public long TotalSentBytes => server.TotalSentBytes;

	public long TotalReceivedBytesUdp => server.TotalReceivedBytesUdp;

	public long TotalSentBytesUdp => server.TotalSentBytesUdp;

	public int ServerUptimeSeconds => (int)server.totalUnpausedTime.Elapsed.TotalSeconds;

	public long ServerUptimeMilliseconds => (int)server.totalUnpausedTime.ElapsedMilliseconds;

	public bool IsShuttingDown => server.exit.GetExit();

	public EnumServerRunPhase CurrentRunPhase => server.RunPhase;

	public IServerConfig Config => server.Config;

	public IServerPlayer[] Players => server.PlayersByUid.Values.ToArray();

	public ServerAPI(ServerMain server)
		: base(server)
	{
	}

	public void LogChat(string s)
	{
		ServerMain.Logger.Chat(s);
	}

	public void LogBuild(string message, params object[] args)
	{
		ServerMain.Logger.Build(message, args);
	}

	public void LogChat(string message, params object[] args)
	{
		ServerMain.Logger.Chat(message, args);
	}

	public void LogVerboseDebug(string message, params object[] args)
	{
		ServerMain.Logger.VerboseDebug(message, args);
	}

	public void LogDebug(string message, params object[] args)
	{
		ServerMain.Logger.Debug(message, args);
	}

	public void LogNotification(string message, params object[] args)
	{
		ServerMain.Logger.Notification(message, args);
	}

	public void LogWarning(string message, params object[] args)
	{
		ServerMain.Logger.Warning(message, args);
	}

	public void LogError(string message, params object[] args)
	{
		ServerMain.Logger.Error(message, args);
	}

	public void LogFatal(string message, params object[] args)
	{
		ServerMain.Logger.Fatal(message, args);
	}

	public void LogEvent(string message, params object[] args)
	{
		ServerMain.Logger.Event(message, args);
	}

	public void ShutDown()
	{
		server.AttemptShutdown("Shutdown through Server API", 7500);
	}

	public void MarkConfigDirty()
	{
		server.ConfigNeedsSaving = true;
	}

	public void AddServerThread(string threadname, IAsyncServerSystem system)
	{
		server.AddServerThread(threadname, system);
	}

	public bool PauseThread(string threadname, int waitTimeoutMs = 5000)
	{
		ServerThread t = server.ServerThreadLoops.FirstOrDefault((ServerThread val) => val.threadName == threadname);
		t.ShouldPause = true;
		while (waitTimeoutMs > 0 && !t.paused)
		{
			Thread.Sleep(50);
			waitTimeoutMs -= 50;
		}
		return t.paused;
	}

	public void ResumeThread(string threadname)
	{
		server.ServerThreadLoops.FirstOrDefault((ServerThread val) => val.threadName == threadname).ShouldPause = false;
	}

	public int LoadMiniDimension(IMiniDimension blocks)
	{
		if (server.SaveGameData.MiniDimensionsCreated >= 16777216)
		{
			return -1;
		}
		int index = ++server.SaveGameData.MiniDimensionsCreated;
		server.SetMiniDimension(blocks, index);
		return index;
	}

	public int SetMiniDimension(IMiniDimension blocks, int subId)
	{
		return server.SetMiniDimension(blocks, subId);
	}

	public IMiniDimension GetMiniDimension(int subId)
	{
		return server.GetMiniDimension(subId);
	}

	public void AddPhysicsTickable(IPhysicsTickable entityBehavior)
	{
		server.ServerUdpNetwork.physicsManager.toAdd.Enqueue(entityBehavior);
	}

	public void RemovePhysicsTickable(IPhysicsTickable entityBehavior)
	{
		server.ServerUdpNetwork.physicsManager.toRemove.Enqueue(entityBehavior);
	}
}
