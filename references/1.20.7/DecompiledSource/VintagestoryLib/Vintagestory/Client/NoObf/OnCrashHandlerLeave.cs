namespace Vintagestory.Client.NoObf;

public class OnCrashHandlerLeave : OnCrashHandler
{
	private ClientMain g;

	public static OnCrashHandlerLeave Create(ClientMain game)
	{
		return new OnCrashHandlerLeave
		{
			g = game
		};
	}

	public override void OnCrash()
	{
		g?.SendLeave(1);
	}
}
