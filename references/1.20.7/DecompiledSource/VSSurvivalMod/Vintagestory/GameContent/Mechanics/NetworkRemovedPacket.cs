using ProtoBuf;

namespace Vintagestory.GameContent.Mechanics;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class NetworkRemovedPacket
{
	public long networkId;
}
