using System.Runtime.CompilerServices;
using System.Threading;

namespace Vintagestory.Common;

public struct FastRWLockWithPriority
{
	private volatile int currentCount;

	private volatile int readLockAttempt;

	public IShutDownMonitor monitor;

	public FastRWLockWithPriority(IShutDownMonitor monitor)
	{
		readLockAttempt = 0;
		currentCount = 0;
		this.monitor = monitor;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int AcquireWriteLock(int bit)
	{
		while (readLockAttempt != 0 && !monitor.ShuttingDown)
		{
		}
		int current = Interlocked.Or(ref currentCount, bit);
		int attempts = 1;
		do
		{
			if (current >= 0)
			{
				return attempts;
			}
			Thread.SpinWait(1);
			attempts++;
			Thread.MemoryBarrier();
			current = currentCount;
		}
		while (!monitor.ShuttingDown);
		return 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ReleaseWriteLock(int bit)
	{
		Interlocked.And(ref currentCount, ~bit);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AcquireReadLock()
	{
		do
		{
			if (Interlocked.CompareExchange(ref currentCount, int.MinValue, 0) == 0)
			{
				readLockAttempt = 0;
				return;
			}
			readLockAttempt = 1;
		}
		while (!monitor.ShuttingDown);
		readLockAttempt = 0;
	}

	public void ReleaseReadLock()
	{
		Interlocked.And(ref currentCount, int.MaxValue);
	}

	internal void WaitUntilFree()
	{
		while (currentCount != 0)
		{
		}
	}

	internal void Reset()
	{
		currentCount = 0;
		readLockAttempt = 0;
	}
}
