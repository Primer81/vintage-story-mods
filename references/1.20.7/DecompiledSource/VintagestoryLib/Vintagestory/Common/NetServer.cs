using System;

namespace Vintagestory.Common;

public abstract class NetServer : IDisposable
{
	public abstract string Name { get; }

	public abstract string LocalEndpoint { get; }

	public abstract void SetIpAndPort(string ip, int port);

	public abstract void Start();

	public abstract NetIncomingMessage ReadMessage();

	public abstract void Dispose();
}
