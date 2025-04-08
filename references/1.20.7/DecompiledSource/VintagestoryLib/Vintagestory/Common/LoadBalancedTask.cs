using System;

namespace Vintagestory.Common;

public interface LoadBalancedTask
{
	void DoWork(int threadNumber);

	bool ShouldExit();

	void HandleException(Exception e);

	void StartWorkerThread(int threadnum);
}
