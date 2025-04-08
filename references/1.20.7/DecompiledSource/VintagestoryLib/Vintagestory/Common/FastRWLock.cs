using System.Runtime.CompilerServices;
using System.Threading;

namespace Vintagestory.Common;

public struct FastRWLock
{
	private volatile int currentCount;

	public IShutDownMonitor monitor;

	public FastRWLock(IShutDownMonitor monitor)
	{
		currentCount = 0;
		this.monitor = monitor;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AcquireReadLock()
	{
		if (Interlocked.CompareExchange(ref currentCount, 1, 0) == 0)
		{
			return;
		}
		int readerIndex = 1;
		do
		{
			int current = Interlocked.CompareExchange(ref currentCount, readerIndex + 1, readerIndex);
			if (current == readerIndex)
			{
				break;
			}
			readerIndex = ((current > readerIndex) ? (readerIndex + 1) : 0);
		}
		while (!monitor.ShuttingDown);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void ReleaseReadLock()
	{
		Interlocked.Decrement(ref currentCount);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void AcquireWriteLock()
	{
		while (Interlocked.CompareExchange(ref currentCount, -1, 0) != 0 && !monitor.ShuttingDown)
		{
		}
	}

	public void ReleaseWriteLock()
	{
		currentCount = 0;
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
	}
}
