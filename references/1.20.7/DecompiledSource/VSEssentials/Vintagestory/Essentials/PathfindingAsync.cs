using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.Essentials;

public class PathfindingAsync : ModSystem, IAsyncServerSystem
{
	protected ICoreServerAPI api;

	protected AStar astar_offthread;

	protected AStar astar_mainthread;

	protected bool isShuttingDown;

	public ConcurrentQueue<PathfinderTask> PathfinderTasks = new ConcurrentQueue<PathfinderTask>();

	protected readonly Stopwatch totalTime = new Stopwatch();

	protected long lastTickTimeMs;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		base.StartServerSide(api);
		this.api = api;
		astar_offthread = new AStar(api);
		astar_mainthread = new AStar(api);
		api.Event.ServerRunPhase(EnumServerRunPhase.Shutdown, delegate
		{
			isShuttingDown = true;
		});
		api.Event.RegisterGameTickListener(OnMainThreadTick, 20);
		api.Server.AddServerThread("ai-pathfinding", this);
	}

	public int OffThreadInterval()
	{
		return 5;
	}

	protected void OnMainThreadTick(float dt)
	{
		int initialCount = PathfinderTasks.Count;
		if (initialCount <= 1)
		{
			return;
		}
		api.World.FrameProfiler.Enter("ai-pathfinding-overflow " + initialCount);
		int maxCount = 1000;
		PathfinderTask task;
		while ((task = Next()) != null && maxCount-- > 0)
		{
			task.waypoints = astar_mainthread.FindPathAsWaypoints(task.startBlockPos, task.targetBlockPos, task.maxFallHeight, task.stepHeight, task.collisionBox, task.searchDepth, task.mhdistanceTolerance);
			task.Finished = true;
			if (isShuttingDown)
			{
				break;
			}
			api.World.FrameProfiler.Mark("path d:" + task.searchDepth + " r:" + ((task.waypoints == null) ? "fail" : task.waypoints.Count.ToString()) + " s:" + task.startBlockPos?.ToString() + " e:" + task.targetBlockPos?.ToString() + " w:" + task.collisionBox.Width);
		}
		api.World.FrameProfiler.Leave();
	}

	public void OnSeparateThreadTick()
	{
		ProcessQueue(astar_offthread, 100);
	}

	public void ProcessQueue(AStar astar, int maxCount)
	{
		PathfinderTask task;
		while ((task = Next()) != null && maxCount-- > 0)
		{
			try
			{
				task.waypoints = astar.FindPathAsWaypoints(task.startBlockPos, task.targetBlockPos, task.maxFallHeight, task.stepHeight, task.collisionBox, task.searchDepth, task.mhdistanceTolerance, task.CreatureType);
			}
			catch (Exception e)
			{
				task.waypoints = null;
				api.World.Logger.Error("Exception thrown during pathfinding. Will ignore. Exception: {0}", e.ToString());
			}
			task.Finished = true;
			if (isShuttingDown)
			{
				break;
			}
		}
	}

	protected PathfinderTask Next()
	{
		PathfinderTask task = null;
		if (!PathfinderTasks.TryDequeue(out task))
		{
			task = null;
		}
		return task;
	}

	public void EnqueuePathfinderTask(PathfinderTask task)
	{
		PathfinderTasks.Enqueue(task);
	}

	public override void Dispose()
	{
		astar_mainthread?.Dispose();
		astar_mainthread = null;
	}

	public void ThreadDispose()
	{
		astar_offthread.Dispose();
		astar_offthread = null;
	}
}
