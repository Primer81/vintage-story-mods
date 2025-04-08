using ProtoBuf;

namespace Vintagestory.GameContent;

[ProtoContract]
public class CatchCreaturePacket
{
	[ProtoMember(1)]
	public long entityId;
}
