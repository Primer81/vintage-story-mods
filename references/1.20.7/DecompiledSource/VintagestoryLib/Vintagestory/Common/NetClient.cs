using System;

namespace Vintagestory.Common;

public abstract class NetClient
{
	public abstract int CurrentlyReceivingBytes { get; }

	public abstract void Connect(string ip, int port, Action<ConnectionResult> OnConnectionResult, Action<Exception> OnDisconnected);

	public abstract NetIncomingMessage ReadMessage();

	public abstract void Send(byte[] data);

	public virtual void Dispose()
	{
	}
}
