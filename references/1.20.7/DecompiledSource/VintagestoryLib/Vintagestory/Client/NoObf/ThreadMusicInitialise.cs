using System;
using System.Threading;

namespace Vintagestory.Client.NoObf;

public class ThreadMusicInitialise
{
	private SystemMusicEngine engine;

	private ClientMain game;

	public ThreadMusicInitialise(SystemMusicEngine engine, ClientMain game)
	{
		this.engine = engine;
		this.game = game;
	}

	public void Process()
	{
		try
		{
			engine.EarlyInitialise();
		}
		catch (ThreadAbortException)
		{
		}
		catch (Exception e)
		{
			game.Logger.Fatal("Caught unhandled exception in Music Engine initialisation. Exiting game.");
			game.Logger.Fatal(e);
			game.Platform.XPlatInterface.ShowMessageBox("Client Thread Crash", "Whoops, a client game thread crashed, please check the client-main.log for more Information. I will now exit the game (and stop the server if in singleplayer). Sorry about that :(");
			game.KillNextFrame = true;
		}
	}
}
