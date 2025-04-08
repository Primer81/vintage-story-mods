using ProtoBuf;

namespace Vintagestory.GameContent.Mechanics;

[ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
public class MechClientRequestPacket
{
	public long networkId;
}
