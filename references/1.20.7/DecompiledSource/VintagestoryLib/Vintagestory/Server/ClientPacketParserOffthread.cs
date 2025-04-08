using System.Threading;

namespace Vintagestory.Server;

internal class ClientPacketParserOffthread
{
	private ServerMain server;

	public ClientPacketParserOffthread(ServerMain server)
	{
		this.server = server;
	}

	internal void Start()
	{
		try
		{
			while (true)
			{
				try
				{
					if (server.stopped || server.exit.exit)
					{
						break;
					}
					Thread.Sleep(10);
					server.PacketParsingLoop();
					continue;
				}
				catch (ThreadInterruptedException)
				{
					continue;
				}
			}
		}
		catch (ThreadAbortException)
		{
		}
		server.ClientPackets.Clear();
	}
}
