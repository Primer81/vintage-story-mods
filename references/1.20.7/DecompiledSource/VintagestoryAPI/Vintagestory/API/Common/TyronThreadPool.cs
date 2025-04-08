using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Vintagestory.API.Config;

namespace Vintagestory.API.Common;

public class TyronThreadPool
{
	public static TyronThreadPool Inst = new TyronThreadPool();

	public ILogger Logger;

	public ConcurrentDictionary<int, string> RunningTasks = new ConcurrentDictionary<int, string>();

	public ConcurrentDictionary<string, Thread> DedicatedThreads = new ConcurrentDictionary<string, Thread>();

	private int keyCounter;

	private int dedicatedCounter;

	public TyronThreadPool()
	{
		ThreadPool.SetMaxThreads(10, 1);
	}

	private int MarkStarted(string caller)
	{
		int key = keyCounter++;
		RunningTasks[key] = caller;
		return key;
	}

	private void MarkEnded(int key)
	{
		RunningTasks.TryRemove(key, out var _);
	}

	public string ListAllRunningTasks()
	{
		StringBuilder sb = new StringBuilder();
		foreach (string name in RunningTasks.Values)
		{
			if (sb.Length > 0)
			{
				sb.Append(", ");
			}
			sb.Append(name);
		}
		if (sb.Length == 0)
		{
			sb.Append("[empty]");
		}
		sb.AppendLine();
		return "Current threadpool tasks: " + sb.ToString() + "\nThread pool thread count: " + ThreadPool.ThreadCount;
	}

	public string ListAllThreads()
	{
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("Server threads (" + DedicatedThreads.Count + "):");
		foreach (KeyValuePair<string, Thread> entry in DedicatedThreads)
		{
			Thread t2 = entry.Value;
			if (t2.ThreadState != System.Threading.ThreadState.Stopped)
			{
				sb.Append("tid" + t2.ManagedThreadId + " ");
				sb.Append(entry.Key);
				sb.Append(": ");
				sb.AppendLine(t2.ThreadState.ToString());
			}
		}
		ProcessThreadCollection threads2 = Process.GetCurrentProcess().Threads;
		List<ProcessThread> threads = new List<ProcessThread>();
		foreach (ProcessThread thread2 in threads2)
		{
			if (thread2 != null)
			{
				threads.Add(thread2);
			}
		}
		threads = threads.OrderByDescending((ProcessThread t) => t.UserProcessorTime.Ticks).ToList();
		sb.AppendLine("\nAll process threads (" + threads.Count + "):");
		foreach (ProcessThread thread in threads)
		{
			if (thread != null)
			{
				sb.Append(thread.ThreadState.ToString() + " ");
				sb.Append("tid" + thread.Id + " ");
				if (RuntimeEnv.OS != OS.Mac)
				{
					sb.Append(thread.StartTime);
				}
				sb.Append(": P ");
				sb.Append(thread.CurrentPriority);
				sb.Append(": ");
				sb.Append(thread.ThreadState.ToString());
				sb.Append(": T ");
				sb.Append(thread.UserProcessorTime.ToString());
				sb.Append(": T Total ");
				sb.AppendLine(thread.TotalProcessorTime.ToString());
			}
		}
		return sb.ToString();
	}

	public static void QueueTask(Action callback, string caller)
	{
		int key = Inst.MarkStarted(caller);
		QueueTask(callback);
		Inst.MarkEnded(key);
	}

	public static void QueueLongDurationTask(Action callback, string caller)
	{
		int key = Inst.MarkStarted(caller);
		QueueLongDurationTask(callback);
		Inst.MarkEnded(key);
	}

	public static void QueueTask(Action callback)
	{
		if (RuntimeEnv.DebugThreadPool)
		{
			Inst.Logger.VerboseDebug("QueueTask." + Environment.StackTrace);
		}
		ThreadPool.QueueUserWorkItem(delegate
		{
			callback();
		});
	}

	public static void QueueLongDurationTask(Action callback)
	{
		if (RuntimeEnv.DebugThreadPool)
		{
			Inst.Logger.VerboseDebug("QueueTask." + Environment.StackTrace);
		}
		ThreadPool.QueueUserWorkItem(delegate
		{
			callback();
		});
	}

	public static Thread CreateDedicatedThread(ThreadStart starter, string name)
	{
		Thread thread = new Thread(starter);
		thread.IsBackground = true;
		thread.Name = name;
		Inst.DedicatedThreads[name + "." + Inst.dedicatedCounter++] = thread;
		return thread;
	}

	public void Dispose()
	{
		RunningTasks.Clear();
		DedicatedThreads.Clear();
		keyCounter = 0;
		dedicatedCounter = 0;
	}
}
