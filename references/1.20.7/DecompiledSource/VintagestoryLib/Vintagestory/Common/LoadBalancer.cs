using System;
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Common;

namespace Vintagestory.Common;

internal class LoadBalancer
{
	private volatile int threadsWorking;

	private LoadBalancedTask caller;

	private Logger logger;

	internal LoadBalancer(LoadBalancedTask caller, Logger logger)
	{
		this.caller = caller;
		this.logger = logger;
	}

	internal void CreateDedicatedThreads(int threadCount, string name, List<Thread> threadlist)
	{
		for (int i = 2; i <= threadCount; i++)
		{
			Thread newThread = CreateDedicatedWorkerThread(i, name, threadlist);
			threadlist?.Add(newThread);
		}
	}

	private Thread CreateDedicatedWorkerThread(int threadnum, string name, List<Thread> threadlist = null)
	{
		Thread thread = TyronThreadPool.CreateDedicatedThread(delegate
		{
			caller.StartWorkerThread(threadnum);
		}, name + threadnum);
		thread.IsBackground = false;
		thread.Priority = Thread.CurrentThread.Priority;
		return thread;
	}

	internal void SynchroniseWorkToMainThread(object source)
	{
		if (Interlocked.CompareExchange(ref threadsWorking, 1, 0) != 0)
		{
			throw new Exception("Thread synchronization problem, blame radfast");
		}
		lock (source)
		{
			Monitor.PulseAll(source);
		}
		try
		{
			caller.DoWork(1);
		}
		finally
		{
			while (Interlocked.CompareExchange(ref threadsWorking, 0, 1) != 1 && !caller.ShouldExit())
			{
			}
		}
	}

	internal void SynchroniseWorkOnWorkerThread(object source, int workernum)
	{
		lock (source)
		{
			Monitor.Wait(source, 10000);
		}
		while (Interlocked.CompareExchange(ref threadsWorking, workernum, workernum - 1) != workernum - 1)
		{
			if (caller.ShouldExit())
			{
				return;
			}
		}
		try
		{
			caller.DoWork(workernum);
		}
		catch (ThreadAbortException)
		{
			throw;
		}
		catch (Exception e)
		{
			caller.HandleException(e);
		}
		finally
		{
			Interlocked.Decrement(ref threadsWorking);
		}
	}

	internal void WorkerThreadLoop(object source, int workernum, int msToSleep = 1)
	{
		try
		{
			while (!caller.ShouldExit())
			{
				SynchroniseWorkOnWorkerThread(source, workernum);
			}
		}
		catch (ThreadAbortException)
		{
		}
		catch (Exception e)
		{
			logger.Fatal("Error thrown in worker thread management (this and all higher threads will now stop as a precaution)\n{0}", e.Message);
			logger.Fatal(e);
		}
	}
}
