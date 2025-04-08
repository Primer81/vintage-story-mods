using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class PlayerVerifyResult
{
	public Packet_ClientIdentification Packet;

	public int Clientid;

	public EnumServerResponse ServerResponse;
}
