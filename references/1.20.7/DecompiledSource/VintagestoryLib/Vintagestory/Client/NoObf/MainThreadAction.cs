using System;

namespace Vintagestory.Client.NoObf;

public class MainThreadAction
{
	private ClientMain game;

	private Func<int> action;

	private string label;

	public MainThreadAction(ClientMain game, Func<int> action, string label)
	{
		this.game = game;
		this.action = action;
		this.label = label;
	}

	public void Enqueue()
	{
		game.EnqueueMainThreadTask(delegate
		{
			action();
		}, label);
	}

	public void Enqueue(Action otherAction)
	{
		game.EnqueueMainThreadTask(otherAction, label);
	}

	public int Invoke()
	{
		return action();
	}
}
