using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class ServerSystemAsync : ServerSystem
{
	private IAsyncServerSystem system;

	public ServerSystemAsync(ServerMain server, string name, IAsyncServerSystem system)
		: base(server)
	{
		base.server = server;
		this.system = system;
		FrameprofilerName = "ss-tick-" + name;
	}

	public override int GetUpdateInterval()
	{
		return system.OffThreadInterval();
	}

	public override void OnSeparateThreadTick()
	{
		system.OnSeparateThreadTick();
	}

	public override void Dispose()
	{
		system.ThreadDispose();
	}
}
