using System.Net.Sockets;

namespace Vintagestory.Client.NoObf;

public class VintageStorySocket : Socket
{
	public bool Disposed { get; private set; }

	public VintageStorySocket(SocketType socketType, ProtocolType protocolType)
		: base(socketType, protocolType)
	{
	}

	public VintageStorySocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
		: base(addressFamily, socketType, protocolType)
	{
	}

	protected override void Dispose(bool disposing)
	{
		Disposed = true;
		base.Dispose(disposing);
	}
}
