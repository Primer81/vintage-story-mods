using System;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

internal class LoadSoundsThread
{
	private readonly ILogger logger;

	private readonly Action onCompleted;

	private ClientMain game;

	public LoadSoundsThread(ILogger logger, ClientMain game, Action onCompleted)
	{
		this.logger = logger;
		this.game = game;
		this.onCompleted = onCompleted;
	}

	public void Process()
	{
		try
		{
			ScreenManager.LoadSoundsInitial();
			logger.Notification("Reloaded sounds, now with mod assets");
		}
		catch (ThreadAbortException)
		{
		}
		catch (Exception e)
		{
			logger.Fatal("Exception in async LoadSounds thread:");
			logger.Fatal(e);
		}
		finally
		{
			onCompleted?.Invoke();
		}
	}

	public void ProcessSlow()
	{
		try
		{
			ScreenManager.LoadSoundsSlow(game);
			logger.Notification("Finished fully loading sounds (async)");
		}
		catch (ThreadAbortException)
		{
		}
		catch (Exception e)
		{
			logger.Fatal("Exception in async LoadSounds thread:");
			logger.Fatal(e);
		}
		finally
		{
			onCompleted?.Invoke();
		}
	}
}
