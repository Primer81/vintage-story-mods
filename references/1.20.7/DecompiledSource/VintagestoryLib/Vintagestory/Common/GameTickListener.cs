using System;

namespace Vintagestory.Common;

public class GameTickListener : GameTickListenerBase
{
	public Action<float> Handler;

	public Action<Exception> ErrorHandler;

	public void OnTriggered(long ellapsedMilliseconds)
	{
		try
		{
			Handler((float)(ellapsedMilliseconds - LastUpdateMilliseconds) / 1000f);
		}
		catch (Exception e)
		{
			if (ErrorHandler == null)
			{
				throw;
			}
			ErrorHandler(e);
		}
		LastUpdateMilliseconds = ellapsedMilliseconds;
	}

	public object Origin()
	{
		return Handler.Target;
	}
}
