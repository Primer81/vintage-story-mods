using System.Net;
using Vintagestory.API.Common;
using Vintagestory.Server;

namespace Vintagestory.Common;

public abstract class NetConnection
{
	public ConnectedClient client { get; set; }

	public abstract IPEndPoint RemoteEndPoint();

	public abstract EnumSendResult Send(byte[] data, bool compressed = false);

	public abstract EnumSendResult HiPerformanceSend(BoxedPacket box, ILogger logger, bool compressionAllowed);

	public abstract bool EqualsConnection(NetConnection connection);

	public abstract void Shutdown();

	public abstract void Close();
}
