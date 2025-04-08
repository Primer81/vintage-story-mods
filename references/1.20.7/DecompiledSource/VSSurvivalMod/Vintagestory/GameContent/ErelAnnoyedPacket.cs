using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ErelAnnoyedPacket
{
	[ProtoMember(1)]
	public bool Annoyed;
}
